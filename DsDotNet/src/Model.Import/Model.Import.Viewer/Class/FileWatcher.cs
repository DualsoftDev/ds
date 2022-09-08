using Model.Import.Office;
using System;
using System.IO;
using System.Linq;
using static Model.Import.Office.Object;

namespace Dual.Model.Import
{
    public static class FileWatcher
    {

        //  NotifyFilters.Attributes                //| NotifyFilters.CreationTime
        //| NotifyFilters.DirectoryName                //| NotifyFilters.FileName
        //| NotifyFilters.LastAccess                //| NotifyFilters.LastWrite
        //| NotifyFilters.Security                //| NotifyFilters.Size

        public static void CreateFileWatcher()
        {
            var watcher = new FileSystemWatcher(Path.GetDirectoryName(FormMain.TheMain.PathXLS));
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += watcher_Changed;
            watcher.EnableRaisingEvents = true;

        }

        private static void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            //~$xxx.xlsx 백업 파일 무시
            //D:\DS_22_08_28(16-19-34)\C2129000 <- 파일 변경시 파일명 날라감
            if (e.ChangeType == WatcherChangeTypes.Changed
                && Path.GetExtension(e.FullPath) != ".xlsx"
                && Path.GetFileNameWithoutExtension(e.FullPath) != Path.GetFileNameWithoutExtension(FormMain.TheMain.PathXLS))
            {
                ((FileSystemWatcher)sender).EnableRaisingEvents = false;
                FormMain.TheMain.ImportExcel(FormMain.TheMain.PathXLS);
                ((FileSystemWatcher)sender).EnableRaisingEvents = true;
            }
        }

    }

}

