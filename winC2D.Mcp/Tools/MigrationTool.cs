using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using winC2D.Core.Models;
using winC2D.Core.Services;

namespace winC2D.Mcp.Tools;

/// <summary>
/// MCP tools for software migration operations:
///   migrate_software  — dryRun pre-check + async submit
///   get_task_status   — poll a running/completed migration task
///   rollback_migration — undo a completed migration
///   list_migrations   — list all tasks in the current session
///
/// All write operations require Administrator or DeveloperMode privilege.
/// </summary>
[McpServerToolType]
internal sealed class MigrationTool(IServiceProvider services)
{
    // ── Blacklisted source path prefixes (case-insensitive) ───────────────────
    private static readonly string[] Blacklist =
    [
        @"C:\Windows",
        @"C:\Program Files\Windows NT",
        @"C:\Program Files\Common Files",
        @"C:\Program Files\Windows Defender",
        @"C:\Program Files (x86)\Windows NT",
        @"C:\Program Files (x86)\Common Files",
    ];

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // ── In-session task tracking ───────────────────────────────────────────────
    // Key: taskId, Value: (task, background Task handle)
    private static readonly ConcurrentDictionary<string, (MigrationTask Task, Task? Execution)> TaskRegistry = new();

    // ─────────────────────────────────────────────────────────────────────────
    // 1. migrate_software
    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "migrate_software"), Description(
        "Migrates a software directory from C: to another drive by moving files and creating a symbolic link at the original path. " +
        "ALWAYS call get_privilege_status first to verify the server can create symlinks. " +
        "ALWAYS call get_disk_info first to confirm the target drive has enough free space. " +
        "ALWAYS call scan_software first to get the exact sourcePath. " +
        "Workflow: " +
        "1) Call with dryRun=true to validate — check 'hasEnoughSpace' and 'blockers' in response. " +
        "2) If dryRun passes, call with dryRun=false to start the migration — you get back a taskId immediately. " +
        "3) Poll get_task_status every 3-5 seconds until state is Completed, Failed, or RolledBack. " +
        "Requires Administrator or DeveloperMode privilege.")]
    public async Task<string> MigrateSoftware(
        [Description("Full path to the source directory, e.g. 'C:\\Program Files\\Google\\Chrome'")] string sourcePath,
        [Description("Target drive letter with colon, e.g. 'D:'")] string targetDrive,
        [Description("Optional subdirectory under the target drive root. Defaults to 'MigratedApps'.")] string targetSubFolder = "MigratedApps",
        [Description("If true, only validates the migration without moving any files. Use this first.")] bool dryRun = false,
        [Description("If true, verifies file integrity after copying. Recommended.")] bool verifyFiles = true)
    {
        // ── Privilege check (skip for dryRun — validation only needs read access) ──
        if (!dryRun && !PrivilegeChecker.CanMigrate())
            return JsonSerializer.Serialize(PrivilegeChecker.BuildInsufficientPrivilegesError(), JsonOpts);

        // ── Normalise paths ───────────────────────────────────────────────────
        sourcePath = sourcePath.TrimEnd('\\', '/');
        var folderName = Path.GetFileName(sourcePath);
        var targetRoot = Path.Combine(targetDrive.TrimEnd('\\') + "\\", targetSubFolder, folderName);

        // ── Pre-checks (shared between dryRun and real run) ───────────────────
        var blockers = new List<string>();

        if (!Directory.Exists(sourcePath))
            blockers.Add($"Source path does not exist: {sourcePath}");

        var isSymlink = Directory.Exists(sourcePath) &&
                        new DirectoryInfo(sourcePath).Attributes.HasFlag(FileAttributes.ReparsePoint);
        if (isSymlink)
            blockers.Add("Source is already a symbolic link (already migrated). Use rollback_migration to undo first.");

        foreach (var blocked in Blacklist)
            if (sourcePath.StartsWith(blocked, StringComparison.OrdinalIgnoreCase))
            { blockers.Add($"Source path is in the system blacklist: {blocked}"); break; }

        long sourceSizeBytes = 0;
        if (Directory.Exists(sourcePath) && !isSymlink)
        {
            try { sourceSizeBytes = GetDirectorySize(sourcePath); }
            catch (Exception ex) { blockers.Add($"Cannot read source directory: {ex.Message}"); }
        }

        var targetDriveInfo = DriveInfo.GetDrives()
            .FirstOrDefault(d => d.IsReady &&
                                 d.RootDirectory.FullName.StartsWith(
                                     targetDrive.TrimEnd('\\') + "\\",
                                     StringComparison.OrdinalIgnoreCase));

        if (targetDriveInfo is null)
            blockers.Add($"Target drive not found or not ready: {targetDrive}");

        var targetDriveFreeBytes = targetDriveInfo?.AvailableFreeSpace ?? 0L;
        var hasEnoughSpace = targetDriveFreeBytes > (long)(sourceSizeBytes * 1.1);
        if (targetDriveInfo is not null && !hasEnoughSpace)
            blockers.Add($"Insufficient disk space on {targetDrive}. " +
                         $"Need {sourceSizeBytes / 1_048_576.0:F0} MB, " +
                         $"available {targetDriveFreeBytes / 1_048_576.0:F0} MB.");

        if (Directory.Exists(targetRoot))
            blockers.Add($"Target directory already exists: {targetRoot}");

        // ── dryRun response ───────────────────────────────────────────────────
        if (dryRun)
        {
            return JsonSerializer.Serialize(new
            {
                dryRun = true,
                sourcePath,
                targetPath = targetRoot,
                sourceSizeMb = Math.Round(sourceSizeBytes / 1_048_576.0, 1),
                targetDriveFreeGb = Math.Round(targetDriveFreeBytes / 1_073_741_824.0, 1),
                hasEnoughSpace,
                isSourceSymlink = isSymlink,
                canProceed = blockers.Count == 0,
                blockers
            }, JsonOpts);
        }

        // ── Abort if blockers ─────────────────────────────────────────────────
        if (blockers.Count > 0)
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = "VALIDATION_FAILED",
                blockers
            }, JsonOpts);

        // ── Create and submit migration task ──────────────────────────────────
        var engine = services.GetRequiredService<IMigrationEngine>();
        var request = new MigrationRequest
        {
            Name           = folderName,
            SourcePath     = sourcePath,
            TargetRootPath = Path.Combine(targetDrive.TrimEnd('\\') + "\\", targetSubFolder),
            Type           = MigrationType.Software,
            CreateSymlink  = true,
            VerifyFiles    = verifyFiles,
        };

        var task = await engine.CreateTaskAsync(request);

        // Fire-and-forget execution; we track it so get_task_status can await it
        var execTask = Task.Run(async () =>
        {
            try { await engine.ExecuteAsync(task); }
            catch { /* State is set to Failed inside engine */ }
        });

        TaskRegistry[task.Id] = (task, execTask);

        return JsonSerializer.Serialize(new
        {
            taskId  = task.Id,
            status  = "running",
            sourcePath,
            targetPath = targetRoot,
            sizeMb  = Math.Round(sourceSizeBytes / 1_048_576.0, 1),
            message = "Migration started. Call get_task_status with the taskId every 3-5 seconds until state is 'Completed', 'Failed', or 'RolledBack'."
        }, JsonOpts);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. get_task_status
    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "get_task_status"), Description(
        "Returns the current status and progress of a migration task. " +
        "Poll every 3-5 seconds after calling migrate_software. " +
        "Stop polling when state is 'Completed', 'Failed', or 'RolledBack'. " +
        "No elevated privileges required.")]
    public async Task<string> GetTaskStatus(
        [Description("The taskId returned by migrate_software.")] string taskId)
    {
        var engine = services.GetRequiredService<IMigrationEngine>();
        var task = await engine.GetTaskAsync(taskId);

        if (task is null)
            return JsonSerializer.Serialize(new
            {
                error = "TASK_NOT_FOUND",
                taskId,
                message = "Task not found. Tasks are stored in memory and are lost if the MCP server restarts."
            }, JsonOpts);

        var elapsed = task.StartedAt.HasValue
            ? (int)(DateTime.UtcNow - task.StartedAt.Value).TotalSeconds
            : 0;

        return JsonSerializer.Serialize(new
        {
            taskId        = task.Id,
            name          = task.Name,
            state         = task.State.ToString(),
            progressPercent = task.ProgressPercent,
            copiedBytes   = task.CopiedBytes,
            totalBytes    = task.TotalBytes,
            copiedFiles   = task.CopiedFiles,
            totalFiles    = task.TotalFiles,
            currentFile   = task.CurrentFile,
            sourcePath    = task.SourcePath,
            targetPath    = task.TargetPath,
            elapsedSeconds = elapsed,
            errorMessage  = task.ErrorMessage,
            completedAt   = task.CompletedAt
        }, JsonOpts);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. rollback_migration
    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "rollback_migration"), Description(
        "Rolls back a completed migration, restoring the software to its original path and removing the symbolic link. " +
        "Only works for tasks that are in 'Completed' state. " +
        "IMPORTANT: Rollback data is stored in memory. If the MCP server was restarted since the migration, rollback is not possible. " +
        "Requires Administrator or DeveloperMode privilege.")]
    public async Task<string> RollbackMigration(
        [Description("The taskId returned by migrate_software.")] string taskId)
    {
        if (!PrivilegeChecker.CanMigrate())
            return JsonSerializer.Serialize(PrivilegeChecker.BuildInsufficientPrivilegesError(), JsonOpts);

        var engine = services.GetRequiredService<IMigrationEngine>();
        var task = await engine.GetTaskAsync(taskId);

        if (task is null)
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = "TASK_NOT_FOUND",
                taskId,
                message = "Task not found. Rollback data is in-memory and lost on server restart."
            }, JsonOpts);

        if (task.State != MigrationState.Completed)
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = "INVALID_STATE",
                taskId,
                currentState = task.State.ToString(),
                message = $"Can only rollback tasks in 'Completed' state. Current state: {task.State}."
            }, JsonOpts);

        try
        {
            var rollbackResult = await engine.RollbackAsync(taskId);
            var updated = await engine.GetTaskAsync(taskId);
            if (rollbackResult.Success)
            {
                return JsonSerializer.Serialize(new
                {
                    success      = true,
                    taskId,
                    restoredPath = task.SourcePath,
                    state        = updated?.State.ToString() ?? "RolledBack",
                    message      = "Rollback completed successfully. The software has been restored to its original location."
                }, JsonOpts);
            }
            return JsonSerializer.Serialize(new
            {
                success  = false,
                error    = "ROLLBACK_FAILED",
                taskId,
                message  = rollbackResult.ErrorMessage ?? "Rollback failed.",
                isPartial = rollbackResult.IsPartial
            }, JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error   = "ROLLBACK_FAILED",
                taskId,
                message = ex.Message
            }, JsonOpts);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. list_migrations
    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "list_migrations"), Description(
        "Lists all migration tasks recorded in the current MCP server session. " +
        "Note: task history is in-memory and is lost if the server restarts. " +
        "No elevated privileges required.")]
    public async Task<string> ListMigrations(
        [Description("Filter by state: 'all', 'completed', 'failed', 'running'. Defaults to 'all'.")] string stateFilter = "all")
    {
        var engine = services.GetRequiredService<IMigrationEngine>();
        var all    = await engine.GetAllTasksAsync();

        var filtered = stateFilter.ToLowerInvariant() switch
        {
            "completed" => all.Where(t => t.State == MigrationState.Completed),
            "failed"    => all.Where(t => t.State is MigrationState.Failed or MigrationState.RolledBack or MigrationState.PartialRollback),
            "running"   => all.Where(t => t.State is MigrationState.Pending or MigrationState.Preparing or MigrationState.Copying or MigrationState.CreatingSymlink or MigrationState.CleaningUp),
            _           => all
        };

        var tasks = filtered.Select(t => new
        {
            taskId      = t.Id,
            name        = t.Name,
            sourcePath  = t.SourcePath,
            targetPath  = t.TargetPath,
            state       = t.State.ToString(),
            progressPercent = t.ProgressPercent,
            sizeMb      = t.TotalBytes > 0 ? Math.Round(t.TotalBytes / 1_048_576.0, 1) : 0,
            createdAt   = t.CreatedAt,
            completedAt = t.CompletedAt,
            errorMessage = t.ErrorMessage
        }).ToList();

        return JsonSerializer.Serialize(new
        {
            stateFilter,
            count = tasks.Count,
            tasks
        }, JsonOpts);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static long GetDirectorySize(string path)
    {
        long size = 0;
        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            try { size += new FileInfo(file).Length; }
            catch { /* skip locked/inaccessible files */ }
        }
        return size;
    }
}
