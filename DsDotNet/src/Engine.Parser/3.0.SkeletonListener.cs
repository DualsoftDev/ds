using Engine.Common;

namespace Engine.Parser;


/// <summary>
/// System, Flow, Task, Cpu
/// Parenting(껍데기만), Listing(root flow toplevel 만),
/// CallPrototype, Aliasing 구조까지 생성
/// </summary>
class SkeletonListener : dsBaseListener
{
    public ParserHelper ParserHelper;
    Model _model => ParserHelper.Model;
    DsSystem _system { get => ParserHelper._system; set => ParserHelper._system = value; }
    RootFlow _rootFlow { get => ParserHelper._rootFlow; set => ParserHelper._rootFlow = value; }
    SegmentBase _parenting { get => ParserHelper._parenting; set => ParserHelper._parenting = value; }

    public SkeletonListener(dsParser parser, ParserHelper helper)
    {
        ParserHelper = helper;
        parser.Reset();
    }


    override public void EnterProgram(ProgramContext ctx)
    {
        var cpuContexts = enumerateChildren<CpuContext>(ctx);

        Dictionary<string, Cpu> dict = new();

        Cpu createCpu(string cpuName)
        {
            if (dict.ContainsKey(cpuName))
                return dict[cpuName];
            var cpu = new Cpu(cpuName, ParserHelper.Model);
            dict[cpuName] = cpu;
            return cpu;
        }
        var flowName2CpuNames =
            from cpuctx in cpuContexts
            let cpuName = cpuctx.identifier1().GetText()
            from flowCtx in enumerateChildren<FlowPathContext>(cpuctx)
            let flowName = flowCtx.GetText()
            select (flowName, createCpu(cpuName))
            ;

        var flowName2CpuNameMap =
            flowName2CpuNames.ToDictionary(tpl => tpl.Item1, tpl => tpl.Item2)
            ;

        ParserHelper.FlowName2CpuMap = flowName2CpuNameMap;
    }

    override public void EnterSystem(SystemContext ctx)
    {
        var name = ctx.identifier1().GetText().DeQuoteOnDemand();
        _system = new DsSystem(name, _model);
        Trace.WriteLine($"System: {name}");
    }
    override public void ExitSystem(SystemContext ctx) { _system = null; }


    override public void EnterFlow(FlowContext ctx)
    {
        var flowName = ctx.identifier1().GetText().DeQuoteOnDemand();
        var flowOf = ctx.flowProp().identifier1();

        var flowFqdn = $"{ParserHelper.CurrentPath}.{flowName}";
        var cpuAssigned = ParserHelper.FlowName2CpuMap.ContainsKey(flowFqdn);
        if (!ParserHelper.ParserOptions.IsSimulationMode && !cpuAssigned)
            throw new Exception($"No CPU assignment for flow [{flowFqdn}");

        Cpu cpu = null;
        if (cpuAssigned)
            cpu = ParserHelper.FlowName2CpuMap[flowFqdn];
        else
        {
            // simulation mode.
            cpu = new Cpu("DummyCpu", _model);
            ParserHelper.FlowName2CpuMap.Add(flowFqdn, cpu);
            if (ParserHelper.FlowName2CpuMap.Values.ForAll(cpu => ! cpu.IsActive))
                cpu.IsActive = true;    // 강제로 active cpu 할당
        }

        var rf = cpu.RootFlows.FirstOrDefault(f => f.Name == flowName);
        if (rf != null)
            throw new Exception($"Duplicated flow name [{flowName}] on {rf.QualifiedName}.");

        _rootFlow = new RootFlow(cpu, flowName, _system);
        cpu.RootFlows.Add(_rootFlow);
    }
    override public void ExitFlow(FlowContext ctx) { _rootFlow = null; }









    /// <summary>CallPrototype </summary>
    override public void EnterCall(CallContext ctx)
    {
        var name = ctx.identifier1().GetText().DeQuoteOnDemand();
        var label = $"{name}\n{ctx.callPhrase().GetText()}";
        var callph = ctx.callPhrase();

        if (_rootFlow.CallPrototypes.Any(cp => cp.Name == name) || _rootFlow.InstanceMap.ContainsKey(name))
            throw new Exception($"Duplicated call definition [{ParserHelper.CurrentPath}.{name}].");

        var call = new CallPrototype(name, _rootFlow);
        Assert(_rootFlow.CallPrototypes.Contains(call));
    }


    override public void EnterIdentifier1Listing(Identifier1ListingContext ctx)
    {
        if (_parenting != null)
            return;

        var name = ctx.identifier1().GetText().DeQuoteOnDemand();
        var seg = SegmentBase.Create(name, _rootFlow);
        if (_rootFlow.CallPrototypes.Any(cp => cp.Name == name) || _rootFlow.InstanceMap.ContainsKey(name))
            throw new Exception($"Duplicated listing [{ParserHelper.CurrentPath}.{name}].");

        _rootFlow.InstanceMap.Add(name, seg);
    }


