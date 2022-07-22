using Engine.Parser;


[assembly: DebuggerDisplay("[{Key}={Value}]", Target = typeof(KeyValuePair<,>))]

namespace Engine;

public partial class Engine : IEngine
{
    public OpcBroker Opc { get; }
    public Cpu Cpu { get; }
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

        void readTagsFromOpc(Cpu cpu)
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
        // root flow 를 cpu 별로 grouping
        var allRootFlows = Model.Systems.SelectMany(s => s.RootFlows);
        var empty = Array.Empty<RootFlow>();
        var flowsGrps = allRootFlows.GroupByToDictionary(flow => flow.Cpu);
        foreach (var (cpu, flows) in flowsGrps.Select(kv => kv.ToTuple()))
        {
            TagGenInfo[] tgis = CreateTags4Child(flows);
            var tags = tgis.Select(tgi => tgi.GeneratedTag).ToArray();
            tgis.Iter(tgi => copyChildSRETagsToSegment(tgi));
            tags.Iter(cpu.AddTag);
            Opc.AddTags(tags);

            // flow 상의 root segment 들에 대한 HMI s/r/e tags
            var hmiTags = flows.SelectMany(f => f.GenereateHmiTags4Segments()).ToArray();
            hmiTags.Iter(cpu.AddTag);
            Opc.AddTags(hmiTags);

            foreach (var f in flows)
                InitializeRootFlow(f);


            cpu.BuildBackwardDependency();
            Opc._cpus.Add(cpu);
        }




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

    void InitializeRootFlow(RootFlow rootFlow)
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


        rootFlow.PrintFlow();

        //cpu.PrintTags();
    }
}

public static class EngineExtension
{

}
