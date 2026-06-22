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
    private static readonly TimeSpan ProgressSaveInterval = TimeSpan.FromSeconds(2);
    private const int ProgressSaveFileInterval = 500;

    private readonly ILogger<JsonMigrationTaskStore> _logger;
    private readonly string _storageDirectory;
    private readonly string _storageFilePath;
    private readonly object _gate = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };
    private readonly Dictionary<string, MigrationTask> _tasks = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (DateTime SavedAt, int Files)> _progressSaves = new(StringComparer.OrdinalIgnoreCase);
    private DateTime _loadedWriteTimeUtc;
    private long _loadedLength = -1;

    public JsonMigrationTaskStore(ILogger<JsonMigrationTaskStore> logger)
    {
        _logger = logger;
        _storageDirectory = PersistentStateFile.CreateStorageDirectory(logger, "tasks");
        _storageFilePath = Path.Combine(_storageDirectory, "migration_tasks.json");

        using var processLock = PersistentStateFile.AcquireProcessLock(_storageFilePath);
        LoadForRead(force: true);
    }

    public Task<MigrationTask?> GetAsync(string taskId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            using var processLock = PersistentStateFile.AcquireProcessLock(_storageFilePath, cancellationToken);
            LoadForRead(force: false);
            return Task.FromResult(_tasks.GetValueOrDefault(taskId));
        }
    }

    public Task<IReadOnlyList<MigrationTask>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            using var processLock = PersistentStateFile.AcquireProcessLock(_storageFilePath, cancellationToken);
            LoadForRead(force: false);
            return Task.FromResult((IReadOnlyList<MigrationTask>)_tasks.Values.ToList());
        }
    }

    public Task UpsertAsync(MigrationTask task, bool immediate = true, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            using var processLock = PersistentStateFile.AcquireProcessLock(_storageFilePath, cancellationToken);
            LoadFromDisk(force: true);
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

            using var processLock = PersistentStateFile.AcquireProcessLock(_storageFilePath, cancellationToken);
            task.LastHeartbeatAt = now;
            LoadFromDisk(force: true);
            task.SchemaVersion = MigrationTask.CurrentSchemaVersion;
            _tasks[task.Id] = task;
            SaveToDisk();
            _progressSaves[task.Id] = (now, task.CopiedFiles);
        }

        return Task.CompletedTask;
    }

    public Task RefreshHeartbeatAsync(string taskId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            using var processLock = PersistentStateFile.AcquireProcessLock(_storageFilePath, cancellationToken);
            LoadFromDisk(force: true);
            if (_tasks.TryGetValue(taskId, out var task))
            {
                task.LastHeartbeatAt = DateTime.UtcNow;
                SaveToDisk();
            }
        }

        return Task.CompletedTask;
    }

    public Task RequestPauseAsync(string taskId, CancellationToken cancellationToken = default) =>
        UpdateRequestAsync(taskId, t => t.PauseRequestedAt = DateTime.UtcNow, cancellationToken);

    public Task RequestResumeAsync(string taskId, CancellationToken cancellationToken = default) =>
        UpdateRequestAsync(taskId, t => t.ResumeRequestedAt = DateTime.UtcNow, cancellationToken);

    public Task RequestCancelAsync(string taskId, bool rollback, CancellationToken cancellationToken = default) =>
        UpdateRequestAsync(taskId, t =>
        {
            t.CancelRequestedAt = DateTime.UtcNow;
            t.CancelRollback = rollback;
        }, cancellationToken);

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
            using var processLock = PersistentStateFile.AcquireProcessLock(_storageFilePath, cancellationToken);
            LoadFromDisk(force: true);
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

    private Task UpdateRequestAsync(
        string taskId,
        Action<MigrationTask> update,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            using var processLock = PersistentStateFile.AcquireProcessLock(_storageFilePath, cancellationToken);
            LoadFromDisk(force: true);
            if (_tasks.TryGetValue(taskId, out var task))
            {
                update(task);
                SaveToDisk();
            }
        }

        return Task.CompletedTask;
    }

    private void LoadFromDisk(bool force)
    {
        if (!File.Exists(_storageFilePath))
        {
            _tasks.Clear();
            _loadedWriteTimeUtc = default;
            _loadedLength = -1;
            return;
        }

        var info = new FileInfo(_storageFilePath);
        if (!force && info.LastWriteTimeUtc == _loadedWriteTimeUtc && info.Length == _loadedLength)
            return;

        // Do not mutate the last known-good state until the entire document parses.
        var json = File.ReadAllText(_storageFilePath);
        var document = JsonSerializer.Deserialize<MigrationTaskStateDocument>(json, _jsonOptions)
            ?? throw new InvalidDataException($"Task state file '{_storageFilePath}' is empty or invalid.");
        var loaded = new Dictionary<string, MigrationTask>(StringComparer.OrdinalIgnoreCase);
        foreach (var task in document.Tasks)
        {
            if (task.SchemaVersion == 0)
                task.SchemaVersion = 1;
            loaded[task.Id] = task;
        }

        _tasks.Clear();
        foreach (var pair in loaded)
            _tasks[pair.Key] = pair.Value;
        _loadedWriteTimeUtc = info.LastWriteTimeUtc;
        _loadedLength = info.Length;
    }

    private void LoadForRead(bool force)
    {
        try
        {
            LoadFromDisk(force);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to refresh migration tasks from {Path}; using last known-good state",
                _storageFilePath);
        }
    }

    private void SaveToDisk()
    {
        Directory.CreateDirectory(_storageDirectory);
        var document = new MigrationTaskStateDocument
        {
            SchemaVersion = SchemaVersion,
            Tasks = _tasks.Values.OrderBy(t => t.CreatedAt).ToList()
        };
        var json = JsonSerializer.Serialize(document, _jsonOptions);
        PersistentStateFile.AtomicWriteAllText(_storageFilePath, json);
        var info = new FileInfo(_storageFilePath);
        _loadedWriteTimeUtc = info.LastWriteTimeUtc;
        _loadedLength = info.Length;
    }

    private static bool IsTerminalState(MigrationState state) => state is
        MigrationState.Completed or MigrationState.Failed or MigrationState.RolledBack or
        MigrationState.PartialRollback or MigrationState.Cancelled;

    private sealed class MigrationTaskStateDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public List<MigrationTask> Tasks { get; set; } = new();
    }
}
