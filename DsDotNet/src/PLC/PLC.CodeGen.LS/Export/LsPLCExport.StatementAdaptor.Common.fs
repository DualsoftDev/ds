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
            tmp1 = <expr1> 및 tmp2 = <expr2> 와 같이
            . 임시 변수를 생성해서 assign rung 을 만들고
            . 임시 변수를 add 함수의 입력 argument 에 tag 로 작성하도록 한다.

    - 임의의 rung 에 사칙연산 함수가 포함되면, 해당 부분만 잘라서 임시 변수에 저장하고 그 값을 이용해야 한다.
      * e.g $result = ($t1 + $t2) > 3
            . $tmp = $t1 + $t2
            . $result = $tmp > 3

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
        // pp.60, https://sol.ls-electric.com/uploads/document/16572861196090/XGI%20%EC%B4%88%EA%B8%89_V21_.pdf
        match typ.Name with
        | BOOL -> "BOOL"
        | CHAR -> "BYTE"
        | INT8 -> "SINT"
        | INT16 -> "INT"
        | INT32 -> "DINT"
        | INT64 -> "LINT"
        | UINT8 -> "USINT"
        | UINT16 -> "UINT"
        | UINT32 -> "UDINT"
        | UINT64 -> "ULINT"
        | FLOAT32 -> "REAL"
        | FLOAT64 -> "LREAL"
        | STRING -> "STRING" // 32 byte
        | _ -> failwithlog "ERROR"

    let private systemTypeToXgkTypeName (typ: System.Type) =
        match typ.Name with
        | BOOL -> "BIT"
        | (INT8 | UINT8 | CHAR)
        | (INT16 | UINT16) 
        | (INT32 | UINT32) -> "WORD"

        | (STRING | FLOAT32 | FLOAT64 | INT64 | UINT64) -> "WORD"     // xxx 이거 맞나?

        | _ -> failwithlog "ERROR"

    let systemTypeToXgxTypeName (target:PlatformTarget) (typ: System.Type) =
        match target with
        | XGI -> systemTypeToXgiTypeName typ 
        | XGK -> systemTypeToXgkTypeName typ
        | _ -> failwithlog "ERROR"

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
        match name with
        | "_1ON" | "_1OFF" -> ()
        | _ ->
            match name |> Seq.toList with
            | ch :: _ when isHangul ch -> ()
            | ch1 :: ch2 :: _ when isValidStart ch1 && isValidStart ch2 -> ()
            | _ -> failwith $"Invalid XGI variable name {name}.  Use longer name"

        match name with
        | RegexPattern @"^ld(\d)+" _ -> failwith $"Invalid XGI variable name {name}."
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
        | "DuFunction" ->
            let defaultBool = 
                { defaultStorageCreationParams false (VariableTag.PlcUserVariable|>int) with
                    Name = name
                    Comment = Some comment }

            XgxVar<bool>(defaultBool)
        | _ -> failwithlog "ERROR"

    let sys = DsSystem("")

    let private getTmpName (nameHint: string) (n:int) = $"_t{n}_{nameHint}"
    let createTypedXgxAutoVariable (prjParam: XgxProjectParams) (nameHint: string) (initValue: obj) comment : IXgxVar =
        let n = prjParam.AutoVariableCounter()
        let name = getTmpName nameHint n
        createXgxVariable name initValue comment


    let internal createXgxAutoVariableT (prjParam: XgxProjectParams) (nameHint: string) comment (initValue: 'T) =
        let n = prjParam.AutoVariableCounter()
        let name = getTmpName nameHint n

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
        /// "Param="MOV,SRC,DST"" 와 같은 형태의 명령. int 는 명령의 길이.  대부분 3
        | XgkParamCmd of string * int
        /// Timer, Counter 등
        | FunctionBlockCmd of FunctionBlock

//let createPLCCommandCopy(endTag, from, toTag) = FunctionPure.CopyMode(endTag, (from, toTag))

[<AutoOpen>]
module XgxExpressionConvertorModule =
    /// '_ON' 에 대한 flat expression
    let fakeAlwaysOnFlatExpression =
        let on =
            { new System.Object() with
                member x.Finalize() = ()
              interface IExpressionizableTerminal with
                  member x.ToText() = "_ON" }

        FlatTerminal(on, false, false)

    /// '_ON' 에 대한 expression
    let fakeAlwaysOnExpression: Expression<bool> =
        let on = createXgxVariable "_ON" true "가짜 _ON" :?> XgxVar<bool>
        DuTerminal(DuVariable on)

    /// '_1ON' 에 대한 expression
    let fake1OnExpression: Expression<bool> =
        let on = createXgxVariable "_1ON" true "가짜 _1ON" :?> XgxVar<bool>
        DuTerminal(DuVariable on)


    let operatorToXgiFunctionName =
        function
        | ">" -> "GT"
        | ">=" -> "GE"
        | "<" -> "LT"
        | "<=" -> "LE"
        | "==" -> "EQ"
        | "!=" -> "NE"
        | "+" -> "ADD"
        | "-" -> "SUB"
        | "*" -> "MUL"
        | "/" -> "DIV"
        | _ -> failwithlog "ERROR"

    let operatorToMnemonic op =
        try
            operatorToXgiFunctionName op
        with ex ->
            match op with
            | "||" -> "OR"
            | "&&" -> "AND"
            | "<>" -> "NE"
            | _ -> failwithlog "ERROR"


    type internal AugmentedConvertorParams =
        { Storage: XgxStorage
          ExpandFunctionStatements: StatementContainer
          Exp: IExpression
          /// Exp 을 평가한 결과를 저장하는 변수
          ExpStore: IStorage option}

    /// expression 내부의 비교 및 사칙 연산을 xgi/xgk function 으로 대체
    ///
    /// - 인자로 받은 {exp, expandFunctionStatements, newLocalStorages} 를 이용해서,
    ///
    ///   * 추가되는 statement 는 expandFunctionStatements 에 추가하고,
    ///
    ///   * 추가되는 local variable 은 newLocalStorages 에 추가한다.
    ///
    ///   * 새로 생성되는 expression 을 반환한다.
    let internal replaceInnerArithmaticOrComparisionToXgiFunctionStatements
        (prjParam: XgxProjectParams) (augmentParams: AugmentedConvertorParams)
      : IExpression =
        let { Storage = newLocalStorages
              ExpandFunctionStatements = expandFunctionStatements
              Exp = exp
              ExpStore = expStore} = augmentParams

        let functionTransformer (_level:int, functionExpression:IExpression, expStore:IStorage option) =
            match functionExpression.FunctionName with
            | Some(">" | ">=" | "<" | "<=" | "==" | "!=" | "+" | "-" | "*" | "/" as op) -> //when level <> 0 ->
                let args = functionExpression.FunctionArguments
                let var:IStorage =
                    expStore |> Option.defaultWith (fun () -> 
                        let initValue = functionExpression.BoxedEvaluatedValue
                        let comment = args |> map (fun a -> a.ToText()) |> String.concat $" {op} "
                        createTypedXgxAutoVariable prjParam "out" initValue comment )

                expandFunctionStatements.Add
                <| DuAugmentedPLCFunction
                    {   FunctionName = op
                        Arguments = args
                        OriginalExpression = functionExpression
                        Output = var }

                newLocalStorages.Add var
                var.ToExpression()
            | _ ->
                functionExpression

        let transformers = {TerminalHandler = snd; FunctionHandler = functionTransformer}
        let newExpression = exp.Transform(transformers, expStore)
        newExpression


    let rec internal binaryToNary
        (prjParam: XgxProjectParams)
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
                let tmpNameHint = operatorToMnemonic op
                createTypedXgxAutoVariable prjParam tmpNameHint exp.BoxedEvaluatedValue $"{op} output"

            storage.Add out

            let args =
                exp.FunctionArguments
                |> List.bind (fun arg -> binaryToNary prjParam { augmentParams with Exp = arg } operatorsToChange op)

            DuAugmentedPLCFunction
                { FunctionName = op
                  Arguments = args
                  OriginalExpression = exp
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
                          | None, Some _fn -> yield! binaryToNary prjParam { augmentParams with Exp = arg } operatorsToChange op
                          | _ -> failwithlog "ERROR" ]

                args
            else
                [ withAugmentedPLCFunction exp ]
        | _ -> [ exp ]

    type internal MergeArithmaticResult =
        /// arithmatic operator 를 적용해서, 결과 값을 이미 tag / variable 에 write 한 경우.  추후의 expression 과 혼합할 필요가 없다.
        | AlreadyApplied of IExpression
        | NotApplied of IExpression

    (* see ``ADD 3 items test`` *)
    /// 사칙 연산 처리
    /// - a + b + c => + [a; b; c] 로 변환 (flat 처리)
    ///     * '+' or '*' 연산에서 argument 갯수가 8 개 이상이면 분할해서 PLC function 생성
    /// - a + (b * c) + d => +[a; x; d], *[b; c] 두개의 expression 으로 변환.  부가적으로 생성된 *[b;c] 는 새로운 statement 를 생성해서 augmentedStatementsStorage 에 추가된다.
    let internal mergeArithmaticOperator
        (prjParam: XgxProjectParams)
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
                binaryToNary prjParam { augmentParams with Exp = exp } [ "+"; "-"; "*"; "/" ] op

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
                            let tmpNameHint, comment = operatorToMnemonic op, exp.ToText()
                            createTypedXgxAutoVariable prjParam tmpNameHint exp.BoxedEvaluatedValue comment

                    let outexp = out.ToExpression()

                    DuAugmentedPLCFunction
                        { FunctionName = op
                          Arguments = args
                          OriginalExpression = exp
                          Output = out }
                    |> augmentedStatementsStorage.Add

                    out |> newLocalStorages.Add

                    if allArgs.Length <= 8 then
                        outexp
                    else
                        chunkBy8 [ outexp ] argsRemaining

                AlreadyApplied(chunkBy8 [] newArgs)
            | _ ->
                NotApplied(exp.WithNewFunctionArguments newArgs)

        | Some(">" | ">=" | "<" | "<=" | "==" | "!=" | "&&" | "||" as op) ->
            let newArgs = binaryToNary prjParam { augmentParams with Exp = exp } [ op ] op
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
                          let out = createXgxAutoVariableT prjParam "split"  ($"{op} split output") false
                          newLocalStorages.Add out

                          DuAugmentedPLCFunction
                              { FunctionName = op
                                Arguments = max
                                OriginalExpression = exp
                                Output = out }
                          |> expandFunctionStatements.Add

                          var2expr out :> IExpression ]

                let grandTotal = createXgxAutoVariableT prjParam "split" ($"{op} split output") false
                newLocalStorages.Add grandTotal

                DuAugmentedPLCFunction
                    { FunctionName = op
                      Arguments = subSums @ remaining
                      OriginalExpression = exp
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

                exp.WithNewFunctionArguments args

            else
                let allowCallback = false
                zipAndExpression prjParam {augmentParams with Exp = exp } allowCallback
        else
            exp

    let internal collectExpandedExpression (prjParam: XgxProjectParams) (augmentParams: AugmentedConvertorParams) : IExpression =
        let newExp =
            replaceInnerArithmaticOrComparisionToXgiFunctionStatements prjParam augmentParams

        let newExp = zipVisitor  prjParam { augmentParams with Exp = newExp }
        newExp


    type IExpression with
        /// expression 을 임시 auto 변수에 저장하는 statement 로 만들고, 그 statement 와 auto variable 를 반환
        member x.ToAssignStatementAndAutoVariable (prjParam: XgxProjectParams) : (Statement * IXgxVar) =
            if x.Terminal.IsSome then
                failwith "Terminal expression cannot be converted to statement"

            let var = createTypedXgxAutoVariable prjParam "_temp_internal" false $"Temporary assignment for {x.ToText()}"
            DuAssign(None, x, var), var

    /// XGK/XGI 공용 Statement 확장
    let internal statement2XgxStatements (prjParam: XgxProjectParams) (augs:Augments) (statement: Statement) : unit =
        let augmentedStatements = StatementContainer() // DuAugmentedPLCFunction case

        let newStatements =
            match statement with
            | DuAssign(condition, exp, target) ->
                let defaultConvertorParams =
                    {   Storage = augs.Storages
                        ExpandFunctionStatements = augmentedStatements
                        Exp = exp
                        ExpStore = None}

                // todo : "sum = tag1 + tag2" 의 처리 : DuAugmentedPLCFunction 하나로 만들고, 'OUT' output 에 sum 을 할당하여야 한다.
                match exp.FunctionName with
                | Some("+" | "-" | "*" | "/" as op) ->

                    let augArithmaticAssignStatements = StatementContainer()
                    let param = { defaultConvertorParams with ExpandFunctionStatements = augArithmaticAssignStatements }
                    match
                        mergeArithmaticOperator prjParam param (Some target)
                    with
                    | AlreadyApplied _exp -> augArithmaticAssignStatements.ToFSharpList()
                    | NotApplied exp ->
                        augArithmaticAssignStatements.ToFSharpList()
                        @ [ DuAugmentedPLCFunction
                                { FunctionName = op
                                  Arguments = exp.FunctionArguments
                                  OriginalExpression = exp
                                  Output = target } ]
                | _ ->
                    let newExp = collectExpandedExpression prjParam defaultConvertorParams
                    [ DuAssign(condition, newExp, target) ]

            | DuVarDecl(exp, decl) ->
                let _newExp =
                    collectExpandedExpression prjParam
                        { Storage = augs.Storages
                          ExpandFunctionStatements = augmentedStatements
                          Exp = exp
                          ExpStore = Some decl}

                (* 일반 변수 선언 부분을 xgi local variable 로 치환한다. *)
                augs.Storages.Remove decl |> ignore

                match decl with
                | :? IXgxVar as loc ->
                    let si = loc.SymbolInfo
                    let comment = loc.Comment.DefaultValue $"[local var in code] {si.Comment}"
                    let initValue = exp.BoxedEvaluatedValue

                    let _typ = initValue.GetType()
                    let var = createXgxVariable decl.Name initValue comment
                    augs.Storages.Add var
                    []

                | (:? IVariable | :? ITag) when decl.IsGlobal ->
                    augs.Storages.Add decl
                    if prjParam.TargetType = XGK then
                        [ DuAssign(Some fake1OnExpression, exp, decl) ]
                    else
                        []
                | (:? IVariable | :? ITag) ->
                    let var = createXgxVariable decl.Name decl.BoxedValue decl.Comment
                    augs.Storages.Add var
                    []

                | _ -> failwithlog "ERROR"

            | (DuTimer _ | DuCounter _) -> [ statement ]

            | DuAction(DuCopy(condition, source, target)) ->
                let funcName = XgiConstants.FunctionNameMove
                [ DuAugmentedPLCFunction
                      { FunctionName = funcName
                        Arguments = [ condition; source ]
                        OriginalExpression = condition
                        Output = target } ]

            | DuAugmentedPLCFunction _ -> failwithlog "ERROR"

        augs.Statements.AddRange (augmentedStatements @ newStatements)


    type Statement with
        /// statement 내부에 존재하는 모든 expression 을 visit 함수를 이용해서 변환한다.   visit 의 예: exp.MakeFlatten()
        member x.VisitExpression (visit:IExpression -> IExpression) : Statement =
            let tryVisit (exp:IExpression<bool> option) : IExpression<bool> option =
                match exp with
                | Some exp -> visit (exp:>IExpression) :?> IExpression<bool> |> Some
                | None -> None

            match x with
            | DuAssign(condition, exp, tgt) -> DuAssign(tryVisit condition, visit exp, tgt)                
            | DuVarDecl(exp, var) -> DuVarDecl(visit exp, var)
            | DuTimer ({ RungInCondition = rungIn; ResetCondition = reset } as tmr) ->
                DuTimer { tmr with RungInCondition = tryVisit rungIn; ResetCondition = tryVisit reset }
            | DuCounter ({UpCondition = up; DownCondition = down; ResetCondition = reset; LoadCondition = load} as ctr) ->
                DuCounter {ctr with UpCondition = tryVisit up; DownCondition = tryVisit down; ResetCondition = tryVisit reset; LoadCondition = tryVisit load}
            | DuAction(DuCopy(condition, source, target)) ->
                DuAction(DuCopy(visit condition :?> IExpression<bool>, visit source, target))

            | DuAugmentedPLCFunction ({Arguments = args} as functionParameters) ->
                let newArgs = args |> map visit
                DuAugmentedPLCFunction { functionParameters with Arguments = newArgs }

        member x.MakeExpressionsFlattenizable() =
            let visitor (exp:IExpression) : IExpression = exp.MakeFlattenizable()
            x.VisitExpression visitor

        member x.AugmentXgkArithmeticExpressionToAssignStatemnt (prjParam: XgxProjectParams) (augs: Augments) =
            let rec visitor (exp:IExpression) : IExpression =
                if exp.Terminal.IsSome then
                    exp
                else
                    let exp2 =
                        let args = exp.FunctionArguments |> map visitor
                        exp.WithNewFunctionArguments args
                    match exp2.FunctionName with
                    | Some (("+" | "-" | "*" | "/") as fn) ->

                        let tmpNameHint = operatorToMnemonic fn
                        let tmpVar = createTypedXgxAutoVariable prjParam tmpNameHint exp2.BoxedEvaluatedValue $"{exp2.ToText()}"
                        let stg = tmpVar :> IStorage
                        let stmt = DuAssign(None, exp2, stg)
                        augs.Statements.Add stmt
                        tmpVar.ToExpression()
                    | _ -> exp2
            x.VisitExpression visitor

