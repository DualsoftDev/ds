using DSModeler.Form;
using Engine.CodeGenCPU;
using System.Collections.Generic;
using System.Linq;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Cpu.RunTime;
using static Engine.Core.EdgeExt;

using static Model.Import.Office.ViewModule;
using Dual.Common.Core;
using Engine.Core;
using System.Reactive.Subjects;
using System;
using System.Windows.Forms;
using Dual.Common.Winform;

namespace DSModeler
{
    public static class ViewDraw
    {
        public static Dictionary<Vertex, Status4> DicStatus;
        public static Dictionary<DsTask, IEnumerable<Vertex>> DicTask;
        public static Subject<Tuple<CoreModule.Vertex, Status4>> StatusChangeSubject = new Subject<Tuple<CoreModule.Vertex, Status4>>();
        public static Subject<Tuple<CoreModule.Vertex, object>> ActionChangeSubject = new Subject<Tuple<CoreModule.Vertex, object>>();

        public static void DrawInitStatus(FormMain formMain, Dictionary<DsSystem, CpuLoader.PouGen> dicCpu)
        {
            DicStatus = new Dictionary<Vertex, Status4>();
            foreach (var item in dicCpu)
            {
                var sys = item.Key;
                var reals = sys.GetVertices().OfType<Vertex>();
                foreach (var r in reals)
                    ViewDraw.DicStatus.Add(r, Status4.Homing);
            }

            StatusChangeSubject.Subscribe(rx =>
            {
                formMain.Do(() =>
                {
                    var ret = GetViewNode(formMain, rx.Item1);
                    foreach (var r in ret)
                    {
                        var form = r.Item1;
                        var node = r.Item2;
                            node.Status4 = rx.Item2;
                            form.UcView.UpdateStatus(node);
                    }
                });
            });
        }

        private static IEnumerable<Tuple<FormDocView, ViewNode>> GetViewNode(FormMain formMain, Vertex v)
        {
            var visibleFroms = formMain.TabbedView.Documents
                                .Where(w => w.IsVisible)
                                .Select(s => s.Tag)
                                .OfType<FormDocView>()
                                .Where(w => w.UcView.Flow == v.Parent.GetFlow());



            return visibleFroms.Select(s => Tuple.Create(s, s.UcView.MasterNode.UsedViewVertexNodes(false)[v]));
        }

        public static void DrawInitActionTask(FormMain formMain, Dictionary<DsSystem, CpuLoader.PouGen> dicCpu)
        {
            DicTask = new Dictionary<DsTask, IEnumerable<Vertex>>();
            foreach (var item in dicCpu)
            {
                var sys = item.Key;
                var calls = sys.GetVertices().OfType<Call>();
                calls.SelectMany(s => s.CallTargetJob.DeviceDefs)
                     .Distinct()
                     .Iter(d =>
                     {
                         var finds = calls.Where(w => w.CallTargetJob.DeviceDefs.Contains(d));
                         DicTask.Add(d, finds);
                     });
            }

            ActionChangeSubject.Subscribe(rx =>
            {
                formMain.Do(() =>
                {
                    var ret = GetViewNode(formMain, rx.Item1);
                    foreach (var r in ret)
                    {
                        var form = r.Item1;
                        var node = r.Item2;
                        form.UcView.UpdateValue(node, rx.Item2);
                    }
                });
            });
        }


        public static void DrawStatus(ViewNode v, FormDocView view)
        {
            var viewNodes = v.UsedViewNodes.Where(w => w.CoreVertex != null);
            foreach (var f in viewNodes)
            {
                if (DicStatus.ContainsKey(f.CoreVertex.Value))
                {
                    f.Status4 = DicStatus[f.CoreVertex.Value];
                    view.UcView.UpdateStatus(f);
                }
            }
        }


    }
}


