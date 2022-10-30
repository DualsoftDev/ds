


namespace Engine.Parser.FS
open System
open System.Runtime.InteropServices
open Antlr4.Runtime
open Engine.Parser
open Antlr4.Runtime;

open System.Linq;
open System.Text;
open System.Text.RegularExpressions;
open type Engine.Parser.dsParser
open Engine.Common.FS

module DsParser =
    let ParseText (text:string, predExtract:dsParser->RuleContext, [<Optional; DefaultParameterValue(true)>]throwOnError) =
        let str = new AntlrInputStream(text)
        let lexer = new dsLexer(str)
        let tokens = new CommonTokenStream(lexer)
        let parser = new dsParser(tokens)

        let listener_lexer = new ErrorListener<int>(throwOnError)
        let listener_parser = new ErrorListener<IToken>(throwOnError)
        lexer.AddErrorListener(listener_lexer)
        parser.AddErrorListener(listener_parser)
        let tree = parser.model()
        let errors = listener_lexer.Errors.Concat(listener_parser.Errors).ToArray()

        parser, tree, errors


    /// <summary>
    /// 주어진 text 내에서 [sys] B = @copy_system(A) 와 같이 copy_system 으로 정의된 영역을
    /// 치환한 text 를 반환한다.  system A 정의 영역을 찾아서 system B 로 치환한 text 반환
    /// 이때, copy 구문은 삭제한다.
    /// </summary>
    let private ExpandSystemCopy(text:string):string =
        /// 원본 text 에서 copy_system 구문을 제외한 나머지 text 를 반환한다.
        let omitSystemCopy(text:string, sysCopies:SystemContext[]):string =
            for cc in sysCopies do
                if Global.Logger <> null then
                    Global.Logger.Debug($"Replacing @copy_system(): {cc.GetText()}")

            let ranges = sysCopies.Select(fun ctx -> (ctx.Start.StartIndex, ctx.Stop.StopIndex)).ToArray()
            let chars =
                text
                    |> Seq.filteri(fun n ch -> ranges |> Seq.forall(fun r -> n < r.StartIndex || r.StopIndex < n))
                    |> Array.ofSeq

            string(chars)

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
                let func = fun (parser:dsParser) -> parser.model()
                let (parser, _, _) = DsParser.ParseText(text, func)
                parser.Reset()
                let sysCtxMap =
                    enumerateChildren<SystemContext>(parser.model())
                        .ToDictionary(ctx => findFirstChild<SystemNameContext>(ctx).GetText(), ctx => ctx)    //ctx => findFirstChild<SysCopySpecContext>(ctx))

                let copySysCtxs = sysCtxMap.Where(fun kv -> findFirstChild<SysCopySpecContext>(kv.Value) <> null).ToArray()

                // 원본 full text 에서 copy_system 구문 삭제한 text 반환
                let textWithoutSysCopy = omitSystemCopy(text, copySysCtxs.Select(kv => kv.Value).ToArray())
                yield return textWithoutSysCopy

                foreach (let kv in copySysCtxs)
                {
                    yield return "\r\n"

                    let newSysName = kv.Key
                    let srcSysName = findFirstChild<SourceSystemNameContext>(kv.Value).GetText()
                    // 원본 시스템의 text 를 사본 system text 로 치환해서 생성
                    let sysText = ToText(sysCtxMap[srcSysName])
                    let pattern = @"(\[sys([^\]]*\]))([^=]*)="
                    let replaced = Regex.Replace(sysText, pattern, $"$1{newSysName}=")
                    yield return replaced
                }
            ]

        return string.Join("\r\n", helper())
    }




















    public static (dsParser, RuleContext, ParserError[]) FromDocument(
        string text, Func<dsParser, RuleContext> predExtract,
        bool throwOnError = true)
    {
        let expanded = ExpandSystemCopy(text)
        return ParseText(expanded, predExtract, throwOnError)
    }

    public static (dsParser, ParserError[]) FromDocument(string text, bool throwOnError = true)
    {
        let func = (dsParser parser) => parser.model()
        let(parser, tree, errors) = FromDocument(text, func, throwOnError)
        return (parser, errors)
    }


    public static List<T> enumerateChildren<T>(IParseTree from, bool includeMe = false, Func<IParseTree, bool> predicate = null) where T : IParseTree
    {
        Func<IParseTree, bool> pred = predicate ?? new Func<IParseTree, bool>(ctx => ctx is T)
        let result = new List<T>()
        enumerateChildrenHelper(result, from, includeMe, pred)
        return result


        void enumerateChildrenHelper(List<T> rslt, IParseTree frm, bool incMe, Func<IParseTree, bool> pred)
        {
            bool ok(IParseTree t)
            {
                if (pred != null)
                    return pred(t)
                return true
            }

            if (incMe && ok(frm))
                rslt.Add((T)frm)
            for (int index = 0 index < frm.ChildCount index++)
                enumerateChildrenHelper(rslt, frm.GetChild(index), true, ok)
        }
    }

    public static IEnumerable<IParseTree> enumerateParents(IParseTree from, bool includeMe=false, Func<IParseTree, bool> predicate = null)
    {
        bool ok(IParseTree t)
        {
            if (predicate != null)
                return predicate(t)
            return true
        }

        if (includeMe && ok(from))
            yield return from

        foreach (let p in enumerateParents(from.Parent, true, ok))
            yield return p
    }


    public static IParseTree findFirstChild(IParseTree from, Func<IParseTree, bool> predicate, bool includeMe=false)
    {
        foreach (let c in enumerateChildren<IParseTree>(from, includeMe))
        {
            if (predicate(c))
                return c
        }

        return null
    }
    public static T findFirstChild<T>(IParseTree from, bool includeMe = false) where T: IParseTree =>
        enumerateChildren<T>(from, includeMe).FirstOrDefault()

    public static IParseTree findFirstAncestor(IParseTree from, Func<IParseTree, bool> predicate, bool includeMe=false)
    {
        foreach (let c in enumerateParents(from, includeMe))
        {
            if (predicate(c))
                return c
        }

        return null
    }
    public static T findFirstAncestor<T>(IParseTree from, bool includeMe = false) where T : IParseTree
    {
        let pred = (IParseTree parseTree) => parseTree is T
        return (T)findFirstAncestor(from, pred, includeMe)
    }



    public static string[] collectNameComponents(IParseTree from)
    {
        IEnumerable<string> splitName(string name)
        {
            let sub = new List<char>()
            let q = false
            let prev = ' '
            for(int i = 0 i < name.Length i++)
            {
                let ch = name[i]
                sub.Add(ch)

                switch(ch)
                {
                    case '\\':
                        let next = name[++i]
                        sub.Add(next)
                        prev = next
                        continue

                    case '.' when q:
                        break

                    case '.':
                        sub.RemoveTail()
                        yield return new string(sub.ToArray())
                        sub.Clear()
                        break

                    case '"' when prev != '\\':
                        sub.RemoveTail()
                        if (q)
                        {
                            yield return new string(sub.ToArray())
                            sub.Clear()
                        }
                        else
                        {
                            q = true
                        }
                        break

                }
            }
            if (sub.Any())
                yield return new string(sub.ToArray())
        }
        let idCtx = findFirstChild(from,
                        tree =>
                            tree is Identifier1Context
                            || tree is Identifier2Context
                            || tree is Identifier3Context
                            || tree is Identifier4Context,
                        true)
        let name = idCtx.GetText()
        return splitName(name).ToArray()
        //return
        //    enumerateChildren<Identifier1Context>(from)
        //        .Select(idf => idf.GetText().DeQuoteOnDemand())
        //        .ToArray()
        //
    }

    public static ParserResult getParseResult(dsParser parser)
    {
        let listener = new AllListener()
        ParseTreeWalker.Default.Walk(listener, parser.model())
        return listener.r
    }

    /// <summary>
    /// parser tree 상의 모든 node (rule context, terminal node, error node) 을 반환한다.
    /// </summary>
    /// <param name="parser">text DS Document (Parser input)</param>
    /// <returns></returns>
    public static List<IParseTree> getAllParseTrees(dsParser parser)
    {
        ParserResult r = getParseResult(parser)

        return r.rules.Cast<IParseTree>()
            .Concat(r.terminals.Cast<IParseTree>())
            .Concat(r.errors.Cast<IParseTree>())
            .ToList()

    }


    /// <summary>parser tree 상의 모든 rule 을 반환한다.</summary>
    public static List<ParserRuleContext> getAllParseRules(dsParser parser)
    {
        ParserResult r = getParseResult(parser)
        return r.rules
    }
}
