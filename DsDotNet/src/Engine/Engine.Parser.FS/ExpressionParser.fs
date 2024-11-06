namespace Engine.Parser.FS

open System.Linq

open Antlr4.Runtime
open Dual.Common.Core.FS
open Engine.Core
open type exprParser
open Antlr4.Runtime.Tree
open System.Text.RegularExpressions
open System

[<AutoOpen>]
module rec ExpressionParserModule =
    let enableSubroutine = false
    let internal createParser (text: string) : exprParser =
        let inputStream = new AntlrInputStream(text)
        let lexer = exprLexer (inputStream)
        let tokenStream = CommonTokenStream(lexer)
        let parser = exprParser (tokenStream)

        let listener_lexer = new ErrorListener<int>(true)
        let listener_parser = new ErrorListener<IToken>(true)
        lexer.AddErrorListener(listener_lexer)
        parser.AddErrorListener(listener_parser)
        parser

    /// storage 이름이 주어졌을 때, 그 이름에 해당하는 storage 를 반환하는 함수의 type
    type StorageFinder = string -> IStorage option

    /// storages 에서 name 을 찾는 기본 함수
    let defaultStorageFinder (storages: Storages) (name: string) : IStorage option =
        storages.TryFindValue name

    let createLambdaCallExpression (storages:Storages) args (exp: IExpression) =
        let newExp =
            match exp.FunctionSpec with
            | Some fs ->
                let newFs:IFunctionSpec = fs.Duplicate()
                newFs.LambdaApplication <- Some { LambdaDecl = fs.LambdaDecl.Value; Arguments = args; Storages=storages }
                match newFs with
                | :? FunctionSpec<bool>   as newFs -> DuFunction newFs :> IExpression
                | :? FunctionSpec<int8>   as newFs -> DuFunction newFs
                | :? FunctionSpec<uint8>  as newFs -> DuFunction newFs
                | :? FunctionSpec<int16>  as newFs -> DuFunction newFs
                | :? FunctionSpec<uint16> as newFs -> DuFunction newFs
                | :? FunctionSpec<int32>  as newFs -> DuFunction newFs
                | :? FunctionSpec<uint32> as newFs -> DuFunction newFs
                | :? FunctionSpec<int64>  as newFs -> DuFunction newFs
                | :? FunctionSpec<uint64> as newFs -> DuFunction newFs
                | :? FunctionSpec<single> as newFs -> DuFunction newFs
                | :? FunctionSpec<double> as newFs -> DuFunction newFs
                | :? FunctionSpec<string> as newFs -> DuFunction newFs
                | :? FunctionSpec<char>   as newFs -> DuFunction newFs
                | _ -> failwith "ERROR"
            | _ -> failwith "ERROR"
        newExp

    let createExpression (parserData:ParserData) (storageFinder:StorageFinder) (ctx: ExprContext) : IExpression =

        let rec helper (ctx: ExprContext) : IExpression =
            let text = ctx.GetText()

            let expr : IExpression =
                match ctx with
                | :? FunctionCallExprContext as exp -> // functionName '(' arguments? ')'
                    debugfn $"FunctionCall: {text}"
                    let funName = exp.TryFindFirstChild<FunctionNameContext>().Value.GetText()

                    let args =
                        [ match exp.TryFindFirstChild<ExprListContext>() with
                          | Some exprListCtx ->
                              for exprCtx in exprListCtx.children.OfType<ExprContext>() do
                                  helper exprCtx
                          | None -> () ]
                    if predefinedFunctionNames.Contains funName then
                        createCustomFunctionExpression funName args
                    else
                        match parserData.LambdaDefs.TryFind(fun lmbd -> lmbd.Prototype.Name = funName) with
                        | Some lambdaDecl -> createLambdaCallExpression parserData.Storages args lambdaDecl.Body
                        | None -> failwith "ERROR"

                | :? CastingExprContext as exp -> // '(' type ')' expr
                    debugfn $"Casting: {text}"
                    let castName = exp.TryFindFirstChild<TypeContext>().Value.GetText()
                    let exprCtx = exp.TryFindFirstChild<ExprContext>().Value
                    let expr = helper exprCtx
                    createCustomFunctionExpression castName [ expr ]

                | (   :? BinaryExprMultiplicativeContext
                    | :? BinaryExprAdditiveContext
                    | :? BinaryExprBitwiseShiftContext
                    | :? BinaryExprRelationalContext
                    | :? BinaryExprEqualityContext
                    | :? BinaryExprBitwiseAndContext
                    | :? BinaryExprBitwiseXorContext
                    | :? BinaryExprBitwiseOrContext
                    | :? BinaryExprLogicalAndContext
                    | :? BinaryExprLogicalOrContext) ->

                    debugfn $"Binary: {text}"

                    match ctx.children.ToFSharpList() with
                    | left :: op :: [ right ] ->
                        let expL = helper (left :?> ExprContext)
                        let expR = helper (right :?> ExprContext)
                        let op = op.GetText()
                        createBinaryExpression expL op expR
                    | _ -> failwithlog "ERROR"


                | :? UnaryExprContext as exp ->
                    debugfn $"Unary: {text}"

                    match exp.children.ToFSharpList() with
                    | op :: [ opnd ] ->
                        let exp = helper (opnd :?> ExprContext)
                        let op = op.GetText()
                        createUnaryExpression op exp
                    | _ -> failwithlog "ERROR"

                | :? TerminalExprContext as terminalExp ->
                    debugfn $"Terminal: {text}"
                    assert (terminalExp.ChildCount = 1)
                    let terminal = terminalExp.children[0].GetChild(0)

                    match terminal with
                    | :? LiteralContext as exp ->
                        assert (exp.ChildCount = 1)
                        let lit2exp x = literal2expr x |> iexpr

                        match exp.children[0] with
                        | :? LiteralSbyteContext ->  text.Replace("y", "")  |> System.SByte.Parse   |> lit2exp
                        | :? LiteralByteContext  ->  text.Replace("uy", "") |> System.Byte.Parse    |> lit2exp
                        | :? LiteralInt16Context ->  text.Replace("s", "")  |> System.Int16.Parse   |> lit2exp
                        | :? LiteralUint16Context -> text.Replace("us", "") |> System.UInt16.Parse  |> lit2exp
                        | :? LiteralInt32Context ->  text                   |> System.Int32.Parse   |> lit2exp
                        | :? LiteralUint32Context -> text.Replace("u", "")  |> System.UInt32.Parse  |> lit2exp
                        | :? LiteralInt64Context ->  text.Replace("L", "")  |> System.Int64.Parse   |> lit2exp
                        | :? LiteralUint64Context -> text.Replace("UL", "") |> System.UInt64.Parse  |> lit2exp
                        | :? LiteralSingleContext -> text.Replace("f", "")  |> System.Single.Parse  |> lit2exp
                        | :? LiteralDoubleContext -> text                   |> System.Double.Parse  |> lit2exp
                        | :? LiteralBoolContext ->   text                   |> System.Boolean.Parse |> lit2exp
                        | :? LiteralStringContext -> text                   |> deQuoteOnDemand      |> lit2exp
                        | :? LiteralCharContext ->
                            // text : "'a'" 의 형태
                            let dq, sq = "\"", "'"

                            text
                            |> unwrapString dq dq
                            |> unwrapString sq sq
                            |> System.Char.Parse
                            |> literal2expr
                            |> iexpr
                        | _ -> failwithlog "ERROR"

                    | :? TagContext -> failwithlog "Obsoleted.  Why not Storage???" // todo : remove
                    //iexpr <| tag (storages[text])

                    | :? StorageContext as sctx ->
                        let varCtx       = sctx.TryFindFirstChild<StorageNameContext>()
                        let UdtMemberCtx = sctx.TryFindFirstChild<UdtMemberContext>()
                        option {
                            let! name =
                                match varCtx, UdtMemberCtx with
                                | Some stg, None -> stg.GetText() |> Some
                                | None, Some udt -> udt.GetText() |> Some
                                | _ -> None
                            let! storage = storageFinder name
                            return storage.ToBoxedExpression() :?> IExpression
                        } |?? (fun () -> failwith $"Failed to find variable/tag name in {sctx.GetText()}")

                    | _ -> failwithlog "ERROR"

                | :? ArrayReferenceExprContext ->
                    debugfn $"ArrayReference: {text}"
                    failwithlog "Not yet"

                | :? ParenthesisExprContext as exp ->
                    debugfn $"Parenthesis: {text}"
                    let exp = exp.TryFindFirstChild<ExprContext>().Value
                    helper exp

                | _ -> failwithlog "Not yet"

            expr

        helper ctx

    let private getFirstChildExpressionContext (ctx: ParserRuleContext) : ExprContext =
        ctx.children.OfType<ExprContext>().First()

    let private (|UnitValue|_|) (x: IExpression) =
        match x.BoxedEvaluatedValue with
        | :? CountUnitType as value -> Some value
        | _ -> None

    let private (|BoolExp|) (x: IExpression) = x :?> IExpression<bool>

    let private parseCounterStatement (parserData:ParserData) (ctx: CounterDeclContext) : Statement =
        let typ =
            ctx.Descendants<CounterTypeContext>().First().GetText().ToUpper()
            |> DU.fromString<CounterType>

        let fail () =
            failwith $"Counter declaration error: {ctx.GetOriginalText()}"

        match typ with
        | Some typ ->
            let storages = parserData.Storages
            let exp = createExpression parserData (defaultStorageFinder storages) (getFirstChildExpressionContext ctx)
            let exp = exp :?> Expression<Counter>
            let name = ctx.Descendants<StorageNameContext>().First().GetText()

            parserData.TimerCounterInstances.Add(name) |> ignore

            // e.g "ctd myCTD = createXgiCTD(2000u, $cd, $xload);"
            // typ = CTD
            // exp = createXgiCTD(2000u, $cd, $xload)
            // name = myCTD
            match exp with
            | DuFunction { Name = functionName
                           Arguments = args } -> // functionName = "createCTU"
                let preset, rungInCondtion =
                    match args with
                    | (UnitValue preset) :: (BoolExp rungInCondtion) :: _ -> preset, rungInCondtion
                    | _ -> failwithlog "ERROR"

                let tcParams =
                    { Storages = storages
                      Name = name
                      Preset = preset
                      RungInCondition = rungInCondtion
                      FunctionName = functionName }

                (* args[0] 는 PV (Preset),
                   args[1] 이후부터 XGI 명령 입력 순서대로...
                *)
                let target = ParserUtil.runtimeTarget
                match typ, functionName, args with
                | CTU, ("createWinCTU" | "createXgiCTU" | "createXgkCTU"), _ :: _ :: [ (BoolExp resetCondition) ] ->
                    CounterStatement.CreateCTU(tcParams, resetCondition)  target
                | CTU, "createAbCTU", _ :: [ _ ] -> CounterStatement.CreateAbCTU(tcParams) target

                | CTD, ("createWinCTD" | "createXgiCTD" | "createXgkCTD"), _ :: _ :: [ (BoolExp resetCondition) ] ->
                    CounterStatement.CreateXgiCTD(tcParams, resetCondition) target
                | CTD, "createAbCTD", _ :: [ _ ] -> CounterStatement.CreateAbCTD(tcParams) target

                | CTUD,
                  ("createWinCTUD" | "createXgiCTUD" | "createXgkCTUD"),
                  _ :: _ :: (BoolExp countDownCondition) :: (BoolExp resetCondition) :: [ (BoolExp ldCondition) ] ->
                    CounterStatement.CreateCTUD(tcParams, countDownCondition, resetCondition, Some ldCondition) target

                | CTUD, "createAbCTUD", _ :: _ :: (BoolExp countDownCondition) :: [ (BoolExp resetCondition) ] ->
                    CounterStatement.CreateAbCTUD(tcParams, countDownCondition, resetCondition) target

                | CTR, ("createWinCTR" | "createXgiCTR" | "createXgkCTR"), _ :: _ :: [ (BoolExp resetCondition) ] ->
                    CounterStatement.CreateXgiCTR(tcParams, resetCondition) target
                | CTR, _, _ -> failwithlog "ERROR: CTR only supported for WINDOWS and XGI platform"

                | _ -> fail ()

            | _ -> fail ()
        | None -> fail ()

    let private parseTimerStatement (parserData:ParserData) (ctx: TimerDeclContext) : Statement =
        let typ =
            ctx.Descendants<TimerTypeContext>().First().GetText().ToUpper()
            |> DU.fromString<TimerType>

        let fail () =
            failwith $"Timer declaration error: {ctx.GetText()}"

        match typ with
        | Some typ ->
            let storages = parserData.Storages
            let exp = createExpression parserData (defaultStorageFinder storages) (getFirstChildExpressionContext ctx)
            let exp = exp :?> Expression<Timer>
            let name = ctx.Descendants<StorageNameContext>().First().GetText()
            // e.g "ton myTon = createXgiTON(2000u, $myQBit0);"
            // typ = TON
            // exp = createXgiTON(2000u, $myQBit0)
            // name = myTon
            parserData.TimerCounterInstances.Add(name) |> ignore

            match exp with
            | DuFunction { Name = functionName
                           Arguments = args } -> // functionName = "createTON"
                let preset, rungInCondtion =
                    match args with
                    | (UnitValue preset) :: (BoolExp rungInCondtion) :: _ -> preset, rungInCondtion
                    | _ -> failwithlog "ERROR"

                let tcParams =
                    {
                        Storages = storages
                        Name = name
                        Preset = preset
                        RungInCondition = rungInCondtion
                        FunctionName = functionName
                    }

                let target = ParserUtil.runtimeTarget
                match typ, functionName, args with
                | TON, ("createWinTON" | "createXgiTON" | "createXgkTON" | "createAbTON"), _ :: [ _ ] ->
                    TimerStatement.CreateTON tcParams  target
                | TOF, ("createWinTOF" | "createXgiTOF" | "createXgkTOF" | "createAbTOF"), _ :: [ _ ] ->
                    TimerStatement.CreateTOF tcParams  target
                | TMR, ("createAbRTO"), _ :: [ _ ] -> TimerStatement.CreateAbRTO tcParams  target
                | TMR, ("createXgiTMR" | "createXgkTMR" |"createWinTMR"), _ :: _ :: [ (BoolExp resetCondition) ] ->
                    TimerStatement.CreateTMR(tcParams, resetCondition) target
                | _ -> fail ()
            | _ -> fail ()
        | None -> fail ()


    let tryCreateStatement (parserData:ParserData) (ctx: StatementContext) : Statement option =
        let storages:Storages = parserData.Storages
        assert (ctx.ChildCount = 1 || (ctx.ChildCount = 2 && ctx.children[1].GetText() = ";"))
        let getStorageName = fun () -> ctx.Descendants<StorageNameContext>().First().GetText()

        let optStatement =
            let fstChild = ctx.children[0]
            match fstChild with
            | :? VarDeclContext as varDeclCtx ->
                let exp = createExpression parserData (defaultStorageFinder storages) (getFirstChildExpressionContext varDeclCtx)

                let declType =
                    ctx.Descendants<TypeContext>().First().GetText() |> System.Type.FromString

                let storageName = getStorageName()
                if storages.ContainsKey storageName then
                    failwith $"ERROR: Duplicated variable declaration {storageName}"

                match exp.FunctionName with
                | Some functionName when functionName = "createTag" ->
                    match exp.FunctionArguments with
                    | tagAddress :: [ tagValue ] ->
                        if tagValue.DataType <> declType then
                            failwith $"ERROR: Type mismatch in {varDeclCtx.GetOriginalText()}"

                        let addr = tagAddress.BoxedEvaluatedValue :?> string
                        let tag = declType.CreateBridgeTag(storageName, addr, tagValue.BoxedEvaluatedValue)
                        storages.Add(storageName, tag)
                    | _ -> failwith $"ERROR: Tag declaration error in {varDeclCtx.GetOriginalText()}"

                    None
                | _ ->
                    if exp.DataType <> declType then
                        failwith $"ERROR: Type mismatch in variable declaration {ctx.GetText()}"

                    let variable =
                        let comment = match ctx.GetText() with | "" -> None | _ as cmt -> Some cmt
                        declType.CreateVariable(storageName, exp.BoxedEvaluatedValue, comment)
                    storages.Add(storageName, variable)
                    Some <| DuVarDecl(exp, variable)

            | :? AssignContext as ctx ->
                let createExp ctx =
                    createExpression parserData (defaultStorageFinder storages) (getFirstChildExpressionContext ctx)
                let children = ctx.children.ToFSharpList()

                match ctx with
                | :? CtxStructMemberAssignContext ->
                    let storageName = ctx.Descendants<StructStorageNameContext>().First().GetText()
                    let isUdtMemberVariable = parserData.IsUdtMemberVariable storageName
                    let isTimerOrCounterMemberVariable = parserData.IsTimerOrCounterMemberVariable storageName
                    if parserData.TargetType = XGK && isUdtMemberVariable then
                        failwith $"ERROR: UDT declaration is not supported in XGK"

                    if not (isUdtMemberVariable || isTimerOrCounterMemberVariable) then
                        failwith $"ERROR: Failed to assign into non existing member variable {storageName}"
                    let exp = createExp (children[0] :?> StructMemberAssignContext)
                    let stgType = parserData.TryGetMemberVariableDataType storageName
                    let pseudoMemberVar =
                        match storages.TryFindValue storageName with
                        | Some v -> v
                        | None ->
                            let v = createMemberVariable storageName exp (Some (exp.ToText()))
                            storages[v.Name] <- v
                            v
                    if stgType.Value <> pseudoMemberVar.DataType then
                        failwith $"ERROR: Type mismatch in member variable assignment {ctx.GetText()}"
                    Some <| DuAssign(None, exp, pseudoMemberVar)
                | _ ->
                    let storageName = getStorageName()
                    if not <| storages.ContainsKey storageName then
                        failwith $"ERROR: Failed to assign into non existing storage {storageName}"

                    let storage = storages[storageName]

                    match children with
                    | [ (:? NormalAssignContext as ctx) ] -> Some <| DuAssign(None, createExp ctx, storage)
                    | _ -> failwithlog "ERROR"

            | :? CounterDeclContext as ctx -> Some <| parseCounterStatement parserData ctx

            | :? TimerDeclContext as ctx -> Some <| parseTimerStatement parserData ctx

            | :? CopyStatementContext as ctx ->
                let expr ctx = ctx |> getFirstChildExpressionContext |> createExpression parserData (defaultStorageFinder storages)
                let condition = ctx.Descendants<CopyConditionContext>().First() |> expr :?> IExpression<bool>

                let source = ctx.Descendants<CopySourceContext>().First() |> expr
                let target = ctx.Descendants<CopyTargetContext>().First().GetText()
                assert (target.StartsWith("$"))
                let target = storages[target.Replace("$", "")]

                Some <| DuAssign(Some condition, source, target)


            | :? CopyStructStatementContext as ctx ->
                // e.g copyStructIf(true, $hong, $people[0]);
                let expr ctx = ctx |> getFirstChildExpressionContext |> createExpression parserData (defaultStorageFinder storages)
                let condition = ctx.Descendants<CopyConditionContext>().First() |> expr :?> IExpression<bool>

                let sourceInstance = ctx.Descendants<UdtInstanceSourceContext>().First().udtInstance()  // e.g "hong"
                let targetInstance = ctx.Descendants<UdtInstanceTargetContext>().First().udtInstance()  // e.g "people[0]"
                let _sourceVar = sourceInstance.udtVar().GetText()   // e.g "hong"
                let _targetVar = targetInstance.udtVar().GetText()   // e.g "people"
                let source, target = sourceInstance.GetText(), targetInstance.GetText()
                let sourceType = parserData.TryGetUdtTypeName(source)   // e.g Some "Person"
                let targetType = parserData.TryGetUdtTypeName(target)   // e.g Some "Person"
                match sourceType, targetType with
                | Some s, Some t ->
                    if s <> t then failwith $"Type mismatch: {s} <> {t}"
                | _ -> failwith $"ERROR: Used undefined UDT type."

                let udtDecl = parserData.TryGetUdtDecl(sourceType.Value).Value
                Some <| DuAction(DuCopyUdt { Storages=parserData.Storages; UdtDecl=udtDecl; Condition=condition; Source=source; Target=target})

            | :? UdtDeclContext as ctx ->
                let typeName = ctx.udtType().GetText()
                let members =
                    ctx.Descendants<VarDeclContext>()
                        .Select(fun ctx -> {
                            Type = ctx.``type``().GetText() |> textToSystemType
                            Name = ctx.storageName().GetText() } )
                        .ToFSharpList()
                let udtDecl = { TypeName = typeName; Members = members }
                parserData.UdtDecls.Add udtDecl
                Some <| DuUdtDecl udtDecl

            | :? UdtDefContext as ctx ->
                let t = ctx.udtType().GetText()
                if not <| parserData.IsUdtType t then
                    failwith $"ERROR: UDT type {t} is not declared"

                let udtInstance = ctx.udtInstance()
                let v = udtInstance.udtVar().GetText()
                let n =
                    let arrDecl = udtInstance.arrayDecl()
                    if isNull arrDecl then
                        1
                    else
                        let arrText = arrDecl.children[0].GetText()
                        match Regex.Replace(arrText, @"\s+", "") with
                        | RegexPattern @"^\[(\d+)\]$" [ Int32Pattern arraySize ] -> arraySize
                        | _ -> failwithlog "ERROR: Invalid array declaration"
                let udtDefinition = { TypeName = t; VarName = v; ArraySize = n }
                parserData.AddUdtDefs(udtDefinition)
                Some <| DuUdtDef udtDefinition

            | _ when (fstChild :? LambdaDeclContext || fstChild :? ProcDeclContext) ->
                if not <| enableSubroutine then
                    failwith $"ERROR: Subroutine is not supported yet"

                let storageFinder (funName:string) (stgName:string): IStorage option =
                    let localStgName = getFormalParameterName funName stgName
                    storages.TryFindValue localStgName
                    |> Option.orElseWith (fun () -> defaultStorageFinder storages stgName)

                let funName =
                    match fstChild with
                    | :? LambdaDeclContext as ctx -> ctx.lambdaName().GetText()
                    | :? ProcDeclContext as ctx -> ctx.procName().GetText()
                    | _ -> failwith "ERROR"

                if predefinedFunctionNames.Contains(funName) then
                    failwith $"ERROR: {funName} is predefined function name"


                let args =  // (int a, int b)
                    [   for a in ctx.Descendants<ArgDeclContext>() do
                            let t = a.``type``().GetText() |> textToSystemType
                            let v = a.argName().GetText()
                            yield { Type = t; Name = v }
                    ]
                for a in args do
                    let encryptedFormalParamName = getFormalParameterName funName a.Name
                    let localVar =
                        let comment = $"{funName}({a.Type} {a.Name})"
                        let defaultValue = { Object = typeDefaultValue a.Type }: BoxedObjectHolder
                        createVariable encryptedFormalParamName defaultValue (Some comment)
                    storages.Add(encryptedFormalParamName, localVar)

                match fstChild with
                | :? LambdaDeclContext as ctx ->
                    // e.g: int sum(int a, int b) => $a + $b;
                    let typ = ctx.``type``().GetText() |> textToSystemType

                    let bodyExp = ctx.Descendants<LambdaBodyExprContext>().First() |> getFirstChildExpressionContext |> createExpression parserData (storageFinder funName)
                    let lambdaDecl = {
                        Prototype = { Type = typ; Name = funName }
                        Arguments = args
                        Body = bodyExp }
                    bodyExp.FunctionSpec.Value.LambdaDecl <- Some lambdaDecl

                    parserData.LambdaDefs.Add(lambdaDecl)
                    Some <| DuLambdaDecl lambdaDecl

                | :? ProcDeclContext as ctx ->
                    let bodies =
                        [   for stmtCtx in ctx.Descendants<StatementContext>() do
                                tryCreateStatement parserData stmtCtx ] |> List.choose id
                    let procDecl = {
                        ProcName = funName
                        Arguments = args
                        Bodies = ResizeArray(bodies)
                    }
                    parserData.ProcDefs.Add(procDecl)
                    Some <| DuProcDecl procDecl
                | _ -> failwith "ERROR"

            | :? ProcCallStatementContext as ctx ->
                if not <| enableSubroutine then
                    failwith $"ERROR: Subroutine is not supported yet"
                let expr ctx = ctx |> getFirstChildExpressionContext |> createExpression parserData (defaultStorageFinder storages)
                let call = ctx.funcCall()
                let funcName = call.functionName().GetText()
                let args = call.arguments()
                let args = if isNull args then [] else args.children.OfType<ExprContext>().ToFSharpList()
                let args = args |> map expr
                let decl = parserData.ProcDefs |> Seq.find(fun p -> p.ProcName = funcName)
                Some <| DuProcCall { ProcDecl = decl; ActuralPrameters = args }
            | _ -> failwithlog "ERROR: Not yet statement"

        optStatement.Iter(fun st -> st.Do())
        optStatement


    let parseCodeForTarget (storages: Storages) (text: string) (target:PlatformTarget): Statement list =
        try
            ParserUtil.runtimeTarget  <- target
            let parser = createParser (text)
            let parserData = new ParserData(target, storages, Some parser)

            let children = parser.toplevels().children

            let topLevels =
                [
                    for t in children do
                        match t with
                        | :? ToplevelContext as ctx -> ctx
                        | :? ITerminalNode as semicolon when semicolon.GetText() = ";" -> ()
                        | _ -> failwithlog "ERROR"
                ]


            [
                for t in topLevels do
                    let text = t.GetOriginalText()
                    tracefn $"Toplevel: {text}"
                    assert (t.ChildCount = 1)

                    match t.children[0] with
                    | :? StatementContext as stmt -> tryCreateStatement parserData stmt
                    | _ -> failwith $"ERROR: {text}: expect statements"
            ] |> List.choose id
        with exn ->
            failwith $"Failed to parse code: {text}\r\n{exn}"

    let parseCodeForWindows (storages: Storages) (text: string) : Statement list =
        parseCodeForTarget storages text WINDOWS

    type System.Type with

        member x.CreateVariable(name: string, boxedValue: obj, comment:string option) =
            createVariable name ({ Object = boxedValue }: BoxedObjectHolder) comment

        member x.CreateBridgeTag(name: string, address: string, boxedValue: obj) : ITag =
            let createParam () =
                {
                    defaultStorageCreationParams (unbox boxedValue) (VariableTag.PcUserVariable|>int) with
                        Name = name
                        Address = Some address
                }

            match x.Name with
            | BOOL    -> new Tag<bool>  (createParam ())
            | CHAR    -> new Tag<char>  (createParam ())
            | FLOAT32 -> new Tag<single>(createParam ())
            | FLOAT64 -> new Tag<double>(createParam ())
            | INT16   -> new Tag<int16> (createParam ())
            | INT32   -> new Tag<int32> (createParam ())
            | INT64   -> new Tag<int64> (createParam ())
            | INT8    -> new Tag<int8>  (createParam ())
            | STRING  -> new Tag<string>(createParam ())
            | UINT16  -> new Tag<uint16>(createParam ())
            | UINT32  -> new Tag<uint32>(createParam ())
            | UINT64  -> new Tag<uint64>(createParam ())
            | UINT8   -> new Tag<uint8> (createParam ())
            | _ -> failwithlog "ERROR"

        member x.CreateBridgeTag(name: string, address: string) : ITag =
            let v = typeDefaultValue x
            x.CreateBridgeTag(name, address, unbox v)

        static member FromString(typeName: string) : System.Type = (textToDataType typeName).ToType()
