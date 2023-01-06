namespace PLC.CodeGen.LSXGI

open Engine.Core
open Engine.Common.FS
open PLC.CodeGen.Common
open System.Security
open System.Diagnostics

(*
    - 사칙연산 함수(Add, ..) 의 입력은 XGI 에서 전원선으로부터 연결이 불가능한 반면,
      ds 문법으로 작성시에는 expression 을 이용하기 때문에 직접 호환이 불가능하다.
      * add(<expr1>, <expr2>) 를
            tmp1 := <expr1> 및 tmp2 := <expr2> 와 같이
            . 임시 변수를 생성해서 assign rung 을 만들고
            . 임시 변수를 add 함수의 입력 argument 에 tag 로 작성하도록 한다.

    - 임의의 rung 에 사칙연산 함수가 포함되면, 해당 부분만 잘라서 임시 변수에 저장하고 그 값을 이용해야 한다.
      * e.g $result := ($t1 + $t2) > 3
            . $tmp = $t1 + $t2
            . $result := $tmp > 3

    - Timer 나 Counter 의 Rung In Condition 은 복수개이더라도 전원선 연결이 가능하다.
      * 임시 변수 없이 expression 을 그대로 전원선 연결해서 그리면 된다.

    - XGI 임시 변수는 XgiLocalVar<'T> type 으로 생성된다.

    - XGI rung 생성시에 Engine.Core 에서 생성된 Statement 를 직접 사용할 수 없는 이유이다.
    - Statement 를 XgiStatement 로 변환한 후, 이를 XGI 생성 모듈에서 사용한다.
    - 변환 시, 추가적으로 생성되는 요소
       * 조건 식을 임시 변수로 저장하기 위한 추가 statement
         - 기존 조건식 갖는 statement 대신 임시 변수를 가지는 statement 로 변환
       * 생성된 임시 변수의 tag 등록

    - [Statement] -> [temporary tag], [XgiStatment] : # XgiStatment >= # Statement
*)


