using winC2D.Core.Models;

namespace winC2D.Core.Services;

/// <summary>
/// Immutable progress snapshot pushed to the UI during a scan.
/// </summary>
public sealed record ScanProgressReport(
    /// <summary>Directory whose size is currently being calculated.</summary>
    string CurrentDirectory,
    /// <summary>How many items have been yielded so far.</summary>
    int ItemsFound,
    /// <summary>Total number of top-level directories to process (known after enumeration phase).</summary>
    int TotalDirectories,
    /// <summary>0–100.</summary>
    int ProgressPercent);

/// <summary>
/// Interface for scanning installed software.
/// </summary>
public interface ISoftwareScanner
{
    /// <summary>
    /// Get default scan directories (Program Files, Program Files x86).
    /// </summary>
    IEnumerable<string> GetDefaultScanDirectories();

    /// <summary>
    /// Stream software items from <paramref name="directories"/> as they are discovered.
    /// Each item is fully built (size calculated, status set) before being yielded.
    /// Progress is reported via <paramref name="progress"/> after each item.
    /// </summary>
    IAsyncEnumerable<SoftwareInfo> ScanStreamAsync(
        IEnumerable<string> directories,
        IProgress<ScanProgressReport>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Same as <see cref="ScanStreamAsync(IEnumerable{string},IProgress{ScanProgressReport}?,CancellationToken)"/>
    /// but uses <see cref="GetDefaultScanDirectories"/>.
    /// </summary>
    IAsyncEnumerable<SoftwareInfo> ScanStreamAsync(
        IProgress<ScanProgressReport>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Precisely recalculate the size of one directory and update its status.
    /// Writes the new value into the size cache.
    /// </summary>
    Task<SoftwareInfo> RecalculateSizeAsync(
        SoftwareInfo software,
        CancellationToken cancellationToken = default);
}