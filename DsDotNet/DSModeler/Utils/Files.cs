namespace DSModeler.Utils;

[SupportedOSPlatform("windows")]
public static class Files
{
    public static void SetLast(string[] filePath)
    {
        DSRegistry.SetValue(RegKey.LastFiles, string.Join("|", filePath));
    }


    public static string[] GetLast()
    {
        object recentlist = DSRegistry.GetValue(RegKey.LastFiles);

        List<string> recents = recentlist == null ? new List<string>()
                    : recentlist.ToString().Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

        return recents.ToArray();
    }

    public static string GetNewFileName(string path, string type, bool fileNameTimeMarking = false)
    {
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileNameWithoutExtension(path);
        string fileExtension = Path.GetExtension(path);
        string dt = $"{DateTime.Now:yyMMdd_HH_mm_ss}";
        string newDirectory = $"{directory}\\{fileName}_{type}_autogen_{dt}";
        _ = Directory.CreateDirectory(newDirectory);
        string flieNamePost = fileNameTimeMarking ? $"{fileName}_{dt}" : fileName;
        return Path.Combine(newDirectory, $"{flieNamePost}{fileExtension}");
    }
}
