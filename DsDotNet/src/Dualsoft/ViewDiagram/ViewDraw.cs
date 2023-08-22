using DSModeler.Form;
using System.Collections.Generic;
using System.Linq;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Model.Import.Office.ViewModule;

namespace DSModeler
{
    public static class ViewDraw
    {
        public static Dictionary<Vertex, Status4> DicStatus = new Dictionary<Vertex, Status4>();


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


