using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using winC2D.Core.Models;
using winC2D.Core.Services;
using winC2D.Core.FileSystem;
using winC2D.Infrastructure.Localization;

namespace winC2D.App.ViewModels;

/// <summary>
/// View model for AppData migration
/// </summary>
public partial class AppDataMigrationViewModel : ObservableObject
{
    private readonly IFileSystem _fileSystem;
    private readonly IMigrationEngine _migrationEngine;
    private readonly ISizeCacheService _sizeCache;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<AppDataMigrationViewModel> _logger;
    private bool _isSynchronizingSelection;
    
    [ObservableProperty]
    private ObservableCollection<AppDataInfo> _appDataItems = new();
    
    [ObservableProperty]
    private ObservableCollection<AppDataInfo> _selectedItems = new();
    
    [ObservableProperty]
    private string _searchText = string.Empty;
    
    [ObservableProperty]
    private bool _isScanning;
    
    [ObservableProperty]
    private bool _isMigrating;
    
    [ObservableProperty]
    private string _statusMessage = "Ready";
    
    [ObservableProperty]
    private string _targetDisk = "D:";
    
    [ObservableProperty]
    private string _targetPath = "D:\\MigratedAppData";
    
    public AppDataMigrationViewModel(
        IFileSystem fileSystem,
        IMigrationEngine migrationEngine,
        ISizeCacheService sizeCache,
        ILocalizationService localizationService,
        ILogger<AppDataMigrationViewModel> logger)
    {
        _fileSystem = fileSystem;
        _migrationEngine = migrationEngine;
        _sizeCache = sizeCache;
        _localizationService = localizationService;
        _logger = logger;

        _localizationService.LanguageChanged += (_, _) => NotifyLocalizationChanged();
        AppDataItems.CollectionChanged += AppDataItems_CollectionChanged;
    }

    private void NotifyLocalizationChanged()
    {
        OnPropertyChanged(nameof(L_Header));
        OnPropertyChanged(nameof(L_Refresh));
        OnPropertyChanged(nameof(L_Search));
        OnPropertyChanged(nameof(L_ColName));
        OnPropertyChanged(nameof(L_ColPath));
        OnPropertyChanged(nameof(L_ColType));
        OnPropertyChanged(nameof(L_ColSize));
        OnPropertyChanged(nameof(L_ColStatus));
        OnPropertyChanged(nameof(L_Target));
        OnPropertyChanged(nameof(L_Migrate));
        OnPropertyChanged(nameof(L_CheckSize));
        OnPropertyChanged(nameof(L_BrowsePath));
    }

    // ── Localized labels ──────────────────────────────────────────────
    public string L_Header    => _localizationService.GetString("AppDataMigration.Header");
    public string L_Refresh   => _localizationService.GetString("AppDataMigration.Refresh");
    public string L_Search    => _localizationService.GetString("AppDataMigration.Search");
    public string L_ColName   => _localizationService.GetString("AppDataMigration.ColName");
    public string L_ColPath   => _localizationService.GetString("AppDataMigration.ColPath");
    public string L_ColType   => _localizationService.GetString("AppDataMigration.ColType");
    public string L_ColSize   => _localizationService.GetString("AppDataMigration.ColSize");
    public string L_ColStatus => _localizationService.GetString("AppDataMigration.ColStatus");
    public string L_Target    => _localizationService.GetString("AppDataMigration.Target");
    public string L_Migrate   => _localizationService.GetString("AppDataMigration.Migrate");
    public string L_CheckSize => _localizationService.GetString("AppDataMigration.CheckSize");
    public string L_BrowsePath => _localizationService.GetString("SoftwareMigration.BrowsePath");

