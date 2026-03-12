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
/// View model for software migration.
///
/// Scan flow (two commands share the same underlying stream):
///   ScanCommand  – calls ISoftwareScanner.ScanStreamAsync which uses cache when fresh,
///                  or calculates precisely on cache-miss. After the stream ends the
///                  in-memory cache is flushed to disk.
///   RefreshCommand – same stream but clears the in-memory cache first, forcing a full
///                  precise recalculation for every directory.
/// </summary>
public partial class SoftwareMigrationViewModel : ObservableObject
{
    private readonly ISoftwareScanner _softwareScanner;
    private readonly IMigrationEngine _migrationEngine;
    private readonly ISizeCacheService _sizeCache;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<SoftwareMigrationViewModel> _logger;
    private readonly MainViewModel _mainViewModel;

    // Token source for the currently active scan so it can be cancelled
    private CancellationTokenSource? _scanCts;

    // BUG-005: batch-level progress tracking across multiple migration tasks.
    private long _batchTotalBytes;
    private long _batchCopiedBytes;

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

    /// <summary>
    /// True whenever the view model is performing any background operation.
    /// Bind command CanExecute / button IsEnabled to this property.
    /// </summary>
    public bool IsBusy => IsScanning || IsMigrating;

    partial void OnIsScanningChanged(bool value)  => OnPropertyChanged(nameof(IsBusy));
    partial void OnIsMigratingChanged(bool value) => OnPropertyChanged(nameof(IsBusy));

