namespace Engine.Parser.FS

open System.Linq
open System.Collections.Generic

open Antlr4.Runtime
open Engine.Common.FS
open Engine.Core
open type exprParser
open Antlr4.Runtime.Tree

[<AutoOpen>]
module rec ExpressionParser =
    let private createParser(text:string) =
        let inputStream = new AntlrInputStream(text)
        let lexer = exprLexer (inputStream)
        let tokenStream = CommonTokenStream(lexer)
        let parser = exprParser (tokenStream)

        let listener_lexer = new ErrorListener<int>(true)
        let listener_parser = new ErrorListener<IToken>(true)
        lexer.AddErrorListener(listener_lexer)
        parser.AddErrorListener(listener_parser)
        parser

    let createExpression
        (storages:Storages)
        (ctx:ExprContext) : IExpression =

        let rec helper(ctx:ExprContext) : IExpression =
            let text = ctx.GetText()
            let expr =
                match ctx with
                | :? FunctionCallExprContext as exp ->  // functionName '(' arguments? ')'
                    tracefn $"FunctionCall: {text}"
                    let funName = exp.TryFindFirstChild<FunctionNameContext>().Value.GetText()
                    let args =
                        [
                            match exp.TryFindFirstChild<ExprListContext>() with
                            | Some exprListCtx ->
                                for exprCtx in exprListCtx.children.OfType<ExprContext>() do
                                    helper exprCtx
                            | None ->
                                ()
                        ]
                    createCustomFunctionExpression funName args

                | :? CastingExprContext as exp ->   // '(' type ')' expr
                    tracefn $"Casting: {text}"
                    let castName = exp.TryFindFirstChild<TypeContext>().Value.GetText()
                    let exprCtx = exp.TryFindFirstChild<ExprContext>().Value
                    let expr = helper exprCtx
                    createCustomFunctionExpression castName [expr]

                |(  :? BinaryExprMultiplicativeContext
                  | :? BinaryExprAdditiveContext
                  | :? BinaryExprBitwiseShiftContext
                  | :? BinaryExprRelationalContext
                  | :? BinaryExprEqualityContext
                  | :? BinaryExprBitwiseAndContext
                  | :? BinaryExprBitwiseXorContext
                  | :? BinaryExprBitwiseOrContext
                  | :? BinaryExprLogicalAndContext
                  | :? BinaryExprLogicalOrContext) ->
                    tracefn $"Binary: {text}"
                    match ctx.children.ToFSharpList() with
                    | left::op::right::[] ->
                        let expL = helper(left :?> ExprContext)
                        let expR = helper(right :?> ExprContext)
                        let op = op.GetText()
                        createBinaryExpression expL op expR
                    | _ ->
                        failwith "ERROR"


                | :? UnaryExprContext as exp ->
                    tracefn $"Unary: {text}"
                    match exp.children.ToFSharpList() with
                    | op::opnd::[] ->
                        let exp = helper(opnd :?> ExprContext)
                        let op = op.GetText()
                        createUnaryExpression op exp
                    | _ ->
                        failwith "ERROR"

                | :? TerminalExprContext as terminalExp ->
                    tracefn $"Terminal: {text}"
                    assert(terminalExp.ChildCount = 1)
                    let terminal = terminalExp.children[0].GetChild(0)
                    match terminal with
                    | :? LiteralContext as exp ->
                        assert(exp.ChildCount = 1)
                        match exp.children[0] with
                        | :? LiteralSbyteContext  -> text.Replace("y" , "")  |> System.SByte.Parse  |> literal2expr |> iexpr
                        | :? LiteralByteContext   -> text.Replace("uy", "")  |> System.Byte.Parse   |> literal2expr |> iexpr
                        | :? LiteralInt16Context  -> text.Replace("s" , "")  |> System.Int16.Parse  |> literal2expr |> iexpr
                        | :? LiteralUint16Context -> text.Replace("us", "")  |> System.UInt16.Parse |> literal2expr |> iexpr
                        | :? LiteralInt32Context  -> text                    |> System.Int32.Parse  |> literal2expr |> iexpr
                        | :? LiteralUint32Context -> text.Replace("u" , "")  |> System.UInt32.Parse |> literal2expr |> iexpr
                        | :? LiteralInt64Context  -> text.Replace("L" , "")  |> System.Int64.Parse  |> literal2expr |> iexpr
                        | :? LiteralUint64Context -> text.Replace("UL", "")  |> System.UInt64.Parse |> literal2expr |> iexpr
                        | :? LiteralSingleContext -> text.Replace("f" , "")  |> System.Single.Parse |> literal2expr |> iexpr
                        | :? LiteralDoubleContext -> text                    |> System.Double.Parse |> literal2expr |> iexpr
                        | :? LiteralStringContext -> text                    |> deQuoteOnDemand     |> literal2expr |> iexpr
                        | :? LiteralCharContext   -> text                    |> System.Char.Parse   |> literal2expr |> iexpr
                        | :? LiteralBoolContext   -> text                    |> System.Boolean.Parse|> literal2expr |> iexpr
                        | _ -> failwith "ERROR"
                    | :? TagContext as texp ->
                        failwith "Obsoleted.  Why not Storage???"   // todo : remove
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
                    | _ ->
                        failwith "ERROR"

                | :? ArrayReferenceExprContext as exp ->
                    tracefn $"ArrayReference: {text}"
                    failwith "Not yet"

                | :? ParenthesysExprContext as exp ->
                    tracefn $"Parenthesys: {text}"
                    let exp = exp.TryFindFirstChild<ExprContext>().Value
                    helper exp

                | _ ->
                    failwith "Not yet"

            expr

        helper ctx

    let parseExpression (storages:Storages) (text:string) =
        try
            let parser = createParser (text)
            let ctx = parser.expr()

            createExpression storages ctx
        with exn ->
            failwith $"Failed to parse Expression: {text}\r\n{exn}" // Just warning.  하나의 이름에 '.' 을 포함하는 경우.  e.g "#seg.testMe!!!"


    let private getFirstChildExpressionContext (ctx:ParserRuleContext) : ExprContext = ctx.children.OfType<ExprContext>().First()

    let private (|UnitValue|) (x:IExpression) = x.BoxedEvaluatedValue :?> CountUnitType
    let private (|BoolExp|) (x:IExpression) = x :?> IExpression<bool>

    let private parseCounterStatement (storages:Storages) (ctx:CounterDeclContext) : Statement =
        let typ = ctx.Descendants<CounterTypeContext>().First().GetText().ToUpper() |> DU.fromString<CounterType>
        let fail() = failwith $"Counter declaration error: {ctx.GetOriginalText()}"
        match typ with
        | Some typ ->
            let exp = createExpression storages (getFirstChildExpressionContext ctx)
            let exp = exp :?> Expression<Counter>
            let name = ctx.Descendants<StorageNameContext>().First().GetText()
            match exp  with
            | DuFunction { Name=functionName; Arguments=args } ->     // functionName = "createCTU"
                match typ, functionName, args with
                | CTU, "createCTU", (UnitValue preset)::(BoolExp rungInCondtion)::[] ->
                    CounterStatement.CreateCTU(storages, name, preset, rungInCondtion)
                | CTU, "createCTU", (UnitValue preset)::(BoolExp rungInCondtion)::(BoolExp resetCondition)::[] ->
                    CounterStatement.CreateCTU(storages, name, preset, rungInCondtion, resetCondition)
                | CTD, "createCTD", (UnitValue preset)::(BoolExp rungInCondtion)::(UnitValue accum)::[] ->
                    CounterStatement.CreateCTD(storages, name, preset, rungInCondtion, accum)
                | CTD, "createCTD", (UnitValue preset)::(BoolExp rungInCondtion)::(BoolExp resetCondition)::(UnitValue accum)::[] ->
                    CounterStatement.CreateCTD(storages, name, preset, rungInCondtion, resetCondition, accum)

                | CTUD, "createCTUD", (UnitValue preset)::(BoolExp countUpCondition)::(BoolExp countDownCondition)::(UnitValue accum)::[] ->
                    CounterStatement.CreateCTUD(storages, name, preset, countUpCondition, countDownCondition, accum)
                | CTUD, "createCTUD", (UnitValue preset)::(BoolExp countUpCondition)::(BoolExp countDownCondition)::(BoolExp resetCondition)::(UnitValue accum)::[] ->
                    CounterStatement.CreateCTUD(storages, name, preset, countUpCondition, countDownCondition, resetCondition, accum)
                | _ -> fail()
            | _ -> fail()
        | None -> fail()

    let private parseTimerStatement (storages:Storages) (ctx:TimerDeclContext) : Statement =
        let typ = ctx.Descendants<TimerTypeContext>().First().GetText().ToUpper() |> DU.fromString<TimerType>
        let fail() = failwith $"Timer declaration error: {ctx.GetText()}"
        match typ with
        | Some typ ->
            let exp = createExpression storages (getFirstChildExpressionContext ctx)
            let exp = exp :?> Expression<Timer>
            let name = ctx.Descendants<StorageNameContext>().First().GetText()
            match exp  with
            | DuFunction { Name=functionName; Arguments=args } ->     // functionName = "createTON"
                match typ, functionName, args with
                | TON, "createTON", (UnitValue preset)::(BoolExp rungInCondtion)::[] ->
                    TimerStatement.CreateTON(storages, name, preset, rungInCondtion)
                | TON, "createTON", (UnitValue preset)::(BoolExp rungInCondtion)::(BoolExp resetCondition)::[] ->
                    TimerStatement.CreateTON(storages, name, preset, rungInCondtion, resetCondition)
                | TOF, "createTOF", (UnitValue preset)::(BoolExp rungInCondtion)::[] ->
                    TimerStatement.CreateTOF(storages, name, preset, rungInCondtion)
                | TOF, "createTOF", (UnitValue preset)::(BoolExp rungInCondtion)::(BoolExp resetCondition)::[] ->
                    TimerStatement.CreateTOF(storages, name, preset, rungInCondtion, resetCondition)

                | RTO, "createRTO", (UnitValue preset)::(BoolExp rungInCondtion)::[] ->
                    TimerStatement.CreateRTO(storages, name, preset, rungInCondtion)
                | RTO, "createRTO", (UnitValue preset)::(BoolExp rungInCondtion)::(BoolExp resetCondition)::[] ->
                    TimerStatement.CreateRTO(storages, name, preset, rungInCondtion, resetCondition)
                | _ -> fail()
            | _ -> fail()
        | None -> fail()


    let createStatement (storages:Storages) (ctx:StatementContext) : Statement option =
        assert(ctx.ChildCount = 1)
        let storageName = ctx.Descendants<StorageNameContext>().First().GetText()

        let statement =
            match ctx.children[0] with
            | :? VarDeclContext as varDeclCtx ->
                let exp = createExpression storages (getFirstChildExpressionContext varDeclCtx)
                let declType = ctx.Descendants<TypeContext>().First().GetText() |> System.Type.FromString
                if storages.ContainsKey storageName then
                    failwith $"ERROR: Duplicated variable declaration {storageName}"

                match exp.FunctionName with
                | Some functionName when functionName = "createTag" ->
                    match exp.FunctionArguments with
                    | tagAddress::tagValue::[] ->
                        if tagValue.DataType <> declType then failwith $"ERROR: Type mismatch in {varDeclCtx.GetOriginalText()}"
                        let addr = tagAddress.BoxedEvaluatedValue :?> string
                        let tag = declType.CreateTag(storageName, addr, tagValue.BoxedEvaluatedValue)
                        storages.Add(storageName, tag)
                    | _ -> failwith $"ERROR: Tag declaration error in {varDeclCtx.GetOriginalText()}"
                    None
                | _ ->
                    if exp.DataType <> declType then
                        failwith $"ERROR: Type mismatch in variable declaration {ctx.GetText()}"
                    let variable = declType.CreateVariable(storageName)
                    storages.Add(storageName, variable)
                    Some <| DuVarDecl (exp, variable)

            | :? AssignContext as assignCtx ->
                let exp = createExpression storages (getFirstChildExpressionContext assignCtx)
                if not <| storages.ContainsKey storageName then
                    failwith $"ERROR: Failed to assign into non existing storage {storageName}"

                let storage = storages[storageName]
                Some <| DuAssign (exp, storage)
            | :? CounterDeclContext as counterDeclCtx -> Some <| parseCounterStatement storages counterDeclCtx
            | :? TimerDeclContext as timerDeclCtx -> Some <| parseTimerStatement storages timerDeclCtx
            | _ ->
                failwith "ERROR: Not yet statement"

        statement.Iter(fun st -> st.Do())
        statement

    let parseStatement (storages:Storages) (text:string) =
        try
            let parser = createParser (text)
            let ctx = parser.statement()

            createStatement storages ctx
        with exn ->
            failwith $"Failed to parse Statement: {text}\r\n{exn}"


    let parseCode (storages:Storages) (text:string) : Statement list =
        try
            let parser = createParser (text)
            let topLevels =
                [
                    for t in parser.toplevels().children do
                        match t with
                        | :? ToplevelContext as ctx -> ctx
                        | :? ITerminalNode as semicolon when semicolon.GetText() = ";" -> ()
                        | _ -> failwith "ERROR"
                ]

            [
                for t in topLevels do
                    let text = t.GetText()
                    tracefn $"Toplevel: {text}"
                    assert(t.ChildCount = 1)

                    match t.children[0] with
                    | :? StatementContext as stmt -> createStatement storages stmt
                    | _ ->
                        failwith $"ERROR: {text}: expect statements"
            ] |> List.choose id
        with exn ->
            failwith $"Failed to parse code: {text}\r\n{exn}"


    type System.Type with
        member x.CreateVariable(name:string, boxedValue:obj) : IStorage =
            let v = boxedValue
            match x.Name with
            | "Single" -> new Variable<single>(name, v :?> single)
            | "Double" -> new Variable<double>(name, v :?> double)
            | "SByte"  -> new Variable<int8>  (name, v :?> int8)
            | "Byte"   -> new Variable<uint8> (name, v :?> uint8)
            | "Int16"  -> new Variable<int16> (name, v :?> int16)
            | "UInt16" -> new Variable<uint16>(name, v :?> uint16)
            | "Int32"  -> new Variable<int32> (name, v :?> int32)
            | "UInt32" -> new Variable<uint32>(name, v :?> uint32)
            | "Int64"  -> new Variable<int64> (name, v :?> int64)
            | "UInt64" -> new Variable<uint64>(name, v :?> uint64)
            | _  -> failwith "ERROR"

        member x.CreateVariable(name:string) : IStorage =
            match x.Name with
            | "Single" -> new Variable<single>(name, 0.0f)
            | "Double" -> new Variable<double>(name, 0.0)
            | "SByte"  -> new Variable<int8>  (name, 0y)
            | "Byte"   -> new Variable<uint8> (name, 0uy)
            | "Int16"  -> new Variable<int16> (name, 0s)
            | "UInt16" -> new Variable<uint16>(name, 0us)
            | "Int32"  -> new Variable<int32> (name, 0)
            | "UInt32" -> new Variable<uint32>(name, 0u)
            | "Int64"  -> new Variable<int64> (name, 0L)
            | "UInt64" -> new Variable<uint64>(name, 0UL)
            | _  -> failwith "ERROR"

        member x.CreateTag(name:string, address:string, boxedValue:obj) : IStorage =
            let v = boxedValue
            match x.Name with
            | "Single" -> new PlcTag<single>(name, address, v :?> single)
            | "Double" -> new PlcTag<double>(name, address, v :?> double)
            | "SByte"  -> new PlcTag<int8>  (name, address, v :?> int8)
            | "Byte"   -> new PlcTag<uint8> (name, address, v :?> uint8)
            | "Int16"  -> new PlcTag<int16> (name, address, v :?> int16)
            | "UInt16" -> new PlcTag<uint16>(name, address, v :?> uint16)
            | "Int32"  -> new PlcTag<int32> (name, address, v :?> int32)
            | "UInt32" -> new PlcTag<uint32>(name, address, v :?> uint32)
            | "Int64"  -> new PlcTag<int64> (name, address, v :?> int64)
            | "UInt64" -> new PlcTag<uint64>(name, address, v :?> uint64)
            | _  -> failwith "ERROR"

        member x.CreateTag(name:string, address:string) : IStorage =
            match x.Name with
            | "Single" -> new PlcTag<single>(name, address, 0.0f)
            | "Double" -> new PlcTag<double>(name, address, 0.0)
            | "SByte"  -> new PlcTag<int8>  (name, address, 0y)
            | "Byte"   -> new PlcTag<uint8> (name, address, 0uy)
            | "Int16"  -> new PlcTag<int16> (name, address, 0s)
            | "UInt16" -> new PlcTag<uint16>(name, address, 0us)
            | "Int32"  -> new PlcTag<int32> (name, address, 0)
            | "UInt32" -> new PlcTag<uint32>(name, address, 0u)
            | "Int64"  -> new PlcTag<int64> (name, address, 0L)
            | "UInt64" -> new PlcTag<uint64>(name, address, 0UL)
            | _  -> failwith "ERROR"


        static member FromString(typeName:string) : System.Type =
            match typeName.ToLower() with
            | ("float32" | "single") -> typedefof<single>
            | ("float64" | "double") -> typedefof<double>
            | ("int8"    | "sbyte")  -> typedefof<int8>
            | ("uint8"   | "byte")   -> typedefof<uint8>
            | ("int16"   | "short")  -> typedefof<int16>
            | ("uint16"  | "ushort") -> typedefof<uint16>
            | ("int32"   | "int" )   -> typedefof<int32>
            | ("uint32"  | "uint")   -> typedefof<uint32>
            | ("int64"   | "long")   -> typedefof<int64>
            | ("uint64"  | "ulong")  -> typedefof<uint64>
            | _  -> failwith "ERROR"
