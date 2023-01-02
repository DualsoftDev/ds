namespace PLC.CodeGen.LSXGI

open Engine.Core

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
    type TempTagCreator = unit -> PlcTag<bool>  // name -> address -> PlcTag<bool>
    [<AbstractClass>]
    type XgiStatementExptender() =
        interface IXgiStatement
        member val TemporaryTags = ResizeArray<PlcTag<bool>>()
        member val ExtendedStatements = ResizeArray<IXgiStatement>()


    let private expandExpression (store:XgiStatementExptender) (exp:IExpression<bool> option) : Terminal<bool> option =
        let helper (tempTagCreator:TempTagCreator) =
            match exp with
            | Some (:? Expression<bool> as exp) ->
                match exp with
                | DuTerminal t -> Some t
                | DuFunction _ ->
                    let temp = tempTagCreator() // "temp" "%MX0.0.1"
                    store.TemporaryTags.Add temp
                    let assign = XgiAssignStatement(exp, temp)
                    store.ExtendedStatements.Add assign
                    Some (DuTag temp)
            | _ ->
                None
        let tagCreator() = PlcTag<bool>("temp", "%MX0.0.0", false)
        helper tagCreator


    type XgiAssignStatement(exp:IExpression, target:IStorage) =
        interface IXgiStatement
        member _.Expression = exp
        member _.Target = target

    type XgiTimerStatement(ts:TimerStatement) as this =
        inherit XgiStatementExptender()
        //let reset = lazy ( expandExpression this ts.ResetCondition )
        let reset = expandExpression this ts.ResetCondition
        member _.Timer = ts.Timer
        member _.RungInCondition:IExpression<bool> = ts.RungInCondition.Value
        member _.Reset:Terminal<bool> option = reset

    type XgiCounterStatement(cs:CounterStatement) as this =
        inherit XgiStatementExptender()
        let typ = cs.Counter.Type
        let expand = expandExpression this
        let failCall() = failwith "INTERNAL ERROR"      // RungInCondition 을 써야 하는 곳에 CU 를 썼다거나... CTU 에서 CD 를 사용하려한다거나..
        let reset = expand cs.ResetCondition
        let cu, cd =
            match typ with
            | CTU -> failCall(), failCall()
            | CTR -> failCall(), failCall()
            | (CTUD | CTD) -> failCall(), expand cs.DownCondition

        member _.RungInCondition:IExpression<bool> =
            match typ with
            | CTU | CTUD | CTR -> cs.UpCondition.Value
            | CTD -> cs.DownCondition.Value

        member _.Reset:Terminal<bool> option = reset
        member _.CountUp:Terminal<bool> option = cu
        member _.CountDown:Terminal<bool> option = cd

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


    let private statement2XgiStatement (statement:Statement) : XgiStatement =
        match statement with
        | DuAssign (exp, target) -> DuXgiAssign (XgiAssignStatement(exp, target))
        | DuTimer ts             -> DuXgiTimer  (XgiTimerStatement(ts))
        | DuCounter cs           -> DuXgiCounter(XgiCounterStatement(cs))
        | DuCopy (exp, src, tgt) -> DuXgiCopy   (XgiCopyStatement(exp, src, tgt))
        | DuVarDecl _            -> failwith "ERROR"

