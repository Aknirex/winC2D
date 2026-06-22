using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using winC2D.Core.Services;
using winC2D.Core.Models;
using winC2D.Core.FileSystem;

namespace winC2D.Infrastructure.Services;

/// <summary>
/// Implementation of rollback manager
/// </summary>
public class RollbackManager : IRollbackManager
{
    private const int SchemaVersion = 2;
    private readonly IFileSystem _fileSystem;
    private readonly ISymlinkManager _symlinkManager;
    private readonly ILogger<RollbackManager> _logger;
    private readonly string _storageDirectory;
    private readonly string _storageFilePath;
    private readonly object _stateGate = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false
    };

    // In-memory storage for rollback points (could be persisted to disk in production)
    private readonly ConcurrentDictionary<string, RollbackPoint> _rollbackPoints = new();
    private readonly ConcurrentDictionary<string, RollbackPoint> _taskRollbackPoints = new();

    public RollbackManager(
        IFileSystem fileSystem,
        ISymlinkManager symlinkManager,
        ILogger<RollbackManager> logger)
    {
        _fileSystem = fileSystem;
        _symlinkManager = symlinkManager;
        _logger = logger;

        _storageDirectory = PersistentStateFile.CreateStorageDirectory(logger, "rollback");
        _storageFilePath = Path.Combine(_storageDirectory, "rollback_points.json");

        LoadPersistedState();
    }

    /// <inheritdoc/>
    public Task<RollbackPoint> CreateRollbackPointAsync(MigrationTask task)
    {
        var rollbackPoint = new RollbackPoint
        {
            Id = Guid.NewGuid().ToString(),
            TaskId = task.Id,
            CreatedAt = DateTime.UtcNow,
            Type = task.Type,
            SourcePath = task.SourcePath,
            TargetPath = task.TargetPath,
            TempTargetPath = task.TempTargetPath
        };

        MutateState(() =>
        {
            _rollbackPoints[rollbackPoint.Id] = rollbackPoint;
            _taskRollbackPoints[task.Id] = rollbackPoint;
        });

        _logger.LogInformation("Created rollback point {Id} for task {TaskId}",
            rollbackPoint.Id, task.Id);

        return Task.FromResult(rollbackPoint);
    }

    /// <inheritdoc/>
    public Task RecordStepAsync(string rollbackPointId, CompletedStep step)
    {
        var found = false;
        MutateState(() =>
        {
            if (_rollbackPoints.TryGetValue(rollbackPointId, out var rollbackPoint))
            {
                rollbackPoint.AddStep(step);
                found = true;
            }
        });
        if (!found)
        {
            _logger.LogWarning("Rollback point not found: {Id}", rollbackPointId);
            return Task.CompletedTask;
        }
        _logger.LogDebug("Recorded step {Step} for rollback point {Id}", step, rollbackPointId);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetBackupPathAsync(string rollbackPointId, string backupPath)
    {
        var found = false;
        MutateState(() =>
        {
            if (_rollbackPoints.TryGetValue(rollbackPointId, out var rollbackPoint))
            {
                rollbackPoint.BackupPath = backupPath;
                found = true;
            }
        });
        if (!found)
        {
            _logger.LogWarning("Rollback point not found when setting backup path: {Id}", rollbackPointId);
            return Task.CompletedTask;
        }
        _logger.LogDebug("Set backup path '{BackupPath}' on rollback point {Id}", backupPath, rollbackPointId);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetTempTargetPathAsync(string rollbackPointId, string tempTargetPath)
    {
        var found = false;
        MutateState(() =>
        {
            if (_rollbackPoints.TryGetValue(rollbackPointId, out var rollbackPoint))
            {
                rollbackPoint.TempTargetPath = tempTargetPath;
                found = true;
            }
        });
        if (!found)
        {
            _logger.LogWarning("Rollback point not found when setting temp target path: {Id}", rollbackPointId);
            return Task.CompletedTask;
        }
        _logger.LogDebug("Set temp target path '{TempTargetPath}' on rollback point {Id}",
            tempTargetPath, rollbackPointId);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<RollbackResult> RollbackAsync(string rollbackPointId, CancellationToken cancellationToken = default)
    {
        RefreshStateForRead();
        if (!_rollbackPoints.TryGetValue(rollbackPointId, out var rollbackPoint))
        {
            return new RollbackResult
            {
                Success = false,
                ErrorMessage = "Rollback point not found"
            };
        }

        _logger.LogInformation("Starting rollback for point {Id} with {Count} step(s) to reverse",
            rollbackPointId, rollbackPoint.CompletedSteps.Count);

        var result = new RollbackResult();

        try
        {
            // Rollback in reverse order of steps.
            // We take a snapshot first because we'll mutate CompletedSteps as we succeed.
            var steps = rollbackPoint.CompletedSteps.ToList();
            var originalSteps = steps.ToHashSet();
            steps.Reverse();

            foreach (var step in steps)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    result.IsPartial = true;
                    _logger.LogWarning("Rollback of point {Id} cancelled after rolling back {Count} step(s)",
                        rollbackPointId, result.RolledBackSteps.Count);
                    break;
                }

                var stepResult = await RollbackStepAsync(
                    rollbackPoint, step, originalSteps, cancellationToken);

                if (!stepResult)
                {
                    result.IsPartial = true;
                    result.FailedStep = step;
                    _logger.LogError(
                        "Failed to rollback step {Step} for point {Id}. " +
                        "Rollback is partial: {RolledBackCount} step(s) succeeded. " +
                        "Remaining steps that were NOT reverted: {Remaining}",
                        step, rollbackPointId,
                        result.RolledBackSteps.Count,
                        string.Join(", ", rollbackPoint.CompletedSteps));
                    break;
                }

                // Step succeeded: remove it from the persisted rollback point so that
                // a crash / restart won't re-execute this rollback step.
                MutateState(() =>
                {
                    if (_rollbackPoints.TryGetValue(rollbackPointId, out var persisted))
                        persisted.CompletedSteps.Remove(step);
                });
                rollbackPoint.CompletedSteps.Remove(step);
                result.RolledBackSteps.Add(step);
            }

            result.Success = !result.IsPartial;

            if (result.Success)
            {
                _logger.LogInformation("Rollback completed successfully for point {Id}", rollbackPointId);
            }
            else
            {
                _logger.LogWarning("Partial rollback for point {Id}: {SuccessCount} succeeded, failed at {FailedStep}",
                    rollbackPointId, result.RolledBackSteps.Count, result.FailedStep);
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Exception = ex;
            result.IsPartial = true;

            _logger.LogError(ex, "Rollback failed for point {Id}", rollbackPointId);
        }

        return result;
    }

    /// <inheritdoc/>
    public Task<RollbackPoint?> GetRollbackPointAsync(string rollbackPointId)
    {
        RefreshStateForRead();
        _rollbackPoints.TryGetValue(rollbackPointId, out var point);
        return Task.FromResult(point);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<RollbackPoint>> GetRollbackPointsForTaskAsync(string taskId)
    {
        RefreshStateForRead();
        var points = _rollbackPoints.Values.Where(p => p.TaskId == taskId);
        return Task.FromResult(points);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<RollbackPoint>> GetAllRollbackPointsAsync()
    {
        RefreshStateForRead();
        return Task.FromResult(_rollbackPoints.Values.AsEnumerable());
    }

    /// <inheritdoc/>
    public Task DeleteRollbackPointAsync(string rollbackPointId)
    {
        RollbackPoint? removed = null;
        MutateState(() =>
        {
            if (_rollbackPoints.TryRemove(rollbackPointId, out var point))
            {
                _taskRollbackPoints.TryRemove(point.TaskId, out _);
                removed = point;
            }
        });
        if (removed is not null)
        {
            _logger.LogDebug("Deleted rollback point {Id}", rollbackPointId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task CleanupOldRollbackPointsAsync(DateTime olderThan)
    {
        var removedCount = 0;
        MutateState(() =>
        {
            var toRemove = _rollbackPoints.Values
                .Where(p => p.CreatedAt < olderThan)
                .ToList();
            foreach (var point in toRemove)
            {
                _rollbackPoints.TryRemove(point.Id, out _);
                _taskRollbackPoints.TryRemove(point.TaskId, out _);
            }
            removedCount = toRemove.Count;
        });

        if (removedCount > 0)
        {
            _logger.LogInformation("Cleaned up {Count} old rollback points", removedCount);
        }

        return Task.CompletedTask;
    }

    #region Private Methods

    private sealed class RollbackStateDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public List<RollbackPoint> RollbackPoints { get; set; } = new();
    }

    private void LoadPersistedState()
    {
        RefreshStateForRead();
    }

    private void RefreshStateForRead()
    {
        lock (_stateGate)
        {
            using var processLock = PersistentStateFile.AcquireProcessLock(_storageFilePath);
            try
            {
                LoadStateCore();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to refresh rollback points from {Path}; using last known-good state",
                    _storageFilePath);
            }
        }
    }

    private void MutateState(Action mutation)
    {
        lock (_stateGate)
        {
            using var processLock = PersistentStateFile.AcquireProcessLock(_storageFilePath);
            LoadStateCore();
            mutation();
            SaveStateCore();
        }
    }

    private void LoadStateCore()
    {
        if (!File.Exists(_storageFilePath))
        {
            _rollbackPoints.Clear();
            _taskRollbackPoints.Clear();
            return;
        }

        var json = File.ReadAllText(_storageFilePath);
        var document = JsonSerializer.Deserialize<RollbackStateDocument>(json, _jsonOptions)
            ?? throw new InvalidDataException($"Rollback state file '{_storageFilePath}' is empty or invalid.");
        var points = new Dictionary<string, RollbackPoint>(StringComparer.OrdinalIgnoreCase);
        foreach (var point in document.RollbackPoints)
            points[point.Id] = point;

        _rollbackPoints.Clear();
        _taskRollbackPoints.Clear();
        foreach (var point in points.Values)
        {
            _rollbackPoints[point.Id] = point;
            if (!string.IsNullOrWhiteSpace(point.TaskId))
                _taskRollbackPoints[point.TaskId] = point;
        }
    }

    private void SaveStateCore()
    {
        Directory.CreateDirectory(_storageDirectory);
        var document = new RollbackStateDocument
        {
            SchemaVersion = SchemaVersion,
            RollbackPoints = _rollbackPoints.Values.OrderBy(p => p.CreatedAt).ToList()
        };
        var json = JsonSerializer.Serialize(document, _jsonOptions);
        PersistentStateFile.AtomicWriteAllText(_storageFilePath, json);
    }

    private async Task<bool> RollbackStepAsync(
        RollbackPoint point,
        CompletedStep step,
        IReadOnlySet<CompletedStep> originalSteps,
        CancellationToken cancellationToken)
    {
        return step switch
        {
            CompletedStep.BackupDeleted => await RollbackBackupDeletedAsync(point),
            CompletedStep.SymlinkCreated => await RollbackSymlinkCreatedAsync(point),
            CompletedStep.TargetFinalized => await RollbackTargetFinalizedAsync(
                point, originalSteps.Contains(CompletedStep.BackupDeleted), cancellationToken),
            CompletedStep.TempFilesCopied => await RollbackTempFilesCopiedAsync(point),
            CompletedStep.FilesCopied => await RollbackFilesCopiedAsync(
                point, originalSteps.Contains(CompletedStep.BackupDeleted), cancellationToken),
            CompletedStep.SourceRenamed => await RollbackSourceRenamedAsync(point),
            _ => true
        };
    }

    private Task<bool> RollbackBackupDeletedAsync(RollbackPoint point)
    {
        // Backup deletion is not directly reversible. Completed migrations are
        // restored from TargetPath when TargetFinalized/FilesCopied is rolled back.
        _logger.LogDebug("Backup was deleted for rollback point {Id}; source will be restored from target if needed.",
            point.Id);
        return Task.FromResult(true);
    }

    private async Task<bool> RollbackSymlinkCreatedAsync(RollbackPoint point)
    {
        // Delete the symlink
        if (_fileSystem.DirectoryExists(point.SourcePath))
        {
            if (_symlinkManager.IsSymlink(point.SourcePath))
            {
                var deleted = await _symlinkManager.DeleteSymlinkAsync(point.SourcePath);
                if (!deleted)
                {
                    _logger.LogError("Failed to delete symlink at {Path}", point.SourcePath);
                    return false;
                }
            }
        }

        return true;
    }

    private async Task<bool> RollbackTargetFinalizedAsync(
        RollbackPoint point,
        bool backupWasDeleted,
        CancellationToken cancellationToken)
    {
        if (!_fileSystem.DirectoryExists(point.TargetPath))
            return true;

        if (backupWasDeleted)
            return await RestoreSourceFromTargetAsync(point, cancellationToken);

        try
        {
            _fileSystem.DeleteDirectory(point.TargetPath, true);
            _logger.LogDebug("Deleted finalized target directory: {Path}", point.TargetPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete finalized target directory: {Path}", point.TargetPath);
            return false;
        }
    }

    private Task<bool> RollbackTempFilesCopiedAsync(RollbackPoint point)
    {
        if (string.IsNullOrWhiteSpace(point.TempTargetPath) ||
            !_fileSystem.DirectoryExists(point.TempTargetPath))
            return Task.FromResult(true);

        try
        {
            _fileSystem.DeleteDirectory(point.TempTargetPath, true);
            _logger.LogDebug("Deleted temporary target directory: {Path}", point.TempTargetPath);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete temporary target directory: {Path}", point.TempTargetPath);
            return Task.FromResult(false);
        }
    }

    private async Task<bool> RollbackFilesCopiedAsync(
        RollbackPoint point,
        bool backupWasDeleted,
        CancellationToken cancellationToken)
    {
        // Legacy compatibility: older versions copied directly to TargetPath
        // and recorded FilesCopied instead of TempFilesCopied/TargetFinalized.
        if (!_fileSystem.DirectoryExists(point.TargetPath))
            return true;

        if (backupWasDeleted)
            return await RestoreSourceFromTargetAsync(point, cancellationToken);

        try
        {
            _fileSystem.DeleteDirectory(point.TargetPath, true);
            _logger.LogDebug("Deleted target directory: {Path}", point.TargetPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete target directory: {Path}", point.TargetPath);
            return false;
        }
    }

    private async Task<bool> RollbackSourceRenamedAsync(RollbackPoint point)
    {
        // Restore the source directory from backup
        if (!string.IsNullOrEmpty(point.BackupPath) && _fileSystem.DirectoryExists(point.BackupPath))
        {
            try
            {
                // If source exists (as symlink), delete it first
                if (_fileSystem.DirectoryExists(point.SourcePath))
                {
                    if (_symlinkManager.IsSymlink(point.SourcePath))
                    {
                        await _symlinkManager.DeleteSymlinkAsync(point.SourcePath);
                    }
                    else
                    {
                        // Source exists but is not a symlink – unexpected state.
                        // Refuse to continue because MoveDirectory would fail with
                        // "destination already exists" and we do not want to overwrite
                        // a real directory.
                        _logger.LogError(
                            "Rollback refused: SourcePath '{Source}' exists but is not a symlink. " +
                            "This indicates an inconsistent state. Manual intervention required. " +
                            "Backup is at '{Backup}'.",
                            point.SourcePath, point.BackupPath);
                        return false;
                    }
                }

                // Move backup back to source
                _fileSystem.MoveDirectory(point.BackupPath, point.SourcePath);
                _logger.LogDebug("Restored source directory from backup: {Backup} -> {Source}",
                    point.BackupPath, point.SourcePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore source directory from backup");
                return false;
            }
        }

        return true;
    }

    private async Task<bool> RestoreSourceFromTargetAsync(
        RollbackPoint point,
        CancellationToken cancellationToken)
    {
        var restoreTempPath = point.SourcePath + "_rollback_" + Guid.NewGuid().ToString("N");

        try
        {
            if (_fileSystem.DirectoryExists(point.SourcePath))
            {
                if (_symlinkManager.IsSymlink(point.SourcePath))
                {
                    var deleted = await _symlinkManager.DeleteSymlinkAsync(point.SourcePath);
                    if (!deleted)
                        return false;
                }
                else
                {
                    _logger.LogError(
                        "Rollback refused: SourcePath '{Source}' exists but is not a symlink. " +
                        "Target remains at '{Target}'.",
                        point.SourcePath, point.TargetPath);
                    return false;
                }
            }

            CopyDirectory(point.TargetPath, restoreTempPath, cancellationToken);
            _fileSystem.MoveDirectory(restoreTempPath, point.SourcePath);
            _fileSystem.DeleteDirectory(point.TargetPath, true);

            _logger.LogDebug("Restored source from target: {Target} -> {Source}",
                point.TargetPath, point.SourcePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore source from target");
            try
            {
                if (_fileSystem.DirectoryExists(restoreTempPath))
                    _fileSystem.DeleteDirectory(restoreTempPath, true);
            }
            catch { }

            return false;
        }
    }

    private void CopyDirectory(
        string sourceDir,
        string targetDir,
        CancellationToken cancellationToken)
    {
        var stack = new Stack<(string Source, string Target)>();
        stack.Push((sourceDir, targetDir));

        while (stack.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (src, dst) = stack.Pop();
            _fileSystem.CreateDirectory(dst);

            foreach (var file in _fileSystem.GetFiles(src, "*", false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fileName = _fileSystem.GetFileName(file)
                    ?? throw new IOException($"Cannot determine file name for '{file}'.");
                _fileSystem.CopyFilePreserveMetadata(file, _fileSystem.CombinePath(dst, fileName), false);
            }

            foreach (var directory in _fileSystem.GetDirectories(src, "*", false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var dirName = _fileSystem.GetFileName(directory)
                    ?? throw new IOException($"Cannot determine directory name for '{directory}'.");
                stack.Push((directory, _fileSystem.CombinePath(dst, dirName)));
            }

            try
            {
                var srcInfo = new DirectoryInfo(src);
                var dstInfo = new DirectoryInfo(dst);
                dstInfo.CreationTimeUtc = srcInfo.CreationTimeUtc;
                dstInfo.LastWriteTimeUtc = srcInfo.LastWriteTimeUtc;
                dstInfo.LastAccessTimeUtc = srcInfo.LastAccessTimeUtc;
            }
            catch { }
        }
    }

    #endregion
}
