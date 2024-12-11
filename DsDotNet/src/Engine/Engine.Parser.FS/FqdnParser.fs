namespace Engine.Parser.FS

open Antlr4.Runtime
open Dual.Common.Core.FS
open Dual.Common.Base.FS

module FqdnParserModule =
    let rTryParseFqdn (text: string) : Result<string[], string> =
        if text.IsNullOrEmpty() then
            Error "Empty name"
        else
            try
                let parser = createFqdnParser (text)
                let ctx = parser.fqdn ()
                let ncs = ctx.Descendants<fqdnParser.NameComponentContext>()
                Ok [| for nc in ncs -> nc.GetText() |]
            with
            | :? ParserError as err ->
                logError $"Failed to parse FQDN: '{text}'" // Just warning.  하나의 이름에 '.' 을 포함하는 경우.  e.g "#seg.testMe!!!"
                Error err.Message
            | exn ->
                Error $"ERROR: {exn}"


    let parseFqdn (text: string) = rTryParseFqdn(text).GetOkValue()
