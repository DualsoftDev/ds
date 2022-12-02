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
        (ctx:ExprContext) : obj = // 실제로는 Expression<'T> =

        let rec helper(ctx:ExprContext) : obj =
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
                        box <| createBinaryExpression expL op expR
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
                        | :? LiteralDoubleContext as exp -> text |> System.Double.Parse |> expr |> box
                        | :? LiteralStringContext as exp -> text |> deQuoteOnDemand     |> expr |> box
                        | :? LiteralSbyteContext  as exp -> text |> System.SByte.Parse  |> expr |> box
                        | :? LiteralByteContext   as exp -> text |> System.Byte.Parse   |> expr |> box
                        | :? LiteralInt16Context  as exp -> text |> System.Int16.Parse  |> expr |> box
                        | :? LiteralUint16Context as exp -> text |> System.UInt16.Parse |> expr |> box
                        | :? LiteralInt32Context  as exp -> text |> System.Int32.Parse  |> expr |> box
                        | :? LiteralUint32Context as exp -> text.Replace("u", "") |> System.UInt32.Parse |> expr |> box
                        | :? LiteralInt64Context  as exp -> text |> System.Int64.Parse  |> expr |> box
                        | :? LiteralUint64Context as exp -> text |> System.UInt64.Parse |> expr |> box
                        | :? LiteralCharContext   as exp -> text |> System.Char.Parse   |> expr |> box

                        | _ -> failwith "ERROR"
                    | :? TagContext as texp ->
                        box <| tag (tagDic[text])
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
