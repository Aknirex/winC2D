using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using winC2D.Core.FileSystem;
using winC2D.Core.Services;
using winC2D.Infrastructure.Services;
using winC2D.Mcp;

// ── MCP Transport Note ──────────────────────────────────────
// The MCP protocol uses JSON-RPC 2.0 over stdin/stdout.
// Diagnostic logging MUST go to stderr; stdout is reserved
// for JSON-RPC protocol messages only.
// ────────────────────────────────────────────────────────────

var services = new ServiceCollection();

// Logging to stderr (stdout is for JSON-RPC protocol)
services.AddLogging(b => b
    .AddConsole()
    .SetMinimumLevel(LogLevel.Warning));

// Core services
services.AddSingleton<IFileSystem, winC2D.Infrastructure.FileSystem.FileSystem>();
services.AddSingleton<ISymlinkManager, SymlinkManager>();
services.AddSingleton<IRollbackManager, RollbackManager>();
services.AddSingleton<IMigrationPreflightService, MigrationPreflightService>();
services.AddSingleton<IMigrationTaskStore, JsonMigrationTaskStore>();
services.AddSingleton<ISoftwareScanner, SoftwareScanner>();
services.AddSingleton<ISizeCacheService, SizeCacheService>();

// Migration engine
services.AddSingleton<IMigrationEngine, MigrationEngine>();

// MCP server components
services.AddSingleton<McpToolRegistry>();
services.AddSingleton<McpServer>();

var provider = services.BuildServiceProvider();

var logger = provider.GetRequiredService<ILogger<McpServer>>();
logger.LogInformation("winC2D MCP server v1.0.0 starting");

var server = provider.GetRequiredService<McpServer>();
var cts = new CancellationTokenSource();

// Handle graceful shutdown on Ctrl+C
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    logger.LogInformation("Shutdown signal received");
};

try
{
    await server.RunAsync(cts.Token);
    return 0;
}
catch (OperationCanceledException)
{
    return 0;
}
catch (Exception ex)
{
    logger.LogError(ex, "Fatal MCP server error");
    return 1;
}