    override public void EnterParenting(ParentingContext ctx)
    {
        Trace.WriteLine($"Parenting: {ctx.GetText()}");
        var name = ctx.identifier1().GetText().DeQuoteOnDemand();
        _parenting = SegmentBase.Create(name, _rootFlow);

        if (_rootFlow.InstanceMap.ContainsKey(name))
            throw new Exception($"Duplicated parenting name [{ParserHelper.CurrentPath}] on {_rootFlow.QualifiedName}.");
        _rootFlow.InstanceMap.Add(name, _parenting);
    }
    override public void ExitParenting(ParentingContext ctx) { _parenting = null; }


    // parenting 이 아닌, root flow 하단의 단일 root segment 는 우선 생성
    override public void EnterCausalToken(CausalTokenContext ctx)
    {
        if (_parenting != null)
            return;

        var ns = collectNameComponents(ctx);
        if (ns.Length > 1)
            return;

        var last = ns[0].DeQuoteOnDemand();
        if (_rootFlow.AliasNameMaps.ContainsKey(ns[0]))
            return;

        if (_rootFlow.CallPrototypes.Any(cp => cp.Name == last))
            return;

        if (_rootFlow.InstanceMap.ContainsKey(last))
            return;

        // 같은 이름의 parenting 이 존재하면, 내부가 존재하는 root segemnt 이므로, skip
        var flowContext = findFirstAncestor<FlowContext>(ctx);
        var hasParentingDefinition =
            enumerateChildren<ParentingContext>(flowContext)
                .Select(parentingCtx => parentingCtx.identifier1().GetText().DeQuoteOnDemand())
                .Contains(last);
        if (hasParentingDefinition)
            return;

        var hasCallPrototypeDefinition =
            enumerateChildren<CallContext>(flowContext)
                .Select(callCtx => callCtx.identifier1().GetText().DeQuoteOnDemand())
                .Contains(last);
        if (hasCallPrototypeDefinition)
            return;

        // 내부 없는 단순 root segment.  e.g "Vp"
        // @sa EnterListing()
        var seg = SegmentBase.Create(last, _rootFlow);
        _rootFlow.InstanceMap.Add(last, seg);
    }


    /*
        [alias] = {
            Ap = { Ap1; Ap2; Ap3; }
        }
     */
    override public void EnterAliasListing(AliasListingContext ctx)
    {
        var defs = collectNameComponents(ctx.aliasDef()); // e.g "P.F.Vp" -> [| "P"; "F"; "Vp" |]
        var aliasMnemonics =    // e.g { Vp1; Vp2; Vp3; }
            enumerateChildren<AliasMnemonicContext>(ctx)
            .Select(mne => collectNameComponents(mne))
            .Do(ns => Assert(ns.Count() == 1))      // Vp1 등은 '.' 허용 안함
            .Select(ns => ns[0])
            .ToArray()
            ;
        Assert(aliasMnemonics.Length == aliasMnemonics.Distinct().Count());

        var def = (
            defs.Length switch
            {
                1 => ParserHelper.GetCurrentPathComponents(defs[0]),
                2 when defs[0] != _system.Name => defs.Prepend(_system.Name).ToArray(),
                3 => defs,
                _ => throw new Exception("ERROR"),
            });


        _rootFlow.BackwardAliasMaps.Add(def, aliasMnemonics);
    }
    override public void ExitAlias(AliasContext ctx)
    {
        var bwd = _rootFlow.BackwardAliasMaps;
        Assert(_rootFlow.AliasNameMaps.Count() == 0);
        Assert(bwd.Values.Count() == bwd.Values.Distinct().Count());
        var reversed =
            from tpl in bwd
            let k = tpl.Key
            from v in tpl.Value
            select (v, k)
            ;

        foreach ((var mnemonic, var target) in reversed)
            _rootFlow.AliasNameMaps.Add(mnemonic, target);
    }


    override public void EnterCpu(CpuContext ctx)
    {
        //var name = ctx.identifier1().GetText();
        //var flowPathContexts =
        //    enumerateChildren<FlowPathContext>(ctx)
        //    ;

        //var flows =
        //    flowPathContexts.Select(fpc =>
        //    {
        //        var systemName = fpc.GetChild(0).GetText();
        //        var dot_ = fpc.GetChild(1).GetText();
        //        var flowName = fpc.GetChild(2).GetText();

        //        var system = _model.Systems.FirstOrDefault(sys => sys.Name == systemName);
        //        var flow = system.RootFlows.FirstOrDefault(f => f.Name == flowName);
        //        return flow;
        //    })
        //    .ToArray()
        //    ;
        //var cpu_ = new Cpu(name, _model) { RootFlows = flows };
    }


    override public void ExitProgram(ProgramContext ctx)
    {
        foreach (var sys in _model.Systems)
        {
            foreach (var flow in sys.RootFlows)
            {
                foreach (var seg in flow.RootSegments)
                    Assert(seg.Cpu != null && seg.Cpu == flow.Cpu);
            }
        }
    }
}
