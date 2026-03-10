using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using winC2D.Core.Events;
using winC2D.Core.Models;
using winC2D.Core.Services;
using winC2D.Infrastructure.Localization;

namespace winC2D.App.ViewModels;

/// <summary>
/// View model for software migration
/// </summary>
public partial class SoftwareMigrationViewModel : ObservableObject
{
    private readonly ISoftwareScanner _softwareScanner;
    private readonly IMigrationEngine _migrationEngine;
    private readonly ISizeCacheService _sizeCache;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<SoftwareMigrationViewModel> _logger;

    /// <summary>Max directory size allowed for precise calculation during startup scan (10 MB)</summary>
    private const long QuickScanThreshold = 10L * 1024 * 1024;
    /// <summary>Max concurrent size calculations during manual refresh</summary>
    private const int RefreshConcurrency = 4;
    
    [ObservableProperty]
    private ObservableCollection<SoftwareInfo> _softwareItems = new();
    
    [ObservableProperty]
    private ObservableCollection<SoftwareInfo> _selectedItems = new();
    
    [ObservableProperty]
    private string _searchText = string.Empty;
    
    [ObservableProperty]
    private bool _isScanning;
    
    [ObservableProperty]
    private bool _isMigrating;
    
    [ObservableProperty]
    private int _scanProgress;
    
    [ObservableProperty]
    private string _statusMessage = "Ready";

    /// <summary>
    /// Name of the directory currently being scanned (displayed in progress bar)
    /// </summary>
    [ObservableProperty]
    private string _currentScanDirectory = string.Empty;
    
    [ObservableProperty]
    private string _targetDisk = "D:";
    
    [ObservableProperty]
    private string _targetPath = "D:\\MigratedApps";
    
    [ObservableProperty]
    private long _totalSelectedSize;
    
    [ObservableProperty]
    private int _totalSelectedCount;
    
    [ObservableProperty]
    private MigrationTask? _currentTask;
    
    [ObservableProperty]
    private int _migrationProgress;

    /// <summary>The item the user right-clicked on; set by the view's PreviewMouseRightButtonDown handler.</summary>
    [ObservableProperty]
    private SoftwareInfo? _rightClickedItem;

    public SoftwareMigrationViewModel(
        ISoftwareScanner softwareScanner,
        IMigrationEngine migrationEngine,
        ISizeCacheService sizeCache,
        ILocalizationService localizationService,
        ILogger<SoftwareMigrationViewModel> logger)
    {
        _softwareScanner  = softwareScanner;
        _migrationEngine  = migrationEngine;
        _sizeCache        = sizeCache;
        _localizationService = localizationService;
        _logger           = logger;
        
        // Subscribe to migration engine events
        _migrationEngine.ProgressChanged += OnMigrationProgressChanged;
        _migrationEngine.ErrorOccurred += OnMigrationError;
        _migrationEngine.StateChanged += OnMigrationStateChanged;

        // Re-fire localized properties when language changes
        _localizationService.LanguageChanged += (_, _) => NotifyLocalizedStrings();

        // Populate available drives
        InitializeDrives();
    }

    // ── Localized UI strings ──────────────────────────────────────────────
    public string L_Header       => _localizationService.GetString("SoftwareMigration.Header");
    public string L_Refresh      => _localizationService.GetString("SoftwareMigration.Refresh");
    public string L_FullRefresh  => _localizationService.GetString("SoftwareMigration.FullRefresh");
    public string L_Search       => _localizationService.GetString("SoftwareMigration.Search");
    public string L_SelectAll    => _localizationService.GetString("SoftwareMigration.SelectAll");
    public string L_DeselectAll  => _localizationService.GetString("SoftwareMigration.DeselectAll");
    public string L_Migrate      => _localizationService.GetString("SoftwareMigration.Migrate");
    public string L_TargetDisk   => _localizationService.GetString("SoftwareMigration.TargetDisk");
    public string L_TargetPath   => _localizationService.GetString("SoftwareMigration.TargetPath");
    public string L_Selected     => _localizationService.GetString("SoftwareMigration.Selected");
    public string L_BrowsePath   => _localizationService.GetString("SoftwareMigration.BrowsePath");
    public string L_ColName      => _localizationService.GetString("SoftwareMigration.ColName");
    public string L_ColPath      => _localizationService.GetString("SoftwareMigration.ColPath");
    public string L_ColSize      => _localizationService.GetString("SoftwareMigration.ColSize");
    public string L_ColStatus    => _localizationService.GetString("SoftwareMigration.ColStatus");
    public string L_MenuOpenExplorer => _localizationService.GetString("SoftwareMigration.MenuOpenExplorer");
    public string L_MenuCopyPath     => _localizationService.GetString("SoftwareMigration.MenuCopyPath");

