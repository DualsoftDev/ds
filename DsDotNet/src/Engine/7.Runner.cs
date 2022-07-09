using Dsu.Common.Utilities.ExtensionMethods;

using Engine.Core;

using System.Linq;

namespace Engine
{
    public static class CpuRunner
    {
        public static void Run(this Cpu cpu)
        {
        }

        public static void Initialize(this Cpu cpu)
        {
            var rootFlows = cpu.Flows.OfType<RootFlow>();
            foreach (var flow in rootFlows)
                flow.GenereateHmiTags();

            cpu.BuildBackwardDependency();

            // cpu 기준으로 call 에 사용된 TX 및 RX 의 Tag 값 external 로 marking
            var otherFlows =
                from system in cpu.Model.Systems
                from flow in system.Flows
                where !(cpu.Flows.Contains(flow))
                select flow
                ;

            var TxRxs =
                otherFlows
                    .SelectMany(f => f.Edges)
                    .SelectMany(e => e.Vertices)
                    .OfType<CallPrototype>()
                    .Select(c => (c.TXs, c.RX))
                    ;

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
                    var s = rx as Segment;
                    var tags = s.ContainerFlow.Cpu.ForwardDependancyMap[s.PortE].OfType<Tag>();
                    tags.Iter(tag =>
                    {
                        tag.Type = TagType.I;
                        tag.IsExternal = true;
                    });
                }
            }

            cpu.PrintTags();
            foreach (var flow in rootFlows)
                flow.PrintFlow();
        }
    }
}
