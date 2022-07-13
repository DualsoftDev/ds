using Engine.Common;
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

            var cpu = flow.Cpu;
            var calls = vertices.OfType<Call>().ToArray();
            var txs = calls.SelectMany(c => c.TXs).OfType<Segment>().Distinct().ToArray();
            var rxs = calls.SelectMany(c => c.RXs).OfType<Segment>().Distinct().ToArray();

            if (isActiveCpu)
            {
                hmiTags.Iter(t => t.IsExternal = true);
                opc.AddTags(hmiTags);

                var startTags = txs.Select(s => new Tag(s.TagS) { Type = TagType.Q, IsExternal = true, OwnerCpu = cpu});
                opc.AddTags(startTags);
                cpu.TxRxTags.AddRange(startTags);

                var endTags = rxs.Select(s => new Tag(s.TagE) { Type = TagType.I, IsExternal = true, OwnerCpu = cpu });
                opc.AddTags(endTags);
                cpu.TxRxTags.AddRange(endTags);

                foreach (var call in calls)
                    call.OwnerCpu = flow.Cpu;
            }


            var tags =
                (isActiveCpu ? flow.Cpu.CollectTags().ToArray() : new Tag[] { })
                .ToDictionary(t => t.Name, t => t);
            // Edge 를 Bit 로 보고
            // A -> B 연결을 A -> Edge -> B 연결 정보로 변환
            foreach(var e in flow.Edges)
            {
                var deps = e.CollectForwardDependancy().ToArray();
                foreach ((var src_, var tgt_) in deps)
                {
                    (var src, var tgt) = (src_, tgt_);
                    if (cpu != src.OwnerCpu)
                        src = tags[ ((Named)src).Name];
                    if (cpu != tgt.OwnerCpu)
                        tgt = tags[((Named)tgt).Name];

                    Debug.Assert(cpu == src.OwnerCpu);
                    Debug.Assert(cpu == tgt.OwnerCpu);
                    flow.Cpu.AddBitDependancy(src, tgt);
                }
            }


            flow.PrintFlow(isActiveCpu);
            //cpu.PrintTags();
            //foreach (var flow in rootFlows)
        }

        public static void InitializeFlows(this Engine engine, CpuBase activeCpu, OpcBroker opc)
        {
            var cpu = activeCpu;
            var model = engine.Model;
            var allRootFlows = model.Systems.SelectMany(s => s.RootFlows);
            var flowsGrps =
                from flow in allRootFlows
                group flow by cpu.RootFlows.Contains(flow) into g
                select new { Active = g.Key, Flows = g.ToList() };
                ;
            var activeFlows = flowsGrps.Where(gr => gr.Active).SelectMany(gr => gr.Flows).ToArray();
            var otherFlows = flowsGrps.Where(gr => !gr.Active).SelectMany(gr => gr.Flows).ToArray();

            FakeCpu fakeCpu = null;
            if (otherFlows.Any())
            {
                // generate fake cpu's for other flows
                fakeCpu = new FakeCpu("FakeCpu", otherFlows, model) { Engine = engine };
                engine.FakeCpu = fakeCpu;

                foreach (var f in otherFlows)
                {
                    f.Cpu = fakeCpu;
                    f.Children.OfType<Segment>().SelectMany(s => s.AllPorts).Iter(p => p.OwnerCpu = fakeCpu);
                    InitializeFlow(f, false, opc);
                }
            }


            foreach (var f in activeFlows)
                InitializeFlow(f, true, opc);

            cpu.BuildBackwardDependency();
            fakeCpu?.BuildBackwardDependency();

            opc._cpus.Add(cpu);
            if (fakeCpu != null)
                opc._cpus.Add(fakeCpu);

            // debugging
            Debug.Assert(cpu.CollectBits().All(b => b.OwnerCpu == cpu));
            Debug.Assert(fakeCpu == null || fakeCpu.CollectBits().All(b => b.OwnerCpu == fakeCpu));
        }
    }
}
