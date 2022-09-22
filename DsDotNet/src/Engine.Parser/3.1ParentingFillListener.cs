using Engine.Core;

using System.Windows.Forms;

namespace Engine.Parser
{
    internal class ParentingFillListener : dsBaseListener
    {
        #region Boiler-plates
        public ParserHelper ParserHelper;
        Model _model => ParserHelper.Model;
        DsSystem _system { get => ParserHelper._system; set => ParserHelper._system = value; }
        RootFlow _rootFlow { get => ParserHelper._rootFlow; set => ParserHelper._rootFlow = value; }
        SegmentBase _parenting { get => ParserHelper._parenting; set => ParserHelper._parenting = value; }

        public ParentingFillListener(dsParser parser, ParserHelper helper)
        {
            ParserHelper = helper;
            parser.Reset();
        }

        override public void EnterSystem(SystemContext ctx)
        {
            var name = ctx.identifier1().GetText().DeQuoteOnDemand();
            _system = _model.Systems.First(s => s.Name == name);
        }
        override public void ExitSystem(SystemContext ctx) { this._system = null; }

        override public void EnterFlow(FlowContext ctx)
        {
            var flowName = ctx.identifier1().GetText().DeQuoteOnDemand();
            _rootFlow = _system.RootFlows.First(f => f.Name == flowName);
        }
        override public void ExitFlow(FlowContext ctx) { _rootFlow = null; }



        override public void ExitParenting(ParentingContext ctx) { _parenting = null; }
        #endregion Boiler-plates


        override public void EnterParenting(ParentingContext ctx)
        {
            var name = ctx.identifier1().GetText().DeQuoteOnDemand();
            _parenting = (SegmentBase)_rootFlow.InstanceMap[name];
            var myFlowCallCtxs = enumerateChildren<Identifier1ListingContext>(ctx).Select(ctx => collectNameComponents(ctx)[0]).ToArray();
            foreach(var call in myFlowCallCtxs)
            {
                var cp = _rootFlow.CallPrototypes.FirstOrDefault(cp => cp.Name == call);
                object instance = new Child(new SubCall(call, _parenting, cp), _parenting);
                _parenting.InstanceMap.Add(call, instance);
            }

            var mySystemOtherFlowCallCtxs = enumerateChildren<Identifier2ListingContext>(ctx).Select(ctx => collectNameComponents(ctx).ToArray()).ToArray();   // collectNameComponents
            foreach(var otherFlowCall in mySystemOtherFlowCallCtxs)
            {
                Assert(otherFlowCall.Length == 2);
                var flow = _system.RootFlows.First(rf => rf.Name == otherFlowCall[0]);
                var cp = flow.CallPrototypes.FirstOrDefault(cp => cp.Name == otherFlowCall[1]);
                var exSeg = flow.RootSegments.FirstOrDefault(cp => cp.Name == otherFlowCall[1]);
                var childName = otherFlowCall.Combine();
                if (cp != null)
                {
                    object instance = new Child(new SubCall(childName, _parenting, cp), _parenting);
                    _parenting.InstanceMap.Add(childName, instance);
                }
                else if (exSeg != null)
                {
                    Console.WriteLine();
                    var segCall = new ExSegment(childName, exSeg);
                    object instance = new Child(segCall, _parenting);
                    _parenting.InstanceMap.Add(childName, instance);
                }
            }
            Console.WriteLine();
        }
    }
}