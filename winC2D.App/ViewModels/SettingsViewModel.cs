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

    /// <summary>
    /// Whether to show expert/dangerous settings (Program File Paths)
    /// </summary>
    [ObservableProperty]
    private bool _showExpertSettings;
    
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

        // Subscribe to language changes so localized labels update reactively
        _localizationService.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        NotifyLocalizationChanged();
    }

    private void NotifyLocalizationChanged()
    {
        OnPropertyChanged(nameof(L_Header));
        OnPropertyChanged(nameof(L_Language));
        OnPropertyChanged(nameof(L_SelectLanguage));
        OnPropertyChanged(nameof(L_Appearance));
        OnPropertyChanged(nameof(L_DarkMode));
        OnPropertyChanged(nameof(L_ProgramFiles));
        OnPropertyChanged(nameof(L_ProgramFilesLabel));
        OnPropertyChanged(nameof(L_ProgramFilesX86Label));
        OnPropertyChanged(nameof(L_Browse));
        OnPropertyChanged(nameof(L_UseCustomX86));
        OnPropertyChanged(nameof(L_WindowsStorage));
        OnPropertyChanged(nameof(L_OpenWinStorage));
        OnPropertyChanged(nameof(L_Reset));
        OnPropertyChanged(nameof(L_ExpertMode));
        OnPropertyChanged(nameof(L_ExpertModeDesc));
    }

    // ── Localized labels ──────────────────────────────────────────────
    public string L_Header            => _localizationService.GetString("Settings.Header");
    public string L_Language          => _localizationService.GetString("Settings.Language");
    public string L_SelectLanguage    => _localizationService.GetString("Settings.SelectLanguage");
    public string L_Appearance        => _localizationService.GetString("Settings.Appearance");
    public string L_DarkMode          => _localizationService.GetString("Settings.DarkMode");
    public string L_ProgramFiles      => _localizationService.GetString("Settings.ProgramFiles");
    public string L_ProgramFilesLabel => _localizationService.GetString("Settings.ProgramFilesLabel");
    public string L_ProgramFilesX86Label => _localizationService.GetString("Settings.ProgramFilesX86Label");
    public string L_Browse            => _localizationService.GetString("Settings.Browse");
    public string L_UseCustomX86      => _localizationService.GetString("Settings.UseCustomX86");
    public string L_WindowsStorage    => _localizationService.GetString("Settings.WindowsStorage");
    public string L_OpenWinStorage    => _localizationService.GetString("Settings.OpenWinStorage");
    public string L_Reset             => _localizationService.GetString("Settings.Reset");
    public string L_ExpertMode        => _localizationService.GetString("Settings.ExpertMode");
    public string L_ExpertModeDesc    => _localizationService.GetString("Settings.ExpertModeDesc");
    
    /// <summary>
    /// Available languages
    /// </summary>
    public IEnumerable<LanguageInfo> AvailableLanguages => _localizationService.AvailableLanguages;
    
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
        ShowExpertSettings = false;
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