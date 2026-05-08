using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using winC2D.Core.FileSystem;
using winC2D.Core.Models;
using winC2D.Core.Services;

namespace winC2D.Infrastructure.Services;

/// <summary>
/// Implementation of <see cref="IFileSystemBrowser"/>.
/// Handles directory enumeration, size scanning (with cache), drive listing,
/// and quick-access persistence.
/// </summary>
public sealed class FileSystemBrowser : IFileSystemBrowser
{
    private readonly IFileSystem _fileSystem;
    private readonly ISizeCacheService _sizeCache;
    private readonly ILogger<FileSystemBrowser> _logger;
    private readonly string _quickAccessPath;

    private List<QuickAccessItem>? _cachedQuickAccess;

    private const int ScanConcurrency = 4;

    // System directories to exclude from enumeration for safety
    private static readonly HashSet<string> _excludedTopLevelDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        "System Volume Information",
        "$Recycle.Bin",
        "$WinREAgent",
        "Recovery",
        "Config.Msi",
        "MSOCache",
    };

    public FileSystemBrowser(
        IFileSystem fileSystem,
        ISizeCacheService sizeCache,
        ILogger<FileSystemBrowser> logger)
    {
        _fileSystem = fileSystem;
        _sizeCache = sizeCache;
        _logger = logger;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var storageDir = Path.Combine(appData, "winC2D");
        Directory.CreateDirectory(storageDir);
        _quickAccessPath = Path.Combine(storageDir, "quick_access.json");
    }

    // ── Directory enumeration ────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FileSystemItem>> GetDirectoryContentsAsync(
        string path,
        CancellationToken ct = default)
    {
        var items = new List<FileSystemItem>();

        await Task.Run(() =>
        {
            try
            {
                if (!_fileSystem.DirectoryExists(path))
                {
                    _logger.LogWarning("Directory not found: {Path}", path);
                    return;
                }

                // Enumerate sub-directories (first-level only)
                foreach (var dirPath in _fileSystem.GetDirectories(path, "*", false))
                {
                    ct.ThrowIfCancellationRequested();

                    var dirName = _fileSystem.GetFileName(dirPath) ?? dirPath;
                    if (_excludedTopLevelDirs.Contains(dirName))
                        continue;

                    var isSymlink = _fileSystem.IsSymlink(dirPath);
                    var status = isSymlink
                        ? FileSystemItemStatus.Migrated
                        : FileSystemItemStatus.Normal;

                    items.Add(new FileSystemItem
                    {
                        Name = dirName,
                        FullPath = dirPath,
                        IsDirectory = true,
                        IsSymlink = isSymlink,
                        Status = status,
                        SizeChecked = false,
                        TypeText = "文件夹",
                    });
                }

                // Enumerate files (first-level only)
                foreach (var filePath in _fileSystem.GetFiles(path, "*", false))
                {
                    ct.ThrowIfCancellationRequested();

                    var fileName = _fileSystem.GetFileName(filePath) ?? filePath;
                    long fileSize = 0;
                    bool sizeOk = false;

                    try
                    {
                        fileSize = _fileSystem.GetFileSize(filePath);
                        sizeOk = true;
                    }
                    catch { /* best effort */ }

                    var isSymlink = _fileSystem.IsSymlink(filePath);
                    var status = isSymlink
                        ? FileSystemItemStatus.Migrated
                        : FileSystemItemStatus.Normal;

                    items.Add(new FileSystemItem
                    {
                        Name = fileName,
                        FullPath = filePath,
                        IsDirectory = false,
                        IsSymlink = isSymlink,
                        Status = status,
                        SizeBytes = fileSize,
                        SizeChecked = sizeOk,
                        TypeText = GetFileTypeText(fileName),
                    });
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error enumerating directory: {Path}", path);
            }
        }, ct);

        // Sort: directories first, then by name (case-insensitive)
        items.Sort((a, b) =>
        {
            if (a.IsDirectory != b.IsDirectory)
                return a.IsDirectory ? -1 : 1;
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });

        return items;
    }

    // ── Size scanning ────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async IAsyncEnumerable<FileSystemItem> ScanSizesAsync(
        IReadOnlyList<FileSystemItem> items,
        IProgress<ScanProgressReport>? progress = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Only scan directories — files already have their size.
        var dirsToScan = items
            .Where(i => i.IsDirectory && !i.IsSymlink)
            .ToList();

        int total = dirsToScan.Count;
        int done = 0;

        if (total == 0)
        {
            // yield file items unmodified so the caller sees them
            foreach (var item in items)
                yield return item;
            yield break;
        }

        var channel = System.Threading.Channels.Channel.CreateUnbounded<FileSystemItem>(new()
        {
            SingleReader = true,
        });

        var semaphore = new SemaphoreSlim(ScanConcurrency, ScanConcurrency);

        var producer = Task.Run(async () =>
        {
            var tasks = dirsToScan.Select(async dirItem =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    ct.ThrowIfCancellationRequested();

                    // Check size cache first
                    if (_sizeCache.TryGet(dirItem.FullPath, out var cached))
                    {
                        dirItem.SizeBytes = cached.SizeBytes;
                        dirItem.SizeChecked = true;
                        dirItem.Status = cached.SizeBytes > 0
                            ? FileSystemItemStatus.Normal
                            : FileSystemItemStatus.Empty;
                    }
                    else
                    {
                        long size = 0;
                        try
                        {
                            size = _fileSystem.GetDirectorySize(dirItem.FullPath, ct);
                            _sizeCache.Set(dirItem.FullPath, size);
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (UnauthorizedAccessException)
                        {
                            dirItem.Status = FileSystemItemStatus.AccessDenied;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error scanning {Path}", dirItem.FullPath);
                            dirItem.Status = FileSystemItemStatus.Error;
                        }

                        dirItem.SizeBytes = size;
                        dirItem.SizeChecked = true;
                        if (dirItem.Status == FileSystemItemStatus.Normal)
                            dirItem.Status = size > 0
                                ? FileSystemItemStatus.Normal
                                : FileSystemItemStatus.Empty;
                    }

                    await channel.Writer.WriteAsync(dirItem, ct);

                    int current = Interlocked.Increment(ref done);
                    progress?.Report(new ScanProgressReport(
                        CurrentDirectory: dirItem.Name,
                        ItemsFound: current,
                        TotalDirectories: total,
                        ProgressPercent: (int)((double)current / total * 100)));
                }
                catch (OperationCanceledException) { /* swallow */ }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Skipped directory during scan: {Path}", dirItem.FullPath);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            channel.Writer.Complete();
        }, ct);

        // Yield scanned directory items as they complete
        var scannedDict = new Dictionary<string, FileSystemItem>(StringComparer.OrdinalIgnoreCase);
        await foreach (var scanned in channel.Reader.ReadAllAsync(ct))
            scannedDict[scanned.FullPath] = scanned;

        await producer;

        // Yield back ALL items in the original order, but with updated scan results
        foreach (var item in items)
        {
            if (!item.IsDirectory || item.IsSymlink)
            {
                // File: already has size from enumeration
                yield return item;
            }
            else if (scannedDict.TryGetValue(item.FullPath, out var updated))
            {
                yield return updated;
            }
            else
            {
                // Directory was not scanned (e.g. excluded by filter)
                yield return item;
            }
        }
    }

    /// <inheritdoc/>
    public async Task<FileSystemItem> RecalculateSizeAsync(
        FileSystemItem item,
        CancellationToken ct = default)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));

        if (!item.IsDirectory || item.IsSymlink)
        {
            item.SizeChecked = true;
            return item;
        }

        if (!_fileSystem.DirectoryExists(item.FullPath))
        {
            item.Status = FileSystemItemStatus.Residual;
            item.SizeBytes = 0;
            item.SizeChecked = true;
            return item;
        }

        try
        {
            long size = await Task.Run(
                () => _fileSystem.GetDirectorySize(item.FullPath, ct), ct);

            _sizeCache.Set(item.FullPath, size);

            item.SizeBytes = size;
            item.SizeChecked = true;
            item.Status = size > 0
                ? FileSystemItemStatus.Normal
                : FileSystemItemStatus.Empty;
        }
        catch (UnauthorizedAccessException)
        {
            item.Status = FileSystemItemStatus.AccessDenied;
            item.SizeChecked = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RecalculateSizeAsync failed for {Path}", item.FullPath);
            item.Status = FileSystemItemStatus.Error;
            item.SizeChecked = true;
        }

        return item;
    }

    // ── Drives ────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<IReadOnlyList<DriveItem>> GetDrivesAsync()
    {
        return Task.Run(() =>
        {
            var drives = new List<DriveItem>();
            try
            {
                foreach (var di in System.IO.DriveInfo.GetDrives())
                {
                    if (di.DriveType is not (System.IO.DriveType.Fixed or System.IO.DriveType.Network))
                        continue;

                    var item = new DriveItem
                    {
                        Name = di.Name.TrimEnd('\\'),
                        RootPath = di.Name,
                        TotalSize = 0,
                        FreeSpace = 0,
                        DriveType = di.DriveType.ToString(),
                        IsReady = false,
                    };

                    if (di.IsReady)
                    {
                        item.IsReady = true;
                        try { item.TotalSize = di.TotalSize; } catch { }
                        try { item.FreeSpace = di.AvailableFreeSpace; } catch { }

                        // Volume label
                        try { item.Label = string.IsNullOrWhiteSpace(di.VolumeLabel) ? "本地磁盘" : di.VolumeLabel; }
                        catch { item.Label = "本地磁盘"; }
                    }
                    else
                    {
                        item.Label = "未就绪";
                    }

                    drives.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error enumerating drives");
            }

            return (IReadOnlyList<DriveItem>)drives;
        });
    }

    // ── Quick access ─────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<IReadOnlyList<QuickAccessItem>> GetQuickAccessItemsAsync()
    {
        if (_cachedQuickAccess is not null)
            return _cachedQuickAccess;

        _cachedQuickAccess = await LoadQuickAccessAsync();
        return _cachedQuickAccess;
    }

    /// <inheritdoc/>
    public async Task AddQuickAccessItemAsync(QuickAccessItem item)
    {
        var items = (await GetQuickAccessItemsAsync()).ToList();

        // Remove existing entry with the same path (will be re-added at top)
        items.RemoveAll(i => string.Equals(i.Path, item.Path, StringComparison.OrdinalIgnoreCase));

        // Re-assign sort orders
        for (int idx = 0; idx < items.Count; idx++)
            items[idx].SortOrder = idx + 1;

        item.SortOrder = 0;
        item.IsPinned = true;
        items.Insert(0, item);

        _cachedQuickAccess = items;
        await SaveQuickAccessAsync(items);
    }

    /// <inheritdoc/>
    public async Task RemoveQuickAccessItemAsync(string path)
    {
        var items = (await GetQuickAccessItemsAsync()).ToList();
        items.RemoveAll(i => string.Equals(i.Path, path, StringComparison.OrdinalIgnoreCase));

        for (int idx = 0; idx < items.Count; idx++)
            items[idx].SortOrder = idx;

        _cachedQuickAccess = items;
        await SaveQuickAccessAsync(items);
    }

    // ── Path helpers ─────────────────────────────────────────────────────

    /// <inheritdoc/>
    public bool DirectoryExists(string path)
        => _fileSystem.DirectoryExists(path);

    /// <inheritdoc/>
    public string? GetParentPath(string path)
    {
        var parent = Path.GetDirectoryName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrEmpty(parent))
            return null;

        // If we're at a drive root (e.g. "C:"), the parent is null
        if (parent.Length < 3 && parent.EndsWith(':'))
            return null;

        return parent;
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private static string GetFileTypeText(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".exe" => "应用程序",
            ".dll" => "动态链接库",
            ".sys" => "系统文件",
            ".msi" => "安装包",
            ".bat" or ".cmd" => "批处理",
            ".ps1" => "PowerShell 脚本",
            ".txt" or ".log" or ".md" => "文本文档",
            ".pdf" => "PDF 文档",
            ".doc" or ".docx" => "Word 文档",
            ".xls" or ".xlsx" => "Excel 表格",
            ".ppt" or ".pptx" => "PowerPoint 演示",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "图片",
            ".mp3" or ".wav" or ".flac" or ".aac" => "音频",
            ".mp4" or ".avi" or ".mkv" or ".mov" => "视频",
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "压缩包",
            ".json" or ".xml" or ".yaml" or ".yml" => "配置文件",
            ".cs" or ".cpp" or ".h" or ".js" or ".ts" or ".py" => "源代码",
            _ => "文件",
        };
    }

    private async Task<List<QuickAccessItem>> LoadQuickAccessAsync()
    {
        try
        {
            if (File.Exists(_quickAccessPath))
            {
                var json = await File.ReadAllTextAsync(_quickAccessPath);
                var loaded = JsonSerializer.Deserialize<List<QuickAccessItem>>(json);
                if (loaded is { Count: > 0 })
                    return loaded;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load quick access from {Path}", _quickAccessPath);
        }

        // Return default presets
        return CreateDefaultQuickAccess();
    }

    private async Task SaveQuickAccessAsync(List<QuickAccessItem> items)
    {
        try
        {
            var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_quickAccessPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save quick access to {Path}", _quickAccessPath);
        }
    }

    private static List<QuickAccessItem> CreateDefaultQuickAccess()
    {
        var items = new List<QuickAccessItem>();
        int order = 0;

        void Add(string name, string path)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                items.Add(new QuickAccessItem
                {
                    DisplayName = name,
                    Path = path,
                    IconGlyph = "Folder24",
                    IsPinned = true,
                    SortOrder = order++,
                });
        }

        Add("Program Files", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));

        if (Environment.Is64BitOperatingSystem)
        {
            var pfx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!string.Equals(pfx86,
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    StringComparison.OrdinalIgnoreCase))
                Add("Program Files (x86)", pfx86);
        }

        Add("AppData (Roaming)", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        Add("AppData (Local)", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

        var localLow = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow");
        Add("AppData (LocalLow)", localLow);

        Add("文档", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        Add("下载", Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
        Add("桌面", Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

        return items;
    }
}
