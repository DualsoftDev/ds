namespace Engine.Parser.FS

open Antlr4.Runtime
open Engine.Common.FS
open Engine.Core
open System.Windows
open type exprParser

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

    //let createExpression(expr:ExprContext) : Expression<'T> =
    //    ()

    //let parseExpression(text:string) =
    //    try
    //        let parser = createParser (text)
    //        let ctx = parser.expr()
    //        let ncs = ctx.Descendants<exprParser.NameComponentContext>()
    //        [ for nc in ncs -> nc.GetText().DeQuoteOnDemand() ]
    //    with exn ->
    //        failwith $"Failed to parse Expression: {text}\r\n{exn}" // Just warning.  하나의 이름에 '.' 을 포함하는 경우.  e.g "#seg.testMe!!!"
    //let parseStatement(text:string) =
    //    try
    //        let parser = createParser (text)
    //        let ctx = parser.statement()
    //        let ncs = ctx.Descendants<exprParser.NameComponentContext>()
    //        [ for nc in ncs -> nc.GetText().DeQuoteOnDemand() ]
    //    with exn ->
    //        failwith $"Failed to parse Expression: {text}\r\n{exn}" // Just warning.  하나의 이름에 '.' 을 포함하는 경우.  e.g "#seg.testMe!!!"
