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

    public static object BuildInsufficientPrivilegesError() => new
    {
        success = false,
        error = "INSUFFICIENT_PRIVILEGES",
        message = "Migration and rollback require administrator rights or Windows Developer Mode.",
        canMigrate = false,
        canScan = true,
        resolutionOptions = ResolutionOptions
    };

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
            description = "Install gsudo with: winget install gerardog.gsudo. Then run: gsudo winC2D.exe --cli migrate ...",
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
