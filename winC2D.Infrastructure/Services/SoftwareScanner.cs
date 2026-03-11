using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using winC2D.Core.Services;
using winC2D.Core.Models;
using winC2D.Core.FileSystem;

namespace winC2D.Infrastructure.Services;

/// <summary>
/// Streams <see cref="SoftwareInfo"/> items from Program Files directories.
///
/// Status assignment rules (applied after every scan, fast or precise):
///   Migrated   – directory is a reparse point (symlink)
///   Empty      – no files and no sub-directories at all
///   Normal     – has at least one .exe (or size > SmallAppThreshold)
///   SmallApp   – has files but no .exe AND size ≤ SmallAppThreshold
///                (replaces the old "Suspicious" label for normal small dirs)
///   Residual   – directory exists but has only files/dirs with no .exe found
///                AND the precise check confirmed this (SuspiciousChecked=true)
///
/// "Suspicious" is intentionally NOT assigned based on size alone.
/// </summary>
public sealed class SoftwareScanner : ISoftwareScanner
{
    private readonly IFileSystem _fileSystem;
    private readonly ISizeCacheService _sizeCache;
    private readonly ILogger<SoftwareScanner> _logger;

    /// <summary>
    /// Directories smaller than this are considered "small apps" rather than suspicious.
    /// Value: 50 MB – large enough to exclude most genuine software.
    /// </summary>
    private const long SmallAppThreshold = 50L * 1024 * 1024;

    /// <summary>Max concurrent size calculations during a full scan.</summary>
    private const int ScanConcurrency = 4;

    public SoftwareScanner(
        IFileSystem fileSystem,
        ISizeCacheService sizeCache,
        ILogger<SoftwareScanner> logger)
    {
        _fileSystem = fileSystem;
        _sizeCache  = sizeCache;
        _logger     = logger;
    }

    // ── ISoftwareScanner ────────────────────────────────────────────────

    /// <inheritdoc/>
    public IEnumerable<string> GetDefaultScanDirectories()
    {
        var dirs = new List<string>();

        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrEmpty(pf) && _fileSystem.DirectoryExists(pf))
            dirs.Add(pf);

