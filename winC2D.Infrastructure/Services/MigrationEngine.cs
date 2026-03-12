using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using winC2D.Core.Services;
using winC2D.Core.Models;
using winC2D.Core.Events;
using winC2D.Core.FileSystem;

namespace winC2D.Infrastructure.Services;

/// <summary>
/// Implementation of the migration engine
/// </summary>
public class MigrationEngine : IMigrationEngine
{
    private readonly IFileSystem _fileSystem;
    private readonly ISymlinkManager _symlinkManager;
    private readonly IRollbackManager _rollbackManager;
    private readonly ILogger<MigrationEngine> _logger;
    private readonly string _storageDirectory;
    private readonly string _storageFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false
    };
    
    private readonly ConcurrentDictionary<string, MigrationTask> _tasks = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens = new();
    
    // Per-task pause gates: Set = running, Reset = paused.
    private readonly ConcurrentDictionary<string, ManualResetEventSlim> _pauseGates = new();
    
    public event EventHandler<MigrationProgressEventArgs>? ProgressChanged;
    public event EventHandler<MigrationErrorEventArgs>? ErrorOccurred;
    public event EventHandler<MigrationTask>? StateChanged;
    
    public MigrationEngine(
        IFileSystem fileSystem,
        ISymlinkManager symlinkManager,
        IRollbackManager rollbackManager,
        ILogger<MigrationEngine> logger)
    {
        _fileSystem = fileSystem;
        _symlinkManager = symlinkManager;
        _rollbackManager = rollbackManager;
        _logger = logger;

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _storageDirectory = Path.Combine(localAppData, "winC2D", "tasks");
        Directory.CreateDirectory(_storageDirectory);
        _storageFilePath = Path.Combine(_storageDirectory, "migration_tasks.json");

        LoadPersistedTasks();
    }
    
    /// <inheritdoc/>
    public async Task<MigrationTask> CreateTaskAsync(MigrationRequest request, CancellationToken cancellationToken = default)
    {
        var task = new MigrationTask
        {
            Id = Guid.NewGuid().ToString(),
            Type = request.Type,
            Name = request.Name,
            SourcePath = request.SourcePath,
            TargetPath = DetermineTargetPath(request),
            State = MigrationState.Pending,
            CreatedAt = DateTime.UtcNow
        };
        
        // Calculate total size
        task.TotalBytes = await Task.Run(() => _fileSystem.GetDirectorySize(request.SourcePath, cancellationToken), cancellationToken);
        task.TotalFiles = await Task.Run(() => _fileSystem.GetFiles(request.SourcePath, "*", true).Count(), cancellationToken);
        
        _tasks[task.Id] = task;
        SaveTasks();
        
        _logger.LogInformation("Created migration task {TaskId} for {Name}: {SourcePath} -> {TargetPath}", 
            task.Id, task.Name, task.SourcePath, task.TargetPath);
        
        return task;
    }
    
    /// <inheritdoc/>
    public Task<MigrationTask?> GetTaskAsync(string taskId)
    {
        _tasks.TryGetValue(taskId, out var task);
        return Task.FromResult(task);
    }
    
    /// <inheritdoc/>
    public Task<IEnumerable<MigrationTask>> GetAllTasksAsync()
    {
        return Task.FromResult(_tasks.Values.AsEnumerable());
    }
    
    /// <inheritdoc/>
    public async Task<MigrationResult> ExecuteAsync(MigrationTask task, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _cancellationTokens[task.Id] = cts;
        
        // Create (or reuse) a pause gate for this task — initially signalled (running).
        var pauseGate = _pauseGates.GetOrAdd(task.Id, _ => new ManualResetEventSlim(true));
        pauseGate.Set(); // Ensure the gate is open when we start/resume.
        
        try
        {
            task.StartedAt = DateTime.UtcNow;
            task.State = MigrationState.Preparing;
            SaveTasks();
            OnStateChanged(task);
            
            // Validate source
            if (!_fileSystem.DirectoryExists(task.SourcePath))
            {
                return await FailTaskAsync(task, $"Source directory not found: {task.SourcePath}", null, stopwatch.Elapsed);
            }
            
            // Check if target already exists
            if (_fileSystem.DirectoryExists(task.TargetPath))
            {
                return await FailTaskAsync(task, $"Target directory already exists: {task.TargetPath}", null, stopwatch.Elapsed);
            }
            
            // Create rollback point
            task.RollbackPoint = await _rollbackManager.CreateRollbackPointAsync(task);
            SaveTasks();
            
            // Step 1: Rename source directory (atomic operation to detect locked files)
            task.State = MigrationState.Copying;
            SaveTasks();
            OnStateChanged(task);
            
            var backupPath = task.SourcePath + "_migrating_" + Guid.NewGuid().ToString("N");
            task.BackupPath = backupPath;
            SaveTasks();
            
            _fileSystem.MoveDirectory(task.SourcePath, backupPath);
            await _rollbackManager.RecordStepAsync(task.RollbackPoint.Id, CompletedStep.SourceRenamed);
            
            // Persist the backup path on the rollback point so that RollbackSourceRenamedAsync
            // can locate the backup directory if a rollback is needed.
            await _rollbackManager.SetBackupPathAsync(task.RollbackPoint.Id, backupPath);
            SaveTasks();
            
            // Step 2: Copy files to target
            await CopyDirectoryAsync(task, backupPath, task.TargetPath, cts.Token);
            
            if (cts.Token.IsCancellationRequested)
            {
                return await CancelAndRollbackAsync(task, stopwatch.Elapsed);
            }
            
            await _rollbackManager.RecordStepAsync(task.RollbackPoint.Id, CompletedStep.FilesCopied);
            
            // Step 3: Create symbolic link
            task.State = MigrationState.CreatingSymlink;
            SaveTasks();
            OnStateChanged(task);
            
            var symlinkCreated = await _symlinkManager.CreateDirectorySymlinkAsync(task.SourcePath, task.TargetPath);
            if (!symlinkCreated)
            {
                return await FailAndRollbackAsync(task, "Failed to create symbolic link", null, stopwatch.Elapsed);
            }
            
            await _rollbackManager.RecordStepAsync(task.RollbackPoint.Id, CompletedStep.SymlinkCreated);
            
            // Step 4: Delete backup directory
            task.State = MigrationState.CleaningUp;
            SaveTasks();
            OnStateChanged(task);
            
            _fileSystem.DeleteDirectory(backupPath, true);
            await _rollbackManager.RecordStepAsync(task.RollbackPoint.Id, CompletedStep.BackupDeleted);
            
            // Success!
            task.State = MigrationState.Completed;
            task.CompletedAt = DateTime.UtcNow;
            SaveTasks();
            OnStateChanged(task);
            
            // Clean up rollback point
            await _rollbackManager.DeleteRollbackPointAsync(task.RollbackPoint.Id);
            
            stopwatch.Stop();
            
            _logger.LogInformation("Migration task {TaskId} completed successfully in {Duration}", 
                task.Id, stopwatch.Elapsed);
            
            return MigrationResult.Succeeded(task, stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            return await CancelAndRollbackAsync(task, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration task {TaskId} failed: {Message}", task.Id, ex.Message);
            return await FailAndRollbackAsync(task, ex.Message, ex, stopwatch.Elapsed);
        }
        finally
        {
            _cancellationTokens.TryRemove(task.Id, out _);
            // Clean up the pause gate only when the task has reached a terminal state,
            // to avoid leaking the ManualResetEventSlim handle.
            if (task.State is MigrationState.Completed
                           or MigrationState.Failed
                           or MigrationState.RolledBack
                           or MigrationState.PartialRollback
                           or MigrationState.Cancelled)
            {
                if (_pauseGates.TryRemove(task.Id, out var gate))
                    gate.Dispose();
            }
        }
    }
    
    /// <inheritdoc/>
    public async IAsyncEnumerable<MigrationResult> ExecuteBatchAsync(IEnumerable<MigrationTask> tasks, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var task in tasks)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;
            
            var result = await ExecuteAsync(task, cancellationToken);
            yield return result;
        }
    }
    
    /// <inheritdoc/>
    public Task PauseAsync(string taskId)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
            return Task.CompletedTask;
        
        // Only pause tasks that are actively running.
        if (task.State is not (MigrationState.Copying or MigrationState.CreatingSymlink or MigrationState.CleaningUp or MigrationState.Preparing))
            return Task.CompletedTask;
        
        // Blocking the pause gate will cause the copy loop to wait at the next
        // checkpoint without triggering cancellation or rollback.
        if (_pauseGates.TryGetValue(taskId, out var gate))
            gate.Reset();
        
        task.State = MigrationState.Paused;
        SaveTasks();
        OnStateChanged(task);
        
        _logger.LogInformation("Migration task {TaskId} paused", taskId);
        return Task.CompletedTask;
    }
    
    /// <inheritdoc/>
    public Task<MigrationResult> ResumeAsync(string taskId, CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
            return Task.FromResult(MigrationResult.Failed(null!, "Task not found", null));
        
        if (task.State != MigrationState.Paused)
            return Task.FromResult(MigrationResult.Failed(task, "Task is not paused", null));
        
        // Signal the pause gate — the copy loop will resume from where it stopped.
        if (_pauseGates.TryGetValue(taskId, out var gate))
            gate.Set();
        
        task.State = MigrationState.Copying;
        SaveTasks();
        OnStateChanged(task);
        
        _logger.LogInformation("Migration task {TaskId} resumed", taskId);
        
        // The background execution loop is already running and waiting on the gate;
        // there is nothing more to do here — return a "running" placeholder result.
        return Task.FromResult(new MigrationResult
        {
            Success = true,
            TaskId = taskId,
            FinalState = MigrationState.Copying
        });
    }
    
    /// <inheritdoc/>
    public async Task<MigrationResult> CancelAsync(string taskId, bool rollback = true, CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
        {
            return MigrationResult.Failed(null!, "Task not found", null);
        }
        
        // Cancel the operation
        if (_cancellationTokens.TryGetValue(taskId, out var cts))
        {
            cts.Cancel();
        }
        
        task.State = MigrationState.Cancelled;
        task.CompletedAt = DateTime.UtcNow;
        SaveTasks();
        OnStateChanged(task);
        
        if (rollback && task.RollbackPoint != null)
        {
            var rollbackResult = await _rollbackManager.RollbackAsync(task.RollbackPoint.Id, cancellationToken);
            return new MigrationResult
            {
                Success = false,
                TaskId = taskId,
                FinalState = MigrationState.Cancelled,
                WasRolledBack = rollbackResult.Success,
                RollbackResult = rollbackResult
            };
        }
        
        return MigrationResult.Failed(task, "Cancelled by user", null);
    }
    
    /// <inheritdoc/>
    public async Task<RollbackResult> RollbackAsync(string taskId, CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out var task) || task.RollbackPoint == null)
        {
            return new RollbackResult
            {
                Success = false,
                ErrorMessage = "No rollback point found"
            };
        }
        
        return await _rollbackManager.RollbackAsync(task.RollbackPoint.Id, cancellationToken);
    }
    
    /// <inheritdoc/>
    public Task<bool> CanRollbackAsync(string taskId)
    {
        if (!_tasks.TryGetValue(taskId, out var task) || task.RollbackPoint == null)
        {
            return Task.FromResult(false);
        }
        
        return Task.FromResult(task.RollbackPoint.CanRollback);
    }
    
    #region Private Methods

    private sealed class MigrationTaskStateDocument
    {
        public List<MigrationTask> Tasks { get; set; } = new();
    }

    private void LoadPersistedTasks()
    {
        try
        {
            if (!File.Exists(_storageFilePath))
                return;

            var json = File.ReadAllText(_storageFilePath);
            var document = JsonSerializer.Deserialize<MigrationTaskStateDocument>(json, _jsonOptions);
            if (document?.Tasks is null)
                return;

            foreach (var task in document.Tasks)
            {
                _tasks[task.Id] = task;
            }

            if (document.Tasks.Count > 0)
            {
                _logger.LogInformation(
                    "Loaded {Count} persisted migration task(s) from {Path}",
                    document.Tasks.Count,
                    _storageFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load migration tasks from {Path}", _storageFilePath);
        }
    }

    private void SaveTasks()
    {
        try
        {
            Directory.CreateDirectory(_storageDirectory);
            var document = new MigrationTaskStateDocument
            {
                Tasks = _tasks.Values.OrderBy(t => t.CreatedAt).ToList()
            };
            var json = JsonSerializer.Serialize(document, _jsonOptions);
            File.WriteAllText(_storageFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist migration tasks to {Path}", _storageFilePath);
        }
    }
    
    private string DetermineTargetPath(MigrationRequest request)
    {
        var folderName = request.CustomTargetFolderName 
            ?? _fileSystem.GetFileName(request.SourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) 
            ?? request.Name;
        
        // Sanitize folder name
        foreach (var c in _fileSystem.GetInvalidFileNameChars())
        {
            folderName = folderName.Replace(c, '_');
        }
        folderName = folderName.Trim().TrimEnd('.');
        
        return _fileSystem.CombinePath(request.TargetRootPath, folderName);
    }
    
    private async Task CopyDirectoryAsync(MigrationTask task, string sourceDir, string targetDir, CancellationToken cancellationToken)
    {
        _fileSystem.CreateDirectory(targetDir);
        
        var files = _fileSystem.GetFiles(sourceDir, "*", false).ToList();
        var directories = _fileSystem.GetDirectories(sourceDir, "*", false).ToList();
        
        // Copy files
        foreach (var file in files)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            
            // Pause checkpoint: block here (without cancelling) if the task is paused.
            if (_pauseGates.TryGetValue(task.Id, out var gate))
                gate.Wait(cancellationToken);
            
            if (cancellationToken.IsCancellationRequested)
                return;
            
            var fileName = _fileSystem.GetFileName(file);
            var targetFile = _fileSystem.CombinePath(targetDir, fileName!);
            
            task.CurrentFile = fileName;
            OnProgressChanged(task);
            
            // Use metadata-preserving copy to retain timestamps and attributes.
            _fileSystem.CopyFilePreserveMetadata(file, targetFile, false);
            
            var fileSize = _fileSystem.GetFileSize(file);
            task.CopiedBytes += fileSize;
            task.CopiedFiles++;
            SaveTasks();
            
            OnProgressChanged(task);
        }
        
        // Copy subdirectories
        foreach (var directory in directories)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            
            // Pause checkpoint for subdirectories as well.
            if (_pauseGates.TryGetValue(task.Id, out var gate))
                gate.Wait(cancellationToken);
            
            if (cancellationToken.IsCancellationRequested)
                return;
            
            var dirName = _fileSystem.GetFileName(directory);
            var targetSubDir = _fileSystem.CombinePath(targetDir, dirName!);
            
            await CopyDirectoryAsync(task, directory, targetSubDir, cancellationToken);
        }

        // Preserve source directory timestamps on the newly created target directory.
        try
        {
            var srcInfo = new DirectoryInfo(sourceDir);
            var dstInfo = new DirectoryInfo(targetDir);
            dstInfo.CreationTimeUtc   = srcInfo.CreationTimeUtc;
            dstInfo.LastWriteTimeUtc  = srcInfo.LastWriteTimeUtc;
            dstInfo.LastAccessTimeUtc = srcInfo.LastAccessTimeUtc;
        }
        catch
        {
            // Best-effort; not fatal.
        }
    }
    
    private async Task<MigrationResult> FailTaskAsync(MigrationTask task, string message, Exception? ex, TimeSpan elapsed)
    {
        task.State = MigrationState.Failed;
        task.ErrorMessage = message;
        task.Exception = ex;
        task.CompletedAt = DateTime.UtcNow;
        SaveTasks();
        OnStateChanged(task);
        
        OnError(task, message, ex, MigrationErrorSeverity.Error);
        
        return MigrationResult.Failed(task, message, ex);
    }
    
    private async Task<MigrationResult> FailAndRollbackAsync(MigrationTask task, string message, Exception? ex, TimeSpan elapsed)
    {
        task.State = MigrationState.RollingBack;
        SaveTasks();
        OnStateChanged(task);
        
        if (task.RollbackPoint != null)
        {
            var rollbackResult = await _rollbackManager.RollbackAsync(task.RollbackPoint.Id);
            
            task.State = rollbackResult.Success ? MigrationState.RolledBack : MigrationState.PartialRollback;
            task.CompletedAt = DateTime.UtcNow;
            SaveTasks();
            OnStateChanged(task);
            
            return new MigrationResult
            {
                Success = false,
                TaskId = task.Id,
                FinalState = task.State,
                ErrorMessage = message,
                Exception = ex,
                WasRolledBack = rollbackResult.Success,
                RollbackResult = rollbackResult
            };
        }
        
        return await FailTaskAsync(task, message, ex, elapsed);
    }
    
    private async Task<MigrationResult> CancelAndRollbackAsync(MigrationTask task, TimeSpan elapsed)
    {
        task.State = MigrationState.Cancelled;
        task.CompletedAt = DateTime.UtcNow;
        SaveTasks();
        OnStateChanged(task);
        
        if (task.RollbackPoint != null)
        {
            var rollbackResult = await _rollbackManager.RollbackAsync(task.RollbackPoint.Id);
            return new MigrationResult
            {
                Success = false,
                TaskId = task.Id,
                FinalState = MigrationState.Cancelled,
                ErrorMessage = "Cancelled by user",
                WasRolledBack = rollbackResult.Success,
                RollbackResult = rollbackResult
            };
        }
        
        return MigrationResult.Failed(task, "Cancelled by user", null);
    }
    
    private void OnProgressChanged(MigrationTask task)
    {
        ProgressChanged?.Invoke(this, new MigrationProgressEventArgs
        {
            TaskId = task.Id,
            State = task.State,
            BytesCopied = task.CopiedBytes,
            TotalBytes = task.TotalBytes,
            FilesCopied = task.CopiedFiles,
            TotalFiles = task.TotalFiles,
            CurrentFile = task.CurrentFile,
            ProgressPercent = task.ProgressPercent
        });
    }
    
    private void OnStateChanged(MigrationTask task)
    {
        StateChanged?.Invoke(this, task);
    }
    
    private void OnError(MigrationTask task, string message, Exception? ex, MigrationErrorSeverity severity)
    {
        ErrorOccurred?.Invoke(this, new MigrationErrorEventArgs
        {
            TaskId = task.Id,
            Message = message,
            Exception = ex,
            Severity = severity,
            CurrentState = task.State,
            IsRecoverable = task.RollbackPoint?.CanRollback ?? false
        });
    }
    
    #endregion
}