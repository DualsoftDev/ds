using System.IO;

namespace Dual.Model.Import
{
    public static class FileWatcher
    {

        //  NotifyFilters.Attributes                //| NotifyFilters.CreationTime
        //| NotifyFilters.DirectoryName                //| NotifyFilters.FileName
        //| NotifyFilters.LastAccess                //| NotifyFilters.LastWrite
        //| NotifyFilters.Security                //| NotifyFilters.Size
        static private string watchPath = "";
        public static void CreateFileWatcher(string path)
        {
            watchPath = path;
            var watcher = new FileSystemWatcher(Path.GetDirectoryName(path));
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
                && Path.GetExtension(e.FullPath) != ".xml"
                && Path.GetFileNameWithoutExtension(e.FullPath) != Path.GetFileNameWithoutExtension(watchPath))
            {
                ((FileSystemWatcher)sender).EnableRaisingEvents = false;
                FormMain.TheMain.ImportExcel(watchPath);
                ((FileSystemWatcher)sender).EnableRaisingEvents = true;
            }
        }

    }

}

