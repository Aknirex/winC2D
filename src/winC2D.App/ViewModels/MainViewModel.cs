using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using winC2D.Infrastructure.Localization;

namespace winC2D.App.ViewModels;

/// <summary>
/// Main view model for the application
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<MainViewModel> _logger;
    
    [ObservableProperty]
    private string _title = "winC2D - Windows Storage Migration Assistant";
    
    [ObservableProperty]
    private string _statusMessage = "Ready";
    
    [ObservableProperty]
    private int _selectedNavigationIndex;
    
    [ObservableProperty]
    private bool _isBusy;
    
    public MainViewModel(
        ILocalizationService localizationService,
        ILogger<MainViewModel> logger)
    {
        _localizationService = localizationService;
        _logger = logger;
        
        // Subscribe to language changes
        _localizationService.LanguageChanged += OnLanguageChanged;
    }
    
    /// <summary>
    /// Available languages
    /// </summary>
    public IEnumerable<LanguageInfo> AvailableLanguages => _localizationService.AvailableLanguages;
    
    /// <summary>
    /// Current language code
    /// </summary>
    public string CurrentLanguage => _localizationService.CurrentLanguage;
    
    /// <summary>
    /// Change the application language
    /// </summary>
    [RelayCommand]
    public void ChangeLanguage(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
            return;
        
        _logger.LogInformation("Changing language to: {Language}", languageCode);
        _localizationService.SetLanguage(languageCode);
        
        // Update title with localized string
        Title = _localizationService.GetString("App.Title");
        StatusMessage = _localizationService.GetString("Status.Ready");
    }
    
    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        _logger.LogInformation("Language changed from {Previous} to {New}", 
            e.PreviousLanguage, e.NewLanguage);
        
        // Notify UI to update localized strings
        OnPropertyChanged(nameof(CurrentLanguage));
        OnPropertyChanged(nameof(AvailableLanguages));
    }
    
    /// <summary>
    /// Get a localized string
    /// </summary>
    public string GetString(string key)
    {
        return _localizationService.GetString(key);
    }
    
    /// <summary>
    /// Get a localized string with format arguments
    /// </summary>
    public string GetString(string key, params object[] args)
    {
        return _localizationService.GetString(key, args);
    }
}