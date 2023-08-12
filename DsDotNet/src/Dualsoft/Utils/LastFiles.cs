using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace DSModeler
{

    public static class LastFiles
    {
        public static void Set(string[] filePath)
        {
            DSRegistry.SetValue(K.LastFiles, String.Join("|", filePath));
        }
   

        public static string[] Get()
        {
            var recentlist = DSRegistry.GetValue(K.LastFiles);

            var recents = recentlist == null ? new List<string>()
                        : recentlist.ToString().Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

            return recents.ToArray();
        }
    }
}
