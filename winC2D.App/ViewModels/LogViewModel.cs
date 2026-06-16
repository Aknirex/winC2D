using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using winC2D.Core.Models;
using winC2D.Core.Services;
using winC2D.Infrastructure.Localization;

namespace winC2D.App.ViewModels;

/// <summary>
/// View model for migration task history and rollback.
/// </summary>
public partial class LogViewModel : ObservableObject
{
    private readonly IMigrationEngine _migrationEngine;
    private readonly IMigrationTaskStore _taskStore;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<LogViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<MigrationLogEntry> _logEntries = new();

    [ObservableProperty]
    private MigrationLogEntry? _selectedEntry;

    [ObservableProperty]
    private bool _showSuccessful = true;

    [ObservableProperty]
    private bool _showFailed = true;

    [ObservableProperty]
    private bool _showRolledBack = true;

    [ObservableProperty]
    private bool _showRunning = true;

    [ObservableProperty]
    private bool _showStale = true;

    public LogViewModel(
        IMigrationEngine migrationEngine,
        IMigrationTaskStore taskStore,
        ILocalizationService localizationService,
        ILogger<LogViewModel> logger)
    {
        _migrationEngine = migrationEngine;
        _taskStore = taskStore;
        _localizationService = localizationService;
        _logger = logger;

        EntriesView = CollectionViewSource.GetDefaultView(LogEntries);
        EntriesView.Filter = FilterEntry;

        _localizationService.LanguageChanged += (_, _) => NotifyLocalizationChanged();
    }

    public ICollectionView EntriesView { get; }

    private void NotifyLocalizationChanged()
    {
        OnPropertyChanged(nameof(L_Header));
        OnPropertyChanged(nameof(L_Refresh));
        OnPropertyChanged(nameof(L_ShowSuccessful));
        OnPropertyChanged(nameof(L_ShowFailed));
        OnPropertyChanged(nameof(L_ShowRolledBack));
        OnPropertyChanged(nameof(L_ShowRunning));
        OnPropertyChanged(nameof(L_ShowStale));
        OnPropertyChanged(nameof(L_ColTime));
        OnPropertyChanged(nameof(L_ColName));
        OnPropertyChanged(nameof(L_ColSource));
        OnPropertyChanged(nameof(L_ColTarget));
        OnPropertyChanged(nameof(L_ColStatus));
        OnPropertyChanged(nameof(L_ColProgress));
        OnPropertyChanged(nameof(L_ColFiles));
        OnPropertyChanged(nameof(L_ColMessage));
        OnPropertyChanged(nameof(L_RollbackSelected));
        OnPropertyChanged(nameof(L_ExportLog));
        OnPropertyChanged(nameof(L_ClearHistory));
        OnPropertyChanged(nameof(L_ClearStale));
    }

    public string L_Header           => _localizationService.GetString("Log.Header");
    public string L_Refresh          => _localizationService.GetString("Log.Refresh");
    public string L_ShowSuccessful   => _localizationService.GetString("Log.ShowSuccessful");
    public string L_ShowFailed       => _localizationService.GetString("Log.ShowFailed");
    public string L_ShowRolledBack   => _localizationService.GetString("Log.ShowRolledBack");
    public string L_ShowRunning      => _localizationService.GetString("Log.ShowRunning");
    public string L_ShowStale        => _localizationService.GetString("Log.ShowStale");
    public string L_ColTime          => _localizationService.GetString("Log.ColTime");
    public string L_ColName          => _localizationService.GetString("Log.ColName");
    public string L_ColSource        => _localizationService.GetString("Log.ColSource");
    public string L_ColTarget        => _localizationService.GetString("Log.ColTarget");
    public string L_ColStatus        => _localizationService.GetString("Log.ColStatus");
    public string L_ColProgress      => _localizationService.GetString("Log.ColProgress");
    public string L_ColFiles         => _localizationService.GetString("Log.ColFiles");
    public string L_ColMessage       => _localizationService.GetString("Log.ColMessage");
    public string L_RollbackSelected => _localizationService.GetString("Log.RollbackSelected");
    public string L_ExportLog        => _localizationService.GetString("Log.ExportLog");
    public string L_ClearHistory     => _localizationService.GetString("Log.ClearHistory");
    public string L_ClearStale       => _localizationService.GetString("Log.ClearStale");

