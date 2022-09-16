namespace Engine.Parser;

class ModelListener : dsBaseListener
{
    #region Boiler-plates
    public ParserHelper ParserHelper;
    Model    _model => ParserHelper.Model;
    DsSystem _system    { get => ParserHelper._system;    set => ParserHelper._system = value; }
    RootFlow _rootFlow  { get => ParserHelper._rootFlow;  set => ParserHelper._rootFlow = value; }
    SegmentBase  _parenting { get => ParserHelper._parenting; set => ParserHelper._parenting = value; }
    /// <summary> Qualified Path Map </summary>
    Dictionary<string, object> QpInstanceMap => ParserHelper.QualifiedInstancePathMap;
    Dictionary<string, object> QpDefinitionMap => ParserHelper.QualifiedDefinitionPathMap;

    string[] CurrentPathNameComponents => ParserHelper.CurrentPathNameComponents;
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
                    throw new ParserException("ERROR: CallPrototype expected.", ctx);
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
                        var fullPrototypeName = ParserHelper.ToFQDN(n);
                        if (QpDefinitionMap.ContainsKey(fullPrototypeName))
                        {
                            var def = QpDefinitionMap[fullPrototypeName];
                            createFromDefinition(def, n, fqdn);
                            continue;
                        }
                        //if (n.Contains("."))
                        //{
                        //}
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


    override public void EnterButtons (ButtonsContext ctx)
    {
        var first = findFirstChild<ParserRuleContext>(ctx);
        var targetDic =
            first switch
            {
                EmergencyButtonsContext => _system.EmergencyButtons,
                AutoButtonsContext => _system.AutoButtons,
                StartButtonsContext => _system.StartButtons,
                ResetButtonsContext => _system.ResetButtons,
                _ => throw new Exception("ERROR"),
            };
        var buttonDefs = enumerateChildren<ButtonDefContext>(first).ToArray();
        foreach (var bd in buttonDefs)
        {
            var buttonName = findFirstChild<ButtonNameContext>(bd).GetText();
            var flows = (
                    from flowNameCtx in enumerateChildren<FlowNameContext>(bd)
                    let flowName = flowNameCtx.GetText()
                    let flow = QpInstanceMap[$"{_system.Name}.{flowName}"] as RootFlow
                    select flow
                ).ToArray();
            
            targetDic.Add(buttonName, flows);

            Console.WriteLine();
        }

        Console.WriteLine();
        //EmergencyButtonsContext
        //ctx
        //base.EnterButtons
    }




    override public void ExitProgram(ProgramContext ctx) { }


    // ParseTreeListener<> method
    override public void VisitTerminal(ITerminalNode node) { return; }
    override public void VisitErrorNode(IErrorNode node) { return; }
    override public void EnterEveryRule(ParserRuleContext ctx) { return; }
    override public void ExitEveryRule(ParserRuleContext ctx) { return; }
}
