using Engine.Common;
using Engine.Core;

namespace Engine.Parser;

class ModelListener : dsBaseListener
{
    #region Boiler-plates
    public ParserHelper ParserHelper;
    Model    _model => ParserHelper.Model;
    DsSystem _system    { get => ParserHelper._system;    set => ParserHelper._system = value; }
    RootFlow _rootFlow  { get => ParserHelper._rootFlow;  set => ParserHelper._rootFlow = value; }
    SegmentBase  _parenting { get => ParserHelper._parenting; set => ParserHelper._parenting = value; }

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
        _parenting = (SegmentBase)_rootFlow.InstanceMap[name];
    }
    override public void ExitParenting(ParentingContext ctx) { _parenting = null; }
    #endregion Boiler-plates









    override public void EnterCausalPhrase(CausalPhraseContext ctx)
    {
        var nameComponentss =
            enumerateChildren<SegmentContext>(ctx)
            .Select(collectNameComponents)
            .ToArray()
            ;

        if (_parenting == null)
        {
            void createFromDefinition(object target, string[] ns)
            {
                switch (target)
                {
                    case CallPrototype cp:
                        var name = ns.Last();
                        var call = new RootCall(name, _rootFlow, cp);
                        _rootFlow.InstanceMap.Add(name, call);
                        break;
                    case SegmentBase exSeg:
                        var exSegCall = new ExSegment(ns.Combine(), exSeg);
                        _rootFlow.InstanceMap.Add(ns.Combine(), exSegCall);
                        break;
                    default:
                        throw new ParserException("ERROR: CallPrototype expected.", ctx);
                }
            }

            foreach (var ns in nameComponentss)
            {
                var name = ns.Combine();
                switch (ns.Length)
                {
                    case 1:
                        var fqdn = ParserHelper.GetCurrentPathComponents(name);
                        if (_rootFlow.AliasNameMaps.ContainsKey(ns))
                        {
                            var targetName = _rootFlow.AliasNameMaps[fqdn];
                            var target = _model.Find(targetName);
                            createFromDefinition(target, fqdn);
                        }
                        else
                        {
                            if (!_rootFlow.InstanceMap.ContainsKey(name))
                            {
                                var fullPrototypeName = ParserHelper.ToFQDN(ns);
                                var target = _model.Find(fullPrototypeName);
                                if (target != null)
                                {
                                    createFromDefinition(target, fqdn);
                                    continue;
                                }

                                var seg = SegmentBase.Create(name, _rootFlow);
                                _rootFlow.InstanceMap.Add(name, seg);
                            }
                        }
                        break;
                    case 3:     // 외부 real 호출
                        var def = _model.Find(ns);
                        createFromDefinition(def, ns);
                        break;
                }
            }
        }
        else
        {
            void createFromDefinition(object target, string[] ns, bool isAlias)
            {
                switch (target)
                {
                    case CallPrototype cp:
                        var name = ns.Last();
                        if (! _parenting.InstanceMap.ContainsKey(name))
                        {
                            var subCall = new SubCall(name, _parenting, cp);
                            var child = new Child(subCall, _parenting) { IsAlias = isAlias };
                            subCall.ContainerChild = child;
                            _parenting.InstanceMap.Add(name, child);
                        }
                        break;

                    case SegmentBase exSeg:
                        if (!_parenting.InstanceMap.ContainsKey(ns.Combine()))
                        {
                            var exSegCall = new ExSegment(ns.Combine(), exSeg);
                            var child = new Child(exSegCall, _parenting) { IsAlias = isAlias };
                            exSegCall.ContainerChild = child;
                            _parenting.InstanceMap.Add(ns.Combine(), child);
                        }
                        break;

                    default:
                        throw new ParserException("ERROR: CallPrototype expected.", ctx);
                }
            }

            foreach (var ns in nameComponentss)
            {
                var name = ns.Combine();
                switch (ns.Length)
                {
                    case 1:
                        var fqdn = ParserHelper.GetCurrentPathComponents(name);
                        if (_rootFlow.AliasNameMaps.ContainsKey(ns))
                        {
                            var targetName = _rootFlow.AliasNameMaps[ns];
                            var target = _model.Find(targetName);
                            createFromDefinition(target, fqdn, true);
                        }
                        else
                        {
                            if (!_rootFlow.InstanceMap.ContainsKey(name))
                            {
                                var fullPrototypeName = ParserHelper.ToFQDN(ns);
                                var target = _model.Find(fullPrototypeName);
                                if (target != null)
                                {
                                    createFromDefinition(target, fqdn, false);
                                    continue;
                                }

                                var seg = SegmentBase.Create(name, _rootFlow);
                                _rootFlow.InstanceMap.Add(name, seg);
                            }
                        }
                        break;
                    case 3:     // 외부 real 호출
                        var def = _model.Find(ns);
                        createFromDefinition(def, ns, false);
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
        var first = findFirstChild<ParserRuleContext>(ctx);     // {Emergency, Auto, Start, Reset}ButtonsContext
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

        var buttonDefs = enumerateChildren<ButtonDefContext>(first).ToArray();
        foreach (var bd in buttonDefs)
        {
            var buttonName = findFirstChild<ButtonNameContext>(bd).GetText();
            var flows = (
                    from flowNameCtx in enumerateChildren<FlowNameContext>(bd)
                    let flowName = flowNameCtx.GetText()
                    let flow = _system.RootFlows.First(rf => rf.Name == flowName)
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
