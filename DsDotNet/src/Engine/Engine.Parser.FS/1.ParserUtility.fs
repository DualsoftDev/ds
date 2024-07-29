namespace rec Engine.Parser.FS

open System
open System.IO
open System.Linq

open Antlr4.Runtime
open Antlr4.Runtime.Tree
open Dual.Common.Core.FS
open Engine.Parser
open type Engine.Parser.dsParser
open Engine.Core

[<AutoOpen>]
module ParserUtilityModule =
    let collectNameComponents (parseTree: IParseTree) = parseTree.CollectNameComponents()

    type IParseTree with

        member x.Descendants<'T when 'T :> IParseTree>
          (
            ?includeMe: bool,
            ?predicate: ParseTreePredicate,
            ?exclude: ParseTreePredicate
          ) : ResizeArray<'T> =

            let includeMe = includeMe |? false
            let predicate = predicate |? (isType<'T>)
            let exclude = exclude |? (fun _ -> false)

            let rec helper (rslt: ResizeArray<'T>, frm: IParseTree, incMe: bool) =
                if not (exclude (frm)) then
                    if (incMe && predicate (frm)) then
                        rslt.Add(forceCast<'T> (frm))

                    for index in [ 0 .. frm.ChildCount - 1 ] do
                        helper (rslt, frm.GetChild(index), true)

            let result = ResizeArray<'T>()
            helper (result, x, includeMe)
            result

        member x.Ascendants<'T when 'T :> IParseTree>(?includeMe: bool, ?predicate: ParseTreePredicate) =

            let includeMe = includeMe |? false
            let predicate = predicate |? (isType<'T>)

            let rec helper (from: IParseTree, includeMe: bool) =
                [
                    if from <> null then
                        if (includeMe && predicate (from) && isType<'T> from) then
                            yield forceCast<'T> (from)

                        yield! helper (from.Parent, true)
                ]

            helper (x, includeMe)

        member x.TryFindFirstChild(predicate: ParseTreePredicate, ?includeMe: bool) =
            let includeMe = includeMe |? false
            x.Descendants<IParseTree>(includeMe) |> Seq.tryFind (predicate)

        member x.TryFindChildren<'T when 'T :> IParseTree>
          (
            ?includeMe: bool,
            ?predicate: ParseTreePredicate,
            ?exclude: ParseTreePredicate
          ) : 'T seq = // :'T

            let includeMe = includeMe |? false
            let predicate = predicate |? truthyfy
            let predicate x = isType<'T> x && predicate x
            let exclude = exclude |? falsify
            x.Descendants<'T>(includeMe, predicate, exclude) 

        member x.TryFindFirstChild<'T when 'T :> IParseTree>
          (
            ?includeMe: bool,
            ?predicate: ParseTreePredicate,
            ?exclude: ParseTreePredicate
          ) : 'T option = // :'T
            x.TryFindChildren(includeMe |? false, predicate |? truthyfy, exclude |? falsify) |> Seq.tryHead
          

        member x.TryFindFirstAscendant(predicate: ParseTreePredicate, ?includeMe: bool) = //:IParseTree option=
            let includeMe = includeMe |? false
            x.Ascendants(includeMe) |> Seq.tryFind (predicate)


        member x.TryFindFirstAscendant<'T when 'T :> IParseTree>(?includeMe: bool) =
            let includeMe = includeMe |? false
            let pred = isType<'T>
            x.TryFindFirstAscendant(pred, includeMe) |> Option.map forceCast<'T>

        member x.TryFindIdentifier1FromContext(?exclude: ParseTreePredicate) =
            let exclude = exclude |? falsify

            option {
                let! ctx = x.TryFindFirstChild<Identifier1Context>(false, exclude = exclude)
                return ctx.GetText()
            }

        member x.TryFindNameComponentContext() : IParseTree option =
            let pred =
                fun (tree: IParseTree) ->
                    tree :? Identifier1Context
                    || tree :? Identifier2Context
                    || tree :? Identifier3Context
                    || tree :? Identifier4Context
                    || tree :? Identifier5Context
                    || tree :? IdentifierCommandNameContext
                    || tree :? IdentifierOperatorNameContext

            x.TryFindFirstChild(pred, true)

        member x.TryGetName() : string option =
            option {
                let! idCtx = x.TryFindNameComponentContext()
                let name = idCtx.GetText()
                return name.DeQuoteOnDemand()
            }

        member x.TryCollectNameComponents() : string[] option = // :Fqdn
            option {
                let! idCtx = x.TryFindNameComponentContext()

                if  idCtx :? Identifier1Context then
                    return [| idCtx.GetText() |]
                else
                    let name = idCtx.GetText()
                    return fwdParseFqdn(name).ToArray()
            }

        member x.CollectNameComponents() : string[] =
            match x.TryCollectNameComponents() with
            | Some names -> names.Select(deQuoteOnDemand).ToArray()
            | None -> failWithLog "Failed to collect name components"

        member x.TryGetSystemName() =
            option {
                let! ctx = x.TryFindFirstAscendant<SystemContext>(true)
                let! names = ctx.TryCollectNameComponents()
                return names.Combine()
            }

    type ParserRuleContext with

        member x.GetRange() =
            let s = x.Start.StartIndex
            let e = x.Stop.StopIndex
            s, e

        member x.GetOriginalText() =
            // https://stackoverflow.com/questions/16343288/how-do-i-get-the-original-text-that-an-antlr4-rule-matched
            x.Start.InputStream.GetText(x.GetRange() |> Antlr4.Runtime.Misc.Interval)


type ParseTreePredicate = IParseTree -> bool
type RuleExtractor = dsParser -> RuleContext

type DsParser() =
    static member ParseText(text: string, extractor: RuleExtractor, ?throwOnError) =
        let throwOnError = throwOnError |? true
        let inputStream = new AntlrInputStream(text)
        let lexer = new dsLexer (inputStream)
        let tokens = new CommonTokenStream(lexer)
        let parser = new dsParser (tokens)

        let listener_lexer = new ErrorListener<int>(throwOnError)
        let listener_parser = new ErrorListener<IToken>(throwOnError)
        lexer.AddErrorListener(listener_lexer)
        parser.AddErrorListener(listener_parser)
        let tree = extractor parser
        let errors = listener_lexer.Errors.Concat(listener_parser.Errors).ToArray()

        parser, tree, errors


    static member FromDocument(text: string, ?predExtract: RuleExtractor, ?throwOnError) = // (dsParser, ParserError[])
        let throwOnError = throwOnError |? true

        let func =
            predExtract |? (fun (parser: dsParser) -> parser.system () :> RuleContext)

        let (parser, _tree, errors) = DsParser.ParseText(text, func, throwOnError)
        (parser, errors)
