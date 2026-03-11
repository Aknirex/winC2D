namespace winC2D.Core.Services;

/// <summary>
/// Cache entry for a scanned directory's size.
/// </summary>
public sealed class SizeCacheEntry
{
    /// <summary>Directory size in bytes. Never -1 (placeholder is no longer stored).</summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// UTC timestamp of the directory's LastWriteTime when the size was measured.
    /// Used as a quick-staleness indicator (note: only reflects direct-child changes).
    /// </summary>
    public DateTime DirectoryLastWriteUtc { get; set; }
}

/// <summary>
/// Persists and retrieves directory size measurements between sessions.
/// Cache file: %LocalAppData%\winC2D\size_cache.json
/// </summary>
public interface ISizeCacheService
{
    /// <summary>
    /// Try to get a cached size for <paramref name="path"/>.
    /// Returns <see langword="false"/> when the entry is missing or the directory
    /// has been modified since it was cached.
    /// </summary>
    bool TryGet(string path, out SizeCacheEntry entry);

    /// <summary>Store (or overwrite) the measured size for <paramref name="path"/>.</summary>
    void Set(string path, long sizeBytes);

    /// <summary>Persist the in-memory cache to disk synchronously.</summary>
    void Save();

    /// <summary>Persist the in-memory cache to disk asynchronously.</summary>
    Task SaveAsync(CancellationToken cancellationToken = default);
}
