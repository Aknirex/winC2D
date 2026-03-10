namespace winC2D.Core.Models;

/// <summary>
/// Represents the state of a migration task
/// </summary>
public enum MigrationState
{
    /// <summary>
    /// Task created, waiting to start
    /// </summary>
    Pending,
    
    /// <summary>
    /// Preparing for migration (checking paths, etc.)
    /// </summary>
    Preparing,
    
    /// <summary>
    /// Copying files
    /// </summary>
    Copying,
    
    /// <summary>
    /// Creating symbolic link
    /// </summary>
    CreatingSymlink,
    
    /// <summary>
    /// Cleaning up temporary files
    /// </summary>
    CleaningUp,
    
    /// <summary>
    /// Migration completed successfully
    /// </summary>
    Completed,
    
    /// <summary>
    /// Migration failed
    /// </summary>
    Failed,
    
    /// <summary>
    /// Rolling back after failure
    /// </summary>
    RollingBack,
    
    /// <summary>
    /// Rollback completed
    /// </summary>
    RolledBack,
    
    /// <summary>
    /// Partial rollback (rollback failed)
    /// </summary>
    PartialRollback,
    
    /// <summary>
    /// Cancelled by user
    /// </summary>
    Cancelled,
    
    /// <summary>
    /// Paused by user
    /// </summary>
    Paused
}

/// <summary>
/// Represents a single migration task
/// </summary>
public class MigrationTask
{
    /// <summary>
    /// Unique task identifier
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Task creation time
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Task start time
    /// </summary>
    public DateTime? StartedAt { get; set; }
    
    /// <summary>
    /// Task completion time
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Type of migration
    /// </summary>
    public MigrationType Type { get; set; }
    
    /// <summary>
    /// Software or folder name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Source path (original location)
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Target path (new location)
    /// </summary>
    public string TargetPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Temporary backup path (renamed source during migration)
    /// </summary>
    public string? BackupPath { get; set; }
    
    /// <summary>
    /// Current state of the task
    /// </summary>
    public MigrationState State { get; set; } = MigrationState.Pending;
    
    /// <summary>
    /// Total bytes to copy
    /// </summary>
    public long TotalBytes { get; set; }
    
    /// <summary>
    /// Bytes copied so far
    /// </summary>
    public long CopiedBytes { get; set; }
    
    /// <summary>
    /// Total files to copy
    /// </summary>
    public int TotalFiles { get; set; }
    
    /// <summary>
    /// Files copied so far
    /// </summary>
    public int CopiedFiles { get; set; }
    
    /// <summary>
    /// Current file being copied
    /// </summary>
    public string? CurrentFile { get; set; }
    
    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Exception if failed
    /// </summary>
    public Exception? Exception { get; set; }
    
    /// <summary>
    /// Rollback point for recovery
    /// </summary>
    public RollbackPoint? RollbackPoint { get; set; }
    
    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int ProgressPercent
    {
        get
        {
            if (TotalBytes == 0) return 0;
            return (int)((double)CopiedBytes / TotalBytes * 100);
        }
    }
}