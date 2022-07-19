using Engine.Common;
using Engine.Core;
using Engine.Graph;
using Engine.OPC;
using Engine.Parser;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Engine
{
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
            Cpu = Model.Cpus.First(cpu => cpu.Name == activeCpuName);
            Cpu.Engine = this;

            Model.BuidGraphInfo();
            this.InitializeFlows(Cpu, Opc);


            Model.Epilogue();


            Debug.Assert(Opc._opcTags.All(t => t.OriginalTag.IsExternal()));
            Opc.Print();

            Model.Cpus.Iter(cpu => readTagsFromOpc(cpu));

            void readTagsFromOpc(Cpu cpu)
            {
                var tpls = Opc.ReadTags(cpu.Tags.Select(t => t.Key)).ToArray();
                foreach ((var tName, var value) in tpls)
                {
                    var tag = cpu.Tags[tName];
                    if (tag.Value != value)
                        cpu.OnOpcTagChanged(tName, value);
                }
            }
        }

        public void Run()
        {
            //Cpu.Run();
            //FakeCpu.Run();
        }
    }


    // Engine Initializer
    partial class Engine
    {
        public void InitializeFlows(CpuBase activeCpu, OpcBroker opc)
        {
            var cpu = activeCpu;
            var allRootFlows = Model.Systems.SelectMany(s => s.RootFlows);
            var flowsGrps =
                from flow in allRootFlows
                group flow by cpu.RootFlows.Contains(flow) into g
                select new { Active = g.Key, Flows = g.ToList() };
            ;
            var activeFlows = flowsGrps.Where(gr => gr.Active).SelectMany(gr => gr.Flows).ToArray();
            var otherFlows = flowsGrps.Where(gr => !gr.Active).SelectMany(gr => gr.Flows).ToArray();
            Debug.Assert(activeFlows.All(f => f.Cpu == activeCpu));

            if (otherFlows.Any())
            {
                // generate fake cpu's for other flows
                FakeCpu = new FakeCpu("FakeCpu", otherFlows, Model) { Engine = this };
            }


            CreateTags4Child(activeCpu, activeFlows);
            CreateTags4Child(FakeCpu, otherFlows);

            foreach (var f in otherFlows)
            {
                f.Cpu = FakeCpu;
                f.ChildVertices.OfType<Segment>().SelectMany(s => s.AllPorts).Iter(p => p.OwnerCpu = FakeCpu);
                InitializeFlow(f, false, opc);
            }


            foreach (var f in activeFlows)
                InitializeFlow(f, true, opc);

            cpu.BuildBackwardDependency();
            FakeCpu?.BuildBackwardDependency();

            opc._cpus.Add(cpu);
            if (FakeCpu != null)
                opc._cpus.Add(FakeCpu);

            // debugging
            Debug.Assert(cpu.CollectBits().All(b => b.OwnerCpu == cpu));
            Debug.Assert(FakeCpu == null || FakeCpu.CollectBits().All(b => b.OwnerCpu == FakeCpu));
        }


        struct TagGenInfo
        {
            public TagType Type;
            public Segment Segment;
            public Child Child;
            public string Context;
            public string TagName => $"{Child.QualifiedName}_{Segment.QualifiedName}_{Type}";

            public TagGenInfo(TagType type, Segment segment, Child child, string context)
            {
                Type = type;
                Segment = segment;
                Child = child;
                Context = context;
            }
        }

        void CreateTags4Child(CpuBase cpu, RootFlow[] activeFlows)
        {
            var tagGenInfos =
                collectTagGenInfo().Distinct()
                //.Select(gi => gi.TagName)
                .ToArray();

            var tgiGroups = tagGenInfos.GroupByToDictionary(tgi => (tgi.Child, tgi.Type));
            foreach ( var kv in tgiGroups)
            {
                (var child, var type) = kv.Key;
                var tgis = kv.Value;
                var tags = tgis.Select(tgi => new Tag(child, tgi.TagName, type, cpu)).ToArray();
                var storage = type switch
                {
                    TagType.Start => child.TagsStart,
                    TagType.Reset => child.TagsReset,
                    TagType.End => child.TagsEnd,
                    _ => throw new Exception("ERROR")
                };

                Debug.Assert(storage.IsNullOrEmpty());
                storage.AddRange(tags);
                var tagNames = String.Join(", ", tgis.Select(tgi => tgi.TagName));
                Global.Logger.Debug($"Adding Child Tags {tagNames} to child [{child.QualifiedName}]");

                // apply to segment
                foreach (var tgi in tgis)
                {
                    var seg = tgi.Segment;
                    var segStorage = tgi.Type switch
                    {
                        TagType.Start => seg.TagsStart,
                        TagType.Reset => seg.TagsReset,
                        TagType.End => seg.TagsEnd,
                        _ => throw new Exception("ERROR")
                    };

                    Global.Logger.Debug($"Adding Export {tgi.Type} Tag [{tgi.TagName}] to segment [{seg.QualifiedName}]");
                    segStorage.Add(new Tag(seg, tgi.TagName, type, seg.OwnerCpu));
                }

                Console.WriteLine();
            }


            IEnumerable<TagGenInfo> collectTagGenInfo()
            {
                var roots = activeFlows.SelectMany(f => f.ChildVertices).Distinct();
                foreach (var root in roots)
                {
                    switch (root)
                    {
                        case Call rootCall:
                            break;

                        case Segment rootSeg:
                            var fqdn = rootSeg.QualifiedName;
                            var children = rootSeg.ChildVertices.OfType<Child>();
                            foreach (var child in children)
                            {
                                var ies =
                                    GraphUtil.getIncomingEdges(rootSeg.GraphInfo.Graph, child)
                                    .Select(qge => qge.OriginalEdge)
                                    .Distinct()
                                    ;
                                // incoming edge 를 reset edge 여부로 grouping 한 것.
                                var iesGroups = ies.GroupByToDictionary(e => e is IResetEdge);
                                var iesResets = iesGroups.ContainsKey(true) ? iesGroups[true] : Array.Empty<Edge>();
                                var iesSet = iesGroups.ContainsKey(false) ? iesGroups[false] : Array.Empty<Edge>();

                                switch (child.Coin)
                                {
                                    case Call call:
                                        if (iesSet.Length > 0)
                                        {
                                            var segStart = call.TXs.OfType<Segment>();
                                            var segEnd = call.RXs.OfType<Segment>();
                                            foreach (var s in segStart)
                                                yield return new TagGenInfo(TagType.Start, s, child, fqdn);
                                            foreach (var e in segEnd)
                                                yield return new TagGenInfo(TagType.End, e, child, fqdn);
                                        }
                                        break;

                                    case ExSegmentCall exSeg:
                                        var seg = exSeg.ExternalSegment;
                                        if (iesGroups.Count() > 1)
                                            throw new Exception("ERROR: Both reset and start edges exists for external segment child call.");
                                        if (iesResets.Any())
                                            yield return new TagGenInfo(TagType.Reset, seg, child, fqdn);
                                        else if (iesSet.Any())
                                            yield return new TagGenInfo(TagType.Start, seg, child, fqdn);

                                        yield return new TagGenInfo(TagType.End, seg, child, fqdn);


                                        break;
                                    default:
                                        throw new Exception("ERROR");
                                }
                            }
                            break;

                        default:
                            throw new Exception("ERROR");
                    }
                }
            }
        }

        void InitializeFlow(RootFlow flow, bool isActiveCpu, OpcBroker opc)
        {
            // my flow 상의 root segment 들에 대한 HMI s/r/e tags
            var hmiTags = flow.GenereateHmiTags4Segments().ToArray();
            //var vertices =
            //    flow.Edges
            //        .SelectMany(e => e.Vertices)
            //        .Concat(flow.ChildVertices)
            //        .Distinct()
            //        .ToArray()
            //        ;

            var cpu = flow.Cpu;

            if (isActiveCpu)
            {
                hmiTags.Iter(t => t.Type = t.Type.Add(TagType.External));
                opc.AddTags(hmiTags);

                //var subCalls = vertices.OfType<Segment>().SelectMany(seg => seg.CallChildren);
                //var rootCalls = vertices.OfType<Call>();
                //var calls = rootCalls.Concat(subCalls).Distinct();
                //foreach (var call in calls)
                //{
                //    call.OwnerCpu = cpu;
                //    var txs = call.TXs.OfType<Segment>().Distinct().ToArray();
                //    var rxs = call.RXs.OfType<Segment>().Distinct().ToArray();

                //    // Call prototype 의 tag 로부터 Call instance 에 사용할 tag 생성

                //    var startTags = txs.Select(s => Tag.CreateCallTx(call, s.TagS)).ToArray();
                //    opc.AddTags(startTags);
                //    call.TxTags = startTags;
                //    cpu.TxRxTags.AddRange(startTags);

                //    var endTags = rxs.Select(s => Tag.CreateCallRx(call, s.TagE)).ToArray();
                //    opc.AddTags(endTags);
                //    call.RxTags = endTags;
                //    cpu.TxRxTags.AddRange(endTags);

                //    call.OwnerCpu = flow.Cpu;
                //}

            }

            var tags =
                (isActiveCpu ? flow.Cpu.CollectTags().Distinct().ToArray() : new Tag[] { })
                .ToDictionary(t => t.Name, t => t);
            // Edge 를 Bit 로 보고
            // A -> B 연결을 A -> Edge -> B 연결 정보로 변환
            foreach (var e in flow.Edges)
            {
                var deps = e.CollectForwardDependancy().ToArray();
                foreach ((var src_, var tgt_) in deps)
                {
                    (var src, var tgt) = (src_, tgt_);
                    if (cpu != src.OwnerCpu)
                        src = tags[((Named)src).Name];
                    if (cpu != tgt.OwnerCpu)
                        tgt = tags[((Named)tgt).Name];

                    Debug.Assert(cpu == src.OwnerCpu);
                    Debug.Assert(cpu == tgt.OwnerCpu);
                    flow.Cpu.AddBitDependancy(src, tgt);
                }
            }


            flow.PrintFlow(isActiveCpu);

            //cpu.PrintTags();
        }
    }

    public static class EngineExtension
    {

    }

    public static class ModelExtension
    {
        public static void BuidGraphInfo(this Model model)
        {
            var rootFlows = model.CollectRootFlows();
            foreach (var flow in rootFlows)
                flow.GraphInfo = GraphUtil.analyzeFlows(new[] { flow }, true);

            foreach (var cpu in model.Cpus)
                cpu.GraphInfo = GraphUtil.analyzeFlows(cpu.RootFlows, true);

            foreach (var segment in model.CollectSegments())
                segment.GraphInfo = GraphUtil.analyzeFlows(new[] { segment }, false);
        }
        public static void Epilogue(this Model model)
        {
            foreach (var segment in model.CollectSegments())
                segment.Epilogue();

            foreach (var cpu in model.Cpus)
                cpu.Epilogue();
        }

        public static IEnumerable<RootFlow> CollectRootFlows(this Model model) => model.Systems.SelectMany(sys => sys.RootFlows);

        public static IEnumerable<Flow> CollectFlows(this Model model)
        {
            var rootFlows = model.CollectRootFlows().ToArray();
            var subFlows = rootFlows.SelectMany(rf => rf.ChildVertices.OfType<Segment>());
            var allFlows = rootFlows.Cast<Flow>().Concat(subFlows);
            return allFlows;
        }
        public static IEnumerable<Segment> CollectSegments(this Model model) =>
            model.CollectRootFlows().SelectMany(rf => rf.ChildVertices).OfType<Segment>()
            ;
    }
}
