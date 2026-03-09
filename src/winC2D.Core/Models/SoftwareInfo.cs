namespace winC2D.Core.Models;

/// <summary>
/// Represents the status of a software item
/// </summary>
public enum SoftwareStatus
{
    /// <summary>
    /// Normal directory, ready for migration
    /// </summary>
    Normal,
    
    /// <summary>
    /// Already migrated (symlink)
    /// </summary>
    Migrated,
    
    /// <summary>
    /// Suspicious (small size, may be residual)
    /// </summary>
    Suspicious,
    
    /// <summary>
    /// Empty directory
    /// </summary>
    Empty,
    
    /// <summary>
    /// Residual (uninstalled software leftover)
    /// </summary>
    Residual
}

/// <summary>
/// Represents installed software information
/// </summary>
public class SoftwareInfo
{
    /// <summary>
    /// Software display name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Installation location path
    /// </summary>
    public string InstallLocation { get; set; } = string.Empty;
    
    /// <summary>
    /// Size in bytes
    /// </summary>
    public long SizeBytes { get; set; }
    
    /// <summary>
    /// Whether this is a symlink (already migrated)
    /// </summary>
    public bool IsSymlink { get; set; }
    
    /// <summary>
    /// Current status of the software
    /// </summary>
    public SoftwareStatus Status { get; set; }
    
    /// <summary>
    /// Whether suspicious status has been checked
    /// </summary>
    public bool SuspiciousChecked { get; set; }
    
    /// <summary>
    /// Human-readable size string
    /// </summary>
    public string SizeText
    {
        get
        {
            if (SizeBytes == -1)
                return "> 10 MB";
            if ((Status == SoftwareStatus.Empty || SuspiciousChecked) && SizeBytes == 0)
                return "0 KB";
            if (SizeBytes <= 0)
                return "Unknown";
            if (SizeBytes < 1024 * 1024)
            {
                var kb = Math.Max(1, SizeBytes / 1024);
                return $"{kb} KB";
            }
            return $"{SizeBytes / (1024 * 1024)} MB";
        }
    }
}