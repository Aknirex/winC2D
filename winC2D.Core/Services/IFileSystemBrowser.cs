using winC2D.Core.Models;

namespace winC2D.Core.Services;

/// <summary>
/// Service for browsing the file system and scanning directory sizes.
/// Provides the data layer for the Explorer-style UI.
/// </summary>
public interface IFileSystemBrowser
{
    // ── Directory enumeration ────────────────────────────────────────────

    /// <summary>
    /// Get the contents (files + directories) of the specified path.
    /// </summary>
    /// <param name="path">Absolute directory path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of file system items in the directory, sorted (dirs first, then by name).</returns>
    Task<IReadOnlyList<FileSystemItem>> GetDirectoryContentsAsync(
        string path,
        CancellationToken ct = default);

    // ── Size scanning ────────────────────────────────────────────────────

    /// <summary>
    /// Stream-scans the sizes of the given items, yielding updated items as
    /// each one finishes. Uses the size cache to avoid redundant calculations.
    /// </summary>
    /// <param name="items">Items whose sizes should be calculated.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="ct">Cancellation token.</param>
    IAsyncEnumerable<FileSystemItem> ScanSizesAsync(
        IReadOnlyList<FileSystemItem> items,
        IProgress<ScanProgressReport>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Recalculate the precise size for a single item and update its status.
    /// Writes the new value into the size cache.
    /// </summary>
    Task<FileSystemItem> RecalculateSizeAsync(
        FileSystemItem item,
        CancellationToken ct = default);

    // ── Drives ────────────────────────────────────────────────────────────

    /// <summary>
    /// Get all available fixed and network drives on the system.
    /// </summary>
    Task<IReadOnlyList<DriveItem>> GetDrivesAsync();

    // ── Quick access ─────────────────────────────────────────────────────

    /// <summary>
    /// Get all quick-access items (pinned paths).
    /// Loads from persisted JSON on first call, caches in memory after.
    /// </summary>
    Task<IReadOnlyList<QuickAccessItem>> GetQuickAccessItemsAsync();

    /// <summary>
    /// Add a path to the quick-access list and persist to disk.
    /// If the path already exists, it is moved to the top (sort order 0).
    /// </summary>
    Task AddQuickAccessItemAsync(QuickAccessItem item);

    /// <summary>
    /// Remove a path from the quick-access list and persist to disk.
    /// </summary>
    Task RemoveQuickAccessItemAsync(string path);

    // ── Path helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Check whether a directory exists at the given path.
    /// </summary>
    bool DirectoryExists(string path);

    /// <summary>
    /// Get the parent directory path, or null if at the root.
    /// </summary>
    string? GetParentPath(string path);
}
