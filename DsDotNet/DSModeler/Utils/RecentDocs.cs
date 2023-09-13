namespace DSModeler.Utils;

[SupportedOSPlatform("windows")]
public static class RecentDocs
{
    public static void SetRecentDoc(IEnumerable<string> docs)
    {
        DSRegistry.SetValue(RegKey.LastDocs, string.Join("|;|", docs));
    }



    public static List<string> GetRegistryRecentDocs()
    {
        object recentlist = DSRegistry.GetValue(RegKey.LastDocs);

        List<string> recents = recentlist == null ? new List<string>()
                    : recentlist.ToString().Split("|;|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
        return recents;
    }
}
