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

        var model = helper.Model;
        model.CreateMRIEdgesTransitiveClosure();


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
}
