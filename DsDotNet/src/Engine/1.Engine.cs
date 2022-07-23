using Engine.Parser;
using Engine.Runner;

[assembly: DebuggerDisplay("[{Key}={Value}]", Target = typeof(KeyValuePair<,>))]

namespace Engine;

public partial class EngineBuilder
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
        Cpu.IsActive = true;

        Model.BuidGraphInfo();
        InitializeAllFlows();


        Model.Epilogue();


        Opc.Print();

        Model.Cpus.Iter(cpu => cpu.PrintTags());

        Engine = new ENGINE(Model, Opc, Cpu);
        Cpu.Engine = Engine;
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

            var myTgis = tgis.Where(tgi => tgi.OwnerCpu == cpu).ToArray();
            foreach (var tgi in myTgis)
            {
                copyChildSRETagsToSegment(tgi);
                buildBitDependencies(tgi, cpu);
            }

            var tags = tgis.Select(tgi => tgi.GeneratedTag).ToArray();
            Debug.Assert(tags.All(tag => tag.OwnerCpu.TagsMap.ContainsKey(tag.Name)));

            Opc.AddTags(tags);


            // flow 상의 root segment 들에 대한 HMI s/r/e tags
            var hmiTags = flows.SelectMany(f => f.GenereateHmiTags4Segments()).ToArray();
            hmiTags.Iter(cpu.AddTag);
            Opc.AddTags(hmiTags);

            cpu.BuildBackwardDependency();
        }




        /// 'Child' 의 Tags{Start,Reset,End} tag 들을 Child 가 실제 가리키는 segment 의 S/R/E Tags 에도 반영한다.
        void copyChildSRETagsToSegment(TagGenInfo tgi)
        {
            var segment = tgi.TagContainerSegment;
            var tag = segment.OwnerCpu.TagsMap[tgi.GeneratedTag.Name];
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


        void buildBitDependencies(TagGenInfo tgi, Cpu cpu)
        {
            var (tag, edge) = (tgi.GeneratedTag, tgi.Edge);

            // call 에 대한 reset edge 는 실효성이 없으므로 무시.  (정보로만 사용)
            var child = tgi.Child as Child;
            if (child != null && child.Coin is Call && edge is IResetEdge)
                return;


            var tName = tag.Name;
            Debug.Assert(edge.OwnerCpu == cpu);
            Debug.Assert(tag.OwnerCpu.TagsMap.ContainsKey(tName));
            // todo : Debug.Assert(tag.OwnerCpu == cpu);
            if (tag.OwnerCpu != cpu)
            {
                if (cpu.TagsMap.ContainsKey(tName))
                    tag = cpu.TagsMap[tName];
                else
                    Debug.Assert(false);
                Debug.Assert(tag.OwnerCpu.TagsMap.ContainsKey(tag.Name));
            }


            var isReset = tgi.Edge is IResetEdge;
            switch (tgi.Child)
            {
                case Segment:
                    switch (tgi.IsSource, tgi.Type)
                    {
                        case (true, TagType.End) when isReset:
                            // todo : Going tag??
                            Global.Logger.Warn("Need going tag???");
                            //cpu.AddBitDependancy(some-going-tag, edge);
                            break;
                        case (true, TagType.End):
                            cpu.AddBitDependancy(tag, edge);
                            break;
                        case (false, TagType.Reset):        // <--- added for segment
                        case (false, TagType.Start):
                            cpu.AddBitDependancy(edge, tag);
                            break;

                        case (true, TagType.Start):
                        case (false, TagType.End):
                            break;

                        case (true, TagType.Reset):
                        default:
                            throw new Exception("ERROR");
                    }
                    break;

                case Child:
                    switch (tgi.IsSource, tgi.Type)
                    {
                        case (true, TagType.End):
                            cpu.AddBitDependancy(tag, edge);
                            break;
                        case (false, TagType.Start):
                            cpu.AddBitDependancy(edge, tag);
                            break;

                        case (true, TagType.Start):
                        case (false, TagType.End):
                            break;

                        default:
                            throw new Exception("ERROR");
                    }


                    break;
                case RootCall call:
                    // todo:
                    //throw new Exception("ERROR");
                    break;
            }

            Console.WriteLine();
        }
    }
}

