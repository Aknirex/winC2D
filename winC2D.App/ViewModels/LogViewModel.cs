using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using winC2D.Core.Services;
using winC2D.Infrastructure.Localization;

namespace winC2D.App.ViewModels;

/// <summary>
/// View model for migration log
/// </summary>
public partial class LogViewModel : ObservableObject
{
    private readonly IRollbackManager _rollbackManager;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<LogViewModel> _logger;
    
    [ObservableProperty]
    private ObservableCollection<MigrationLogEntry> _logEntries = new();
    
    [ObservableProperty]
    private MigrationLogEntry? _selectedEntry;
    
    [ObservableProperty]
    private string _filterText = string.Empty;
    
    [ObservableProperty]
    private bool _showSuccessful = true;
    
    [ObservableProperty]
    private bool _showFailed = true;
    
    [ObservableProperty]
    private bool _showRolledBack = true;
    
    public LogViewModel(
        IRollbackManager rollbackManager,
        ILocalizationService localizationService,
        ILogger<LogViewModel> logger)
    {
        _rollbackManager = rollbackManager;
        _localizationService = localizationService;
        _logger = logger;

        _localizationService.LanguageChanged += (_, _) => NotifyLocalizationChanged();
    }

    private void NotifyLocalizationChanged()
    {
        OnPropertyChanged(nameof(L_Header));
        OnPropertyChanged(nameof(L_Refresh));
        OnPropertyChanged(nameof(L_ShowSuccessful));
        OnPropertyChanged(nameof(L_ShowFailed));
        OnPropertyChanged(nameof(L_ShowRolledBack));
        OnPropertyChanged(nameof(L_ColTime));
        OnPropertyChanged(nameof(L_ColType));
        OnPropertyChanged(nameof(L_ColSource));
        OnPropertyChanged(nameof(L_ColTarget));
        OnPropertyChanged(nameof(L_ColStatus));
        OnPropertyChanged(nameof(L_RollbackSelected));
        OnPropertyChanged(nameof(L_ExportLog));
        OnPropertyChanged(nameof(L_ClearHistory));
    }

    // ── Localized labels ──────────────────────────────────────────────
    public string L_Header          => _localizationService.GetString("Log.Header");
    public string L_Refresh         => _localizationService.GetString("Log.Refresh");
    public string L_ShowSuccessful  => _localizationService.GetString("Log.ShowSuccessful");
    public string L_ShowFailed      => _localizationService.GetString("Log.ShowFailed");
    public string L_ShowRolledBack  => _localizationService.GetString("Log.ShowRolledBack");
    public string L_ColTime         => _localizationService.GetString("Log.ColTime");
    public string L_ColType         => _localizationService.GetString("Log.ColType");
    public string L_ColSource       => _localizationService.GetString("Log.ColSource");
    public string L_ColTarget       => _localizationService.GetString("Log.ColTarget");
    public string L_ColStatus       => _localizationService.GetString("Log.ColStatus");
    public string L_RollbackSelected => _localizationService.GetString("Log.RollbackSelected");
    public string L_ExportLog       => _localizationService.GetString("Log.ExportLog");
    public string L_ClearHistory    => _localizationService.GetString("Log.ClearHistory");
    
    /// <summary>
    /// Load log entries
    /// </summary>
    [RelayCommand]
    private async Task LoadLogsAsync()
    {
        try
        {
            var rollbackPoints = await _rollbackManager.GetAllRollbackPointsAsync();
            
            LogEntries.Clear();
            
            foreach (var point in rollbackPoints.OrderByDescending(p => p.CreatedAt))
            {
                var entry = new MigrationLogEntry
                {
                    Id = point.Id,
                    Time = point.CreatedAt,
                    Type = point.Type.ToString(),
                    SourcePath = point.SourcePath,
                    TargetPath = point.TargetPath,
                    Status = point.CanRollback ? "Completed" : "RolledBack"
                };
                
                LogEntries.Add(entry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load log entries");
        }
    }
    
    /// <summary>
    /// Rollback selected entry
    /// </summary>
    [RelayCommand]
    private async Task RollbackAsync()
    {
        if (SelectedEntry == null)
            return;
        
        try
        {
            var result = await _rollbackManager.RollbackAsync(SelectedEntry.Id);
            
            if (result.Success)
            {
                _logger.LogInformation("Rollback successful for {Id}", SelectedEntry.Id);
                SelectedEntry.Status = "RolledBack";
                OnPropertyChanged(nameof(SelectedEntry));
            }
            else
            {
                _logger.LogError("Rollback failed for {Id}: {Error}", SelectedEntry.Id, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rollback failed for {Id}", SelectedEntry?.Id);
        }
    }
    
    /// <summary>
    /// Export log to file
    /// </summary>
    [RelayCommand]
    private void ExportLog()
    {
        // TODO: Implement log export
        _logger.LogInformation("Exporting log...");
    }
    
    /// <summary>
    /// Clear log entries
    /// </summary>
    [RelayCommand]
    private void ClearLog()
    {
        LogEntries.Clear();
        _logger.LogInformation("Log cleared");
    }
}

/// <summary>
/// Migration log entry
/// </summary>
public class MigrationLogEntry
{
    public string Id { get; set; } = string.Empty;
    public DateTime Time { get; set; }
    public string Type { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}