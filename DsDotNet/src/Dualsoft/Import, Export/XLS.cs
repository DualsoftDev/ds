using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraPrinting;
using Model.Import.Office;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Engine.Core.CoreModule;

namespace DSModeler
{
    public static class XLS
    {
        public static void ExportExcel(GridControl gc)
        {
            if (!Global.IsLoadedPPT()) return;
            if (Global.BusyCheck()) return;
            Task.Run(() =>
            {
                var pathXLS = Path.ChangeExtension(Files.GetLast().First(), "xlsx");
                var newPath = Files.GetNewPath(pathXLS);
                Global.ExportPathXLS = newPath;

                var datatable = ExportIOTable.ToDataSet(Global.ActiveSys);
                gc.DataSource = datatable;
                ((GridView)gc.MainView).OptionsView.ColumnAutoWidth = false;
                ((GridView)gc.MainView).OptionsPrint.AutoWidth = false;
                ((GridView)gc.MainView).BestFitColumns();

                gc.ExportToXlsx(newPath);
                //ExportIOTable.ToFile(new List<DsSystem>() { Global.ActiveSys }, newPath);
                Global.Logger.Info($"{newPath} 생성완료!!");
                Process.Start($"{newPath}");
            });
        }

    }
}


