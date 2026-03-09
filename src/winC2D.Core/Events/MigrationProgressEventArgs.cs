namespace winC2D.Core.Events;

/// <summary>
/// Event arguments for migration progress updates
/// </summary>
public class MigrationProgressEventArgs : EventArgs
{
    /// <summary>
    /// Task ID
    /// </summary>
    public string TaskId { get; set; } = string.Empty;
    
    /// <summary>
    /// Current state of the migration
    /// </summary>
    public MigrationState State { get; set; }
    
    /// <summary>
    /// Current step description
    /// </summary>
    public string CurrentStep { get; set; } = string.Empty;
    
    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int ProgressPercent { get; set; }
    
    /// <summary>
    /// Bytes copied so far
    /// </summary>
    public long BytesCopied { get; set; }
    
    /// <summary>
    /// Total bytes to copy
    /// </summary>
    public long TotalBytes { get; set; }
    
    /// <summary>
    /// Files copied so far
    /// </summary>
    public int FilesCopied { get; set; }
    
    /// <summary>
    /// Total files to copy
    /// </summary>
    public int TotalFiles { get; set; }
    
    /// <summary>
    /// Current file being processed
    /// </summary>
    public string? CurrentFile { get; set; }
    
    /// <summary>
    /// Elapsed time since migration started
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }
    
    /// <summary>
    /// Estimated remaining time
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    
    /// <summary>
    /// Transfer speed in bytes per second
    /// </summary>
    public long BytesPerSecond { get; set; }
}