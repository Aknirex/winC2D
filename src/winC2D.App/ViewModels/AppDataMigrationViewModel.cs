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
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<AppDataMigrationViewModel> _logger;
    
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
        ILocalizationService localizationService,
        ILogger<AppDataMigrationViewModel> logger)
    {
        _fileSystem = fileSystem;
        _migrationEngine = migrationEngine;
        _localizationService = localizationService;
        _logger = logger;
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
        AppDataItems.Clear();
        
        try
        {
            var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var localLow = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData\\LocalLow");
            
            await ScanDirectoryAsync(roaming, "Roaming");
            await ScanDirectoryAsync(local, "Local");
            
            if (_fileSystem.DirectoryExists(localLow))
            {
                await ScanDirectoryAsync(localLow, "LocalLow");
            }
            
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
                    var info = new AppDataInfo
                    {
                        Name = Path.GetFileName(dir),
                        Path = dir,
                        Type = type,
                        Status = SoftwareStatus.Suspicious,
                        SizeBytes = 0,
                        SizeChecked = false
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
        
        IsMigrating = true;
        StatusMessage = _localizationService.GetString("Status.Migrating");
        
        try
        {
            foreach (var appData in SelectedItems.ToList())
            {
                var request = new MigrationRequest
                {
                    Type = MigrationType.AppData,
                    Name = appData.Name,
                    SourcePath = appData.Path,
                    TargetRootPath = TargetPath
                };
                
                var task = await _migrationEngine.CreateTaskAsync(request);
                var result = await _migrationEngine.ExecuteAsync(task);
                
                if (result.Success)
                {
                    appData.Status = SoftwareStatus.Migrated;
                    _logger.LogInformation("Successfully migrated AppData: {Name}", appData.Name);
                }
                else
                {
                    _logger.LogError("Failed to migrate AppData {Name}: {Error}", appData.Name, result.ErrorMessage);
                    StatusMessage = _localizationService.GetString("Status.MigrationFailed", appData.Name, result.ErrorMessage);
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
            SelectedItems.Clear();
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
            var size = await Task.Run(() => _fileSystem.GetDirectorySize(appData.Path));
            appData.SizeBytes = size;
            appData.SizeChecked = true;
            appData.Status = size > 0 ? SoftwareStatus.Normal : SoftwareStatus.Empty;
            
            OnPropertyChanged(nameof(appData.SizeText));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check AppData size: {Name}", appData.Name);
        }
    }
}

/// <summary>
/// AppData folder information
/// </summary>
public class AppDataInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public bool SizeChecked { get; set; }
    public SoftwareStatus Status { get; set; }
    
    public string SizeText
    {
        get
        {
            if (!SizeChecked)
                return "...";
            if (SizeBytes == 0)
                return "0 KB";
            if (SizeBytes < 1024 * 1024)
                return $"{SizeBytes / 1024} KB";
            return $"{SizeBytes / (1024 * 1024)} MB";
        }
    }
}