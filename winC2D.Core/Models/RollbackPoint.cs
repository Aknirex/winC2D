namespace winC2D.Core.Models;

/// <summary>
/// Represents a completed step in the migration process
/// </summary>
public enum CompletedStep
{
    /// <summary>
    /// Source directory has been renamed to backup
    /// </summary>
    SourceRenamed,
    
    /// <summary>
    /// Files have been copied to target
    /// </summary>
    FilesCopied,
    
    /// <summary>
    /// Symbolic link has been created
    /// </summary>
    SymlinkCreated,
    
    /// <summary>
    /// Backup directory has been deleted
    /// </summary>
    BackupDeleted
}

/// <summary>
/// Represents a rollback point for recovery
/// </summary>
public class RollbackPoint
{
    /// <summary>
    /// Unique identifier for this rollback point
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Associated migration task ID
    /// </summary>
    public string TaskId { get; set; } = string.Empty;
    
    /// <summary>
    /// When this rollback point was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Type of migration
    /// </summary>
    public MigrationType Type { get; set; }
    
    /// <summary>
    /// Original source path
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Target path (new location)
    /// </summary>
    public string TargetPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Temporary backup path (renamed source)
    /// </summary>
    public string? BackupPath { get; set; }
    
    /// <summary>
    /// List of completed steps
    /// </summary>
    public List<CompletedStep> CompletedSteps { get; set; } = new();
    
    /// <summary>
    /// Whether this rollback point can be rolled back
    /// </summary>
    public bool CanRollback => CompletedSteps.Count > 0;
    
    /// <summary>
    /// Last completed step
    /// </summary>
    public CompletedStep? LastStep => CompletedSteps.Count > 0 ? CompletedSteps[^1] : null;
    
    /// <summary>
    /// Add a completed step
    /// </summary>
    public void AddStep(CompletedStep step)
    {
        if (!CompletedSteps.Contains(step))
        {
            CompletedSteps.Add(step);
        }
    }
    
    /// <summary>
    /// Remove the last completed step
    /// </summary>
    public CompletedStep? RemoveLastStep()
    {
        if (CompletedSteps.Count == 0) return null;
        
        var last = CompletedSteps[^1];
        CompletedSteps.RemoveAt(CompletedSteps.Count - 1);
        return last;
    }
}