using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Wpf.Ui.Appearance;
using winC2D.Infrastructure.Localization;

namespace winC2D.App.ViewModels;

/// <summary>
/// View model for settings
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<SettingsViewModel> _logger;
    
    [ObservableProperty]
    private string _programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
    
    [ObservableProperty]
    private string _programFilesX86Path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
    
    [ObservableProperty]
    private bool _useCustomX86Path;
    
    [ObservableProperty]
    private string _selectedLanguage = "en";

    partial void OnSelectedLanguageChanged(string value)
    {
        // Apply language immediately so the sidebar and all views update in real-time
        if (!string.IsNullOrEmpty(value))
            _localizationService.SetLanguage(value);
    }

    [ObservableProperty]
    private bool _isDarkTheme = true; // default matches App.xaml Theme="Dark"

    partial void OnIsDarkThemeChanged(bool value)
    {
        ApplicationThemeManager.Apply(
            value ? ApplicationTheme.Dark : ApplicationTheme.Light);
    }
    
    public SettingsViewModel(
        ILocalizationService localizationService,
        ILogger<SettingsViewModel> logger)
    {
        _localizationService = localizationService;
        _logger = logger;
        
        SelectedLanguage = localizationService.CurrentLanguage;
    }
    
    /// <summary>
    /// Available languages
    /// </summary>
    public IEnumerable<LanguageInfo> AvailableLanguages => _localizationService.AvailableLanguages;
    
    /// <summary>
    /// Save settings
    /// </summary>
    [RelayCommand]
    private void SaveSettings()
    {
        _logger.LogInformation("Saving settings...");
        // Language is applied immediately via OnSelectedLanguageChanged; nothing extra needed.
        // TODO: Save other settings (paths etc.) to configuration
        _logger.LogInformation("Settings saved successfully");
    }
    
    /// <summary>
    /// Reset settings to defaults
    /// </summary>
    [RelayCommand]
    private void ResetSettings()
    {
        _logger.LogInformation("Resetting settings to defaults...");
        
        ProgramFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        ProgramFilesX86Path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        UseCustomX86Path = false;
        SelectedLanguage = "en";
        
        _localizationService.SetLanguage("en");
        
        _logger.LogInformation("Settings reset to defaults");
    }
    
    /// <summary>
    /// Open Windows Storage settings
    /// </summary>
    [RelayCommand]
    private void OpenWindowsStorage()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ms-settings:storagesense",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open Windows Storage settings");
        }
    }
    
    /// <summary>
    /// Browse for Program Files path
    /// </summary>
    [RelayCommand]
    private void BrowseProgramFiles()
    {
        // TODO: Implement folder browser dialog
        _logger.LogInformation("Browse for Program Files path");
    }
    
    /// <summary>
    /// Browse for Program Files (x86) path
    /// </summary>
    [RelayCommand]
    private void BrowseProgramFilesX86()
    {
        // TODO: Implement folder browser dialog
        _logger.LogInformation("Browse for Program Files (x86) path");
    }
}