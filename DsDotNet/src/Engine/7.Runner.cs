using Dsu.Common.Utilities.ExtensionMethods;

using Engine.Core;
using Engine.OPC;

using System.Collections.Generic;
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
            }


            flow.PrintFlow(isActiveCpu);
            //cpu.PrintTags();
            //foreach (var flow in rootFlows)
        }

        public static void InitializeFlows(this Engine engine, CpuBase cpu, OpcBroker opc)
        {
            var model = engine.Model;
            var flowsGrps =
                from system in model.Systems
                from flow in system.RootFlows
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
        }
    }
}
