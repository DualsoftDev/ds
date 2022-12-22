namespace PLC.CodeGen.LSXGI

open System.Collections.Generic
open Engine.Common.FS
open PLC.CodeGen.LSXGI

//open FSharpPlus
//open QuickGraph
//open Dual.Core
//open Dual.Core.QGraph
//open Dual.Core.Types
//open Dual.Core.ModelPostProcessor
//open SequenceValidator
//open TestFramework


/// PLC 생성에 대한 validation.  Unit test 용으로 사용.
[<AutoOpen>]
module Validation =

    /// XG5000 XML statements 생성
    let internal generateRungStatements (opt:CodeGenerationOption) (rootVertices:seq<IVertex>) (edges:seq<IEdge>) (segments:ISegment seq) =
        let ladderInfo = processSystem rootVertices edges segments opt
        let statements = ladderInfo.Rungs |> Seq.groupBy(fun ri -> ri.GetCoilTerminal()) |> map(rungInfoToStatement opt) |> toList

        let plctags = statementToTag statements |> distinct |> toList

        statements, plctags

    let generateNameByOption (generator:(IVertex -> string) option) v =
        match generator with
        | Some(f) -> (f v)
        | _ -> v.ToText()


    /// 모델을 분석해서 PLC rung 생성에 필요한 정보를 생성하고,
    /// 해당 rung 정보로부터 Start trigger 신호를 살린 이후에
    /// Tag 값들이 scan 별로 변동되는 정보를 반환한다.
    ///
    /// return type : [(scanCounter, [tagName, newValue])]
    let getTagValueChangedSequence opt (rootVertices:seq<IVertex>) (edges:seq<IEdge>) (segments:ISegment seq)  =
        let statements, tags = generateRungStatements opt rootVertices edges segments

        /// 사용된 PLCTag 를 BitTag 로 변환한 dictionary
        let dic =
            tags
            |> map (fun t -> t.Name, BitTag(t.Name))
            |> Tuple.toDictionary

        let conditionsAndCoils =
            statements |> map (fun s -> s.Condition |> toBitTagExpression dic, s.Command.CoilTerminalTag.ToText())

        /// 초기값이 Finish인 Vertex들
        let initFinishVertices = rootVertices |> Seq.collect(getAllVertices2) |> Seq.where(fun v -> v.InitialStatus = VertexStatus.Finish)
        let forceFinishKeys = initFinishVertices |> Seq.map(generateNameByOption opt.ForceFinishNameGenerator) |> Seq.map(fun str -> str + "_Status")
        let forceSensorKeys = initFinishVertices |> Seq.map(fun v -> v.SensorPort.GetTag().ToText())

        /// BitTag 로 표현된 expression terminal 의 boolean 값을 평가한다.
        /// PulseTag 로 사용되면, scan counter 값을 이용해서 rising/falling 을 평가한다.
        let terminalEvaluator (sc:int) =
            let f =
                fun (terminal:IExpressionTerminal) ->
                    let v = (terminal :?> BitTag).Value
                    match terminal with
                    | :? PulseTag as pt ->
                        // 이전 scan과 비교해서 현재 scan 에서 변경된 것만 인정
                        (sc - pt.ScanCounter = 1) && v
                    | _ -> v
            f

        let history:FullScanHistory =
            let singleScan (sc:int): SingleScanHistory =
                [
                    for cond, coil in conditionsAndCoils do
                        let tgtCoil = dic.[coil]
                        let newValue = Expression.evaluate (terminalEvaluator sc) cond
                        let oldValue = tgtCoil.Value
                        if oldValue <> newValue then
                            tracefn "Coil %s changed to %b" coil newValue
                            tgtCoil.Value <- newValue
                            tgtCoil.ScanCounter <- sc
                            yield coil, newValue
                ]

            /// 초기값이 Finish인 애들 강제 Finish
            dic |> Seq.where(fun kv -> forceFinishKeys.Contains(kv.Key)) |> Seq.iter(fun kv -> kv.Value.Value <- true)

            /// 수동조작 초기값이 finish 상태가 될때까지 스캔
            while dic |> Seq.where(fun kv -> forceSensorKeys.Contains(kv.Key)) |> Seq.where(fun kv -> kv.Value.Value = false) |> Seq.any do
                singleScan 0 |> ignore

            /// 수동 메모리 off
            dic |> Seq.where(fun kv -> forceFinishKeys.Contains(kv.Key)) |> Seq.iter(fun kv -> kv.Value.Value <- false)
            dic.["O_Root"].Value <- true

            singleScan 0 |> ignore
            singleScan 0 |> ignore

            dic.["O_Root"].Value <- false

            let rec scan (sc:int) = // sc for scan counter
                [
                    let ss = singleScan sc
                    if ss.IsEmpty then
                        ()
                    else
                        yield sc, ss
                        yield! scan (sc+1)
                ]

            scan 0

        history
