using Microsoft.Win32;
using System.Security.Principal;

namespace winC2D.Mcp;

/// <summary>
/// Detects the privilege level under which the current process is running.
/// Used by MCP tools to gate write operations.
/// </summary>
public static class PrivilegeChecker
{
    public enum Level
    {
        /// <summary>Process is running with full administrator token.</summary>
        Administrator,
        /// <summary>Windows Developer Mode is enabled (symlinks without elevation).</summary>
        DeveloperMode,
        /// <summary>Normal user — scan is allowed, migration is not.</summary>
        Restricted
    }

    /// <summary>Returns the effective privilege level of the current process.</summary>
    public static Level GetLevel()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);

        if (principal.IsInRole(WindowsBuiltInRole.Administrator))
            return Level.Administrator;

        if (IsDeveloperModeEnabled())
            return Level.DeveloperMode;

        return Level.Restricted;
    }

    /// <summary>
    /// Returns true if the caller has enough privilege to create symlinks
    /// (Administrator or Developer Mode).
    /// </summary>
    public static bool CanMigrate() => GetLevel() != Level.Restricted;

    /// <summary>
    /// Builds the structured JSON error object returned to the agent when a
    /// tool requires elevated privileges that are not present.
    /// </summary>
    public static object BuildInsufficientPrivilegesError() => new
    {
        error = "INSUFFICIENT_PRIVILEGES",
        canMigrate = false,
        canScan = true,
        resolutionOptions = new object[]
        {
            new
            {
                id = "run_as_admin",
                description = "Right-click your Agent host (e.g. Claude Desktop) and select 'Run as administrator'."
            },
            new
            {
                id = "developer_mode",
                description = "Enable Windows Developer Mode: Settings → System → Developer Options → Developer Mode."
            },
            new
            {
                id = "gsudo",
                description = "Install gsudo (winget install gerardog.gsudo) then change your MCP config: { \"command\": \"gsudo\", \"args\": [\"winC2D.exe\", \"--mcp\"] }",
                url = "https://github.com/gerardog/gsudo"
            }
        }
    };

    // ──────────────────────────────────────────────────────────────────────────

    private static bool IsDeveloperModeEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock");
            return key?.GetValue("AllowDevelopmentWithoutDevLicense") is 1;
        }
        catch { return false; }
    }
}