    /// <summary>Returns the localized display text for a SoftwareStatus value.</summary>
    public string GetStatusText(SoftwareStatus status) => status switch
    {
        SoftwareStatus.Normal     => _localizationService.GetString("SoftwareMigration.StatusNormal"),
        SoftwareStatus.Migrated   => _localizationService.GetString("SoftwareMigration.StatusMigrated"),
        SoftwareStatus.Suspicious => _localizationService.GetString("SoftwareMigration.StatusSuspicious"),
        SoftwareStatus.Empty      => _localizationService.GetString("SoftwareMigration.StatusEmpty"),
        SoftwareStatus.Residual   => _localizationService.GetString("SoftwareMigration.StatusResidual"),
        _                         => status.ToString()
    };

    /// <summary>Returns the localized tooltip for a SoftwareInfo item's size cell.</summary>
    public string GetSizeTooltip(SoftwareInfo item)
    {
        if (item.SizeBytes == -1)
            return _localizationService.GetString("SoftwareMigration.TooltipSizeExceeds");
        return string.Empty;
    }

    /// <summary>Returns the localized tooltip for a SoftwareInfo item's status cell.</summary>
    public string GetStatusTooltip(SoftwareInfo item) => item.Status switch
    {
        SoftwareStatus.Suspicious => _localizationService.GetString("SoftwareMigration.TooltipSuspicious"),
        SoftwareStatus.Empty      => _localizationService.GetString("SoftwareMigration.TooltipEmpty"),
        SoftwareStatus.Migrated   => _localizationService.GetString("SoftwareMigration.TooltipMigrated"),
        _                         => string.Empty
    };

    private void NotifyLocalizedStrings()
    {
        OnPropertyChanged(nameof(L_Header));
        OnPropertyChanged(nameof(L_Refresh));
        OnPropertyChanged(nameof(L_FullRefresh));
        OnPropertyChanged(nameof(L_Search));
        OnPropertyChanged(nameof(L_SelectAll));
        OnPropertyChanged(nameof(L_DeselectAll));
        OnPropertyChanged(nameof(L_Migrate));
        OnPropertyChanged(nameof(L_TargetDisk));
        OnPropertyChanged(nameof(L_TargetPath));
        OnPropertyChanged(nameof(L_Selected));
        OnPropertyChanged(nameof(L_BrowsePath));
        OnPropertyChanged(nameof(L_ColName));
        OnPropertyChanged(nameof(L_ColPath));
        OnPropertyChanged(nameof(L_ColSize));
        OnPropertyChanged(nameof(L_ColStatus));
        OnPropertyChanged(nameof(L_MenuOpenExplorer));
        OnPropertyChanged(nameof(L_MenuCopyPath));
        OnPropertyChanged(nameof(StatusMessage));
        // Force DataGrid row cells to re-read localized status / tooltip text
        foreach (var item in SoftwareItems)
        {
            item.NotifyStatusTextChanged();
        }
    }
    
    /// <summary>
    /// Available disk drives
    /// </summary>
    public ObservableCollection<string> AvailableDrives { get; } = new();

    /// <summary>Populate AvailableDrives from DriveInfo and pre-select a non-system drive.</summary>
    private void InitializeDrives()
    {
        AvailableDrives.Clear();
        string? systemDrive = System.IO.Path.GetPathRoot(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows))
            ?.TrimEnd('\\');

        string? firstOther = null;
        foreach (var drive in System.IO.DriveInfo.GetDrives())
        {
            if (drive.DriveType is System.IO.DriveType.Fixed or System.IO.DriveType.Network)
            {
                var letter = drive.Name.TrimEnd('\\');
                AvailableDrives.Add(letter);
                if (firstOther is null &&
                    !string.Equals(letter, systemDrive, StringComparison.OrdinalIgnoreCase))
                    firstOther = letter;
            }
        }

