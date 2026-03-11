using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace winC2D.Mcp;

/// <summary>
/// Minimal MCP stdio POC — verifies that WinExe + redirected stdin/stdout
/// works correctly with the MCP SDK before writing real tools.
///
/// Usage:  winC2D.exe --mcp-poc
/// Test:   npx @modelcontextprotocol/inspector winC2D.exe --mcp-poc
/// </summary>
public static class McpPoc
{
    public static async Task RunAsync(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // CRITICAL: All logs must go to stderr — stdout is the MCP JSON-RPC channel
        builder.Logging.AddConsole(o =>
            o.LogToStandardErrorThreshold = LogLevel.Trace);
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        builder.Services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new() { Name = "winC2D", Version = "poc-0.1" };
            })
            .WithStdioServerTransport()
            .WithTools<PocTools>();

        await builder.Build().RunAsync();
    }
}

/// <summary>
/// Minimal tool set for POC validation.
/// NOTE: Must NOT be static — MCP SDK requires instantiatable types for WithTools generic.
/// </summary>
[McpServerToolType]
internal sealed class PocTools
{
    [McpServerTool, Description("Echo test. Returns the input string. Verifies MCP stdio channel is working.")]
    public static string Echo([Description("Any string to echo back")] string message)
        => $"winC2D echo: {message}";

    [McpServerTool, Description(
        "Returns current privilege level. " +
        "Administrator or DeveloperMode = full migration capability. " +
        "Restricted = scan only, migration requires elevated privileges.")]
    public static string GetPrivilegeStatus()
    {
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);

        if (principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
            return """{"privilegeLevel":"Administrator","canMigrate":true,"canScan":true}""";

        if (IsDeveloperModeEnabled())
            return """{"privilegeLevel":"DeveloperMode","canMigrate":true,"canScan":true}""";

        return """
            {
              "privilegeLevel": "Restricted",
              "canMigrate": false,
              "canScan": true,
              "error": "INSUFFICIENT_PRIVILEGES",
              "resolutionOptions": [
                { "id": "run_as_admin",    "description": "Right-click your Agent host (e.g. Claude Desktop) and select 'Run as administrator'." },
                { "id": "developer_mode",  "description": "Enable Windows Developer Mode: Settings → System → Developer Options → Developer Mode." },
                { "id": "gsudo",           "description": "Install gsudo via: winget install gerardog.gsudo  Then change your MCP config to: { \"command\": \"gsudo\", \"args\": [\"winC2D.exe\", \"--mcp\"] }", "url": "https://github.com/gerardog/gsudo" }
              ]
            }
            """;
    }

    private static bool IsDeveloperModeEnabled()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock");
            return key?.GetValue("AllowDevelopmentWithoutDevLicense") is 1;
        }
        catch { return false; }
    }
}

