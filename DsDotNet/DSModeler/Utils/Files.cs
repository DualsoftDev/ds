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

    public static string GetNewFileName(string path, string type, bool fileNameTimeMarking = false)
    {
        var directory = Path.GetDirectoryName(path);
        var fileName = Path.GetFileNameWithoutExtension(path);
        var fileExtension = Path.GetExtension(path);
        var dt = $"{DateTime.Now:yyMMdd_HH_mm_ss}";
        var newDirectory = $"{directory}\\{fileName}_{type}_autogen_{dt}";
        Directory.CreateDirectory(newDirectory);
        var flieNamePost = fileNameTimeMarking ? $"{fileName}_{dt}" : fileName;
        return Path.Combine(newDirectory, $"{flieNamePost}{fileExtension}");
    }
}
