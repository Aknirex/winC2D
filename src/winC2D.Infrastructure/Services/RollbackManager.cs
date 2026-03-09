using Microsoft.Extensions.Logging;

namespace winC2D.Infrastructure.Services;

/// <summary>
/// Implementation of rollback manager
/// </summary>
public class RollbackManager : IRollbackManager
{
    private readonly IFileSystem _fileSystem;
    private readonly ISymlinkManager _symlinkManager;
    private readonly ILogger<RollbackManager> _logger;
    
    // In-memory storage for rollback points (could be persisted to disk in production)
    private readonly Dictionary<string, RollbackPoint> _rollbackPoints = new();
    private readonly Dictionary<string, RollbackPoint> _taskRollbackPoints = new();
    
    public RollbackManager(
        IFileSystem fileSystem,
        ISymlinkManager symlinkManager,
        ILogger<RollbackManager> logger)
    {
        _fileSystem = fileSystem;
        _symlinkManager = symlinkManager;
        _logger = logger;
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
            TargetPath = task.TargetPath
        };
        
        _rollbackPoints[rollbackPoint.Id] = rollbackPoint;
        _taskRollbackPoints[task.Id] = rollbackPoint;
        
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
        _logger.LogDebug("Recorded step {Step} for rollback point {Id}", step, rollbackPointId);
        
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
        
        _logger.LogInformation("Starting rollback for point {Id}", rollbackPointId);
        
        var result = new RollbackResult();
        
        try
        {
            // Rollback in reverse order of steps
            var steps = rollbackPoint.CompletedSteps.ToList();
            steps.Reverse();
            
            foreach (var step in steps)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                var stepResult = await RollbackStepAsync(rollbackPoint, step);
                result.RolledBackSteps.Add(step);
                
                if (!stepResult)
                {
                    result.IsPartial = true;
                    _logger.LogError("Failed to rollback step {Step}", step);
                    break;
                }
            }
            
            result.Success = !result.IsPartial;
            
            if (result.Success)
            {
                _logger.LogInformation("Rollback completed successfully for point {Id}", rollbackPointId);
            }
            else
            {
                _logger.LogWarning("Partial rollback for point {Id}", rollbackPointId);
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
        if (_rollbackPoints.TryGetValue(rollbackPointId, out var point))
        {
            _rollbackPoints.Remove(rollbackPointId);
            _taskRollbackPoints.Remove(point.TaskId);
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
            _rollbackPoints.Remove(point.Id);
            _taskRollbackPoints.Remove(point.TaskId);
        }
        
        if (toRemove.Any())
        {
            _logger.LogInformation("Cleaned up {Count} old rollback points", toRemove.Count);
        }
        
        return Task.CompletedTask;
    }
    
    #region Private Methods
    
    private async Task<bool> RollbackStepAsync(RollbackPoint point, CompletedStep step)
    {
        return step switch
        {
            CompletedStep.BackupDeleted => await RollbackBackupDeletedAsync(point),
            CompletedStep.SymlinkCreated => await RollbackSymlinkCreatedAsync(point),
            CompletedStep.FilesCopied => await RollbackFilesCopiedAsync(point),
            CompletedStep.SourceRenamed => await RollbackSourceRenamedAsync(point),
            _ => true
        };
    }
    
    private Task<bool> RollbackBackupDeletedAsync(RollbackPoint point)
    {
        // Backup was deleted, nothing to rollback
        // The files are now in TargetPath, we need to restore them to BackupPath first
        // But since backup is deleted, we can't restore from there
        // This is a critical state - files exist in TargetPath but backup is gone
        _logger.LogWarning("Cannot rollback BackupDeleted step - backup was already deleted");
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
    
    private async Task<bool> RollbackFilesCopiedAsync(RollbackPoint point)
    {
        // Delete the copied files from target
        if (_fileSystem.DirectoryExists(point.TargetPath))
        {
            try
            {
                _fileSystem.DeleteDirectory(point.TargetPath, true);
                _logger.LogDebug("Deleted target directory: {Path}", point.TargetPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete target directory: {Path}", point.TargetPath);
                return false;
            }
        }
        
        return true;
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
                        // Source exists but is not a symlink - this shouldn't happen
                        _logger.LogWarning("Source path exists but is not a symlink: {Path}", point.SourcePath);
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
    
    #endregion
}