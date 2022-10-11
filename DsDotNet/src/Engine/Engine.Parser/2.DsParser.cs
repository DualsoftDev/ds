using Antlr4.Runtime;

namespace Engine.Parser;

class DsParser
{
    public static (dsParser, RuleContext, ParserError[]) ParseText(
        string text, Func<dsParser, RuleContext> predExtract,
        bool throwOnError = true)
    {
        var str = new AntlrInputStream(text);
        var lexer = new dsLexer(str);
        var tokens = new CommonTokenStream(lexer);
        var parser = new dsParser(tokens);

        var listener_lexer = new ErrorListener<int>(throwOnError);
        var listener_parser = new ErrorListener<IToken>(throwOnError);
        lexer.AddErrorListener(listener_lexer);
        parser.AddErrorListener(listener_parser);
        var tree = parser.program();
        var errors = listener_lexer.Errors.Concat(listener_parser.Errors).ToArray();

        return (parser, tree, errors);
    }

    static string ExpandSystemCopy(string text)
    {
        string omitSystemCopy(string text, SystemContext[] sysCopies)
        {
            foreach (var cc in sysCopies)
                Global.Logger?.Debug($"Replacing @copy_system(): {cc.GetText()}");

            var ranges = sysCopies.Select(ctx => (ctx.Start.StartIndex, ctx.Stop.StopIndex)).ToArray();
            var chars =
                text
                    .Where((ch, n) => ranges.ForAll(r => !n.InClosedRange(r.StartIndex, r.StopIndex)))
                    .Select((ch, _) => ch)
                    .ToArray()
                    ;
            return new string(chars);
        }

        IEnumerable<string> helper()
        {
            var func = (dsParser parser) => parser.program();
            var (parser, _, _) = DsParser.ParseText(text, func);
            parser.Reset();
            var sysCtxMap =
                enumerateChildren<SystemContext>(parser.program())
                .ToDictionary(ctx => findFirstChild<SystemNameContext>(ctx).GetText(), ctx => ctx)    //ctx => findFirstChild<SysCopySpecContext>(ctx))
                ;
            var copySysCtxs = sysCtxMap.Where(kv => findFirstChild<SysCopySpecContext>(kv.Value) != null).ToArray();
            var textWithoutSysCopy = omitSystemCopy(text, copySysCtxs.Select(kv => kv.Value).ToArray());
            yield return textWithoutSysCopy;

            foreach (var kv in copySysCtxs)
            {
                var newSysName = kv.Key;
                var srcSysName = findFirstChild<SourceSystemNameContext>(kv.Value).GetText();
                var sysText = sysCtxMap[srcSysName].GetText();
                yield return "\r\n";
                yield return sysText.Replace($"[sys]{srcSysName}", $"[sys]{newSysName}");
            }
        }

        return string.Join("\r\n", helper());
    }
    public static (dsParser, RuleContext, ParserError[]) FromDocument(
        string text, Func<dsParser, RuleContext> predExtract,
        bool throwOnError = true)
    {
        var expanded = ExpandSystemCopy(text);
        return ParseText(expanded, predExtract, throwOnError);
    }

    public static (dsParser, ParserError[]) FromDocument(string text, bool throwOnError = true)
    {
        var func = (dsParser parser) => parser.program();
        var(parser, tree, errors) = FromDocument(text, func, throwOnError);
        return (parser, errors);
    }


    public static List<T> enumerateChildren<T>(IParseTree from, bool includeMe = false, Func<IParseTree, bool> predicate = null) where T : IParseTree
    {
        Func<IParseTree, bool> pred = predicate ?? new Func<IParseTree, bool>(ctx => ctx is T);
        var result = new List<T>();
        enumerateChildrenHelper(result, from, includeMe, pred);
        return result;


        void enumerateChildrenHelper(List<T> rslt, IParseTree frm, bool incMe, Func<IParseTree, bool> pred)
        {
            bool ok(IParseTree t)
            {
                if (pred != null)
                    return pred(t);
                return true;
            }

            if (incMe && ok(frm))
                rslt.Add((T)frm);
            for (int index = 0; index < frm.ChildCount; index++)
                enumerateChildrenHelper(rslt, frm.GetChild(index), true, ok);
        }
    }

    public static IEnumerable<IParseTree> enumerateParents(IParseTree from, bool includeMe=false, Func<IParseTree, bool> predicate = null)
    {
        bool ok(IParseTree t)
        {
            if (predicate != null)
                return predicate(t);
            return true;
        }

        if (includeMe && ok(from))
            yield return from;

        foreach (var p in enumerateParents(from.Parent, true, ok))
            yield return p;
    }


    public static IParseTree findFirstChild(IParseTree from, Func<IParseTree, bool> predicate, bool includeMe=false)
    {
        foreach (var c in enumerateChildren<IParseTree>(from, includeMe))
        {
            if (predicate(c))
                return c;
        }

        return null;
    }
    public static T findFirstChild<T>(IParseTree from, bool includeMe = false) where T: IParseTree =>
        enumerateChildren<T>(from, includeMe).FirstOrDefault();

    public static IParseTree findFirstAncestor(IParseTree from, Func<IParseTree, bool> predicate, bool includeMe=false)
    {
        foreach (var c in enumerateParents(from, includeMe))
        {
            if (predicate(c))
                return c;
        }

        return null;
    }
    public static T findFirstAncestor<T>(IParseTree from, bool includeMe = false) where T : IParseTree
    {
        var pred = (IParseTree parseTree) => parseTree is T;
        return (T)findFirstAncestor(from, pred, includeMe);
    }



    public static string[] collectNameComponents(IParseTree from) =>
        enumerateChildren<Identifier1Context>(from)
            .Select(idf => idf.GetText().DeQuoteOnDemand())
            .ToArray()
            ;

    public static ParserResult getParseResult(dsParser parser)
    {
        var listener = new AllListener();
        ParseTreeWalker.Default.Walk(listener, parser.program());
        return listener.r;
    }

    /**
     * parser tree 상의 모든 node (rule context, terminal node, error node) 을 반환한다.
     * @param text DS Document (Parser input)
     * @returns
     */
    public static List<IParseTree> getAllParseTrees(dsParser parser)
    {
        ParserResult r = getParseResult(parser);

        return r.rules.Cast<IParseTree>()
            .Concat(r.terminals.Cast<IParseTree>())
            .Concat(r.errors.Cast<IParseTree>())
            .ToList()
            ;
    }


    /**
     * parser tree 상의 모든 rule 을 반환한다.
     * @param text DS Document (Parser input)
     * @returns
     */
    public static List<ParserRuleContext> getAllParseRules(dsParser parser)
    {
        ParserResult r = getParseResult(parser);
        return r.rules;
    }
}
