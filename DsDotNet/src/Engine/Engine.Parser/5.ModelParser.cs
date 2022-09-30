namespace Engine.Parser;

public static class ModelParser
{
    public static ParserHelper ParseFromString2(string text, ParserOptions options)
    {
        var (parser, errors) = DsParser.FromDocument(text);
        var helper = new ParserHelper(options);

        var sListener = new SkeletonListener(parser, helper);
        ParseTreeWalker.Default.Walk(sListener, parser.program());
        Trace.WriteLine("--- End of skeleton listener");

        var eleListener = new ElementListener(parser, helper);
        ParseTreeWalker.Default.Walk(eleListener, parser.program());
        Trace.WriteLine("--- End of element listener");


        var edgeListener = new EdgeListener(parser, helper);
        ParseTreeWalker.Default.Walk(edgeListener, parser.program());
        Trace.WriteLine("--- End of edge listener");

        return helper;
    }

    public static Model ParseFromString(string text, ParserOptions options) => ParseFromString2(text, options).Model;

}
