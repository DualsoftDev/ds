

namespace DSModeler;
[SupportedOSPlatform("windows")]
public static class XLS
{
    public static void ExportExcel(GridControl gc)
    {
        if (!Global.IsLoadedPPT())
        {
            return;
        }

        if (Global.BusyCheck())
        {
            return;
        }

        _ = Task.Run(() =>
        {
            gc.Do(() =>
            {
                string pathXLS = Path.ChangeExtension(Files.GetLast().First(), "xlsx");
                string newPath = Files.GetNewFileName(pathXLS, "XLS", true);
                Global.ExportPathXLS = newPath;

                System.Data.DataTable datatable = ExportIOTable.ToDataSet(Global.ActiveSys);
                gc.DataSource = datatable;
                ((GridView)gc.MainView).OptionsView.ColumnAutoWidth = false;
                ((GridView)gc.MainView).OptionsPrint.AutoWidth = false;
                ((GridView)gc.MainView).BestFitColumns();

                // Get its XLSX export options.
                XlsxExportOptionsEx options = new()
                {
                    ShowGridLines = true,
                    AllowSortingAndFiltering = DevExpress.Utils.DefaultBoolean.False,
                    ExportType = DevExpress.Export.ExportType.WYSIWYG,
                    TextExportMode = TextExportMode.Text,
                    SheetName = "IO LIST"
                };

                gc.ExportToXlsx(newPath, options);
                Global.Logger.Info($"{newPath} 생성완료!!");
                _ = Process.Start(new ProcessStartInfo { FileName = Path.GetDirectoryName(newPath), UseShellExecute = true });
            });
        });
    }
}