namespace Engine.Parser
{
    internal class ParentingFillListener : dsBaseListener
    {
        #region Boiler-plates
        public ParserHelper ParserHelper;
        Model _model => ParserHelper.Model;
        DsSystem _system { get => ParserHelper._system; set => ParserHelper._system = value; }
        Flow _rootFlow { get => ParserHelper._rootFlow; set => ParserHelper._rootFlow = value; }
        Segment _parenting { get => ParserHelper._parenting; set => ParserHelper._parenting = value; }

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

        override public void EnterFlow(FlowBlockContext ctx)
        {
            var flowName = ctx.identifier1().GetText().DeQuoteOnDemand();
            _rootFlow = _system.Flows.First(f => f.Name == flowName);
        }
        override public void ExitFlow(FlowBlockContext ctx) { _rootFlow = null; }



        override public void ExitParenting(ParentingContext ctx) { _parenting = null; }
        #endregion Boiler-plates


        override public void EnterParenting(ParentingContext ctx)
        {
            var name = ctx.identifier1().GetText().DeQuoteOnDemand();
            _parenting = (Segment)_rootFlow.InstanceMap[name];
            var myFlowCallCtxs = Descendants<Identifier1ListingContext>(ctx).Select(ctx => CollectNameComponents(ctx)[0]).ToArray();
            foreach(var call in myFlowCallCtxs)
            {
                var cp = _rootFlow.CallPrototypes.FirstOrDefault(cp => cp.Name == call);
                var instance = new Child(new SubCall(call, _parenting, cp), _parenting);
                _parenting.InstanceMap.Add(call, instance);
            }

            var mySystemOtherFlowCallCtxs = Descendants<Identifier2ListingContext>(ctx).Select(ctx => CollectNameComponents(ctx).ToArray()).ToArray();   // CollectNameComponents
            foreach(var otherFlowCall in mySystemOtherFlowCallCtxs)
            {
                Assert(otherFlowCall.Length == 2);
                var flow = _system.Flows.First(rf => rf.Name == otherFlowCall[0]);
                var cp = flow.CallPrototypes.FirstOrDefault(cp => cp.Name == otherFlowCall[1]);
                var exSeg = flow.RootSegments.FirstOrDefault(cp => cp.Name == otherFlowCall[1]);
                var childName = otherFlowCall.Combine();
                if (cp != null)
                {
                    var instance = new Child(new SubCall(childName, _parenting, cp), _parenting);
                    _parenting.InstanceMap.Add(childName, instance);
                }
                else if (exSeg != null)
                {
                    var segCall = new ExSegment(childName, exSeg);
                    var instance = new Child(segCall, _parenting);
                    _parenting.InstanceMap.Add(childName, instance);
                }
            }
        }

        /// <summary> Flow 바로 밑에 존재하는 "A.B;" 형태의 처리</summary>
        ///
        ///
        /// <param name="ctx"></param>
        override public void EnterIdentifier2Listing(Identifier2ListingContext ctx)
        {
            // parenting 이 존재할 때의 A.B 처리는 EnterParenting 에서 이미 수행했으므로 skip
            if (_parenting != null)
                return;

            var id2 = CollectNameComponents(ctx);
            var fqdn = id2.Prepend(_system.Name).ToArray();
            var objs = _model.FindAll(fqdn).ToArray();
            var obj = objs.FirstOrDefault();
            if (objs.Length > 1)        // call prototype 과, call 이 동시에 존재하는 경우
                Console.WriteLine();

            switch(obj)
            {
                case Segment seg:
                    break;
                case CallPrototype cp:
                    break;
                default:
                    break;
            }
        }
    }
}