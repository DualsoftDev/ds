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
using DevExpress.XtraBars.Docking2010.Views.Tabbed;

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


