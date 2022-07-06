using Antlr4.Runtime.Tree;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DsParser
{
    public static class ModelParser
    {
        public static Model ParseFromString(string text)
        {
            var parser = DsParser.FromDocument(text);
            var listener = new ModelListener(parser);
            ParseTreeWalker.Default.Walk(listener, parser.program());
            Trace.WriteLine("--- End of model listener");
            var model = listener.Model;

            parser.Reset();
            var elistener = new ElementsListener(parser, model);
            ParseTreeWalker.Default.Walk(elistener, parser.program());

            return model;
        }
    }
}
