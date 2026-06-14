using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using winC2D.Infrastructure;

namespace winC2D.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        using var services = BuildServices();
        return await CliApplication.RunAsync(
            args,
            services,
            Console.Out,
            Console.Error,
            BuildServices,
            Environment.ProcessPath);
    }

    private static ServiceProvider BuildServices()
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
}
