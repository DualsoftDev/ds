using Engine.Common;

namespace Engine.Parser;


/// <summary>
/// System, Flow, Task, Cpu
/// Parenting, Listing, CallPrototype, 구조까지 생성
/// </summary>
class SkeletonListener : dsBaseListener
{
    public ParserHelper ParserHelper;
    Model _model => ParserHelper.Model;
    DsSystem _system { get => ParserHelper._system; set => ParserHelper._system = value; }
    RootFlow _rootFlow { get => ParserHelper._rootFlow; set => ParserHelper._rootFlow = value; }
    SegmentBase _parenting { get => ParserHelper._parenting; set => ParserHelper._parenting = value; }
    /// <summary> Qualified Path Map </summary>
    Dictionary<(DsSystem, string), object> QpInstanceMap => ParserHelper.QpInstanceMap;
    Dictionary<(DsSystem, string), object> QpDefinitionMap => ParserHelper.QpDefinitionMap;

    string[] CurrentPathNameComponents => ParserHelper.CurrentPathNameComponents;
    string CurrentPath => ParserHelper.CurrentPath;

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
        var n = ctx.id().GetText();
        _system = new DsSystem(n, _model);
        ParserHelper.AliasNameMaps.Add(_system, new Dictionary<string, string>());
        ParserHelper.BackwardAliasMaps.Add(_system, new Dictionary<string, string[]>());
        Trace.WriteLine($"System: {n}");
    }
    override public void ExitSystem(SystemContext ctx) { _system = null; }


    override public void EnterFlow(FlowContext ctx)
    {
        var flowName = ctx.id().GetText();
        var flowOf = ctx.flowProp().id();

        var flowFqdn = $"{CurrentPath}.{flowName}";
        var cpuAssigned = ParserHelper.FlowName2CpuMap.ContainsKey(flowFqdn);
        if (!ParserHelper.ParserOptions.IsSimulationMode && !cpuAssigned)
            throw new Exception($"No CPU assignment for flow [{flowFqdn}");

        Cpu cpu = null;
        if (cpuAssigned)
            cpu = ParserHelper.FlowName2CpuMap[$"{CurrentPath}.{flowName}"];
        else
        {
            // simulation mode.
            cpu = new Cpu("DummyCpu", _model);
            ParserHelper.FlowName2CpuMap.Add(flowFqdn, cpu);
            if (ParserHelper.FlowName2CpuMap.Values.ForAll(cpu => ! cpu.IsActive))
                cpu.IsActive = true;    // 강제로 active cpu 할당
        }

        _rootFlow = new RootFlow(cpu, flowName, _system);
        cpu.RootFlows.Add(_rootFlow);
        QpInstanceMap.Add((_system, CurrentPath), _rootFlow);
        Trace.WriteLine($"Flow: {flowName}");
    }
    override public void ExitFlow(FlowContext ctx) { _rootFlow = null; }









    /// <summary>CallPrototype </summary>
    override public void EnterCall(CallContext ctx)
    {
        var name = ctx.id().GetText();
        var label = $"{name}\n{ctx.callPhrase().GetText()}";
        var callph = ctx.callPhrase();

        var call = new CallPrototype(name, _rootFlow);
        QpDefinitionMap.Add((_system, $"{CurrentPath}.{name}"), call);
    }


    override public void EnterListing(ListingContext ctx)
    {
        var name = ctx.id().GetText();
        var seg = SegmentBase.Create(name, _rootFlow);
        var key = (_system, $"{CurrentPath}.{name}");
        QpDefinitionMap.Add(key, seg);
        QpInstanceMap.Add(key, seg);
    }


    override public void EnterParenting(ParentingContext ctx)
    {
        Trace.WriteLine($"Parenting: {ctx.GetText()}");
        var name = ctx.id().GetText();
        _parenting = SegmentBase.Create(name, _rootFlow);
        QpInstanceMap.Add((_system, CurrentPath), _parenting);
    }
    override public void ExitParenting(ParentingContext ctx) { _parenting = null; }






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