    [RelayCommand]
    private void BrowsePath()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description            = "Select AppData migration target folder",
            SelectedPath           = TargetPath,
            ShowNewFolderButton    = true,
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK &&
            !string.IsNullOrEmpty(dialog.SelectedPath))
        {
            TargetPath = dialog.SelectedPath;
            var root = Path.GetPathRoot(dialog.SelectedPath)?.TrimEnd('\\', '/');
            if (!string.IsNullOrEmpty(root))
                TargetDisk = root;
        }
    }
    
    /// <summary>
    /// Scan for AppData folders
    /// </summary>
    [RelayCommand]
    private async Task ScanAsync()
    {
        if (IsScanning)
            return;
        
        IsScanning = true;
        StatusMessage = _localizationService.GetString("Status.ScanningAppData");
        ClearAppDataItems();
        
        try
        {
            var roaming  = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local    = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var localLow = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData\\LocalLow");
            
            await ScanDirectoryAsync(roaming,  "Roaming");
            await ScanDirectoryAsync(local,    "Local");
            
            if (_fileSystem.DirectoryExists(localLow))
                await ScanDirectoryAsync(localLow, "LocalLow");
            
            // BUG-007: persist the size cache so subsequent scans benefit from cached values.
            await _sizeCache.SaveAsync();
            
            StatusMessage = _localizationService.GetString("Status.ScanComplete", AppDataItems.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan AppData");
            StatusMessage = _localizationService.GetString("Status.ScanFailed", ex.Message);
        }
        finally
        {
            IsScanning = false;
        }
    }
    
    private async Task ScanDirectoryAsync(string basePath, string type)
    {
        await Task.Run(() =>
        {
            try
            {
                foreach (var dir in _fileSystem.GetDirectories(basePath, "*", false))
                {
                    var sizeChecked = _sizeCache.TryGet(dir, out var cached);
                    var sizeBytes = sizeChecked ? cached.SizeBytes : 0;

                    var info = new AppDataInfo
                    {
                        Name = Path.GetFileName(dir),
                        Path = dir,
                        Type = type,
                        Status = sizeChecked
                            ? sizeBytes > 0 ? SoftwareStatus.Normal : SoftwareStatus.Empty
                            : SoftwareStatus.Suspicious,
                        SizeBytes = sizeBytes,
                        SizeChecked = sizeChecked
                    };
                    
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        AppDataItems.Add(info);
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scan directory: {Path}", basePath);
            }
        });
    }
    
    /// <summary>
    /// Migrate selected AppData folders
    /// </summary>
    [RelayCommand]
    private async Task MigrateAsync()
    {
        if (IsMigrating || SelectedItems.Count == 0)
            return;

        // BUG-008: refuse to migrate to the same drive as the source.
        var targetDriveRoot = Path.GetPathRoot(TargetPath)
            ?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            ?? string.Empty;

        var sameDriveItems = SelectedItems
            .Where(s => string.Equals(
                Path.GetPathRoot(s.Path)
                    ?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                targetDriveRoot,
                StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Name)
            .ToList();

        if (sameDriveItems.Count > 0)
        {
            StatusMessage = _localizationService.GetString(
                "Status.SameDriveError", string.Join(", ", sameDriveItems), targetDriveRoot);
            _logger.LogWarning(
                "AppData migration aborted: target drive {Drive} is the same as source for: {Items}",
                targetDriveRoot, string.Join(", ", sameDriveItems));
            return;
        }
        
        IsMigrating = true;
        StatusMessage = _localizationService.GetString("Status.Migrating");
        
        try
        {
            foreach (var appData in SelectedItems.ToList())
            {
                var request = new MigrationRequest
                {
                    Type           = MigrationType.AppData,
                    Name           = appData.Name,
                    SourcePath     = appData.Path,
                    TargetRootPath = TargetPath
                };
                
                var task   = await _migrationEngine.CreateTaskAsync(request);
                var result = await _migrationEngine.ExecuteAsync(task);
                
                if (result.Success)
                {
                    appData.Status = SoftwareStatus.Migrated;
                    _logger.LogInformation("Successfully migrated AppData: {Name}", appData.Name);
                }
                else
                {
                    _logger.LogError("Failed to migrate AppData {Name}: {Error}", appData.Name, result.ErrorMessage);
                    StatusMessage = _localizationService.GetString(
                        "Status.MigrationFailed", appData.Name, result.ErrorMessage ?? string.Empty);

                    // BUG-004: stop on first failure to avoid cascading half-migrations.
                    break;
                }
            }
            
            StatusMessage = _localizationService.GetString("Status.MigrationComplete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AppData migration failed");
            StatusMessage = _localizationService.GetString("Status.MigrationError", ex.Message);
        }
        finally
        {
            IsMigrating = false;
            ClearSelection();
        }
    }
    
    /// <summary>
    /// Check AppData folder size
    /// </summary>
    [RelayCommand]
    private async Task CheckSizeAsync(AppDataInfo? appData)
    {
        if (appData == null || appData.SizeChecked)
            return;
        
        try
        {
            // BUG-007: try the cache first; only measure from disk on a cache miss.
            long size;
            if (_sizeCache.TryGet(appData.Path, out var cached))
            {
                size = cached.SizeBytes;
            }
            else
            {
                size = await Task.Run(() => _fileSystem.GetDirectorySize(appData.Path));
                _sizeCache.Set(appData.Path, size);
                await _sizeCache.SaveAsync();
            }

            appData.SizeBytes   = size;
            appData.SizeChecked = true;
            appData.Status      = size > 0 ? SoftwareStatus.Normal : SoftwareStatus.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check AppData size: {Name}", appData.Name);
        }
    }

    partial void OnAppDataItemsChanging(ObservableCollection<AppDataInfo> value)
    {
        value.CollectionChanged -= AppDataItems_CollectionChanged;
        foreach (var item in value)
            item.PropertyChanged -= AppDataItem_PropertyChanged;
    }

    partial void OnAppDataItemsChanged(ObservableCollection<AppDataInfo> value)
    {
        value.CollectionChanged += AppDataItems_CollectionChanged;
        foreach (var item in value)
            AttachAppDataItem(item);

        RebuildSelectionFromItems();
    }

    private void AppDataItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isSynchronizingSelection)
            return;

        if (e.OldItems is not null)
        {
            foreach (AppDataInfo item in e.OldItems)
            {
                item.PropertyChanged -= AppDataItem_PropertyChanged;
                SelectedItems.Remove(item);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (AppDataInfo item in e.NewItems)
            {
                AttachAppDataItem(item);
                if (item.IsSelected && !SelectedItems.Contains(item))
                    SelectedItems.Add(item);
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
            RebuildSelectionFromItems();
    }

    private void AppDataItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not AppDataInfo item ||
            e.PropertyName != nameof(AppDataInfo.IsSelected) ||
            _isSynchronizingSelection)
            return;

        if (item.IsSelected)
        {
            if (!SelectedItems.Contains(item))
                SelectedItems.Add(item);
        }
        else
        {
            SelectedItems.Remove(item);
        }
    }

    private void AttachAppDataItem(AppDataInfo item)
    {
        item.PropertyChanged -= AppDataItem_PropertyChanged;
        item.PropertyChanged += AppDataItem_PropertyChanged;
    }

    private void ClearAppDataItems()
    {
        _isSynchronizingSelection = true;
        try
        {
            foreach (var item in AppDataItems)
            {
                item.PropertyChanged -= AppDataItem_PropertyChanged;
                item.IsSelected = false;
            }

            AppDataItems.Clear();
            SelectedItems.Clear();
        }
        finally
        {
            _isSynchronizingSelection = false;
        }
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
        finally
        {
            _isSynchronizingSelection = false;
        }
    }

    private void RebuildSelectionFromItems()
    {
        _isSynchronizingSelection = true;
        try
        {
            SelectedItems.Clear();
            foreach (var item in AppDataItems.Where(i => i.IsSelected))
                SelectedItems.Add(item);
        }
        finally
        {
            _isSynchronizingSelection = false;
        }
    }
}

/// <summary>
/// AppData folder information
/// </summary>
public partial class AppDataInfo : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _path = string.Empty;

    [ObservableProperty]
    private string _type = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SizeText))]
    private long _sizeBytes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SizeText))]
    private bool _sizeChecked;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SizeText))]
    private SoftwareStatus _status;
    
    public string SizeText
    {
        get
        {
            if (!SizeChecked)
                return "...";
            if (SizeBytes == 0)
                return "0 KB";
            if (SizeBytes < 1024)
                return $"{SizeBytes} B";
            if (SizeBytes < 1024 * 1024)
                return $"{Math.Max(1, SizeBytes / 1024)} KB";
            if (SizeBytes < 1024L * 1024 * 1024)
                return $"{SizeBytes / (1024 * 1024)} MB";
            return $"{SizeBytes / (1024L * 1024 * 1024):F1} GB";
        }
    }
}
