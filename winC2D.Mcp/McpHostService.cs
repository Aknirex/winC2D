using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using winC2D.Mcp.Tools;
using winC2D.Infrastructure;

namespace winC2D.Mcp;

/// <summary>
/// Entry point for the MCP server mode (winC2D.exe --mcp).
///
/// Design rules:
///   - stdout is the MCP JSON-RPC channel — NOTHING else may write to it.
///   - stderr is for logs (see LogToStandardErrorThreshold below).
///   - No WPF Dispatcher, no windows, no GUI resources are initialised.
///   - All winC2D.Core / Infrastructure services are registered via AddWinC2DServices().
/// </summary>
public static class McpHostService
{
    /// <summary>Application name reported to MCP clients.</summary>
    public const string ServerName = "winC2D";

    /// <summary>Reflects the assembly version at build time.</summary>
    public static readonly string ServerVersion =
        typeof(McpHostService).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    public static async Task RunAsync(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // ── Logging ──────────────────────────────────────────────────────────
        // ALL log output must go to stderr so it does not corrupt the JSON-RPC
        // stream on stdout.
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(o =>
            o.LogToStandardErrorThreshold = LogLevel.Trace);
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        // ── Core / Infrastructure services ───────────────────────────────────
        // Same registrations as the GUI — no ViewModels, no Views.
        builder.Services.AddWinC2DServices();

        // ── MCP server ───────────────────────────────────────────────────────
        builder.Services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new() { Name = ServerName, Version = ServerVersion };
            })
            .WithStdioServerTransport()
            .WithTools<PrivilegeStatusTool>()
            .WithTools<DiskInfoTool>()
            .WithTools<ScanTool>()
            .WithTools<MigrationTool>();

        await builder.Build().RunAsync();
    }
}