[<AutoOpen>]
module ConvertorPrologModule =
    let systemTypeNameToXgiTypeName = function
        | "Boolean" -> "BOOL"
        | "Single" -> "REAL"
        | "Double" -> "LREAL"
        | ("SByte" | "Char")   -> "BYTE"
        | "Byte"   -> "BYTE"
        | "Int16"  -> "SINT"
        | "UInt16" -> "USINT"
        | "Int32"  -> "DINT"
        | "UInt32" -> "UDINT"
        | "Int64"  -> "LINT"
        | "UInt64" -> "ULINT"
        | "String" -> "STRING"  // 32 byte
        | _ -> failwith "ERROR"



    let mutable internal autoVariableCounter = 0

    type IXgiLocalVar =
        inherit IVariable
        abstract SymbolInfo:SymbolInfo
    type IXgiLocalVar<'T> =
        inherit IXgiLocalVar
        inherit IVariable<'T>

    type XgiLocalVar<'T when 'T:equality>(name, comment, initValue:'T) =
        inherit VariableBase<'T>(name, initValue)

        interface IXgiLocalVar<'T> with
            member x.SymbolInfo = x.SymbolInfo
        interface INamedExpressionizableTerminal with
            member x.StorageName = name
        interface IText with
            member x.ToText() = name
        member x.SymbolInfo =
            let kindVar = int Variable.Kind.VAR
            let plcType = systemTypeNameToXgiTypeName typedefof<'T>.Name
            let comment = SecurityElement.Escape comment
            fwdCreateSymbol name comment "" kindVar "" plcType

        override x.ToBoxedExpression() = var2expr x


[<AutoOpen>]
module rec TypeConvertorModule =
    type IXgiStatement = interface end
    type CommentedXgiStatement = CommentedXgiStatement of comment:string * statement:XgiStatement
    let (|CommentAndXgiStatement|) = function | CommentedXgiStatement(x, y) -> x, y
    let commentAndXgiStatement = (|CommentAndXgiStatement|)

    [<AbstractClass>]
    type XgiStatementExptender() =
        interface IXgiStatement
        member val TemporaryTags = ResizeArray<IXgiLocalVar>()
        member val ExtendedStatements = ResizeArray<XgiStatement>()


    let tagCreator (nameHint:string) comment (initValue:'Q) =
        autoVariableCounter <- autoVariableCounter + 1
        XgiLocalVar($"_tmp{nameHint}{autoVariableCounter}", comment, initValue)


    let private expandExpression (store:XgiStatementExptender) (exp:IExpression<'T>) (nameHint:string) : Terminal<'T> =
        match exp with
        | :? Expression<'T> as exp ->
            match exp with
            | DuTerminal t -> t
            | DuFunction fs ->
                let comment = exp.ToText(false) //fs.Name
                let temp = tagCreator nameHint comment (exp.Evaluate())
                store.TemporaryTags.Add temp
                let assign = DuXgiAssign <| XgiAssignStatement(exp, temp)
                store.ExtendedStatements.Add(assign)
                DuVariable temp
        | _ ->
            failwith "ERROR"



    type XgiAssignStatement(exp:IExpression, target:IStorage) =
        interface IXgiStatement
        member _.Expression = exp
        member _.Target = target

    type XgiTimerStatement(ts:TimerStatement) as this =
        inherit XgiStatementExptender()
        let reset = ts.ResetCondition |> map (fun res -> expandExpression this res "RES")
        member _.Timer = ts.Timer
        member _.RungInCondition:IExpression<bool> = ts.RungInCondition.Value
        member _.Reset:Terminal<bool> option = reset

    type XgiCounterStatement(cs:CounterStatement) as this =
        inherit XgiStatementExptender()
        let typ = cs.Counter.Type
        let expand = expandExpression this
        let reset = cs.ResetCondition |> map (fun res -> expand res "RES")
        let cu, cd, ld =
            match typ with
            | ( CTU | CTD | CTR ) -> None, None, None
            | CTUD ->
                None,
                cs.DownCondition |> map (fun cd -> expand cd "CD"),
                cs.LoadCondition |> map (fun ld -> expand ld "LD")

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
        | DuXgiAssign  of XgiAssignStatement
        | DuXgiTimer   of XgiTimerStatement
        | DuXgiCounter of XgiCounterStatement
        | DuXgiCopy    of XgiCopyStatement
    with
        member x.GetStatement():IXgiStatement =
            match x with
            | DuXgiAssign  s -> s
            | DuXgiTimer   s -> s
            | DuXgiCounter s -> s
            | DuXgiCopy    s -> s



    [<DebuggerDisplay("{ToText()}")>]
    type XgiConvertorExpression =
        | FunctionInstance of op:string * args:XgiConvertorExpression list * outSymbol:SymbolInfo
        | Terminal of IExpression
        member x.ToText() =
            match x with
            | FunctionInstance (op, args, outSymbol) ->
                let args = args |> map toText |> String.concat ", "
                $"{op}({args})"
            | Terminal t -> t.ToText(false)
    // todo
    let collectExpandedExpression (exp:IExpression) : XgiConvertorExpression list =
        [
            match exp.FunctionName with
            | Some ("+"|"-"|"*"|"/"|">"|">="|"<"|"<="|"="|"!=" as op) ->
                let newArgs = exp.FunctionArguments |> bind collectExpandedExpression
                let out = fwdCreateSymbol "xxx" "xxx" "" 1 "" "BOOL"        // todo
                FunctionInstance (op, newArgs, out)
            | _ ->
                Terminal exp
        ]

    let private statement2XgiStatement (statement:Statement) : XgiStatement =
        match statement with
        | DuAssign (exp, target) ->
            let xxx = collectExpandedExpression exp;
            DuXgiAssign (XgiAssignStatement(exp, target))
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
        | CounterMode of XgiCounterStatement   // IExpressionizableTerminal *  CommandTag  * int  //endTag, countResetTag, count
    with
        member x.GetInstanceText() =
            match x with
            | TimerMode timerStatement -> timerStatement.Timer.Name
            | CounterMode counterStatement ->  counterStatement.Counter.Name
        member x.UsedCommandTags() : INamedExpressionizableTerminal list =
            failwith "Need check"


            //match x with
            ////| TimerMode(tag, time) -> [ tag ]
            //| TimerMode timerStatement ->
            //    timerStatement.RungInCondition
            //    |> Option.toList
            //    |> List.cast<IExpressionizableTerminal>
            //| CounterMode counterStatement ->
            //    [ counterStatement.UpCondition; counterStatement.ResetCondition ]
            //    |> List.choose id
            //    |> List.map (fun x -> x :?> IExpressionizableTerminal)

        interface IFunctionCommand with
            member this.TerminalEndTag: INamedExpressionizableTerminal =
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