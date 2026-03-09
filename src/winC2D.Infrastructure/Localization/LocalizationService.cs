using System.Globalization;
using System.Resources;

namespace winC2D.Infrastructure.Localization;

/// <summary>
/// Implementation of localization service using resource files
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly ResourceManager _resourceManager;
    private CultureInfo _currentCulture;
    
    /// <summary>
    /// Available languages
    /// </summary>
    public static readonly LanguageInfo[] SupportedLanguages = new[]
    {
        new LanguageInfo { Code = "en", NativeName = "English", EnglishName = "English" },
        new LanguageInfo { Code = "zh-CN", NativeName = "简体中文", EnglishName = "Simplified Chinese" },
        new LanguageInfo { Code = "zh-Hant", NativeName = "繁體中文", EnglishName = "Traditional Chinese" },
        new LanguageInfo { Code = "ja", NativeName = "日本語", EnglishName = "Japanese" },
        new LanguageInfo { Code = "ko", NativeName = "한국어", EnglishName = "Korean" },
        new LanguageInfo { Code = "ru", NativeName = "Русский", EnglishName = "Russian" },
        new LanguageInfo { Code = "pt-BR", NativeName = "Português (Brasil)", EnglishName = "Portuguese (Brazil)" }
    };
    
    public LocalizationService()
    {
        _resourceManager = new ResourceManager("winC2D.Infrastructure.Resources.Strings", typeof(LocalizationService).Assembly);
        _currentCulture = CultureInfo.CurrentUICulture;
        
        // Try to load saved language preference
        var savedLanguage = LoadLanguagePreference();
        if (!string.IsNullOrEmpty(savedLanguage))
        {
            SetLanguage(savedLanguage);
        }
    }
    
    /// <inheritdoc/>
    public string CurrentLanguage => _currentCulture.Name;
    
    /// <inheritdoc/>
    public IEnumerable<LanguageInfo> AvailableLanguages => SupportedLanguages;
    
    /// <inheritdoc/>
    public string GetString(string key)
    {
        try
        {
            var value = _resourceManager.GetString(key, _currentCulture);
            return value ?? key;
        }
        catch
        {
            return key;
        }
    }
    
    /// <inheritdoc/>
    public string GetString(string key, params object[] args)
    {
        try
        {
            var format = GetString(key);
            return string.Format(_currentCulture, format, args);
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
        
        var previousLanguage = _currentCulture.Name;
        
        try
        {
            var culture = new CultureInfo(languageCode);
            _currentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            
            SaveLanguagePreference(languageCode);
            
            LanguageChanged?.Invoke(this, new LanguageChangedEventArgs
            {
                PreviousLanguage = previousLanguage,
                NewLanguage = languageCode
            });
        }
        catch
        {
            // Invalid culture, keep current
        }
    }
    
    /// <inheritdoc/>
    public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;
    
    /// <summary>
    /// Save language preference to settings
    /// </summary>
    private void SaveLanguagePreference(string languageCode)
    {
        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configPath = Path.Combine(appDataPath, "winC2D");
            Directory.CreateDirectory(configPath);
            
            var configFile = Path.Combine(configPath, "language.txt");
            File.WriteAllText(configFile, languageCode);
        }
        catch
        {
            // Ignore save errors
        }
    }
    
    /// <summary>
    /// Load language preference from settings
    /// </summary>
    private string? LoadLanguagePreference()
    {
        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configFile = Path.Combine(appDataPath, "winC2D", "language.txt");
            
            if (File.Exists(configFile))
            {
                return File.ReadAllText(configFile).Trim();
            }
        }
        catch
        {
            // Ignore load errors
        }
        
        return null;
    }
}