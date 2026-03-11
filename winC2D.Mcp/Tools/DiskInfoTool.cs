using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;

namespace winC2D.Mcp.Tools;

/// <summary>
/// MCP tool: get_disk_info
/// Returns free / total space for all fixed drives, highlighting non-C drives
/// suitable as migration targets.
/// No elevated privileges required.
/// </summary>
[McpServerToolType]
internal sealed class DiskInfoTool
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [McpServerTool(Name = "get_disk_info"), Description(
        "Returns free and total disk space for all fixed drives. " +
        "Use this before migrate_software to pick a suitable target drive. " +
        "Non-C drives with enough free space are the best migration targets. " +
        "No elevated privileges required.")]
    public static string GetDiskInfo()
    {
        var drives = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
            .Select(d =>
            {
                var letter = d.RootDirectory.FullName.TrimEnd('\\'); // "C:", "D:", …
                var totalGb  = Math.Round(d.TotalSize        / 1_073_741_824.0, 1);
                var freeGb   = Math.Round(d.AvailableFreeSpace / 1_073_741_824.0, 1);
                var usedGb   = Math.Round((d.TotalSize - d.AvailableFreeSpace) / 1_073_741_824.0, 1);
                var freePercent = d.TotalSize > 0
                    ? (int)Math.Round(d.AvailableFreeSpace * 100.0 / d.TotalSize)
                    : 0;
                var isSystemDrive = letter.Equals(
                    Environment.GetEnvironmentVariable("SystemDrive")?.TrimEnd('\\'),
                    StringComparison.OrdinalIgnoreCase);

                return new
                {
                    drive          = letter,
                    label          = string.IsNullOrEmpty(d.VolumeLabel) ? null : d.VolumeLabel,
                    fileSystem     = d.DriveFormat,
                    totalGb,
                    usedGb,
                    freeGb,
                    freePercent,
                    isSystemDrive,
                    recommendedTarget = !isSystemDrive && freeGb >= 1.0
                };
            })
            .OrderBy(d => d.drive)
            .ToList();

        var result = new
        {
            drives,
            summary = new
            {
                totalDrives              = drives.Count,
                recommendedTargets       = drives.Where(d => d.recommendedTarget).Select(d => d.drive).ToArray(),
                largestFreeGb            = drives.Count > 0 ? drives.Max(d => d.freeGb) : 0,
                largestFreeTarget        = drives.Where(d => d.recommendedTarget)
                                                  .OrderByDescending(d => d.freeGb)
                                                  .FirstOrDefault()?.drive
            }
        };

        return JsonSerializer.Serialize(result, JsonOpts);
    }
}
