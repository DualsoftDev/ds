using DSModeler.Form;
using System.Linq;
using static Model.Import.Office.ViewModule;

namespace DSModeler
{
    public static class ViewDraw
    {

        public static void DrawStatus(FormMain form, ViewNode v, FormDocView view)
        {
            var viewNodes = v.UsedViewNodes.Where(w => w.CoreVertex != null);
            foreach (var f in viewNodes)
            {
                if (form.DicStatus.ContainsKey(f.CoreVertex.Value))
                {
                    f.Status4 = form.DicStatus[f.CoreVertex.Value];
                    view.UcView.UpdateStatus(f);
                }
            }
        }
    }
}


