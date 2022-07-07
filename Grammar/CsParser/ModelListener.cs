using System.Diagnostics;
using System.Linq;

using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace DsParser
{
    class ModelListener : dsBaseListener
    {
        public Model Model { get; }
        DsSystem _system;
        Task _task;
        RootFlow _rootFlow;

        public ModelListener(dsParser parser)
        {
            Model = new Model();
            parser.Reset();
        }


        override public void EnterSystem(dsParser.SystemContext ctx)
        {
            var n = ctx.id().GetText();
            _system = new DsSystem(n, Model);
            Trace.WriteLine($"System: {n}");
        }
        //override public void ExitSystem(dsParser.SystemContext ctx) { this.systemName = null; }

        override public void EnterTask(dsParser.TaskContext ctx)
        {
            var name = ctx.id().GetText();
            _task = new Task(name, _system);
        }
        //override public void ExitTask(dsParser.TaskContext ctx) { this.taskName = null; }

        override public void EnterFlow(dsParser.FlowContext ctx)
        {
            var flowName = ctx.id().GetText();
            var flowOf = ctx.flowProp().id();
            _rootFlow = new RootFlow(flowName, _system);
            Trace.WriteLine($"Flow: {flowName}");
        }
        override public void ExitFlow(dsParser.FlowContext ctx) { _rootFlow = null; }

        override public void EnterCausalPhrase(dsParser.CausalPhraseContext ctx)
        {
            //var dnfs =
            //    DsParser.enumerateChildren<dsParser.CausalTokensDNFContext>(
            //        ctx, false, r => r is dsParser.CausalTokensDNFContext);

            var names =
                DsParser.enumerateChildren<dsParser.SegmentContext>(
                    ctx, false, r => r is dsParser.SegmentContext)
                .Select(segCtx => segCtx.GetText())
                ;

            var _segments =
                names
                .Where(n => ! n.Contains('.'))
                .Select(n => new RootSegment(n, _rootFlow))  // _flow 에 segment 로 등록됨
                .ToArray()
                ;
        }

        override public void EnterCall(dsParser.CallContext ctx)
        {
            var name = ctx.id().GetText();
            var label = $"{name}\n{ctx.callPhrase().GetText()}";
            var callph = ctx.callPhrase();
            //var tx = callph.segments(0);
            //var rx = callph.segments(1);
            var call = new Call(name, _task);

            //var parentId = $"{this.systemName}.{this.taskName}";
            //var id = $"{parentId}.{name}";
            //this.nodes[id] = new Node(id, label, parentId, NodeType.call);
            Trace.WriteLine($"CALL: {name}");
        }


        override public void EnterListing(dsParser.ListingContext ctx)
        {
            var name = ctx.id().GetText();
            var seg = new RootSegment(name, _rootFlow);

            //var id = $"{this.systemName}.{this.taskName}.{name}";
            ////const node = { "data": { id, "label": name, "background_color": "gray", parent: this.taskName }        };
            //var parentId = $"{this.systemName}.{this.taskName}";
            //this.nodes[id] = new Node(id, label: name, parentId, NodeType.segment);
        }



        override public void EnterCausals(dsParser.CausalsContext ctx)
        {
            Trace.WriteLine($"Causals: {ctx.GetText()}");
        }
        //override public void ExitCausals(dsParser.CausalsContext ctx) {}

        override public void EnterParenting(dsParser.ParentingContext ctx) {
            Trace.WriteLine($"Parenting: {ctx.GetText()}");
            var name = ctx.id().GetText();
            var seg = new RootSegment(name, _rootFlow);
        }
        //override public void ExitParenting(dsParser.ParentingContext ctx) { }


        // ParseTreeListener<> method
        override public void VisitTerminal(ITerminalNode node)     { return; }
        override public void VisitErrorNode(IErrorNode node)        { return; }
        override public void EnterEveryRule(ParserRuleContext ctx) { return; }
        override public void ExitEveryRule(ParserRuleContext ctx) { return; }
    }
}
