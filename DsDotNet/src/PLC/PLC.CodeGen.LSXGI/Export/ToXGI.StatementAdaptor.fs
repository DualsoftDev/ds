namespace PLC.CodeGen.LSXGI

open Engine.Core
open PLC.CodeGen.Common

(*
    - Timer 나 Counter 의 Rung In Condition 은 expression 을 받을 수 있으나,
      산전 XGI, AB 모두 부수적인 것들은 tag 만 받을 수 있다.
      e.g CTUD 의 경우, 기본 조건인 CU 는 expression 을 받지만,
          CD 는 조건식으로 주어진 경우, tag 에 저장한 후, 해당 tag 만 CD 에 지정할 수 있다.
    - XGI rung 생성시에 Engine.Core 에서 생성된 Statement 를 직접 사용할 수 없는 이유이다.
    - Statement 를 XgiStatement 로 변환한 후, 이를 XGI 생성 모듈에서 사용한다.
    - 변환 시, 추가적으로 생성되는 요소
       * 조건 식을 임시 변수로 저장하기 위한 추가 statement
         - 기존 조건식 갖는 statement 대신 임시 변수를 가지는 statement 로 변환
       * 생성된 임시 변수의 tag 등록

    - [Statement] -> [temporary tag], [XgiStatment] : # XgiStatment >= # Statement
*)



