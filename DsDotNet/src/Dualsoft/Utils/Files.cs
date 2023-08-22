using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DSModeler
{

    public static class Files
    {
        public static void SetLast(string[] filePath)
        {
            DSRegistry.SetValue(K.LastFiles, String.Join("|", filePath));
        }


        public static string[] GetLast()
        {
            var recentlist = DSRegistry.GetValue(K.LastFiles);

            var recents = recentlist == null ? new List<string>()
                        : recentlist.ToString().Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

            return recents.ToArray();
        }

        public static string GetNewPath(string path)
        {
            var newPath = Path.Combine(Path.GetDirectoryName(path)
                        , string.Join("_", Path.GetFileNameWithoutExtension(path)));

            var extension = Path.GetExtension(path);
            var excelName = Path.GetFileNameWithoutExtension(newPath) + $"_{DateTime.Now.ToString("yyMMdd(HH-mm-ss)")}.{extension}";
            var excelDirectory = Path.Combine(Path.GetDirectoryName(newPath), Path.GetFileNameWithoutExtension(excelName));
            Directory.CreateDirectory(excelDirectory);

            return Path.Combine(excelDirectory, excelName);
        }
    }
}
