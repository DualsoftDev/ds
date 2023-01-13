namespace PLC.CodeGen.LSXGI

open System.Linq
open System.Security

open Engine.Core
open Engine.Common.FS
open PLC.CodeGen.Common
open System
open Engine.Common.FS

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
    let systemTypeToXgiTypeName (typ:System.Type) =
        match typ.Name with
        | "Boolean" -> "BOOL"
        | "Byte"    -> "BYTE"
        | "Double"  -> "LREAL"
        | "Int16"   -> "SINT"
        | "Int32"   -> "DINT"
        | "Int64"   -> "LINT"
        | "Single"  -> "REAL"
        | "String"  -> "STRING"  // 32 byte
        | "UInt16"  -> "USINT"
        | "UInt32"  -> "UDINT"
        | "UInt64"  -> "ULINT"
        | ("SByte" | "Char")   -> "BYTE"
        | _ -> failwith "ERROR"



    let mutable internal autoVariableCounter = 0

    type IXgiLocalVar =
        inherit IVariable
        inherit INamedExpressionizableTerminal
        abstract SymbolInfo:SymbolInfo
    type IXgiLocalVar<'T> =
        inherit IXgiLocalVar
        inherit IVariable<'T>

    type XgiLocalVar<'T when 'T:equality>(name, comment, initValue:'T) =
        inherit VariableBase<'T>(name, initValue)
        let symbolInfo =
            let plcType = systemTypeToXgiTypeName typedefof<'T>
            let comment = SecurityElement.Escape comment
            let initValueHolder:BoxedObjectHolder = {Object=initValue}
            fwdCreateSymbolInfo name comment plcType initValueHolder

        interface IXgiLocalVar with
            member x.SymbolInfo = x.SymbolInfo
        interface INamedExpressionizableTerminal with
            member x.StorageName = name
        interface IText with
            member x.ToText() = name
        member x.SymbolInfo = symbolInfo

        override x.ToBoxedExpression() = var2expr x

    let getType (x:obj) : System.Type =
        match x with
        | :? IExpression as exp -> exp.DataType
        | :? IStorage as stg -> stg.DataType
        | :? IValue as value -> value.ObjValue.GetType()
        | _ -> failwith "ERROR"


[<AutoOpen>]
module rec TypeConvertorModule =
    type IXgiStatement = interface end
    type CommentedXgiStatements = CommentedXgiStatements of comment:string * statements:Statement list
    let (|CommentAndXgiStatements|) = function | CommentedXgiStatements(x, ys) -> x, ys
    let commentAndXgiStatement = (|CommentAndXgiStatements|)

    let createXgiVariable (typ:System.Type) (name:string) (initValue:obj) comment : IXgiLocalVar =
        (*
            "n0" is an incorrect variable.
            The folling characters are allowed:
            Only alphabet capital/small letters and '_' are allowed in the first letter.
            Only alphabet capital/small letters and '_' are allowed in the second letter.
            (e.g. variable1, _variable2, variableAB_3, SYMBOL, ...)
        *)
        match name |> Seq.toList with
        | ch::_ when isHangul ch -> ()
        | ch1::ch2::_ when isValidStart ch1 && isValidStart ch2 -> ()
        | _ -> failwith $"Invalid XGI variable name {name}.  Use longer name"

        match name with
        | RegexPattern "ld(\d)+" _ -> failwith $"Invalid XGI variable name {name}."
        | _ -> ()

        match typ.Name with
        | "Boolean"-> XgiLocalVar<bool>  (name, comment, unbox initValue)
        | "Byte"   -> XgiLocalVar<uint8> (name, comment, unbox initValue)
        | "Char"   -> XgiLocalVar<char>  (name, comment, unbox initValue)
        | "Double" -> XgiLocalVar<double>(name, comment, unbox initValue)
        | "Int16"  -> XgiLocalVar<int16> (name, comment, unbox initValue)
        | "Int32"  -> XgiLocalVar<int32> (name, comment, unbox initValue)
        | "Int64"  -> XgiLocalVar<int64> (name, comment, unbox initValue)
        | "SByte"  -> XgiLocalVar<int8>  (name, comment, unbox initValue)
        | "Single" -> XgiLocalVar<single>(name, comment, unbox initValue)
        | "String" -> XgiLocalVar<string>(name, comment, unbox initValue)
        | "UInt16" -> XgiLocalVar<uint16>(name, comment, unbox initValue)
        | "UInt32" -> XgiLocalVar<uint32>(name, comment, unbox initValue)
        | "UInt64" -> XgiLocalVar<uint64>(name, comment, unbox initValue)
        | _  -> failwith "ERROR"

    let createTypedXgiAutoVariable (typ:System.Type) (nameHint:string) (initValue:obj) comment : IXgiLocalVar =
        autoVariableCounter <- autoVariableCounter + 1
        let name = $"_tmp{nameHint}{autoVariableCounter}"
        let typ = initValue.GetType()
        createXgiVariable typ name initValue comment


    let internal createXgiAutoVariableT (nameHint:string) comment (initValue:'T) =
        autoVariableCounter <- autoVariableCounter + 1
        XgiLocalVar($"_tmp{nameHint}{autoVariableCounter}", comment, initValue)


    (* Moved from Command.fs *)

    /// FunctionBlocks은 Timer와 같은 현재 측정 시간을 저장하는 Instance가 필요있는 Command 해당
    type FunctionBlock =
        | TimerMode of TimerStatement //endTag, time
        | CounterMode of CounterStatement   // IExpressionizableTerminal *  CommandTag  * int  //endTag, countResetTag, count
    with
        member x.GetInstanceText() =
            match x with
            | TimerMode timerStatement -> timerStatement.Timer.Name
            | CounterMode counterStatement ->  counterStatement.Counter.Name

        interface IFunctionCommand with
            member this.TerminalEndTag: INamedExpressionizableTerminal =
                match this with
                | TimerMode timerStatement -> timerStatement.Timer.DN
                | CounterMode counterStatement -> counterStatement.Counter.DN


    /// 실행을 가지는 type
    type CommandTypes =
        | CoilCmd          of CoilOutputMode
        /// Predicate.  (boolean function)
        | PredicateCmd     of Predicate
        /// Non-boolean function
        | FunctionCmd      of Function
        | ActionCmd        of PLCAction
        /// Timer, Counter 등
        | FunctionBlockCmd of FunctionBlock

    //let createPLCCommandCopy(endTag, from, toTag) = FunctionPure.CopyMode(endTag, (from, toTag))

[<AutoOpen>]
module XgiExpressionConvertorModule =
    type XgiStorage = ResizeArray<IStorage>

    let operatorToXgiFunctionName = function
        | ">"  -> "GT"
        | ">=" -> "GTE"
        | "<"  -> "LT"
        | "<=" -> "LTE"
        | "="  -> "EQ"
        | "!=" -> "NE"
        | "+"  -> "ADD"
        | "-"  -> "SUB"
        | "*"  -> "MUL"
        | "/"  -> "DIV"
        |  _ -> failwith "ERROR"

    let collectExpandedExpression
        (storage:XgiStorage)
        (expandFunctionStatements:ResizeArray<Statement>)
        (exp:IExpression)
        : IExpression
      =
        let xgiLocalVars = ResizeArray<IXgiLocalVar>()
        let rec helper (exp:IExpression) = [
            match exp.FunctionName with
            | Some funcName ->
                let newArgs = exp.FunctionArguments |> bind helper
                match funcName with
                | ("&&" | "||" | "!") as op ->
                    exp.WithNewFunctionArguments newArgs
                | (">"|">="|"<"|"<="|"="|"!="  |  "+"|"-"|"*"|"/") as op ->
                    let withDefaultValueT (default_value:'T) =
                        let out = createXgiAutoVariableT "out" $"{op} output" default_value
                        xgiLocalVars.Add out
                        expandFunctionStatements.Add <| DuAugmentedPLCFunction { FunctionName = op; Arguments = newArgs; Output = out; }
                        DuTerminal (DuVariable out) :> IExpression

                    match exp.DataType.Name with
                    | "Boolean"-> withDefaultValueT false
                    | "Byte"   -> withDefaultValueT 0uy
                    | "Char"   -> withDefaultValueT ' '
                    | "Double" -> withDefaultValueT 0.0
                    | "Int16"  -> withDefaultValueT 0s
                    | "Int32"  -> withDefaultValueT 0
                    | "Int64"  -> withDefaultValueT 0L
                    | "SByte"  -> withDefaultValueT 0y
                    | "Single" -> withDefaultValueT 0.f
                    | "String" -> withDefaultValueT ""
                    | "UInt16" -> withDefaultValueT 0us
                    | "UInt32" -> withDefaultValueT 0u
                    | "UInt64" -> withDefaultValueT 0UL
                    | _ -> failwith "ERROR"

                | (FunctionNameRising | FunctionNameFalling) ->
                    exp
                | _ ->
                    failwith "ERROR"
            | _ ->
                exp
        ]

        let newExp = helper exp |> List.exactlyOne
        xgiLocalVars.Cast<IStorage>() |> storage.AddRange   // 위의 helper 수행 이후가 아니면, xgiLocalVars 가 채워지지 않는다.
        newExp

    (* see ``ADD 3 items test`` *)
    /// 사칙 연산 처리
    /// - a + b + c => + [a; b; c] 로 변환
    /// - a + (b * c) + d => +[a; x; d], *[b; c] 두개의 expression 으로 변환.  부가적으로 생성된 *[b;c] 는 새로운 statement 를 생성해서 augmentedStatementsStorage 에 추가된다.
    let private mergeArithmaticOperator
        (storage:XgiStorage)
        (augmentedStatementsStorage:ResizeArray<Statement>)
        (exp:IExpression)
        : IExpression
      =
        let rec helper (currentOp:string) (exp:IExpression) : IExpression list =
            match exp.FunctionName with
            | Some ("+"|"-"|"*"|"/" as op) ->
                if op = currentOp then
                    let args = [
                        for arg in exp.FunctionArguments do
                            match arg.Terminal, arg.FunctionName with
                            | Some _, _ -> yield arg
                            | None, Some fn ->
                                yield! helper op arg
                            | _ -> failwith "ERROR"
                    ]
                    args
                else
                    let go (v:'Q) =
                        let out = createXgiAutoVariableT "_temp_internal_" $"{op} output" v
                        storage.Add out
                        let args = exp.FunctionArguments |> List.bind (helper op)
                        DuAugmentedPLCFunction {FunctionName=op; Arguments=args; Output=out } |> augmentedStatementsStorage.Add
                        [ var2expr out :> IExpression ]

                    let v = exp.BoxedEvaluatedValue
                    match exp.DataType.Name with
                    | "Boolean"-> go (v :?> bool)
                    | "Byte"   -> go (v :?> uint8)
                    | "Char"   -> go (v :?> char)
                    | "Double" -> go (v :?> double)
                    | "Int16"  -> go (v :?> int16)
                    | "Int32"  -> go (v :?> int32)
                    | "Int64"  -> go (v :?> int64)
                    | "SByte"  -> go (v :?> int8)
                    | "Single" -> go (v :?> float32)
                    | "String" -> go (v :?> string)
                    | "UInt16" -> go (v :?> uint16)
                    | "UInt32" -> go (v :?> uint32)
                    | "UInt64" -> go (v :?> uint64)
                    | _ -> failwith "ERROR"
            | _ ->
                [ exp ]

        let topOperator = exp.FunctionName.Value
        let newArgs = helper topOperator exp
        exp.WithNewFunctionArguments newArgs

    let private statement2XgiStatements (storage:XgiStorage) (statement:Statement) : Statement list =
        let expandFunctionStatements = ResizeArray<Statement>()  // DuAugmentedPLCFunction case

        let newStatements =
            match statement with
            | DuAssign (exp, target) ->
                // todo : "sum := tag1 + tag2" 의 처리 : DuAugmentedPLCFunction 하나로 만들고, 'OUT' output 에 sum 을 할당하여야 한다.
                match exp.FunctionName with
                | Some ("+"|"-"|"*"|"/" as op) ->

                    let augmentedStatementsStorage = ResizeArray<Statement>()
                    let exp = mergeArithmaticOperator storage augmentedStatementsStorage exp

                    let tgt = target :?> INamedExpressionizableTerminal
                    augmentedStatementsStorage @ [ DuAugmentedPLCFunction {FunctionName=op; Arguments=exp.FunctionArguments; Output=tgt } ]
                | _ ->
                    let newExp = collectExpandedExpression storage expandFunctionStatements exp
                    [ DuAssign (newExp, target) ]

            | DuVarDecl (exp, decl) ->
                let newExp_ = collectExpandedExpression storage expandFunctionStatements exp

                (* 일반 변수 선언 부분을 xgi local variable 로 치환한다. *)
                storage.Remove decl |> ignore

                match decl with
                | :? IXgiLocalVar as loc ->
                    let si = loc.SymbolInfo
                    let comment = $"[local var in code] {si.Comment}"
                    let initValue = exp.BoxedEvaluatedValue

                    let typ = initValue.GetType()
                    let var = createXgiVariable typ decl.Name initValue comment
                    storage.Add var

                | _ ->
                    failwith "ERROR"

                []
            | ( DuTimer _ | DuCounter _ ) ->
                [statement]

            | DuAction (DuCopy(condition, source, target)) ->
                let funcName = XgiConstants.FunctionNameMove
                let output = target:?>INamedExpressionizableTerminal
                [ DuAugmentedPLCFunction {FunctionName=funcName; Arguments=[condition; source]; Output=output } ]

            | DuAugmentedPLCFunction _ ->
                failwith "ERROR"

        expandFunctionStatements @ newStatements |> List.ofSeq

    let internal commentedStatement2CommentedXgiStatements (storage:XgiStorage) (CommentedStatement(comment, statement)) : CommentedXgiStatements =
        let xgiStatements = statement2XgiStatements storage statement
        let rungComment =
            let statementComment = statement.ToText()
            match comment.NonNullAny(), xgiGenerationOptions.IsAppendExpressionTextToRungComment with
            | true, true -> $"{comment}\r\n{statementComment}"
            | true, false -> comment
            | false, true -> statementComment
            | false, false -> ""
            |> escapeXml

        CommentedXgiStatements(rungComment, xgiStatements)
