using System;
using System.Collections.Generic;
using System.Linq;
using DsParser;
using Engine.Core;

namespace Engine
{
    /// <summary>
    /// Parser ���� ���� ����ü�� Engine �� ����ü�� ��ȯ
    /// Parser ������ parsing �� ���, engine ������ engine �� �´� �߰� �۾� �ʿ��ؼ� �̿�ȭ.
    /// </summary>
    class ModelParser
    {
        public static Model ParseFromString(string text)
        {
            var pModel = DsG4ModelParser.ParseFromString(text);
            return Convert(pModel);
        }

        /// Parser ���� ���� ����ü�� Engine �� ����ü�� ��ȯ
        static Model Convert(PModel pModel)
        {
            // parser ����ü�� �̿� �����ϴ� engine ����ü�� dictionary
            var dict = new Dictionary<object, object>();

            T pick<T>(object old, Func<T> creator=null) where T : class
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
                        var call_ = pick<Call>(pCall, () => new Call(pCall.Name, task));
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
                        var call = pick<Call>(pCall);
                        call.TX = tx;
                        call.RX = rx;
                    }
                }

                foreach (var pFlow in pSys.Flows.OfType<PRootFlow>())
                {
                    var flow = pick<Flow>(pFlow);
                    foreach (var pEdge in pFlow.Edges)
                    {
                        var ss = pEdge.Sources.Select(pS => pick<Core.ISegmentOrCall>(pS)).ToArray();
                        var t = pick<Core.ISegmentOrCall>(pEdge.Target);
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
                        switch(op)
                        {
                            case ">":  edge = new WeakSetEdge(ss, op, t); break;
                            case ">>": edge = new StrongSetEdge(ss, op, t); break;
                            case "|>": edge = new WeakResetEdge(ss, op, t); break;
                            case "|>>":edge = new StrongResetEdge(ss, op, t); break;
                            default:
                                throw new Exception("ERROR");
                        };


                        flow.Edges.Add(edge);
                    }
                }
            }

            return model;
        }
    }
}
