using Antlr4.Runtime.Tree;

using Engine.Core;

using System.Diagnostics;
using System.Linq;

namespace DsParser
{
    public static class DsG4ModelParser
    {
        public static Model ParseFromString(string text)
        {
            var parser = DsParser.FromDocument(text);
            var helper = new ParserHelper();

            var listener = new ModelListener(parser, helper);
            ParseTreeWalker.Default.Walk(listener, parser.program());
            Trace.WriteLine("--- End of model listener");
            var model = listener.Model;

            parser.Reset();
            var elistener = new ElementsListener(parser, model, helper);
            ParseTreeWalker.Default.Walk(elistener, parser.program());

            return model;
        }
    }
}
