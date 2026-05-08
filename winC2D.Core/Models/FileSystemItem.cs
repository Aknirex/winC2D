using CommunityToolkit.Mvvm.ComponentModel;

namespace winC2D.Core.Models;

/// <summary>
/// Generic file system item representing a file or directory in the browser.
/// Replaces the specialised <see cref="SoftwareInfo"/> for the unified Explorer view.
/// </summary>
public partial class FileSystemItem : ObservableObject
{
    /// <summary>File or directory name (without path).</summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>Full, absolute path to the item.</summary>
    [ObservableProperty]
    private string _fullPath = string.Empty;

    /// <summary>Whether this item is a directory (true) or a file (false).</summary>
    [ObservableProperty]
    private bool _isDirectory;

    /// <summary>Whether the user has selected this item for migration.</summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Size in bytes. For directories this is the recursive total after scanning;
    /// 0 means not yet measured or truly empty.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SizeText))]
    private long _sizeBytes;

    /// <summary>Whether the size has been calculated (true) or is unknown (false).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SizeText))]
    private bool _sizeChecked;

    /// <summary>Whether this item is a reparse point / symbolic link.</summary>
    [ObservableProperty]
    private bool _isSymlink;

    /// <summary>Current migration-related status of the item.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SizeText))]
    private FileSystemItemStatus _status = FileSystemItemStatus.Normal;

    /// <summary>Extension-based type description (e.g. "应用程序", "文件夹", "文本文档").</summary>
    [ObservableProperty]
    private string _typeText = string.Empty;

    /// <summary>
    /// Human-readable size string.
    /// </summary>
    public string SizeText
    {
        get
        {
            if (!SizeChecked)
                return "…";

            if (SizeBytes <= 0)
                return Status == FileSystemItemStatus.Empty ? "0 KB" : "—";

            if (SizeBytes < 1024)
                return $"{SizeBytes} B";

            if (SizeBytes < 1024 * 1024)
                return $"{Math.Max(1, SizeBytes / 1024)} KB";

            if (SizeBytes < 1024L * 1024 * 1024)
                return $"{SizeBytes / (1024 * 1024)} MB";

            return $"{SizeBytes / (1024L * 1024 * 1024):F1} GB";
        }
    }
}
