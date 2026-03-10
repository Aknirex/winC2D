namespace winC2D.Core.Services;

/// <summary>
/// Cache entry for a scanned directory's size.
/// </summary>
public sealed class SizeCacheEntry
{
    /// <summary>Directory size in bytes. -1 means "exceeds threshold".</summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// UTC timestamp of the directory's LastWriteTime when the size was measured.
    /// Used to detect stale cache entries.
    /// </summary>
    public DateTime DirectoryLastWriteUtc { get; set; }
}

/// <summary>
/// Persists and retrieves directory size measurements between sessions.
/// Cache file: %AppData%\winC2D\size_cache.json
/// </summary>
public interface ISizeCacheService
{
    /// <summary>Try to get a cached size for <paramref name="path"/>.</summary>
    /// <returns><see langword="true"/> and a valid entry when the cache is fresh; otherwise <see langword="false"/>.</returns>
    bool TryGet(string path, out SizeCacheEntry entry);

    /// <summary>Store (or overwrite) the size for <paramref name="path"/>.</summary>
    void Set(string path, long sizeBytes);

    /// <summary>Persist the in-memory cache to disk.</summary>
    void Save();
}
