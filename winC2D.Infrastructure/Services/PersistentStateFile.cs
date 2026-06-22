using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace winC2D.Infrastructure.Services;

internal static class PersistentStateFile
{
    public static string CreateStorageDirectory(ILogger logger, string leaf)
    {
        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (string.IsNullOrWhiteSpace(localAppData))
            localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var preferred = Path.Combine(localAppData, "winC2D", leaf);
        try
        {
            Directory.CreateDirectory(preferred);
            return preferred;
        }
        catch (Exception ex)
        {
            var fallback = Path.Combine(Path.GetTempPath(), "winC2D", leaf);
            logger.LogWarning(ex,
                "Cannot use persistent state directory {Preferred}; falling back to {Fallback}",
                preferred, fallback);
            Directory.CreateDirectory(fallback);
            return fallback;
        }
    }

    public static IDisposable AcquireProcessLock(
        string stateFilePath,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(stateFilePath).ToUpperInvariant();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fullPath)));
        var mutex = new Mutex(false, $"Local\\winC2D-state-{hash}");

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (mutex.WaitOne(TimeSpan.FromMilliseconds(100)))
                        return new MutexLease(mutex);
                }
                catch (AbandonedMutexException)
                {
                    return new MutexLease(mutex);
                }
            }
        }
        catch
        {
            mutex.Dispose();
            throw;
        }
    }

    public static void AtomicWriteAllText(string path, string content)
    {
        var tempPath = $"{path}.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(tempPath, content);
            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
                // A unique stale temp file cannot block a later save.
            }
        }
    }

    private sealed class MutexLease(Mutex mutex) : IDisposable
    {
        private Mutex? _mutex = mutex;

        public void Dispose()
        {
            var value = Interlocked.Exchange(ref _mutex, null);
            if (value is null)
                return;
            value.ReleaseMutex();
            value.Dispose();
        }
    }
}
