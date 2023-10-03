using Dual.Common.Core;
using Engine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Core.EdgeExt;
using static Engine.Core.TagKindModule;
using static Engine.Core.TagKindModule.TagDS;
using static Engine.Import.Office.ViewModule;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Diagram.View.MSAGL
{

    public class ViewVertex
    {
        public List<ViewNode> ViewNodes { get; set; }  //alias 포함
        public Status4 Status { get; set; }
        public List<DsTask> DsTasks { get; set; }
        public bool ErrorTX { get; set; }
        public bool ErrorRX { get; set; }
        public object Value { get; set; }
    }

    public static class ViewUtil
    {

        public static Dictionary<Vertex, ViewVertex> DicNode;
        public static ViewVertex CreateViewVertex(Vertex v, List<ViewNode> viewNodes, List<DsTask> tasks)
        {
            return new ViewVertex()
            {
                ViewNodes = viewNodes,
                Status = Status4.Homing,
                DsTasks = tasks,
            };
        }

        //public static void DrawInit(DsSystem system, IEnumerable<ViewNode> flowNodes)
        //{
        //    DicNode = new();

        //    var dicViewNodes =
        //        flowNodes.SelectMany(v=> v.UsedViewNodes.Where(w => w.CoreVertex != null))
        //                 .Select(s => s.CoreVertex.Value)
        //                 .ToDictionary(s => s.CoreVertex.Value, s => s);
        
        //    foreach (Vertex v in system.GetVertices().OfType<Vertex>())
        //    {
        //        List<DsTask> tasks = new();
        //        if (v is Call c)
        //            tasks = c.CallTargetJob.DeviceDefs.Cast<DsTask>().ToList();
        //        DicNode.Add(v, CreateViewVertex(v, dicViewNodes[v], tasks));
        //    }
        //}


        //private static IDisposable _Disposable;
        //public static void ViewChangeSubject()
        //{
        //    _Disposable?.Dispose();
        //    _Disposable = VertexChangeSubject.Subscribe(rx =>
        //    {
        //        if (rx.IsEventVertex)
        //        {
        //            EventVertex ev = rx as EventVertex;

        //            var node = DicNode[ev.Target];
        //            Dictionary<Vertex, ViewNode> nodes = view.MasterNode.UsedViewVertexNodes();
        //            if (nodes.ContainsKey(ev.Target))
        //            {
        //                ViewNode node = nodes[ev.Target];
        //                switch (ev.TagKind)
        //                {
        //                    case VertexTag.ready: node.Status4 = Status4.Ready; break;
        //                    case VertexTag.going: node.Status4 = Status4.Going; break;
        //                    case VertexTag.finish: node.Status4 = Status4.Finish; break;
        //                    case VertexTag.homing: node.Status4 = Status4.Homing; break;
        //                    default: break;
        //                }
        //                view.UpdateStatus(node);
        //            }
        //        }

        //        if (rx.IsEventAction)
        //        {
        //            //EventAction ea = rx as EventAction;
        //            //var vertex = rx.
        //            //var value = Convert.ToBoolean(ea.Item2);

        //            //var status = DicSV[vertex].Item1;
        //            //var txErr = DicSV[vertex].Item3;
        //            //var rxErr = DicSV[vertex].Item4;

        //            //DicSV[vertex] = Tuple.Create(status, value, txErr, rxErr);

        //            //Dictionary<Vertex, ViewNode> nodes = view.MasterNode.UsedViewVertexNodes();
        //            //if (nodes.ContainsKey(rx.Item1))
        //            //{
        //            //    ViewNode node = nodes[rx.Item1];
        //            //    view.UpdateValue(node, rx.Item2);
        //            //}
        //        }
        //    });
        //}







        public static Dictionary<DsTask, IEnumerable<Vertex>> DicTask;
        public static Dictionary<Vertex, Tuple<Status4, bool, bool, bool>> DicSV;

        public static Subject<TagDS> VertexChangeSubject = new();
        public static Subject<Tuple<Vertex, object>> ActionChangeSubject = new();




        public static void DrawInitStatus(IEnumerable<DsSystem> systems)
        {
            DicSV = new Dictionary<Vertex, Tuple<Status4, bool, bool, bool>>();
            foreach (var sys in systems)
            {
                IEnumerable<Vertex> reals = sys.GetVertices().OfType<Vertex>();
                foreach (Vertex r in reals)
                {
                    DicSV.Add(r, Tuple.Create(Status4.Homing, false, false, false));
                }
            }
        }

        public static void DrawInitActionTask(IEnumerable<DsSystem> systems)
        {
            DicTask = new Dictionary<DsTask, IEnumerable<Vertex>>();
            foreach (var sys in systems)
            {
                IEnumerable<Call> calls = sys.GetVertices().OfType<Call>();
                calls.SelectMany(s => s.CallTargetJob.DeviceDefs)
                     .Distinct()
                     .Iter(d =>
                     {
                         IEnumerable<Call> finds = calls.Where(w => w.CallTargetJob.DeviceDefs.Contains(d));
                         DicTask.Add(d, finds);
                     });
            }
        }


        public static void DrawStatusNValue(ViewNode v, UcView view)
        {
            IEnumerable<ViewNode> viewNodes = v.UsedViewNodes.Where(w => w.CoreVertex != null);
            foreach (ViewNode f in viewNodes)
            {
                if (DicSV.ContainsKey(f.CoreVertex.Value))
                {
                    f.Status4 = DicSV[f.CoreVertex.Value].Item1;
                    var value = DicSV[f.CoreVertex.Value].Item2;
                    var txErr = DicSV[f.CoreVertex.Value].Item3;
                    var rxErr = DicSV[f.CoreVertex.Value].Item4;
                    view.UpdateStatus(f);
                    view.UpdateError(f, txErr, rxErr);
                    view.UpdateValue(f, value);
                }
            }
        }


        private static IDisposable _DisposableV;
        public static void ViewDrawStatusChangeSubject(UcView view)
        {
            _DisposableV?.Dispose();
            _DisposableV = VertexChangeSubject.Subscribe(rx =>
            {
                if (rx.IsEventVertex)
                {
                    EventVertex ev = rx as EventVertex;
                    Dictionary<Vertex, ViewNode> nodes = view.MasterNode.UsedViewVertexNodes();
                    if (nodes.ContainsKey(ev.Target))
                    {
                        ViewNode node = nodes[ev.Target];
                        switch (ev.TagKind)
                        {
                            case VertexTag.ready: node.Status4 = Status4.Ready; break;
                            case VertexTag.going: node.Status4 = Status4.Going; break;
                            case VertexTag.finish: node.Status4 = Status4.Finish; break;
                            case VertexTag.homing: node.Status4 = Status4.Homing; break;
                            default: break;
                        }
                        view.UpdateStatus(node);
                    }
                }

                if (rx.IsEventAction)
                {
                    //EventAction ea = rx as EventAction;
                    //var vertex = rx.
                    //var value = Convert.ToBoolean(ea.Item2);

                    //var status = DicSV[vertex].Item1;
                    //var txErr = DicSV[vertex].Item3;
                    //var rxErr = DicSV[vertex].Item4;

                    //DicSV[vertex] = Tuple.Create(status, value, txErr, rxErr);

                    //Dictionary<Vertex, ViewNode> nodes = view.MasterNode.UsedViewVertexNodes();
                    //if (nodes.ContainsKey(rx.Item1))
                    //{
                    //    ViewNode node = nodes[rx.Item1];
                    //    view.UpdateValue(node, rx.Item2);
                    //}
                }
            });
        }


        private static IDisposable _DisposableA;
        public static void ViewDrawActionChangeSubject(UcView view)
        {
            _DisposableA?.Dispose();
            _DisposableA = ActionChangeSubject.Subscribe(rx =>
            {
                var vertex = rx.Item1;
                var value = Convert.ToBoolean(rx.Item2);

                var status = DicSV[vertex].Item1;
                var txErr = DicSV[vertex].Item3;
                var rxErr = DicSV[vertex].Item4;

                DicSV[vertex] = Tuple.Create(status, value, txErr, rxErr);

                Dictionary<Vertex, ViewNode> nodes = view.MasterNode.UsedViewVertexNodes();
                if (nodes.ContainsKey(rx.Item1))
                {
                    ViewNode node = nodes[rx.Item1];
                    view.UpdateValue(node, rx.Item2);
                }
            });
        }
    }
}


