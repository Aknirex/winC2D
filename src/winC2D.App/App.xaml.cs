using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using winC2D.Core.Services;
using winC2D.Infrastructure;
using winC2D.Infrastructure.Localization;
using winC2D.App.ViewModels;
using winC2D.App.Views;

namespace winC2D.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IServiceProvider? _serviceProvider;
    private ILogger<App>? _logger;
    
    /// <summary>
    /// Service provider for dependency injection
    /// </summary>
    public static IServiceProvider Services => 
        ((App)Current)._serviceProvider 
        ?? throw new InvalidOperationException("Services not initialized");
    
    public App()
    {
        // Configure dependency injection
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        
        // Add winC2D services
        services.AddWinC2DServices();
        
        // Add ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<SoftwareMigrationViewModel>();
        services.AddSingleton<AppDataMigrationViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<LogViewModel>();
        
        // Add Views
        services.AddSingleton<MainWindow>();
        services.AddSingleton<SoftwareMigrationView>();
        services.AddSingleton<AppDataMigrationView>();
        services.AddSingleton<SettingsView>();
        services.AddSingleton<LogView>();
        
        _serviceProvider = services.BuildServiceProvider();
    }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        _logger = _serviceProvider?.GetRequiredService<ILogger<App>>();
        _logger?.LogInformation("Application starting...");
        
        // Check for administrator privileges
        if (!IsRunningAsAdministrator())
        {
            _logger?.LogWarning("Not running as administrator. Some features may not work.");
            ShowAdministratorWarning();
        }
        
        // Load language preference
        var localizationService = _serviceProvider?.GetRequiredService<ILocalizationService>();
        _logger?.LogInformation("Current language: {Language}", localizationService?.CurrentLanguage);
        
        // Show main window
        var mainWindow = _serviceProvider?.GetRequiredService<MainWindow>();
        mainWindow?.Show();
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.LogInformation("Application exiting...");
        base.OnExit(e);
    }
    
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.LogCritical(e.Exception, "Unhandled exception occurred");
        
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nPlease check the logs for details.",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        
        // Mark as handled to prevent crash
        e.Handled = true;
    }
    
    private static bool IsRunningAsAdministrator()
    {
        try
        {
            using var identity = new System.Security.Principal.WindowsIdentity(
                System.Security.Principal.WindowsIdentity.GetCurrent().Token);
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
    
    private static void ShowAdministratorWarning()
    {
        MessageBox.Show(
            "This application requires administrator privileges to migrate software.\n\n" +
            "Please restart the application as administrator.",
            "Administrator Privileges Required",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }
}