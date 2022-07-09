using System;
using System.Collections.Generic;
using System.Linq;

using DsParser;

using Dsu.Common.Utilities.ExtensionMethods;

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

            Model model = pick<Model>(pModel, () => new Model());

            foreach (var pSys in pModel.Systems)
            {
                var sys = pick<DsSystem>(pSys, () => new DsSystem(pSys.Name, model));
                foreach (var pFlow in pSys.Flows.OfType<PRootFlow>())
                {
                    var rootFlow = pick<RootFlow>(pFlow, () => new RootFlow(pFlow.Name, sys));
                    foreach (var pSeg in pFlow.Segments)
                    {
                        var seg_ = pick<Segment>(pSeg, () => new Segment(pSeg.Name, rootFlow));
                    }
                }
                foreach (var pTask in pSys.Tasks)
                {
                    var task = pick<Task>(pTask, () => new Task(pTask.Name, sys));
                    foreach (var pCall in pTask.Calls)
                    {
                        var call_ = pick<CallPrototype>(pCall, () => new CallPrototype(pCall.Name, task));
                    }
                }

            }
            foreach (var pCpu in pModel.Cpus)
            {
                var flows = pCpu.Flows.Select(pf => pick<Flow>(pf)).ToArray();
                var cpu = pick<Cpu>(pCpu, () => new Cpu(pCpu.Name, flows, model));
            }


            // second scan : fill edge, call tx, rx
            foreach (var pSys in pModel.Systems)
            {
                foreach (var pTask in pSys.Tasks)
                {
                    foreach (var pCall in pTask.Calls)
                    {
                        var tx = pick<Segment>(pCall.TX);
                        var rx = pick<Segment>(pCall.RX);
                        var call = pick<CallPrototype>(pCall);
                        call.TXs.Add(tx);
                        call.RX = rx;
                    }
                }

                foreach (var pFlow in pSys.Flows.OfType<PRootFlow>())
                {
                    var flow = pick<RootFlow>(pFlow);
                    foreach (var pEdge in pFlow.Edges)
                    {
                        IVertex convertOnCall(IVertex v)
                        {
                            var callProto = v as CallPrototype;
                            if (callProto == null)
                                return v;

                            Call call = flow.Children.OfType<Call>().FirstOrDefault(c => c.Prototype == callProto);
                            if (call == null)
                            {
                                call = new Call(callProto.Name, callProto);
                                flow.Children.Add(call);
                            }

                            return call;
                        }
                        var ss = pEdge.Sources.Select(pS => convertOnCall(pick<IVertex>(pS))).ToArray();
                        var t = convertOnCall(pick<IVertex>(pEdge.Target));
                        var op = pEdge.Operator;

                        //Edge edge = op switch
                        //{
                        //    ">" => new WeakSetEdge(ss, op, t),
                        //    ">>" => new StrongSetEdge(ss, op, t),
                        //    "|>" => new WeakResetEdge(ss, op, t),
                        //    "|>>" => new StrongResetEdge(ss, op, t),
                        //    _ => throw new Exception("ERROR"),
                        //};
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


                        flow.Edges.Add(edge);
                    }

                    foreach (var s in flow.Children.OfType<Segment>())
                    {
                        new Port[] { s.PortS, s.PortR, s.PortE }.Iter(p => p.OwnerCpu = flow.Cpu);
                    }
                }
            }

            return model;
        }
    }
}
