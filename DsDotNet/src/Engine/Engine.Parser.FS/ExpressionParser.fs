namespace Engine.Parser.FS

open System.Linq
open System.Collections.Generic

open Antlr4.Runtime
open Engine.Common.FS
open Engine.Core
open type exprParser

[<AutoOpen>]
module ExpressionParser =
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
        (tagDic:Dictionary<string, Tag<_>>)
        (varDic:Dictionary<string, StorageVariable<_>>)
        (ctx:ExprContext) : IExpression =

        let rec helper(ctx:ExprContext) : IExpression =
            let text = ctx.GetText()
            let expr =
                match ctx with
                | :? FunctionCallExprContext as exp ->
                    tracefn $"FunctionCall: {text}"
                    let funName = exp.TryFindFirstChild<FunctionNameContext>().Value.GetText()
                    let args =
                        [
                            match exp.TryFindFirstChild<ExprListContext>() with
                            | Some exprListCtx ->
                                for exprCtx in exprListCtx.children do
                                    helper (exprCtx :?> ExprContext)
                            | None ->
                                ()
                        ]
                    createCustomFunctionExpression funName args

                | :? BinaryExprContext as exp ->
                    tracefn $"Binary: {text}"
                    match exp.children.ToFSharpList() with
                    | left::op::right::[] ->
                        let expL = helper(left :?> ExprContext)
                        let expR = helper(right :?> ExprContext)
                        let op = op.GetText()
                        createBinaryExpression expL op expR
                    | _ ->
                        failwith "ERROR"

                | :? UnaryExprContext as exp ->
                    tracefn $"Unary: {text}"
                    failwith "Not yet"

                | :? TerminalExprContext as terminalExp ->
                    tracefn $"Terminal: {text}"
                    assert(terminalExp.ChildCount = 1)
                    let terminal = terminalExp.children[0].GetChild(0)
                    match terminal with
                    | :? LiteralContext as exp ->
                        assert(exp.ChildCount = 1)
                        match exp.children[0] with
                        | :? LiteralStringContext as exp -> text |> deQuoteOnDemand|> literal |> iexpr
                        | :? LiteralDoubleContext as exp -> text                   |> System.Double.Parse |> literal |> iexpr
                        | :? LiteralSingleContext as exp -> text.Replace("f", "")  |> System.Single.Parse |> literal |> iexpr
                        | :? LiteralSbyteContext  as exp -> text.Replace("y", "")  |> System.SByte.Parse  |> literal |> iexpr
                        | :? LiteralByteContext   as exp -> text.Replace("uy", "") |> System.Byte.Parse   |> literal |> iexpr
                        | :? LiteralInt16Context  as exp -> text.Replace("s", "")  |> System.Int16.Parse  |> literal |> iexpr
                        | :? LiteralUint16Context as exp -> text.Replace("us", "") |> System.UInt16.Parse |> literal |> iexpr
                        | :? LiteralInt32Context  as exp -> text                   |> System.Int32.Parse  |> literal |> iexpr
                        | :? LiteralUint32Context as exp -> text.Replace("u", "")  |> System.UInt32.Parse |> literal |> iexpr
                        | :? LiteralInt64Context  as exp -> text.Replace("L", "")  |> System.Int64.Parse  |> literal |> iexpr
                        | :? LiteralUint64Context as exp -> text.Replace("UL", "") |> System.UInt64.Parse |> literal |> iexpr
                        | :? LiteralCharContext   as exp -> text                   |> System.Char.Parse   |> literal |> iexpr

                        | _ -> failwith "ERROR"
                    | :? TagContext as texp ->
                        iexpr <| tag (tagDic[text])
                    | :? VariableContext as vexp ->
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

            createExpression null null ctx
        with exn ->
            failwith $"Failed to parse Expression: {text}\r\n{exn}" // Just warning.  하나의 이름에 '.' 을 포함하는 경우.  e.g "#seg.testMe!!!"
    //let parseStatement(text:string) =
    //    try
    //        let parser = createParser (text)
    //        let ctx = parser.statement()
    //        let ncs = ctx.Descendants<exprParser.NameComponentContext>()
    //        [ for nc in ncs -> nc.GetText().DeQuoteOnDemand() ]
    //    with exn ->
    //        failwith $"Failed to parse Expression: {text}\r\n{exn}" // Just warning.  하나의 이름에 '.' 을 포함하는 경우.  e.g "#seg.testMe!!!"
