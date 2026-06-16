using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly IMigrationPreflightService _preflightService;
    private readonly IMigrationTaskStore _taskStore;
    private readonly ILogger<MigrationEngine> _logger;

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
        : this(
            fileSystem,
            symlinkManager,
            rollbackManager,
            new PermissivePreflightService(fileSystem),
            new JsonMigrationTaskStore(NullLogger<JsonMigrationTaskStore>.Instance),
            logger)
    {
    }

    public MigrationEngine(
        IFileSystem fileSystem,
        ISymlinkManager symlinkManager,
        IRollbackManager rollbackManager,
        IMigrationPreflightService preflightService,
        IMigrationTaskStore taskStore,
        ILogger<MigrationEngine> logger)
    {
        _fileSystem = fileSystem;
        _symlinkManager = symlinkManager;
        _rollbackManager = rollbackManager;
        _preflightService = preflightService;
        _taskStore = taskStore;
        _logger = logger;
    }
    
    /// <inheritdoc/>
    public async Task<MigrationTask> CreateTaskAsync(MigrationRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _preflightService.ValidateAsync(request, cancellationToken);

        var task = new MigrationTask
        {
            Id = Guid.NewGuid().ToString(),
            Type = request.Type,
            Name = request.Name,
            SourcePath = request.SourcePath,
            TargetPath = validation.TargetPath,
            VerifyFiles = request.VerifyFiles,
            State = MigrationState.Pending,
            CreatedAt = DateTime.UtcNow
        };
        
        // Calculate total size
        task.TotalBytes = validation.SourceSizeBytes > 0
            ? validation.SourceSizeBytes
            : await Task.Run(() => _fileSystem.GetDirectorySize(request.SourcePath, cancellationToken), cancellationToken);
        task.TotalFiles = await Task.Run(() => _fileSystem.GetFiles(request.SourcePath, "*", true).Count(), cancellationToken);
        
        _tasks[task.Id] = task;
        await SaveTaskAsync(task);
        
        _logger.LogInformation("Created migration task {TaskId} for {Name}: {SourcePath} -> {TargetPath}", 
            task.Id, task.Name, task.SourcePath, task.TargetPath);
        
        return task;
    }
    
    /// <inheritdoc/>
    public Task<MigrationTask?> GetTaskAsync(string taskId)
    {
        return _taskStore.GetAsync(taskId);
    }
    
    /// <inheritdoc/>
    public Task<IEnumerable<MigrationTask>> GetAllTasksAsync()
    {
        return _taskStore.GetAllAsync().ContinueWith(t => t.Result.AsEnumerable());
    }

    public Task<MigrationPreflightResult> ValidateAsync(MigrationRequest request, CancellationToken cancellationToken = default)
    {
        return _preflightService.ValidateAsync(request, cancellationToken);
    }

    public Task<int> CleanupTasksAsync(TaskCleanupOptions options, CancellationToken cancellationToken = default)
    {
        return CleanupTasksCoreAsync(options, cancellationToken);
    }

    private async Task<int> CleanupTasksCoreAsync(TaskCleanupOptions options, CancellationToken cancellationToken)
    {
        var removed = await _taskStore.CleanupAsync(options, cancellationToken);
        await _rollbackManager.CleanupOldRollbackPointsAsync(DateTime.UtcNow.AddDays(-Math.Max(0, options.OlderThanDays)));
        return removed;
    }
    
    /// <inheritdoc/>
    public async Task<MigrationResult> ExecuteAsync(MigrationTask task, CancellationToken cancellationToken = default)
    {
        _tasks[task.Id] = task;
        var stopwatch = Stopwatch.StartNew();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _cancellationTokens[task.Id] = cts;
        
        // Create (or reuse) a pause gate for this task — initially signalled (running).
        var pauseGate = _pauseGates.GetOrAdd(task.Id, _ => new ManualResetEventSlim(true));
        pauseGate.Set(); // Ensure the gate is open when we start/resume.
        
        try
        {
            task.StartedAt = DateTime.UtcNow;
            task.LastHeartbeatAt = DateTime.UtcNow;
            task.State = MigrationState.Preparing;
            await SaveTaskAsync(task);
            OnStateChanged(task);
            
            var validation = await _preflightService.ValidateAsync(new MigrationRequest
            {
                Type = task.Type,
                Name = task.Name,
                SourcePath = task.SourcePath,
                TargetRootPath = Path.GetDirectoryName(task.TargetPath) ?? string.Empty,
                CustomTargetFolderName = Path.GetFileName(task.TargetPath),
                VerifyFiles = task.VerifyFiles
            }, cancellationToken);
            if (!validation.CanProceed)
                return await FailTaskAsync(task, string.Join("; ", validation.Blockers), null, stopwatch.Elapsed);

            var tempTargetPath = BuildTempTargetPath(task.TargetPath);
            task.TempTargetPath = tempTargetPath;
            await SaveTaskAsync(task);
            
            // Create rollback point
            task.RollbackPoint = await _rollbackManager.CreateRollbackPointAsync(task);
            await _rollbackManager.SetTempTargetPathAsync(task.RollbackPoint.Id, tempTargetPath);
            await SaveTaskAsync(task);
            
            // Step 1: Rename source directory (atomic operation to detect locked files)
            task.State = MigrationState.Copying;
            await SaveTaskAsync(task);
            OnStateChanged(task);
            
            var backupPath = task.SourcePath + "_migrating_" + Guid.NewGuid().ToString("N");
            task.BackupPath = backupPath;
            await SaveTaskAsync(task);

            // Diagnostic: check for locked files before attempting the rename
            var lockedFiles = DetectLockedFiles(task.SourcePath);
            if (lockedFiles.Count > 0)
            {
                _logger.LogWarning(
                    "[DIAG] {Count} file(s) in '{SourcePath}' appear to be locked by running processes: {LockedFiles}",
                    lockedFiles.Count, task.SourcePath, string.Join(", ", lockedFiles));

                // If we found locked files, fail early with a clear message instead of
                // attempting a MoveDirectory that will certainly fail with access-denied.
                var fileLockMessage = $"Cannot migrate '{task.Name}' because {lockedFiles.Count} file(s) "
                    + "are locked by running processes. Please close the application "
                    + $"(including any background services/tray icons) and retry. "
                    + $"Locked files: {string.Join(", ", lockedFiles.Take(10))}";
                return await FailTaskAsync(task, fileLockMessage, null, System.Diagnostics.Stopwatch.StartNew().Elapsed);
            }

            try
            {
                _logger.LogInformation(
                    "[DIAG] Attempting MoveDirectory: '{SourcePath}' -\u003E '{BackupPath}'. Admin={IsAdmin}",
                    task.SourcePath, backupPath,
                    System.Security.Principal.WindowsIdentity.GetCurrent().Name is { } n
                        && new System.Security.Principal.WindowsPrincipal(
                            System.Security.Principal.WindowsIdentity.GetCurrent())
                           .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator));

                _fileSystem.MoveDirectory(task.SourcePath, backupPath);

                _logger.LogInformation("[DIAG] MoveDirectory succeeded.");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogError(uaEx,
                    "[DIAG] MoveDirectory FAILED with UnauthorizedAccessException: {Message}. HRESULT=0x{HResult:X8}. "
                    + "This typically means: (a) process is not running as admin, or (b) files are locked by running processes.",
                    uaEx.Message, uaEx.HResult);
                throw;
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx,
                    "[DIAG] MoveDirectory FAILED with IOException: {Message}. HRESULT=0x{HResult:X8}. "
                    + "This typically means files are in use by another process.",
                    ioEx.Message, ioEx.HResult);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[DIAG] MoveDirectory FAILED with {ExceptionType}: {Message}. HRESULT=0x{HResult:X8}.",
                    ex.GetType().Name, ex.Message, ex.HResult);
                throw;
            }
            await _rollbackManager.RecordStepAsync(task.RollbackPoint.Id, CompletedStep.SourceRenamed);
            
            // Persist the backup path on the rollback point so that RollbackSourceRenamedAsync
            // can locate the backup directory if a rollback is needed.
            await _rollbackManager.SetBackupPathAsync(task.RollbackPoint.Id, backupPath);
            await SaveTaskAsync(task);
            
            // Step 2: Copy files to an isolated temporary target. The final
            // target path should not appear until the copy has completed.
            await CopyDirectoryAsync(task, backupPath, tempTargetPath, cts.Token);
            
            if (cts.Token.IsCancellationRequested)
            {
                return await CancelAndRollbackAsync(task, stopwatch.Elapsed, cts.Token);
            }
            
            await _rollbackManager.RecordStepAsync(task.RollbackPoint.Id, CompletedStep.TempFilesCopied);

            // Step 3: Atomically promote the fully-copied temp directory to the final target
            _fileSystem.MoveDirectory(tempTargetPath, task.TargetPath);
            await _rollbackManager.RecordStepAsync(task.RollbackPoint.Id, CompletedStep.TargetFinalized);
            
            // Step 4: Create symbolic link
            task.State = MigrationState.CreatingSymlink;
            await SaveTaskAsync(task);
            OnStateChanged(task);
            
            var symlinkCreated = await _symlinkManager.CreateDirectorySymlinkAsync(task.SourcePath, task.TargetPath);
            if (!symlinkCreated)
            {
                return await FailAndRollbackAsync(task, "Failed to create symbolic link", null, stopwatch.Elapsed, cts.Token);
            }
            
            await _rollbackManager.RecordStepAsync(task.RollbackPoint.Id, CompletedStep.SymlinkCreated);
            
            // Step 5: Delete backup directory
            task.State = MigrationState.CleaningUp;
            await SaveTaskAsync(task);
            OnStateChanged(task);
            
            _fileSystem.DeleteDirectory(backupPath, true);
            await _rollbackManager.RecordStepAsync(task.RollbackPoint.Id, CompletedStep.BackupDeleted);
            
            // Success!
            task.State = MigrationState.Completed;
            task.CompletedAt = DateTime.UtcNow;
            await SaveTaskAsync(task);
            OnStateChanged(task);
            
            stopwatch.Stop();
            
            _logger.LogInformation("Migration task {TaskId} completed successfully in {Duration}", 
                task.Id, stopwatch.Elapsed);
            
            return MigrationResult.Succeeded(task, stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            return await CancelAndRollbackAsync(task, stopwatch.Elapsed, cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration task {TaskId} failed: {Message}", task.Id, ex.Message);
            return await FailAndRollbackAsync(task, ex.Message, ex, stopwatch.Elapsed, cts.Token);
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
    public async Task PauseAsync(string taskId)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
            return;
        
        // Only pause tasks that are actively running.
        if (task.State is not (MigrationState.Copying or MigrationState.CreatingSymlink or MigrationState.CleaningUp or MigrationState.Preparing))
            return;
        
        // Blocking the pause gate will cause the copy loop to wait at the next
        // checkpoint without triggering cancellation or rollback.
        if (_pauseGates.TryGetValue(taskId, out var gate))
            gate.Reset();
        
        task.State = MigrationState.Paused;
        await SaveTaskAsync(task);
        OnStateChanged(task);
        
        _logger.LogInformation("Migration task {TaskId} paused", taskId);
    }
    
    /// <inheritdoc/>
    /// <remarks>
    /// Resuming unblocks the paused copy loop inside the already-running
    /// <see cref="ExecuteAsync"/> call.  This method returns immediately;
    /// the final result of the migration is delivered asynchronously via the
    /// <see cref="StateChanged"/> event when <see cref="ExecuteAsync"/> completes.
    /// To await the definitive outcome, subscribe to <see cref="StateChanged"/>
    /// and watch for a terminal state.
    /// </remarks>
    public async Task<MigrationResult> ResumeAsync(string taskId, CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
            return MigrationResult.Failed(null!, "Task not found", null);
        
        if (task.State != MigrationState.Paused)
            return MigrationResult.Failed(task, "Task is not paused", null);
        
        // Signal the pause gate — the copy loop will resume from where it stopped.
        if (_pauseGates.TryGetValue(taskId, out var gate))
            gate.Set();
        
        task.State = MigrationState.Copying;
        await SaveTaskAsync(task);
        OnStateChanged(task);
        
        _logger.LogInformation("Migration task {TaskId} resumed", taskId);
        
        // The background execution loop is already running and waiting on the gate;
        // returning a placeholder result to indicate the resume request was accepted.
        // The authoritative result arrives later via StateChanged / ProgressChanged events.
        return new MigrationResult
        {
            Success = true,
            TaskId = taskId,
            FinalState = MigrationState.Copying
        };
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
        await SaveTaskAsync(task);
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

    public Task RequestPauseAsync(string taskId, CancellationToken cancellationToken = default)
    {
        return _taskStore.RequestPauseAsync(taskId, cancellationToken);
    }

    public Task RequestResumeAsync(string taskId, CancellationToken cancellationToken = default)
    {
        return _taskStore.RequestResumeAsync(taskId, cancellationToken);
    }

    public Task RequestCancelAsync(string taskId, bool rollback = true, CancellationToken cancellationToken = default)
    {
        return _taskStore.RequestCancelAsync(taskId, rollback, cancellationToken);
    }
    
    /// <inheritdoc/>
    public async Task<RollbackResult> RollbackAsync(string taskId, CancellationToken cancellationToken = default)
    {
        _tasks.TryGetValue(taskId, out var task);
        task ??= await _taskStore.GetAsync(taskId, cancellationToken);
        if (task == null || task.RollbackPoint == null)
        {
            return new RollbackResult
            {
                Success = false,
                ErrorMessage = "No rollback point found"
            };
        }
        
        task.State = MigrationState.RollingBack;
        await SaveTaskAsync(task);
        OnStateChanged(task);

        var result = await _rollbackManager.RollbackAsync(task.RollbackPoint.Id, cancellationToken);

        task.State = result.Success ? MigrationState.RolledBack : MigrationState.PartialRollback;
        task.CompletedAt = DateTime.UtcNow;
        if (!result.Success)
            task.ErrorMessage = result.ErrorMessage ?? "Rollback failed";
        await SaveTaskAsync(task);
        OnStateChanged(task);

        return result;
    }
    
    /// <inheritdoc/>
    public async Task<bool> CanRollbackAsync(string taskId)
    {
        var task = await _taskStore.GetAsync(taskId);
        if (task?.RollbackPoint == null)
            return false;
        
        return task.RollbackPoint.CanRollback;
    }
    
    #region Private Methods

    private sealed class PermissivePreflightService : IMigrationPreflightService
    {
        private readonly IFileSystem _fileSystem;

        public PermissivePreflightService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public Task<MigrationPreflightResult> ValidateAsync(MigrationRequest request, CancellationToken cancellationToken = default)
        {
            var sourceName = _fileSystem.GetFileName(
                request.SourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var folderName = request.CustomTargetFolderName ?? sourceName ?? request.Name ?? "MigratedApp";
            var targetPath = _fileSystem.CombinePath(request.TargetRootPath, folderName);
            if (string.IsNullOrWhiteSpace(targetPath))
                targetPath = Path.Combine(request.TargetRootPath, folderName);

            var result = new MigrationPreflightResult
            {
                SourcePath = request.SourcePath,
                TargetPath = targetPath,
                SourceSizeBytes = 0,
                TargetFreeBytes = long.MaxValue,
                HasEnoughSpace = true
            };

            try
            {
                var sourcePath = Path.GetFullPath(request.SourcePath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var normalizedTarget = Path.GetFullPath(targetPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var sourceWithSeparator = sourcePath + Path.DirectorySeparatorChar;

                if (normalizedTarget.StartsWith(sourceWithSeparator, StringComparison.OrdinalIgnoreCase))
                    result.Blockers.Add("Target path cannot be inside the source directory.");
            }
            catch (Exception ex)
            {
                result.Blockers.Add($"Invalid migration path: {ex.Message}");
            }

            return Task.FromResult(new MigrationPreflightResult
            {
                SourcePath = result.SourcePath,
                TargetPath = result.TargetPath,
                SourceSizeBytes = result.SourceSizeBytes,
                TargetFreeBytes = result.TargetFreeBytes,
                HasEnoughSpace = result.HasEnoughSpace,
                Blockers = result.Blockers
            });
        }
    }

    /// <summary>
    /// Diagnostic helper: detects files in a directory that are locked by running processes,
    /// AND checks whether the directory itself can be renamed (write access to the parent).
    /// This helps distinguish file-lock errors from permission-denied errors.
    /// </summary>
    private static List<string> DetectLockedFiles(string directoryPath)
    {
        var lockedFiles = new List<string>();

        if (!Directory.Exists(directoryPath))
        {
            lockedFiles.Add("[DirectoryNotFound]");
            return lockedFiles;
        }

        // Check directory-level write access: try creating and deleting a temp file
        // in the source directory to verify the parent allows rename operations.
        try
        {
            var parentDir = Path.GetDirectoryName(directoryPath);
            if (!string.IsNullOrEmpty(parentDir))
            {
                var testFile = Path.Combine(parentDir, $"_winC2D_test_{Guid.NewGuid():N}.tmp");
                try
                {
                    File.WriteAllText(testFile, "winC2D access test");
                    File.Delete(testFile);
                }
                catch (UnauthorizedAccessException)
                {
                    lockedFiles.Add($"[ParentDirAccessDenied] Cannot write to '{parentDir}'. "
                        + "This directory is protected and requires administrator privileges to modify.");
                }
            }
        }
        catch
        {
            // Best-effort
        }

        // Check individual files for process locks
        try
        {
            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            const int maxFilesToCheck = 200; // limit scan to avoid excessive runtime
            var checkedCount = 0;

            foreach (var file in files)
            {
                if (checkedCount++ >= maxFilesToCheck)
                    break;

                try
                {
                    // Try to open the file with exclusive read to detect locks
                    using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None);
                }
                catch (IOException)
                {
                    // File is locked by another process
                    lockedFiles.Add(Path.GetFileName(file));
                }
                catch (UnauthorizedAccessException)
                {
                    // Access denied - note this separately as it is a different root cause
                    lockedFiles.Add($"{Path.GetFileName(file)} [AccessDenied]");
                }
                // Other exceptions (FileNotFound etc.) are not lock-related
            }
        }
        catch
        {
            // Best-effort; if enumeration itself fails, return what we have
        }

        return lockedFiles;
    }

    private Task SaveTaskAsync(MigrationTask task)
    {
        task.LastHeartbeatAt = DateTime.UtcNow;
        return _taskStore.UpsertAsync(task, immediate: true);
    }

    private Task SaveProgressAsync(MigrationTask task)
    {
        return _taskStore.SaveProgressAsync(task);
    }

    private string BuildTempTargetPath(string targetPath)
    {
        string tempPath;
        do
        {
            tempPath = targetPath + "_copying_" + Guid.NewGuid().ToString("N");
        }
        while (_fileSystem.DirectoryExists(tempPath) || _fileSystem.FileExists(tempPath));

        return tempPath;
    }

    
    private async Task CopyDirectoryAsync(MigrationTask task, string sourceDir, string targetDir, CancellationToken cancellationToken)
    {
        // Iterative depth-first traversal to avoid stack overflow on deeply nested
        // directory trees. Each stack entry is a (sourcePath, targetPath) pair.
        var stack = new Stack<(string Source, string Target)>();
        stack.Push((sourceDir, targetDir));

        while (stack.Count > 0)
        {
            await ApplyExternalControlRequestsAsync(task, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

            // Pause checkpoint
            if (_pauseGates.TryGetValue(task.Id, out var gate))
                gate.Wait(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

            var (src, dst) = stack.Pop();
            _fileSystem.CreateDirectory(dst);

            // Copy files in this directory
            var files = _fileSystem.GetFiles(src, "*", false).ToList();
            foreach (var file in files)
            {
                await ApplyExternalControlRequestsAsync(task, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return;

                if (_pauseGates.TryGetValue(task.Id, out gate))
                    gate.Wait(cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return;

                var fileName = _fileSystem.GetFileName(file);
                var targetFile = _fileSystem.CombinePath(dst, fileName!);

                task.CurrentFile = fileName;
                OnProgressChanged(task);

                _fileSystem.CopyFilePreserveMetadata(file, targetFile, false);

                var fileSize = _fileSystem.GetFileSize(file);
                if (task.VerifyFiles)
                {
                    var copiedSize = _fileSystem.GetFileSize(targetFile);
                    if (copiedSize != fileSize)
                        throw new IOException($"File verification failed for '{fileName}'.");
                }

                task.CopiedBytes += fileSize;
                task.CopiedFiles++;
                await SaveProgressAsync(task);

                OnProgressChanged(task);
            }

            // Push subdirectories onto the stack (order doesn't matter for DFS)
            var directories = _fileSystem.GetDirectories(src, "*", false).ToList();
            foreach (var directory in directories)
            {
                var dirName = _fileSystem.GetFileName(directory);
                stack.Push((directory, _fileSystem.CombinePath(dst, dirName!)));
            }

            // Preserve source directory timestamps on the newly created target directory
            try
            {
                var srcInfo = new DirectoryInfo(src);
                var dstInfo = new DirectoryInfo(dst);
                dstInfo.CreationTimeUtc   = srcInfo.CreationTimeUtc;
                dstInfo.LastWriteTimeUtc  = srcInfo.LastWriteTimeUtc;
                dstInfo.LastAccessTimeUtc = srcInfo.LastAccessTimeUtc;
            }
            catch
            {
                // Best-effort; not fatal.
            }
        }
    }

    private async Task ApplyExternalControlRequestsAsync(MigrationTask task, CancellationToken cancellationToken)
    {
        var persisted = await _taskStore.GetAsync(task.Id, cancellationToken);
        if (persisted is null)
            return;

        if (persisted.CancelRequestedAt is not null)
        {
            task.CancelRequestedAt = persisted.CancelRequestedAt;
            task.CancelRollback = persisted.CancelRollback;
            throw new OperationCanceledException(cancellationToken);
        }

        var pauseRequested = persisted.PauseRequestedAt is not null &&
                             (persisted.ResumeRequestedAt is null ||
                              persisted.PauseRequestedAt > persisted.ResumeRequestedAt);
        if (!pauseRequested || task.State == MigrationState.Paused)
            return;

        task.PauseRequestedAt = persisted.PauseRequestedAt;
        task.State = MigrationState.Paused;
        await SaveTaskAsync(task);
        OnStateChanged(task);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);

            persisted = await _taskStore.GetAsync(task.Id, cancellationToken);
            if (persisted?.CancelRequestedAt is not null)
            {
                task.CancelRequestedAt = persisted.CancelRequestedAt;
                task.CancelRollback = persisted.CancelRollback;
                throw new OperationCanceledException(cancellationToken);
            }

            if (persisted?.ResumeRequestedAt is not null &&
                (persisted.PauseRequestedAt is null || persisted.ResumeRequestedAt >= persisted.PauseRequestedAt))
            {
                task.ResumeRequestedAt = persisted.ResumeRequestedAt;
                task.State = MigrationState.Copying;
                await SaveTaskAsync(task);
                OnStateChanged(task);
                return;
            }
        }
    }
    
    private async Task<MigrationResult> FailTaskAsync(MigrationTask task, string message, Exception? ex, TimeSpan elapsed)
    {
        task.State = MigrationState.Failed;
        task.ErrorMessage = message;
        task.Exception = ex;
        task.CompletedAt = DateTime.UtcNow;
        await SaveTaskAsync(task);
        OnStateChanged(task);
        
        OnError(task, message, ex, MigrationErrorSeverity.Error);
        
        return MigrationResult.Failed(task, message, ex);
    }
    
    private async Task<MigrationResult> FailAndRollbackAsync(
        MigrationTask task, string message, Exception? ex, TimeSpan elapsed,
        CancellationToken cancellationToken = default)
    {
        task.State = MigrationState.RollingBack;
        task.ErrorMessage = message;
        await SaveTaskAsync(task);
        OnStateChanged(task);
        
        if (task.RollbackPoint != null)
        {
            // Use a fresh timeout token so a cancelled copy operation does not
            // immediately cancel the rollback that protects the original data.
            using var rollbackCts = new CancellationTokenSource();
            rollbackCts.CancelAfter(TimeSpan.FromMinutes(5));
            
            var rollbackResult = await _rollbackManager.RollbackAsync(task.RollbackPoint.Id, rollbackCts.Token);
            
            task.State = rollbackResult.Success ? MigrationState.RolledBack : MigrationState.PartialRollback;
            task.CompletedAt = DateTime.UtcNow;
            task.ErrorMessage = message;
            await SaveTaskAsync(task);
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
    
    private async Task<MigrationResult> CancelAndRollbackAsync(
        MigrationTask task, TimeSpan elapsed,
        CancellationToken cancellationToken = default)
    {
        task.State = MigrationState.Cancelled;
        task.CompletedAt = DateTime.UtcNow;
        await SaveTaskAsync(task);
        OnStateChanged(task);
        
        if (task.CancelRollback && task.RollbackPoint != null)
        {
            using var rollbackCts = new CancellationTokenSource();
            rollbackCts.CancelAfter(TimeSpan.FromMinutes(5));
            
            var rollbackResult = await _rollbackManager.RollbackAsync(task.RollbackPoint.Id, rollbackCts.Token);
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
