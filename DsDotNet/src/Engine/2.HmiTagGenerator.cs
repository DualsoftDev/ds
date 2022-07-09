using Dsu.Common.Utilities.ExtensionMethods;

using Engine.Core;
using Engine.Graph;

using log4net;

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Engine
{
    public static class HmiTagGenerator
    {
        static ILog Logger => Program.Logger;

        /// <summary> flow 의 모든 root segment 에 대해서 S/R/E tag 생성 </summary>
        static void GenerateHmiTag(Segment segment)
        {
            var flow = segment.ContainerFlow;
            var cpu = flow.Cpu;
            var name = $"{flow.System.Name}_{flow.Name}_{segment.Name}";
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
        static void GenerateHmiAutoTagForRootSegment(RootFlow flow)
        {
            var cpu = flow.Cpu;
            var midName = $"{flow.System.Name}_{flow.Name}";

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
                    var s = new Tag(init, $"AutoStart_{midName}_{init.Name}") { OwnerCpu = cpu, IsExternal = true };
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
                    var r = new Tag(last, $"AutoReset_{midName}_{last.Name}") { OwnerCpu = cpu, IsExternal = true };
                    cpu.AddBitDependancy(r, last.PortR);
                }
            }
        }
        static void GenerateHmiAutoTagForCalls(RootFlow flow)
        {

        }
        static void GenerateHmiAutoTagForCalls(Segment segment)
        {

        }
        /// <summary>
        /// Flow 에 속한 root segment 에 대해서 S/R/E tag 생성
        /// - init 에 대해서 auto start,
        /// - last segment에 대해서 auto reset tag 생성
        /// todo: flow 에 속한 call 에 대한 HMI tag 생성
        /// <para> - 생성된 tag 는 CPU 에 저장된다.</para>
        /// </summary>
        public static void GenereateHmiTags(this RootFlow flow)
        {
            var cpu = flow.Cpu;

            var segments = flow.Children.OfType<Segment>();

            // 모든 root segment 에 대해서 S/R/E tag 생성
            segments.Iter(s => GenerateHmiTag(s));
            GenerateHmiAutoTagForRootSegment(flow);
            GenerateHmiAutoTagForCalls(flow);

            // root segment 에 포함된 call 에 대해 tag 생성
            segments.Iter(s => GenerateHmiAutoTagForCalls(s));
        }

        public static IEnumerable<Tag> CollectTags(this Cpu cpu)
        {
            IEnumerable<IBit> Helper()
            {
                foreach (var map in new[] { cpu.ForwardDependancyMap, cpu.BackwardDependancyMap })
                {
                    foreach (var tpl in map)
                    {
                        yield return tpl.Key;
                        foreach (var v in tpl.Value)
                            yield return v;
                    }
                }
            }

            return Helper().OfType<Tag>().Distinct();
        }


        public static void PrintTags(this Cpu cpu)
        {
            var tags = cpu.CollectTags().ToArray();
            var externalTagNames = string.Join("\r\n\t", tags.Where(t => t.IsExternal).Select(t => t.Name));
            var internalTagNames = string.Join("\r\n\t", tags.Where(t => ! t.IsExternal).Select(t => t.Name));
            Logger.Debug($"-- Tags for {cpu.Name}");
            Logger.Debug($"  External:\r\n\t{externalTagNames}");
            Logger.Debug($"  Internal:\r\n\t{internalTagNames}");
        }
    }
}
