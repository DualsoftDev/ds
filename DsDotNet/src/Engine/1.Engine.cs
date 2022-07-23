using Engine.Parser;
using Engine.Runner;

[assembly: DebuggerDisplay("[{Key}={Value}]", Target = typeof(KeyValuePair<,>))]

namespace Engine;

public partial class EngineBuilder : IEngine
{
    public OpcBroker Opc { get; }
    public Cpu Cpu { get; }
    public Model Model { get; }
    public ENGINE Engine { get; }

    public EngineBuilder(string modelText, string activeCpuName)
    {
        Model = ModelParser.ParseFromString(modelText);

        Opc = new OpcBroker();
        Cpu = Model.Cpus.First(cpu => cpu.Name == activeCpuName);
        Cpu.Engine = this;
        Cpu.IsActive = true;

        Model.BuidGraphInfo();
        InitializeAllFlows();


        Model.Epilogue();


        Opc.Print();

        Model.Cpus.Iter(cpu => cpu.PrintTags());

        Engine = new ENGINE(Model, Opc, Cpu);
    }

    /// <summary> Used for Unit test only.</summary>
    internal EngineBuilder(string modelText)
    {
        Opc = new OpcBroker();
        Model = ModelParser.ParseFromString(modelText);
    }
}

// Engine Initializer
partial class EngineBuilder
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

