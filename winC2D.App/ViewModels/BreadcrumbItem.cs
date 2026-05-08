namespace winC2D.App.ViewModels;

/// <summary>
/// Represents a single segment in the breadcrumb navigation bar.
/// </summary>
public class BreadcrumbItem
{
    /// <summary>Display name shown in the UI (e.g. "Program Files").</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Full path this segment represents.
    /// null for the root "This PC" node.
    /// </summary>
    public string? FullPath { get; init; }

    /// <summary>Zero-based index within the breadcrumb list.</summary>
    public int Index { get; init; }

    /// <summary>Whether this is the last (current) segment.</summary>
    public bool IsLast { get; init; }

    /// <summary>Whether this is the root "This PC" segment.</summary>
    public bool IsRoot => FullPath is null;
}
