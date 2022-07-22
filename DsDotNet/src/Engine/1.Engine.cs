global using System;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.Linq;
global using log4net;

global using Engine.Common;
global using Engine.Core;
global using Engine.OPC;

using Engine.Parser;


[assembly: DebuggerDisplay("[{Key}={Value}]", Target = typeof(KeyValuePair<,>))]

namespace Engine;

public partial class Engine : IEngine
{
    public OpcBroker Opc { get; }
    public Cpu Cpu { get; }
    public FakeCpu FakeCpu { get; set; }
    public Model Model { get; }

    public Engine(string modelText, string activeCpuName)
    {
        Model = ModelParser.ParseFromString(modelText);

        Opc = new OpcBroker();
        Cpu = Model.Cpus.OfType<Cpu>().First(cpu => cpu.Name == activeCpuName);
        Cpu.Engine = this;
        Cpu.IsActive = true;

        Model.BuidGraphInfo();
        InitializeAllFlows();


        Model.Epilogue();


        Opc.Print();

        Model.Cpus.Iter(cpu => readTagsFromOpc(cpu));
        Model.Cpus.Iter(cpu => cpu.PrintTags());

        void readTagsFromOpc(CpuBase cpu)
        {
            var tpls = Opc.ReadTags(cpu.TagsMap.Select(t => t.Key)).ToArray();
            foreach ((var tName, var value) in tpls)
            {
                var tag = cpu.TagsMap[tName];
                if (tag.Value != value)
                    cpu.OnOpcTagChanged(tName, value);
            }
        }
    }

    public void Run()
    {
        //Cpu.Run();
        //FakeCpu.Run();
        Global.Logger.Info("Engine started!");
    }
}


// Engine Initializer
partial class Engine
{
    public void InitializeAllFlows()
    {
        // active cpu 의 flow 와 나머지 flow 로 grouping
        var allRootFlows = Model.Systems.SelectMany(s => s.RootFlows);
        var empty = Array.Empty<RootFlow>();
        var flowsGrps = allRootFlows.GroupByToDictionary(Cpu.RootFlows.Contains);
        var activeFlows = flowsGrps.ContainsKey(true) ? flowsGrps[true] : empty;
        var otherFlows = flowsGrps.ContainsKey(false) ? flowsGrps[false]: empty;


        Debug.Assert(activeFlows.All(f => f.Cpu == Cpu));

        if (otherFlows.Any())
        {
            // generate fake cpu's for other flows
            FakeCpu = new FakeCpu("FakeCpu", otherFlows, Model) { Engine = this };
            Model.Cpus.Add(FakeCpu);
            foreach (var f in otherFlows)
            {
                f.Cpu = FakeCpu;
                f.RootSegments.SelectMany(s => s.AllPorts).Iter(p => p.OwnerCpu = FakeCpu);
            }
        }


        TagGenInfo[] tgisActive = CreateTags4Child(Cpu, activeFlows);
        TagGenInfo[] tgisFake   = CreateTags4Child(FakeCpu, otherFlows);
        var tagsActive = tgisActive.Select(tgi => tgi.GeneratedTag).ToArray();
        var tagsFake = tgisFake.Select(tgi => tgi.GeneratedTag).ToArray();
        tagsActive.Iter(Cpu.AddTag);
        tagsFake.Iter(FakeCpu.AddTag);

        var allTgis = tgisActive.Concat(tgisFake).ToArray();
        allTgis.Iter(tgi => copyChildSRETagsToSegment(tgi));

        Opc.AddTags(tagsActive);
        Opc.AddTags(tagsFake);

        // other flow 상의 root segment 들에 대한 HMI s/r/e tags
        var otherFlowsHmiTags = otherFlows.SelectMany(f => f.GenereateHmiTags4Segments()).ToArray();
        otherFlowsHmiTags.Iter(FakeCpu.AddTag);
        Opc.AddTags(otherFlowsHmiTags);

        // active flow 상의 root segment 들에 대한 HMI s/r/e tags
        var activeFlowsHmiTags = activeFlows.SelectMany(f => f.GenereateHmiTags4Segments()).ToArray();
        activeFlowsHmiTags.Iter(Cpu.AddTag);
        Opc.AddTags(activeFlowsHmiTags);


        foreach (var f in otherFlows)
            InitializeRootFlow(f, false);


        foreach (var f in activeFlows)
            InitializeRootFlow(f, true);

        Cpu.BuildBackwardDependency();
        FakeCpu?.BuildBackwardDependency();

        Opc._cpus.Add(Cpu);
        if (FakeCpu != null)
            Opc._cpus.Add(FakeCpu);

        // todo: debugging
        //Debug.Assert(Cpu.CollectBits().All(b => b.OwnerCpu == Cpu));
        //Debug.Assert(FakeCpu == null || FakeCpu.CollectBits().All(b => b.OwnerCpu == FakeCpu));



        /// 'Child' 의 Tags{Start,Reset,End} tag 들을 Child 가 실제 가리키는 segment 의 S/R/E Tags 에도 반영한다.
        void copyChildSRETagsToSegment(TagGenInfo tgi)
        {
            var segment = tgi.TagContainerSegment;
            var tag = tgi.GeneratedTag;
            var tt = tag.Type;
            if (tt.HasFlag(TagType.Start))
                segment.AddStartTags(tag);
            else if (tt.HasFlag(TagType.Reset))
                segment.AddResetTags(tag);
            else if (tt.HasFlag(TagType.End))
                segment.AddEndTags(tag);
            else
                throw new Exception("ERROR");
        }
    }

    /// <summary> Root flow 의 root segment 를 타 시스템에서 호출하기 위한 interface tag 를 생성한다. </summary>

    void InitializeRootFlow(RootFlow rootFlow, bool isActiveCpu)
    {
        var cpu = rootFlow.Cpu;
        var tags = cpu.TagsMap;
        // Edge 를 Bit 로 보고
        // A -> B 연결을 A -> Edge -> B 연결 정보로 변환
        foreach (var e in rootFlow.Edges)
        {
            var deps = e.CollectForwardDependancy().ToArray();
            foreach ((var src_, var tgt_) in deps)
            {
                (var src, var tgt) = (src_, tgt_);
                if (cpu != src.OwnerCpu)
                    src = tags[((Named)src).Name];
                if (cpu != tgt.OwnerCpu)
                    tgt = tags[((Named)tgt).Name];

                // todo:
                //Debug.Assert(cpu == src.OwnerCpu);
                //Debug.Assert(cpu == tgt.OwnerCpu);

                rootFlow.Cpu.AddBitDependancy(src, tgt);
            }
        }


        rootFlow.PrintFlow(isActiveCpu);

        //cpu.PrintTags();
    }
}

public static class EngineExtension
{

}
