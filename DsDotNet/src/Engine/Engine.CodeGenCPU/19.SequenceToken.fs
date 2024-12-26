[<AutoOpen>]
module Engine.CodeGenCPU.SequenceToken

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS
open Engine.Common


let checkMultipleTransitionGroups(sys: DsSystem, transEdges: seq<Edge>) =
    let groups = 
        transEdges
        |> Seq.groupBy (fun e -> e.Target.GetPureReal())

    let multipleTransitionGroups = 
        groups
        |> Seq.filter (fun (_, group) -> Seq.length group > 1)
        |> Seq.toList

    if multipleTransitionGroups.Length > 0 then
        let detailedErrorInfo = 
            multipleTransitionGroups
            |> List.map (fun (target, group) -> 
                let edgesInfo = 
                    group
                    |> Seq.map (fun e ->  
                        sprintf "%s.%s > %s.%s" (e.Source.Flow.Name) (e.Source.Name) (e.Target.Flow.Name) (e.Target.Name))
                    |> String.concat "\n"
                let targetPure = target.GetPureReal()
                sprintf "전송타겟작업 : %s.%s\n연결정보:\n%s" (targetPure.Flow.Name) (targetPure.Name) edgesInfo)
            |> String.concat "\n\n"

        failwithf "복수의 작업에서 SEQ 전송을 시도하고 있습니다. \r\n미전송 작업 이름에 취소선을 사용하세요: %s\n\n%s" sys.Name detailedErrorInfo


let applyVertexToken(sys: DsSystem) =
    let fn = getFuncName()
    let edges = sys.GetFlowEdges()
                    .Where(fun e->e.EdgeType.HasFlag(EdgeType.Start))
                    .Where(fun e->e.Source.TryGetPureReal().IsSome && e.Target.TryGetPureReal().IsSome)
                |> Seq.distinctBy(fun e -> (e.Source.GetPureReal(), e.Target.GetPureReal()))

    let noTransEdges= edges.Where(fun e->e.Source.GetPureReal().NoTransData)
    let transEdges  = edges.Except noTransEdges

    let andEdgeCheck = transEdges.Where(fun e->e.Target:?Real) // alias Or 처리아니고 And Edge인 경우만 체크
    // 그룹 내부에 아이템이 1개 초과인 경우 체크
    checkMultipleTransitionGroups(sys, andEdgeCheck)
    //let groups = 
    //    transEdges
    //    |> Seq.groupBy (fun e -> e.Target.GetPureReal())

    [|
        //for (tgt, items) in groups do
        for edge in transEdges do
            let src = edge.Source.GetPureReal()
            let tgt = edge.Target.GetPureReal()
            let data = src.VR.RealTokenData
            yield (src.VR.F.Expr <&&> tgt.VR.R.Expr, data.ToExpression()) --> (tgt.VR.RealTokenData, fn) 

        for edge in noTransEdges do
            let src = edge.Source.GetPureReal()
            let data = src.VR.RealTokenData
            yield (src.VR.GP.Expr, data.ToExpression()) --> (src.VR.MergeTokenData, fn) 
    |]
