namespace Engine.Parser.FS

open Antlr4.Runtime
open Dual.Common.Core.FS
open Engine.Core

module FqdnParserModule =
    let parseFqdn (text: string) =
        let createParser (text: string) : fqdnParser =
            let inputStream = new AntlrInputStream(text)
            let lexer = fqdnLexer (inputStream)
            let tokenStream = CommonTokenStream(lexer)
            let parser = fqdnParser (tokenStream)

            let listener_lexer = new ErrorListener<int>(true)
            let listener_parser = new ErrorListener<IToken>(true)
            lexer.AddErrorListener(listener_lexer)
            parser.AddErrorListener(listener_parser)
            parser

        try
            let parser = createParser (text)
            let ctx = parser.fqdn ()
            let ncs = ctx.Descendants<fqdnParser.NameComponentContext>()
            [ for nc in ncs -> nc.GetText() ]
        with
        | :? ParserException ->
            logWarn $"Failed to parse FQDN: {text}" // Just warning.  하나의 이름에 '.' 을 포함하는 경우.  e.g "#seg.testMe!!!"
            [ text ]
        | exn -> failwith $"ERROR: {exn}"
