using Engine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Core.EdgeExt;
using static Engine.Import.Office.ViewModule;

namespace PowerPointAddInForDS
{
    public static class ViewDraw
    {
        public static Dictionary<Vertex, Status4> DicStatus;
        public static Dictionary<DsTask, IEnumerable<Vertex>> DicTask;
        public static Subject<CoreModule.Vertex> StatusChangeSubject = new Subject<Vertex>();
        public static Subject<Tuple<CoreModule.Vertex, object>> ActionChangeSubject = new Subject<Tuple<CoreModule.Vertex, object>>();

        public static void DrawInitStatus(DsSystem sys)
        {
            DicStatus = new Dictionary<Vertex, Status4>();
            IEnumerable<Vertex> reals = sys.GetVertices().OfType<Vertex>();
            foreach (Vertex r in reals)
            {
                ViewDraw.DicStatus.Add(r, Status4.Homing);
            }
        }



        public static void DrawStatus(ViewNode v, FormDocView view)
        {
            IEnumerable<ViewNode> viewNodes = v.UsedViewNodes.Where(w => w.CoreVertex != null);
            foreach (ViewNode f in viewNodes)
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


