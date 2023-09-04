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

        public static string GetNewFileName(string path, string type)
        {
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileNameWithoutExtension(path);

            var newDirectory = $"{directory}\\{fileName}_{type}_autogen_{DateTime.Now:yyMMdd_HH_mm_ss}";
            Directory.CreateDirectory(newDirectory);

            return Path.Combine(newDirectory, Path.GetFileName(path));
        }
    }
}
