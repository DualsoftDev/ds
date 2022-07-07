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

            // clean up
            var segmentsWithEmptyFlow =
                model.Systems
                    .SelectMany(s => s.Flows).OfType<RootFlow>()
                    .SelectMany(f => f.Segments).Cast<RootSegment>()
                    .Where(s => s.ChildFlow.Edges.Count == 0)
                    ;
            foreach (var s in segmentsWithEmptyFlow)
                s.ChildFlow = null;

            return model;
        }
    }
}
