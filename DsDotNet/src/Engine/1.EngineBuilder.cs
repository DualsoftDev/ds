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
        EngineModule.Initialize();

        Model = ModelParser.ParseFromString(modelText);

        Opc = new OpcBroker();
        Cpu = Model.Cpus.First(cpu => cpu.Name == activeCpuName);
        Cpu.IsActive = true;

        Model.BuildGraphInfo();
        RetouchTags();

        Model.Epilogue();

        Opc.Print();

        Engine = new ENGINE(Model, Opc, Cpu);
        Cpu.Engine = Engine;
    }

    /// <summary> Used for Unit test only.</summary>
    internal EngineBuilder(string modelText)
    {
        EngineModule.Initialize();
        Opc = new OpcBroker();
        Model = ModelParser.ParseFromString(modelText);
    }
}

// Engine Initializer
partial class EngineBuilder
{
    public void RetouchTags()
    {
        RenameBits();
        RebuildMap();
        MarkTxRxTags();
        return;

        void RenameBits()
        {
            // root flow 를 cpu 별로 grouping
            var allRootFlows = Model.Systems.SelectMany(s => s.RootFlows);
            var flowsGrps = allRootFlows.GroupByToDictionary(flow => flow.Cpu);
            foreach (var (cpu, flows) in flowsGrps.Select(kv => kv.ToTuple()))
            {
                var cpuBits = new HashSet<IBit>();
                // rename flow/segment tags, add flow auto bit
                foreach (var f in flows)
                {
                    foreach (var seg in f.RootSegments)
                    {
                        var q = seg.QualifiedName;
                        foreach (var t in new[] { seg.TagStart, seg.TagReset, seg.TagEnd, seg.Going, seg.Ready })
                        {
                            cpuBits.Add(t);
                            t.Name = $"{t.InternalName}_{q}";
                        }
                        foreach (var p in new PortInfo[] { seg.PortS, seg.PortR, seg.PortE, })
                        {
                            cpuBits.Add(p);
                            p.Name = $"{p.InternalName}_{q}";
                            if (p.Actual != null)
                            {
                                cpuBits.Add(p.Actual);
                                //if (p.Actual is Named named)
                                //    named.Name = $"{p.InternalName}_Actual_{q}";
                                //else
                                //    throw new Exception("ERROR");
                            }
                            if (p.Plan != null)
                            {
                                cpuBits.Add(p.Plan);
                                if (p == seg.PortE && p.Plan is Named named)
                                    named.Name = $"{p.InternalName}_Plan_{q}";
                            }
                        }
                    }
                    cpuBits.Add(f.Auto);
                    f.Auto.Name = $"Auto_{f.QualifiedName}";
                }

                Debug.Assert(cpuBits.ForAll(cpu.BitsMap.Values.Contains));
                Debug.Assert(cpuBits.OfType<Tag>().ForAll(cpu.TagsMap.Values.Contains));
            }
        }

        void RebuildMap()
        {
            foreach (var cpu in Model.Cpus)
            {
                var cpuBits = cpu.BitsMap.Values.ToHashSet();
                var oldKeys = cpu.BitsMap.Where(kv => cpuBits.Contains(kv.Value)).Select(kv => kv.Key).ToArray();
                foreach (var ok in oldKeys)
                {
                    cpu.BitsMap.Remove(ok);
                    cpu.TagsMap.Remove(ok);
                }

                foreach (var b in cpuBits)
                {
                    var n = b.GetName();
                    cpu.BitsMap.Add(n, b);
                    if (b is Tag t)
                        cpu.TagsMap.Add(n, t);
                }

                Opc.AddTags(cpuBits.OfType<Tag>());
            }
        }

        void MarkTxRxTags()
        {
            var allRootSegments =
                Model.Systems.SelectMany(s => s.RootFlows)
                .SelectMany(f => f.RootSegments)
                ;

            foreach(var rs in allRootSegments)
            {
                foreach (var tx in rs.Children.SelectMany(ch => ch.TagsStart).OfType<Tag>())
                    tx.Type = tx.Type | TagType.TX | TagType.External;

                foreach (var rx in rs.Children.SelectMany(ch => ch.TagsEnd).OfType<Tag>())
                    rx.Type = rx.Type | TagType.RX | TagType.External;

                foreach (var ch in rs.Children)
                {
                    if (ch.TagReset is Tag reset)
                    {
                        Debug.Assert(ch.Coin is ExSegmentCall);
                        Debug.Assert(reset.Type.HasFlag(TagType.Reset));
                        reset.Type = reset.Type | TagType.External;
                    }
                }
            }
        }
    }
}

