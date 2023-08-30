using DevExpress.Data.Filtering;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using Dual.Common.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using static Engine.Core.CoreModule;

namespace DSModeler
{
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
}
