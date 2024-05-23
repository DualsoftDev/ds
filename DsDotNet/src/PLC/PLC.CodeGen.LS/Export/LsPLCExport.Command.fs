namespace PLC.CodeGen.LS

open System
open System.Linq

open PLC.CodeGen.Common
open PLC.CodeGen.LS.Config.POU.Program.LDRoutine
open Dual.Common.Core.FS
open Engine.Core
open FB
open ConvertorPrologModule
open Engine.Core.ExpressionModule

[<AutoOpen>]
module internal rec Command =
    /// Rung 의 Command 정의를 위한 type.
    //Command = CoilCmd or FunctionCmd or FunctionBlockCmd
    type CommandTypes with

        member x.CoilTerminalTag =
            /// Terminal End Tag
            let tet (fc: #IFunctionCommand) = fc.TerminalEndTag

            match x with
            | CoilCmd cc -> tet (cc)
            | PredicateCmd pc -> tet (pc)
            | FunctionCmd fc -> tet (fc)
            | FunctionBlockCmd fbc -> tet (fbc)
            | ActionCmd _ac -> failwithlog "ERROR: check"

        member x.InstanceName =
            match x with
            | FunctionBlockCmd(fbc) -> fbc.GetInstanceText()
            | _ -> failwithlog "do not make instanceTag"

        member x.VarType =
            match x with
            | FunctionBlockCmd(fbc) ->
                match fbc with
                | TimerMode ts ->
                    match ts.Timer.Type with
                    | TON -> VarType.TON
                    | TOF -> VarType.TOFF
                    | TMR -> VarType.TMR

                | CounterMode cs ->
                    match cs.Counter.Type with
                    | CTU -> VarType.CTU_INT
                    | CTD -> VarType.CTD_INT
                    | CTUD -> VarType.CTUD_INT
                    | CTR -> VarType.CTR
            | _ -> failwithlog "do not make instanceTag"

        member x.LDEnum =
            match x with
            | CoilCmd(cc) ->
                match cc with
                | COMCoil _ -> ElementType.CoilMode
                | COMClosedCoil _ -> ElementType.ClosedCoilMode
                | COMSetCoil _ -> ElementType.SetCoilMode
                | COMResetCoil _ -> ElementType.ResetCoilMode
                | COMPulseCoil _ -> ElementType.PulseCoilMode
                | COMNPulseCoil _ -> ElementType.NPulseCoilMode
            | (PredicateCmd _ | FunctionCmd _ | FunctionBlockCmd _ | ActionCmd _) -> ElementType.VertFBMode

    //   /// Coil의 부정 Command를 반환한다.
    //member x.``ReverseCmd 사용안함`` () =
    //   match cmdType with
    //   | CoilCmd (cc) ->
    //       match cc with
    //       | COMCoil(tag) -> CoilCmd(CoilOutputMode.COMClosedCoil(tag))
    //       | COMClosedCoil(tag) -> CoilCmd(CoilOutputMode.COMCoil(tag))
    //       | _ ->
    //           failwithlogf "This ReverseCmd is not support"
    //   | _ ->
    //       failwithlogf "This ReverseCmd is not support"

    //let createOutputCoil(tag)    = CoilCmd(CoilOutputMode.COMCoil(tag))
    //let createOutputCoilNot(tag) = CoilCmd(CoilOutputMode.COMClosedCoil(tag))
    //let createOutputSet(tag)     = CoilCmd(CoilOutputMode.COMSetCoil(tag))
    //let createOutputRst(tag)     = CoilCmd(CoilOutputMode.COMResetCoil(tag))
    //let createOutputPulse(tag)   = CoilCmd(CoilOutputMode.COMPulseCoil(tag))
    //let createOutputNPulse(tag)  = CoilCmd(CoilOutputMode.COMNPulseCoil(tag))

    type IExpression with
        member x.GetTerminalString (prjParam: XgxProjectParams) =
            match x.Terminal with
            | Some t ->
                match t.Variable, t.Literal with
                | Some v, None -> v.Name
                | None, Some (:? ILiteralHolder as lh) ->
                    match prjParam.TargetType with
                    | XGK -> lh.ToTextWithoutTypeSuffix()
                    | _ -> lh.ToText()
                | _ -> failwith "ERROR: Unknown terminal literal case."
            | _ -> failwith "ERROR: Not a Terminal"

    /// Option<IExpression<bool>> to IExpression
    let private obe2e (obe: IExpression<bool> option) : IExpression = obe.Value :> IExpression
    let private flatten (exp: IExpression) = exp.Flatten() :?> FlatExpression

    // <timer> for XGI
    let private bxiFunctionBlockTimer (prjParam: XgxProjectParams) (x, y) (timerStatement: TimerStatement)  target: BlockXmlInfo =
        let ts = timerStatement
        let typ = ts.Timer.Type     // TON, TOF, TMR
        let time: int = int ts.Timer.PRE.Value

        let inputParameters =
            [ "PT", (literal2expr $"T#{time}MS") :> IExpression
              "IN", obe2e ts.RungInCondition
              match typ with
              | TMR -> "RST", obe2e ts.ResetCondition
              | _ -> () ]

        let outputParameters = []

        let blockXml =
            let cmd = FunctionBlockCmd(TimerMode(ts))
            bxiFunctionBlockInstanceXmls prjParam (x, y) cmd inputParameters outputParameters target

        blockXml

    let private bxiFunctionBlockCounter (prjParam: XgxProjectParams) (x, y) (counterStatement: CounterStatement) target: BlockXmlInfo =
        assert(prjParam.TargetType = XGI)
        //let paramDic = Dictionary<string, FuctionParameterShape>()
        let cs = counterStatement
        let pv = int16 cs.Counter.PRE.Value
        let typ = cs.Counter.Type

        let inputParameters =
            [ "PV", (literal2expr pv) :> IExpression
              match typ with
              | CTU -> // cu, r, pv,       q, cv
                  "CU", obe2e cs.UpCondition
                  "R", obe2e cs.ResetCondition
              | CTD -> // cd, ld, pv,       q, cv
                  "CD", obe2e cs.DownCondition
                  "LD", obe2e cs.LoadCondition
              | CTUD -> // cu, cd, r, ld, pv,       qu, qd, cv
                  "CU", obe2e cs.UpCondition
                  "CD", obe2e cs.DownCondition
                  "R", obe2e cs.ResetCondition
                  "LD", obe2e cs.LoadCondition
              | CTR -> // cd, pv, rst,       q, cv
                  "CD", obe2e cs.DownCondition
                  "RST", obe2e cs.ResetCondition ]

        let outputParameters = []

        let blockXml =
            let cmd = FunctionBlockCmd(CounterMode(cs))
            bxiFunctionBlockInstanceXmls prjParam (x, y) cmd inputParameters outputParameters target

        blockXml


    type System.Type with

        member x.GetSizeString(target:PlatformTarget) = systemTypeToXgxTypeName target x


    let bxiPredicate (prjParam: XgxProjectParams) (x, y) (predicate: Predicate) target: BlockXmlInfo =
        match predicate with
        | Compare(name, output, args) ->
            let namedInputParameters =
                [ "EN", fakeAlwaysOnExpression :> IExpression ]
                @ (args |> List.indexed |> List.map1st (fun n -> $"IN{n + 1}"))

            let outputParameters = [ "OUT", output ]

            let func =
                match name with
                | ("GT" | "GE" | "EQ" | "LE" | "LT" | "NE") ->
                    let opCompType = args[0].DataType.GetSizeString(target)

                    if name = "NE" then
                        $"{name}_{opCompType}" // NE_BOOL
                    else
                        $"{name}2_{opCompType}" // e.g "GT2_INT"
                | _ -> failwithlog "NOT YET"

            bxiBox prjParam (x, y) func namedInputParameters outputParameters "" target

    let bxiFunction (prjParam: XgxProjectParams) (x, y) (func: Function) (target:PlatformTarget): BlockXmlInfo =
        match func with
        | Arithmatic(name, output, args) ->
            let namedInputParameters =
                [ "EN", fakeAlwaysOnExpression :> IExpression ]
                @ (args |> List.indexed |> List.map1st (fun n -> $"IN{n + 1}"))

            let outputParameters = [ "OUT", output ]

            let plcFuncType =
                let outputType = getType output
                systemTypeToXgxTypeName target outputType

            let func =
                // argument 갯수에 따라서 다른 함수를 불러야 할 때 사용.  e.g "ADD3_INT" : 3개의 인수를 더하는 함수
                let arity = args.Length

                match name with
                | ("ADD" | "MUL") -> $"{name}{arity}_{plcFuncType}"
                | ("SUB" | "DIV") -> name // DIV 는 DIV, DIV2 만 존재함
                | _ -> failwithlog "NOT YET"

            bxiBox prjParam (x, y) func namedInputParameters outputParameters "" target

    let bxiAction (prjParam: XgxProjectParams) (x, y) (func: PLCAction) targetPLC: BlockXmlInfo =
        match func with
        | Move(condition, source, target) ->
            let namedInputParameters = [ "EN", condition :> IExpression; "IN", source ]

            let output = target :?> INamedExpressionizableTerminal
            let outputParameters = [ "OUT", output ]
            bxiBox prjParam (x, y) XgiConstants.FunctionNameMove namedInputParameters outputParameters "" targetPLC

    let bxiFunctionBlockInstanceXmls
            (prjParam: XgxProjectParams)
            (rungStartX, rungStartY)
            (cmd: CommandTypes)
            (namedInputParameters: (string * IExpression) list)
            (namedOutputParameters: (string * INamedExpressionizableTerminal) list)
            target
        : BlockXmlInfo =
            let func = cmd.VarType.ToString()
            let instanceName = cmd.InstanceName
            bxiBox prjParam (rungStartX, rungStartY) func namedInputParameters namedOutputParameters instanceName target

    /// cmd 인자로 주어진 function block 의 type 과
    /// namedInputParameters 로 주어진 function block 에 연결된 다릿발 정보를 이용해서
    /// function block rung 을 그린다.
    let bxiBox
            (prjParam: XgxProjectParams)
            (rungStartX, rungStartY)
            (functionName: string)
            (namedInputParameters: (string * IExpression) list)
            (namedOutputParameters: (string * INamedExpressionizableTerminal) list)
            (instanceName: string)
            (targetType : PlatformTarget)
        : BlockXmlInfo =
            let iDic = namedInputParameters |> dict
            let oDic = namedOutputParameters |> Tuple.toDictionary

            let systemTypeToXgxType (typ: System.Type) =
                systemTypeToXgxTypeName targetType typ |> DU.tryParseEnum<CheckType> |> Option.get

            /// 입력 인자들을 function 의 입력 순서 맞게 재배열
            let alignedInputParameters =
                /// e.g ["CD, 0x00200001, , 0"; "LD, 0x00200001, , 0"; "PV, 0x00200040, , 0"]
                let inputSpecs = getFunctionInputSpecs functionName |> Array.ofSeq

                namedInputParameters.Length = inputSpecs.Length
                |> verifyM "ERROR: Function input parameter mismatch."

                [| for s in inputSpecs do
                       let exp = iDic[s.Name]
                       let exprDataType = systemTypeToXgxType exp.DataType

                       let typeCheckExcludes = [ "TON"; "TOF"; "RTO"; "CTU"; "CTD"; "CTUD"; "CTR" ]

                       if (typeCheckExcludes.Any(fun ex -> functionName = ex || functionName.StartsWith($"{ex}_"))) then
                           () // xxx: timer, counter 에 대해서는 일단, type check skip
                       else
                           s.CheckType.HasFlag(exprDataType) |> verify

                       s.Name, exp, s.CheckType |]

            /// 출력 인자들을 function 의 출력 순서 맞게 재배열
            let alignedOutputParameters =
                /// e.g ["ENO, 0x00200001, , 0"; "OUT, 0x00200001, , 0";]
                let outputSpecs = getFunctionOutputSpecs functionName |> Array.ofSeq

                [ for (i, s) in outputSpecs.Indexed() do
                      option {
                          let! terminal = oDic.TryFind(s.Name)

                          match terminal with
                          | :? IStorage as storage -> s.CheckType.HasFlag(systemTypeToXgxType storage.DataType) |> verify
                          | _ -> ()

                          return s.Name, i, terminal, s.CheckType
                      } ]
                |> List.choose id

            let (x, y) = (rungStartX, rungStartY)

            /// y 위치에 literal parameter 쓸 공간 확보 (x 좌표는 아직 미정)
            let reservedLiteralInputParam = ResizeArray<int * IExpression>()
            let mutable sy = 0

            let inputBlockXmls =
                [ for (portOffset, (_name, exp, checkType)) in alignedInputParameters.Indexed() do
                      if portOffset > 0 && exp.Terminal.IsSome then
                          (y + portOffset, exp) |> reservedLiteralInputParam.Add
                          sy <- sy + 1
                      else
                          checkType.HasFlag CheckType.BOOL
                          |> verifyM "ERROR: Only BOOL type can be used as compound expression for input."

                          let blockXml = bxiFunctionInputLadderBlock prjParam (x, y + sy) (flatten exp)
                          portOffset, blockXml
                          sy <- sy + blockXml.TotalSpanY ]

            /// 입력 parameter 를 그렸을 때, 1 줄을 넘는 것들의 갯수 만큼 horizontal line spacing 필요
            let plusHorizontalPadding = inputBlockXmls.Count(fun (_, x) -> x.TotalSpanY > 1)

            /// function start X
            let fsx = inputBlockXmls.Max(fun (_, x) -> x.TotalSpanX) + plusHorizontalPadding

            let outputCellXmls =
                [ for (_portOffset, (_name, yoffset, terminal, _checkType)) in alignedOutputParameters.Indexed() do
                      rxiFBParameter (fsx + 1, y + yoffset) terminal.StorageName ]

            /// 문어발: input parameter end 와 function input adaptor 와의 'S' shape 연결
            let tentacleXmls =
                [ for (inputBlockIndex, (portOffset, b)) in inputBlockXmls.Indexed() do
                      let i = inputBlockIndex
                      let bex = b.X + b.TotalSpanX // block end X
                      let bey = b.Y
                      let c = coord (bex, bey)
                      let spanX = (fsx - bex)

                      if b.TotalSpanX > 1 then
                          /// 'S' shape 의 하단부 수평선 끝점 x 좌표
                          let hEndX = if i = 0 then fsx - 1 else bex + i - 1

                          yield!
                              tryHlineTo (bex, bey) (hEndX)
                              |> map (fun xml ->
                                  tracefn $"H: ({bex}, {bey}) -> ({hEndX}, {bey})"

                                  { Coordinate = c
                                    Xml = xml
                                    SpanX = spanX
                                    SpanY = 1 })

                          if i > 0 then
                              let bexi = bex + i
                              let yi = y + portOffset
                              tracefn $"V: ({bexi - 1}, {bey}) -> [({bexi - 1}, {yi})]"
                              // 'S' shape 의 세로선 그리기
                              yield! rxisVLineUpTo (bexi - 1, bey) yi

                              // 'S' shape 의 상단부 수평선 그리기
                              yield!
                                  tryHlineTo (bexi, yi) (fsx - 1)
                                  |> map (fun xml ->
                                      tracefn $"H: ({bexi}, {yi}) -> [({bexi}, {fsx - 1})]"

                                      let c2 = coord (bexi, yi)
                                      { Coordinate = c2
                                        Xml = xml
                                        SpanX = spanX
                                        SpanY = 1 }) ]

            let allXmls =
                [
                  (* Timer 의 PT, Counter 의 PV 등의 상수 값을 입력 모선에서 연결하지 않고, function cell 에 바로 입력 하기 위함*)
                  for (ry, rexp) in reservedLiteralInputParam do
                      let literal =
                          match rexp.Terminal with
                          | Some terminal ->
                              match terminal.Literal, terminal.Variable with
                              | Some(:? ILiteralHolder as literal), None -> literal.ToTextWithoutTypeSuffix()
                              | Some literal, None -> literal.ToText()
                              | None, Some variable -> variable.Name
                              | _ -> failwithlog "ERROR"
                          | _ -> failwithlog "ERROR"

                      rxiFBParameter (x + fsx - 1, ry) literal

                  yield! inputBlockXmls |> bind (fun (_, bx) -> bx.XmlElements)
                  yield! outputCellXmls
                  yield! tentacleXmls
                  let x, y = rungStartX, rungStartY

                  //Command 결과출력
                  rxiFunctionAt (functionName, functionName) instanceName (x + fsx, y) ]


            { X = x
              Y = y
              TotalSpanX = fsx + 3
              TotalSpanY = max sy (allXmls.Max(fun x -> x.SpanY))
              XmlElements = allXmls |> List.sortBy (fun x -> x.Coordinate) }


    /// (x, y) 위치에 cmd 를 생성.  cmd 가 차지하는 height 와 xml 목록을 반환
    let bxiCommand (prjParam: XgxProjectParams) (x, y) (cmd: CommandTypes) : BlockXmlInfo =
        match prjParam.TargetType with
        | XGI ->
            match cmd with
            | PredicateCmd(pc) -> bxiPredicate prjParam (x, y) pc XGI
            | FunctionCmd(fc) -> bxiFunction prjParam (x, y) fc XGI
            | ActionCmd(ac) -> bxiAction prjParam (x, y) ac XGI
            | FunctionBlockCmd(fbc) ->
                match fbc with
                | TimerMode(timerStatement) -> bxiFunctionBlockTimer prjParam (x, y) timerStatement XGI
                | CounterMode(counterStatement) -> bxiFunctionBlockCounter prjParam (x, y) counterStatement XGI
            | _ -> failwithlog "Unknown CommandType"

        | XGK ->
            match cmd with
            | FunctionBlockCmd(fbc) -> bxiXgkFBCommand prjParam (x, y) fbc
            | XgkParamCmd(param, width) -> bxiXgkFBCommandWithParam (x, y) (param, width)
            | _ -> failwithlog "Unknown CommandType"

        | _ -> failwithlog $"Unknown Target: {prjParam.TargetType}"

    /// (x, y) 위치에 coil 생성.  height(=1) 와 xml 목록을 반환
    let bxiCoil (x, y) (cmdExp: CommandTypes) (coilText:string) : BlockXmlInfo =
        let spanX = max 0 (coilCellX - x - 2)

        let xmls =
            [
                if spanX > 0 then
                    let c = coord (x + 1, y)
                    let lengthParam = $"Param={dq}{3 * spanX}{dq}"
                    let xml = elementFull (int ElementType.MultiHorzLineMode) c lengthParam ""

                    { Coordinate = c
                      Xml = xml
                      SpanX = spanX
                      SpanY = 1 }

                let c = coord (coilCellX, y)
                let xml = elementBody (int cmdExp.LDEnum) c coilText        // coilText: XGK 에서는 직접변수를, XGI 에서는 변수명을 사용

                { Coordinate = c
                  Xml = xml
                  SpanX = 1
                  SpanY = 1 } ]

        { X = x
          Y = y
          TotalSpanX = 31
          TotalSpanY = 1
          XmlElements = xmls }


    let bxiXgkFBCommandWithParam (x, y) (cmdParam: string, cmdWidth:int) : BlockXmlInfo =
        let xmls =
            let spanX = (coilCellX - x - cmdWidth)

            [ let c = coord (x, y)
              let xml =
                let lengthParam = $"Param={dq}{3 * spanX}{dq}"
                elementFull (int ElementType.MultiHorzLineMode) c lengthParam ""

              { Coordinate = c
                Xml = xml
                SpanX = spanX
                SpanY = 1 }

              let c = coord (coilCellX, y)
              let xml = elementFull (int ElementType.FBMode) c cmdParam ""

              { Coordinate = c
                Xml = xml
                SpanX = cmdWidth
                SpanY = 1 } ]

        { X = x
          Y = y
          TotalSpanX = 31
          TotalSpanY = 1
          XmlElements = xmls }

    let bxiXgkFBCommand (prjParam: XgxProjectParams) (x, y) (fbc: FunctionBlock) : BlockXmlInfo =
        let cmdWidth = 3
        let cmdParam = 
            match fbc with
            | TimerMode ts ->
                let typ = ts.Timer.Type.ToString()
                let var = ts.Timer.Name
                let value =
                    let res = prjParam.GetXgkTimerResolution(ts.Timer.TimerStruct.XgkStructVariableDevicePos)
                    int <| (float ts.Timer.PRE.Value) / res
                $"Param={dq}{typ},{var},{value}{dq}"        // e.g : Param="TON,T0000,1000"
            | CounterMode cs ->
                let typ = cs.Counter.Type.ToString()
                let var = cs.Counter.Name
                let value = cs.Counter.PRE.Value 
                $"Param={dq}{typ},{var},{value}{dq}"        // e.g : Param="CTU,C0000,1000"
        bxiXgkFBCommandWithParam (x, y) (cmdParam, cmdWidth)


    /// 왼쪽에 FB (비교 연산 등) 를 그리고, 오른쪽에 coil 을 그린다.
    let xmlXgkFBLeft (x, y) (fbParam: string) (target: string) : XmlOutput =
        assert (x = 0)
        let inner =
            [ 
                let c = coord (x, y)
                elementFull (int ElementType.FBMode) c fbParam ""

                let c = coord (x + 3, y)
                let spanX = coilCellX - 1
                let lengthParam = $"Param={dq}{3 * spanX}{dq}"
                elementFull (int ElementType.MultiHorzLineMode) c lengthParam ""

                let c = coord (coilCellX, y)
                elementBody (int ElementType.CoilMode) c target
            ] |> joinLines
        wrapWithRung inner


    /// 왼쪽에 _ON 을 조건으로 우측에 FB (사칙 연산) 을 그린다.
    let xmlXgkFBRight (x, y) (fbParam: string) : XmlOutput =
        assert (x = 0)
        let inner =
            [ 
                let c = coord (x, y)
                elementFull (int ElementType.ContactMode) c "" "_ON"

                let c = coord (x + 1, y)
                let spanX = coilCellX - 4
                let lengthParam = $"Param={dq}{3 * spanX}{dq}"
                elementFull (int ElementType.MultiHorzLineMode) c lengthParam ""

                let c = coord (coilCellX, y)
                elementFull (int ElementType.FBMode) c fbParam ""
            ] |> joinLines
        wrapWithRung inner

    let rxiXgkFB (prjParam: XgxProjectParams) (x, y) (condition:IExpression) (fbParam: string, fbWidth:int) : RungXmlInfo =
        assert (x = 0)
        let conditionBlockXml = bxiFunctionInputLadderBlock prjParam (x, y) (condition.Flatten() :?> FlatExpression)
        let cbx = conditionBlockXml

        let c = coord (x + cbx.TotalSpanX, y)
        let spanX = coilCellX - 4
        let xml =
            [
                let lengthParam = $"Param={dq}{3 * spanX}{dq}"
                elementFull (int ElementType.MultiHorzLineMode) c lengthParam ""

                let c = coord(coilCellX - fbWidth - cbx.TotalSpanX, y)
                elementFull (int ElementType.FBMode) c fbParam ""
            ] |> joinLines

        (* 좌측 expression 이 multiline 인 경우, 우측 FB 의 Coordinate 값이 expression 의 coordinate 중간에 삽입되는 형태로 정렬되어야 한다.  *)
        let xmls = cbx.XmlElements @ [{ Coordinate = c; Xml = xml; SpanX = spanX; SpanY = 1}]

        {
            Coordinate = coord(0, y + cbx.TotalSpanY)
            Xml = mergeXmls xmls
            SpanX = fbWidth; SpanY = 1
        }

    /// function input 에 해당하는 expr 을 그리되, 맨 마지막을 multi horizontal line 연결 가능한 상태로 만든다.
    let bxiFunctionInputLadderBlock (prjParam: XgxProjectParams) (x, y) (expr: FlatExpression) : BlockXmlInfo =
        let blockXml = bxiLadderBlock prjParam (x, y) expr

        if isFunctionBlockConnectable expr then
            blockXml
        else
            let b = blockXml
            let x = b.X + b.TotalSpanX //+ 1

            let lineXml =
                let c = coord (x, b.Y)
                let xml = hlineStartMarkAt (x, b.Y)

                { Coordinate = c
                  Xml = xml
                  SpanX = 1
                  SpanY = 1 }

            { blockXml with
                TotalSpanX = b.TotalSpanX + 1
                XmlElements = b.XmlElements +++ lineXml }

    /// x y 위치에서 expression 표현하기 위한 정보 반환
    /// {| Xml=[|c, str|]; NextX=sx; NextY=maxY; VLineUpRightMaxY=maxY |}
    /// - Xml : 좌표 * 결과 xml 문자열
    let rec private bxiLadderBlock (prjParam: XgxProjectParams) (x, y) (expr: FlatExpression) : BlockXmlInfo =
        let c = coord (x, y)
        let isXgk = prjParam.TargetType = XGK

        match expr with
        | FlatTerminal(terminal, pulse, neg) ->
            let mode =
                match pulse, neg with
                | true, true -> ElementType.NPulseContactMode
                | true, false -> ElementType.PulseContactMode
                | false, true -> ElementType.ClosedContactMode
                | false, false -> ElementType.ContactMode
                |> int

            // XGK 에서는 직접변수를, XGI 에서는 변수명을 사용
            let terminalText =
                match terminal, prjParam.TargetType with
                | :? IStorage as storage, XGK ->
                    if storage.Name.Contains (xgkTimerCounterContactMarking) then
                        storage.Name.Replace (xgkTimerCounterContactMarking, "")
                     else
                        match storage.Address, storage.Name with
                        | "", StartsWith("_") -> storage.Name
                        | _ -> storage.Address
                | :? IStorage as storage, _ ->   storage.Name
                | :? LiteralHolder<bool> as onoff, _ -> if onoff.Value then "_ON" else "_OFF"
                | _ ->
                    terminal.ToText()

            let str = elementBody mode c terminalText

            let xml = { Coordinate = c; Xml = str; SpanX = 1; SpanY = 1 }

            {   XmlElements = [ xml ]
                X = x; Y = y
                TotalSpanX = 1; TotalSpanY = 1
            }

        | FlatNary(And, exprs) ->
            let mutable sx = x

            let blockedExprXmls: BlockXmlInfo list =
                [ for exp in exprs do
                      let sub = bxiLadderBlock prjParam (sx, y) exp
                      sx <- sx + sub.TotalSpanX
                      sub ]

            let spanX = blockedExprXmls.Sum(fun x -> x.TotalSpanX)
            let spanY = blockedExprXmls.Max(fun x -> x.TotalSpanY)
            let exprXmls = blockedExprXmls |> List.collect (fun x -> x.XmlElements)

            { XmlElements = exprXmls
              X = x
              Y = y
              TotalSpanX = spanX
              TotalSpanY = spanY }


        | FlatNary(Or, exprs) ->
            let mutable sy = y

            let blockedExprXmls: BlockXmlInfo list =
                [ for exp in exprs do
                      let sub = bxiLadderBlock prjParam (x, sy) exp
                      sy <- sy + sub.TotalSpanY
                      sub ]

            let spanX = blockedExprXmls.Max(fun x -> x.TotalSpanX)
            let spanY = blockedExprXmls.Sum(fun x -> x.TotalSpanY)
            let exprXmls = blockedExprXmls |> List.collect (fun x -> x.XmlElements)

            let xmls =
                [   yield! exprXmls

                    let auxLineXmls =
                        [ for ri in blockedExprXmls do
                              if ri.TotalSpanX < spanX then
                                  let span = (spanX - ri.TotalSpanX - 1)
                                  let param = $"Param={dq}{span * 3}{dq}"
                                  let mode = int ElementType.MultiHorzLineMode
                                  let c = coord (x + ri.TotalSpanX, ri.Y)
                                  let xml = elementFull mode c param ""

                                  { Coordinate = c; Xml = xml; SpanX = span; SpanY = 1 } ]

                    yield! auxLineXmls


                    // 좌측 vertical lines
                    if x >= 1 then
                        let dy =
                            blockedExprXmls
                            |> List.take(blockedExprXmls.Length - 1)
                            |> List.sumBy(fun e -> e.TotalSpanY)
                        yield! rxisVLineDownN (x - 1, y) dy

                    // ```OR variable length 역삼각형 test```
                    let lowestY =
                        blockedExprXmls.Where(fun sri -> sri.TotalSpanX <= spanX).Max(fun sri -> sri.Y)
                    // 우측 vertical lines
                    yield! rxisVLineDownN (x + spanX - 1, y) (lowestY - y) ]

            let xmls = xmls |> List.distinct // dirty hacking!

            { XmlElements = xmls
              X = x
              Y = y
              TotalSpanX = spanX
              TotalSpanY = spanY }

        | FlatNary(OpArithmatic _, _exprs) when isXgk ->
            failwithlog "ERROR : Should have been processed in early stage." // 사전에 미리 처리 되었어야 한다.  여기 들어오면 안된다. XgiStatement

        | FlatNary(OpCompare cmp, args) when isXgk ->
            let param =
                let op = operatorToXgkFunctionName cmp args[0].DataType |> escapeXml
                $"Param={dq}{op},{args[0]},{args[1]}{dq}"        // XGK 에서는 직접변수를 사용

            failwithlog "ERROR : Should have been processed in early stage." // 사전에 미리 처리 되었어야 한다.  여기 들어오면 안된다. XgiStatement
        | FlatNary((OpCompare fn | OpArithmatic fn), _exprs) ->
            failwithlog "ERROR : Should have been processed in early stage." // 사전에 미리 처리 되었어야 한다.  여기 들어오면 안된다. XgiStatement

        // terminal case
        | FlatNary(OpUnit, inner :: []) -> bxiLadderBlock prjParam (x, y) inner

        // negation 없애기
        | FlatNary(Neg, inner :: []) -> FlatNary(OpUnit, [ inner.Negate() ]) |> bxiLadderBlock prjParam (x, y)


        | FlatNary(risingOrFallingAfter, flatExpArg::[]) when risingOrFallingAfter = RisingAfter || risingOrFallingAfter = FallingAfter ->
            let blockXml = bxiLadderBlock prjParam (x, y) flatExpArg
            let mode =
                match risingOrFallingAfter with
                | RisingAfter -> ElementType.RisingContact
                | FallingAfter -> ElementType.FallingContact
                | _ -> failwith "ERROR: Unexpected."
            let xx, yy = x + blockXml.TotalSpanX, y
            let c = coord (xx, yy)
            let xml = elementFull mode c "" ""
            { blockXml with
                TotalSpanX = blockXml.TotalSpanX + 1
                X = x; Y = y;
                XmlElements = blockXml.XmlElements +++ { Coordinate = c; Xml = xml; SpanX = 1; SpanY = 1 } }

        | _ -> failwithlog "Unknown FlatExpression case"

    type FlatExpression with
        member exp.BxiLadderBlock (prjParam: XgxProjectParams, (x, y)) = bxiLadderBlock prjParam (x, y) exp

    /// Flat expression 을 논리 Cell 좌표계 x y 에서 시작하는 rung 를 작성한다.
    ///
    /// - xml 및 다음 y 좌표 반환
    ///
    /// - expr 이 None 이면 그리지 않는다.
    ///
    /// - cmdExp 이 None 이면 command 를 그리지 않는다.
    let rxiRung (prjParam: XgxProjectParams) (x, y) (expr: FlatExpression option) (cmdExp: CommandTypes option) : RungXmlInfo =
        let rxiRungImpl (x, y) (expr: FlatExpression option) (cmdExp: CommandTypes option) : RungXmlInfo =
            let exprSpanX, exprSpanY, exprXmls =
                match expr with
                | Some expr ->
                    let exprBlockXmlElement = bxiLadderBlock prjParam (x, y) expr
                    let ex = exprBlockXmlElement
                    ex.TotalSpanX, ex.TotalSpanY, ex.XmlElements |> List.distinct
                | _ -> 0, 0, []

            let cmdSpanX, cmdSpanY, cmdXmls =
                match cmdExp with
                | Some cmdExp ->
                    let nx = x + exprSpanX

                    let cmdXmls1 =
                        match cmdExp with
                        | CoilCmd _cc ->
                            let coilText = // XGK 에서는 직접변수를, XGI 에서는 변수명을 사용
                                match prjParam.TargetType, cmdExp.CoilTerminalTag with
                                | XGK, (:? IStorage as stg) when not <| (stg :? XgkTimerCounterStructResetCoil) -> stg.Address
                                | _ -> cmdExp.CoilTerminalTag.StorageName
                            bxiCoil (nx - 1, y) cmdExp coilText
                        | _ ->      // | PredicateCmd _pc | FunctionCmd _ | FunctionBlockCmd _ | ActionCmd _
                            bxiCommand prjParam (nx, y) cmdExp

                    let cmdXmls2 =
                        { cmdXmls1 with
                            XmlElements = cmdXmls1.XmlElements |> List.distinct } // dirty hack!

                    let spanX = exprSpanX + cmdXmls2.TotalSpanX
                    let spanY = max exprSpanY cmdXmls2.TotalSpanY
                    spanX, spanY, cmdXmls2
                | None ->
                    0, 0, { X = x; Y = y; TotalSpanX = 0; TotalSpanY = 0; XmlElements = [] }

            let xml = (exprXmls @ cmdXmls.XmlElements).MergeXmls()

            let spanX = exprSpanX + cmdSpanX
            let spanY = max exprSpanY cmdSpanY
            let c = coord (x, spanY + y)

            {   Xml = xml; Coordinate = c; SpanX = spanX; SpanY = spanY; }

        match prjParam.TargetType, cmdExp with
        | XGK, Some (ActionCmd(Move(condition, source, target))) when source.Terminal.IsSome ->
            let fbParam, fbWidth =
                let s, d = source.GetTerminalString(prjParam), target.Name
                let mov =
                    let st, tt = source.DataType, target.DataType
                    // move 의 type 이 동일해야 한다.  timer/counter 는 예외.  reset coil 이나 preset 설정 등 허용.
                    assert (st = tt || tt = typeof<TimerCounterBaseStruct>)
                    operatorToXgkFunctionName "MOV" st
                $"Param={dq}{mov},{s},{d}{dq}", 3           // Param="MOV,source,destination"
            rxiXgkFB prjParam (x, y) condition (fbParam, fbWidth)

        | _ ->
            match prjParam.TargetType, expr, cmdExp with
            | (XGI, _, _) | (_, Some _, _) | (_, _, None) ->        // prjParam.TargetType = XGI || expr.IsSome || cmdExp.IsNone
                rxiRungImpl (x, y) expr cmdExp
            | XGK, _, Some (FunctionBlockCmd(fbc)) ->
                match fbc with
                | CounterMode(counterStatement) when counterStatement.Counter.Type = CTUD ->
                    let counter = counterStatement.Counter
                    // CTUD, C, U, D, N
                    let up, down =      // reset 조건은 statement2statements 에서 counter 의 reset 조건을 따로 statement 로 추가하였으므로, 여기서는 무시한다.
                        match counterStatement.UpCondition, counterStatement.DownCondition with
                        | Some u, Some d -> u.GetTerminalString(prjParam), d.GetTerminalString(prjParam)
                        | _ -> failwithlog "ERROR"
                    let rungInCondition =
                        match expr with
                        | Some expr -> expr
                        | _ -> (Expression.True :> IExpression).Flatten() :?> FlatExpression
                    let pv = counter.PRE.Value

                    let mutable spanY = 1
                    let xml =
                        [
                            let { X = _xx; Y = yy; TotalSpanX = totalSpanX; TotalSpanY = totalSpanY; XmlElements = xmls } : BlockXmlInfo =
                                rungInCondition.BxiLadderBlock(prjParam, (x, y))
                            xmls[0].Xml

                            hlineTo (totalSpanX, yy) (coilCellX - 5)

                            if totalSpanY > 1 then
                                spanY <- totalSpanY

                            let param =
                                let counterVariable = counter.CounterStruct.XgkStructVariableName
                                $"Param={dq}CTUD,{counterVariable},{up},{down},{pv}{dq}"
                            xgkFBAt param (coilCellX - 5 - 1, yy)
                        ] |> joinLines

                    { Xml = xml; Coordinate = coord(0, y + spanY); SpanX = coilCellX; SpanY = spanY }

                | _ ->
                    let exp =
                        match fbc with
                        | CounterMode(counterStatement) ->
                            counterStatement.GetUpOrDownCondition().Flatten() :?> FlatExpression
                        | TimerMode(timerStatement) ->
                            timerStatement.RungInCondition.Value.Flatten() :?> FlatExpression
                    rxiRungImpl (x, y) (Some exp) cmdExp
            | _, _, Some _ ->
                    rxiRungImpl (x, y) expr cmdExp
            | _ ->
                failwithlog "ERROR"
