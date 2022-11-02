
namespace rec Engine.Parser.FS

open System
open System.Linq
open System.Text
open System.Runtime.InteropServices
open System.Text.RegularExpressions
open System.Collections.Generic

open Antlr4.Runtime
open Antlr4.Runtime.Tree

open Engine.Common
open Engine.Common.FS
open Engine.Parser
open type Engine.Parser.dsParser


type DsParser() =
    static member ParseText (text:string, extractor:dsParser->#RuleContext, [<Optional; DefaultParameterValue(true)>]throwOnError) =
        let str = new AntlrInputStream(text)
        let lexer = new dsLexer(str)
        let tokens = new CommonTokenStream(lexer)
        let parser = new dsParser(tokens)

        let listener_lexer = new ErrorListener<int>(throwOnError)
        let listener_parser = new ErrorListener<IToken>(throwOnError)
        lexer.AddErrorListener(listener_lexer)
        parser.AddErrorListener(listener_parser)
        let tree = extractor parser
        let errors = listener_lexer.Errors.Concat(listener_parser.Errors).ToArray()

        parser, tree, errors


    /// <summary>
    /// 주어진 text 내에서 [sys] B = @copy_system(A) 와 같이 copy_system 으로 정의된 영역을
    /// 치환한 text 를 반환한다.  system A 정의 영역을 찾아서 system B 로 치환한 text 반환
    /// 이때, copy 구문은 삭제한다.
    /// </summary>
    static member private ExpandSystemCopy(text:string):string =
        /// 원본 text 에서 copy_system 구문을 제외한 나머지 text 를 반환한다.
        let omitSystemCopy(text:string, sysCopies:SystemContext[]):string =
            for cc in sysCopies do
                if Global.Logger <> null then
                    Global.Logger.Debug($"Replacing @copy_system(): {cc.GetText()}")

            let ranges = sysCopies.Select(fun ctx -> (ctx.Start.StartIndex, ctx.Stop.StopIndex)).ToArray()
            let chars =
                text
                    |> Seq.filteri(fun n ch -> ranges |> Seq.forall(fun r -> n < fst r || snd r < n))
                    |> Array.ofSeq

            String(chars)

        // see RuleContext.GetText()
        let rec ToText(ctx:RuleContext):string =
            if ctx.ChildCount = 0 then
                ""
            else
                let sb = new StringBuilder()
                let mutable last = " "
                for i in [0 .. ctx.ChildCount - 1] do
                    let ch = ctx.GetChild(i)
                    let text =
                        match ch with
                        | :? RuleContext as rc -> ToText(rc)
                        | _ -> ch.GetText()

                    // [sys ip = 123.2...] 에서 sys token 과 ip token 사이 공백을 삽입한다.
                    if (Char.IsLetterOrDigit(last.Last()) && Char.IsLetterOrDigit(text[0])) then
                        sb.Append(" ") |> ignore
                    last <- text
                    sb.Append(text)  |> ignore

                sb.ToString()

        let helper() =
            [
                let func = fun (parser:dsParser) -> parser.model() :> RuleContext
                let (parser, _, _) = DsParser.ParseText(text, func)
                parser.Reset()
                let sysCtxMap =
                    DsParser.enumerateChildren<SystemContext>(parser.model())
                        .Select(fun ctx ->
                            let sysName = DsParser.findFirstChild<SystemNameContext>(ctx) |> Option.get |> fun x -> x.GetText()
                            sysName, ctx)
                        |> dict |> Dictionary

                let copySysCtxs = sysCtxMap.Where(fun kv -> DsParser.findFirstChild<SysCopySpecContext>(kv.Value) |> Option.isSome).ToArray()

                // 원본 full text 에서 copy_system 구문 삭제한 text 반환
                let textWithoutSysCopy = omitSystemCopy(text, copySysCtxs.Select(fun kv -> kv.Value).ToArray())
                yield textWithoutSysCopy

                for kv in copySysCtxs do
                    yield "\r\n"

                    let newSysName = kv.Key
                    let srcSysName = DsParser.findFirstChild<SourceSystemNameContext>(kv.Value) |> Option.get |> fun x -> x.GetText()
                    // 원본 시스템의 text 를 사본 system text 로 치환해서 생성
                    let sysText = ToText(sysCtxMap[srcSysName])
                    let pattern = @"(\[sys([^\]]*\]))([^=]*)="
                    let replaced = Regex.Replace(sysText, pattern, $"$1{newSysName}=")
                    yield replaced
            ]
        helper().JoinLines()






    static member FromDocument(text:string, predExtract:dsParser->#RuleContext, [<Optional; DefaultParameterValue(true)>]throwOnError) =       // (dsParser, RuleContext, ParserError[])
        let expanded = DsParser.ExpandSystemCopy(text)
        DsParser.ParseText(expanded, predExtract, throwOnError)

    static member FromDocument(text:string, [<Optional; DefaultParameterValue(true)>]throwOnError:bool) =       // (dsParser, ParserError[])
        let func = fun (parser:dsParser) -> parser.model()
        let (parser, tree, errors) = DsParser.FromDocument(text, func, throwOnError)
        (parser, errors)


    static member enumerateChildren<'T when 'T :> IParseTree >(
        from:IParseTree
        , ?includeMe:bool
        , ?predicate:(IParseTree->bool)
        ) : ResizeArray<'T> =         // ResizeArray<'T>

        let includeMe = defaultArg includeMe false
        let predicate = defaultArg predicate (isType<'T>)
        let rec enumerateChildrenHelper(rslt:ResizeArray<'T>, frm:IParseTree, incMe:bool, pred:(IParseTree->bool)) =
            if (incMe && pred(frm)) then
                rslt.Add(forceCast<'T>(frm))

            for index in [ 0 .. frm.ChildCount - 1 ] do
                enumerateChildrenHelper(rslt, frm.GetChild(index), true, pred)

        //Func<IParseTree, bool> pred = predicate ?? new Func<IParseTree, bool>(ctx => ctx is T)
        let result = ResizeArray<'T>()
        enumerateChildrenHelper(result, from, includeMe, predicate)
        result


    static member enumerateParents(from:IParseTree      // IEnumerable<IParseTree>
        , ?includeMe:bool
        , ?predicate:(IParseTree->bool)) =

        let includeMe = defaultArg includeMe false
        let predicate = defaultArg predicate (fun _ -> true)
        let rec helper(from:IParseTree, includeMe:bool) =
            [
                if (includeMe && predicate(from)) then
                    yield from

                yield! helper(from.Parent, true)
            ]
        helper(from, includeMe)



    static member findFirstChild(from:IParseTree, predicate:(IParseTree->bool), ?includeMe:bool) =
        let includeMe = defaultArg includeMe false
        DsParser.enumerateChildren<IParseTree>(from, includeMe) |> Seq.tryFind(predicate)

    static member findFirstChild<'T when 'T :> IParseTree>(from:IParseTree, ?includeMe:bool) : 'T option =   // :'T
        let includeMe = defaultArg includeMe false
        DsParser.enumerateChildren<'T>(from, includeMe) |> Seq.tryFind(isType<'T>)

    static member findFirstAncestor(from:IParseTree, predicate:(IParseTree->bool), ?includeMe:bool) = //:IParseTree option=
        let includeMe = defaultArg includeMe false
        DsParser.enumerateParents(from, includeMe) |> Seq.tryFind(predicate)


    static member findFirstAncestor<'T when 'T :> IParseTree>(from:IParseTree, ?includeMe:bool) =
        let includeMe = defaultArg includeMe false
        let pred = isType<'T>
        DsParser.findFirstAncestor(from, pred, includeMe) |> Option.map forceCast<'T>



    static member collectNameComponents(from:IParseTree):string[] = // :Fqdn
        let rec splitName(name:string) = // : Fqdn
            [
                let sub = new ResizeArray<char>()
                let mutable prev = ' '
                let pop =
                    let mutable i = 0
                    fun () ->
                        if i < name.Length then
                            let c = name.[i]
                            i <- i + 1
                            Some c
                        else
                            None

                let mutable ch = pop()
                //let mutable quit = false
                let mutable q = false
                while ch.IsSome do
                    match ch with
                    | Some ch ->
                        sub.Add(ch)
                        match ch with

                        | '\\' ->
                            let next = pop() |> Option.get
                            sub.Add(next)
                            prev <- next

                        | '.' when q->
                            ()  //break

                        | '.' ->
                            sub.RemoveTail() |> ignore
                            yield String(sub.ToArray())
                            sub.Clear()

                        | '"' when prev <> '\\' ->
                            sub.RemoveTail() |> ignore
                            if q then
                                yield String(sub.ToArray())
                                sub.Clear()
                            else
                                q <- true

                        | _ ->
                            ()
                    | None ->
                        q <- true
                    ch <- pop()

                if sub.Any() then
                    yield String(sub.ToArray())
            ]

        let idCtx =
            let pred = fun (tree:IParseTree) ->
                            tree :? Identifier1Context
                            || tree :? Identifier2Context
                            || tree :? Identifier3Context
                            || tree :? Identifier4Context
            DsParser.findFirstChild(from, pred, true) |> Option.get
        let name = idCtx.GetText()
        splitName(name).ToArray()


    static member getParseResult(parser:dsParser) = // : ParserResult
        let listener = new AllListener()
        ParseTreeWalker.Default.Walk(listener, parser.model())
        listener.r

    /// <summary>
    /// parser tree 상의 모든 node (rule context, terminal node, error node) 을 반환한다.
    /// </summary>
    /// <param name="parser">text DS Document (Parser input)</param>
    /// <returns></returns>
    static member getAllParseTrees(parser:dsParser) = // ResizeArray<IParseTree>
        let r:ParserResult = DsParser.getParseResult(parser)

        r.rules.Cast<IParseTree>()
            .Concat(r.terminals.Cast<IParseTree>())
            .Concat(r.errors.Cast<IParseTree>())
            .ToList()



    /// <summary>parser tree 상의 모든 rule 을 반환한다.</summary>
    static member getAllParseRules(parser:dsParser) = // ResizeArray<ParserRuleContext>
        let r:ParserResult = DsParser.getParseResult(parser)
        r.rules

