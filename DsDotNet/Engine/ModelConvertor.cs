using DsParser;

using System;
using System.Collections.Generic;
using System.Text;
using Dsu.Common.Utilities.ExtensionMethods;
using System.Linq;

namespace Engine
{
    class ModelConvertor
    {
        public static Model Convert(PModel pModel)
        {
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
                var cpu_ = pick<Cpu>(pCpu, () => new Cpu());
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
                        var ss = pEdge.Sources.Select(pS => pick<ISegmentOrCall>(pS)).ToArray();
                        var t = pick<ISegmentOrCall>(pEdge.Target);
                        var edge = pick<Edge>(pEdge, () => new Edge(ss, t));
                        flow.Edges.Add(edge);
                    }
                }
            }

            return model;
        }
    }
}
