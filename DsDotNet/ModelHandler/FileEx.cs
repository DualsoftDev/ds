using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelHandler;

public class FileEx
{
    public static bool IsFileLocked(string filePath)
    {
        try
        {
            var fi = new FileInfo(filePath);
            using (var stream = fi.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                stream.Close();
        }
        catch (IOException)
        {
            return true;
        }
        return false;
    }

    public static async Task AwaitUnlock(string filePath)
    {
        while (IsFileLocked(filePath))
            await Task.Delay(100);
    }

    public static bool FileContentsAreEqual(string path1, string path2)
    {
        var f1 = new FileInfo(path1);
        var f2 = new FileInfo(path2);
        if (!f1.Exists || !f2.Exists || f1.Length != f2.Length)
            return false;

        return System.IO.File.ReadAllBytes(path1).SequenceEqual(System.IO.File.ReadAllBytes(path2));
    }

    public static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
