using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using winC2D.Infrastructure.Localization;

namespace winC2D.App.Views;

/// <summary>
/// Interaction logic for AboutView.xaml
/// </summary>
public partial class AboutView : UserControl, INotifyPropertyChanged
{
    private readonly ILocalizationService _localizationService;

    public AboutView(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
        InitializeComponent();
        DataContext = this;
        
        // Subscribe to language changes
        _localizationService.LanguageChanged += (_, _) => NotifyAllPropertiesChanged();
    }

    // ── Localized properties ──────────────────────────────────────────────
    public string L_Header => _localizationService.GetString("About.Header");
    public string L_Title => _localizationService.GetString("About.Title");
    public string L_Version => _localizationService.GetString("About.Version");
    public string L_License => _localizationService.GetString("About.License");
    public string L_Author => _localizationService.GetString("About.Author");
    public string L_Description => _localizationService.GetString("About.Description");
    public string L_LinksHeader => _localizationService.GetString("About.LinksHeader");
    public string L_Repository => _localizationService.GetString("About.Repository");
    public string L_Documentation => _localizationService.GetString("About.Documentation");
    public string L_IssueTracker => _localizationService.GetString("About.IssueTracker");
    public string L_DisclaimerTitle => _localizationService.GetString("About.DisclaimerTitle");
    public string L_DisclaimerText => _localizationService.GetString("About.DisclaimerText");

    /// <summary>
    /// Dynamic documentation URI based on current language.
    /// Maps language codes to GitHub wiki language variants.
    /// </summary>
    public string DocumentationUri
    {
        get
        {
            var currentLang = _localizationService.CurrentLanguage;
            
            return currentLang switch
            {
                "zh-CN"  => "https://github.com/Aknirex/winC2D/wiki/%E4%B8%AD%E6%96%87%E6%96%87%E6%A1%A3",
                "zh-Hant" => "https://github.com/Aknirex/winC2D/wiki/%E4%B8%AD%E6%96%87%E6%96%87%E6%A1%A3",
                "ja"     => "https://github.com/Aknirex/winC2D/wiki/Japanese-Documentation",
                "ko"     => "https://github.com/Aknirex/winC2D/wiki/Korean-Documentation",
                "ru"     => "https://github.com/Aknirex/winC2D/wiki/Russian-Documentation",
                "pt-BR"  => "https://github.com/Aknirex/winC2D/wiki/Portuguese-Documentation",
                _        => "https://github.com/Aknirex/winC2D/wiki"  // Default to English
            };
        }
    }

    public string RepositoryUri => "https://github.com/Aknirex/winC2D";

    private void NotifyAllPropertiesChanged()
    {
        // Notify all localized properties that their values have changed
        OnPropertyChanged(nameof(L_Header));
        OnPropertyChanged(nameof(L_Title));
        OnPropertyChanged(nameof(L_Version));
        OnPropertyChanged(nameof(L_License));
        OnPropertyChanged(nameof(L_Author));
        OnPropertyChanged(nameof(L_Description));
        OnPropertyChanged(nameof(L_LinksHeader));
        OnPropertyChanged(nameof(L_Repository));
        OnPropertyChanged(nameof(L_Documentation));
        OnPropertyChanged(nameof(L_IssueTracker));
        OnPropertyChanged(nameof(L_DisclaimerTitle));
        OnPropertyChanged(nameof(L_DisclaimerText));
        OnPropertyChanged(nameof(DocumentationUri));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true
        });
        e.Handled = true;
    }
}

