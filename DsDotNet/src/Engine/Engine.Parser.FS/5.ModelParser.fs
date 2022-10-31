namespace Engine.Parser.FS

open Antlr4.Runtime
open System
open Engine.Core.CodeElements
open Engine.Core.CoreModule

open Engine.Common.FS
open Engine.Parser
open System.Linq
open Engine.Core
open type Engine.Parser.dsParser
open Engine.Common.FS
open Engine.Parser
open System.Linq
open Engine.Core
open type Engine.Parser.dsParser
open type Engine.Parser.FS.DsParser
open Antlr4.Runtime.Tree
open Antlr4.Runtime
open Engine.Common.FS
open Engine.Common.FS.Functions
open Antlr4.Runtime.Tree

module ModelParser =
    let Walk(parser:dsParser, helper:ParserHelper) =
        let sListener = new SkeletonListener(parser, helper)
        ParseTreeWalker.Default.Walk(sListener, parser.model())
        tracefn("--- End of skeleton listener")

        let eleListener = new ElementListener(parser, helper)
        ParseTreeWalker.Default.Walk(eleListener, parser.model())
        tracefn("--- End of element listener")


        let edgeListener = new EdgeListener(parser, helper)
        ParseTreeWalker.Default.Walk(edgeListener, parser.model())
        tracefn("--- End of edge listener")

        let etcListener = new EtcListener(parser, helper)
        ParseTreeWalker.Default.Walk(etcListener, parser.model())
        tracefn("--- End of etc listener")

    let ParseFromString2(text:string, options:ParserOptions):ParserHelper =
        let (parser, errors) = DsParser.FromDocument(text)
        let helper = new ParserHelper(options)

        Walk(parser, helper)

        let model = helper.Model
        model.CreateMRIEdgesTransitiveClosure()
        model.Validate() |> ignore

        helper


    let ParseFromString(text:string, options:ParserOptions):Model = ParseFromString2(text, options).Model


    let ParsePartial(text:string, helper:ParserHelper, predExtract:(dsParser->RuleContext) option) =
        let predExtract = defaultArg predExtract (fun (parser:dsParser) -> parser.model())
        let (parser, ast, parserErrors) = FromDocument(text, predExtract)
        Walk(parser, helper)
