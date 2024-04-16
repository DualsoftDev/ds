namespace PLC.CodeGen.LS

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
            . $tmp := $t1 + $t2
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
    let internal systemTypeToXgiTypeName (typ: System.Type) =
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

    let private systemTypeToXgkTypeName (typ: System.Type) =
        match typ.Name with
        | BOOL -> "BIT"
        | (INT8 | UINT8 | CHAR)
        | (INT16 | UINT16) 
        | (INT32 | UINT32) -> "WORD"
        | _ -> failwithlog "ERROR"

    let systemTypeToXgxTypeName (target:PlatformTarget) (typ: System.Type) =
        match target with
        | XGI -> systemTypeToXgiTypeName typ 
        | XGK -> systemTypeToXgkTypeName typ
        | _ -> failwithlog "ERROR"


    let mutable internal autoVariableCounter = 0

    type IXgxVar =
        inherit IVariable
        inherit INamedExpressionizableTerminal
        abstract SymbolInfo: SymbolInfo

    type IXgxVar<'T> =
        inherit IXgxVar
        inherit IVariable<'T>

    /// XGI/XGK 에서 사용하는 tag 주소를 갖지 않는 variable
    type XgxVar<'T when 'T: equality>(param: StorageCreationParams<'T>) =
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

        interface IXgxVar with
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

    type CommentedXgxStatements = CommentedXgiStatements of comment: string * statements: Statement list

    let (|CommentAndXgxStatements|) = function | CommentedXgiStatements(x, ys) -> x, ys

    let commentAndXgxStatement = (|CommentAndXgxStatements|)

    let createXgxVariable (name: string) (initValue: obj) comment : IXgxVar =
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
        | BOOL -> XgxVar<bool>(createParam ())
        | CHAR -> XgxVar<char>(createParam ())
        | FLOAT32 -> XgxVar<single>(createParam ())
        | FLOAT64 -> XgxVar<double>(createParam ())
        | INT16 -> XgxVar<int16>(createParam ())
        | INT32 -> XgxVar<int32>(createParam ())
        | INT64 -> XgxVar<int64>(createParam ())
        | INT8 -> XgxVar<int8>(createParam ())
        | STRING -> XgxVar<string>(createParam ())
        | UINT16 -> XgxVar<uint16>(createParam ())
        | UINT32 -> XgxVar<uint32>(createParam ())
        | UINT64 -> XgxVar<uint64>(createParam ())
        | UINT8 -> XgxVar<uint8>(createParam ())
        | _ -> failwithlog "ERROR"

    let sys = DsSystem("")

    let createTypedXgiAutoVariable (nameHint: string) (initValue: obj) comment : IXgxVar =
        autoVariableCounter <- autoVariableCounter + 1
        let name = $"_tmp{nameHint}{autoVariableCounter}"
        createXgxVariable name initValue comment


    let internal createXgiAutoVariableT (nameHint: string) comment (initValue: 'T) =
        autoVariableCounter <- autoVariableCounter + 1
        let name = $"_tmp{nameHint}{autoVariableCounter}"

        let param =
            { defaultStorageCreationParams (initValue) (VariableTag.PlcUserVariable|>int) with
                Name = name
                Comment = Some comment }

        XgxVar(param)


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
module XgxExpressionConvertorModule =
    type XgxStorage = ResizeArray<IStorage>

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

    let operatorToXgkFunctionName op =
        match op with
        | "+" -> "ADD"
        | "-" -> "SUB"
        | "*" -> "MUL"
        | "/" -> "DIV"
        | (">" | ">=" | "<"  | "<="  | "=" | "<>" | "!=" ) -> op
        | _ -> failwithlog "ERROR"

    type private AugmentedConvertorParams =
        { Storage: XgxStorage
          ExpandFunctionStatements: StatementContainer
          Exp: IExpression }

    /// expression 내부의 비교 및 사칙 연산을 xgi/xgk function 으로 대체
    ///
    /// - 인자로 받은 {exp, expandFunctionStatements, newLocalStorages} 를 이용해서,
    ///
    ///   * 추가되는 statement 는 expandFunctionStatements 에 추가하고,
    ///
    ///   * 추가되는 local variable 은 newLocalStorages 에 추가한다.
    ///
    ///   * 새로 생성되는 expression 을 반환한다.
    let private replaceInnerArithmaticOrComparisionToXgiFunctionStatements (prjParam: XgxProjectParams)
        { Storage = newLocalStorages
          ExpandFunctionStatements = expandFunctionStatements
          Exp = exp }
      : IExpression =
        let xgiLocalVars = ResizeArray<IXgxVar>()

        let rec helper (exp: IExpression) =
            [   match exp.FunctionName with
                | Some funcName ->
                    let newArgs = exp.FunctionArguments |> bind helper

                    match funcName with
                    | ("&&" | "||" | "!") -> exp.WithNewFunctionArguments newArgs
                    | (">" | ">=" | "<" | "<=" | "=" | "!=" | "+" | "-" | "*" | "/") as op ->
                        if prjParam.TargetType <> XGI then 
                            failwithlog $"Inline function only supported on XGI"

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
                | _ -> exp
            ]

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
    /// - a + b + c => + [a; b; c] 로 변환 (flat 처리)
    ///     * '+' or '*' 연산에서 argument 갯수가 8 개 이상이면 분할해서 PLC function 생성
    /// - a + (b * c) + d => +[a; x; d], *[b; c] 두개의 expression 으로 변환.  부가적으로 생성된 *[b;c] 는 새로운 statement 를 생성해서 augmentedStatementsStorage 에 추가된다.
    let private mergeArithmaticOperator
        (_prjParam: XgxProjectParams)
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
            | _ ->
                NotApplied(exp.WithNewFunctionArguments newArgs)

        | Some(">" | ">=" | "<" | "<=" | "=" | "!=" | "&&" | "||" as op) ->
            let newArgs = binaryToNary { augmentParams with Exp = exp } [ op ] op
            NotApplied(exp.WithNewFunctionArguments newArgs)

        | _ ->
            NotApplied(exp)

    let rec private zipAndExpression (prjParam: XgxProjectParams) (augmentParams: AugmentedConvertorParams) (allowCallback:bool) : IExpression =
        let { Storage = newLocalStorages
              ExpandFunctionStatements = expandFunctionStatements
              Exp = exp
              }: AugmentedConvertorParams =
            augmentParams

        let flatExpression = exp.Flatten() :?> FlatExpression
        let w, _h = flatExpression |> precalculateSpan

        if w > maxNumHorizontalContact then
            let exp = if allowCallback then zipVisitor prjParam augmentParams else exp

            match exp.FunctionName with
            | Some op when op.IsOneOf("||") ->
                zipVisitor prjParam { augmentParams with Exp = exp }
            | Some op when op = "&&" ->
                let mutable partSpanX = 0
                let maxX = maxNumHorizontalContact

                let folder (z: IExpression list list * IExpression list) (e: IExpression) =
                    let built, building = z
                    let _flatExp = e.Flatten() :?> FlatExpression
                    let spanX = e.Flatten() :?> FlatExpression |> precalculateSpan |> fst

                    let max, remaining =
                        if partSpanX + spanX > maxX then
                            partSpanX <- spanX
                            built +++ building, [ e ]
                        else
                            partSpanX <- partSpanX + spanX
                            built, building @ [ e ]
                    (max |> filter List.any), remaining

                let maxs, remaining = List.fold folder ([], []) exp.FunctionArguments

                let subSums =
                    [ for max in maxs do
                          let out = createXgiAutoVariableT "_temp_internal_"  ($"{op} split output") false
                          newLocalStorages.Add out

                          DuAugmentedPLCFunction
                              { FunctionName = op
                                Arguments = max
                                Output = out }
                          |> expandFunctionStatements.Add

                          var2expr out :> IExpression ]

                let grandTotal = createXgiAutoVariableT "_temp_internal_" ($"{op} split output") false
                newLocalStorages.Add grandTotal

                DuAugmentedPLCFunction
                    { FunctionName = op
                      Arguments = subSums @ remaining
                      Output = grandTotal }
                |> expandFunctionStatements.Add

                var2expr grandTotal :> IExpression
            | _ -> exp
        else
            exp

    and private zipVisitor (prjParam: XgxProjectParams) (augmentParams: AugmentedConvertorParams) : IExpression =
        let exp =
            match mergeArithmaticOperator prjParam augmentParams None with
            | AlreadyApplied exp -> exp
            | NotApplied exp -> exp


        let w, _h = exp.Flatten() :?> FlatExpression |> precalculateSpan

        if w > maxNumHorizontalContact && exp.FunctionName.IsSome && exp.FunctionName.Value.IsOneOf("&&", "||") then
            if exp.FunctionArguments.Any(fun e -> e.Flatten() :?> FlatExpression |> precalculateSpan |> fst >= 20 ) then
                let args = [
                    for arg in exp.FunctionArguments do
                        zipAndExpression prjParam {augmentParams with Exp = arg } true
                ]
                let psedoFunction (_args: Args) : bool =
                    failwithlog "THIS IS PSEUDO FUNCTION.  SHOULD NOT BE EVALUATED!!!!"
                DuFunction { FunctionBody = psedoFunction;  Name = exp.FunctionName.Value; Arguments = args }
            else
                let allowCallback = false
                zipAndExpression prjParam {augmentParams with Exp = exp } allowCallback


            //match exp.FunctionName with
            //| Some "&&" ->
            //    zipAndExpression prjParam {augmentParams with Exp = exp }
            //| _ ->
            //    exp
        else
            exp

    let private collectExpandedExpression (prjParam: XgxProjectParams) (augmentParams: AugmentedConvertorParams) : IExpression =
        let newExp =
            replaceInnerArithmaticOrComparisionToXgiFunctionStatements prjParam augmentParams

        let newExp = zipVisitor  prjParam { augmentParams with Exp = newExp }
        newExp

    /// exp 내에 포함된, {문장(statement)으로 추출 해야만 할 요소}를 newStatements 에 추가한다.
    /// 이 과정에서 추가할 필요가 있는 storate 는 newLocalStorages 에 추가한다.
    /// 반환 : exp, 추가된 storage, 추가된 statement
    ///
    /// e.g: XGK 의 경우, 함수를 지원하지 않으므로,
    ///     입력 exp: "2 + 3 > 4"
    ///     추가 statement : "tmp1 := 2 + 3"
    ///     추가 storage : tmp2
    ///     최종 exp: "tmp1 > 4"
    ///     반환 : exp, [tmp2], [tmp1 := 2 + 3]
    let exp2exp (prjParam: XgxProjectParams) (exp: IExpression) (newLocalStorages: XgxStorage) (newStatements:StatementContainer) : IExpression =
        let rec helper (nestLevel:int) (exp: IExpression) : IExpression * IStorage list * Statement list =
            if exp.Terminal.IsSome || prjParam.TargetType = XGI then
                exp, [], []
            else
                match exp.FunctionName, exp.FunctionArguments with
                | Some fn, l::r::[] ->
                    let lexpr, lstgs, lstmts = helper (nestLevel + 1) l
                    let rexpr, rstgs, rstmts = helper (nestLevel + 1) r

                    if fn.IsOneOf("!=", "=", "<>") && lexpr.DataType = typeof<bool> then
                        // XGK 에는 bit 의 비교 연산이 없다.  따라서, bool 타입의 비교 연산을 수행할 경우, 이를 OR, AND 로 변환한다.
                        let l, r, nl, nr = lexpr, rexpr, fLogicalNot [lexpr], fLogicalNot [rexpr]
                        let newExp =
                            match fn with
                            | ("!=" | "<>") -> fLogicalOr([fLogicalAnd [l; nr]; fLogicalAnd [nl; r]])
                            | "=" -> fLogicalOr([fLogicalAnd [l; r]; fLogicalAnd [nl; nr]])
                        newExp, (lstgs @ rstgs), (lstmts @ rstmts)
                    else
                        // XGK 에는 IEC Function 을 이용할 수 없으므로, 수식 내에 포함된 사칙 연산이나 비교 연산을 XGK function 으로 변환한다.
                        let newExp = DuFunction{FunctionBody = PsedoFunction<bool>; Name=fn; Arguments=[lexpr; rexpr]}

                        let tmpVar = createTypedXgiAutoVariable "_temp_internal_" exp.BoxedEvaluatedValue $"{exp} store"
                        let stg = tmpVar :> IStorage
                        let varExp = tmpVar.ToExpression()
                        match fn with
                        | ("+" | "-" | "*" | "/")
                        | (">" | ">=" | "<" | "<=" | "=" | "!=") ->
                            let stmt = DuAssign(newExp, tmpVar)
                            varExp, (lstgs @ rstgs @ [ stg ]), (lstmts @ rstmts @ [ stmt ])
                        | _ ->
                            if lstgs.any() || rstgs.any() then
                                newExp, (lstgs @ rstgs @ [ stg ]), (lstmts @ rstmts)
                            else
                                exp, [], []
                | _ ->
                    exp, [], []

        let expr, stgs, stmts = helper 0 exp
        newLocalStorages.AddRange stgs
        newStatements.AddRange stmts
        expr



    /// Statement 확장
    let private statement2XgxStatements (prjParam: XgxProjectParams) (newLocalStorages: XgxStorage) (statement: Statement) : Statement list =
        let augmentedStatements = StatementContainer() // DuAugmentedPLCFunction case

        let newStatements =
            match statement with
            | DuAssign(exp, target) ->
                let exp = exp2exp prjParam exp newLocalStorages augmentedStatements


                // todo : "sum := tag1 + tag2" 의 처리 : DuAugmentedPLCFunction 하나로 만들고, 'OUT' output 에 sum 을 할당하여야 한다.
                match exp.FunctionName with
                | Some("+" | "-" | "*" | "/" as op) ->

                    let augArithmaticAssignStatements = StatementContainer()
                    match
                        mergeArithmaticOperator prjParam
                            { Storage = newLocalStorages
                              ExpandFunctionStatements = augArithmaticAssignStatements
                              Exp = exp }
                            (Some target)
                    with
                    | AlreadyApplied _exp -> augArithmaticAssignStatements.ToFSharpList()
                    | NotApplied exp ->
                        let tgt = target :?> INamedExpressionizableTerminal

                        augArithmaticAssignStatements.ToFSharpList()
                        @ [ DuAugmentedPLCFunction
                                { FunctionName = op
                                  Arguments = exp.FunctionArguments
                                  Output = tgt } ]
                | _ ->
                    let newExp =
                        collectExpandedExpression prjParam
                            { Storage = newLocalStorages
                              ExpandFunctionStatements = augmentedStatements
                              Exp = exp }

                    [ DuAssign(newExp, target) ]

            | DuVarDecl(exp, decl) ->
                let _newExp =
                    collectExpandedExpression prjParam
                        { Storage = newLocalStorages
                          ExpandFunctionStatements = augmentedStatements
                          Exp = exp }

                (* 일반 변수 선언 부분을 xgi local variable 로 치환한다. *)
                newLocalStorages.Remove decl |> ignore

                match decl with
                | :? IXgxVar as loc ->
                    let si = loc.SymbolInfo
                    let comment = loc.Comment.DefaultValue $"[local var in code] {si.Comment}"
                    let initValue = exp.BoxedEvaluatedValue

                    let _typ = initValue.GetType()
                    let var = createXgxVariable decl.Name initValue comment
                    newLocalStorages.Add var

                | (:? IVariable | :? ITag) when decl.IsGlobal -> newLocalStorages.Add decl
                | (:? IVariable | :? ITag) ->
                    let var = createXgxVariable decl.Name decl.BoxedValue decl.Comment
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
    let internal statement2Statements
        (prjParam: XgxProjectParams)
        (newLocalStorages: XgxStorage)
        (CommentedStatement(comment, statement))
      : CommentedXgxStatements =
        let xgiStatements = statement2XgxStatements prjParam newLocalStorages statement

        let rungComment =
            [
                comment
                if prjParam.AppendDebugInfoToRungComment then
                    let statementComment = statement.ToText()
                    statementComment
            ] |> ofNotNullAny |> String.concat "\r\n"
            |> escapeXml

       
        CommentedXgiStatements(rungComment, xgiStatements)
