using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UtfUnknown;

namespace Dual.Common.Core;

public static partial class FileEx
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

        return File.ReadAllBytes(path1).SequenceEqual(File.ReadAllBytes(path2));
    }

    public static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}


/* charset 처리 */
public static partial class FileEx
{
    /// <summary>
    /// UTF-8로 인코딩되었는지 여부 반환
    /// </summary>
    public static bool IsUtf8(string path)
    {
        DetectionResult result = CharsetDetector.DetectFromFile(path);
        return result.Detected.EncodingName.Contains("utf-8");
    }

    public static Encoding GetEncoding(string path)
    {
        if (IsUtf8(path))
            return Encoding.UTF8;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding("euc-kr");
    }

    /// <summary>
    /// UTF-8 및 ANSI(euc-kr)로 인코딩된 파일 내용 읽기
    /// </summary>
    public static string ReadAllText(string path)
    {
        if (IsUtf8(path))
            return File.ReadAllText(path);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using (StreamReader reader = new StreamReader(path, Encoding.GetEncoding("euc-kr")))
            return reader.ReadToEnd();
    }

    /// <summary>
    /// UTF-8 및 ANSI(euc-kr)로 인코딩된 파일 내용 읽기
    /// </summary>
    public static string[] ReadAllLines(string path)
    {
        return FileEx.ReadAllText(path).Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    }
}