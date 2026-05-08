using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using winC2D.Core.Events;
using winC2D.Core.Models;
using winC2D.Core.Services;
using winC2D.Infrastructure.Localization;

namespace winC2D.App.ViewModels;

/// <summary>
/// View model for the unified file-system explorer.
/// Manages the sidebar (quick-access + drives), breadcrumb navigation,
/// directory listing, size scanning, selection, and migration.
/// </summary>
public partial class FileSystemBrowserViewModel : ObservableObject
{
    private readonly IFileSystemBrowser _browser;
    private readonly IMigrationEngine _migrationEngine;
    private readonly ISizeCacheService _sizeCache;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<FileSystemBrowserViewModel> _logger;
    private readonly MainViewModel _mainViewModel;

    private CancellationTokenSource? _scanCts;
    private bool _isSynchronizingSelection;

    // Batch migration progress tracking
    private long _batchTotalBytes;
    private long _batchCopiedBytes;

    // ═════════════════════════════════════════════════════════════════════
    // Sidebar
    // ═════════════════════════════════════════════════════════════════════

    [ObservableProperty]
    private ObservableCollection<QuickAccessItem> _quickAccessItems = new();

    [ObservableProperty]
    private ObservableCollection<DriveItem> _drives = new();

    // ═════════════════════════════════════════════════════════════════════
    // Breadcrumb
    // ═════════════════════════════════════════════════════════════════════

    [ObservableProperty]
    private ObservableCollection<BreadcrumbItem> _breadcrumbs = new();

    /// <summary>
    /// Current absolute directory path. null = "This PC" view (drives).
    /// </summary>
    [ObservableProperty]
    private string? _currentPath;

    // ═════════════════════════════════════════════════════════════════════
    // Main panel
    // ═════════════════════════════════════════════════════════════════════

    [ObservableProperty]
    private ObservableCollection<FileSystemItem> _items = new();

    [ObservableProperty]
    private ObservableCollection<FileSystemItem> _selectedItems = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    // ═════════════════════════════════════════════════════════════════════
    // Operation state
    // ═════════════════════════════════════════════════════════════════════

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isMigrating;

    public bool IsBusy => IsScanning || IsMigrating;

    partial void OnIsScanningChanged(bool value) => OnPropertyChanged(nameof(IsBusy));
    partial void OnIsMigratingChanged(bool value) => OnPropertyChanged(nameof(IsBusy));

    [ObservableProperty]
    private int _scanProgress;

    [ObservableProperty]
    private int _migrationProgress;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private string _currentScanDirectory = string.Empty;