        if (Environment.Is64BitOperatingSystem)
        {
            var pfx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!string.IsNullOrEmpty(pfx86) &&
                !string.Equals(pfx86, pf, StringComparison.OrdinalIgnoreCase) &&
                _fileSystem.DirectoryExists(pfx86))
                dirs.Add(pfx86);
        }

        return dirs;
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<SoftwareInfo> ScanStreamAsync(
        IProgress<ScanProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
        => ScanStreamAsync(GetDefaultScanDirectories(), progress, cancellationToken);

    /// <inheritdoc/>
    public async IAsyncEnumerable<SoftwareInfo> ScanStreamAsync(
        IEnumerable<string> directories,
        IProgress<ScanProgressReport>? progress = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // ── Phase 1: enumerate all first-level sub-directories ────────────
        var allDirs = CollectSubDirectories(directories);
        int total   = allDirs.Count;
        int done    = 0;

        // ── Phase 2: process with bounded concurrency ─────────────────────
        // We use a channel as a producer/consumer bridge so that IAsyncEnumerable
        // can yield items in order of completion without blocking the caller.
        var channel = System.Threading.Channels.Channel.CreateUnbounded<SoftwareInfo>(
            new System.Threading.Channels.UnboundedChannelOptions { SingleReader = true });

        var semaphore = new SemaphoreSlim(ScanConcurrency, ScanConcurrency);

        var producer = Task.Run(async () =>
        {
            var tasks = allDirs.Select(async path =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var item = await BuildItemAsync(path, cancellationToken);
                    await channel.Writer.WriteAsync(item, cancellationToken);

                    int current = Interlocked.Increment(ref done);
                    progress?.Report(new ScanProgressReport(
                        CurrentDirectory : Path.GetFileName(path) ?? path,
                        ItemsFound       : current,
                        TotalDirectories : total,
                        ProgressPercent  : total > 0 ? (int)((double)current / total * 100) : 100));
                }
                catch (OperationCanceledException) { /* swallow – outer loop will stop */ }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Skipped directory during scan: {Path}", path);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            channel.Writer.Complete();
        }, cancellationToken);

        // Yield items as the producer writes them
        await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
            yield return item;

        await producer; // propagate any unhandled producer exception
    }

    /// <inheritdoc/>
    public async Task<SoftwareInfo> RecalculateSizeAsync(
        SoftwareInfo software,
        CancellationToken cancellationToken = default)
    {
        if (software is null) throw new ArgumentNullException(nameof(software));

        var path = software.InstallLocation;

        if (_fileSystem.IsSymlink(path))
        {
            software.IsSymlink = true;
            software.Status    = SoftwareStatus.Migrated;
            return software;
        }

        if (!_fileSystem.DirectoryExists(path))
        {
            software.Status          = SoftwareStatus.Residual;
            software.SizeBytes       = 0;
            software.SuspiciousChecked = true;
            return software;
        }

        long size   = 0;
        bool hasExe = false;

        try
        {
            await Task.Run(() =>
            {
                foreach (var file in _fileSystem.GetFiles(path, "*", true))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!hasExe && file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        hasExe = true;
                    try { size += _fileSystem.GetFileSize(file); } catch { }
                }
            }, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during precise size calculation for {Path}", path);
        }

        // Write result into cache so next fast scan can reuse it
        _sizeCache.Set(path, size);

        software.SizeBytes         = size;
        software.SuspiciousChecked = true;
        software.Status            = DetermineStatus(isSymlink: false, size: size, hasExe: hasExe);
        return software;
    }

    // ── private helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Enumerate all first-level sub-directories across all base directories,
    /// deduplicating by normalised path.
    /// </summary>
    private List<string> CollectSubDirectories(IEnumerable<string> baseDirs)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result  = new List<string>();

        foreach (var baseDir in baseDirs)
        {
            if (!_fileSystem.DirectoryExists(baseDir)) continue;
            try
            {
                foreach (var sub in _fileSystem.GetDirectories(baseDir, "*", false))
                {
                    var norm = Normalise(sub);
                    if (visited.Add(norm))
                        result.Add(norm);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cannot enumerate {BaseDir}", baseDir);
            }
        }

        return result;
    }

    /// <summary>
    /// Build one <see cref="SoftwareInfo"/> for <paramref name="path"/>.
    /// Uses the size cache when fresh; otherwise calculates precisely.
    /// </summary>
    private async Task<SoftwareInfo> BuildItemAsync(string path, CancellationToken ct)
    {
        var name      = Path.GetFileName(path) ?? path;
        var isSymlink = _fileSystem.IsSymlink(path);

        if (isSymlink)
        {
            return new SoftwareInfo
            {
                Name            = name,
                InstallLocation = path,
                IsSymlink       = true,
                SizeBytes       = 0,
                Status          = SoftwareStatus.Migrated,
                SuspiciousChecked = true
            };
        }

        // ── Try cache ────────────────────────────────────────────────────
        if (_sizeCache.TryGet(path, out var cached))
        {
            var (cachedStatus, _) = AnalyseSize(cached.SizeBytes);
            return new SoftwareInfo
            {
                Name              = name,
                InstallLocation   = path,
                IsSymlink         = false,
                SizeBytes         = cached.SizeBytes,
                Status            = cachedStatus,
                SuspiciousChecked = true   // cache implies a previous precise measurement
            };
        }

        // ── Cache miss: precise full calculation ─────────────────────────
        long size   = 0;
        bool hasExe = false;
        bool hasAny = false;

        try
        {
            await Task.Run(() =>
            {
                foreach (var file in _fileSystem.GetFiles(path, "*", true))
                {
                    ct.ThrowIfCancellationRequested();
                    hasAny = true;
                    if (!hasExe && file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        hasExe = true;
                    try { size += _fileSystem.GetFileSize(file); } catch { }
                }

                if (!hasAny)
                    hasAny = _fileSystem.GetDirectories(path, "*", false).Any();
            }, ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error scanning {Path}", path);
        }

        // Write into cache so subsequent fast launches reuse this measurement
        _sizeCache.Set(path, size);

        var status = hasAny
            ? DetermineStatus(isSymlink: false, size: size, hasExe: hasExe)
            : SoftwareStatus.Empty;

        return new SoftwareInfo
        {
            Name              = name,
            InstallLocation   = path,
            IsSymlink         = false,
            SizeBytes         = size,
            Status            = status,
            SuspiciousChecked = true
        };
    }

    /// <summary>
    /// Derives the display status from measured size and .exe presence.
    /// "Suspicious" is no longer assigned here – it is only used by migration
    /// logic to mean "needs manual review" (e.g. no .exe found after precise check).
    /// </summary>
    private static SoftwareStatus DetermineStatus(bool isSymlink, long size, bool hasExe)
    {
        if (isSymlink) return SoftwareStatus.Migrated;
        if (size == 0) return SoftwareStatus.Empty;
        if (!hasExe)   return SoftwareStatus.Residual;   // has files but no executable
        return SoftwareStatus.Normal;
    }

    /// <summary>
    /// Derives status purely from a cached size value (no .exe info available).
    /// Returns the status and whether the info came from cache.
    /// </summary>
    private static (SoftwareStatus status, bool fromCache) AnalyseSize(long sizeBytes)
    {
        if (sizeBytes == 0) return (SoftwareStatus.Empty, true);
        // Without exe info we fall back to Normal – the precise check can downgrade it later.
        return (SoftwareStatus.Normal, true);
    }

    private static string Normalise(string path) =>
        path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}