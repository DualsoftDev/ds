using DevExpress.XtraBars.Docking2010.Views.Tabbed;
using DSModeler.Form;
using Dual.Common.Core;
using Engine.CodeGenCPU;
using Engine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Core.EdgeExt;
using static Engine.Import.Office.ViewModule;

namespace DSModeler
{
    public static class ViewDraw
    {
        public static Dictionary<Vertex, Status4> DicStatus;
        public static Dictionary<DsTask, IEnumerable<Vertex>> DicTask;
        public static Subject<CoreModule.Vertex> StatusChangeSubject = new Subject<Vertex>();
        public static Subject<Tuple<CoreModule.Vertex, object>> ActionChangeSubject = new Subject<Tuple<CoreModule.Vertex, object>>();

        public static void DrawInitStatus(TabbedView tv, Dictionary<DsSystem, CpuLoader.PouGen> dicCpu)
        {
            DicStatus = new Dictionary<Vertex, Status4>();
            foreach (var item in dicCpu)
            {
                var sys = item.Key;
                var reals = sys.GetVertices().OfType<Vertex>();
                foreach (var r in reals)
                    ViewDraw.DicStatus.Add(r, Status4.Homing);
            }
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


