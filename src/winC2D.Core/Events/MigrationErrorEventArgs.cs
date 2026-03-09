namespace winC2D.Core.Events;

/// <summary>
/// Severity level for migration errors
/// </summary>
public enum MigrationErrorSeverity
{
    /// <summary>
    /// Warning - migration can continue
    /// </summary>
    Warning,
    
    /// <summary>
    /// Error - migration failed but can be rolled back
    /// </summary>
    Error,
    
    /// <summary>
    /// Critical - migration failed and rollback may not be possible
    /// </summary>
    Critical
}

/// <summary>
/// Event arguments for migration error events
/// </summary>
public class MigrationErrorEventArgs : EventArgs
{
    /// <summary>
    /// Task ID
    /// </summary>
    public string TaskId { get; set; } = string.Empty;
    
    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Exception that caused the error
    /// </summary>
    public Exception? Exception { get; set; }
    
    /// <summary>
    /// Severity of the error
    /// </summary>
    public MigrationErrorSeverity Severity { get; set; }
    
    /// <summary>
    /// Current state when the error occurred
    /// </summary>
    public MigrationState CurrentState { get; set; }
    
    /// <summary>
    /// Whether the error is recoverable
    /// </summary>
    public bool IsRecoverable { get; set; }
    
    /// <summary>
    /// Suggested action for the user
    /// </summary>
    public string? SuggestedAction { get; set; }
    
    /// <summary>
    /// Create a warning event
    /// </summary>
    public static MigrationErrorEventArgs Warning(string taskId, string message, MigrationState state)
    {
        return new MigrationErrorEventArgs
        {
            TaskId = taskId,
            Message = message,
            Severity = MigrationErrorSeverity.Warning,
            CurrentState = state,
            IsRecoverable = true
        };
    }
    
    /// <summary>
    /// Create an error event
    /// </summary>
    public static MigrationErrorEventArgs Error(string taskId, string message, Exception? exception, MigrationState state, bool recoverable = true)
    {
        return new MigrationErrorEventArgs
        {
            TaskId = taskId,
            Message = message,
            Exception = exception,
            Severity = MigrationErrorSeverity.Error,
            CurrentState = state,
            IsRecoverable = recoverable
        };
    }
    
    /// <summary>
    /// Create a critical error event
    /// </summary>
    public static MigrationErrorEventArgs Critical(string taskId, string message, Exception? exception, MigrationState state)
    {
        return new MigrationErrorEventArgs
        {
            TaskId = taskId,
            Message = message,
            Exception = exception,
            Severity = MigrationErrorSeverity.Critical,
            CurrentState = state,
            IsRecoverable = false
        };
    }
}