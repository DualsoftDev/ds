using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

namespace DSModeler;

[SupportedOSPlatform("windows")]
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

    public static string GetNewFileName(string path)
    {
        var directory = Path.GetDirectoryName(path);

        var newDirectory = directory + $"_{DateTime.Now:yyMMdd_HH_mm_ss}";
        Directory.CreateDirectory(newDirectory);

        return Path.Combine(newDirectory, Path.GetFileName(path));
    }
}
