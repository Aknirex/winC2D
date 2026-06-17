using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace winC2D.Cli;

[SupportedOSPlatform("windows")]
public static class PrivilegeChecker
{
    public enum Level
    {
        Administrator,
        DeveloperMode,
        Restricted
    }

    public static Level GetLevel()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);

        if (principal.IsInRole(WindowsBuiltInRole.Administrator))
            return Level.Administrator;

        return IsDeveloperModeEnabled()
            ? Level.DeveloperMode
            : Level.Restricted;
    }

    public static bool CanMigrate() => GetLevel() != Level.Restricted;

    public static object BuildInsufficientPrivilegesError(string? runWithGsudo = null)
    {
        var result = new Dictionary<string, object?>
        {
            ["success"] = false,
            ["error"] = "INSUFFICIENT_PRIVILEGES",
            ["message"] = "Migration requires administrator rights to write to protected directories like C:\\Program Files.",
            ["canMigrate"] = false,
            ["canScan"] = true,
            ["resolutionOptions"] = ResolutionOptions
        };

        result["installGsudo"] = "winget install gerardog.gsudo";
        result["installHint"] = "Run this once to install gsudo (if not already bundled).";

        if (!string.IsNullOrWhiteSpace(runWithGsudo))
        {
            result["runWith"] = runWithGsudo;
            result["runWithHint"] = "Execute this command to run the migration with administrator privileges via gsudo.";
        }

        return result;
    }

    public static object[] ResolutionOptions =>
    [
        new
        {
            id = "run_as_admin",
            title = "Run the agent host as administrator",
            description = "Start the shell or agent host as administrator, then retry the CLI command.",
            effort = "low",
            persistent = false
        },
        new
        {
            id = "developer_mode",
            title = "Enable Windows Developer Mode",
            description = "Settings > System > Developer Options > Developer Mode. Once enabled, symlink creation does not require elevation.",
            effort = "low",
            persistent = true
        },
        new
        {
            id = "gsudo",
            title = "Use gsudo",
            description = "Install gsudo with: winget install gerardog.gsudo. Then run: gsudo winC2D.Cli.exe migrate ...",
            effort = "medium",
            persistent = true,
            installCommand = "winget install gerardog.gsudo",
            url = "https://github.com/gerardog/gsudo"
        }
    ];

    private static bool IsDeveloperModeEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock");
            return key?.GetValue("AllowDevelopmentWithoutDevLicense") is 1;
        }
        catch
        {
            return false;
        }
    }
}
