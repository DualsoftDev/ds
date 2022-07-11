using Dsu.Common.Utilities.ExtensionMethods;

using Engine.Core;
using Engine.OPC;

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Engine
{
    public static class CpuRunner
    {
        public static void Run(this CpuBase cpu)
        {
        }

        static void InitializeFlow(RootFlow flow, bool isActiveCpu, OpcBroker opc)
        {
            // my flow 상의 root segment 들에 대한 HMI s/r/e tags
            var hmiTags = flow.GenereateHmiTags4Segments().ToArray();
            var vertices =
                flow.Edges
                    .SelectMany(e => e.Vertices)
                    .Distinct()
                    .ToArray()
                    ;

            var calls = vertices.OfType<Call>().ToArray();
            var txs = calls.SelectMany(c => c.TXs).OfType<Segment>().Distinct().ToArray();
            var rxs = calls.Select(c => c.RX).OfType<Segment>().Distinct().ToArray();

            if (isActiveCpu)
            {
                hmiTags.Iter(t => t.IsExternal = true);
                opc.AddTags(hmiTags);

                var startTags = txs.Select(s => s.TagS);
                startTags.Iter(t => { t.Type = TagType.Q; t.IsExternal = true; });
                opc.AddTags(startTags);

                var endTags = rxs.Select(s => s.TagE);
                endTags.Iter(t => { t.Type = TagType.I; t.IsExternal = true; });
                opc.AddTags(endTags);

                foreach (var call in calls)
                    call.OwnerCpu = flow.Cpu;
            }


            var tags = isActiveCpu ? flow.Cpu.CollectTags().ToArray() : new Tag[] { };
            // Edge 를 Bit 로 보고
            // A -> B 연결을 A -> Edge -> B 연결 정보로 변환
            foreach(var e in flow.Edges)
            {
                var deps = e.CollectForwardDependancy().ToArray();
                foreach ((var src, var tgt) in deps)
                {
                    Debug.Assert(flow.Cpu == src.OwnerCpu);
                    Debug.Assert(flow.Cpu == tgt.OwnerCpu);
                    flow.Cpu.AddBitDependancy(src, tgt);
                }
            }


            flow.PrintFlow(isActiveCpu);
            //cpu.PrintTags();
            //foreach (var flow in rootFlows)
        }

        public static void InitializeFlows(this Engine engine, CpuBase cpu, OpcBroker opc)
        {
            var model = engine.Model;
            var allRootFlows = model.Systems.SelectMany(s => s.RootFlows);
            var flowsGrps =
                from flow in allRootFlows
                group flow by cpu.RootFlows.Contains(flow) into g
                select new { Active = g.Key, Flows = g.ToList() };
                ;
            var activeFlows = flowsGrps.Where(gr => gr.Active).SelectMany(gr => gr.Flows).ToArray();
            var otherFlows = flowsGrps.Where(gr => !gr.Active).SelectMany(gr => gr.Flows).ToArray();

            // generate fake cpu's for other flows
            var fakeCpu = new FakeCpu("FakeCpu", otherFlows, model) { Engine = engine };
            engine.FakeCpu = fakeCpu;

            foreach (var f in otherFlows)
            {
                f.Cpu = fakeCpu;
                f.Children.OfType<Segment>().SelectMany(s => s.AllPorts).Iter(p => p.OwnerCpu = fakeCpu);
                InitializeFlow(f, false, opc);
            }

            foreach (var f in activeFlows)
                InitializeFlow(f, true, opc);

            cpu.BuildBackwardDependency();
            fakeCpu.BuildBackwardDependency();

            opc._cpus.Add(cpu);
            opc._cpus.Add(fakeCpu);

            // debugging
            Debug.Assert(cpu.CollectBits().All(b => b.OwnerCpu == cpu));
            Debug.Assert(fakeCpu.CollectBits().All(b => b.OwnerCpu == fakeCpu));



        }
    }
}
