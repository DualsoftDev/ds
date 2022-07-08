using Dsu.Common.Utilities.ExtensionMethods;

using Engine.Core;
using Engine.Graph;

using System.Diagnostics;
using System.Linq;

namespace Engine
{
    public static class CpuRunner
    {
        static void Initialize(this Cpu cpu)
        {
            foreach (var flow in cpu.Flows)
                flow.GenereateHmiTags();

            cpu.BuildBackwardDependency();

            // cpu 기준으로 call 에 사용된 TX 및 RX 의 Tag 값 external 로 marking
            var otherFlows =
                from system in cpu.Model.Systems
                from flow in system.Flows
                where !(cpu.Flows.Contains(flow))
                select flow
                ;


            var x1 =
                otherFlows
                    .SelectMany(f => f.Edges)
                    .SelectMany(e => e.Vertices)
                    .OfType<Call>()
                    .ToArray()
                    ;

            var TxRxs =
                otherFlows
                    .SelectMany(f => f.Edges)
                    .SelectMany(e => e.Vertices)
                    .OfType<Call>()
                    .Select(c => (c.TXs, c.RX))
                    ;

                //from f in otherFlows
                //from e in f.Edges
                //from c in s.Children
                //select (c.TXs, c.RX)
                //;

            foreach( (var txs, var rx) in TxRxs)
            {
                foreach (var s in txs.OfType<Segment>())
                {
                    var tags = s.ContainerFlow.Cpu.BackwardDependancyMap[s.PortS].OfType<Tag>();
                    tags.Iter(tag =>
                    {
                        tag.Type = TagType.Q;
                        tag.IsExternal = true;
                    });
                }

                {
                    var tag = rx as Tag;
                    if (tag != null)
                    {
                        tag.Type = TagType.I;
                        tag.IsExternal = true;
                    }
                }
            }
        }
        public static void Run(this Cpu cpu)
        {
            cpu.Initialize();
        }
    }


    public static class HmiTagGenerator
    {
        /// <summary> flow 의 모든 root segment 에 대해서 S/R/E tag 생성 </summary>
        static void GenerateHmiTag(Segment segment)
        {
            var flow = segment.ContainerFlow;
            var cpu = flow.Cpu;
            var name = $"{flow.Name}_{segment.Name}";
            var s = new Tag(segment, $"Start_{name}");
            var r = new Tag(segment, $"Reset_{name}");
            var e = new Tag(segment, $"End_{name}");

            new[] { s, r, e }
                .Iter(t => t.OwnerCpu = cpu);

            cpu.AddBitDependancy(s, segment.PortS);
            cpu.AddBitDependancy(r, segment.PortR);
            cpu.AddBitDependancy(segment.PortE, e);
        }

        /// <summary> flow 의 init, last segment 에 대해서 auto start, auto reset tag 생성 </summary>
        static void GenerateHmiAutoTag(Flow flow)
        {
            var cpu = flow.Cpu;

            // graph 분석
            var graphInfo = GraphUtil.analyzeFlows(new[] { flow });

            foreach (var init_ in graphInfo.Inits)
            {
                var init = init_ as Segment;
                if (init == null)
                {
                    Debug.Assert(init_ is Call);
                    // do nothing for call
                }
                else
                {
                    var s = new Tag(init, $"AutoStart_{flow.Name}_{init.Name}") { OwnerCpu = cpu };
                    cpu.AddBitDependancy(s, init.PortS);
                }
            }

            foreach (var last_ in graphInfo.Lasts)
            {
                var last = last_ as Segment;
                if (last == null)
                {
                    Debug.Assert(last_ is Call);
                    // do nothing for call
                }
                else
                {
                    var r = new Tag(last, $"AutoReset_{flow.Name}_{last.Name}") { OwnerCpu = cpu };
                    cpu.AddBitDependancy(r, last.PortR);
                }
            }
        }

        /// <summary>
        /// Flow 에 속한 root segment 에 대해서 S/R/E tag 생성
        /// - init 에 대해서 auto start,
        /// - last segment에 대해서 auto reset tag 생성
        /// todo: flow 에 속한 call 에 대한 HMI tag 생성
        /// </summary>
        public static void GenereateHmiTags(this Flow flow)
        {
            var cpu = flow.Cpu;

            // 모든 root segment 에 대해서 S/R/E tag 생성
            flow.Segments.Iter(s => GenerateHmiTag(s));
            GenerateHmiAutoTag(flow);
        }
    }
}
