using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;

namespace DSModeler;

[SupportedOSPlatform("windows")]
public static class RecentDocs
{
    public static void SetRecentDoc(IEnumerable<string> docs)
    {
        DSRegistry.SetValue(K.LastDocs, String.Join("|;|", docs));
    }



    public static List<string> GetRegistryRecentDocs()
    {
        var recentlist = DSRegistry.GetValue(K.LastDocs);

        var recents = recentlist == null ? new List<string>()
                    : recentlist.ToString().Split("|;|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
        return recents;
    }
}
