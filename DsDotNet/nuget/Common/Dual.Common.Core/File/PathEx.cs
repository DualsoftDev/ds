using System.IO;

namespace Dual.Common.Core
{
    public class PathEx
    {
        public static string Normalize(string path)
        {
            var splits = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Join(Path.DirectorySeparatorChar.ToString(), splits);
        }

        public static bool Equals(string pathA, string pathB)
        {
            return pathA.ToLower() == pathB.ToLower();
        }
    }
}
