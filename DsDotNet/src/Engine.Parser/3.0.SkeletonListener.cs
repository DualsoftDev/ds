using Engine.Common;

namespace Engine.Parser;


/// <summary>
/// ParserSystem, Flow, Task, Cpu
/// Parenting, Listing, CallPrototype, Aliasing 구조까지 생성
/// </summary>
class SkeletonListener : dsBaseListener
{
    public ParserHelper ParserHelper;
    ParserModel _model => ParserHelper.Model;
    ParserSystem _system { get => ParserHelper._system; set => ParserHelper._system = value; }
    ParserRootFlow _rootFlow { get => ParserHelper._rootFlow; set => ParserHelper._rootFlow = value; }
    ParserSegment _parenting { get => ParserHelper._parenting; set => ParserHelper._parenting = value; }

    public SkeletonListener(dsParser parser, ParserHelper helper)
    {
        ParserHelper = helper;
        parser.Reset();
    }


    override public void EnterProgram(ProgramContext ctx)
    {
        var cpuContexts = enumerateChildren<CpuContext>(ctx);

        Dictionary<string, ParserCpu> dict = new();

        ParserCpu createCpu(string cpuName)
        {
            if (dict.ContainsKey(cpuName))
                return dict[cpuName];
            var cpu = new ParserCpu(cpuName, ParserHelper.Model);
            dict[cpuName] = cpu;
            return cpu;
        }
        var flowName2CpuNames =
            from cpuctx in cpuContexts
            let cpuName = cpuctx.id().GetText()
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
        var name = ctx.id().GetText().DeQuoteOnDemand();
        _system = new ParserSystem(name, _model);
        Trace.WriteLine($"ParserSystem: {name}");
    }
    override public void ExitSystem(SystemContext ctx) { _system = null; }


    override public void EnterFlow(FlowContext ctx)
    {
        var flowName = ctx.id().GetText().DeQuoteOnDemand();
        var flowOf = ctx.flowProp().id();

        var flowFqdn = $"{ParserHelper.CurrentPath}.{flowName}";
        var cpuAssigned = ParserHelper.FlowName2CpuMap.ContainsKey(flowFqdn);
        if (!ParserHelper.ParserOptions.IsSimulationMode && !cpuAssigned)
            throw new Exception($"No CPU assignment for flow [{flowFqdn}");

        ParserCpu cpu = null;
        if (cpuAssigned)
            cpu = ParserHelper.FlowName2CpuMap[flowFqdn];
        else
        {
            // simulation mode.
            cpu = new ParserCpu("DummyCpu", _model);
            ParserHelper.FlowName2CpuMap.Add(flowFqdn, cpu);
            if (ParserHelper.FlowName2CpuMap.Values.ForAll(cpu => ! cpu.IsActive))
                cpu.IsActive = true;    // 강제로 active cpu 할당
        }

        var rf = cpu.ParserRootFlows.FirstOrDefault(f => f.Name == flowName);
        if (rf != null)
            throw new Exception($"Duplicated flow name [{flowName}] on {rf.QualifiedName}.");

        _rootFlow = new ParserRootFlow(cpu, flowName, _system);
        cpu.ParserRootFlows.Add(_rootFlow);
    }
    override public void ExitFlow(FlowContext ctx) { _rootFlow = null; }









    /// <summary>CallPrototype </summary>
    override public void EnterCall(CallContext ctx)
    {
        var name = ctx.id().GetText().DeQuoteOnDemand();
        var label = $"{name}\n{ctx.callPhrase().GetText()}";
        var callph = ctx.callPhrase();

        if (_rootFlow.CallPrototypes.Any(cp => cp.Name == name) || _rootFlow.InstanceMap.ContainsKey(name))
            throw new Exception($"Duplicated call definition [{ParserHelper.CurrentPath}.{name}].");

        var call = new CallPrototype(name, _rootFlow);
        Assert(_rootFlow.CallPrototypes.Contains(call));
    }


    override public void EnterListing(ListingContext ctx)
    {
        var name = ctx.id().GetText().DeQuoteOnDemand();
        var seg = ParserSegment.Create(name, _rootFlow);
        if (_rootFlow.CallPrototypes.Any(cp => cp.Name == name) || _rootFlow.InstanceMap.ContainsKey(name))
            throw new Exception($"Duplicated listing [{ParserHelper.CurrentPath}.{name}].");

        _rootFlow.InstanceMap.Add(name, seg);
    }


    override public void EnterParenting(ParentingContext ctx)
    {
        Trace.WriteLine($"Parenting: {ctx.GetText()}");
        var name = ctx.id().GetText().DeQuoteOnDemand();
        _parenting = ParserSegment.Create(name, _rootFlow);

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
                .Select(parentingCtx => parentingCtx.id().GetText().DeQuoteOnDemand())
                .Contains(last);
        if (hasParentingDefinition)
            return;

        // 내부 없는 단순 root segment.  e.g "Vp"
        // @sa EnterListing()
        var seg = ParserSegment.Create(last, _rootFlow);
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
        //var name = ctx.id().GetText();
        //var flowPathContexts =
        //    enumerateChildren<FlowPathContext>(ctx)
        //    ;

        //var flows =
        //    flowPathContexts.Select(fpc =>
        //    {
        //        var systemName = fpc.GetChild(0).GetText();
        //        var dot_ = fpc.GetChild(1).GetText();
        //        var flowName = fpc.GetChild(2).GetText();

        //        var system = _model.ParserSystems.FirstOrDefault(sys => sys.Name == systemName);
        //        var flow = system.ParserRootFlows.FirstOrDefault(f => f.Name == flowName);
        //        return flow;
        //    })
        //    .ToArray()
        //    ;
        //var cpu_ = new Cpu(name, _model) { ParserRootFlows = flows };
    }


    override public void ExitProgram(ProgramContext ctx)
    {
        foreach (var sys in _model.ParserSystems)
        {
            foreach (var flow in sys.ParserRootFlows)
            {
                foreach (var seg in flow.RootParserSegments)
                    Assert(seg.Cpu != null && seg.Cpu == flow.Cpu);
            }
        }
    }
}
