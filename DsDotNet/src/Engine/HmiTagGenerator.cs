using Dsu.Common.Utilities.ExtensionMethods;

using Engine.Core;
using Engine.Graph;

using System.Diagnostics;

namespace Engine
{
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
