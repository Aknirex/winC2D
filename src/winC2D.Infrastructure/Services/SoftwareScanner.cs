using Microsoft.Extensions.Logging;

namespace winC2D.Infrastructure.Services;

/// <summary>
/// Implementation of software scanner
/// </summary>
public class SoftwareScanner : ISoftwareScanner
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<SoftwareScanner> _logger;
    
    /// <summary>
    /// Size threshold for suspicious directories (10 MB)
    /// </summary>
    private const long SuspiciousSizeThreshold = 10L * 1024 * 1024;
    
    public event EventHandler<ScanProgressEventArgs>? ProgressChanged;
    
    public SoftwareScanner(IFileSystem fileSystem, ILogger<SoftwareScanner> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<SoftwareInfo>> ScanAsync(IEnumerable<string> directories, CancellationToken cancellationToken = default)
    {
        var result = new List<SoftwareInfo>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var directoriesList = directories.ToList();
        var scannedCount = 0;
        
        foreach (var baseDir in directoriesList)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            
            if (!_fileSystem.DirectoryExists(baseDir))
                continue;
            
            OnProgressChanged(baseDir, scannedCount, directoriesList.Count, result.Count);
            
            var subDirs = _fileSystem.GetDirectories(baseDir, "*", false);
            
            foreach (var subDir in subDirs)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                var normalizedPath = NormalizePath(subDir);
                
                if (visited.Contains(normalizedPath))
                    continue;
                
                visited.Add(normalizedPath);
                
                try
                {
                    var info = await BuildSoftwareInfoAsync(subDir, cancellationToken);
                    result.Add(info);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to scan directory: {Path}", subDir);
                }
            }
            
            scannedCount++;
        }
        
        return result;
    }
    
    /// <inheritdoc/>
    public Task<IEnumerable<SoftwareInfo>> ScanAsync(CancellationToken cancellationToken = default)
    {
        var defaultDirs = GetDefaultScanDirectories();
        return ScanAsync(defaultDirs, cancellationToken);
    }
    
    /// <inheritdoc/>
    public IEnumerable<string> GetDefaultScanDirectories()
    {
        var dirs = new List<string>();
        
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrEmpty(programFiles) && _fileSystem.DirectoryExists(programFiles))
        {
            dirs.Add(programFiles);
        }
        
        if (Environment.Is64BitOperatingSystem)
        {
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!string.IsNullOrEmpty(programFilesX86) && _fileSystem.DirectoryExists(programFilesX86))
            {
                dirs.Add(programFilesX86);
            }
        }
        
        return dirs;
    }
    
    /// <inheritdoc/>
    public async Task<SoftwareInfo> CheckSuspiciousAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        if (software == null || string.IsNullOrEmpty(software.InstallLocation))
            return software;
        
        var path = software.InstallLocation;
        
        if (!_fileSystem.DirectoryExists(path))
        {
            software.Status = SoftwareStatus.Residual;
            software.SizeBytes = 0;
            software.SuspiciousChecked = true;
            return software;
        }
        
        var hasEntries = false;
        var hasExe = false;
        var size = 0L;
        
        try
        {
            var files = _fileSystem.GetFiles(path, "*", true);
            
            foreach (var file in files)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                hasEntries = true;
                
                if (!hasExe && file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    hasExe = true;
                }
                
                try
                {
                    size += _fileSystem.GetFileSize(file);
                }
                catch
                {
                    // Ignore files that can't be accessed
                }
            }
            
            if (!hasEntries)
            {
                var subDirs = _fileSystem.GetDirectories(path, "*", true);
                hasEntries = subDirs.Any();
            }
        }
        catch
        {
            // Ignore scan errors
        }
        
        software.SizeBytes = size;
        software.SuspiciousChecked = true;
        
        if (!hasEntries)
        {
            software.Status = SoftwareStatus.Empty;
        }
        else if (!hasExe)
        {
            software.Status = SoftwareStatus.Residual;
        }
        else
        {
            software.Status = SoftwareStatus.Normal;
        }
        
        return software;
    }
    
    /// <inheritdoc/>
    public Task<long> CalculateSizeAsync(string path, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => _fileSystem.GetDirectorySize(path, cancellationToken), cancellationToken);
    }
    
    #region Private Methods
    
    private async Task<SoftwareInfo> BuildSoftwareInfoAsync(string path, CancellationToken cancellationToken)
    {
        var isSymlink = _fileSystem.IsSymlink(path);
        
        var hasEntries = false;
        try
        {
            hasEntries = _fileSystem.GetFiles(path, "*", false).Any() || 
                         _fileSystem.GetDirectories(path, "*", false).Any();
        }
        catch
        {
            // Ignore access errors
        }
        
        // Quick size calculation (stop at threshold for performance)
        var (size, exceededThreshold) = await GetDirectorySizeUntilThresholdAsync(path, SuspiciousSizeThreshold, cancellationToken);
        
        var status = SoftwareStatus.Normal;
        if (isSymlink)
        {
            status = SoftwareStatus.Migrated;
        }
        else if (!hasEntries)
        {
            status = SoftwareStatus.Empty;
        }
        else if (!exceededThreshold && size <= SuspiciousSizeThreshold)
        {
            status = SoftwareStatus.Suspicious;
        }
        
        // Use -1 to indicate size exceeded threshold
        var displaySize = exceededThreshold ? -1 : size;
        
        return new SoftwareInfo
        {
            Name = GetDirectoryName(path),
            InstallLocation = path,
            SizeBytes = displaySize,
            IsSymlink = isSymlink,
            Status = status,
            SuspiciousChecked = false
        };
    }
    
    private async Task<(long size, bool exceededThreshold)> GetDirectorySizeUntilThresholdAsync(string path, long threshold, CancellationToken cancellationToken)
    {
        long size = 0;
        var exceeded = false;
        
        try
        {
            var files = _fileSystem.GetFiles(path, "*", true);
            
            foreach (var file in files)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                try
                {
                    size += _fileSystem.GetFileSize(file);
                    
                    if (size > threshold)
                    {
                        exceeded = true;
                        break;
                    }
                }
                catch
                {
                    // Ignore files that can't be accessed
                }
            }
        }
        catch
        {
            // Ignore scan errors
        }
        
        return (size, exceeded);
    }
    
    private string NormalizePath(string path)
    {
        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
    
    private string GetDirectoryName(string path)
    {
        var normalized = NormalizePath(path);
        return Path.GetFileName(normalized) ?? path;
    }
    
    private void OnProgressChanged(string currentDirectory, int scanned, int total, int found)
    {
        ProgressChanged?.Invoke(this, new ScanProgressEventArgs
        {
            CurrentDirectory = currentDirectory,
            DirectoriesScanned = scanned,
            TotalDirectories = total,
            ItemsFound = found,
            ProgressPercent = total > 0 ? (int)((double)scanned / total * 100) : 0
        });
    }
    
    #endregion
}