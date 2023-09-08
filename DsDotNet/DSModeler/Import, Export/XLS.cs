using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraPrinting;
using Dual.Common.Winform;
using Engine.Import.Office;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace DSModeler;
[SupportedOSPlatform("windows")]
public static class XLS
{
    public static void ExportExcel(GridControl gc)
    {
        if (!Global.IsLoadedPPT()) return;
        if (Global.BusyCheck()) return;
        Task.Run(() =>
        {
            gc.Do(() =>
            {
                var pathXLS = Path.ChangeExtension(Files.GetLast().First(), "xlsx");
                var newPath = Files.GetNewFileName(pathXLS, "XLS", true);
                Global.ExportPathXLS = newPath;

                var datatable = ExportIOTable.ToDataSet(Global.ActiveSys);
                gc.DataSource = datatable;
                ((GridView)gc.MainView).OptionsView.ColumnAutoWidth = false;
                ((GridView)gc.MainView).OptionsPrint.AutoWidth = false;
                ((GridView)gc.MainView).BestFitColumns();

                // Get its XLSX export options.
                XlsxExportOptionsEx options = new XlsxExportOptionsEx();
                options.ShowGridLines = true;
                options.AllowSortingAndFiltering = DevExpress.Utils.DefaultBoolean.False;
                options.ExportType = DevExpress.Export.ExportType.WYSIWYG;
                options.TextExportMode = TextExportMode.Text;
                options.SheetName = "IO LIST";

                gc.ExportToXlsx(newPath, options);
                Global.Logger.Info($"{newPath} 생성완료!!");
                Process.Start($"{newPath}");
            });
        });
    }
}