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
    private readonly IFileSystem _fileSystem;
    private readonly ISymlinkManager _symlinkManager;
    private readonly ILogger<RollbackManager> _logger;
    private readonly string _storageDirectory;
    private readonly string _storageFilePath;
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

        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (string.IsNullOrWhiteSpace(localAppData))
            localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _storageDirectory = Path.Combine(localAppData, "winC2D", "rollback");
        Directory.CreateDirectory(_storageDirectory);
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
        
        _rollbackPoints[rollbackPoint.Id] = rollbackPoint;
        _taskRollbackPoints[task.Id] = rollbackPoint;

        SaveState();
        
        _logger.LogInformation("Created rollback point {Id} for task {TaskId}", 
            rollbackPoint.Id, task.Id);
        
        return Task.FromResult(rollbackPoint);
    }
    
    /// <inheritdoc/>
    public Task RecordStepAsync(string rollbackPointId, CompletedStep step)
    {
        if (!_rollbackPoints.TryGetValue(rollbackPointId, out var rollbackPoint))
        {
            _logger.LogWarning("Rollback point not found: {Id}", rollbackPointId);
            return Task.CompletedTask;
        }
        
        rollbackPoint.AddStep(step);
        SaveState();
        _logger.LogDebug("Recorded step {Step} for rollback point {Id}", step, rollbackPointId);
        
        return Task.CompletedTask;
    }
    
    /// <inheritdoc/>
    public Task SetBackupPathAsync(string rollbackPointId, string backupPath)
    {
        if (!_rollbackPoints.TryGetValue(rollbackPointId, out var rollbackPoint))
        {
            _logger.LogWarning("Rollback point not found when setting backup path: {Id}", rollbackPointId);
            return Task.CompletedTask;
        }
        
        rollbackPoint.BackupPath = backupPath;
        SaveState();
        _logger.LogDebug("Set backup path '{BackupPath}' on rollback point {Id}", backupPath, rollbackPointId);
        
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetTempTargetPathAsync(string rollbackPointId, string tempTargetPath)
    {
        if (!_rollbackPoints.TryGetValue(rollbackPointId, out var rollbackPoint))
        {
            _logger.LogWarning("Rollback point not found when setting temp target path: {Id}", rollbackPointId);
            return Task.CompletedTask;
        }

        rollbackPoint.TempTargetPath = tempTargetPath;
        SaveState();
        _logger.LogDebug("Set temp target path '{TempTargetPath}' on rollback point {Id}",
            tempTargetPath, rollbackPointId);

        return Task.CompletedTask;
    }
    
    /// <inheritdoc/>
    public async Task<RollbackResult> RollbackAsync(string rollbackPointId, CancellationToken cancellationToken = default)
    {
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
                
                var stepResult = await RollbackStepAsync(rollbackPoint, step, originalSteps);
                
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
                rollbackPoint.CompletedSteps.Remove(step);
                SaveState();
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
        _rollbackPoints.TryGetValue(rollbackPointId, out var point);
        return Task.FromResult(point);
    }
    
    /// <inheritdoc/>
    public Task<IEnumerable<RollbackPoint>> GetRollbackPointsForTaskAsync(string taskId)
    {
        var points = _rollbackPoints.Values.Where(p => p.TaskId == taskId);
        return Task.FromResult(points);
    }
    
    /// <inheritdoc/>
    public Task<IEnumerable<RollbackPoint>> GetAllRollbackPointsAsync()
    {
        return Task.FromResult(_rollbackPoints.Values.AsEnumerable());
    }
    
    /// <inheritdoc/>
    public Task DeleteRollbackPointAsync(string rollbackPointId)
    {
        if (_rollbackPoints.TryRemove(rollbackPointId, out var point))
        {
            _taskRollbackPoints.TryRemove(point.TaskId, out _);
            SaveState();
            _logger.LogDebug("Deleted rollback point {Id}", rollbackPointId);
        }
        
        return Task.CompletedTask;
    }
    
    /// <inheritdoc/>
    public Task CleanupOldRollbackPointsAsync(DateTime olderThan)
    {
        var toRemove = _rollbackPoints.Values
            .Where(p => p.CreatedAt < olderThan)
            .ToList();
        
        foreach (var point in toRemove)
        {
            _rollbackPoints.TryRemove(point.Id, out _);
            _taskRollbackPoints.TryRemove(point.TaskId, out _);
        }

        if (toRemove.Any())
            SaveState();
        
        if (toRemove.Any())
        {
            _logger.LogInformation("Cleaned up {Count} old rollback points", toRemove.Count);
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
        try
        {
            if (!File.Exists(_storageFilePath))
                return;

            var json = File.ReadAllText(_storageFilePath);
            var document = JsonSerializer.Deserialize<RollbackStateDocument>(json, _jsonOptions);
            if (document?.RollbackPoints is null)
                return;

            foreach (var point in document.RollbackPoints)
            {
                _rollbackPoints[point.Id] = point;
                if (!string.IsNullOrWhiteSpace(point.TaskId))
                    _taskRollbackPoints[point.TaskId] = point;
            }

            if (document.RollbackPoints.Count > 0)
            {
                _logger.LogInformation(
                    "Loaded {Count} persisted rollback point(s) from {Path}",
                    document.RollbackPoints.Count,
                    _storageFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load rollback points from {Path}", _storageFilePath);
        }
    }

    private void SaveState()
    {
        try
        {
            Directory.CreateDirectory(_storageDirectory);
            var document = new RollbackStateDocument
            {
                SchemaVersion = 2,
                RollbackPoints = _rollbackPoints.Values.OrderBy(p => p.CreatedAt).ToList()
            };
            var json = JsonSerializer.Serialize(document, _jsonOptions);

            // Atomic write to prevent corruption on crash mid-write.
            var tmpPath = _storageFilePath + ".tmp";
            File.WriteAllText(tmpPath, json);
            File.Move(tmpPath, _storageFilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist rollback points to {Path}", _storageFilePath);
        }
    }
    
    private async Task<bool> RollbackStepAsync(
        RollbackPoint point,
        CompletedStep step,
        IReadOnlySet<CompletedStep> originalSteps)
    {
        return step switch
        {
            CompletedStep.BackupDeleted => await RollbackBackupDeletedAsync(point),
            CompletedStep.SymlinkCreated => await RollbackSymlinkCreatedAsync(point),
            CompletedStep.TargetFinalized => await RollbackTargetFinalizedAsync(
                point, originalSteps.Contains(CompletedStep.BackupDeleted)),
            CompletedStep.TempFilesCopied => await RollbackTempFilesCopiedAsync(point),
            CompletedStep.FilesCopied => await RollbackFilesCopiedAsync(
                point, originalSteps.Contains(CompletedStep.BackupDeleted)),
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
    
    private async Task<bool> RollbackTargetFinalizedAsync(RollbackPoint point, bool backupWasDeleted)
    {
        if (!_fileSystem.DirectoryExists(point.TargetPath))
            return true;

        if (backupWasDeleted)
            return await RestoreSourceFromTargetAsync(point);

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

    private async Task<bool> RollbackFilesCopiedAsync(RollbackPoint point, bool backupWasDeleted)
    {
        // Legacy compatibility: older versions copied directly to TargetPath
        // and recorded FilesCopied instead of TempFilesCopied/TargetFinalized.
        if (!_fileSystem.DirectoryExists(point.TargetPath))
            return true;

        if (backupWasDeleted)
            return await RestoreSourceFromTargetAsync(point);

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

    private async Task<bool> RestoreSourceFromTargetAsync(RollbackPoint point)
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

            CopyDirectory(point.TargetPath, restoreTempPath);
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

    private void CopyDirectory(string sourceDir, string targetDir)
    {
        var stack = new Stack<(string Source, string Target)>();
        stack.Push((sourceDir, targetDir));

        while (stack.Count > 0)
        {
            var (src, dst) = stack.Pop();
            _fileSystem.CreateDirectory(dst);

            foreach (var file in _fileSystem.GetFiles(src, "*", false))
            {
                var fileName = _fileSystem.GetFileName(file)
                    ?? throw new IOException($"Cannot determine file name for '{file}'.");
                _fileSystem.CopyFilePreserveMetadata(file, _fileSystem.CombinePath(dst, fileName), false);
            }

            foreach (var directory in _fileSystem.GetDirectories(src, "*", false))
            {
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
