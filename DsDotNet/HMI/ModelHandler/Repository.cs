using Ionic.Zip;

using System.Reactive.Disposables;
using System.Text;

namespace ModelHandler;

public class Repository : IDisposable
{
    public ZipFile? ZipFile { get; private set; }
    FileLock? _zipLock;
    string zipDir;
    string zipPath;
    public IDisposable CreateZipFileUnlockRegion()
    {
        if (_zipLock != null)
        {
            _zipLock.Dispose();
            return Disposable.Create(() => _zipLock = new FileLock(ZipFile!.Name));
        }

        return Disposable.Create(() => { });
    }
    public TempFolder TempFolder { get; } = new TempFolder();

    public Repository(string _zipDir)
    {
        zipDir = _zipDir;
        zipPath = zipDir + ".zip";
        if (File.Exists(zipPath))
        {
            ZipFile = ZipFile.Read(zipPath, new ReadOptions() { Encoding = Encoding.UTF8 });
            ZipToTemp();
            // 존재하는 zip 파일을 열 경우, lock 이 유지 되는 것으로 확인됨
        }
        else
        {
            ZipFile = new ZipFile(zipPath)
            {
                AlternateEncoding = Encoding.UTF8,
                AlternateEncodingUsage = ZipOption.Always,
            };
            _zipLock = new FileLock(zipPath);
        }
    }

    public void CompressDirectory()
    {
        ZipFile!.AddDirectory(zipDir);
        ZipFile.Save(zipPath);
    }

    public void Dispose()
    {
        ZipFile?.Dispose();
        ZipFile = null;
        _zipLock?.Dispose();
        _zipLock = null;
        TempFolder.Dispose();
    }
    
    public void SaveZipFile()
    {
        _zipLock?.Dispose();
        ZipFile!.Save();
        _zipLock = new FileLock(ZipFile.Name);
    }
    
    public void ZipToTemp()
    {
        ZipFile!.ExtractAll(TempFolder.Dir, ExtractExistingFileAction.OverwriteSilently);
    }
    
    public ZipEntry? AddToZip(string sourcePath)
    {
        var fileName = Path.GetFileName(sourcePath);
        var existing = ZipFile!.Entries.FirstOrDefault(e => e.FileName == fileName);
        if (existing == null)
            return ZipFile.AddEntry(fileName, File.ReadAllBytes(sourcePath));
        else
            ZipFile.UpdateEntry(fileName, File.ReadAllBytes(sourcePath));
        
        return null;
    }
    public void AddToZipAndTempFolder(string sourcePath)
    {
        var name = Path.GetFileName(sourcePath);
        var target = Path.Combine(TempFolder.Dir, name);
        var diff = !FileEx.FileContentsAreEqual(sourcePath, target);
        if (diff)
            File.Copy(sourcePath, target, true);
        AddToZip(sourcePath);
    }
    
    public void DeleteFromZipAndTempFolder(string name)
    {
        ZipFile!.DeleteEntry(name);
        File.Delete(Path.Combine(TempFolder.Dir, name));
    }

    public static string? ValidateZip(string zipPath)
    {
        var zip = ZipFile.Read(zipPath, new ReadOptions() { Encoding = Encoding.UTF8 });
        var files = zip.Entries.Select(e => e.FileName).ToArray();
        if (!files.Any(n => n == "model.zip"))
            return "No project file";

        var stepFiles = files.Where(n => Path.GetExtension(n).ToLower().IsOneOf(".stp", ".step"));
        foreach (var stp in stepFiles)
        {
            var dsg = $"{Path.GetFileNameWithoutExtension(stp)}.dsg";
            var dsgEntry = files.FirstOrDefault(n => n == dsg);
            if (dsgEntry == null)
                return $"No compiled graphic file [{dsg}]";
        }
        
        return null;
    }

    public static bool IsValidZip(string zipPath) => ValidateZip(zipPath)!.IsNullOrEmpty();
    public string ValidateZip() => ValidateZip(ZipFile!.Name)!;
    public bool IsValidZip() => ValidateZip(ZipFile!.Name)!.IsNullOrEmpty();
}

public static class EmIonicZip
{
    public static bool ExistsEntry(this ZipFile zip, string name)
        => zip.Entries.Any(e => e.FileName == name);
    public static bool DeleteEntry(this ZipFile zip, string name)
    {
        if (zip.ExistsEntry(name))
        {
            zip.RemoveEntries(new[] { name });
            return true;
        }
        return false;
    }
}
