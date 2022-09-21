using Engine.Common;

namespace Engine.Parser
{
    class SingletonCreateListener : dsBaseListener
    {
        #region Boiler-plates
        public ParserHelper ParserHelper;
        Model _model => ParserHelper.Model;
        DsSystem _system { get => ParserHelper._system; set => ParserHelper._system = value; }
        RootFlow _rootFlow { get => ParserHelper._rootFlow; set => ParserHelper._rootFlow = value; }
        SegmentBase _parenting { get => ParserHelper._parenting; set => ParserHelper._parenting = value; }

        string[] CurrentPathNameComponents => ParserHelper.CurrentPathNameComponents;
        string CurrentPath => ParserHelper.CurrentPath;

        public SingletonCreateListener(dsParser parser, ParserHelper helper)
        {
            ParserHelper = helper;
            parser.Reset();
        }

        override public void EnterSystem(SystemContext ctx)
        {
            var name = ctx.id().GetText().DeQuoteOnDemand();
            _system = _model.Systems.First(s => s.Name == name);
        }
        override public void ExitSystem(SystemContext ctx) { this._system = null; }

        override public void EnterFlow(FlowContext ctx)
        {
            var flowName = ctx.id().GetText().DeQuoteOnDemand();
            _rootFlow = _system.RootFlows.First(f => f.Name == flowName);
        }
        override public void ExitFlow(FlowContext ctx) { _rootFlow = null; }



        override public void EnterParenting(ParentingContext ctx)
        {
            var name = ctx.id().GetText().DeQuoteOnDemand();
            _parenting = (SegmentBase)_rootFlow.InstanceMap[name];
        }
        override public void ExitParenting(ParentingContext ctx) { _parenting = null; }
        #endregion Boiler-plates





    }
}