    // ═════════════════════════════════════════════════════════════════════
    // Selection / target
    // ═════════════════════════════════════════════════════════════════════

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalSelectedSizeText))]
    private long _totalSelectedSize;

    [ObservableProperty]
    private int _totalSelectedCount;

    public string TotalSelectedSizeText => FormatSize(TotalSelectedSize);

    [ObservableProperty]
    private string _targetPath = string.Empty;

    [ObservableProperty]
    private MigrationTask? _currentTask;

    // ═════════════════════════════════════════════════════════════════════
    // Constructor
    // ═════════════════════════════════════════════════════════════════════

    public FileSystemBrowserViewModel(
        IFileSystemBrowser browser,
        IMigrationEngine migrationEngine,
        ISizeCacheService sizeCache,
        ILocalizationService localizationService,
        ILogger<FileSystemBrowserViewModel> logger,
        MainViewModel mainViewModel)
    {
        _browser = browser;
        _migrationEngine = migrationEngine;
        _sizeCache = sizeCache;
        _localizationService = localizationService;
        _logger = logger;
        _mainViewModel = mainViewModel;

        _migrationEngine.ProgressChanged += OnMigrationProgressChanged;
        _migrationEngine.ErrorOccurred += OnMigrationError;
        _migrationEngine.StateChanged += OnMigrationStateChanged;

        _localizationService.LanguageChanged += (_, _) => NotifyLocalizedStrings();

        Items.CollectionChanged += Items_CollectionChanged;

        // Initialise default target path
        InitTargetPath();
    }

    /// <summary>
    /// Called by the View's Loaded event. Initialises the sidebar (quick-access + drives)
    /// and navigates to the "This PC" root view.
    /// </summary>
    public async Task LoadAsync()
    {
        await LoadQuickAccessAsync();
        await LoadDrivesAsync();
        NavigateToThisPC();
    }

    private void InitTargetPath()
    {
        string? systemDrive = Path.GetPathRoot(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows))
            ?.TrimEnd('\\');

        string? firstOther = null;
        foreach (var di in System.IO.DriveInfo.GetDrives())
        {
            if (di.DriveType is System.IO.DriveType.Fixed or System.IO.DriveType.Network)
            {
                var letter = di.Name.TrimEnd('\\');
                if (firstOther is null &&
                    !string.Equals(letter, systemDrive, StringComparison.OrdinalIgnoreCase))
                    firstOther = letter;
            }
        }

        var drive = firstOther ?? systemDrive ?? "C:";
        TargetPath = Path.Combine(drive + "\\", "MigratedData");
    }

    // ═════════════════════════════════════════════════════════════════════
    // Sidebar commands
    // ═════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task NavigateDriveAsync(DriveItem? drive)
    {
        if (drive is null || string.IsNullOrEmpty(drive.RootPath)) return;
        await NavigateToPathAsync(drive.RootPath);
    }

    [RelayCommand]
    private async Task NavigateQuickAccessAsync(QuickAccessItem? qa)
    {
        if (qa is null || string.IsNullOrEmpty(qa.Path)) return;
        await NavigateToPathAsync(qa.Path);
    }

    [RelayCommand]
    private async Task AddToQuickAccessAsync()
    {
        if (string.IsNullOrEmpty(CurrentPath)) return;

        var name = Path.GetFileName(CurrentPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                   ?? CurrentPath;
        await _browser.AddQuickAccessItemAsync(new QuickAccessItem
        {
            DisplayName = name,
            Path = CurrentPath,
            IconGlyph = "Folder24",
            IsPinned = true,
        });
        await LoadQuickAccessAsync();
    }

    [RelayCommand]
    private async Task RemoveFromQuickAccessAsync(QuickAccessItem? item)
    {
        if (item is null) return;
        await _browser.RemoveQuickAccessItemAsync(item.Path);
        await LoadQuickAccessAsync();
    }

    // ═════════════════════════════════════════════════════════════════════
    // Breadcrumb navigation
    // ═════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task NavigateBreadcrumbAsync(BreadcrumbItem? crumb)
    {
        if (crumb is null) return;
        if (crumb.IsRoot)
        {
            NavigateToThisPC();
            return;
        }
        if (!string.IsNullOrEmpty(crumb.FullPath))
            await NavigateToPathAsync(crumb.FullPath);
    }

    [RelayCommand]
    private async Task NavigateUpAsync()
    {
        var parent = _browser.GetParentPath(CurrentPath ?? string.Empty);
        if (parent is null)
        {
            NavigateToThisPC();
        }
        else
        {
            await NavigateToPathAsync(parent);
        }
    }

    /// <summary>
    /// Called from the DataGrid when the user double-clicks a directory row.
    /// </summary>
    [RelayCommand]
    private async Task NavigateItemAsync(FileSystemItem? item)
    {
        if (item is null || !item.IsDirectory) return;
        await NavigateToPathAsync(item.FullPath);
    }

    // ═════════════════════════════════════════════════════════════════════
    // Core navigation
    // ═════════════════════════════════════════════════════════════════════

    private void NavigateToThisPC()
    {
        CurrentPath = null;
        Items.Clear();
        SelectedItems.Clear();
        BuildBreadcrumbs();
        PushStatus("就绪", isBusy: false);
    }

    private async Task NavigateToPathAsync(string path)
    {
        if (string.IsNullOrEmpty(path)) return;

        PushStatus("正在加载…", isBusy: true);

        try
        {
            var contents = await _browser.GetDirectoryContentsAsync(path);
            ReplaceItems(contents);
            CurrentPath = path;
            BuildBreadcrumbs();
            PushStatus($"{contents.Count} 个项目", isBusy: false);
        }
        catch (UnauthorizedAccessException)
        {
            PushStatus("访问被拒绝", isBusy: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Navigation failed: {Path}", path);
            PushStatus($"加载失败: {ex.Message}", isBusy: false);
        }
    }

    private void BuildBreadcrumbs()
    {
        Breadcrumbs.Clear();

        // Root node
        Breadcrumbs.Add(new BreadcrumbItem
        {
            DisplayName = "此电脑",
            FullPath = null,
            Index = 0,
            IsLast = CurrentPath is null,
        });

        if (CurrentPath is null) return;

        // Split path into segments
        var parts = new List<string>();
        var current = CurrentPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        while (!string.IsNullOrEmpty(current))
        {
            var name = Path.GetFileName(current);
            if (string.IsNullOrEmpty(name)) break;

            parts.Insert(0, name);

            var parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent) || parent == current) break;
            if (parent.Length == 2 && parent[1] == ':')
            {
                // Drive root
                parts.Insert(0, parent);
                break;
            }
            current = parent;
        }

        string accumulated = string.Empty;
        for (int i = 0; i < parts.Count; i++)
        {
            if (i == 0 && parts[i].Length == 2 && parts[i][1] == ':')
                accumulated = parts[i] + "\\";
            else
                accumulated = Path.Combine(accumulated, parts[i]);

            Breadcrumbs.Add(new BreadcrumbItem
            {
                DisplayName = parts[i],
                FullPath = accumulated,
                Index = i + 1,
                IsLast = i == parts.Count - 1,
            });
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    // Scan
    // ═════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task ScanSizesAsync()
    {
        if (IsScanning || Items.Count == 0) return;

        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var ct = _scanCts.Token;

        IsScanning = true;
        ScanProgress = 0;

        // Take a snapshot of current items
        var snapshot = Items.ToList();

        try
        {
            var dispatcher = System.Windows.Application.Current.Dispatcher;
            var progress = new Progress<ScanProgressReport>(report =>
            {
                dispatcher.InvokeAsync(() =>
                {
                    CurrentScanDirectory = report.CurrentDirectory;
                    ScanProgress = report.ProgressPercent;
                    PushStatus($"正在计算: {report.ItemsFound}/{report.TotalDirectories} - {report.CurrentDirectory}", isBusy: true);
                });
            });

            var updated = new Dictionary<string, FileSystemItem>(StringComparer.OrdinalIgnoreCase);
            await foreach (var item in _browser.ScanSizesAsync(snapshot, progress, ct))
            {
                updated[item.FullPath] = item;
            }

            // Persist cache
            await _sizeCache.SaveAsync(ct);

            // Merge results back into the live collection
            await dispatcher.InvokeAsync(() =>
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    if (updated.TryGetValue(Items[i].FullPath, out var newItem))
                        Items[i] = newItem;
                }
            });

            PushStatus($"扫描完成: {snapshot.Count} 个项目", isBusy: false);
        }
        catch (OperationCanceledException)
        {
            PushStatus("扫描已取消", isBusy: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan failed");
            PushStatus($"扫描失败: {ex.Message}", isBusy: false);
        }
        finally
        {
            IsScanning = false;
            CurrentScanDirectory = string.Empty;
            ScanProgress = 100;
        }
    }

    [RelayCommand]
    private void CancelScan()
    {
        _scanCts?.Cancel();
    }

    // ═════════════════════════════════════════════════════════════════════
    // Migration
    // ═════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task MigrateAsync()
    {
        if (IsMigrating || SelectedItems.Count == 0) return;

        // Validate target path
        if (string.IsNullOrWhiteSpace(TargetPath))
        {
            PushStatus("请先设置目标路径", isBusy: false);
            return;
        }

        if (!_browser.DirectoryExists(TargetPath))
        {
            try { Directory.CreateDirectory(TargetPath); }
            catch (Exception ex)
            {
                PushStatus($"无法创建目标目录: {ex.Message}", isBusy: false);
                return;
            }
        }

        // BUG-008: refuse to migrate to the same drive
        var targetDriveRoot = Path.GetPathRoot(TargetPath)
            ?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? string.Empty;
        var sameDriveItems = SelectedItems
            .Where(s =>
            {
                var srcRoot = Path.GetPathRoot(s.FullPath)
                    ?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return string.Equals(srcRoot, targetDriveRoot, StringComparison.OrdinalIgnoreCase);
            })
            .Select(s => s.Name)
            .ToList();

        if (sameDriveItems.Count > 0)
        {
            PushStatus($"错误: {string.Join(", ", sameDriveItems)} 与目标磁盘相同", isBusy: false);
            return;
        }

        IsMigrating = true;
        MigrationProgress = 0;

        var itemsToMigrate = SelectedItems
            .Where(s => s.Status != FileSystemItemStatus.Migrated && !s.IsSymlink)
            .ToList();

        _batchTotalBytes = itemsToMigrate.Sum(s => s.SizeBytes > 0 ? s.SizeBytes : 0);
        _batchCopiedBytes = 0;

        try
        {
            bool hasError = false;

            foreach (var item in itemsToMigrate)
            {
                if (item.IsSymlink || item.Status == FileSystemItemStatus.Migrated)
                {
                    _logger.LogWarning("Skipping already migrated item: {Name}", item.Name);
                    continue;
                }

                var request = new MigrationRequest
                {
                    Type = MigrationType.Generic,
                    Name = item.Name,
                    SourcePath = item.FullPath,
                    TargetRootPath = TargetPath,
                };

                var task = await _migrationEngine.CreateTaskAsync(request);
                CurrentTask = task;

                var result = await _migrationEngine.ExecuteAsync(task);

                if (result.Success)
                {
                    item.Status = FileSystemItemStatus.Migrated;
                    item.IsSymlink = true;
                    _logger.LogInformation("Migrated: {Name}", item.Name);
                    _batchCopiedBytes += item.SizeBytes > 0 ? item.SizeBytes : 0;
                }
                else
                {
                    hasError = true;
                    _logger.LogError("Migration failed for {Name}: {Error}", item.Name, result.ErrorMessage);
                    PushStatus($"迁移失败: {item.Name} - {result.ErrorMessage ?? "未知错误"}", isBusy: false);
                    break;
                }
            }

            if (!hasError)
                PushStatus("迁移完成", isBusy: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration error");
            PushStatus($"迁移出错: {ex.Message}", isBusy: false);
        }
        finally
        {
            IsMigrating = false;
            CurrentTask = null;
            _batchTotalBytes = 0;
            _batchCopiedBytes = 0;
            ClearSelection();
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    // Refresh
    // ═════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (!string.IsNullOrEmpty(CurrentPath))
            await NavigateToPathAsync(CurrentPath);
    }

    // ═════════════════════════════════════════════════════════════════════
    // Selection
    // ═════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void SelectAll()
    {
        _isSynchronizingSelection = true;
        try
        {
            SelectedItems.Clear();
            foreach (var item in Items)
            {
                if (item.Status != FileSystemItemStatus.Migrated && !item.IsSymlink)
                {
                    item.IsSelected = true;
                    SelectedItems.Add(item);
                }
            }
        }
        finally { _isSynchronizingSelection = false; }
        UpdateSelectedInfo();
    }

    [RelayCommand]
    private void DeselectAll()
    {
        ClearSelection();
    }

    // ═════════════════════════════════════════════════════════════════════
    // Context menu
    // ═════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void OpenInExplorer(FileSystemItem? item)
    {
        if (item is null) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{item.FullPath}\"",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open Explorer for {Path}", item.FullPath);
        }
    }

    [RelayCommand]
    private void CopyPath(FileSystemItem? item)
    {
        if (item is null) return;
        try { System.Windows.Clipboard.SetText(item.FullPath); }
        catch (Exception ex) { _logger.LogError(ex, "Clipboard failed"); }
    }

    [RelayCommand]
    private async Task RecalculateSizeAsync(FileSystemItem? item)
    {
        if (item is null) return;
        try
        {
            await _browser.RecalculateSizeAsync(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RecalculateSize failed for {Path}", item.FullPath);
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    // Browse target path
    // ═════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void BrowseTargetPath()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "选择迁移目标文件夹",
            SelectedPath = TargetPath,
            ShowNewFolderButton = true,
            UseDescriptionForTitle = true,
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK &&
            !string.IsNullOrEmpty(dialog.SelectedPath))
            TargetPath = dialog.SelectedPath;
    }

    // ═════════════════════════════════════════════════════════════════════
    // Sidebar loading
    // ═════════════════════════════════════════════════════════════════════

    private async Task LoadQuickAccessAsync()
    {
        try
        {
            var items = await _browser.GetQuickAccessItemsAsync();
            var dispatcher = System.Windows.Application.Current.Dispatcher;
            await dispatcher.InvokeAsync(() =>
            {
                QuickAccessItems.Clear();
                foreach (var item in items.OrderBy(i => i.SortOrder))
                    QuickAccessItems.Add(item);
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load quick access");
        }
    }

    private async Task LoadDrivesAsync()
    {
        try
        {
            var drives = await _browser.GetDrivesAsync();
            var dispatcher = System.Windows.Application.Current.Dispatcher;
            await dispatcher.InvokeAsync(() =>
            {
                Drives.Clear();
                foreach (var d in drives)
                    Drives.Add(d);
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load drives");
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    // Collection change handlers
    // ═════════════════════════════════════════════════════════════════════

    private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isSynchronizingSelection) return;

        if (e.OldItems is not null)
        {
            foreach (FileSystemItem item in e.OldItems)
            {
                item.PropertyChanged -= Item_PropertyChanged;
                SelectedItems.Remove(item);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (FileSystemItem item in e.NewItems)
            {
                item.PropertyChanged += Item_PropertyChanged;
                if (item.IsSelected && !SelectedItems.Contains(item))
                    SelectedItems.Add(item);
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
            RebuildSelectionFromItems();

        UpdateSelectedInfo();
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not FileSystemItem item || _isSynchronizingSelection) return;

        if (e.PropertyName == nameof(FileSystemItem.IsSelected))
        {
            if (item.IsSelected)
            {
                if (!SelectedItems.Contains(item))
                    SelectedItems.Add(item);
            }
            else
            {
                SelectedItems.Remove(item);
            }
            UpdateSelectedInfo();
        }

        if (e.PropertyName == nameof(FileSystemItem.SizeBytes) && SelectedItems.Contains(item))
            UpdateSelectedInfo();
    }

    private void ReplaceItems(IReadOnlyList<FileSystemItem> newItems)
    {
        _isSynchronizingSelection = true;
        try
        {
            foreach (var item in Items)
                item.PropertyChanged -= Item_PropertyChanged;
            Items.Clear();
            SelectedItems.Clear();
            foreach (var item in newItems)
            {
                item.PropertyChanged += Item_PropertyChanged;
                Items.Add(item);
            }
        }
        finally { _isSynchronizingSelection = false; }
        UpdateSelectedInfo();
    }

    private void ClearSelection()
    {
        _isSynchronizingSelection = true;
        try
        {
            foreach (var item in SelectedItems.ToList())
                item.IsSelected = false;
            SelectedItems.Clear();
        }
        finally { _isSynchronizingSelection = false; }
        UpdateSelectedInfo();
    }

    private void RebuildSelectionFromItems()
    {
        _isSynchronizingSelection = true;
        try
        {
            SelectedItems.Clear();
            foreach (var item in Items.Where(i => i.IsSelected))
                SelectedItems.Add(item);
        }
        finally { _isSynchronizingSelection = false; }
        UpdateSelectedInfo();
    }

    private void UpdateSelectedInfo()
    {
        TotalSelectedCount = SelectedItems.Count;
        TotalSelectedSize = SelectedItems.Sum(s => s.SizeBytes > 0 ? s.SizeBytes : 0);
    }

    // ═════════════════════════════════════════════════════════════════════
    // Status forwarding
    // ═════════════════════════════════════════════════════════════════════

    private void PushStatus(string message, bool isBusy)
    {
        StatusMessage = message;
        _mainViewModel.SetStatus(message, isBusy);
    }

    // ═════════════════════════════════════════════════════════════════════
    // Migration engine event handlers
    // ═════════════════════════════════════════════════════════════════════

    private void OnMigrationProgressChanged(object? sender, MigrationProgressEventArgs e)
    {
        int percent;
        if (_batchTotalBytes > 0)
        {
            var doneSoFar = _batchCopiedBytes + e.BytesCopied;
            percent = (int)Math.Clamp(doneSoFar * 100L / _batchTotalBytes, 0, 100);
        }
        else
        {
            percent = e.ProgressPercent;
        }

        MigrationProgress = percent;
        PushStatus($"迁移中: {e.FilesCopied}/{e.TotalFiles} 文件 ({e.BytesCopied / (1024 * 1024)}/{e.TotalBytes / (1024 * 1024)} MB)", isBusy: true);
    }

    private void OnMigrationError(object? sender, MigrationErrorEventArgs e)
    {
        _logger.LogError("Migration error: {Message}", e.Message);
        PushStatus($"迁移错误: {e.Message}", isBusy: false);
    }

    private void OnMigrationStateChanged(object? sender, MigrationTask task)
        => CurrentTask = task;

    // ═════════════════════════════════════════════════════════════════════
    // Localization
    // ═════════════════════════════════════════════════════════════════════

    private void NotifyLocalizedStrings()
    {
        OnPropertyChanged(nameof(StatusMessage));
    }

    // ═════════════════════════════════════════════════════════════════════
    // Helpers
    // ═════════════════════════════════════════════════════════════════════

    private static string FormatSize(long sizeBytes)
    {
        if (sizeBytes <= 0) return "0 KB";
        if (sizeBytes < 1024) return $"{sizeBytes} B";
        if (sizeBytes < 1024 * 1024) return $"{Math.Max(1, sizeBytes / 1024)} KB";
        if (sizeBytes < 1024L * 1024 * 1024) return $"{sizeBytes / (1024 * 1024)} MB";
        return $"{sizeBytes / (1024L * 1024 * 1024):F1} GB";
    }
}
