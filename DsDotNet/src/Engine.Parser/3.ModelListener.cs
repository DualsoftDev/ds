namespace Engine.Parser;

class ModelListener : dsBaseListener
{
    #region Boiler-plates
    public ParserHelper ParserHelper;
    Model    _model => ParserHelper.Model;
    DsSystem _system    { get => ParserHelper._system;    set => ParserHelper._system = value; }
    DsTask   _task      { get => ParserHelper._task;      set => ParserHelper._task = value; }
    RootFlow _rootFlow  { get => ParserHelper._rootFlow;  set => ParserHelper._rootFlow = value; }
    SegmentBase  _parenting { get => ParserHelper._parenting; set => ParserHelper._parenting = value; }
    /// <summary> Qualified Path Map </summary>
    Dictionary<string, object> QpInstanceMap => ParserHelper.QualifiedInstancePathMap;
    Dictionary<string, object> QpDefinitionMap => ParserHelper.QualifiedDefinitionPathMap;

    string CurrentPath => ParserHelper.CurrentPath;

    public ModelListener(dsParser parser, ParserHelper helper)
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

    override public void EnterTask(TaskContext ctx)
    {
        var name = ctx.id().GetText();
        _task = _system.Tasks.First(t => t.Name == name);
        Trace.WriteLine($"Task: {name}");
    }
    override public void ExitTask(TaskContext ctx) { _task = null; }

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









    override public void EnterCausalPhrase(CausalPhraseContext ctx)
    {
        var names =
            enumerateChildren<SegmentContext>(ctx)
            .Select(segCtx => segCtx.GetText())
            ;

        void createFromDefinition(object target, string n, string fqdn)
        {
            switch (target)
            {
                case CallPrototype cp:
                    var call = new RootCall(n, _rootFlow, cp);
                    QpInstanceMap.Add(fqdn, call);
                    break;
                default:
                    throw new Exception("ERROR");
            }
        }

        if (_parenting == null)
        {
            foreach (var n in names)
            {
                var fqdn = $"{CurrentPath}.{n}";
                if (ParserHelper.AliasNameMaps[_system].ContainsKey(n))
                {
                    var targetName = ParserHelper.AliasNameMaps[_system][n];
                    var target = QpDefinitionMap[targetName];
                    createFromDefinition(target, n, fqdn);
                }
                else
                {
                    if (!QpInstanceMap.ContainsKey(fqdn))
                    {
                        if (n.Contains("."))
                        {
                            var fullPrototypeName = ParserHelper.ToFQDN(n);
                            if (QpDefinitionMap.ContainsKey(fullPrototypeName))
                            {
                                var def = QpDefinitionMap[fullPrototypeName];
                                createFromDefinition(def, n, fqdn);
                                continue;
                            }
                        }
                        var seg = SegmentBase.Create(n, _rootFlow);
                        QpInstanceMap.Add(fqdn, seg);
                    }
                }
            }
        }
    }



    override public void EnterCausals(CausalsContext ctx)
    {
        Trace.WriteLine($"Causals: {ctx.GetText()}");
    }
    //override public void ExitCausals(CausalsContext ctx) {}






    override public void ExitProgram(ProgramContext ctx) {}


    // ParseTreeListener<> method
    override public void VisitTerminal(ITerminalNode node)     { return; }
    override public void VisitErrorNode(IErrorNode node)        { return; }
    override public void EnterEveryRule(ParserRuleContext ctx) { return; }
    override public void ExitEveryRule(ParserRuleContext ctx) { return; }
}
