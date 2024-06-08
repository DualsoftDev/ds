namespace Engine.Parser.FS

open System.Linq

open Antlr4.Runtime
open Dual.Common.Core.FS
open Engine.Core
open type exprParser
open Antlr4.Runtime.Tree
open System.Text.RegularExpressions

[<AutoOpen>]
module rec ExpressionParser =
    let private createParser (text: string) =
        let inputStream = new AntlrInputStream(text)
        let lexer = exprLexer (inputStream)
        let tokenStream = CommonTokenStream(lexer)
        let parser = exprParser (tokenStream)

        let listener_lexer = new ErrorListener<int>(true)
        let listener_parser = new ErrorListener<IToken>(true)
        lexer.AddErrorListener(listener_lexer)
        parser.AddErrorListener(listener_parser)
        parser

    let createExpression (storages: Storages) (ctx: ExprContext) : IExpression =

        let rec helper (ctx: ExprContext) : IExpression =
            let text = ctx.GetText()

            let expr =
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

                    createCustomFunctionExpression funName args

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
                        | :? LiteralSbyteContext ->  text.Replace("y", "")  |> System.SByte.Parse |> lit2exp
                        | :? LiteralByteContext  ->  text.Replace("uy", "") |> System.Byte.Parse  |> lit2exp
                        | :? LiteralInt16Context ->  text.Replace("s", "")  |> System.Int16.Parse |> lit2exp
                        | :? LiteralUint16Context -> text.Replace("us", "") |> System.UInt16.Parse |> lit2exp
                        | :? LiteralInt32Context ->  text |> System.Int32.Parse |> lit2exp
                        | :? LiteralUint32Context -> text.Replace("u", "") |> System.UInt32.Parse |> lit2exp
                        | :? LiteralInt64Context ->  text.Replace("L", "") |> System.Int64.Parse |> lit2exp
                        | :? LiteralUint64Context -> text.Replace("UL", "") |> System.UInt64.Parse |> lit2exp
                        | :? LiteralSingleContext -> text.Replace("f", "") |> System.Single.Parse |> lit2exp
                        | :? LiteralDoubleContext -> text |> System.Double.Parse  |> lit2exp
                        | :? LiteralBoolContext ->   text |> System.Boolean.Parse |> lit2exp
                        | :? LiteralStringContext -> text |> deQuoteOnDemand      |> lit2exp
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
                        let storage =
                            option {
                                let! storageCtx = sctx.TryFindFirstChild<StorageNameContext>()
                                let name = storageCtx.GetText()
                                return! storages.TryFind(name)
                            }

                        match storage with
                        | Some strg -> strg.ToBoxedExpression() :?> IExpression
                        | None -> failwith $"Failed to find variable/tag name in {sctx.GetText()}"

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

    let parseExpression (storages: Storages) (text: string) : IExpression =
        try
            let parser = createParser (text)
            let ctx = parser.expr ()

            createExpression storages ctx
        with exn ->
            failwith $"Failed to parse Expression: {text}\r\n{exn}" // Just warning.  하나의 이름에 '.' 을 포함하는 경우.  e.g "#seg.testMe!!!"

                
    let private getFirstChildExpressionContext (ctx: ParserRuleContext) : ExprContext =
        ctx.children.OfType<ExprContext>().First()

    let private (|UnitValue|_|) (x: IExpression) =
        match x.BoxedEvaluatedValue with
        | :? CountUnitType as value -> Some value
        | _ -> None

    let private (|BoolExp|) (x: IExpression) = x :?> IExpression<bool>

    let private parseCounterStatement (storages: Storages) (ctx: CounterDeclContext) : Statement =
        let typ =
            ctx.Descendants<CounterTypeContext>().First().GetText().ToUpper()
            |> DU.fromString<CounterType>

        let fail () =
            failwith $"Counter declaration error: {ctx.GetOriginalText()}"

        match typ with
        | Some typ ->
            let exp = createExpression storages (getFirstChildExpressionContext ctx)
            let exp = exp :?> Expression<Counter>
            let name = ctx.Descendants<StorageNameContext>().First().GetText()

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

    let private parseTimerStatement (storages: Storages) (ctx: TimerDeclContext) : Statement =
        let typ =
            ctx.Descendants<TimerTypeContext>().First().GetText().ToUpper()
            |> DU.fromString<TimerType>

        let fail () =
            failwith $"Timer declaration error: {ctx.GetText()}"

        match typ with
        | Some typ ->
            let exp = createExpression storages (getFirstChildExpressionContext ctx)
            let exp = exp :?> Expression<Timer>
            let name = ctx.Descendants<StorageNameContext>().First().GetText()

            match exp with
            | DuFunction { Name = functionName
                           Arguments = args } -> // functionName = "createTON"
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


    let tryCreateStatement (storages: Storages) (ctx: StatementContext) : Statement option =
        assert (ctx.ChildCount = 1)
        let getStorageName = fun () -> ctx.Descendants<StorageNameContext>().First().GetText()

        let optStatement =
            match ctx.children[0] with
            | :? VarDeclContext as varDeclCtx ->
                let exp = createExpression storages (getFirstChildExpressionContext varDeclCtx)

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

            | :? AssignContext as assignCtx ->
                let storageName = getStorageName()
                if not <| storages.ContainsKey storageName then
                    failwith $"ERROR: Failed to assign into non existing storage {storageName}"

                let storage = storages[storageName]

                let createExp ctx =
                    createExpression storages (getFirstChildExpressionContext ctx)

                match assignCtx.children.ToFSharpList() with
                | [ (:? NormalAssignContext as ctx) ] -> Some <| DuAssign(None, createExp ctx, storage)
                | _ -> failwithlog "ERROR"

            | :? CounterDeclContext as counterDeclCtx -> Some <| parseCounterStatement storages counterDeclCtx

            | :? TimerDeclContext as timerDeclCtx -> Some <| parseTimerStatement storages timerDeclCtx

            | :? CopyStatementContext as copyStatementCtx ->
                let expr ctx =
                    ctx |> getFirstChildExpressionContext |> createExpression storages

                let condition =
                    copyStatementCtx.Descendants<CopyConditionContext>().First() |> expr :?> IExpression<bool>

                let source = copyStatementCtx.Descendants<CopySourceContext>().First() |> expr
                let target = copyStatementCtx.Descendants<CopyTargetContext>().First().GetText()
                assert (target.StartsWith("$"))
                let target = storages[target.Replace("$", "")]
                Some <| DuAction(DuCopy(condition, source, target))

            | :? UdtDeclContext as ctx ->
                let typeName = ctx.udtType().GetText()
                let members =
                    ctx.Descendants<VarDeclContext>()
                        .Select(fun ctx -> {
                            Type = ctx.``type``().GetText()
                            Name = ctx.storageName().GetText() } )
                        .ToFSharpList()
                Some <| DuUdtDecl(typeName, members)
            | :? UdtInstancesContext as ctx ->
                let t = ctx.udtType().GetText()
                let v = ctx.udtVar().GetText()
                let n =
                    let arrDecl = ctx.arrayDecl()
                    if isNull arrDecl then
                        1
                    else
                        let arrText = arrDecl.children[0].GetText()
                        match Regex.Replace(arrText, @"\s+", "") with
                        | RegexPattern @"^\[(\d+)\]$" [ Int32Pattern arraySize ] -> arraySize
                        | _ -> failwithlog "ERROR: Invalid array declaration"
                Some <| DuUdtInstances(t, v, n)
            | _ -> failwithlog "ERROR: Not yet statement"

        optStatement.Iter(fun st -> st.Do())
        optStatement

    let tryParseStatement (storages: Storages) (text: string) : Statement option =
        try
            let parser = createParser (text)
            let ctx = parser.statement ()

            tryCreateStatement storages ctx
        with exn ->
            failwith $"Failed to parse Statement: {text}\r\n{exn}"


    let parseCodeForTarget (storages: Storages) (text: string) (target:PlatformTarget): Statement list =
        try
            ParserUtil.runtimeTarget  <- target
            let parser = createParser (text)

            let children = parser.toplevels().children

            let topLevels =
                [   for t in children do
                        match t with
                        | :? ToplevelContext as ctx -> ctx
                        | :? ITerminalNode as semicolon when semicolon.GetText() = ";" -> ()
                        | _ -> failwithlog "ERROR" ]


            [   for t in topLevels do
                    let text = t.GetOriginalText()
                    //debugfn $"Toplevel: {text}"
                    assert (t.ChildCount = 1)

                    match t.children[0] with
                    | :? StatementContext as stmt -> tryCreateStatement storages stmt
                    | _ -> failwith $"ERROR: {text}: expect statements" ]
            |> List.choose id
        with exn ->
            failwith $"Failed to parse code: {text}\r\n{exn}"

    let parseCodeForWindows (storages: Storages) (text: string) : Statement list =
        parseCodeForTarget storages text WINDOWS

    type System.Type with

        member x.CreateVariable(name: string, boxedValue: obj, comment:string option) =
            createVariable name ({ Object = boxedValue }: BoxedObjectHolder) comment

        member x.CreateBridgeTag(name: string, address: string, boxedValue: obj) : ITag =
            let createParam () =
                {   defaultStorageCreationParams (unbox boxedValue) (VariableTag.PcUserVariable|>int) with
                        Name = name
                        Address = Some address }

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
