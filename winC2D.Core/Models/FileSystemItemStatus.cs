namespace winC2D.Core.Models;

/// <summary>
/// Status of a file system item for migration purposes.
/// </summary>
public enum FileSystemItemStatus
{
    /// <summary>Normal item, ready for migration.</summary>
    Normal,

    /// <summary>Already migrated – the item is a symbolic link pointing elsewhere.</summary>
    Migrated,

    /// <summary>Reserved for manual user review before migration.</summary>
    Suspicious,

    /// <summary>Directory or file is empty (zero bytes, no contents).</summary>
    Empty,

    /// <summary>Directory has contents but no meaningful data (e.g. residual folder).</summary>
    Residual,

    /// <summary>Access to the item was denied by the OS.</summary>
    AccessDenied,

    /// <summary>An error occurred while reading the item.</summary>
    Error
}
