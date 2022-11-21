namespace rec Engine.Parser.FS

open System.Linq

open Antlr4.Runtime
open Antlr4.Runtime.Tree

open Engine.Common.FS
open Engine.Parser
open type Engine.Parser.dsParser
open type Engine.Parser.FS.DsParser
open Engine.Core




[<AutoOpen>]
module ParserUtilityModule =
    let parseFqdn(text:string) =
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
            let ncs = ctx.enumerateChildren<fqdnParser.NameComponentContext>()
            [ for nc in ncs -> nc.GetText().DeQuoteOnDemand() ]
        with
            | :? ParserException ->
                logWarn $"Failed to parse FQDN: {text}"
                [ text ]
            | exn ->
                failwith $"ERROR: {exn}"

    type IParseTree with
        member x.enumerateChildren<'T when 'T :> IParseTree >(
            ?includeMe:bool
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
            enumerateChildrenHelper(result, x, includeMe)
            result

        member x.enumerateParents<'T when 'T :> IParseTree >(
            ?includeMe:bool
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
            helper(x, includeMe)

        member x.tryFindFirstChild(predicate:ParseTreePredicate, ?includeMe:bool) =
            let includeMe = includeMe |? false
            x.enumerateChildren<IParseTree>(includeMe) |> Seq.tryFind(predicate)

        member x.tryFindFirstChild<'T when 'T :> IParseTree>(?includeMe:bool, ?predicate:ParseTreePredicate, ?exclude:ParseTreePredicate) : 'T option =   // :'T
            let includeMe = includeMe |? false
            let predicate = predicate |? truthyfy
            let predicate x = isType<'T> x && predicate x
            let exclude = exclude |? falsify
            x.enumerateChildren<'T>(includeMe, predicate, exclude) |> Seq.tryHead

        member x.tryFindFirstAncestor(predicate:ParseTreePredicate, ?includeMe:bool) = //:IParseTree option=
            let includeMe = includeMe |? false
            x.enumerateParents(includeMe) |> Seq.tryFind(predicate)


        member x.tryFindFirstAncestor<'T when 'T :> IParseTree>(?includeMe:bool) =
            let includeMe = includeMe |? false
            let pred = isType<'T>
            x.tryFindFirstAncestor(pred, includeMe) |> Option.map forceCast<'T>

        member x.tryFindIdentifier1FromContext(?exclude:ParseTreePredicate) =
            let exclude = exclude |? falsify
            option {
                let! ctx = x.tryFindFirstChild<Identifier1Context>(false, exclude=exclude)
                return ctx.GetText().DeQuoteOnDemand()
            }

        member x.tryFindNameComponentContext() : IParseTree option =
            let pred =
                fun (tree:IParseTree) ->
                    tree :? Identifier1Context
                    || tree :? Identifier2Context
                    || tree :? Identifier3Context
                    || tree :? Identifier4Context
            x.tryFindFirstChild(pred, true)

        member x.tryGetName():string option =
            option {
                let! idCtx = x.tryFindNameComponentContext()
                let name = idCtx.GetText().DeQuoteOnDemand()
                return name
            }

        member x.tryCollectNameComponents():string[] option = // :Fqdn
            option {
                let! idCtx = x.tryFindNameComponentContext()
                if idCtx :? Identifier1Context then
                    return [| idCtx.GetText().DeQuoteOnDemand() |]
                else
                    let! name = x.tryGetName()
                    return parseFqdn(name).ToArray()
            }

        member x.collectNameComponents():string[] = x.tryCollectNameComponents() |> Option.get

        member x.tryGetSystemName() =
            option {
                let! ctx = x.tryFindFirstAncestor<SystemContext>(true)
                let! names = ctx.tryCollectNameComponents()
                return names.Combine()
            }


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



