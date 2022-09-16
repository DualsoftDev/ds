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
    Dictionary<(DsSystem, string), object> QpInstanceMap => ParserHelper.QpInstanceMap;
    Dictionary<(DsSystem, string), object> QpDefinitionMap => ParserHelper.QpDefinitionMap;

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
        _parenting = (SegmentBase)QpInstanceMap[(_system, $"{CurrentPath}.{name}")];
    }
    override public void ExitParenting(ParentingContext ctx) { _parenting = null; }
    #endregion Boiler-plates









    override public void EnterCausalPhrase(CausalPhraseContext ctx)
    {
        var nameComponentss =
            enumerateChildren<SegmentContext>(ctx)
            .Select(segCtx => enumerateChildren<IdentifierContext>(segCtx).Select(idf => idf.GetText()).ToArray())
            .ToArray()
            ;

        void createFromDefinition(object target, string n, string fqdn)
        {
            switch (target)
            {
                case CallPrototype cp:
                    var call = new RootCall(n, _rootFlow, cp);
                    QpInstanceMap.Add((_system, fqdn), call);
                    break;
                case SegmentBase exSeg:
                    var exSegCall = new ExSegment(fqdn, exSeg);
                    QpInstanceMap.Add((_system, fqdn), exSegCall);
                    break;
                default:
                    throw new ParserException("ERROR: CallPrototype expected.", ctx);
            }
        }

        if (_parenting == null)
        {
            foreach (var ns in nameComponentss)
            {
                var name = ns.Combine();
                switch (ns.Length)
                {
                    case 1:
                        {
                            var fqdn = $"{CurrentPath}.{name}";
                            if (ParserHelper.AliasNameMaps[_system].ContainsKey(name))
                            {
                                var targetName = ParserHelper.AliasNameMaps[_system][name];
                                var target = QpDefinitionMap[(_system, targetName)];
                                createFromDefinition(target, name, fqdn);
                            }
                            else
                            {
                                if (!QpInstanceMap.ContainsKey((_system, fqdn)))
                                {
                                    var fullPrototypeName = ParserHelper.ToFQDN(name);
                                    if (QpDefinitionMap.ContainsKey((_system, fullPrototypeName)))
                                    {
                                        var def = QpDefinitionMap[(_system, fullPrototypeName)];
                                        createFromDefinition(def, name, fqdn);
                                        continue;
                                    }
                                    var seg = SegmentBase.Create(name, _rootFlow);
                                    QpInstanceMap.Add((_system, fqdn), seg);
                                }
                            }
                        }

                        break;
                    case 3:     // 외부 real 호출
                        {
                            var fqdn = ns.Combine();
                            Assert(_system.Name != ns[0]);
                            var exSystem = _model.Systems.First(s => s.Name == ns[0]);
                            var def = QpDefinitionMap[(exSystem, fqdn)];
                            createFromDefinition(def, name, fqdn);
                        }
                        break;
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
                    let flow = QpInstanceMap[(_system, $"{_system.Name}.{flowName}")] as RootFlow
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
