using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using winC2D.Cli;
using winC2D.Core.Services;
using winC2D.Infrastructure;
using winC2D.Infrastructure.Localization;
using winC2D.App.Converters;
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
    }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        // Agent CLI mode: no windows, stdout is machine-readable JSON.
        if (e.Args.Any(a => string.Equals(a, "--cli", StringComparison.OrdinalIgnoreCase)))
        {
            var previousContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            var exitCode = RunCliAsync(e.Args).GetAwaiter().GetResult();
            SynchronizationContext.SetSynchronizationContext(previousContext);
            Shutdown(exitCode);
            return;
        }

        base.OnStartup(e);

        _serviceProvider = BuildGuiServices();
        
        _logger = _serviceProvider?.GetRequiredService<ILogger<App>>();
        _logger?.LogInformation("Application starting...");
        
        // Check for administrator privileges — auto-elevate
        // Skip elevation when debugging or when --no-elevate flag is present
        if (!IsRunningAsAdministrator() && !System.Diagnostics.Debugger.IsAttached && !e.Args.Contains("--no-elevate"))
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

    private static ServiceProvider BuildGuiServices()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddWinC2DServices();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<SoftwareMigrationViewModel>();
        services.AddSingleton<AppDataMigrationViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<LogViewModel>();
        services.AddSingleton<FileSystemBrowserViewModel>();

        services.AddSingleton<MainWindow>();
        services.AddSingleton<SoftwareMigrationView>();
        services.AddSingleton<AppDataMigrationView>();
        services.AddSingleton<SettingsView>();
        services.AddSingleton<LogView>();
        services.AddSingleton<AboutView>();
        services.AddSingleton<FileSystemBrowserView>();

        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildCliServices()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        services.AddWinC2DServices();

        return services.BuildServiceProvider();
    }

    private static async Task<int> RunCliAsync(string[] args)
    {
        using var services = BuildCliServices();
        var executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;

        return await CliApplication.RunAsync(
            args,
            services,
            Console.Out,
            Console.Error,
            BuildCliServices,
            executablePath);
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
