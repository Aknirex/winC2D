namespace winC2D.Core.Services;

/// <summary>
/// Interface for managing rollback operations
/// </summary>
public interface IRollbackManager
{
    /// <summary>
    /// Create a rollback point before migration
    /// </summary>
    /// <param name="task">Migration task</param>
    /// <returns>Created rollback point</returns>
    Task<RollbackPoint> CreateRollbackPointAsync(MigrationTask task);
    
    /// <summary>
    /// Record a completed step
    /// </summary>
    /// <param name="rollbackPointId">Rollback point ID</param>
    /// <param name="step">Completed step</param>
    Task RecordStepAsync(string rollbackPointId, CompletedStep step);
    
    /// <summary>
    /// Rollback to the state before migration
    /// </summary>
    /// <param name="rollbackPointId">Rollback point ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rollback result</returns>
    Task<RollbackResult> RollbackAsync(string rollbackPointId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get a rollback point by ID
    /// </summary>
    /// <param name="rollbackPointId">Rollback point ID</param>
    /// <returns>Rollback point or null if not found</returns>
    Task<RollbackPoint?> GetRollbackPointAsync(string rollbackPointId);
    
    /// <summary>
    /// Get all rollback points for a task
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <returns>List of rollback points</returns>
    Task<IEnumerable<RollbackPoint>> GetRollbackPointsForTaskAsync(string taskId);
    
    /// <summary>
    /// Get all rollback points
    /// </summary>
    /// <returns>List of all rollback points</returns>
    Task<IEnumerable<RollbackPoint>> GetAllRollbackPointsAsync();
    
    /// <summary>
    /// Delete a rollback point after successful migration
    /// </summary>
    /// <param name="rollbackPointId">Rollback point ID</param>
    Task DeleteRollbackPointAsync(string rollbackPointId);
    
    /// <summary>
    /// Clean up old rollback points
    /// </summary>
    /// <param name="olderThan">Delete rollback points older than this date</param>
    Task CleanupOldRollbackPointsAsync(DateTime olderThan);
}