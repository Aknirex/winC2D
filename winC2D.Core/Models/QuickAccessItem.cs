using CommunityToolkit.Mvvm.ComponentModel;

namespace winC2D.Core.Models;

/// <summary>
/// Represents a pinned quick-access entry in the Explorer sidebar.
/// Persisted to JSON in the user's AppData folder.
/// </summary>
public partial class QuickAccessItem : ObservableObject
{
    /// <summary>Human-readable display name shown in the sidebar.</summary>
    [ObservableProperty]
    private string _displayName = string.Empty;

    /// <summary>Absolute file system path this entry points to.</summary>
    [ObservableProperty]
    private string _path = string.Empty;

    /// <summary>
    /// WPF-UI SymbolIcon name for the sidebar icon (e.g. "FolderOpen24", "Document24").
    /// </summary>
    [ObservableProperty]
    private string _iconGlyph = "Folder24";

    /// <summary>Whether this entry is currently pinned (visible in the quick-access list).</summary>
    [ObservableProperty]
    private bool _isPinned = true;

    /// <summary>Sort order (lower = first).</summary>
    [ObservableProperty]
    private int _sortOrder;
}
