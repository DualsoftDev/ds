using Engine.Parser;
using Engine.Runner;

namespace Engine;

public partial class EngineBuilder
{
    public OpcBroker Opc { get; }
    public Cpu Cpu { get; }
    public Model Model { get; }
    public ENGINE Engine { get; }

    public EngineBuilder(string modelText, string activeCpuName)
    {
        ModelRunnerModule.Initialize();

        Model = ModelParser.ParseFromString(modelText);

        Opc = new OpcBroker();
        Cpu = Model.Cpus.First(cpu => cpu.Name == activeCpuName);
        Cpu.IsActive = true;

        Model.BuidGraphInfo();
        InitializeAllFlows();

        Model.Epilogue();

        Opc.Print();

        Engine = new ENGINE(Model, Opc, Cpu);
        Cpu.Engine = Engine;
    }

    /// <summary> Used for Unit test only.</summary>
    internal EngineBuilder(string modelText)
    {
        ModelRunnerModule.Initialize();
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
            var cpuTags = new HashSet<Tag>();
            // rename flow/segment tags
            foreach (var f in flows)
            {
                foreach(var seg in f.RootSegments)
                {
                    var q = seg.QualifiedName;
                    foreach(var t in new[] { seg.TagStart, seg.TagReset, seg.TagEnd, })
                    {
                        cpuTags.Add(t);
                        t.Name = $"{t.InternalName}_{q}";
                    }
                }
                cpuTags.Add(f.Auto);
                f.Auto.Name = $"Auto_{f.QualifiedName}";
            }

            Debug.Assert(cpuTags.ForAll(cpu.BitsMap.Values.Contains));
            Debug.Assert(cpuTags.ForAll(cpu.TagsMap.Values.Contains));

            var oldKeys = cpu.BitsMap.Where(kv => cpuTags.Contains(kv.Value)).Select(kv => kv.Key).ToArray();
            foreach(var ok in oldKeys)
            {
                cpu.BitsMap.Remove(ok);
                cpu.TagsMap.Remove(ok);
            }

            foreach(var t in cpuTags)
            {
                cpu.BitsMap.Add(t.Name, t);
                cpu.TagsMap.Add(t.Name, t);
            }

            Opc.AddTags(cpuTags);

            // rename CPU bits map





            //TagGenInfo[] tgis = CreateTags4Child(flows);

            //var tags = tgis.Select(tgi => tgi.GeneratedTag).ToArray();
            //Debug.Assert(tags.All(tag => tag.Cpu.BitsMap.ContainsKey(tag.Name)));

            //Opc.AddTags(tags);

            //var goingTags = flows.SelectMany(f => f.RootSegments).Select(seg => seg.Going).Where(t => t is not null);
            //Opc.AddTags(goingTags);


            //// todo: flow 상의 root segment 들에 대한 s/r/e tags
            ////Opc.AddTags(hmiTags);

            //Console.WriteLine();
        }



        ///// 'Child' 의 Tags{Start,Reset,End} tag 들을 Child 가 실제 가리키는 segment 의 S/R/E Tags 에도 반영한다.
        //void copyChildSRETagsToSegment(TagGenInfo tgi)
        //{
        //    var segment = tgi.TagContainerSegment;
        //    var tag = segment.Cpu.TagsMap[tgi.GeneratedTag.Name];
        //    var tt = tag.Type;
        //    var edge = tgi.Edge;
        //    Debug.Assert(segment.Cpu == tag.Cpu);
        //    var edgeTag =
        //        edge.Cpu == tag.Cpu
        //        ? tag
        //        : edge.Cpu.TagsMap[tag.Name]
        //        ;
        //    Debug.Assert(edge.Cpu == edgeTag.Cpu);

        //    if (tt.HasFlag(TagType.Start))
        //    {
        //        segment.AddStartTags(tag);
        //        if (tgi.IsTarget)
        //            edge.TargetTag = edgeTag;
        //    }
        //    else if (tt.HasFlag(TagType.Reset))
        //    {
        //        segment.AddResetTags(tag);
        //        if (tgi.IsTarget)
        //            edge.TargetTag = edgeTag;
        //    }
        //    else if (tt.HasFlag(TagType.End))
        //    {
        //        segment.AddEndTags(tag);
        //        if (tgi.IsSource)
        //            edge.SourceTags.Add(edgeTag);
        //    }
        //    else if (tt.HasFlag(TagType.Going))
        //    {
        //        Debug.Assert(segment.Going == null);
        //        segment.Going = tag;
        //        Debug.Assert(segment.Going == edgeTag);
        //        if (tgi.IsSource)
        //            edge.SourceTags.Add(segment.Going);
        //    }
        //    else
        //        throw new Exception("ERROR");
        //}


        //void buildBitDependencies(TagGenInfo tgi, Cpu cpu)
        //{
        //    var (tag, edge) = (tgi.GeneratedTag, tgi.Edge);
        //    var isReset = edge is IResetEdge;

        //    // call 에 대한 reset edge 는 실효성이 없으므로 무시.  (정보로만 사용)
        //    var child = tgi.Child as Child;
        //    if (child != null && child.Coin is Call && isReset)
        //        return;


        //    var tName = tag.Name;
        //    Debug.Assert(edge.Cpu == cpu);
        //    Debug.Assert(tag.Cpu.BitsMap.ContainsKey(tName));
        //    // todo : Debug.Assert(tag.OwnerCpu == cpu);
        //    if (tag.Cpu != cpu)
        //    {
        //        if (cpu.TagsMap.ContainsKey(tName))
        //            tag = cpu.TagsMap[tName];
        //        else
        //            Debug.Assert(false);
        //        Debug.Assert(tag.Cpu.TagsMap.ContainsKey(tag.Name));
        //    }


        //    switch (tgi.Child)
        //    {
        //        case Segment:
        //            switch (isReset, tgi.IsSource, tgi.Type)
        //            {
        //                // { reset edge case
        //                case (true, true, TagType.Going):
        //                    Debug.Assert(edge.SourceTags.Contains(tag));
        //                    //cpu.AddBitDependancy(tag, edge);
        //                    break;
        //                case (true, false, TagType.Reset):
        //                    Debug.Assert(edge.TargetTag == tag);
        //                    //cpu.AddBitDependancy(edge, tag);
        //                    break;
        //                // }

        //                // { start edge case
        //                case (false, true, TagType.End):
        //                    Debug.Assert(edge.SourceTags.Contains(tag));
        //                    //cpu.AddBitDependancy(tag, edge);
        //                    break;
        //                case (false, false, TagType.Start):
        //                    Debug.Assert(edge.TargetTag == tag);
        //                    //cpu.AddBitDependancy(edge, tag);
        //                    break;
        //                // }

        //                default:
        //                    throw new Exception("ERROR");
        //            }
        //            break;

        //        case Child:
        //            switch (tgi.IsSource, tgi.Type)
        //            {
        //                case (true, TagType.End):
        //                    Debug.Assert(edge.SourceTags.Contains(tag));
        //                    //cpu.AddBitDependancy(tag, edge);
        //                    break;
        //                case (false, TagType.Start):
        //                    Debug.Assert(edge.TargetTag == tag);
        //                    //cpu.AddBitDependancy(edge, tag);
        //                    break;

        //                case (true, TagType.Start):
        //                case (false, TagType.End):
        //                    break;

        //                default:
        //                    throw new Exception("ERROR");
        //            }


        //            break;
        //        case RootCall call:
        //            // todo:
        //            //throw new Exception("ERROR");
        //            break;
        //    }
        //}
    }
}

