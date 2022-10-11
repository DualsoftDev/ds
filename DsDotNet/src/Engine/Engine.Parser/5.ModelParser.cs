using Antlr4.Runtime;

namespace Engine.Parser;

public static class ModelParser
{
    static void Walk(dsParser parser, ParserHelper helper)
    {
        var sListener = new SkeletonListener(parser, helper);
        ParseTreeWalker.Default.Walk(sListener, parser.program());
        Trace.WriteLine("--- End of skeleton listener");

        var eleListener = new ElementListener(parser, helper);
        ParseTreeWalker.Default.Walk(eleListener, parser.program());
        Trace.WriteLine("--- End of element listener");


        var edgeListener = new EdgeListener(parser, helper);
        ParseTreeWalker.Default.Walk(edgeListener, parser.program());
        Trace.WriteLine("--- End of edge listener");

        var etcListener = new EtcListener(parser, helper);
        ParseTreeWalker.Default.Walk(etcListener, parser.program());
        Trace.WriteLine("--- End of etc listener");
    }
    public static ParserHelper ParseFromString2(string text, ParserOptions options)
    {
        var (parser, errors) = DsParser.FromDocument(text);
        var helper = new ParserHelper(options);

        Walk(parser, helper);

        return helper;
    }

    public static Model ParseFromString(string text, ParserOptions options) => ParseFromString2(text, options).Model;


    public static void ParsePartial(string text, ParserHelper helper, Func<dsParser, RuleContext> predExtract=null)
    {
        if (predExtract == null)
            predExtract = new Func<dsParser, RuleContext>((dsParser parser) => parser.program());
        var (parser, ast, parserErrors) = FromDocument(text, predExtract);
        Walk(parser, helper);
    }

    static string omitSystemCopy(string text, SystemContext[] sysCopies )
    {
        var ranges = sysCopies.Select(ctx => (ctx.Start.StartIndex, ctx.Stop.StopIndex)).ToArray();
        var chars =
            text
                .Where((ch, n) => ranges.ForAll(r => !n.InClosedRange(r.StartIndex, r.StopIndex)))
                .Select((ch, _) => ch)
                .ToArray()
                ;
        return new string(chars);
    }

    // [sys] B = @copy_system(A)
    public static string ExpandSystemCopy(string text)
    {
        IEnumerable<string> helper()
        {
            var (parser, errors) = DsParser.FromDocument(text);
            var helper = new ParserHelper(ParserOptions.Create4Simulation());

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

        //var sListener = new CopySystemListener(parser, helper);
        //ParseTreeWalker.Default.Walk(sListener, parser.program());
        //Trace.WriteLine("--- End of copy system listener");

        //return null;
    }

}
