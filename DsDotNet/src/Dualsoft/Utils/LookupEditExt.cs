using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using System.Drawing;
using System.Runtime.Versioning;

namespace DSModeler;
[SupportedOSPlatform("windows")]
public static class LookupEditExt
{
    public static void InitEdit(GridLookUpEdit gle, GridView gv)
    {
        gle.Properties.DisplayMember = "Display";

        gv.PreviewLineCount = 20;
        gv.OptionsSelection.EnableAppearanceFocusedCell = false;
        gv.OptionsView.ShowAutoFilterRow = true;
        gv.OptionsView.ShowGroupPanel = false;
        gv.CustomDrawCell += (s, e) =>
        {
            if (e.Column.FieldName == "IOType")
            {
                var cellValue = e.DisplayText.ToString().ToUpper();
                if (cellValue == "INPUT")
                    e.Cache.FillRectangle(Color.RoyalBlue, e.Bounds);
                else if (cellValue == "OUTPUT")
                    e.Cache.FillRectangle(Color.Salmon, e.Bounds);
                else
                    e.Cache.FillRectangle(Color.Transparent, e.Bounds);


                e.Appearance.DrawString(e.Cache, e.DisplayText, e.Bounds);
                e.Handled = true;
            }
        };
    }
}
