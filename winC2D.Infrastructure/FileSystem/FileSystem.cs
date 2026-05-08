using System.IO;
using System.Runtime.InteropServices;
using winC2D.Core.FileSystem;

namespace winC2D.Infrastructure.FileSystem;

/// <summary>
/// Real file system implementation
/// </summary>
public class FileSystem : IFileSystem
{
    #region Directory Operations
    
    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }
    
    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }
    
    public void DeleteDirectory(string path, bool recursive = false)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive);
        }
    }
    
    public void MoveDirectory(string sourcePath, string destinationPath)
    {
        Directory.Move(sourcePath, destinationPath);
    }
    
    public IEnumerable<string> GetDirectories(string path, string searchPattern = "*", bool recursive = false)
    {
        var option = recursive
            ? new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint }
            : new EnumerationOptions { RecurseSubdirectories = false, IgnoreInaccessible = true };
        return Directory.GetDirectories(path, searchPattern, option);
    }
    
    public IEnumerable<string> GetFiles(string path, string searchPattern = "*", bool recursive = false)
    {
        var option = recursive
            ? new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint }
            : new EnumerationOptions { RecurseSubdirectories = false, IgnoreInaccessible = true };
        return Directory.GetFiles(path, searchPattern, option);
    }
    
    public long GetDirectorySize(string path, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(path))
            return 0;

        long size = 0;

        try
        {
            // Use DirectoryInfo.EnumerateFiles which returns FileInfo objects with
            // Length pre-populated from WIN32_FIND_DATA, avoiding a second system
            // call per file that Directory.EnumerateFiles → new FileInfo() would incur.
            // For a directory with 100k files this saves ~100k GetFileAttributesEx calls.
            var dirInfo = new DirectoryInfo(path);
            var enumerationOptions = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                // Skip reparse points (symlinks, junctions) to avoid double-counting / infinite loops
                AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.System,
            };

            int count = 0;
            foreach (var file in dirInfo.EnumerateFiles("*", enumerationOptions))
            {
                // Batch cancellation check: only poll every 512 files.
                // IsCancellationRequested on CancellationToken involves an interlocked
                // read and a volatile barrier; doing it per-file for millions of files
                // adds measurable overhead.
                if ((++count & 0x1FF) == 0 && cancellationToken.IsCancellationRequested)
                    break;

                // file.Length uses the cached nFileSizeLow/nFileSizeHigh from the
                // native FindFirstFile/FindNextFile call – no extra syscall needed.
                size += file.Length;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Return what we have so far (partial size is better than zero).
        }

        return size;
    }
    
    public bool IsSymlink(string path)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            return (attributes & FileAttributes.ReparsePoint) != 0;
        }
        catch
        {
            return false;
        }
    }
    
    public void CreateDirectorySymlink(string linkPath, string targetPath)
    {
        // Remove trailing slashes for consistency
        linkPath = linkPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        targetPath = targetPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        
        // Use .NET 6+ API
        Directory.CreateSymbolicLink(linkPath, targetPath);
    }
    
    public string? GetSymlinkTarget(string linkPath)
    {
        try
        {
            var info = new DirectoryInfo(linkPath);
            if ((info.Attributes & FileAttributes.ReparsePoint) == 0)
                return null;
            
            var target = info.LinkTarget;
            return target;
        }
        catch
        {
            return null;
        }
    }
    
    #endregion
    
    #region File Operations
    
    public bool FileExists(string path)
    {
        return File.Exists(path);
    }
    
    public void CopyFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        File.Copy(sourcePath, destinationPath, overwrite);
    }

    public void CopyFilePreserveMetadata(string sourcePath, string destinationPath, bool overwrite = false)
    {
        File.Copy(sourcePath, destinationPath, overwrite);

        try
        {
            var src = new FileInfo(sourcePath);

            // Preserve timestamps
            File.SetCreationTimeUtc(destinationPath, src.CreationTimeUtc);
            File.SetLastWriteTimeUtc(destinationPath, src.LastWriteTimeUtc);
            File.SetLastAccessTimeUtc(destinationPath, src.LastAccessTimeUtc);

            // Preserve file attributes (read-only, hidden, archive, etc.)
            // Strip ReparsePoint / Compressed / Encrypted as those cannot be simply copied.
            const FileAttributes nonCopyable =
                FileAttributes.ReparsePoint |
                FileAttributes.Compressed   |
                FileAttributes.Encrypted    |
                FileAttributes.Offline      |
                FileAttributes.SparseFile;

            var attrs = src.Attributes & ~nonCopyable;
            if (attrs != FileAttributes.Normal && attrs != 0)
                File.SetAttributes(destinationPath, attrs);
        }
        catch
        {
            // Metadata copy is best-effort; the file content is already copied.
        }
    }

    public void MoveFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        if (overwrite && File.Exists(destinationPath))
        {
            File.Delete(destinationPath);
        }
        File.Move(sourcePath, destinationPath);
    }
    
    public void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
    
    public long GetFileSize(string path)
    {
        var info = new FileInfo(path);
        return info.Exists ? info.Length : 0;
    }
    
    public FileAttributes GetFileAttributes(string path)
    {
        return File.GetAttributes(path);
    }
    
    #endregion
    
    #region Path Operations
    
    public string CombinePath(params string[] paths)
    {
        return Path.Combine(paths);
    }
    
    public string? GetDirectoryName(string path)
    {
        return Path.GetDirectoryName(path);
    }
    
    public string? GetFileName(string path)
    {
        return Path.GetFileName(path);
    }
    
    public string? GetFileNameWithoutExtension(string path)
    {
        return Path.GetFileNameWithoutExtension(path);
    }
    
    public string GetFullPath(string path)
    {
        return Path.GetFullPath(path);
    }
    
    public bool IsValidPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;
        
        try
        {
            // Try to get full path - will throw if invalid
            _ = Path.GetFullPath(path);
            
            // Check for invalid characters
            var invalidChars = Path.GetInvalidPathChars();
            return !path.Any(c => invalidChars.Contains(c));
        }
        catch
        {
            return false;
        }
    }
    
    public char[] GetInvalidPathChars()
    {
        return Path.GetInvalidPathChars();
    }
    
    public char[] GetInvalidFileNameChars()
    {
        return Path.GetInvalidFileNameChars();
    }
    
    #endregion
    
    #region Drive Operations
    
    public IEnumerable<DriveInfo> GetDrives()
    {
        return DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
    }
    
    public DriveInfo? GetDrive(string driveName)
    {
        try
        {
            var drive = new DriveInfo(driveName);
            return drive.IsReady ? drive : null;
        }
        catch
        {
            return null;
        }
    }
    
    public long GetAvailableFreeSpace(string driveName)
    {
        var drive = GetDrive(driveName);
        return drive?.AvailableFreeSpace ?? 0;
    }
    
    #endregion
}