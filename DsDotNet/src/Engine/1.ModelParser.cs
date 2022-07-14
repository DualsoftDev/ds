using System;
using System.Collections.Generic;
using System.Linq;

using DsParser;

using Engine.Common;
using Engine.Core;

namespace Engine
{
    /// <summary>
    /// Parser 에서 만든 구조체를 Engine 용 구조체로 변환
    /// Parser 에서는 parsing 에 충실, engine 에서는 engine 에 맞는 추가 작업 필요해서 이원화.
    /// </summary>
    class ModelParser
    {
        public static Model ParseFromString(string text)
        {
            var pModel = DsG4ModelParser.ParseFromString(text);
            return Convert(pModel);
        }

        /// Parser 에서 만든 구조체를 Engine 용 구조체로 변환
        static Model Convert(PModel pModel)
        {
            // parser 구조체와 이에 대응하는 engine 구조체의 dictionary
            var dict = new Dictionary<object, object>();

            T pick<T>(object old, Func<T> creator = null) where T : class
            {
                if (dict.ContainsKey(old))
                    return (T)dict[old];

                if (creator == null)
                    throw new Exception("ERROR");

                var t = creator();
                dict[old] = t;

                return t;
            }

            void preparePick(IEnumerable<IPVertex> vertices)
            {
                foreach(var pV in vertices)
                {
                    var pSegment = pV as PSegment;
                    if (pSegment != null && !dict.ContainsKey(pSegment))
                        dict.Add(pSegment, new Segment(pSegment.Name, pick<RootFlow>(pSegment.ContainerFlow)));

                    var pCall = pV as PCall;
                    if (pCall != null && !dict.ContainsKey(pCall))
                    {
                        var container = pick<ISegmentOrFlow>(pCall.Container);
                        dict.Add(pCall, new Call(pCall.Name, container, pick<CallPrototype>(pCall.Prototype)));
                    }
                }
            }

            void fillEdges(Flow flow, PFlow pFlow)
            {
                if (pFlow == null)
                    return;

                if (flow == null)
                {
                    var pChildFlow = pFlow as PChildFlow;
                    if (pFlow is PRootFlow)
                        flow = pick<RootFlow>(pFlow, () => new RootFlow(pFlow.Name, pick<DsSystem>(pFlow.System)));
                    else if (pChildFlow != null)
                        flow = pick<ChildFlow>(pChildFlow, () => new ChildFlow(pChildFlow.Name, pick<Segment>(pChildFlow.ContainerSegment)));
                }


                foreach (var pEdge in pFlow.Edges)
                {
                    preparePick(pEdge.Vertices);
                    var ss = pEdge.Sources.Select(pS => dict[pS]).Cast<Core.IVertex>().ToArray();
                    var t = dict[pEdge.Target] as Core.IVertex;
                    var op = pEdge.Operator;

                    Edge edge = null;
                    switch (op)
                    {
                        case ">": edge = new WeakSetEdge(flow, ss, op, t); break;
                        case ">>": edge = new StrongSetEdge(flow, ss, op, t); break;
                        case "|>": edge = new WeakResetEdge(flow, ss, op, t); break;
                        case "|>>": edge = new StrongResetEdge(flow, ss, op, t); break;
                        default:
                            throw new Exception("ERROR");
                    };


                    flow.AddEdge(edge);

                    foreach ( var pV in pEdge.Vertices)
                    {
                        var v = pick<IVertex>(pV);
                        if (!flow.Children.Contains(v))
                            flow.Children.Add(v);
                        Console.WriteLine();


                    }

                }
            }

            Model model = pick<Model>(pModel, () => new Model());

            void firstScan()
            {
                foreach (var pSys in pModel.Systems)
                {
                    var sys = pick<DsSystem>(pSys, () => new DsSystem(pSys.Name, model));
                    foreach (var pTask in pSys.Tasks)
                    {
                        var task = pick<DsTask>(pTask, () => new DsTask(pTask.Name, sys));
                        foreach (var pCall in pTask.Calls)
                        {
                            var call_ = pick<CallPrototype>(pCall, () => new CallPrototype(pCall.Name, task));
                        }
                    }

                    foreach (var pFlow in pSys.RootFlows.OfType<PRootFlow>())
                    {
                        var rootFlow = pick<RootFlow>(pFlow, () => new RootFlow(pFlow.Name, sys));
                        foreach (var pSeg in pFlow.Segments)
                        {
                            var seg = pick<Segment>(pSeg, () => new Segment(pSeg.Name, rootFlow));
                            //foreach (var pEdge in pSeg.ChildFlow.Edges)
                            //{
                            //    var ss = pEdge.Sources.Select(pS => pick<IVertex>(pS)).ToArray();
                            //    var t = pick<IVertex>(pEdge.Target);
                            //    var edge = pick<Edge>(pEdge, () => new Edge(seg.ChildFlow, ss, pEdge.Operator, t));

                            //}

                            foreach (var pChCall in pSeg.Children?.OfType<PCall>())
                            {
                                var callProto = pick<CallPrototype>(pChCall.Prototype);
                                var container = pick<ISegmentOrFlow>(pChCall.Container);
                                var call = pick<Call>(pChCall, () => new Call(pChCall.Name, container, callProto));
                                Console.WriteLine();
                            }
                            Console.WriteLine();
                        }
                    }

                }
                foreach (var pCpu in pModel.Cpus)
                {
                    var flows = pCpu.RootFlows.Select(pf => pick<RootFlow>(pf)).ToArray();
                    var cpu = pick<Cpu>(pCpu, () => new Cpu(pCpu.Name, flows, model));
                }
            }

            void secondScan()
            {
                // second scan : fill edge, call tx, rx
                foreach (var pSys in pModel.Systems)
                {
                    foreach (var pTask in pSys.Tasks)
                    {
                        foreach (var pCall in pTask.Calls)
                        {
                            var call = pick<CallPrototype>(pCall);
                            var txs = pCall.TXs.Select(pTx => pTx == null ? null : pick<Segment>(pTx)).OfNotNull();
                            var rxs = pCall.RXs.Select(pRx => pRx == null ? null : pick<Segment>(pRx)).OfNotNull();


                            call.TXs.AddRange(txs);
                            call.RXs.AddRange(rxs);
                        }
                    }

                    foreach (var pFlow in pSys.RootFlows)
                    {
                        var flow = pick<RootFlow>(pFlow);

                        preparePick(pFlow.Segments);

                        foreach (var pSegment in pFlow.Segments)
                        {
                            var child = (SegmentOrCallBase)dict[pSegment];
                            if (!flow.Children.Contains(child))
                                flow.Children.Add(child);

                            if (pSegment.ChildFlow != null)
                            {
                                var segment = child as Segment;
                                fillEdges(segment.ChildFlow, pSegment.ChildFlow);
                                segment.ChildFlow.Cpu = flow.Cpu;

                                foreach (var px in pSegment.Children)
                                    Console.WriteLine();
                                foreach (var px in pSegment.ChildFlow.Edges)
                                    Console.WriteLine();
                            }
                        }

                        fillEdges(flow, pFlow);


                        foreach (var s in flow.Children.OfType<Segment>())
                        {
                            new Port[] { s.PortS, s.PortR, s.PortE }.Iter(p => p.OwnerCpu = flow.Cpu);
                        }
                    }
                }
            }

            void cleanUp()
            {
                var rootFlows = model.Systems.SelectMany(s => s.RootFlows);

                var segmentsWithEmptyFlow =
                    from rf in rootFlows
                    from s in rf.Children.OfType<Segment>()
                    where s.ChildFlow.IsEmptyFlow
                    select s
                    ;

                foreach (var s in segmentsWithEmptyFlow)
                    s.ChildFlow = null;


                // root flow 의 sub flow 중 empty 인 것들 정리
                var emptySubFlows =
                    from s in model.Systems
                    from rf in s.RootFlows
                    from sf in rf.SubFlows
                    where sf.IsEmptyFlow
                    select (rf, sf)
                    ;
                foreach ((var rootFlow, var subFlow) in emptySubFlows.ToArray())
                    rootFlow.SubFlows.Remove(subFlow);
            }


            firstScan();
            secondScan();
            cleanUp();

            return model;
        }
    }
}
