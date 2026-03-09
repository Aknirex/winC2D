using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using winC2D.Core.Services;

namespace winC2D.Infrastructure.Services;

/// <summary>
/// Implementation of symbolic link manager
/// </summary>
public class SymlinkManager : ISymlinkManager
{
    private readonly ILogger<SymlinkManager> _logger;
    
    public SymlinkManager(ILogger<SymlinkManager> logger)
    {
        _logger = logger;
    }
    
    /// <inheritdoc/>
    public async Task<bool> CreateDirectorySymlinkAsync(string linkPath, string targetPath)
    {
        try
        {
            // Normalize paths
            linkPath = linkPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            targetPath = targetPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            
            // Check if target exists
            if (!Directory.Exists(targetPath))
            {
                _logger.LogError("Target directory does not exist: {TargetPath}", targetPath);
                return false;
            }
            
            // Check if link path already exists
            if (Directory.Exists(linkPath) || File.Exists(linkPath))
            {
                _logger.LogError("Link path already exists: {LinkPath}", linkPath);
                return false;
            }
            
            // Ensure parent directory exists
            var parentDir = Path.GetDirectoryName(linkPath);
            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }
            
            // Use .NET 6+ API for creating symbolic links
            Directory.CreateSymbolicLink(linkPath, targetPath);
            
            // Verify the symlink was created
            var created = await VerifySymlinkAsync(linkPath);
            if (!created)
            {
                _logger.LogError("Failed to verify symlink creation: {LinkPath}", linkPath);
                return false;
            }
            
            _logger.LogInformation("Created directory symlink: {LinkPath} -> {TargetPath}", linkPath, targetPath);
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied creating symlink: {LinkPath}. Administrator privileges required.", linkPath);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create directory symlink: {LinkPath} -> {TargetPath}", linkPath, targetPath);
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<bool> CreateFileSymlinkAsync(string linkPath, string targetPath)
    {
        try
        {
            // Normalize paths
            linkPath = linkPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            targetPath = targetPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            
            // Check if target exists
            if (!File.Exists(targetPath))
            {
                _logger.LogError("Target file does not exist: {TargetPath}", targetPath);
                return false;
            }
            
            // Check if link path already exists
            if (File.Exists(linkPath) || Directory.Exists(linkPath))
            {
                _logger.LogError("Link path already exists: {LinkPath}", linkPath);
                return false;
            }
            
            // Ensure parent directory exists
            var parentDir = Path.GetDirectoryName(linkPath);
            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }
            
            // Use .NET 6+ API for creating symbolic links
            File.CreateSymbolicLink(linkPath, targetPath);
            
            // Verify the symlink was created
            var info = new FileInfo(linkPath);
            if ((info.Attributes & FileAttributes.ReparsePoint) == 0)
            {
                _logger.LogError("Failed to verify file symlink creation: {LinkPath}", linkPath);
                return false;
            }
            
            _logger.LogInformation("Created file symlink: {LinkPath} -> {TargetPath}", linkPath, targetPath);
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied creating symlink: {LinkPath}. Administrator privileges required.", linkPath);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create file symlink: {LinkPath} -> {TargetPath}", linkPath, targetPath);
            return false;
        }
    }
    
    /// <inheritdoc/>
    public bool IsSymlink(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                var info = new DirectoryInfo(path);
                return (info.Attributes & FileAttributes.ReparsePoint) != 0;
            }
            
            if (File.Exists(path))
            {
                var info = new FileInfo(path);
                return (info.Attributes & FileAttributes.ReparsePoint) != 0;
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    /// <inheritdoc/>
    public string? GetSymlinkTarget(string linkPath)
    {
        try
        {
            if (Directory.Exists(linkPath))
            {
                var info = new DirectoryInfo(linkPath);
                if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    return info.LinkTarget;
                }
            }
            
            if (File.Exists(linkPath))
            {
                var info = new FileInfo(linkPath);
                if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    return info.LinkTarget;
                }
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }
    
    /// <inheritdoc/>
    public async Task<bool> DeleteSymlinkAsync(string linkPath)
    {
        try
        {
            if (Directory.Exists(linkPath) && IsSymlink(linkPath))
            {
                Directory.Delete(linkPath);
                _logger.LogInformation("Deleted directory symlink: {LinkPath}", linkPath);
                return true;
            }
            
            if (File.Exists(linkPath) && IsSymlink(linkPath))
            {
                File.Delete(linkPath);
                _logger.LogInformation("Deleted file symlink: {LinkPath}", linkPath);
                return true;
            }
            
            _logger.LogWarning("Path is not a symlink or does not exist: {LinkPath}", linkPath);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete symlink: {LinkPath}", linkPath);
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<bool> VerifySymlinkAsync(string linkPath)
    {
        try
        {
            if (!IsSymlink(linkPath))
            {
                return false;
            }
            
            var target = GetSymlinkTarget(linkPath);
            if (string.IsNullOrEmpty(target))
            {
                return false;
            }
            
            // Check if target exists
            var fullPath = target;
            if (!Path.IsPathRooted(target))
            {
                // Resolve relative path
                var linkDir = Path.GetDirectoryName(linkPath);
                if (!string.IsNullOrEmpty(linkDir))
                {
                    fullPath = Path.GetFullPath(Path.Combine(linkDir, target));
                }
            }
            
            return Directory.Exists(fullPath) || File.Exists(fullPath);
        }
        catch
        {
            return false;
        }
    }
}