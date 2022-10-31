using Antlr4.Runtime

namespace Engine.Parser.FS

public static class ModelParser
{
    static void Walk(dsParser parser, ParserHelper helper)
    {
        let sListener = new SkeletonListener(parser, helper)
        ParseTreeWalker.Default.Walk(sListener, parser.model())
        Trace.WriteLine("--- End of skeleton listener")

        let eleListener = new ElementListener(parser, helper)
        ParseTreeWalker.Default.Walk(eleListener, parser.model())
        Trace.WriteLine("--- End of element listener")


        let edgeListener = new EdgeListener(parser, helper)
        ParseTreeWalker.Default.Walk(edgeListener, parser.model())
        Trace.WriteLine("--- End of edge listener")

        let etcListener = new EtcListener(parser, helper)
        ParseTreeWalker.Default.Walk(etcListener, parser.model())
        Trace.WriteLine("--- End of etc listener")
    }
    public static ParserHelper ParseFromString2(string text, ParserOptions options)
    {
        let (parser, errors) = DsParser.FromDocument(text)
        let helper = new ParserHelper(options)

        Walk(parser, helper)

        let model = helper.Model
        model.CreateMRIEdgesTransitiveClosure()
        model.Validate()

        return helper
    }

    public static Model ParseFromString(string text, ParserOptions options) => ParseFromString2(text, options).Model


    public static void ParsePartial(string text, ParserHelper helper, Func<dsParser, RuleContext> predExtract=null)
    {
        if (predExtract == null)
            predExtract = new Func<dsParser, RuleContext>((dsParser parser) => parser.model())
        let (parser, ast, parserErrors) = FromDocument(text, predExtract)
        Walk(parser, helper)
    }
}
