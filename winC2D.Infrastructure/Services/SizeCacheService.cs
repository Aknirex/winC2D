using System.Text.Json;
using Microsoft.Extensions.Logging;
using winC2D.Core.Services;

namespace winC2D.Infrastructure.Services;

/// <summary>
/// JSON-backed implementation of <see cref="ISizeCacheService"/>.
/// Cache file: %AppData%\winC2D\size_cache.json
/// </summary>
public class SizeCacheService : ISizeCacheService
{
    private readonly ILogger<SizeCacheService> _logger;
    private readonly string _cacheFilePath;

    // key = normalised directory path (lower-invariant)
    private readonly Dictionary<string, SizeCacheEntry> _cache;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false
    };

    public SizeCacheService(ILogger<SizeCacheService> logger)
    {
        _logger = logger;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir     = Path.Combine(appData, "winC2D");
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

        // Validate freshness: compare stored LastWriteTime against current disk value
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
                // Directory was modified – discard stale entry
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
                SizeBytes              = sizeBytes,
                DirectoryLastWriteUtc  = info.LastWriteTimeUtc
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
            var json = JsonSerializer.Serialize(_cache, _jsonOptions);
            File.WriteAllText(_cacheFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save size cache to {Path}", _cacheFilePath);
        }
    }

    // ── private ──────────────────────────────────────────────────────────

    private Dictionary<string, SizeCacheEntry> Load()
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
                return new Dictionary<string, SizeCacheEntry>(StringComparer.OrdinalIgnoreCase);

            var json = File.ReadAllText(_cacheFilePath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, SizeCacheEntry>>(json);
            return dict ?? new Dictionary<string, SizeCacheEntry>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load size cache. Starting with empty cache.");
            return new Dictionary<string, SizeCacheEntry>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string Normalise(string path) =>
        path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .ToLowerInvariant();
}
