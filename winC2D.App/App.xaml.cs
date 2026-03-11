using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using winC2D.Core.Services;
using winC2D.Infrastructure;
using winC2D.Infrastructure.Localization;
using winC2D.App.Converters;
using winC2D.App.ViewModels;
using winC2D.App.Views;
using winC2D.Mcp;

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
        services.AddSingleton<AboutView>();
        
        _serviceProvider = services.BuildServiceProvider();
    }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        // ── MCP server mode: AI agent entry point ────────────────────────────────
        if (e.Args.Contains("--mcp"))
        {
            // No windows, no WPF dispatcher — pure stdio JSON-RPC server.
            McpHostService.RunAsync(e.Args).GetAwaiter().GetResult();
            Shutdown(0);
            return;
        }

        // ── MCP POC mode: kept for debugging / channel verification ───────────
        if (e.Args.Contains("--mcp-poc"))
        {
            McpPoc.RunAsync(e.Args).GetAwaiter().GetResult();
            Shutdown(0);
            return;
        }

        base.OnStartup(e);
        
        _logger = _serviceProvider?.GetRequiredService<ILogger<App>>();
        _logger?.LogInformation("Application starting...");
        
        // Check for administrator privileges — auto-elevate
        if (!IsRunningAsAdministrator())
        {
            _logger?.LogWarning("Not running as administrator. Requesting elevation.");
            RequestElevation();
            // RequestElevation will shut down or let user cancel; if cancelled we continue without admin
        }
        
        // Load language preference
        var localizationService = _serviceProvider?.GetRequiredService<ILocalizationService>();
        _logger?.LogInformation("Current language: {Language}", localizationService?.CurrentLanguage);

        // Wire up static converters that need localization service
        if (localizationService is not null)
        {
            SoftwareStatusTextConverter.LocalizationService = localizationService;
            SizeCellTooltipConverter.LocalizationService    = localizationService;
            StatusCellTooltipConverter.LocalizationService  = localizationService;
        }
        
        // Show main window
        var mainWindow = _serviceProvider?.GetRequiredService<MainWindow>();
        mainWindow?.Show();
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.LogInformation("Application exiting...");
        // Persist the size cache so the next launch benefits from the measurements
        // collected during this session.
        try
        {
            _serviceProvider?.GetService<ISizeCacheService>()?.Save();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to save size cache on exit.");
        }
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

    /// <summary>
    /// Re-launch the current process with administrator (runas) elevation.
    /// If the user cancels the UAC prompt we continue running without admin.
    /// </summary>
    private void RequestElevation()
    {
        try
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
                return;

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName        = exePath,
                UseShellExecute = true,   // required for Verb to work
                Verb            = "runas" // triggers UAC
            };

            System.Diagnostics.Process.Start(psi);
            // Shut down this non-elevated instance
            Shutdown(0);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // Error 1223 = user clicked "No" on the UAC prompt — keep running without admin
            _logger?.LogWarning("User declined elevation. Running without administrator privileges.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to restart as administrator.");
        }
    }
}