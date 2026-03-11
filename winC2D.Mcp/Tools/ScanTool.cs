using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using winC2D.Core.Models;
using winC2D.Core.Services;

namespace winC2D.Mcp.Tools;

/// <summary>
/// MCP tool: scan_software
/// Streams all software found under Program Files and returns a JSON summary.
/// No elevated privileges required.
/// </summary>
[McpServerToolType]
internal sealed class ScanTool(IServiceProvider services)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [McpServerTool(Name = "scan_software"), Description(
        "Scans Program Files directories and returns all installed software with " +
        "size and migration status. " +
        "Status values: Normal=ready to migrate, Migrated=already on another drive (symlink), " +
        "Residual=no executable found, Empty=empty directory. " +
        "WARNING: First run may take 1-5 minutes depending on the number of installed programs. " +
        "Subsequent calls are faster because sizes are cached. " +
        "No elevated privileges required.")]
    public async Task<string> ScanSoftware(
        [Description(
            "Optional comma-separated list of directory paths to scan. " +
            "Leave empty to use default Program Files directories.")]
        string? directories = null)
    {
        var scanner = services.GetRequiredService<ISoftwareScanner>();

        IEnumerable<string> scanDirs = string.IsNullOrWhiteSpace(directories)
            ? scanner.GetDefaultScanDirectories()
            : directories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var items     = new List<object>();
        var byStatus  = new Dictionary<string, int>(StringComparer.Ordinal);
        long totalBytes = 0;

        await foreach (var sw in scanner.ScanStreamAsync(scanDirs))
        {
            var statusKey = sw.Status.ToString();
            byStatus[statusKey] = byStatus.TryGetValue(statusKey, out var c) ? c + 1 : 1;
            totalBytes += sw.SizeBytes;

            items.Add(new
            {
                name            = sw.Name,
                installLocation = sw.InstallLocation,
                sizeMb          = sw.SizeBytes > 0 ? Math.Round(sw.SizeBytes / 1_048_576.0, 1) : 0.0,
                status          = statusKey,
                isSymlink       = sw.IsSymlink ? (bool?)true : null   // omit false to reduce noise
            });
        }

        // Sort: Normal first (most useful to agent), then by size descending
        var sorted = items
            .OfType<dynamic>()
            .OrderBy(i => i.status != "Normal" ? 1 : 0)
            .ThenByDescending(i => (double)i.sizeMb)
            .ToList();

        var result = new
        {
            scannedDirectories = scanDirs.ToArray(),
            summary = new
            {
                total           = items.Count,
                byStatus,
                totalSizeGb     = Math.Round(totalBytes / 1_073_741_824.0, 2),
                migratableSizeMb= Math.Round(
                    items.OfType<dynamic>().Where(i => (string)i.status == "Normal").Sum(i => (double)i.sizeMb),
                    1)
            },
            software = sorted
        };

        return JsonSerializer.Serialize(result, JsonOpts);
    }
}
