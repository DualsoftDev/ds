namespace Engine.Parser.FS

open System.Linq
open System.Collections.Generic

open Antlr4.Runtime
open Engine.Common.FS
open Engine.Core
open type exprParser
open Antlr4.Runtime.Tree

[<AutoOpen>]
module ExpressionParser =
    type Storages = Dictionary<string, IStorage>

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
                        | :? LiteralSbyteContext  -> text.Replace("y" , "")  |> System.SByte.Parse  |> literal |> iexpr
                        | :? LiteralByteContext   -> text.Replace("uy", "")  |> System.Byte.Parse   |> literal |> iexpr
                        | :? LiteralInt16Context  -> text.Replace("s" , "")  |> System.Int16.Parse  |> literal |> iexpr
                        | :? LiteralUint16Context -> text.Replace("us", "")  |> System.UInt16.Parse |> literal |> iexpr
                        | :? LiteralInt32Context  -> text                    |> System.Int32.Parse  |> literal |> iexpr
                        | :? LiteralUint32Context -> text.Replace("u" , "")  |> System.UInt32.Parse |> literal |> iexpr
                        | :? LiteralInt64Context  -> text.Replace("L" , "")  |> System.Int64.Parse  |> literal |> iexpr
                        | :? LiteralUint64Context -> text.Replace("UL", "")  |> System.UInt64.Parse |> literal |> iexpr
                        | :? LiteralSingleContext -> text.Replace("f" , "")  |> System.Single.Parse |> literal |> iexpr
                        | :? LiteralDoubleContext -> text                    |> System.Double.Parse |> literal |> iexpr
                        | :? LiteralStringContext -> text                    |> deQuoteOnDemand     |> literal |> iexpr
                        | :? LiteralCharContext   -> text                    |> System.Char.Parse   |> literal |> iexpr
                        | :? LiteralBoolContext   -> text                    |> System.Boolean.Parse|> literal |> iexpr
                        | _ -> failwith "ERROR"
                    | :? TagContext as texp ->
                        failwith "Not yet"
                        //iexpr <| tag (storages[text])
                    | :? StorageContext as vexp ->
                        //var (varDic[text])
                        failwith "Not yet"
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

    let parseExpression(text:string) =
        try
            let parser = createParser (text)
            let ctx = parser.expr()

            createExpression null ctx
        with exn ->
            failwith $"Failed to parse Expression: {text}\r\n{exn}" // Just warning.  하나의 이름에 '.' 을 포함하는 경우.  e.g "#seg.testMe!!!"





    type System.Type with
        member x.CreateVariable(name:string) : IStorage =
            match x.Name with
            | "Single" -> Variable<single>(name, 0.0f)
            | "Double" -> Variable<double>(name, 0.0)
            | "SByte"  -> Variable<int8>(name, 0y)
            | "Byte"   -> Variable<uint8>(name, 0uy)
            | "Int16"  -> Variable<int16>(name, 0s)
            | "UInt16" -> Variable<uint16>(name, 0us)
            | "Int32"  -> Variable<int32>(name, 0)
            | "UInt32" -> Variable<uint32>(name, 0u)
            | "Int64"  -> Variable<int64>(name, 0L)
            | "UInt64" -> Variable<uint64>(name, 0UL)
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

    let createStatement (storages:Storages) (ctx:StatementContext) : Statement =
        assert(ctx.ChildCount = 1)
        let storageName = ctx.Descendants<StorageNameContext>().First().GetText()
        let getFirstChildExpressionContext (ctx:ParserRuleContext) : ExprContext = ctx.children.OfType<ExprContext>().First()

        let statement =
            match ctx.children[0] with
            | :? VarDeclContext as varDeclCtx ->
                let exp = createExpression storages (getFirstChildExpressionContext varDeclCtx)
                let typ = ctx.Descendants<TypeContext>().First().GetText() |> System.Type.FromString
                if exp.DataType <> typ then
                    failwith $"ERROR: Type mismatch in variable declaration {ctx.GetText()}"
                if storages.ContainsKey storageName then
                    failwith $"ERROR: Duplicated variable declaration {storageName}"

                let storage = exp.DataType.CreateVariable(storageName)
                storages.Add(storageName, storage)
                VarDecl (exp, storage)

            | :? AssignContext as assignCtx ->
                let exp = createExpression storages (getFirstChildExpressionContext assignCtx)
                if not <| storages.ContainsKey storageName then
                    failwith $"ERROR: Failed to assign into non existing storage {storageName}"

                let storage = storages[storageName]
                Assign (exp, storage)
            | _ ->
                failwith "ERROR: Not yet statement"

        statement.Do()
        statement

    let parseStatement(text:string) =
        try
            let parser = createParser (text)
            let ctx = parser.statement()

            createStatement null ctx
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
            ]
        with exn ->
            failwith $"Failed to parse code: {text}\r\n{exn}"
