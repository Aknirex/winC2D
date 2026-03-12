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

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
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
            TargetPath = task.TargetPath
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
                RollbackPoints = _rollbackPoints.Values.OrderBy(p => p.CreatedAt).ToList()
            };
            var json = JsonSerializer.Serialize(document, _jsonOptions);
            File.WriteAllText(_storageFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist rollback points to {Path}", _storageFilePath);
        }
    }
    
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
        // The backup directory was already deleted as the final cleanup step.
        // Files now live at TargetPath, but there is no backup to restore from.
        // This is an unrecoverable state — return false so the caller marks the
        // rollback as partial (IsPartial = true) instead of silently succeeding.
        _logger.LogError(
            "Cannot rollback BackupDeleted step for rollback point {Id}: " +
            "backup directory was already deleted. " +
            "Source='{SourcePath}', Target='{TargetPath}', Backup='{BackupPath}'. " +
            "Files remain at the target location. Manual intervention required.",
            point.Id, point.SourcePath, point.TargetPath, point.BackupPath ?? "<unknown>");
        return Task.FromResult(false);
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