namespace winC2D.Core.Models;

/// <summary>
/// Represents the result of a migration operation
/// </summary>
public class MigrationResult
{
    /// <summary>
    /// Whether the migration was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Associated task ID
    /// </summary>
    public string TaskId { get; set; } = string.Empty;
    
    /// <summary>
    /// Final state of the migration
    /// </summary>
    public MigrationState FinalState { get; set; }
    
    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Exception if failed
    /// </summary>
    public Exception? Exception { get; set; }
    
    /// <summary>
    /// Source path
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Target path
    /// </summary>
    public string TargetPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Total time taken
    /// </summary>
    public TimeSpan Duration { get; set; }
    
    /// <summary>
    /// Total bytes transferred
    /// </summary>
    public long BytesTransferred { get; set; }
    
    /// <summary>
    /// Total files transferred
    /// </summary>
    public int FilesTransferred { get; set; }
    
    /// <summary>
    /// Whether rollback was performed
    /// </summary>
    public bool WasRolledBack { get; set; }
    
    /// <summary>
    /// Rollback result if rollback was performed
    /// </summary>
    public RollbackResult? RollbackResult { get; set; }
    
    /// <summary>
    /// Create a successful result
    /// </summary>
    public static MigrationResult Succeeded(MigrationTask task, TimeSpan duration)
    {
        return new MigrationResult
        {
            Success = true,
            TaskId = task.Id,
            FinalState = MigrationState.Completed,
            SourcePath = task.SourcePath,
            TargetPath = task.TargetPath,
            Duration = duration,
            BytesTransferred = task.CopiedBytes,
            FilesTransferred = task.CopiedFiles
        };
    }
    
    /// <summary>
    /// Create a failed result
    /// </summary>
    public static MigrationResult Failed(MigrationTask task, string errorMessage, Exception? exception = null)
    {
        return new MigrationResult
        {
            Success = false,
            TaskId = task.Id,
            FinalState = task.State,
            ErrorMessage = errorMessage,
            Exception = exception,
            SourcePath = task.SourcePath,
            TargetPath = task.TargetPath
        };
    }
}

/// <summary>
/// Represents the result of a rollback operation
/// </summary>
public class RollbackResult
{
    /// <summary>
    /// Whether the rollback was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Error message if rollback failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Exception if rollback failed
    /// </summary>
    public Exception? Exception { get; set; }
    
    /// <summary>
    /// Steps that were rolled back
    /// </summary>
    public List<CompletedStep> RolledBackSteps { get; set; } = new();
    
    /// <summary>
    /// Whether the rollback was partial (some steps could not be rolled back)
    /// </summary>
    public bool IsPartial { get; set; }
}