    [RelayCommand]
    public async Task LoadLogsAsync()
    {
        try
        {
            var tasks = await _migrationEngine.GetAllTasksAsync();

            LogEntries.Clear();
            var now = DateTime.UtcNow;
            foreach (var task in tasks.OrderByDescending(t => t.StartedAt ?? t.CreatedAt))
            {
                var isStale = _taskStore.IsStale(task, now);
                LogEntries.Add(MigrationLogEntry.FromTask(
                    task,
                    isStale,
                    isStale ? _taskStore.GetStaleReason(task, now) : null,
                    _localizationService));
            }

            EntriesView.Refresh();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load migration history");
        }
    }

    [RelayCommand(CanExecute = nameof(CanRollbackSelected))]
    private async Task RollbackAsync()
    {
        if (SelectedEntry == null)
            return;

        try
        {
            var result = await _migrationEngine.RollbackAsync(SelectedEntry.Id);
            if (result.Success)
            {
                _logger.LogInformation("Rollback completed for task {Id}", SelectedEntry.Id);
                await LoadLogsAsync();
            }
            else
            {
                SelectedEntry.Message = result.ErrorMessage ?? "Rollback failed.";
                _logger.LogError("Rollback failed for task {Id}: {Error}", SelectedEntry.Id, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rollback failed for task {Id}", SelectedEntry?.Id);
        }
    }

    private bool CanRollbackSelected()
    {
        return SelectedEntry?.CanRollback == true;
    }

    partial void OnSelectedEntryChanged(MigrationLogEntry? oldValue, MigrationLogEntry? newValue)
    {
        if (oldValue is not null)
            oldValue.IsSelected = false;
        if (newValue is not null)
            newValue.IsSelected = true;
        RollbackCommand.NotifyCanExecuteChanged();
    }

    partial void OnShowSuccessfulChanged(bool value) => EntriesView.Refresh();
    partial void OnShowFailedChanged(bool value) => EntriesView.Refresh();
    partial void OnShowRolledBackChanged(bool value) => EntriesView.Refresh();
    partial void OnShowRunningChanged(bool value) => EntriesView.Refresh();
    partial void OnShowStaleChanged(bool value) => EntriesView.Refresh();

    [RelayCommand]
    private void ExportLog()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt",
            FileName = $"winC2D-history-{DateTime.Now:yyyyMMdd-HHmmss}.csv"
        };

        if (dialog.ShowDialog() != true)
            return;

        var lines = EntriesView.Cast<MigrationLogEntry>()
            .Select(e => string.Join(",",
                Csv(e.Time.ToString("yyyy-MM-dd HH:mm:ss")),
                Csv(e.Id),
                Csv(e.Name),
                Csv(e.Status),
                Csv(e.Progress),
                Csv(e.Files),
                Csv(e.SourcePath),
                Csv(e.TargetPath),
                Csv(e.Message),
                Csv(e.WorkerLogPath)));

        File.WriteAllLines(dialog.FileName, new[]
        {
            "Time,TaskId,Name,Status,Progress,Files,Source,Target,Message,WorkerLog"
        }.Concat(lines));
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogEntries.Clear();
        EntriesView.Refresh();
    }

    [RelayCommand]
    private async Task ClearStaleAsync()
    {
        await _migrationEngine.CleanupTasksAsync(new TaskCleanupOptions
        {
            State = TaskCleanupState.Stale,
            OlderThanDays = 0
        });
        await LoadLogsAsync();
    }