    [ObservableProperty]
    private int _scanProgress;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    /// <summary>Directory currently being sized (shown in the progress bar label).</summary>
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
        ILogger<SoftwareMigrationViewModel> logger,
        MainViewModel mainViewModel)
    {
        _softwareScanner     = softwareScanner;
        _migrationEngine     = migrationEngine;
        _sizeCache           = sizeCache;
        _localizationService = localizationService;
        _logger              = logger;
        _mainViewModel       = mainViewModel;

        _migrationEngine.ProgressChanged += OnMigrationProgressChanged;
        _migrationEngine.ErrorOccurred   += OnMigrationError;
        _migrationEngine.StateChanged    += OnMigrationStateChanged;

        _localizationService.LanguageChanged += (_, _) => NotifyLocalizedStrings();

        InitializeDrives();
    }

    // ── Localized UI strings ─────────────────────────────────────────────
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

    /// <summary>Returns the localized tooltip for a SoftwareInfo item's status cell.</summary>
    public string GetStatusTooltip(SoftwareInfo item) => item.Status switch
    {
        SoftwareStatus.Residual   => _localizationService.GetString("SoftwareMigration.TooltipResidual"),
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
        foreach (var item in SoftwareItems)
            item.NotifyStatusTextChanged();
    }

    /// <summary>
    /// Updates the local StatusMessage and forwards the status to the main window's
    /// status bar via MainViewModel.SetStatus().
    /// </summary>
    private void PushStatus(string message, bool isBusy)
    {
        StatusMessage = message;
        _mainViewModel.SetStatus(message, isBusy);
    }

    // ── Drives ───────────────────────────────────────────────────────────

    public ObservableCollection<string> AvailableDrives { get; } = new();

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

        TargetDisk = firstOther ?? systemDrive ?? "C:";
        TargetPath = System.IO.Path.Combine(TargetDisk + "\\", "MigratedApps");
    }

    // ── Commands – path / clipboard ──────────────────────────────────────

    [RelayCommand]
    private void BrowsePath()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description            = "Select migration target folder",
            SelectedPath           = TargetPath,
            ShowNewFolderButton    = true,
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK &&
            !string.IsNullOrEmpty(dialog.SelectedPath))
        {
            TargetPath = dialog.SelectedPath;
            var root = System.IO.Path.GetPathRoot(dialog.SelectedPath)?.TrimEnd('\\', '/');
            if (!string.IsNullOrEmpty(root) && AvailableDrives.Contains(root))
                TargetDisk = root;
        }
    }

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
        catch (Exception ex) { _logger.LogError(ex, "Failed to open Explorer for {Path}", item.InstallLocation); }
    }

    [RelayCommand]
    private void CopyPath(SoftwareInfo? item)
    {
        if (item is null) return;
        try { System.Windows.Clipboard.SetText(item.InstallLocation); }
        catch (Exception ex) { _logger.LogError(ex, "Failed to copy path to clipboard"); }
    }

    // ── Commands – scan ──────────────────────────────────────────────────

    /// <summary>
    /// Cache-aware scan: uses ISoftwareScanner.ScanStreamAsync which reads the
    /// size cache for known directories and calculates precisely only on cache miss.
    /// After the stream ends the in-memory cache is saved to disk.
    /// </summary>
    [RelayCommand]
    private async Task ScanAsync()
    {
        if (IsScanning) return;
        await RunScanAsync(invalidateCache: false);
    }

    /// <summary>
    /// Full precise refresh: clears the in-memory cache so that every directory
    /// is recalculated from scratch, then saves to disk.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsScanning) return;
        await RunScanAsync(invalidateCache: true);
    }

    /// <summary>Shared implementation for Scan and Refresh commands.</summary>
    private async Task RunScanAsync(bool invalidateCache)
    {
        // UX-002: do not allow scanning while a migration is in progress.
        if (IsMigrating) return;

        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var ct = _scanCts.Token;

        // UX-001: take a snapshot of the current list so we can restore it on cancellation.
        var previousItems = SoftwareItems.ToList();

        IsScanning = true;
        SoftwareItems.Clear();
        ScanProgress = 0;
        CurrentScanDirectory = string.Empty;
        PushStatus(_localizationService.GetString("Status.Scanning"), isBusy: true);

        if (invalidateCache)
        {
            // BUG-001: actually clear the in-memory cache so that every directory is
            // remeasured from disk during this scan, instead of reusing stale values.
            _sizeCache.Clear();
        }

        try
        {
            var dispatcher = System.Windows.Application.Current.Dispatcher;

            var progress = new Progress<ScanProgressReport>(report =>
            {
                // Progress callback may come from any thread
                dispatcher.InvokeAsync(() =>
                {
                    CurrentScanDirectory = report.CurrentDirectory;
                    ScanProgress         = report.ProgressPercent;
                    PushStatus(_localizationService.GetString(
                        "Status.ScanningProgress", report.ItemsFound), isBusy: true);
                });
            });

            await foreach (var item in _softwareScanner.ScanStreamAsync(progress, ct))
            {
                await dispatcher.InvokeAsync(() => SoftwareItems.Add(item));
            }

            // Persist the updated cache to disk
            await _sizeCache.SaveAsync(ct);

            PushStatus(_localizationService.GetString("Status.ScanComplete", SoftwareItems.Count), isBusy: false);
        }
        catch (OperationCanceledException)
        {
            // UX-001: restore the previous list so the user is not left with an empty view.
            SoftwareItems.Clear();
            foreach (var item in previousItems)
                SoftwareItems.Add(item);

            PushStatus(_localizationService.GetString("Status.ScanCancelled"), isBusy: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan failed");
            PushStatus(_localizationService.GetString("Status.ScanFailed", ex.Message), isBusy: false);
        }
        finally
        {
            IsScanning           = false;
            CurrentScanDirectory = string.Empty;
            ScanProgress         = 100;
        }
    }

    // ── Commands – migration ─────────────────────────────────────────────

    [RelayCommand]
    private async Task MigrateAsync()
    {
        if (IsMigrating || SelectedItems.Count == 0) return;

        // BUG-008: refuse to migrate to the same drive as the source.
        var targetDriveRoot = System.IO.Path.GetPathRoot(TargetPath)
            ?.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)
            ?? string.Empty;

        var sameDriveItems = SelectedItems
            .Where(s => string.Equals(
                System.IO.Path.GetPathRoot(s.InstallLocation)
                    ?.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar),
                targetDriveRoot,
                StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Name)
            .ToList();

        if (sameDriveItems.Count > 0)
        {
            PushStatus(_localizationService.GetString(
                "Status.SameDriveError", string.Join(", ", sameDriveItems), targetDriveRoot), isBusy: false);
            _logger.LogWarning(
                "Migration aborted: target drive {Drive} is the same as the source drive for: {Items}",
                targetDriveRoot, string.Join(", ", sameDriveItems));
            return;
        }

        IsMigrating       = true;
        MigrationProgress = 0;
        PushStatus(_localizationService.GetString("Status.Migrating"), isBusy: true);

        // BUG-005: pre-compute total bytes across all selected items so the progress
        // bar reflects the whole batch, not just the current task.
        var itemsToMigrate = SelectedItems
            .Where(s => s.Status != SoftwareStatus.Migrated)
            .ToList();
        long batchTotalBytes  = itemsToMigrate.Sum(s => s.SizeBytes > 0 ? s.SizeBytes : 0);
        long batchCopiedBytes = 0;
        _batchTotalBytes  = batchTotalBytes;
        _batchCopiedBytes = 0;

        try
        {
            bool hasError = false;

            foreach (var software in itemsToMigrate)
            {
                if (software.Status == SoftwareStatus.Migrated)
                {
                    _logger.LogWarning("Skipping already migrated software: {Name}", software.Name);
                    continue;
                }

                var request = new MigrationRequest
                {
                    Type           = MigrationType.Software,
                    Name           = software.Name,
                    SourcePath     = software.InstallLocation,
                    TargetRootPath = TargetPath
                };

                var task   = await _migrationEngine.CreateTaskAsync(request);
                CurrentTask = task;

                var result = await _migrationEngine.ExecuteAsync(task);

                if (result.Success)
                {
                    software.Status    = SoftwareStatus.Migrated;
                    software.IsSymlink = true;
                    _logger.LogInformation("Successfully migrated: {Name}", software.Name);
                    batchCopiedBytes += software.SizeBytes > 0 ? software.SizeBytes : 0;
                    _batchCopiedBytes = batchCopiedBytes;
                }
                else
                {
                    hasError = true;
                    _logger.LogError("Failed to migrate {Name}: {Error}", software.Name, result.ErrorMessage);
                    PushStatus(_localizationService.GetString(
                        "Status.MigrationFailed", software.Name, result.ErrorMessage ?? string.Empty), isBusy: false);

                    // BUG-004: stop the batch on first failure to avoid further data risk.
                    break;
                }
            }

            if (!hasError)
                PushStatus(_localizationService.GetString("Status.MigrationComplete"), isBusy: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed");
            PushStatus(_localizationService.GetString("Status.MigrationError", ex.Message), isBusy: false);
        }
        finally
        {
            IsMigrating       = false;
            CurrentTask       = null;
            _batchTotalBytes  = 0;
            _batchCopiedBytes = 0;
            SelectedItems.Clear();
            UpdateSelectedInfo();
        }
    }

    /// <summary>
    /// Recalculates the precise size for a single item and updates its status.
    /// Called from the DataGrid row context menu.
    /// </summary>
    [RelayCommand]
    private async Task RecalculateSizeAsync(SoftwareInfo? software)
    {
        if (software is null) return;
        try
        {
            await _softwareScanner.RecalculateSizeAsync(software);
            software.NotifyStatusTextChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recalculate size for {Name}", software.Name);
        }
    }

    // ── Selection ────────────────────────────────────────────────────────

    [RelayCommand]
    private void SelectAll()
    {
        SelectedItems.Clear();
        // BUG-006: only select items that are actually eligible for migration.
        foreach (var item in SoftwareItems.Where(i => i.Status == SoftwareStatus.Normal))
            SelectedItems.Add(item);
        UpdateSelectedInfo();
    }

    [RelayCommand]
    private void DeselectAll()
    {
        SelectedItems.Clear();
        UpdateSelectedInfo();
    }

    partial void OnSelectedItemsChanged(ObservableCollection<SoftwareInfo> value)
        => UpdateSelectedInfo();

    private void UpdateSelectedInfo()
    {
        TotalSelectedCount = SelectedItems.Count;
        TotalSelectedSize  = SelectedItems.Sum(s => s.SizeBytes > 0 ? s.SizeBytes : 0);
    }

    // ── Migration engine event handlers ──────────────────────────────────

    private void OnMigrationProgressChanged(object? sender, MigrationProgressEventArgs e)
    {
        // BUG-005: compute progress relative to the whole batch, not just the current task.
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
        PushStatus(_localizationService.GetString("Status.MigrationProgress",
            e.FilesCopied, e.TotalFiles,
            e.BytesCopied / (1024 * 1024), e.TotalBytes / (1024 * 1024)), isBusy: true);
    }

    private void OnMigrationError(object? sender, MigrationErrorEventArgs e)
    {
        _logger.LogError("Migration error: {Message}", e.Message);
        PushStatus(_localizationService.GetString("Status.MigrationError", e.Message), isBusy: false);
    }

    private void OnMigrationStateChanged(object? sender, MigrationTask task)
        => CurrentTask = task;
}

