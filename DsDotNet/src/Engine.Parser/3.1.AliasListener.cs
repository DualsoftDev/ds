namespace Engine.Parser
{
    class AliasListener : dsBaseListener
    {
        #region Boiler-plates
        public ParserHelper ParserHelper;
        Model _model => ParserHelper.Model;
        DsSystem _system { get => ParserHelper._system; set => ParserHelper._system = value; }
        DsTask _task { get => ParserHelper._task; set => ParserHelper._task = value; }
        RootFlow _rootFlow { get => ParserHelper._rootFlow; set => ParserHelper._rootFlow = value; }
        SegmentBase _parenting { get => ParserHelper._parenting; set => ParserHelper._parenting = value; }
        /// <summary> Qualified Path Map </summary>
        Dictionary<string, object> QpInstanceMap => ParserHelper.QualifiedInstancePathMap;
        Dictionary<string, object> QpDefinitionMap => ParserHelper.QualifiedDefinitionPathMap;

        string CurrentPath => ParserHelper.CurrentPath;

        public AliasListener(dsParser parser, ParserHelper helper)
        {
            ParserHelper = helper;
            parser.Reset();
        }

        override public void EnterSystem(SystemContext ctx)
        {
            var name = ctx.id().GetText();
            _system = _model.Systems.First(s => s.Name == name);
        }
        override public void ExitSystem(SystemContext ctx) { this._system = null; }

        override public void EnterSysTask(SysTaskContext ctx)
        {
            var name = ctx.id().GetText();
            _task = _system.Tasks.First(t => t.Name == name);
            Trace.WriteLine($"Task: {name}");
        }
        override public void ExitSysTask(SysTaskContext ctx) { _task = null; }

        override public void EnterFlow(FlowContext ctx)
        {
            var flowName = ctx.id().GetText();
            _rootFlow = _system.RootFlows.First(f => f.Name == flowName);
        }
        override public void ExitFlow(FlowContext ctx) { _rootFlow = null; }



        override public void EnterParenting(ParentingContext ctx)
        {
            var name = ctx.id().GetText();
            _parenting = (SegmentBase)QpInstanceMap[$"{CurrentPath}.{name}"];
        }
        override public void ExitParenting(ParentingContext ctx) { _parenting = null; }
        #endregion Boiler-plates




        /*
            [alias] = {
                P.F.Vp = { Vp1; Vp2; Vp3; }
            }
         */
        override public void EnterAliasListing(AliasListingContext ctx)
        {
            var def = ctx.aliasDef().GetText(); // e.g "P.F.Vp"
            var aliasMnemonics =    // e.g { Vp1; Vp2; Vp3; }
                enumerateChildren<AliasMnemonicContext>(ctx)
                .Select(mne => mne.GetText())
                .ToArray()
                ;
            Assert(aliasMnemonics.Length == aliasMnemonics.Distinct().Count());

            //var defInstance = _model.FindObject<object>(def);

            ParserHelper.BackwardAliasMaps[_system].Add(def, aliasMnemonics);
        }
        override public void ExitAlias(AliasContext ctx)
        {
            var bwd = ParserHelper.BackwardAliasMaps[_system];
            Assert(ParserHelper.AliasNameMaps[_system].Count() == 0);
            Assert(bwd.Values.Count() == bwd.Values.Distinct().Count());
            var reversed =
                from tpl in bwd
                let k = tpl.Key
                from v in tpl.Value
                select (v, k)
                ;

            foreach ((var mnemonic, var target) in reversed)
                ParserHelper.AliasNameMaps[_system].Add(mnemonic, target);
        }
    }
}
