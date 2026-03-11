using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;

namespace winC2D.Mcp.Tools;

/// <summary>
/// MCP tool: get_privilege_status
/// Reports current privilege level and which operations are available.
/// Agents should call this before migrate_software.
/// No elevated privileges required.
/// </summary>
[McpServerToolType]
internal sealed class PrivilegeStatusTool
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [McpServerTool(Name = "get_privilege_status"), Description(
        "Returns the current privilege level of the MCP server process and which operations are available. " +
        "Always call this before migrate_software or rollback_migration to verify the server has sufficient privileges. " +
        "Privilege levels: " +
        "Administrator = full access (process is elevated); " +
        "DeveloperMode = full access (Windows Developer Mode is enabled, no elevation needed); " +
        "Restricted = scan and disk info only, migration is blocked. " +
        "No elevated privileges required.")]
    public static string GetPrivilegeStatus()
    {
        var level = PrivilegeChecker.GetLevel();
        var canMigrate = level != PrivilegeChecker.Level.Restricted;

        object result = level switch
        {
            PrivilegeChecker.Level.Administrator => new
            {
                privilegeLevel = "Administrator",
                canMigrate = true,
                canScan = true,
                details = "Running as administrator. All operations are available."
            },
            PrivilegeChecker.Level.DeveloperMode => new
            {
                privilegeLevel = "DeveloperMode",
                canMigrate = true,
                canScan = true,
                details = "Windows Developer Mode is enabled. Symlink creation does not require elevation. All operations are available."
            },
            _ => (object)new
            {
                privilegeLevel = "Restricted",
                canMigrate = false,
                canScan = true,
                details = "Running without elevated privileges and Developer Mode is not enabled. Scan and disk info are available; migration requires elevated privileges.",
                resolutionOptions = new object[]
                {
                    new
                    {
                        id = "run_as_admin",
                        title = "Run Agent host as administrator",
                        description = "Right-click your Agent host (e.g. Claude Desktop) and select 'Run as administrator', then retry.",
                        effort = "low",
                        persistent = false
                    },
                    new
                    {
                        id = "developer_mode",
                        title = "Enable Windows Developer Mode (recommended)",
                        description = "Settings → System → Developer Options → Developer Mode. Once enabled, no elevation is needed permanently.",
                        effort = "low",
                        persistent = true
                    },
                    new
                    {
                        id = "gsudo",
                        title = "Install gsudo and update MCP config",
                        description = "Install gsudo via: winget install gerardog.gsudo  Then change your MCP config command to 'gsudo' with winC2D.exe as the first arg.",
                        effort = "medium",
                        persistent = true,
                        installCommand = "winget install gerardog.gsudo",
                        mcpConfigExample = new { command = "gsudo", args = new[] { "C:\\path\\to\\winC2D.exe", "--mcp" } },
                        url = "https://github.com/gerardog/gsudo"
                    }
                }
            }
        };

        return JsonSerializer.Serialize(result, JsonOpts);
    }
}
