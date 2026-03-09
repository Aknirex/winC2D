namespace winC2D.Core.Models;

/// <summary>
/// Represents a request to create a migration task
/// </summary>
public class MigrationRequest
{
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
    /// Target root path (new location root)
    /// </summary>
    public string TargetRootPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether to overwrite existing target
    /// </summary>
    public bool Overwrite { get; set; } = false;
    
    /// <summary>
    /// Whether to create symbolic link after migration
    /// </summary>
    public bool CreateSymlink { get; set; } = true;
    
    /// <summary>
    /// Whether to verify files after copy
    /// </summary>
    public bool VerifyFiles { get; set; } = true;
    
    /// <summary>
    /// Custom target folder name (if different from source folder name)
    /// </summary>
    public string? CustomTargetFolderName { get; set; }
}