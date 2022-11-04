namespace Engine.Parser.FS

open Antlr4.Runtime

open type DsParser

[<RequireQualifiedAccess>]
module Fqdn =

    let parse(text:string) =
        let createParser(text:string) =
            let inputStream = new AntlrInputStream(text)
            let lexer = fqdnLexer (inputStream)
            let tokenStream = CommonTokenStream(lexer)
            let parser = fqdnParser (tokenStream)

            let listener_lexer = new ErrorListener<int>(true)
            let listener_parser = new ErrorListener<IToken>(true)
            lexer.AddErrorListener(listener_lexer)
            parser.AddErrorListener(listener_parser)
            parser


        let parser = createParser (text)
        let ctx = parser.fqdn()
        let ncs = enumerateChildren<fqdnParser.NameComponentContext>(ctx)
        [ for nc in ncs -> nc.GetText() ]

