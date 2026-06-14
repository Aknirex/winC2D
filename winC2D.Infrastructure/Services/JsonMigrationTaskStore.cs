using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using winC2D.Core.Models;
using winC2D.Core.Services;

namespace winC2D.Infrastructure.Services;

public sealed class JsonMigrationTaskStore : IMigrationTaskStore
{
    private const int SchemaVersion = 2;
    private static readonly TimeSpan PendingStaleAfter = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan HeartbeatStaleAfter = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ProgressSaveInterval = TimeSpan.FromMilliseconds(500);
    private const int ProgressSaveFileInterval = 100;

    private readonly ILogger<JsonMigrationTaskStore> _logger;
    private readonly string _storageDirectory;
    private readonly string _storageFilePath;
    private readonly object _gate = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };
    private readonly Dictionary<string, MigrationTask> _tasks = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (DateTime SavedAt, int Files)> _progressSaves = new(StringComparer.OrdinalIgnoreCase);

    public JsonMigrationTaskStore(ILogger<JsonMigrationTaskStore> logger)
    {
        _logger = logger;
        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (string.IsNullOrWhiteSpace(localAppData))
            localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _storageDirectory = Path.Combine(localAppData, "winC2D", "tasks");
        Directory.CreateDirectory(_storageDirectory);
        _storageFilePath = Path.Combine(_storageDirectory, "migration_tasks.json");
        LoadFromDisk();
    }

    public Task<MigrationTask?> GetAsync(string taskId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            LoadFromDisk();
            return Task.FromResult(_tasks.GetValueOrDefault(taskId));
        }
    }

    public Task<IReadOnlyList<MigrationTask>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            LoadFromDisk();
            return Task.FromResult((IReadOnlyList<MigrationTask>)_tasks.Values.ToList());
        }
    }

    public Task UpsertAsync(MigrationTask task, bool immediate = true, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            LoadFromDisk();
            task.SchemaVersion = MigrationTask.CurrentSchemaVersion;
            _tasks[task.Id] = task;
            if (immediate)
                SaveToDisk();
        }

        return Task.CompletedTask;
    }

    public Task SaveProgressAsync(MigrationTask task, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var now = DateTime.UtcNow;
            _progressSaves.TryGetValue(task.Id, out var last);
            var shouldSave = last.SavedAt == default ||
                             now - last.SavedAt >= ProgressSaveInterval ||
                             task.CopiedFiles - last.Files >= ProgressSaveFileInterval ||
                             IsTerminalState(task.State);

            if (!shouldSave)
                return Task.CompletedTask;

            task.LastHeartbeatAt = now;
            LoadFromDisk();
            task.SchemaVersion = MigrationTask.CurrentSchemaVersion;
            _tasks[task.Id] = task;
            _progressSaves[task.Id] = (now, task.CopiedFiles);
            SaveToDisk();
        }

        return Task.CompletedTask;
    }

    public Task RefreshHeartbeatAsync(string taskId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            LoadFromDisk();
            if (_tasks.TryGetValue(taskId, out var task))
            {
                task.LastHeartbeatAt = DateTime.UtcNow;
                SaveToDisk();
            }
        }

        return Task.CompletedTask;
    }

    public Task RequestPauseAsync(string taskId, CancellationToken cancellationToken = default)
    {
        return UpdateRequestAsync(taskId, t => t.PauseRequestedAt = DateTime.UtcNow);
    }

    public Task RequestResumeAsync(string taskId, CancellationToken cancellationToken = default)
    {
        return UpdateRequestAsync(taskId, t => t.ResumeRequestedAt = DateTime.UtcNow);
    }

    public Task RequestCancelAsync(string taskId, bool rollback, CancellationToken cancellationToken = default)
    {
        return UpdateRequestAsync(taskId, t =>
        {
            t.CancelRequestedAt = DateTime.UtcNow;
            t.CancelRollback = rollback;
        });
    }

    public bool IsStale(MigrationTask task, DateTime utcNow)
    {
        if (IsTerminalState(task.State))
            return false;

        if (task.State == MigrationState.Pending)
            return utcNow - task.CreatedAt > PendingStaleAfter && !IsWorkerAlive(task);

        if (task.LastHeartbeatAt is null)
            return !IsWorkerAlive(task);

        return utcNow - task.LastHeartbeatAt.Value > HeartbeatStaleAfter && !IsWorkerAlive(task);
    }

    public string GetStaleReason(MigrationTask task, DateTime utcNow)
    {
        if (!IsStale(task, utcNow))
            return string.Empty;

        if (task.WorkerProcessId is null)
        {
            return task.State == MigrationState.Pending
                ? "Task was created but no worker process id was recorded. It likely predates worker tracking, failed before launch metadata was saved, or was created by a test/old build."
                : "Task has no worker process id, so no running process can continue it.";
        }

        if (task.LastHeartbeatAt is null)
            return $"Worker process {task.WorkerProcessId} is no longer running and never wrote a heartbeat.";

        var ageSeconds = Math.Max(0, (int)(utcNow - task.LastHeartbeatAt.Value).TotalSeconds);
        return $"Worker process {task.WorkerProcessId} is no longer running. Last heartbeat was {ageSeconds} seconds ago.";
    }

    public bool IsWorkerAlive(MigrationTask task)
    {
        if (task.WorkerProcessId is null)
            return false;

        try
        {
            var process = Process.GetProcessById(task.WorkerProcessId.Value);
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }

    public Task<int> CleanupAsync(TaskCleanupOptions options, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            LoadFromDisk();
            var now = DateTime.UtcNow;
            var cutoff = now.AddDays(-Math.Max(0, options.OlderThanDays));
            var remove = _tasks.Values
                .Where(t => t.CreatedAt < cutoff)
                .Where(t => options.State == TaskCleanupState.Stale ? IsStale(t, now) : IsTerminalState(t.State))
                .Select(t => t.Id)
                .ToList();

            foreach (var id in remove)
                _tasks.Remove(id);

            if (remove.Count > 0)
                SaveToDisk();

            return Task.FromResult(remove.Count);
        }
    }

    private Task UpdateRequestAsync(string taskId, Action<MigrationTask> update)
    {
        lock (_gate)
        {
            LoadFromDisk();
            if (_tasks.TryGetValue(taskId, out var task))
            {
                update(task);
                SaveToDisk();
            }
        }

        return Task.CompletedTask;
    }

    private void LoadFromDisk()
    {
        try
        {
            _tasks.Clear();
            if (!File.Exists(_storageFilePath))
                return;

            var json = File.ReadAllText(_storageFilePath);
            var document = JsonSerializer.Deserialize<MigrationTaskStateDocument>(json, _jsonOptions);
            if (document?.Tasks is null)
                return;

            foreach (var task in document.Tasks)
            {
                if (task.SchemaVersion == 0)
                    task.SchemaVersion = 1;
                _tasks[task.Id] = task;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load migration tasks from {Path}", _storageFilePath);
        }
    }

    private void SaveToDisk()
    {
        try
        {
            Directory.CreateDirectory(_storageDirectory);
            var document = new MigrationTaskStateDocument
            {
                SchemaVersion = SchemaVersion,
                Tasks = _tasks.Values.OrderBy(t => t.CreatedAt).ToList()
            };
            var json = JsonSerializer.Serialize(document, _jsonOptions);
            var tmpPath = _storageFilePath + ".tmp";
            File.WriteAllText(tmpPath, json);
            File.Move(tmpPath, _storageFilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist migration tasks to {Path}", _storageFilePath);
        }
    }

    private static bool IsTerminalState(MigrationState state) => state is
        MigrationState.Completed or
        MigrationState.Failed or
        MigrationState.RolledBack or
        MigrationState.PartialRollback or
        MigrationState.Cancelled;

    private sealed class MigrationTaskStateDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public List<MigrationTask> Tasks { get; set; } = new();
    }
}
