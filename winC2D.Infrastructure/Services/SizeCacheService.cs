using System.Text.Json;
using Microsoft.Extensions.Logging;
using winC2D.Core.Services;

namespace winC2D.Infrastructure.Services;

/// <summary>
/// JSON-backed implementation of <see cref="ISizeCacheService"/>.
/// Cache file: %LocalAppData%\winC2D\size_cache.json
/// </summary>
public sealed class SizeCacheService : ISizeCacheService
{
    private readonly ILogger<SizeCacheService> _logger;
    private readonly string _cacheFilePath;

    // key = normalised directory path (lower-invariant, no trailing slash)
    private readonly Dictionary<string, SizeCacheEntry> _cache;

    // Surrogate used only for JSON serialisation so we can round-trip the
    // OrdinalIgnoreCase comparer that the runtime dictionary needs.
    private sealed class CacheSurrogate
    {
        public Dictionary<string, SizeCacheEntry> Entries { get; set; } = new();
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false
    };

    public SizeCacheService(ILogger<SizeCacheService> logger)
    {
        _logger = logger;

        // Use LocalAppData (non-roaming) because sizes are machine-specific.
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(localAppData, "winC2D");
        Directory.CreateDirectory(dir);
        _cacheFilePath = Path.Combine(dir, "size_cache.json");

        _cache = Load();
    }

    /// <inheritdoc/>
    public bool TryGet(string path, out SizeCacheEntry entry)
    {
        var key = Normalise(path);
        if (!_cache.TryGetValue(key, out var cached))
        {
            entry = default!;
            return false;
        }

        // Validate freshness: compare stored LastWriteTime against current disk value.
        // Note: DirectoryInfo.LastWriteTime only reflects direct-child changes, so this
        // is a best-effort check rather than a guarantee.
        try
        {
            var info = new DirectoryInfo(path);
            if (!info.Exists)
            {
                _cache.Remove(key);
                entry = default!;
                return false;
            }

            if (info.LastWriteTimeUtc != cached.DirectoryLastWriteUtc)
            {
                _cache.Remove(key);
                entry = default!;
                return false;
            }
        }
        catch
        {
            entry = default!;
            return false;
        }

        entry = cached;
        return true;
    }

    /// <inheritdoc/>
    public void Set(string path, long sizeBytes)
    {
        try
        {
            var info = new DirectoryInfo(path);
            if (!info.Exists) return;

            _cache[Normalise(path)] = new SizeCacheEntry
            {
                SizeBytes             = sizeBytes,
                DirectoryLastWriteUtc = info.LastWriteTimeUtc
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create cache entry for {Path}", path);
        }
    }

    /// <inheritdoc/>
    public void Save()
    {
        try
        {
            // Wrap in a surrogate so the JSON is a plain object with an "Entries" key,
            // making it easy to extend the schema in the future.
            var surrogate = new CacheSurrogate { Entries = new Dictionary<string, SizeCacheEntry>(_cache) };
            var json = JsonSerializer.Serialize(surrogate, _jsonOptions);
            File.WriteAllText(_cacheFilePath, json);
            _logger.LogDebug("Size cache saved ({Count} entries) to {Path}", _cache.Count, _cacheFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save size cache to {Path}", _cacheFilePath);
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var surrogate = new CacheSurrogate { Entries = new Dictionary<string, SizeCacheEntry>(_cache) };
            var json = JsonSerializer.Serialize(surrogate, _jsonOptions);
            await File.WriteAllTextAsync(_cacheFilePath, json, cancellationToken);
            _logger.LogDebug("Size cache saved async ({Count} entries) to {Path}", _cache.Count, _cacheFilePath);
        }
        catch (OperationCanceledException) { /* shutting down – swallow */ }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save size cache (async) to {Path}", _cacheFilePath);
        }
    }

    // ── private ──────────────────────────────────────────────────────────

    private Dictionary<string, SizeCacheEntry> Load()
    {
        var empty = new Dictionary<string, SizeCacheEntry>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (!File.Exists(_cacheFilePath))
                return empty;

            var json = File.ReadAllText(_cacheFilePath);

            // Try new surrogate format first, fall back to legacy flat format.
            var surrogate = JsonSerializer.Deserialize<CacheSurrogate>(json);
            if (surrogate?.Entries is { Count: > 0 } entries)
            {
                // Re-create with the correct comparer.
                var result = new Dictionary<string, SizeCacheEntry>(
                    entries.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var kv in entries)
                    result[kv.Key] = kv.Value;
                return result;
            }

            // Legacy: top-level was a plain Dictionary<string, SizeCacheEntry>
            var legacy = JsonSerializer.Deserialize<Dictionary<string, SizeCacheEntry>>(json);
            if (legacy is { Count: > 0 })
            {
                var result = new Dictionary<string, SizeCacheEntry>(
                    legacy.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var kv in legacy)
                    result[kv.Key] = kv.Value;
                return result;
            }

            return empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load size cache from {Path}. Starting fresh.", _cacheFilePath);
            return empty;
        }
    }

    private static string Normalise(string path) =>
        path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .ToLowerInvariant();
}
