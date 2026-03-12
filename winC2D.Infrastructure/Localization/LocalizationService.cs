using System.Globalization;

namespace winC2D.Infrastructure.Localization;

/// <summary>
/// Implementation of localization service using a built-in translation dictionary.
/// </summary>
public class LocalizationService : ILocalizationService
{
    private string _currentLangCode;
    
    /// <summary>
    /// Available languages
    /// </summary>
    public static readonly LanguageInfo[] SupportedLanguages = new[]
    {
        new LanguageInfo { Code = "en",    NativeName = "English",             EnglishName = "English" },
        new LanguageInfo { Code = "zh-CN", NativeName = "简体中文",            EnglishName = "Simplified Chinese" },
        new LanguageInfo { Code = "zh-Hant",NativeName = "繁體中文",           EnglishName = "Traditional Chinese" },
        new LanguageInfo { Code = "ja",    NativeName = "日本語",              EnglishName = "Japanese" },
        new LanguageInfo { Code = "ko",    NativeName = "한국어",              EnglishName = "Korean" },
        new LanguageInfo { Code = "ru",    NativeName = "Русский",             EnglishName = "Russian" },
        new LanguageInfo { Code = "pt-BR", NativeName = "Português (Brasil)",  EnglishName = "Portuguese (Brazil)" }
    };
    
    public LocalizationService()
    {
        // Try to load saved language preference; fall back to system UI culture
        var saved = LoadLanguagePreference();
        _currentLangCode = !string.IsNullOrEmpty(saved) ? saved : "en";
        ApplyCulture(_currentLangCode);
    }
    
    /// <inheritdoc/>
    public string CurrentLanguage => _currentLangCode;
    
    /// <inheritdoc/>
    public IEnumerable<LanguageInfo> AvailableLanguages => SupportedLanguages;
    
    /// <inheritdoc/>
    public string GetString(string key) => Translations.Get(_currentLangCode, key);
    
    /// <inheritdoc/>
    public string GetString(string key, params object[] args)
    {
        try
        {
            var format = GetString(key);
            return string.Format(CultureInfo.CurrentCulture, format, args);
        }
        catch
        {
            return key;
        }
    }
    
    /// <inheritdoc/>
    public void SetLanguage(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
            return;
        
        var previousLanguage = _currentLangCode;
        
        try
        {
            _currentLangCode = languageCode;
            ApplyCulture(languageCode);
            SaveLanguagePreference(languageCode);
            
            LanguageChanged?.Invoke(this, new LanguageChangedEventArgs
            {
                PreviousLanguage = previousLanguage,
                NewLanguage      = languageCode
            });
        }
        catch
        {
            // Revert on error
            _currentLangCode = previousLanguage;
        }
    }
    
    /// <inheritdoc/>
    public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

    // ── helpers ──────────────────────────────────────────────────────────

    private static void ApplyCulture(string langCode)
    {
        try
        {
            var culture = new CultureInfo(langCode);
            // Set both UI culture (resource lookup) and format culture (number/date formatting).
            CultureInfo.CurrentCulture                = culture;
            CultureInfo.CurrentUICulture              = culture;
            CultureInfo.DefaultThreadCurrentCulture   = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
        catch
        {
            // Ignore invalid culture codes
        }
    }
    
    private static void SaveLanguagePreference(string languageCode)
    {
        try
        {
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "winC2D");
            Directory.CreateDirectory(configPath);
            File.WriteAllText(Path.Combine(configPath, "language.txt"), languageCode);
        }
        catch { /* Ignore */ }
    }
    
    private static string? LoadLanguagePreference()
    {
        try
        {
            var configFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "winC2D", "language.txt");
            return File.Exists(configFile) ? File.ReadAllText(configFile).Trim() : null;
        }
        catch { return null; }
    }
}
