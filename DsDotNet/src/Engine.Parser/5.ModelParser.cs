namespace Engine.Parser;

public static class ModelParser
{
    public static Model ParseFromString(string text)
    {
        var parser = DsParser.FromDocument(text);
        var helper = new ParserHelper();

        var sListener = new SkeletonListener(parser, helper);
        ParseTreeWalker.Default.Walk(sListener, parser.program());
        Trace.WriteLine("--- End of skeleton listener");

        var aListener = new AliasListener(parser, helper);
        ParseTreeWalker.Default.Walk(aListener, parser.program());
        Trace.WriteLine("--- End of alias listener");


        var mListener = new ModelListener(parser, helper);
        ParseTreeWalker.Default.Walk(mListener, parser.program());
        Trace.WriteLine("--- End of model listener");

        parser.Reset();
        var eListener = new ElementsListener(parser, helper);
        ParseTreeWalker.Default.Walk(eListener, parser.program());

        return helper.Model;
    }
}
