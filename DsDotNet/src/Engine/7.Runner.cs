using Dsu.Common.Utilities.ExtensionMethods;

using Engine.Core;
using Engine.OPC;

using System.Collections.Generic;
using System.Linq;

namespace Engine
{
    public static class CpuRunner
    {
        public static void Run(this Cpu cpu)
        {
        }


        //public static void InitializeFlow(this RootFlow flow, ICpu cpu, OpcBroker opc)
        //{
        //    // flow 상의 root segment 들에 대한 HMI s/r/e tags
        //    var exportTags = flow.GenereateHmiTags4Segments();
        //    opc.AddTags(exportTags);

        //}

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


            //var model = cpu.Model;
            //// cpu 기준으로 active system 과 passive system 으로 구분
            //// active system 의 모든 root segment 의 auto S/R/E => Tag export
            //// active system 에서 사용한 모든 call 의 TX, RX => Tag imort
            //var rootFlows = cpu.RootFlows;



            var calls = vertices.OfType<Call>().ToArray();
            var txs = calls.SelectMany(c => c.TXs).OfType<Segment>().Distinct().ToArray();
            var rxs = calls.Select(c => c.RX).OfType<Segment>().Distinct().ToArray();

            opc.AddTags(hmiTags);
            opc.AddTags(txs.Select(s => s.TagS));
            opc.AddTags(rxs.Select(s => s.TagE));



            //// my CPU 의 Call 에서 사용한 TX 및 RX
            //var myTxRxs =
            //        myCalls
            //        .SelectMany(c => c.TxRxs)
            //        .Distinct().ToArray()
            //        ;

            //var calledFlows =
            //    myTxRxs.OfType<Segment>()
            //    .Select(s => s.ContainerFlow)
            //    .Distinct().ToArray()
            //    ;
            //var calledSystems = calledFlows.Select(f => f.System).Distinct().ToArray();


            //cpu.BuildBackwardDependency();

            //// cpu 기준으로 call 에 사용된 TX 및 RX 의 Tag 값 external 로 marking
            //var otherFlows =
            //    from system in cpu.Model.Systems
            //    from flow in system.RootFlows
            //    where !(cpu.RootFlows.Contains(flow))
            //    select flow
            //    ;

            //var TxRxs =
            //    otherFlows
            //        .SelectMany(f => f.Edges)
            //        .SelectMany(e => e.Vertices)
            //        .OfType<Call>()
            //        .Select(c => (c.TXs, c.RX))
            //        ;

            //foreach ((var txs, var rx) in TxRxs)
            //{
            //    foreach (var s in txs.OfType<Segment>())
            //    {
            //        var tags = s.ContainerFlow.Cpu.BackwardDependancyMap[s.PortS].OfType<Tag>();
            //        tags.Iter(tag =>
            //        {
            //            tag.Type = TagType.Q;
            //            tag.IsExternal = true;
            //        });
            //    }

            //    {
            //        var s = rx as Segment;
            //        var tags = s.ContainerFlow.Cpu.ForwardDependancyMap[s.PortE].OfType<Tag>();
            //        tags.Iter(tag =>
            //        {
            //            tag.Type = TagType.I;
            //            tag.IsExternal = true;
            //        });
            //    }
            //}





            //cpu.PrintTags();
            //foreach (var flow in rootFlows)
            //    flow.PrintFlow();
        }

        public static void InitializeMyFlows(this Cpu cpu, OpcBroker opc)
        {

        }

        //public static void InitializeOtherFlows(this Cpu activeCpu, OpcBroker opc)
        //{
        //    // cpu 기준으로 call 에 사용된 TX 및 RX 의 Tag 값 external 로 marking
        //    var otherFlows =
        //        from system in activeCpu.Model.Systems
        //        from flow in system.RootFlows
        //        where !(activeCpu.RootFlows.Contains(flow))
        //        select flow
        //        ;

        //    InitializeFlow(activeCpu, false, opc);
        //}

        public static void InitializeFlows(this Engine engine, Cpu cpu, OpcBroker opc)
        {
            var model = engine.Model;
            var flowsGrps =
                from system in model.Systems
                from flow in system.RootFlows
                group flow by cpu.RootFlows.Contains(flow) into g
                select new { Active = g.Key, Flows = g.ToList() };
                ;
            var activeFlows = flowsGrps.Where(gr => gr.Active).SelectMany(gr => gr.Flows);
            var otherFlows = flowsGrps.Where(gr => !gr.Active).SelectMany(gr => gr.Flows);

            otherFlows.Iter(f => InitializeFlow(f, false, opc));
            activeFlows.Iter(f => InitializeFlow(f, true, opc));


        }

    }
}
