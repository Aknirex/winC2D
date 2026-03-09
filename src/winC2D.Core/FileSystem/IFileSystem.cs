namespace winC2D.Core.FileSystem;

/// <summary>
/// Interface for file system operations (abstraction for testing)
/// </summary>
public interface IFileSystem
{
    #region Directory Operations
    
    /// <summary>
    /// Check if a directory exists
    /// </summary>
    bool DirectoryExists(string path);
    
    /// <summary>
    /// Create a directory
    /// </summary>
    void CreateDirectory(string path);
    
    /// <summary>
    /// Delete a directory
    /// </summary>
    void DeleteDirectory(string path, bool recursive = false);
    
    /// <summary>
    /// Move a directory
    /// </summary>
    void MoveDirectory(string sourcePath, string destinationPath);
    
    /// <summary>
    /// Get all subdirectories in a directory
    /// </summary>
    IEnumerable<string> GetDirectories(string path, string searchPattern = "*", bool recursive = false);
    
    /// <summary>
    /// Get all files in a directory
    /// </summary>
    IEnumerable<string> GetFiles(string path, string searchPattern = "*", bool recursive = false);
    
    /// <summary>
    /// Get directory size in bytes
    /// </summary>
    long GetDirectorySize(string path, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if path is a symbolic link
    /// </summary>
    bool IsSymlink(string path);
    
    /// <summary>
    /// Create a directory symbolic link
    /// </summary>
    void CreateDirectorySymlink(string linkPath, string targetPath);
    
    /// <summary>
    /// Get the target of a symbolic link
    /// </summary>
    string? GetSymlinkTarget(string linkPath);
    
    #endregion
    
    #region File Operations
    
    /// <summary>
    /// Check if a file exists
    /// </summary>
    bool FileExists(string path);
    
    /// <summary>
    /// Copy a file
    /// </summary>
    void CopyFile(string sourcePath, string destinationPath, bool overwrite = false);
    
    /// <summary>
    /// Move a file
    /// </summary>
    void MoveFile(string sourcePath, string destinationPath, bool overwrite = false);
    
    /// <summary>
    /// Delete a file
    /// </summary>
    void DeleteFile(string path);
    
    /// <summary>
    /// Get file size in bytes
    /// </summary>
    long GetFileSize(string path);
    
    /// <summary>
    /// Get file attributes
    /// </summary>
    FileAttributes GetFileAttributes(string path);
    
    #endregion
    
    #region Path Operations
    
    /// <summary>
    /// Combine paths
    /// </summary>
    string CombinePath(params string[] paths);
    
    /// <summary>
    /// Get the directory name from a path
    /// </summary>
    string? GetDirectoryName(string path);
    
    /// <summary>
    /// Get the file name from a path
    /// </summary>
    string? GetFileName(string path);
    
    /// <summary>
    /// Get the file name without extension
    /// </summary>
    string? GetFileNameWithoutExtension(string path);
    
    /// <summary>
    /// Get the full path
    /// </summary>
    string GetFullPath(string path);
    
    /// <summary>
    /// Check if a path is valid
    /// </summary>
    bool IsValidPath(string path);
    
    /// <summary>
    /// Get invalid path characters
    /// </summary>
    char[] GetInvalidPathChars();
    
    /// <summary>
    /// Get invalid file name characters
    /// </summary>
    char[] GetInvalidFileNameChars();
    
    #endregion
    
    #region Drive Operations
    
    /// <summary>
    /// Get available drives
    /// </summary>
    IEnumerable<DriveInfo> GetDrives();
    
    /// <summary>
    /// Get drive info
    /// </summary>
    DriveInfo? GetDrive(string driveName);
    
    /// <summary>
    /// Get available free space on a drive
    /// </summary>
    long GetAvailableFreeSpace(string driveName);
    
    #endregion
}