using Engine.Core;

namespace Engine.Parser
{
    internal class CallInstanceListener : dsBaseListener
    {
        #region Boiler-plates
        public ParserHelper ParserHelper;
        Model _model => ParserHelper.Model;
        DsSystem _system { get => ParserHelper._system; set => ParserHelper._system = value; }
        RootFlow _rootFlow { get => ParserHelper._rootFlow; set => ParserHelper._rootFlow = value; }
        SegmentBase _parenting { get => ParserHelper._parenting; set => ParserHelper._parenting = value; }

        public CallInstanceListener(dsParser parser, ParserHelper helper)
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



        override public void EnterParenting(ParentingContext ctx)
        {
            var name = ctx.identifier1().GetText().DeQuoteOnDemand();
            _parenting = (SegmentBase)_rootFlow.InstanceMap[name];
        }
        override public void ExitParenting(ParentingContext ctx) { _parenting = null; }
        #endregion Boiler-plates



        /// <summary> Flow 바로 밑에 존재하는 "Call;" 형태의 처리</summary>
        /// 
        /// 
        /// <param name="ctx"></param>
        override public void EnterIdentifier1Listing(Identifier1ListingContext ctx)
        {
            // parenting 이 존재할 때는 이미 3.0.SkeletonListener.EnterParenting 에서 이미 수행했으므로 skip
            if (_parenting != null)
                return;

            var callns = collectNameComponents(ctx);
            var callName = callns.Combine();
            var cp = _rootFlow.Find<CallPrototype>(callName);
            if (cp == null)
            {
                // Real listing : 3.0.SkeletonListener 에서 이미 만들어져 있어야 한다.
                Assert(_rootFlow.InstanceMap.ContainsKey(callName));
            }
            else
            {
                var rootCall = new RootCall(callName, _rootFlow, cp);
                _rootFlow.InstanceMap.Add(callName, rootCall);
            }
            Console.WriteLine();
        }

        override public void EnterIdentifier2Listing(Identifier2ListingContext ctx)
        {
            // parenting 이 존재할 때는 추후 3.2.ParentingFillListener.EnterParenting 에서 처리할 것임.
            if (_parenting != null)
                return;

            var ns = collectNameComponents(ctx);
            var name2 = ns.Combine();
            var target = _system.Find(ns);
            switch(target)
            {
                case CallPrototype cp:
                    var rootCall = new RootCall(name2, _rootFlow, cp);
                    _rootFlow.InstanceMap.Add(name2, rootCall);
                    break;

                case SegmentBase seg:
                    var exSeg = new ExSegment(name2, seg);
                    _rootFlow.InstanceMap.Add(name2, exSeg);
                    break;
                default:
                    throw new Exception("ERROR");
            }

            Console.WriteLine();
        }

    }
}