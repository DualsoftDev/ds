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
    DsTask _task { get => ParserHelper._task; set => ParserHelper._task = value; }
    RootFlow _rootFlow { get => ParserHelper._rootFlow; set => ParserHelper._rootFlow = value; }
    SegmentBase _parenting { get => ParserHelper._parenting; set => ParserHelper._parenting = value; }
    /// <summary> Qualified Path Map </summary>
    Dictionary<string, object> QpInstanceMap => ParserHelper.QualifiedInstancePathMap;
    Dictionary<string, object> QpDefinitionMap => ParserHelper.QualifiedDefinitionPathMap;

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
            flowName2CpuNames.ToDictionary(tpl => tpl.Item1, tpl=>tpl.Item2)
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

    override public void EnterSysTask(SysTaskContext ctx)
    {
        var name = ctx.id().GetText();
        _task = new SysTask(name, _system);
        QpInstanceMap.Add(CurrentPath, _task);
    }
    override public void ExitSysTask(SysTaskContext ctx) { _task = null; }

    override public void EnterFlowTask(FlowTaskContext ctx)
    {
        var task = new FlowTask(_rootFlow);
        _rootFlow.FlowTask = task;
        QpInstanceMap.Add(CurrentPath, task);
    }
    override public void ExitFlowTask(FlowTaskContext ctx) { _task = null; }


    override public void EnterFlow(FlowContext ctx)
    {
        var flowName = ctx.id().GetText();
        var flowOf = ctx.flowProp().id();
        var cpu = ParserHelper.FlowName2CpuMap[$"{CurrentPath}.{flowName}"];
        _rootFlow = new RootFlow(cpu, flowName, _system);
        cpu.RootFlows.Add(_rootFlow);
        QpInstanceMap.Add(CurrentPath, _rootFlow);
        Trace.WriteLine($"Flow: {flowName}");
    }
    override public void ExitFlow(FlowContext ctx) { _rootFlow = null; }









    /// <summary>CallPrototype </summary>
    override public void EnterCall(CallContext ctx)
    {
        var name = ctx.id().GetText();
        var label = $"{name}\n{ctx.callPhrase().GetText()}";
        var callph = ctx.callPhrase();

        if (ctx.Parent is FlowTaskContext flowTask)
        {
            Debug.Assert(_task is null);
            var call = new CallPrototype(name, _rootFlow.FlowTask);
            QpDefinitionMap.Add($"{CurrentPath}.{name}", call);
            Console.WriteLine();
        }
        else if (ctx.Parent is SysTaskContext sysTask)
        {
            Debug.Assert(_task is SysTask);
            var call = new CallPrototype(name, (SysTask)_task);
            QpDefinitionMap.Add($"{CurrentPath}.{name}", call);
            Trace.WriteLine($"CALL: {name}");
        }
    }


    override public void EnterListing(ListingContext ctx)
    {
        var name = ctx.id().GetText();
        var seg = SegmentBase.Create(name, _rootFlow);
        QpDefinitionMap.Add($"{CurrentPath}.{name}", seg);
        QpInstanceMap.Add($"{CurrentPath}.{name}", seg);
    }


    override public void EnterParenting(ParentingContext ctx)
    {
        Trace.WriteLine($"Parenting: {ctx.GetText()}");
        var name = ctx.id().GetText();
        _parenting = SegmentBase.Create(name, _rootFlow);
        QpInstanceMap.Add(CurrentPath, _parenting);
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
        foreach(var sys in _model.Systems)
        {
            foreach (var flow in sys.RootFlows)
            {
                foreach (var seg in flow.RootSegments)
                    Assert(seg.Cpu != null && seg.Cpu == flow.Cpu);
            }
        }
    }
}
