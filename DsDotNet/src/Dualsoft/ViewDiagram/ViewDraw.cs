using DSModeler.Form;
using Engine.CodeGenCPU;
using System.Collections.Generic;
using System.Linq;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Cpu.RunTime;
using static Engine.Core.EdgeExt;

using static Model.Import.Office.ViewModule;
namespace DSModeler
{
    public static class ViewDraw
    {
        public static Dictionary<Vertex, Status4> DicStatus = new Dictionary<Vertex, Status4>();

        public static void DrawInitStatus(Dictionary<DsSystem, CpuLoader.PouGen> dicCpu)
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


