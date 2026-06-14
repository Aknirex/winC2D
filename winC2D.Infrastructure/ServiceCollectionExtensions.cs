using Microsoft.Extensions.DependencyInjection;
using winC2D.Core.FileSystem;
using winC2D.Core.Services;
using winC2D.Infrastructure.FileSystem;
using winC2D.Infrastructure.Localization;
using winC2D.Infrastructure.Services;

namespace winC2D.Infrastructure;

/// <summary>
/// Extension methods for configuring services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add winC2D infrastructure services
    /// </summary>
    public static IServiceCollection AddWinC2DServices(this IServiceCollection services)
    {
        // File system
        services.AddSingleton<IFileSystem, Infrastructure.FileSystem.FileSystem>();
        
        // Services
        services.AddSingleton<ISymlinkManager, SymlinkManager>();
        services.AddSingleton<IRollbackManager, RollbackManager>();
        services.AddSingleton<ISizeCacheService, SizeCacheService>();
        services.AddSingleton<ISoftwareScanner, SoftwareScanner>();
        services.AddSingleton<IMigrationPreflightService, MigrationPreflightService>();
        services.AddSingleton<IMigrationTaskStore, JsonMigrationTaskStore>();
        services.AddSingleton<IMigrationEngine, MigrationEngine>();
        services.AddSingleton<IFileSystemBrowser, FileSystemBrowser>();
        
        // Localization
        services.AddSingleton<ILocalizationService, LocalizationService>();
        
        return services;
    }
}
