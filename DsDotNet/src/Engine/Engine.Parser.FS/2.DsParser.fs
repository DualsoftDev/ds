namespace rec Engine.Parser.FS

open System.Linq

open Antlr4.Runtime
open Antlr4.Runtime.Tree

open Engine.Common.FS
open Engine.Parser
open type Engine.Parser.dsParser
open type Engine.Parser.FS.DsParser
open Engine.Core

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
        let throwOnError = defaultArg throwOnError true
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


    ///// <summary>
    ///// 주어진 text 내에서 [sys] B = @copy_system(A) 와 같이 copy_system 으로 정의된 영역을
    ///// 치환한 text 를 반환한다.  system A 정의 영역을 찾아서 system B 로 치환한 text 반환
    ///// 이때, copy 구문은 삭제한다.
    ///// </summary>
    //static member private ExpandSystemCopy(text:string):string =
    //    let func = fun (parser:dsParser) -> parser.system() :> RuleContext
    //    let (parser, _, _) = DsParser.ParseText(text, func)
    //    parser.Reset()
    //    let model = parser.system()
    //    let sysCtxMap =
    //        DsParser.enumerateChildren<SystemContext>(model)
    //            .Select(fun ctx ->
    //                let sysName = DsParser.tryFindFirstChild<SystemNameContext>(ctx) |> Option.get |> fun x -> x.GetText()
    //                sysName, ctx)
    //            |> dict |> Dictionary

    //    let copySysCtxs =
    //        sysCtxMap.Where(fun (KeyValue(sysName, sysCtxt)) -> sysCtxt.children.Any(isType<LoadDeviceContext>)).ToArray()

    //    // 원본 full text 에서 copy_system 구문을 치환한??? 삭제한 text 반환
    //    let replaces =
    //        copySysCtxs.Select(fun (KeyValue(name, ctx)) ->
    //            let sourceSystemName = DsParser.tryFindFirstChild<SourceSystemNameContext>(ctx) |> Option.get |> fun x -> x.GetText()
    //            let srcCtx = sysCtxMap[sourceSystemName]
    //            let nameCtx = tryFindFirstChild<SystemNameContext>(srcCtx).Value
    //            let copiedSystemText = srcCtx.GetReplacedText([RangeReplace.Create(nameCtx, name)])
    //            RangeReplace.Create(ctx, copiedSystemText)).ToArray()
    //    let replacedText = model.GetReplacedText(replaces)
    //    //logDebug $"Replaced Text:\r\n{replacedText}"
    //    replacedText

    //static member FromDocument(text:string, predExtract:RuleExtractor, ?throwOnError) =       // (dsParser, RuleContext, ParserError[])
    //    //let expanded = DsParser.ExpandSystemCopy(text)
    //    DsParser.ParseText(text, predExtract, throwOnError)

    static member FromDocument(text:string, ?predExtract:RuleExtractor, ?throwOnError) =       // (dsParser, ParserError[])
        let throwOnError = defaultArg throwOnError true
        let func = defaultArg predExtract (fun (parser:dsParser) -> parser.system() :> RuleContext)
        //let (parser, tree, errors) = DsParser.FromDocument(text, func, throwOnError)
        let (parser, tree, errors) = ParseText(text, func, throwOnError)
        (parser, errors)


    static member enumerateChildren<'T when 'T :> IParseTree >(
        from:IParseTree
        , ?includeMe:bool
        , ?predicate:ParseTreePredicate
        , ?exclude:ParseTreePredicate
        ) : ResizeArray<'T> =         // ResizeArray<'T>

        let includeMe = defaultArg includeMe false
        let predicate = defaultArg predicate (isType<'T>)
        let exclude = defaultArg exclude (fun x -> false)
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

        let includeMe = defaultArg includeMe false
        let predicate = defaultArg predicate (isType<'T>)
        let rec helper(from:IParseTree, includeMe:bool) =
            [
                if from <> null then
                    if (includeMe && predicate(from) && isType<'T> from) then
                        yield forceCast<'T>(from)

                    yield! helper(from.Parent, true)
            ]
        helper(from, includeMe)



    static member tryFindFirstChild(from:IParseTree, predicate:ParseTreePredicate, ?includeMe:bool) =
        let includeMe = defaultArg includeMe false
        enumerateChildren<IParseTree>(from, includeMe) |> Seq.tryFind(predicate)

    static member tryFindFirstChild<'T when 'T :> IParseTree>(from:IParseTree, ?includeMe:bool, ?predicate:ParseTreePredicate, ?exclude:ParseTreePredicate) : 'T option =   // :'T
        let includeMe = defaultArg includeMe false
        let predicate = defaultArg predicate truthyfy
        let predicate x = isType<'T> x && predicate x
        let exclude = defaultArg exclude falsify
        enumerateChildren<'T>(from, includeMe, predicate, exclude) |> Seq.tryHead

    static member tryFindFirstAncestor(from:IParseTree, predicate:ParseTreePredicate, ?includeMe:bool) = //:IParseTree option=
        let includeMe = defaultArg includeMe false
        enumerateParents(from, includeMe) |> Seq.tryFind(predicate)


    static member tryFindFirstAncestor<'T when 'T :> IParseTree>(from:IParseTree, ?includeMe:bool) =
        let includeMe = defaultArg includeMe false
        let pred = isType<'T>
        tryFindFirstAncestor(from, pred, includeMe) |> Option.map forceCast<'T>

    static member tryFindIdentifier1FromContext(context:IParseTree, ?exclude:ParseTreePredicate) =
        let exclude = defaultArg exclude falsify
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

    static member getContextInformation(parserRuleContext:ParserRuleContext) =      // collectUpwardContextInformation
        let ctx = parserRuleContext
        let system  = LoadedSystemName.OrElse(tryGetSystemName ctx)
        let flow      = tryFindFirstAncestor<FlowBlockContext>(ctx, true).Bind(tryFindIdentifier1FromContext)
        let parenting = tryFindFirstAncestor<ParentingBlockContext>(ctx, true).Bind(tryFindIdentifier1FromContext)
        let ns        = collectNameComponents(ctx).ToFSharpList()
        ContextInformation.Create(ctx, system, flow, parenting, ns)


[<AutoOpen>]
module DsParserHelperModule =

    let choiceParentWrapper (ci:ContextInformation) (flow:Flow option) (parenting:Real option) =
        match ci.Parenting with
        | Some prnt -> Real parenting.Value
        | None -> Flow flow.Value
    let tryFindParentWrapper (system:DsSystem) (ci:ContextInformation) =
        option {
            let! flowName = ci.Flow
            match ci.Tuples with
            | Some sys, Some flow, Some parenting, _ ->
                let! real = tryFindReal system flow parenting
                return Real real
            | Some sys, Some flow, None, _ ->
                let! f = tryFindFlow system flowName
                return Flow f
            | _ -> failwith "ERROR"
        }

    let tryFindToken (system:DsSystem) (ctx:CausalTokenContext):Vertex option =
        let ci = getContextInformation ctx
        option {
            let! flowName = ci.Flow
            let! flow = tryFindFlow system flowName
            assert(flowName = flow.Name)

            let! parentWrapper = tryFindParentWrapper system ci
            let graph = parentWrapper.GetGraph()
            match ci.Names with
            | ofn::ofrn::[] ->      // of(r)n: other flow (real) name
                return! graph.TryFindVertex(ci.Names.Combine())
            | callOrAlias::[] ->
                return! graph.TryFindVertex(callOrAlias)
            | _ ->
                failwith "ERROR"
        }