[<AutoOpen>]
module rec TypeConvertorModule =
    type IXgiStatement = interface end
    type TempTagCreator = string -> PlcTag<bool>  // name -> address -> PlcTag<bool>
    type CommentedXgiStatement = CommentedXgiStatement of comment:string * statement:XgiStatement
    let (|CommentAndXgiStatement|) = function | CommentedXgiStatement(x, y) -> x, y
    let commentAndXgiStatement = (|CommentAndXgiStatement|)

    [<AbstractClass>]
    type XgiStatementExptender() =
        interface IXgiStatement
        member val TemporaryTags = ResizeArray<PlcTag<bool>>()
        member val ExtendedStatements = ResizeArray<XgiStatement>()


    let private expandExpression (store:XgiStatementExptender) (exp:IExpression<bool> option) (nameHint:string) : Terminal<bool> option =
        let helper (tempTagCreator:TempTagCreator) =
            match exp with
            | Some (:? Expression<bool> as exp) ->
                match exp with
                | DuTerminal t -> Some t
                | DuFunction _ ->
                    let temp = tempTagCreator nameHint // "temp" "%MX0.0.1"
                    store.TemporaryTags.Add temp
                    let assign = DuXgiAssign <| XgiAssignStatement(exp, temp)
                    store.ExtendedStatements.Add assign
                    Some (DuTag temp)
            | _ ->
                None
        let tagCreator =
            let mutable n = 0
            fun nameHint ->
                n <- n + 1
                PlcTag<bool>($"temp{nameHint}{n}", "%MX0", false)
        helper tagCreator


    type XgiAssignStatement(exp:IExpression, target:IStorage) =
        interface IXgiStatement
        member _.Expression = exp
        member _.Target = target

    type XgiTimerStatement(ts:TimerStatement) as this =
        inherit XgiStatementExptender()
        //let reset = lazy ( expandExpression this ts.ResetCondition )
        let reset = expandExpression this ts.ResetCondition "RES"
        member _.Timer = ts.Timer
        member _.RungInCondition:IExpression<bool> = ts.RungInCondition.Value
        member _.Reset:Terminal<bool> option = reset

    type XgiCounterStatement(cs:CounterStatement) as this =
        inherit XgiStatementExptender()
        let typ = cs.Counter.Type
        let expand = expandExpression this
        let reset = expand cs.ResetCondition "RES"
        let cu, cd, ld =
            match typ with
            | ( CTU | CTD | CTR ) -> None, None, None
            | CTUD -> None, expand cs.DownCondition "CD", expand cs.LoadCondition "LD"

        member _.Counter = cs.Counter
        member _.RungInCondition:IExpression<bool> =
            match typ with
            | ( CTU | CTUD | CTR ) -> cs.UpCondition.Value
            | CTD -> cs.DownCondition.Value

        member _.Reset:Terminal<bool> option = reset
        member _.CountUp:Terminal<bool> option = cu
        member _.CountDown:Terminal<bool> option = cd
        member _.Load:Terminal<bool> option = ld

    type XgiCopyStatement(condition:IExpression<bool>, source:IExpression, target:IStorage) =
        interface IXgiStatement
        member _.Condition = condition
        member _.Source = source
        member _.Target = target

    type XgiStatement =
        | DuXgiAssign of XgiAssignStatement
        | DuXgiTimer of XgiTimerStatement
        | DuXgiCounter of XgiCounterStatement
        | DuXgiCopy of XgiCopyStatement
    with
        member x.GetStatement():IXgiStatement =
            match x with
            | DuXgiAssign s -> s
            | DuXgiTimer  s -> s
            | DuXgiCounter  s -> s
            | DuXgiCopy  s -> s


    let private statement2XgiStatement (statement:Statement) : XgiStatement =
        match statement with
        | DuAssign (exp, target) -> DuXgiAssign (XgiAssignStatement(exp, target))
        | DuTimer ts             -> DuXgiTimer  (XgiTimerStatement(ts))
        | DuCounter cs           -> DuXgiCounter(XgiCounterStatement(cs))
        | DuCopy (exp, src, tgt) -> DuXgiCopy   (XgiCopyStatement(exp, src, tgt))
        | DuVarDecl _            -> failwith "ERROR"

    let internal commentedStatement2CommentedXgiStatement (CommentedStatement(comment, statement)) : CommentedXgiStatement =
        let xgiStatement = statement2XgiStatement statement
        CommentedXgiStatement(comment, xgiStatement)



    (* Moved from Command.fs *)

    ///FunctionBlocks은 Timer와 같은 현재 측정 시간을 저장하는 Instance가 필요있는 Command 해당
    type FunctionBlock =
        | TimerMode of XgiTimerStatement //endTag, time
        | CounterMode of XgiCounterStatement   // IExpressionTerminal *  CommandTag  * int  //endTag, countResetTag, count
    with
        member x.GetInstanceText() =
            match x with
            | TimerMode timerStatement -> timerStatement.Timer.Name
            | CounterMode counterStatement ->  counterStatement.Counter.Name
        member x.UsedCommandTags() : IExpressionTerminal list =
            failwith "Need check"


            //match x with
            ////| TimerMode(tag, time) -> [ tag ]
            //| TimerMode timerStatement ->
            //    timerStatement.RungInCondition
            //    |> Option.toList
            //    |> List.cast<IExpressionTerminal>
            //| CounterMode counterStatement ->
            //    [ counterStatement.UpCondition; counterStatement.ResetCondition ]
            //    |> List.choose id
            //    |> List.map (fun x -> x :?> IExpressionTerminal)

        interface IFunctionCommand with
            member this.TerminalEndTag: IExpressionTerminal =
                match this with
                //| TimerMode(tag, time) -> tag
                | TimerMode timerStatement -> timerStatement.Timer.DN
                | CounterMode counterStatement -> counterStatement.Counter.DN


    /// 실행을 가지는 type
    type CommandTypes =
        | CoilCmd          of CoilOutputMode
        | FunctionCmd      of FunctionPure
        /// Timer, Counter 등
        | FunctionBlockCmd of FunctionBlock

    let createPLCCommandCopy(endTag, from, toTag) = FunctionPure.CopyMode(endTag, (from, toTag))
    let createPLCCommandCompare(endTag, op, left, right) =
        match op with
        | GT ->FunctionPure.CompareGT(endTag, (left, right))
        | GE ->FunctionPure.CompareGE(endTag, (left, right))
        | EQ ->FunctionPure.CompareEQ(endTag, (left, right))
        | LE ->FunctionPure.CompareLE(endTag, (left, right))
        | LT ->FunctionPure.CompareLT(endTag, (left, right))
        | NE ->FunctionPure.CompareNE(endTag, (left, right))

    let createPLCCommandAdd(endTag, tag, value)          = FunctionPure.Add(endTag, tag, value)
    //let createPLCCommandTimer(endTag, time)              = FunctionBlock.TimerMode(endTag, time)
    //let createPLCCommandCounter(endTag, resetTag, count) = FunctionBlock.CounterMode(endTag, resetTag , count)