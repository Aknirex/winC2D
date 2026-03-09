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
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<SoftwareMigrationViewModel> _logger;
    
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
    
    public SoftwareMigrationViewModel(
        ISoftwareScanner softwareScanner,
        IMigrationEngine migrationEngine,
        ILocalizationService localizationService,
        ILogger<SoftwareMigrationViewModel> logger)
    {
        _softwareScanner = softwareScanner;
        _migrationEngine = migrationEngine;
        _localizationService = localizationService;
        _logger = logger;
        
        // Subscribe to migration engine events
        _migrationEngine.ProgressChanged += OnMigrationProgressChanged;
        _migrationEngine.ErrorOccurred += OnMigrationError;
        _migrationEngine.StateChanged += OnMigrationStateChanged;
    }
    
    /// <summary>
    /// Available disk drives
    /// </summary>
    public ObservableCollection<string> AvailableDrives { get; } = new();
    
    /// <summary>
    /// Scan for installed software
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
        
        try
        {
            _softwareScanner.ProgressChanged += OnScanProgressChanged;
            
            var software = await _softwareScanner.ScanAsync();
            
            foreach (var item in software)
            {
                SoftwareItems.Add(item);
            }
            
            StatusMessage = _localizationService.GetString("Status.ScanComplete", SoftwareItems.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan software");
            StatusMessage = _localizationService.GetString("Status.ScanFailed", ex.Message);
        }
        finally
        {
            _softwareScanner.ProgressChanged -= OnScanProgressChanged;
            IsScanning = false;
        }
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
                    StatusMessage = _localizationService.GetString("Status.MigrationFailed", software.Name, result.ErrorMessage);
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