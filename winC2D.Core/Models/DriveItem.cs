using CommunityToolkit.Mvvm.ComponentModel;

namespace winC2D.Core.Models;

/// <summary>
/// Lightweight representation of a logical drive shown in the Explorer sidebar.
/// </summary>
public partial class DriveItem : ObservableObject
{
    /// <summary>Drive letter + colon, e.g. "C:".</summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>Root path, e.g. "C:\".</summary>
    [ObservableProperty]
    private string _rootPath = string.Empty;

    /// <summary>Volume label, e.g. "系统盘" or "Data".</summary>
    [ObservableProperty]
    private string _label = string.Empty;

    /// <summary>Total capacity in bytes.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FreeSpaceText))]
    [NotifyPropertyChangedFor(nameof(UsagePercent))]
    private long _totalSize;

    /// <summary>Available free space in bytes.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FreeSpaceText))]
    [NotifyPropertyChangedFor(nameof(UsagePercent))]
    private long _freeSpace;

    /// <summary>Drive type string: "Fixed", "Network", etc.</summary>
    [ObservableProperty]
    private string _driveType = "Fixed";

    /// <summary>Whether the drive is ready for I/O.</summary>
    [ObservableProperty]
    private bool _isReady = true;

    /// <summary>Human-readable free / total text (e.g. "120 GB 可用 / 256 GB").</summary>
    public string FreeSpaceText
    {
        get
        {
            if (!IsReady) return "不可用";

            var free = FormatBytes(FreeSpace);
            var total = FormatBytes(TotalSize);
            return $"{free} 可用 / {total}";
        }
    }

    /// <summary>Disk usage percentage (0-100).</summary>
    public int UsagePercent
    {
        get
        {
            if (TotalSize <= 0 || !IsReady) return 0;
            var used = TotalSize - FreeSpace;
            if (used <= 0) return 0;
            return (int)Math.Clamp(used * 100L / TotalSize, 0, 100);
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024 * 1024)} MB";
        if (bytes < 1024L * 1024 * 1024 * 1024) return $"{bytes / (1024L * 1024 * 1024):F1} GB";
        return $"{bytes / (1024L * 1024 * 1024 * 1024):F2} TB";
    }
}