    private bool FilterEntry(object item)
    {
        if (item is not MigrationLogEntry entry)
            return false;

        return entry.Category switch
        {
            MigrationLogCategory.Success => ShowSuccessful,
            MigrationLogCategory.Failed => ShowFailed,
            MigrationLogCategory.RolledBack => ShowRolledBack,
            MigrationLogCategory.Running => ShowRunning,
            MigrationLogCategory.Stale => ShowStale,
            _ => true
        };
    }

    private static string Csv(string? value)
    {
        value ??= string.Empty;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}

public enum MigrationLogCategory
{
    Running,
    Success,
    Failed,
    RolledBack,
    Stale
}

public partial class MigrationLogEntry : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public DateTime Time { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Progress { get; set; } = string.Empty;
    public string Files { get; set; } = string.Empty;
    public bool CanRollback { get; set; }
    public MigrationLogCategory Category { get; set; }
    public string? WorkerLogPath { get; set; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string _message = string.Empty;

    public static MigrationLogEntry FromTask(MigrationTask task, bool isStale, string? staleReason, ILocalizationService localization)
    {
        var category = isStale
            ? MigrationLogCategory.Stale
            : task.State switch
        {
            MigrationState.Completed => MigrationLogCategory.Success,
            MigrationState.RolledBack or MigrationState.PartialRollback => MigrationLogCategory.RolledBack,
            MigrationState.Failed or MigrationState.Cancelled => MigrationLogCategory.Failed,
            _ => MigrationLogCategory.Running
        };

        var status = isStale
            ? localization.GetString("Log.Status.Stale")
            : localization.GetString($"Log.Status.{task.State}");

        return new MigrationLogEntry
        {
            Id = task.Id,
            Time = task.StartedAt ?? task.CreatedAt,
            Name = string.IsNullOrWhiteSpace(task.Name) ? Path.GetFileName(task.SourcePath) : task.Name,
            SourcePath = task.SourcePath,
            TargetPath = task.TargetPath,
            Status = status,
            Progress = $"{task.ProgressPercent}%",
            Files = task.TotalFiles > 0 ? $"{task.CopiedFiles}/{task.TotalFiles}" : task.CopiedFiles.ToString(),
            Message = BuildMessage(task, isStale, staleReason, localization),
            WorkerLogPath = task.WorkerLogPath,
            CanRollback = task.State == MigrationState.Completed && task.RollbackPoint?.CanRollback == true,
            Category = category
        };
    }

    private static string BuildMessage(MigrationTask task, bool isStale, string? staleReason, ILocalizationService localization)
    {
        if (!string.IsNullOrWhiteSpace(task.ErrorMessage))
            return task.ErrorMessage;

        if (isStale)
        {
            var prefix = localization.GetString("Log.Message.Stale");
            return string.IsNullOrWhiteSpace(staleReason) ? prefix : $"{prefix} {staleReason}";
        }

        return task.State switch
        {
            MigrationState.Pending => localization.GetString("Log.Message.Pending"),
            MigrationState.Preparing => localization.GetString("Log.Message.Preparing"),
            MigrationState.Copying => string.IsNullOrWhiteSpace(task.CurrentFile)
                ? localization.GetString("Log.Message.Copying")
                : localization.GetString("Log.Message.CopyingFile", task.CurrentFile),
            MigrationState.CreatingSymlink => localization.GetString("Log.Message.CreatingSymlink"),
            MigrationState.CleaningUp => localization.GetString("Log.Message.CleaningUp"),
            MigrationState.Completed => localization.GetString("Log.Message.Completed"),
            MigrationState.RolledBack => localization.GetString("Log.Message.RolledBack"),
            MigrationState.PartialRollback => localization.GetString("Log.Message.PartialRollback"),
            MigrationState.Cancelled => localization.GetString("Log.Message.Cancelled"),
            MigrationState.Paused => localization.GetString("Log.Message.Paused"),
            _ => string.Empty
        };
    }
}
