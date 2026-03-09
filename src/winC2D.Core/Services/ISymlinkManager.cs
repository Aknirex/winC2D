namespace winC2D.Core.Services;

/// <summary>
/// Interface for managing symbolic links
/// </summary>
public interface ISymlinkManager
{
    /// <summary>
    /// Create a directory symbolic link
    /// </summary>
    /// <param name="linkPath">Path where the symlink will be created</param>
    /// <param name="targetPath">Path that the symlink will point to</param>
    /// <returns>True if successful</returns>
    Task<bool> CreateDirectorySymlinkAsync(string linkPath, string targetPath);
    
    /// <summary>
    /// Create a file symbolic link
    /// </summary>
    /// <param name="linkPath">Path where the symlink will be created</param>
    /// <param name="targetPath">Path that the symlink will point to</param>
    /// <returns>True if successful</returns>
    Task<bool> CreateFileSymlinkAsync(string linkPath, string targetPath);
    
    /// <summary>
    /// Check if a path is a symbolic link
    /// </summary>
    /// <param name="path">Path to check</param>
    /// <returns>True if it's a symlink</returns>
    bool IsSymlink(string path);
    
    /// <summary>
    /// Get the target of a symbolic link
    /// </summary>
    /// <param name="linkPath">Symlink path</param>
    /// <returns>Target path or null if not a symlink</returns>
    string? GetSymlinkTarget(string linkPath);
    
    /// <summary>
    /// Delete a symbolic link
    /// </summary>
    /// <param name="linkPath">Symlink path</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteSymlinkAsync(string linkPath);
    
    /// <summary>
    /// Verify that a symbolic link is valid
    /// </summary>
    /// <param name="linkPath">Symlink path</param>
    /// <returns>True if the symlink points to a valid target</returns>
    Task<bool> VerifySymlinkAsync(string linkPath);
}