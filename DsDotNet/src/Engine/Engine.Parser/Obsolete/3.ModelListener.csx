using Engine.Core;

namespace Engine.Parser;

class ModelListener : dsBaseListener
{
    #region Boiler-plates
    public ParserHelper ParserHelper;
    Model    _model => ParserHelper.Model;
    DsSystem _system    { get => ParserHelper._system;    set => ParserHelper._system = value; }
    Flow _rootFlow  { get => ParserHelper._rootFlow;  set => ParserHelper._rootFlow = value; }
    Segment  _parenting { get => ParserHelper._parenting; set => ParserHelper._parenting = value; }

    public ModelListener(dsParser parser, ParserHelper helper)
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



    override public void EnterParenting(ParentingContext ctx)
    {
        var name = ctx.identifier1().GetText().DeQuoteOnDemand();
        _parenting = (Segment)_rootFlow.InstanceMap[name];
    }
    override public void ExitParenting(ParentingContext ctx) { _parenting = null; }
    #endregion Boiler-plates





    override public void EnterCausalToken(CausalTokenContext ctx)
    {
        var container = (Flow)_parenting ?? _rootFlow;
        var instanceMap = container.InstanceMap;
        var ns = CollectNameComponents(ctx);
        var fqdn = ns.Combine();
        if (instanceMap.ContainsKey(fqdn))
            return;

        void createInstanceFromCallPrototype(CallPrototype cp, string callName, Dictionary<string, IParserObject> instanceMap)
        {
            IParserObject instance =
                _parenting == null
                ? new RootCall(callName, _rootFlow, cp)
                : new Child(new SubCall(callName, _parenting, cp), _parenting)
                ;
            instanceMap.Add(callName, instance);
        }

        switch (ns.Length)
        {
            case 1:
                var last = fqdn.DeQuoteOnDemand();
                if (_rootFlow.AliasNameMaps.ContainsKey(last))
                {
                    var aliasTarget = _rootFlow.AliasNameMaps[last];
                    var target = _model.FindFirst(aliasTarget);

                    switch (target)
                    {
                        case CallPrototype cp:
                            createInstanceFromCallPrototype(cp, last, instanceMap);
                            break;
                        default:
                            throw new ParserError("ERROR: CallPrototype expected.", ctx);
                    }
                }
                else
                {
                    var cp = _rootFlow.CallPrototypes.FirstOrDefault(cp => cp.Name == last);
                    if (cp != null)
                        createInstanceFromCallPrototype(cp, last, instanceMap);
                    else if (_parenting == null)
                        Assert(_rootFlow.InstanceMap.ContainsKey(last));
                    else
                        Assert(false);

                }
                break;
            case 2:
                // A.B => my_system_other_flow.{call, real}
                {
                    var targets = _model.FindAll(ParserHelper.CurrentPathNameComponents.Concat(ns).ToArray());
                    var target = targets.FirstOrDefault();
                    if (targets.Count() > 1)    // 복수 존재시, call def 를 우선
                    {
                        target = targets.OfType<CallPrototype>().FirstOrDefault();
                        if (target == null)
                            throw new Exception("ERROR");
                    }

                    //var (flowName, lastName) = (ns[0], ns[1]);
                    //var flow = _system.Flows.FirstOrDefault(rf => rf.Name == flowName);
                    //var targets = flow.FindAll(ns[1]).ToArray();    // call def 과 call instance 둘다 존재할 수 있다.
                    //var target =
                    //    targets.Length > 1
                    //    ? flow.Find<CallPrototype>(ns[1])
                    //    : targets.FirstOrDefault();
                    switch (target)
                    {
                        case null:
                            throw new ParserError($"ERROR : failed to find [{ns.Combine()}]", ctx);

                        case Segment exSeg: //when _parenting != null:
                            var exSegCall = new ExSegment(ns.Combine(), exSeg);
                            if (_parenting == null)
                                instanceMap.Add(ns.Combine(), exSegCall);
                            else
                            {
                                var child = new Child(exSegCall, _parenting);
                                instanceMap.Add(ns.Combine(), child);
                            }
                            break;

                        case CallPrototype cp:
                            createInstanceFromCallPrototype(cp, ns.Combine(), instanceMap);
                            break;

                        default:
                            throw new ParserError("ERROR : unknown??.", ctx);
                    }
                }
                break;
            default:
                throw new Exception("ERROR");
        }
    }


    override public void EnterCausals(CausalsContext ctx)
    {
        Debug.WriteLine($"Causals: {ctx.GetText()}");
    }
    //override public void ExitCausals(CausalsContext ctx) {}


    override public void EnterButtons (ButtonsContext ctx)
    {
        var first = TryFindFirstChild<ParserRuleContext>(ctx);     // {Emergency, Auto, Start, Reset}ButtonsContext
        var targetDic =
            first switch
            {
                EmergencyButtonsContext => _system.EmergencyButtons,
                AutoButtonsContext => _system.AutoButtons,
                StartButtonsContext => _system.StartButtons,
                ResetButtonsContext => _system.ResetButtons,
                _ => throw new Exception("ERROR"),
            };

        var category = first.GetChild(1).GetText();       // [| '[', category, ']', buttonBlock |] 에서 category 만 추려냄 (e.g 'emg')
        var key = (_system, category);
        if (ParserHelper.ButtonCategories.Contains(key))
            throw new Exception($"Duplicated button category {category} near {ctx.GetText()}");
        else
            ParserHelper.ButtonCategories.Add(key);

        var buttonDefs = Descendants<ButtonDefContext>(first).ToArray();
        foreach (var bd in buttonDefs)
        {
            var buttonName = TryFindFirstChild<ButtonNameContext>(bd).GetText();
            var flows = (
                    from flowNameCtx in Descendants<FlowNameContext>(bd)
                    let flowName = flowNameCtx.GetText()
                    let flow = _system.Flows.First(rf => rf.Name == flowName)
                    select flow
                ).ToArray();

            var duplicatedFlows = flows.FindDuplicates();
            if (duplicatedFlows.Any())
            {
                var dupNames = string.Join(", ", duplicatedFlows.Select(flow => flow.QualifiedName));
                throw new Exception($"Duplicated flow(s) {dupNames} near {ctx.GetText()}.");
            }

            if (targetDic.ContainsKey(buttonName))
                throw new Exception($"Duplicated button name [{buttonName}] near {ctx.GetText()}");
            targetDic.Add(buttonName, flows);
        }
    }




    //override public void ExitProgram(ProgramContext ctx) { }


    //// ParseTreeListener<> method
    //override public void VisitTerminal(ITerminalNode node) { return; }
    //override public void VisitErrorNode(IErrorNode node) { return; }
    //override public void EnterEveryRule(ParserRuleContext ctx) { return; }
    //override public void ExitEveryRule(ParserRuleContext ctx) { return; }
}
