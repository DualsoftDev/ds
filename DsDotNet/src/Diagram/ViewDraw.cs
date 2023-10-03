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
namespace Diagram.View.MSAGL
{
    public static class ViewUtil
    {

        public static Dictionary<DsTask, IEnumerable<Vertex>> DicTask;
        public static Dictionary<Vertex, Tuple<Status4, bool, bool, bool>> DicSV;

        public static Subject<EventVertex> VertexChangeSubject = new();
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


        private static IDisposable _Disposable;
        public static void ViewDrawStatusChangeSubject(UcView view)
        {
            _Disposable?.Dispose();
            _Disposable = VertexChangeSubject.Subscribe(rx =>
                {
                    Dictionary<Vertex, ViewNode> nodes = view.MasterNode.UsedViewVertexNodes();
                    if (nodes.ContainsKey(rx.Target))
                    {
                        ViewNode node = nodes[rx.Target];
                        switch (rx.TagKind)
                        {
                            case VertexTag.ready: node.Status4 = Status4.Ready; break;
                            case VertexTag.going: node.Status4 = Status4.Going; break;
                            case VertexTag.finish: node.Status4 = Status4.Finish; break;
                            case VertexTag.homing: node.Status4 = Status4.Homing; break;
                            default: break;
                        }
                        view.UpdateStatus(node);
                    }
                });
        }
    }
}


