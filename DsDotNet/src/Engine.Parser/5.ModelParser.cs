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

        var mListener = new ModelListener(parser, helper);
        ParseTreeWalker.Default.Walk(mListener, parser.program());
        Trace.WriteLine("--- End of model listener");

        parser.Reset();
        var eListener = new ElementsListener(parser, helper);
        ParseTreeWalker.Default.Walk(eListener, parser.program());

        return helper;
    }

    public static Model ParseFromString(string text, ParserOptions options) => ParseFromString2(text, options).Model;

}
