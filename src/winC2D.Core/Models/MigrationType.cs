namespace winC2D.Core.Models;

/// <summary>
/// Represents the type of migration operation
/// </summary>
public enum MigrationType
{
    /// <summary>
    /// Software migration from Program Files
    /// </summary>
    Software,
    
    /// <summary>
    /// AppData folder migration
    /// </summary>
    AppData,
    
    /// <summary>
    /// User folder migration (Documents, Pictures, etc.)
    /// </summary>
    UserFolder
}