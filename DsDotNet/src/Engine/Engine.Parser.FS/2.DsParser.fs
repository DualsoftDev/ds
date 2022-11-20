namespace rec Engine.Parser.FS

open System.Linq

open Antlr4.Runtime
open Antlr4.Runtime.Tree

open Engine.Common.FS
open Engine.Common.FS
open Engine.Parser
open type Engine.Parser.dsParser
open type Engine.Parser.FS.DsParser
open Engine.Core

open System.Collections.Generic
open System.Diagnostics

open Engine.Core
open type Engine.Parser.dsParser
open Antlr4.Runtime.Tree
open Antlr4.Runtime

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

        try
            let parser = createParser (text)
            let ctx = parser.fqdn()
            let ncs = enumerateChildren<fqdnParser.NameComponentContext>(ctx)
            [ for nc in ncs -> nc.GetText().DeQuoteOnDemand() ]
        with
            | :? ParserException ->
                logWarn $"Failed to parse FQDN: {text}"
                [ text ]
            | exn ->
                failwith $"ERROR: {exn}"



type ParseTreePredicate = IParseTree->bool
type RuleExtractor = dsParser -> RuleContext

type DsParser() =
    static member val LoadedSystemName:string option = None with get, set
    static member ParseText (text:string, extractor:RuleExtractor, ?throwOnError) =
        let throwOnError = throwOnError |? true
        let inputStream = new AntlrInputStream(text)
        let lexer = new dsLexer(inputStream)
        let tokens = new CommonTokenStream(lexer)
        let parser = new dsParser(tokens)

        let listener_lexer = new ErrorListener<int>(throwOnError)
        let listener_parser = new ErrorListener<IToken>(throwOnError)
        lexer.AddErrorListener(listener_lexer)
        parser.AddErrorListener(listener_parser)
        let tree = extractor parser
        let errors = listener_lexer.Errors.Concat(listener_parser.Errors).ToArray()

        parser, tree, errors


    static member FromDocument(text:string, ?predExtract:RuleExtractor, ?throwOnError) =       // (dsParser, ParserError[])
        let throwOnError = throwOnError |? true
        let func = predExtract |? (fun (parser:dsParser) -> parser.system() :> RuleContext)
        //let (parser, tree, errors) = DsParser.FromDocument(text, func, throwOnError)
        let (parser, tree, errors) = ParseText(text, func, throwOnError)
        (parser, errors)


    static member enumerateChildren<'T when 'T :> IParseTree >(
        from:IParseTree
        , ?includeMe:bool
        , ?predicate:ParseTreePredicate
        , ?exclude:ParseTreePredicate
        ) : ResizeArray<'T> =         // ResizeArray<'T>

        let includeMe = includeMe |? false
        let predicate = predicate |? (isType<'T>)
        let exclude   = exclude |? (fun x -> false)
        let rec enumerateChildrenHelper(rslt:ResizeArray<'T>, frm:IParseTree, incMe:bool) =
            if not (exclude(frm)) then
                if (incMe && predicate(frm) && isType<'T> frm) then
                    rslt.Add(forceCast<'T>(frm))

                for index in [ 0 .. frm.ChildCount - 1 ] do
                    enumerateChildrenHelper(rslt, frm.GetChild(index), true)

        //Func<IParseTree, bool> pred = predicate ?? new Func<IParseTree, bool>(ctx => ctx is T)
        let result = ResizeArray<'T>()
        enumerateChildrenHelper(result, from, includeMe)
        result


    static member enumerateParents<'T when 'T :> IParseTree >(
        from:IParseTree      // IEnumerable<IParseTree>
        , ?includeMe:bool
        , ?predicate:ParseTreePredicate) =

        let includeMe = includeMe |? false
        let predicate = predicate |? (isType<'T>)
        let rec helper(from:IParseTree, includeMe:bool) =
            [
                if from <> null then
                    if (includeMe && predicate(from) && isType<'T> from) then
                        yield forceCast<'T>(from)

                    yield! helper(from.Parent, true)
            ]
        helper(from, includeMe)



    static member tryFindFirstChild(from:IParseTree, predicate:ParseTreePredicate, ?includeMe:bool) =
        let includeMe = includeMe |? false
        enumerateChildren<IParseTree>(from, includeMe) |> Seq.tryFind(predicate)

    static member tryFindFirstChild<'T when 'T :> IParseTree>(from:IParseTree, ?includeMe:bool, ?predicate:ParseTreePredicate, ?exclude:ParseTreePredicate) : 'T option =   // :'T
        let includeMe = includeMe |? false
        let predicate = predicate |? truthyfy
        let predicate x = isType<'T> x && predicate x
        let exclude = exclude |? falsify
        enumerateChildren<'T>(from, includeMe, predicate, exclude) |> Seq.tryHead

    static member tryFindFirstAncestor(from:IParseTree, predicate:ParseTreePredicate, ?includeMe:bool) = //:IParseTree option=
        let includeMe = includeMe |? false
        enumerateParents(from, includeMe) |> Seq.tryFind(predicate)


    static member tryFindFirstAncestor<'T when 'T :> IParseTree>(from:IParseTree, ?includeMe:bool) =
        let includeMe = includeMe |? false
        let pred = isType<'T>
        tryFindFirstAncestor(from, pred, includeMe) |> Option.map forceCast<'T>

    static member tryFindIdentifier1FromContext(context:IParseTree, ?exclude:ParseTreePredicate) =
        let exclude = exclude |? falsify
        option {
            let! ctx = tryFindFirstChild<Identifier1Context>(context, false, exclude=exclude)
            return ctx.GetText().DeQuoteOnDemand()
        }

    static member tryFindNameComponentContext(from:IParseTree) : IParseTree option =
        let pred =
            fun (tree:IParseTree) ->
                tree :? Identifier1Context
                || tree :? Identifier2Context
                || tree :? Identifier3Context
                || tree :? Identifier4Context
        DsParser.tryFindFirstChild(from, pred, true)

    static member tryGetName(from:IParseTree):string option =
        option {
            let! idCtx = tryFindNameComponentContext from
            let name = idCtx.GetText().DeQuoteOnDemand()
            return name
        }

    static member tryCollectNameComponents(from:IParseTree):string[] option = // :Fqdn
        option {
            let! idCtx = tryFindNameComponentContext from
            if idCtx :? Identifier1Context then
                return [| idCtx.GetText().DeQuoteOnDemand() |]
            else
                let! name = tryGetName from
                return Fqdn.parse(name).ToArray()
        }

    static member collectNameComponents(from:IParseTree):string[] = tryCollectNameComponents from |> Option.get

    static member tryGetSystemName(from:IParseTree) =
        option {
            let! ctx = tryFindFirstAncestor<SystemContext>(from, true)
            let! names = tryCollectNameComponents(ctx)
            return names.Combine()
        }


