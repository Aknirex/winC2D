namespace winC2D.Core.Services;

/// <summary>
/// Interface for the migration engine
/// </summary>
public interface IMigrationEngine
{
    #region Events
    
    /// <summary>
    /// Event raised when migration progress changes
    /// </summary>
    event EventHandler<MigrationProgressEventArgs>? ProgressChanged;
    
    /// <summary>
    /// Event raised when an error occurs during migration
    /// </summary>
    event EventHandler<MigrationErrorEventArgs>? ErrorOccurred;
    
    /// <summary>
    /// Event raised when migration state changes
    /// </summary>
    event EventHandler<MigrationTask>? StateChanged;
    
    #endregion
    
    #region Task Management
    
    /// <summary>
    /// Create a new migration task
    /// </summary>
    /// <param name="request">Migration request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created migration task</returns>
    Task<MigrationTask> CreateTaskAsync(MigrationRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get a migration task by ID
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <returns>Migration task or null if not found</returns>
    Task<MigrationTask?> GetTaskAsync(string taskId);
    
    /// <summary>
    /// Get all migration tasks
    /// </summary>
    /// <returns>List of migration tasks</returns>
    Task<IEnumerable<MigrationTask>> GetAllTasksAsync();
    
    #endregion
    
    #region Execution
    
    /// <summary>
    /// Execute a migration task
    /// </summary>
    /// <param name="task">Task to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Migration result</returns>
    Task<MigrationResult> ExecuteAsync(MigrationTask task, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute multiple migration tasks in sequence
    /// </summary>
    /// <param name="tasks">Tasks to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of migration results</returns>
    IAsyncEnumerable<MigrationResult> ExecuteBatchAsync(IEnumerable<MigrationTask> tasks, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Pause a running migration task
    /// </summary>
    /// <param name="taskId">Task ID</param>
    Task PauseAsync(string taskId);
    
    /// <summary>
    /// Resume a paused migration task
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Migration result</returns>
    Task<MigrationResult> ResumeAsync(string taskId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancel a running migration task
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <param name="rollback">Whether to rollback changes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Migration result</returns>
    Task<MigrationResult> CancelAsync(string taskId, bool rollback = true, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Rollback
    
    /// <summary>
    /// Rollback a completed migration
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rollback result</returns>
    Task<RollbackResult> RollbackAsync(string taskId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a migration can be rolled back
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <returns>True if rollback is possible</returns>
    Task<bool> CanRollbackAsync(string taskId);
    
    #endregion
}