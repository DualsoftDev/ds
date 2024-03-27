namespace PLC.CodeGen.LSXGI

open System.Linq
open System.Security

open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.Common

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
    let systemTypeToXgiTypeName (typ: System.Type) =
        match typ.Name with
        | BOOL -> "BOOL"
        | UINT8 -> "BYTE"
        | FLOAT64 -> "LREAL"
        | INT16 -> "SINT"
        | INT32 -> "DINT"
        | INT64 -> "LINT"
        | FLOAT32 -> "REAL"
        | STRING -> "STRING" // 32 byte
        | UINT16 -> "UINT"
        | UINT32 -> "UDINT"
        | UINT64 -> "ULINT"
        | (INT8 | CHAR) -> "BYTE"
        | _ -> failwithlog "ERROR"



    let mutable internal autoVariableCounter = 0

    type IXgiVar =
        inherit IVariable
        inherit INamedExpressionizableTerminal
        abstract SymbolInfo: SymbolInfo

    type IXgiVar<'T> =
        inherit IXgiVar
        inherit IVariable<'T>

    /// XGI 에서 사용하는 tag 주소를 갖지 않는 variable
    type XgiVar<'T when 'T: equality>(param: StorageCreationParams<'T>) =
        inherit VariableBase<'T>(param)

        let { Name = name
              Value = initValue
              Comment = comment } =
            param

        let symbolInfo =
            let plcType = systemTypeToXgiTypeName typedefof<'T>
            let comment = comment |> map (fun cmt -> SecurityElement.Escape cmt) |? ""
            let initValueHolder: BoxedObjectHolder = { Object = initValue }
            let kind = int Variable.Kind.VAR
            fwdCreateSymbolInfo name comment plcType kind initValueHolder

        interface IXgiVar with
            member x.SymbolInfo = x.SymbolInfo

        interface INamedExpressionizableTerminal with
            member x.StorageName = name

        interface IText with
            member x.ToText() = name

        member x.SymbolInfo = symbolInfo

        override x.ToBoxedExpression() = var2expr x

    let getType (x: obj) : System.Type =
        match x with
        | :? IExpression as exp -> exp.DataType
        | :? IStorage as stg -> stg.DataType
        | :? IValue as value -> value.ObjValue.GetType()
        | _ -> failwithlog "ERROR"


[<AutoOpen>]
module rec TypeConvertorModule =
    type IXgiStatement =
        interface
        end

    type CommentedXgiStatements = CommentedXgiStatements of comment: string * statements: Statement list

    let (|CommentAndXgiStatements|) =
        function
        | CommentedXgiStatements(x, ys) -> x, ys

    let commentAndXgiStatement = (|CommentAndXgiStatements|)

    let createXgiVariable (name: string) (initValue: obj) comment : IXgiVar =
        (*
            "n0" is an incorrect variable.
            The folling characters are allowed:
            Only alphabet capital/small letters and '_' are allowed in the first letter.
            Only alphabet capital/small letters and '_' are allowed in the second letter.
            (e.g. variable1, _variable2, variableAB_3, SYMBOL, ...)
        *)
        match name |> Seq.toList with
        | ch :: _ when isHangul ch -> ()
        | ch1 :: ch2 :: _ when isValidStart ch1 && isValidStart ch2 -> ()
        | _ -> failwith $"Invalid XGI variable name {name}.  Use longer name"

        match name with
        | RegexPattern @"ld(\d)+" _ -> failwith $"Invalid XGI variable name {name}."
        | _ -> ()

        let createParam () =
            { defaultStorageCreationParams (unbox initValue) (VariableTag.PlcUserVariable|>int) with
                Name = name
                Comment = Some comment }

        let typ = initValue.GetType()

        match typ.Name with
        | BOOL -> XgiVar<bool>(createParam ())
        | CHAR -> XgiVar<char>(createParam ())
        | FLOAT32 -> XgiVar<single>(createParam ())
        | FLOAT64 -> XgiVar<double>(createParam ())
        | INT16 -> XgiVar<int16>(createParam ())
        | INT32 -> XgiVar<int32>(createParam ())
        | INT64 -> XgiVar<int64>(createParam ())
        | INT8 -> XgiVar<int8>(createParam ())
        | STRING -> XgiVar<string>(createParam ())
        | UINT16 -> XgiVar<uint16>(createParam ())
        | UINT32 -> XgiVar<uint32>(createParam ())
        | UINT64 -> XgiVar<uint64>(createParam ())
        | UINT8 -> XgiVar<uint8>(createParam ())
        | _ -> failwithlog "ERROR"

    let sys = DsSystem("")

    let createTypedXgiAutoVariable (nameHint: string) (initValue: obj) comment : IXgiVar =
        autoVariableCounter <- autoVariableCounter + 1
        let name = $"_tmp{nameHint}{autoVariableCounter}"
        createXgiVariable name initValue comment


    let internal createXgiAutoVariableT (nameHint: string) comment (initValue: 'T) =
        autoVariableCounter <- autoVariableCounter + 1
        let name = $"_tmp{nameHint}{autoVariableCounter}"

        let param =
            { defaultStorageCreationParams (initValue) (VariableTag.PlcUserVariable|>int) with
                Name = name
                Comment = Some comment }

        XgiVar(param)


    (* Moved from Command.fs *)

    /// FunctionBlocks은 Timer와 같은 현재 측정 시간을 저장하는 Instance가 필요있는 Command 해당
    type FunctionBlock =
        | TimerMode of TimerStatement //endTag, time
        | CounterMode of CounterStatement // IExpressionizableTerminal *  CommandTag  * int  //endTag, countResetTag, count

        member x.GetInstanceText() =
            match x with
            | TimerMode timerStatement -> timerStatement.Timer.Name
            | CounterMode counterStatement -> counterStatement.Counter.Name

        interface IFunctionCommand with
            member this.TerminalEndTag: INamedExpressionizableTerminal =
                match this with
                | TimerMode timerStatement -> timerStatement.Timer.DN
                | CounterMode counterStatement -> counterStatement.Counter.DN


    /// 실행을 가지는 type
    type CommandTypes =
        | CoilCmd of CoilOutputMode
        /// Predicate.  (boolean function)
        | PredicateCmd of Predicate
        /// Non-boolean function
        | FunctionCmd of Function
        | ActionCmd of PLCAction
        /// Timer, Counter 등
        | FunctionBlockCmd of FunctionBlock

//let createPLCCommandCopy(endTag, from, toTag) = FunctionPure.CopyMode(endTag, (from, toTag))

[<AutoOpen>]
module XgiExpressionConvertorModule =
    type XgiStorage = ResizeArray<IStorage>

    let operatorToXgiFunctionName =
        function
        | ">" -> "GT"
        | ">=" -> "GTE"
        | "<" -> "LT"
        | "<=" -> "LTE"
        | "=" -> "EQ"
        | "!=" -> "NE"
        | "+" -> "ADD"
        | "-" -> "SUB"
        | "*" -> "MUL"
        | "/" -> "DIV"
        | _ -> failwithlog "ERROR"

    type private AugmentedConvertorParams =
        { Storage: XgiStorage
          ExpandFunctionStatements: ResizeArray<Statement>
          Exp: IExpression }

    /// expression 내부의 비교 및 사칙 연산을 xgi function 으로 대체
    let private replaceInnerArithmaticOrComparisionToXgiFunctionStatements
        { Storage = newLocalStorages
          ExpandFunctionStatements = expandFunctionStatements
          Exp = exp }
        : IExpression =
        let xgiLocalVars = ResizeArray<IXgiVar>()

        let rec helper (exp: IExpression) =
            [ match exp.FunctionName with
              | Some funcName ->
                  let newArgs = exp.FunctionArguments |> bind helper

                  match funcName with
                  | ("&&" | "||" | "!") -> exp.WithNewFunctionArguments newArgs
                  | (">" | ">=" | "<" | "<=" | "=" | "!=" | "+" | "-" | "*" | "/") as op ->
                      let out = createTypedXgiAutoVariable "out" exp.BoxedEvaluatedValue $"{op} output"
                      xgiLocalVars.Add out

                      expandFunctionStatements.Add
                      <| DuAugmentedPLCFunction
                          { FunctionName = op
                            Arguments = newArgs
                            Output = out }

                      out.ToExpression()

                  | (FunctionNameRising | FunctionNameFalling) -> exp
                  | _ -> failwithlog "ERROR"
              | _ -> exp ]

        let newExp = helper exp |> List.exactlyOne
        xgiLocalVars.Cast<IStorage>() |> newLocalStorages.AddRange // 위의 helper 수행 이후가 아니면, xgiLocalVars 가 채워지지 않는다.
        newExp

    let rec private binaryToNary
        (augmentParams: AugmentedConvertorParams)
        (operatorsToChange: string list)
        (currentOp: string)
        : IExpression list =
        let { Storage = storage
              ExpandFunctionStatements = augmentedStatementsStorage
              Exp = exp } =
            augmentParams

        let withAugmentedPLCFunction (exp: IExpression) =
            let op = exp.FunctionName.Value

            let out =
                createTypedXgiAutoVariable "_temp_internal_" exp.BoxedEvaluatedValue $"{op} output"

            storage.Add out

            let args =
                exp.FunctionArguments
                |> List.bind (fun arg -> binaryToNary { augmentParams with Exp = arg } operatorsToChange op)

            DuAugmentedPLCFunction
                { FunctionName = op
                  Arguments = args
                  Output = out }
            |> augmentedStatementsStorage.Add

            out.ToExpression()

        match exp.FunctionName with
        | Some op when operatorsToChange.Contains(op) -> // ("+"|"-"|"*"|"/"   (*|"&&"|"||"*) as op) ->
            if op = currentOp then
                let args =
                    [ for arg in exp.FunctionArguments do
                          match arg.Terminal, arg.FunctionName with
                          | Some _, _ -> yield arg
                          | None, Some("-" | "/") -> yield withAugmentedPLCFunction arg
                          | None, Some _fn -> yield! binaryToNary { augmentParams with Exp = arg } operatorsToChange op
                          | _ -> failwithlog "ERROR" ]

                args
            else
                [ withAugmentedPLCFunction exp ]
        | _ -> [ exp ]

    type private MergeArithmaticResult =
        /// arithmatic operator 를 적용해서, 결과 값을 이미 tag / variable 에 write 한 경우.  추후의 expression 과 혼합할 필요가 없다.
        | AlreadyApplied of IExpression
        | NotApplied of IExpression

    (* see ``ADD 3 items test`` *)
    /// 사칙 연산 처리
    /// - a + b + c => + [a; b; c] 로 변환
    ///     * '+' or '*' 연산에서 argument 갯수가 8 개 이상이면 분할해서 PLC function 생성
    /// - a + (b * c) + d => +[a; x; d], *[b; c] 두개의 expression 으로 변환.  부가적으로 생성된 *[b;c] 는 새로운 statement 를 생성해서 augmentedStatementsStorage 에 추가된다.
    let private mergeArithmaticOperator
        (augmentParams: AugmentedConvertorParams)
        (outputStore: IStorage option)
        : MergeArithmaticResult =
        let { Storage = newLocalStorages
              ExpandFunctionStatements = augmentedStatementsStorage
              Exp = exp } =
            augmentParams

        match exp.FunctionName with
        | Some("+" | "-" | "*" | "/" as op) ->
            let newArgs =
                binaryToNary { augmentParams with Exp = exp } [ "+"; "-"; "*"; "/" ] op

            match op with
            | "+"
            | "*" when newArgs.Length >= 8 ->
                let rec chunkBy8 (prevSum: IExpression list) (argsRemaining: IExpression list) : IExpression =
                    let allArgs = prevSum @ argsRemaining
                    let numSkip = min 8 allArgs.Length
                    let args = allArgs.Take(numSkip).ToFSharpList()
                    let argsRemaining = allArgs |> List.skip numSkip

                    let out =
                        if argsRemaining.IsEmpty then
                            outputStore.Value
                        else
                            createTypedXgiAutoVariable "_temp_internal_" exp.BoxedEvaluatedValue "comment"

                    let outexp = out.ToExpression()

                    DuAugmentedPLCFunction
                        { FunctionName = op
                          Arguments = args
                          Output = out :?> INamedExpressionizableTerminal }
                    |> augmentedStatementsStorage.Add

                    out |> newLocalStorages.Add

                    if allArgs.Length <= 8 then
                        outexp
                    else
                        chunkBy8 [ outexp ] argsRemaining

                AlreadyApplied(chunkBy8 [] newArgs)
            | _ -> NotApplied(exp.WithNewFunctionArguments newArgs)
        | Some(">" | ">=" | "<" | "<=" | "=" | "!=" | "&&" | "||" as op) ->
            let newArgs = binaryToNary { augmentParams with Exp = exp } [ op ] op
            NotApplied(exp.WithNewFunctionArguments newArgs)
        | _ -> NotApplied(exp)

    let private splitWideExpression (augmentParams: AugmentedConvertorParams) : IExpression =
        let { Storage = newLocalStorages
              ExpandFunctionStatements = expandFunctionStatements }: AugmentedConvertorParams =
            augmentParams

        let exp =
            match mergeArithmaticOperator augmentParams None with
            | AlreadyApplied exp -> exp
            | NotApplied exp -> exp

        let w, _h = exp.Flatten() :?> FlatExpression |> precalculateSpan

        if w > maxNumHorizontalContact then
            match exp.FunctionName with
            | Some "&&" ->
                let mutable partSpanX = 0
                let maxX = maxNumHorizontalContact

                let folder (z: IExpression list list * IExpression list) (e: IExpression) =
                    let built, building = z
                    let spanX = e.Flatten() :?> FlatExpression |> precalculateSpan |> fst

                    let max, remaining =
                        if partSpanX + spanX > maxX then
                            partSpanX <- spanX
                            built +++ building, [ e ]
                        else
                            partSpanX <- partSpanX + spanX
                            built, building @ [ e ]
                    max |> filter List.any, remaining

                let maxs, remaining = (exp.FunctionArguments |> List.fold folder ([], []))

                let subSums =
                    [ for max in maxs do
                          let out = createXgiAutoVariableT "_temp_internal_" "&& split output" false
                          newLocalStorages.Add out

                          DuAugmentedPLCFunction
                              { FunctionName = "&&"
                                Arguments = max
                                Output = out }
                          |> expandFunctionStatements.Add

                          var2expr out :> IExpression ]

                let grandTotal = createXgiAutoVariableT "_temp_internal_" "&& split output" false
                newLocalStorages.Add grandTotal

                DuAugmentedPLCFunction
                    { FunctionName = "&&"
                      Arguments = subSums @ remaining
                      Output = grandTotal }
                |> expandFunctionStatements.Add

                var2expr grandTotal :> IExpression
            | _ -> exp
        else
            exp

    let private collectExpandedExpression (augmentParams: AugmentedConvertorParams) : IExpression =
        let newExp =
            replaceInnerArithmaticOrComparisionToXgiFunctionStatements augmentParams

        let newExp = splitWideExpression { augmentParams with Exp = newExp }
        newExp



    let private statement2XgiStatements (newLocalStorages: XgiStorage) (statement: Statement) : Statement list =
        let augmentedStatements = ResizeArray<Statement>() // DuAugmentedPLCFunction case

        let newStatements =
            match statement with
            | DuAssign(exp, target) ->
                // todo : "sum := tag1 + tag2" 의 처리 : DuAugmentedPLCFunction 하나로 만들고, 'OUT' output 에 sum 을 할당하여야 한다.
                match exp.FunctionName with
                | Some("+" | "-" | "*" | "/" as op) ->

                    let augStmtsStorage = ResizeArray<Statement>()

                    match
                        mergeArithmaticOperator
                            { Storage = newLocalStorages
                              ExpandFunctionStatements = augStmtsStorage
                              Exp = exp }
                            (Some target)
                    with
                    | AlreadyApplied _exp -> augStmtsStorage.ToFSharpList()
                    | NotApplied exp ->
                        let tgt = target :?> INamedExpressionizableTerminal

                        augStmtsStorage.ToFSharpList()
                        @ [ DuAugmentedPLCFunction
                                { FunctionName = op
                                  Arguments = exp.FunctionArguments
                                  Output = tgt } ]
                | _ ->
                    let newExp =
                        collectExpandedExpression
                            { Storage = newLocalStorages
                              ExpandFunctionStatements = augmentedStatements
                              Exp = exp }

                    [ DuAssign(newExp, target) ]

            | DuVarDecl(exp, decl) ->
                let _newExp =
                    collectExpandedExpression
                        { Storage = newLocalStorages
                          ExpandFunctionStatements = augmentedStatements
                          Exp = exp }

                (* 일반 변수 선언 부분을 xgi local variable 로 치환한다. *)
                newLocalStorages.Remove decl |> ignore

                match decl with
                | :? IXgiVar as loc ->
                    let si = loc.SymbolInfo
                    let comment = loc.Comment.DefaultValue $"[local var in code] {si.Comment}"
                    let initValue = exp.BoxedEvaluatedValue

                    let _typ = initValue.GetType()
                    let var = createXgiVariable decl.Name initValue comment
                    newLocalStorages.Add var

                | (:? IVariable | :? ITag) when decl.IsGlobal -> newLocalStorages.Add decl
                | (:? IVariable | :? ITag) ->
                    let var = createXgiVariable decl.Name decl.BoxedValue decl.Comment
                    newLocalStorages.Add var

                | _ -> failwithlog "ERROR"

                []
            | (DuTimer _ | DuCounter _) -> [ statement ]

            | DuAction(DuCopy(condition, source, target)) ->
                let funcName = XgiConstants.FunctionNameMove
                let output = target :?> INamedExpressionizableTerminal

                [ DuAugmentedPLCFunction
                      { FunctionName = funcName
                        Arguments = [ condition; source ]
                        Output = output } ]

            | DuAugmentedPLCFunction _ -> failwithlog "ERROR"

        augmentedStatements @ newStatements |> List.ofSeq

    /// S -> [XS]
    let internal commentedStatement2CommentedXgiStatements
        (prjParam: XgiProjectParams)
        (newLocalStorages: XgiStorage)
        (CommentedStatement(comment, statement))
        : CommentedXgiStatements =
        let xgiStatements = statement2XgiStatements newLocalStorages statement

        let rungComment =
            let statementComment = statement.ToText()

            match comment.NonNullAny(), prjParam.AppendExpressionTextToRungComment with
            | true, true -> $"{comment}\r\n{statementComment}"
            | true, false -> comment
            | false, true -> statementComment
            | false, false -> ""
            |> escapeXml

        CommentedXgiStatements(rungComment, xgiStatements)
