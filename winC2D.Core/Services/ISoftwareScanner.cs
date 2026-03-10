using winC2D.Core.Models;

namespace winC2D.Core.Services;

/// <summary>
/// Interface for scanning installed software
/// </summary>
public interface ISoftwareScanner
{
    /// <summary>
    /// Event raised when scan progress changes
    /// </summary>
    event EventHandler<ScanProgressEventArgs>? ProgressChanged;
    
    /// <summary>
    /// Scan for installed software in specified directories
    /// </summary>
    /// <param name="directories">Directories to scan</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of found software</returns>
    Task<IEnumerable<SoftwareInfo>> ScanAsync(IEnumerable<string> directories, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Scan for installed software using default directories
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of found software</returns>
    Task<IEnumerable<SoftwareInfo>> ScanAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get default scan directories (Program Files, Program Files (x86))
    /// </summary>
    /// <returns>List of default directories</returns>
    IEnumerable<string> GetDefaultScanDirectories();
    
    /// <summary>
    /// Check a suspicious directory for more details
    /// </summary>
    /// <param name="software">Software to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated software info</returns>
    Task<SoftwareInfo> CheckSuspiciousAsync(SoftwareInfo software, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Calculate directory size
    /// </summary>
    /// <param name="path">Directory path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Size in bytes</returns>
    Task<long> CalculateSizeAsync(string path, CancellationToken cancellationToken = default);
}

/// <summary>
/// Event arguments for scan progress updates
/// </summary>
public class ScanProgressEventArgs : EventArgs
{
    /// <summary>
    /// Current directory being scanned
    /// </summary>
    public string CurrentDirectory { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of directories scanned so far
    /// </summary>
    public int DirectoriesScanned { get; set; }
    
    /// <summary>
    /// Total directories to scan (if known)
    /// </summary>
    public int? TotalDirectories { get; set; }
    
    /// <summary>
    /// Number of software items found
    /// </summary>
    public int ItemsFound { get; set; }
    
    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int ProgressPercent { get; set; }
}