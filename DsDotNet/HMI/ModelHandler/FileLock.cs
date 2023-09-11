namespace ModelHandler;

public class FileLock : IDisposable
{
    FileStream? _lock;
    public FileLock(string path)
    {
        if (File.Exists(path))
            _lock = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
    }

    public bool IsLocked => _lock != null;

    public void Dispose()
    {
        _lock?.Dispose();
        _lock = null;
    }
}
