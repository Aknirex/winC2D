namespace winC2D.Core.Models;

/// <summary>
/// Represents the status of a software item.
/// </summary>
public enum SoftwareStatus
{
    /// <summary>
    /// Normal directory with at least one executable; ready for migration.
    /// </summary>
    Normal,

    /// <summary>
    /// Already migrated – the install location is a symbolic link pointing to the
    /// new drive.
    /// </summary>
    Migrated,

    /// <summary>
    /// Reserved for future use: needs manual user review before migrating.
    /// NOT assigned by the scanner based on size.
    /// </summary>
    Suspicious,

    /// <summary>
    /// Directory exists but contains no files and no sub-directories.
    /// </summary>
    Empty,

    /// <summary>
    /// Directory has files/sub-directories but no executable was found.
    /// Likely the remnants of an already-uninstalled application.
    /// </summary>
    Residual
}

/// <summary>
/// Represents a directory found under Program Files.
/// </summary>
public class SoftwareInfo : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));

    private bool _isSelected;
    private long _sizeBytes;
    private bool _isSymlink;
    private SoftwareStatus _status;
    private bool _suspiciousChecked;

    /// <summary>
    /// Call this when the UI language changes so bound columns re-read localized text.
    /// </summary>
    public void NotifyStatusTextChanged()
    {
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(SizeText));
    }

    /// <summary>Directory name used as a display label.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Full path to the installation directory.</summary>
    public string InstallLocation { get; set; } = string.Empty;

    /// <summary>Whether the user selected this directory for migration.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    /// <summary>
    /// Measured size in bytes.
    /// 0  = empty or not yet measured.
    /// &gt;0 = precise size.
    /// </summary>
    public long SizeBytes
    {
        get => _sizeBytes;
        set
        {
            if (_sizeBytes == value) return;
            _sizeBytes = value;
            OnPropertyChanged(nameof(SizeBytes));
            OnPropertyChanged(nameof(SizeText));
        }
    }

    /// <summary>Whether this directory is a reparse point (symlink).</summary>
    public bool IsSymlink
    {
        get => _isSymlink;
        set
        {
            if (_isSymlink == value) return;
            _isSymlink = value;
            OnPropertyChanged(nameof(IsSymlink));
        }
    }

    /// <summary>Current status of the directory.</summary>
    public SoftwareStatus Status
    {
        get => _status;
        set
        {
            if (_status == value) return;
            _status = value;
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(SizeText));
        }
    }

    /// <summary>
    /// True when the directory has been fully inspected (size + exe presence checked).
    /// Always true after a scan because SoftwareScanner now does a precise calculation
    /// for every directory.
    /// </summary>
    public bool SuspiciousChecked
    {
        get => _suspiciousChecked;
        set
        {
            if (_suspiciousChecked == value) return;
            _suspiciousChecked = value;
            OnPropertyChanged(nameof(SuspiciousChecked));
        }
    }

    /// <summary>Human-readable size string.</summary>
    public string SizeText
    {
        get
        {
            if (SizeBytes <= 0)
                return Status == SoftwareStatus.Empty ? "0 KB" : "—";

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
