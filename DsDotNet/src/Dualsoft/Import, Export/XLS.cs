using Model.Import.Office;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Engine.Core.CoreModule;

namespace DSModeler
{
    public static class XLS
    {
        public static void ExportExcel()
        {
            if (!Global.IsLoadedPPT()) return;
            Task.Run(() =>
            {
                var pathXLS = Path.ChangeExtension(Files.GetLast().First(), "xlsx");
                var newPath = Files.GetNewPath(pathXLS);
                Global.ExportPathXLS = newPath;
                ExportIOTable.ToFiie(new List<DsSystem>() { Global.ActiveSys }, newPath);
                Global.Logger.Info($"{newPath} 생성완료!!");
                Process.Start($"{newPath}");
            });
        }

    }
}


