using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                foreach(var pV in vertices.Where(v => ! dict.ContainsKey(v)))
                {
                    switch(pV)
                    {
                        case PAlias a:
                            switch(a.AliasTarget)
                            {
                                case PSegment pSeg:
                                    var aliasTarget = pick<Segment>(pSeg);
                                    dict.Add(a, new SegmentAlias(a.Name, pick<Flow>(a.ContainerFlow), aliasTarget));
                                    break;
                                case PCall call:
                                    break;
                                default:
                                    throw new Exception("ERROR");
                            }
                            break;
                        case PSegment s:
                            dict.Add(s, new Segment(s.Name, pick<RootFlow>(s.ContainerFlow)));
                            break;
                        case PCall pCall:
                            var container = pick<IWallet>(pCall.Container);
                            dict.Add(pCall, new Call(pCall.Name, container, pick<CallPrototype>(pCall.Prototype)));
                            break;
                        default:
                            throw new Exception("ERROR");
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
                        if (!flow.ChildVertices.Contains(v))
                            flow.ChildVertices.Add(v);
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
                }


                foreach (var pSys in pModel.Systems)
                {
                    var sys = pick<DsSystem>(pSys);

                    foreach (var pFlow in pSys.RootFlows.OfType<PRootFlow>())
                    {
                        var rootFlow = pick<RootFlow>(pFlow, () => new RootFlow(pFlow.Name, sys));
                    }
                }

                foreach (var pSys in pModel.Systems)
                {
                    var sys = pick<DsSystem>(pSys);

                    foreach (var pFlow in pSys.RootFlows.OfType<PRootFlow>())
                    {
                        var rootFlow = pick<RootFlow>(pFlow);
                        foreach (var pSeg in pFlow.Segments)
                        {
                            var seg = pick<Segment>(pSeg, () => new Segment(pSeg.Name, rootFlow));
                        }
                    }
                }

                var pRootFlows = pModel.Systems.SelectMany(psys => psys.RootFlows);
                var pChildFlows =
                    pRootFlows
                        .SelectMany(prf => prf.Children.OfType<PSegment>())
                        .Select(pseg => pseg.ChildFlow)
                        .Where(cf => cf != null)
                        .Distinct()
                        .ToArray()
                        ;
                foreach (var pcf in pChildFlows)
                {
                    var cf = pick<Segment>(pcf.ContainerSegment).ChildFlow;
                    Debug.Assert(cf != null);
                    dict.Add(pcf, cf);
                    //var childFlow_ = pick<ChildFlow>(pcf, () => new ChildFlow(pcf.Name, seg));
                }

                //var pRootSegements = pRootFlows.SelectMany(rf => rf.Children.OfType<PSegment>()).Distinct().ToArray();
                foreach (var pSys in pModel.Systems)
                {
                    var sys = pick<DsSystem>(pSys);

                    foreach (var pFlow in pSys.RootFlows.OfType<PRootFlow>())
                    {
                        var rootFlow = pick<RootFlow>(pFlow);
                        foreach (var pSeg in pFlow.Segments)
                        {
                            var seg = pick<Segment>(pSeg, () => new Segment(pSeg.Name, rootFlow));
                        }
                    }
                }





                foreach (var pSys in pModel.Systems)
                {
                    var sys = pick<DsSystem>(pSys);

                    foreach (var pFlow in pSys.RootFlows.OfType<PRootFlow>())
                    {
                        var rootFlow = pick<RootFlow>(pFlow);
                        foreach (var pSeg in pFlow.Segments)
                        {
                            var seg = pick<Segment>(pSeg);
                            foreach (var pCh in pSeg.Children)
                            {
                                switch(pCh)
                                {
                                    case PCall pChCall:
                                        var callProto = pick<CallPrototype>(pChCall.Prototype);
                                        var container = pick<IWallet>(pChCall.Container);
                                        var call = pick<Call>(pChCall, () => new Call(pChCall.Name, container, callProto));
                                        break;
                                    case PAlias pAlias:
                                        break;

                                    case PSegment pChSeg:
                                        //var chSeg = pick<Segment>(pChSeg);
                                        //Debug.Assert(chSeg != null);
                                        //Debug.Assert(chSeg.ContainerFlow.System != seg.ContainerFlow.System);
                                        break;

                                    default:
                                        throw new Exception("ERROR");
                                }

                            }
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
                foreach (var pSys in pModel.Systems)
                {
                    foreach (var pFlow in pSys.RootFlows.OfType<PRootFlow>())
                    {
                        foreach (var pSeg in pFlow.Segments)
                        {
                            foreach (var pCh in pSeg.Children)
                            {
                                switch (pCh)
                                {
                                    case PAlias pAlias:
                                        {
                                            var pa = pAlias;
                                            var container = pick<Flow>(pAlias.ContainerFlow);
                                            var pTarget = pAlias.AliasTarget;
                                            switch (pTarget)
                                            {
                                                case PCallPrototype pCp:
                                                    var cp = pick<CallPrototype>(pCp);
                                                    var call = pick<Call>(pAlias, () => new CallAlias(pAlias.Name, pAlias.AliasTargetName, container, cp));
                                                    break;
                                                case PSegment seg:
                                                    var target = pick<Segment>(pTarget);
                                                    var _ = new SegmentAlias(pAlias.Name, container, target);
                                                    break;
                                                default:
                                                    throw new Exception("ERROR");
                                            }
                                        }
                                        break;
                                    case PCall _:
                                    case PSegment _:
                                        break;
                                    default:
                                        throw new Exception("ERROR");
                                }

                            }
                        }
                    }
                }
            }


            void thirdScan()
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
                            var child = (Coin)dict[pSegment];
                            if (!flow.ChildVertices.Contains(child))
                                flow.ChildVertices.Add(child);

                            if (pSegment.ChildFlow != null)
                            {
                                var segment = child as Segment;
                                fillEdges(segment.ChildFlow, pSegment.ChildFlow);
                                segment.ChildFlow.Cpu = flow.Cpu;
                            }
                        }

                        fillEdges(flow, pFlow);


                        foreach (var s in flow.ChildVertices.OfType<Segment>())
                        {
                            new Port[] { s.PortS, s.PortR, s.PortE }.Iter(p => p.OwnerCpu = flow.Cpu);
                        }
                    }
                }
            }

            void cleanUp()
            {
                //var rootFlows = model.Systems.SelectMany(s => s.RootFlows);

                //var segmentsWithEmptyFlow =
                //    from rf in rootFlows
                //    from s in rf.Children.OfType<Segment>()
                //    where s.ChildFlow.IsEmptyFlow
                //    select s
                //    ;

                //foreach (var s in segmentsWithEmptyFlow)
                //    s.ChildFlow = null;


                //// root flow 의 sub flow 중 empty 인 것들 정리
                //var emptySubFlows =
                //    from s in model.Systems
                //    from rf in s.RootFlows
                //    from sf in rf.SubFlows
                //    where sf.IsEmptyFlow
                //    select (rf, sf)
                //    ;
                //foreach ((var rootFlow, var subFlow) in emptySubFlows.ToArray())
                //    rootFlow.SubFlows.Remove(subFlow);
            }


            firstScan();
            secondScan();
            thirdScan();
            cleanUp();

            return model;
        }
    }
}
