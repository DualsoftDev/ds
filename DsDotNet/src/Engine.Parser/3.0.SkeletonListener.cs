using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Antlr4.Runtime.Misc;

using Engine.Core;

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


    override public void EnterProgram(dsParser.ProgramContext ctx)
    {
        var cpuContexts =
            DsParser.enumerateChildren<dsParser.CpuContext>(ctx, false, r => r is dsParser.CpuContext)
            ;

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
            from flowCtx in DsParser.enumerateChildren<dsParser.FlowPathContext>(cpuctx, false, r => r is dsParser.FlowPathContext)
            let flowName = flowCtx.GetText()
            select (flowName, createCpu(cpuName))
            ;

        var flowName2CpuNameMap =
            flowName2CpuNames.ToDictionary(tpl => tpl.Item1, tpl=>tpl.Item2)
            ;

        ParserHelper.FlowName2CpuMap = flowName2CpuNameMap;
    }

    override public void EnterSystem(dsParser.SystemContext ctx)
    {
        var n = ctx.id().GetText();
        _system = new DsSystem(n, _model);
        ParserHelper.AliasNameMaps.Add(_system, new Dictionary<string, string>());
        ParserHelper.BackwardAliasMaps.Add(_system, new Dictionary<string, string[]>());
        Trace.WriteLine($"System: {n}");
    }
    override public void ExitSystem(dsParser.SystemContext ctx) { _system = null; }

    override public void EnterTask(dsParser.TaskContext ctx)
    {
        var name = ctx.id().GetText();
        _task = new DsTask(name, _system);
        QpInstanceMap.Add(CurrentPath, _task);
    }
    override public void ExitTask(dsParser.TaskContext ctx) { _task = null; }

    override public void EnterFlow(dsParser.FlowContext ctx)
    {
        var flowName = ctx.id().GetText();
        var flowOf = ctx.flowProp().id();
        var cpu = ParserHelper.FlowName2CpuMap[$"{CurrentPath}.{flowName}"];
        _rootFlow = new RootFlow(cpu, flowName, _system);
        cpu.RootFlows.Add(_rootFlow);
        QpInstanceMap.Add(CurrentPath, _rootFlow);
        Trace.WriteLine($"Flow: {flowName}");
    }
    override public void ExitFlow(dsParser.FlowContext ctx) { _rootFlow = null; }









    /// <summary>CallPrototype </summary>
    override public void EnterCall(dsParser.CallContext ctx)
    {
        var name = ctx.id().GetText();
        var label = $"{name}\n{ctx.callPhrase().GetText()}";
        var callph = ctx.callPhrase();
        var call = new CallPrototype(name, _task);
        QpDefinitionMap.Add($"{CurrentPath}.{name}", call);
        Trace.WriteLine($"CALL: {name}");
    }


    override public void EnterListing(dsParser.ListingContext ctx)
    {
        var name = ctx.id().GetText();
        var seg = SegmentBase.Create(name, _rootFlow);
        QpDefinitionMap.Add($"{CurrentPath}.{name}", seg);
        QpInstanceMap.Add($"{CurrentPath}.{name}", seg);
    }


    override public void EnterParenting(dsParser.ParentingContext ctx)
    {
        Trace.WriteLine($"Parenting: {ctx.GetText()}");
        var name = ctx.id().GetText();
        _parenting = SegmentBase.Create(name, _rootFlow);
        QpInstanceMap.Add(CurrentPath, _parenting);
    }
    override public void ExitParenting(dsParser.ParentingContext ctx) { _parenting = null; }






    override public void EnterCpu(dsParser.CpuContext ctx)
    {
        //var name = ctx.id().GetText();
        //var flowPathContexts =
        //    DsParser.enumerateChildren<dsParser.FlowPathContext>(ctx, false, r => r is dsParser.FlowPathContext)
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


    override public void ExitProgram(dsParser.ProgramContext ctx)
    {
        foreach(var sys in _model.Systems)
        {
            foreach (var flow in sys.RootFlows)
            {
                foreach (var seg in flow.RootSegments)
                    Debug.Assert(seg.Cpu != null && seg.Cpu == flow.Cpu);
            }
        }
    }
}