        // Pre-select the first non-system drive, or C: if there is none
        TargetDisk = firstOther ?? systemDrive ?? "C:";
        TargetPath = System.IO.Path.Combine(TargetDisk + "\\", "MigratedApps");
    }

    /// <summary>Open a folder-picker dialog and update TargetPath.</summary>
    [RelayCommand]
    private void BrowsePath()
    {
        // WPF has no built-in folder dialog in .NET 6+; use the Win32 shell dialog via
        // Microsoft.WindowsAPICodePack-Shell if available, otherwise fall back to the
        // legacy WinForms FolderBrowserDialog (always available on Windows).
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description        = "Select migration target folder",
            SelectedPath       = TargetPath,
            ShowNewFolderButton = true,
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK &&
            !string.IsNullOrEmpty(dialog.SelectedPath))
        {
            TargetPath = dialog.SelectedPath;
            // Update disk label to match chosen path's root
            var root = System.IO.Path.GetPathRoot(dialog.SelectedPath)
                           ?.TrimEnd('\\', '/');
            if (!string.IsNullOrEmpty(root) && AvailableDrives.Contains(root))
                TargetDisk = root;
        }
    }

    /// <summary>Open the selected item's folder in Windows Explorer.</summary>
    [RelayCommand]
    private void OpenInExplorer(SoftwareInfo? item)
    {
        if (item is null) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = "explorer.exe",
                Arguments       = $"\"{item.InstallLocation}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open Explorer for {Path}", item.InstallLocation);
        }
    }

    /// <summary>Copy the selected item's path to clipboard.</summary>
    [RelayCommand]
    private void CopyPath(SoftwareInfo? item)
    {
        if (item is null) return;
        try { System.Windows.Clipboard.SetText(item.InstallLocation); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy path to clipboard");
        }
    }
    
    /// <summary>
    /// Fast startup scan: reads sizes from cache; skips precise calculation for uncached dirs > threshold.
    /// </summary>
    [RelayCommand]
    private async Task ScanAsync()
    {
        if (IsScanning)
            return;
        
        IsScanning = true;
        StatusMessage = _localizationService.GetString("Status.Scanning");
        SoftwareItems.Clear();
        ScanProgress = 0;
        CurrentScanDirectory = string.Empty;
        
        try
        {
            var dispatcher = System.Windows.Application.Current.Dispatcher;

            await Task.Run(async () =>
            {
                var dirs = _softwareScanner.GetDefaultScanDirectories().ToList();
                var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int scannedCount = 0;

                foreach (var baseDir in dirs)
                {
                    if (!System.IO.Directory.Exists(baseDir))
                    { scannedCount++; continue; }

                    await dispatcher.InvokeAsync(() =>
                    {
                        CurrentScanDirectory = baseDir;
                        ScanProgress = dirs.Count > 0 ? (int)((double)scannedCount / dirs.Count * 100) : 0;
                    });

                    var subDirs = System.IO.Directory.GetDirectories(baseDir);

                    foreach (var subDir in subDirs)
                    {
                        var norm = subDir.TrimEnd(
                            System.IO.Path.DirectorySeparatorChar,
                            System.IO.Path.AltDirectorySeparatorChar);

                        if (!visited.Add(norm.ToLowerInvariant()))
                            continue;

                        await dispatcher.InvokeAsync(() =>
                            CurrentScanDirectory = System.IO.Path.GetFileName(norm) ?? norm);

                        var item = await BuildItemWithCacheAsync(norm);
                        await dispatcher.InvokeAsync(() =>
                        {
                            SoftwareItems.Add(item);
                            StatusMessage = _localizationService.GetString(
                                "Status.ScanningProgress", SoftwareItems.Count);
                        });
                    }

                    scannedCount++;
                    await dispatcher.InvokeAsync(() =>
                        ScanProgress = dirs.Count > 0
                            ? (int)((double)scannedCount / dirs.Count * 100) : 100);
                }
            });

            StatusMessage = _localizationService.GetString("Status.ScanComplete", SoftwareItems.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan software");
            StatusMessage = _localizationService.GetString("Status.ScanFailed", ex.Message);
        }
        finally
        {
            IsScanning = false;
            CurrentScanDirectory = string.Empty;
            ScanProgress = 100;
        }
    }

    /// <summary>
    /// Full refresh: precisely calculates size for every directory (concurrently) and writes cache.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsScanning)
            return;

        IsScanning = true;
        StatusMessage = _localizationService.GetString("Status.Scanning");
        SoftwareItems.Clear();
        ScanProgress = 0;
        CurrentScanDirectory = string.Empty;

        try
        {
            var dispatcher = System.Windows.Application.Current.Dispatcher;

            await Task.Run(async () =>
            {
                var dirs = _softwareScanner.GetDefaultScanDirectories().ToList();
                var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Collect all sub-directories first
                var allSubDirs = new List<string>();
                foreach (var baseDir in dirs)
                {
                    if (!System.IO.Directory.Exists(baseDir)) continue;
                    foreach (var sub in System.IO.Directory.GetDirectories(baseDir))
                    {
                        var norm = sub.TrimEnd(
                            System.IO.Path.DirectorySeparatorChar,
                            System.IO.Path.AltDirectorySeparatorChar);
                        if (visited.Add(norm.ToLowerInvariant()))
                            allSubDirs.Add(norm);
                    }
                }

                int total     = allSubDirs.Count;
                int completed = 0;
                var semaphore = new System.Threading.SemaphoreSlim(RefreshConcurrency, RefreshConcurrency);

                var tasks = allSubDirs.Select(async sub =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        await dispatcher.InvokeAsync(() =>
                            CurrentScanDirectory = System.IO.Path.GetFileName(sub) ?? sub);

                        // Always calculate exact size
                        long size = await _softwareScanner.CalculateSizeAsync(sub);
                        _sizeCache.Set(sub, size);

                        var item = BuildItemFromSize(sub, size);

                        int done = System.Threading.Interlocked.Increment(ref completed);
                        await dispatcher.InvokeAsync(() =>
                        {
                            SoftwareItems.Add(item);
                            ScanProgress = total > 0 ? (int)((double)done / total * 100) : 100;
                            StatusMessage = _localizationService.GetString(
                                "Status.ScanningProgress", SoftwareItems.Count);
                        });
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await System.Threading.Tasks.Task.WhenAll(tasks);

                // Persist cache after full refresh
                _sizeCache.Save();
            });

            StatusMessage = _localizationService.GetString("Status.ScanComplete", SoftwareItems.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual refresh failed");
            StatusMessage = _localizationService.GetString("Status.ScanFailed", ex.Message);
        }
        finally
        {
            IsScanning = false;
            CurrentScanDirectory = string.Empty;
            ScanProgress = 100;
        }
    }

    // ── cache-aware helpers ───────────────────────────────────────────────

    /// <summary>
    /// Build a SoftwareInfo for <paramref name="path"/>.
    /// Uses cache when fresh; otherwise falls back to quick threshold scan.
    /// </summary>
    private async Task<SoftwareInfo> BuildItemWithCacheAsync(string path)
    {
        var name = System.IO.Path.GetFileName(path) ?? path;
        var isSymlink = IsSymlink(path);

        if (isSymlink)
            return new SoftwareInfo { Name = name, InstallLocation = path, IsSymlink = true, Status = SoftwareStatus.Migrated };

        // Try cache first
        if (_sizeCache.TryGet(path, out var cached))
        {
            return BuildItemFromSize(path, cached.SizeBytes);
        }

        // Cache miss: run quick threshold scan (skip dirs that would exceed threshold)
        long size = 0;
        bool exceeded = false;
        try
        {
            await Task.Run(() =>
            {
                foreach (var file in System.IO.Directory.EnumerateFiles(path, "*", System.IO.SearchOption.AllDirectories))
                {
                    try { size += new System.IO.FileInfo(file).Length; } catch { }
                    if (size > QuickScanThreshold) { exceeded = true; break; }
                }
            });
        }
        catch { /* access denied etc. */ }

        var displaySize = exceeded ? -1L : size;
        return BuildItemFromSize(path, displaySize);
    }

    private static SoftwareInfo BuildItemFromSize(string path, long sizeBytes)
    {
        var name = System.IO.Path.GetFileName(path) ?? path;
        var isSymlink = IsSymlink(path);

        SoftwareStatus status;
        if (isSymlink)
            status = SoftwareStatus.Migrated;
        else if (sizeBytes == 0)
            status = SoftwareStatus.Empty;
        else if (sizeBytes > 0 && sizeBytes <= 10L * 1024 * 1024)
            status = SoftwareStatus.Suspicious;
        else
            status = SoftwareStatus.Normal;

        return new SoftwareInfo
        {
            Name            = name,
            InstallLocation = path,
            SizeBytes       = sizeBytes,
            IsSymlink       = isSymlink,
            Status          = status,
            SuspiciousChecked = sizeBytes != -1
        };
    }

    private static bool IsSymlink(string path)
    {
        try
        {
            var attr = System.IO.File.GetAttributes(path);
            return (attr & System.IO.FileAttributes.ReparsePoint) != 0;
        }
        catch { return false; }
    }
    
    /// <summary>
    /// Migrate selected software
    /// </summary>
    [RelayCommand]
    private async Task MigrateAsync()
    {
        if (IsMigrating || SelectedItems.Count == 0)
            return;
        
        IsMigrating = true;
        MigrationProgress = 0;
        StatusMessage = _localizationService.GetString("Status.Migrating");
        
        try
        {
            foreach (var software in SelectedItems.ToList())
            {
                if (software.Status == SoftwareStatus.Migrated)
                {
                    _logger.LogWarning("Skipping already migrated software: {Name}", software.Name);
                    continue;
                }
                
                var request = new MigrationRequest
                {
                    Type = MigrationType.Software,
                    Name = software.Name,
                    SourcePath = software.InstallLocation,
                    TargetRootPath = TargetPath
                };
                
                var task = await _migrationEngine.CreateTaskAsync(request);
                CurrentTask = task;
                
                var result = await _migrationEngine.ExecuteAsync(task);
                
                if (result.Success)
                {
                    software.Status = SoftwareStatus.Migrated;
                    software.IsSymlink = true;
                    _logger.LogInformation("Successfully migrated: {Name}", software.Name);
                }
                else
                {
                    _logger.LogError("Failed to migrate {Name}: {Error}", software.Name, result.ErrorMessage);
                    StatusMessage = _localizationService.GetString("Status.MigrationFailed", software.Name, result.ErrorMessage ?? string.Empty);
                }
            }
            
            StatusMessage = _localizationService.GetString("Status.MigrationComplete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed");
            StatusMessage = _localizationService.GetString("Status.MigrationError", ex.Message);
        }
        finally
        {
            IsMigrating = false;
            CurrentTask = null;
            SelectedItems.Clear();
            UpdateSelectedInfo();
        }
    }
    
    /// <summary>
    /// Check suspicious software
    /// </summary>
    [RelayCommand]
    private async Task CheckSuspiciousAsync(SoftwareInfo? software)
    {
        if (software == null || software.SuspiciousChecked)
            return;
        
        try
        {
            var updated = await _softwareScanner.CheckSuspiciousAsync(software);
            
            // Update properties
            software.Status = updated.Status;
            software.SizeBytes = updated.SizeBytes;
            software.SuspiciousChecked = updated.SuspiciousChecked;
            
            // Notify UI
            OnPropertyChanged(nameof(software.SizeText));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check suspicious software: {Name}", software.Name);
        }
    }
    
    /// <summary>
    /// Select all items
    /// </summary>
    [RelayCommand]
    private void SelectAll()
    {
        SelectedItems.Clear();
        foreach (var item in SoftwareItems)
        {
            SelectedItems.Add(item);
        }
        UpdateSelectedInfo();
    }
    
    /// <summary>
    /// Deselect all items
    /// </summary>
    [RelayCommand]
    private void DeselectAll()
    {
        SelectedItems.Clear();
        UpdateSelectedInfo();
    }
    
    /// <summary>
    /// Update selected items info
    /// </summary>
    partial void OnSelectedItemsChanged(ObservableCollection<SoftwareInfo> value)
    {
        UpdateSelectedInfo();
    }
    
    private void UpdateSelectedInfo()
    {
        TotalSelectedCount = SelectedItems.Count;
        TotalSelectedSize = SelectedItems.Sum(s => s.SizeBytes > 0 ? s.SizeBytes : 0);
    }
    
    private void OnScanProgressChanged(object? sender, ScanProgressEventArgs e)
    {
        ScanProgress = e.ProgressPercent;
        CurrentScanDirectory = System.IO.Path.GetFileName(e.CurrentDirectory) ?? e.CurrentDirectory;
        StatusMessage = _localizationService.GetString("Status.ScanningProgress", e.ItemsFound);
    }
    
    private void OnMigrationProgressChanged(object? sender, MigrationProgressEventArgs e)
    {
        MigrationProgress = e.ProgressPercent;
        StatusMessage = _localizationService.GetString("Status.MigrationProgress", 
            e.FilesCopied, e.TotalFiles, e.BytesCopied / (1024 * 1024), e.TotalBytes / (1024 * 1024));
    }
    
    private void OnMigrationError(object? sender, MigrationErrorEventArgs e)
    {
        _logger.LogError("Migration error: {Message}", e.Message);
        StatusMessage = _localizationService.GetString("Status.MigrationError", e.Message);
    }
    
    private void OnMigrationStateChanged(object? sender, MigrationTask task)
    {
        CurrentTask = task;
    }
}