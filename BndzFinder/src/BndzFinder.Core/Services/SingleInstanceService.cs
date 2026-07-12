namespace BndzFinder.Core.Services;

public interface ISingleInstanceService
{
    bool TryAcquire(string mutexName);
    void Release();
}

public sealed class SingleInstanceService : ISingleInstanceService, IDisposable
{
    private FileStream? _lockStream;

    public bool TryAcquire(string mutexName)
    {
        var lockPath = Path.Combine(Path.GetTempPath(), $"{mutexName}.lock");
        try
        {
            _lockStream = new FileStream(lockPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    public void Release()
    {
        _lockStream?.Dispose();
        _lockStream = null;
    }

    public void Dispose() => Release();
}
