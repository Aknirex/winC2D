namespace winC2D.Infrastructure.Localization;

/// <summary>
/// Interface for localization services
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Get a localized string by key
    /// </summary>
    /// <param name="key">String key</param>
    /// <returns>Localized string</returns>
    string GetString(string key);
    
    /// <summary>
    /// Get a localized string with format arguments
    /// </summary>
    /// <param name="key">String key</param>
    /// <param name="args">Format arguments</param>
    /// <returns>Formatted localized string</returns>
    string GetString(string key, params object[] args);
    
    /// <summary>
    /// Get current language code
    /// </summary>
    string CurrentLanguage { get; }
    
    /// <summary>
    /// Get available languages
    /// </summary>
    IEnumerable<LanguageInfo> AvailableLanguages { get; }
    
    /// <summary>
    /// Set the current language
    /// </summary>
    /// <param name="languageCode">Language code (e.g., "en", "zh-CN")</param>
    void SetLanguage(string languageCode);
    
    /// <summary>
    /// Event raised when language changes
    /// </summary>
    event EventHandler<LanguageChangedEventArgs>? LanguageChanged;
}

/// <summary>
/// Information about a language
/// </summary>
public class LanguageInfo
{
    /// <summary>
    /// Language code (e.g., "en", "zh-CN")
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Native name of the language
    /// </summary>
    public string NativeName { get; set; } = string.Empty;
    
    /// <summary>
    /// English name of the language
    /// </summary>
    public string EnglishName { get; set; } = string.Empty;
}

/// <summary>
/// Event arguments for language change
/// </summary>
public class LanguageChangedEventArgs : EventArgs
{
    /// <summary>
    /// Previous language code
    /// </summary>
    public string PreviousLanguage { get; set; } = string.Empty;
    
    /// <summary>
    /// New language code
    /// </summary>
    public string NewLanguage { get; set; } = string.Empty;
}