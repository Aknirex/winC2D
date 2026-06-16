using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using winC2D.Core.FileSystem;
using winC2D.Core.Models;
using winC2D.Core.Services;

namespace winC2D.Cli;

public static class CliApplication
{
    private const string InternalRunTaskCommand = "__run-task";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task<int> RunAsync(
        string[] args,
        IServiceProvider services,
        TextWriter stdout,
        TextWriter stderr,
        Func<IServiceProvider>? serviceProviderFactory = null,
        string? executablePath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalized = NormalizeArgs(args);
            if (normalized.Count == 0)
                return await WriteAsync(stdout, CliExitCode.Success, BuildUsage());

            var command = normalized[0].ToLowerInvariant();
            var commandArgs = normalized.Skip(1).ToArray();

            return command switch
            {
                "privilege-status" => await WriteAsync(stdout, CliExitCode.Success, BuildPrivilegeStatus()),
                "disk-info" => await WriteAsync(stdout, CliExitCode.Success, BuildDiskInfo(services)),
                "scan" => await ScanAsync(commandArgs, services, stdout, cancellationToken),
                "preflight" => await PreflightAsync(commandArgs, services, stdout, cancellationToken),
                "migrate" => await MigrateAsync(commandArgs, services, stdout, stderr, serviceProviderFactory, executablePath, cancellationToken),
                "status" => await StatusAsync(commandArgs, services, stdout),
                "pause" => await PauseAsync(commandArgs, services, stdout, cancellationToken),
                "resume" => await ResumeAsync(commandArgs, services, stdout, cancellationToken),
                "cancel" => await CancelAsync(commandArgs, services, stdout, cancellationToken),
                "rollback" => await RollbackAsync(commandArgs, services, stdout, cancellationToken),
                "list" => await ListAsync(commandArgs, services, stdout),
                "cleanup" => await CleanupAsync(commandArgs, services, stdout, cancellationToken),
                "help" or "--help" or "-h" => await WriteAsync(stdout, CliExitCode.Success, BuildUsage()),
                "version" or "--version" or "-v" => await WriteAsync(stdout, CliExitCode.Success, BuildVersion()),
                InternalRunTaskCommand => await RunTaskAsync(commandArgs, services, stdout, cancellationToken),
                _ => await WriteAsync(stdout, CliExitCode.ArgumentError, Error("UNKNOWN_COMMAND", $"Unknown CLI command: {command}"))
            };
        }
        catch (CliParseException ex)
        {
            return await WriteAsync(stdout, CliExitCode.ArgumentError, Error("ARGUMENT_ERROR", ex.Message));
        }
        catch (Exception ex)
        {
            await stderr.WriteLineAsync(ex.ToString());
            return await WriteAsync(stdout, CliExitCode.UnhandledException, Error("UNHANDLED_EXCEPTION", ex.Message));
        }
    }

    private static async Task<int> ScanAsync(
        string[] args,
        IServiceProvider services,
        TextWriter stdout,
        CancellationToken cancellationToken)
    {
        var parsed = Parse(args, valueOptions: ["directories"], flags: []);
        EnsureNoPositionals(parsed);

        var scanner = services.GetRequiredService<ISoftwareScanner>();
        var scanDirs = parsed.TryGetValue("directories", out var directories) && !string.IsNullOrWhiteSpace(directories)
            ? directories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : scanner.GetDefaultScanDirectories();

        var items = new List<SoftwareItemDto>();
        var byStatus = new Dictionary<string, int>(StringComparer.Ordinal);
        long totalBytes = 0;

        await foreach (var software in scanner.ScanStreamAsync(scanDirs, cancellationToken: cancellationToken))
        {
            var status = software.Status.ToString();
            byStatus[status] = byStatus.TryGetValue(status, out var count) ? count + 1 : 1;
            totalBytes += software.SizeBytes;

            items.Add(new SoftwareItemDto(
                software.Name,
                software.InstallLocation,
                Math.Round(software.SizeBytes / 1_048_576.0, 1),
                status,
                software.IsSymlink ? true : null));
        }

        var sorted = items
            .OrderBy(i => i.Status == nameof(SoftwareStatus.Normal) ? 0 : 1)
            .ThenByDescending(i => i.SizeMb)
            .ToList();

        return await WriteAsync(stdout, CliExitCode.Success, new
        {
            success = true,
            scannedDirectories = scanDirs.ToArray(),
            summary = new
            {
                total = items.Count,
                byStatus,
                totalSizeGb = Math.Round(totalBytes / 1_073_741_824.0, 2),
                migratableSizeMb = Math.Round(
                    items.Where(i => i.Status == nameof(SoftwareStatus.Normal)).Sum(i => i.SizeMb),
                    1)
            },
            software = sorted
        });
    }

    private static MigrationRequest BuildMigrationRequest(CliArguments parsed)
    {
        var sourcePath = Require(parsed, "source").TrimEnd('\\', '/');
        var targetPath = Require(parsed, "target").TrimEnd('\\', '/');
        if (string.IsNullOrWhiteSpace(Path.GetPathRoot(targetPath)))
            throw new CliParseException("--target must be an absolute destination directory such as D:\\Program Files.");

        return new MigrationRequest
        {
            Name = Path.GetFileName(sourcePath),
            SourcePath = sourcePath,
            TargetRootPath = targetPath,
            Type = MigrationType.Software,
            CreateSymlink = true,
            VerifyFiles = ParseBool(parsed.GetValueOrDefault("verify"), defaultValue: true, "verify")
        };
    }

    private static object BuildPreflightPayload(MigrationPreflightResult validation, bool dryRun)
    {
        return new
        {
            success = validation.CanProceed,
            dryRun = dryRun ? true : (bool?)null,
            canProceed = validation.CanProceed,
            sourcePath = validation.SourcePath,
            targetPath = validation.TargetPath,
            sourceSizeBytes = validation.SourceSizeBytes,
            targetFreeBytes = validation.TargetFreeBytes,
            sourceSizeMb = Math.Round(validation.SourceSizeBytes / 1_048_576.0, 1),
            targetDriveFreeGb = Math.Round(validation.TargetFreeBytes / 1_073_741_824.0, 1),
            hasEnoughSpace = validation.HasEnoughSpace,
            isSourceSymlink = validation.IsSourceSymlink,
            blockers = validation.Blockers,
            warnings = validation.Warnings
        };
    }

    private static async Task<int> PreflightAsync(
        string[] args,
        IServiceProvider services,
        TextWriter stdout,
        CancellationToken cancellationToken)
    {
        var parsed = Parse(args, valueOptions: ["source", "target", "verify"], flags: []);
        EnsureNoPositionals(parsed);
        var request = BuildMigrationRequest(parsed);
        var validation = await services.GetRequiredService<IMigrationEngine>().ValidateAsync(request, cancellationToken);

        return await WriteAsync(stdout, validation.CanProceed ? CliExitCode.Success : CliExitCode.BusinessFailure, BuildPreflightPayload(validation, dryRun: false));
    }

    private static async Task<int> MigrateAsync(
        string[] args,
        IServiceProvider services,
        TextWriter stdout,
        TextWriter stderr,
        Func<IServiceProvider>? serviceProviderFactory,
        string? executablePath,
        CancellationToken cancellationToken)
    {
        var parsed = Parse(
            args,
            valueOptions: ["source", "target", "verify", "timeout-seconds"],
            flags: ["dry-run", "yes", "wait"]);
        EnsureNoPositionals(parsed);

        var request = BuildMigrationRequest(parsed);
        var dryRun = parsed.HasFlag("dry-run");
        var wait = parsed.HasFlag("wait");
        var timeoutSeconds = ParseInt(parsed.GetValueOrDefault("timeout-seconds"), defaultValue: 1800, "timeout-seconds");

        if (!dryRun && !parsed.HasFlag("yes"))
            return await WriteAsync(stdout, CliExitCode.ArgumentError, Error("CONFIRMATION_REQUIRED", "Real migrations require --yes. Use --dry-run first to validate."));

        if (!dryRun && !PrivilegeChecker.CanMigrate())
        {
            var level = PrivilegeChecker.GetLevel();
            await stderr.WriteLineAsync(
                $"[DIAG] Privilege check FAILED: level={level}, isAdmin={level == PrivilegeChecker.Level.Administrator}, "
                + $"developerMode={level == PrivilegeChecker.Level.DeveloperMode}, "
                + $"process={Environment.ProcessPath}, pid={Environment.ProcessId}");

            // Return actionable error with gsudo install + run instructions.
            var runWithGsudo = BuildGsudoCommand(executablePath, args);
            return await WriteAsync(stdout, CliExitCode.InsufficientPrivileges,
                PrivilegeChecker.BuildInsufficientPrivilegesError(runWithGsudo));
        }

        var engine = services.GetRequiredService<IMigrationEngine>();
        var validation = await engine.ValidateAsync(request, cancellationToken);

        if (dryRun)
            return await WriteAsync(stdout, validation.CanProceed ? CliExitCode.Success : CliExitCode.BusinessFailure, BuildPreflightPayload(validation, dryRun: true));

        if (!validation.CanProceed)
        {
            return await WriteAsync(stdout, CliExitCode.BusinessFailure, new
            {
                success = false,
                error = "VALIDATION_FAILED",
                message = "Migration validation failed.",
                blockers = validation.Blockers
            });
        }

        var task = await engine.CreateTaskAsync(request, cancellationToken);

        var worker = await StartWorkerProcessAsync(executablePath, task.Id, services, stderr, cancellationToken);
        if (!worker.Success)
        {
            task.State = MigrationState.Failed;
            task.CompletedAt = DateTime.UtcNow;
            task.ErrorMessage = worker.ErrorMessage ?? "Could not start the hidden CLI worker process.";
            if (services.GetService<IMigrationTaskStore>() is { } failureStore)
                await failureStore.UpsertAsync(task, immediate: true, cancellationToken);

            return await WriteAsync(stdout, CliExitCode.UnhandledException, Error("WORKER_START_FAILED", worker.ErrorMessage ?? "Could not start the hidden CLI worker process.", new
            {
                taskId = task.Id,
                workerLogPath = task.WorkerLogPath
            }));
        }

        if (wait)
            return await WaitForTaskAsync(task.Id, serviceProviderFactory, services, stdout, timeoutSeconds, cancellationToken);

        return await WriteAsync(stdout, CliExitCode.Success, new
        {
            success = true,
            taskId = task.Id,
            state = task.State.ToString(),
            workerPid = worker.ProcessId,
            workerLogPath = task.WorkerLogPath,
            startedAt = task.WorkerStartedAt,
            sourcePath = request.SourcePath,
            targetPath = task.TargetPath,
            sizeMb = Math.Round(validation.SourceSizeBytes / 1_048_576.0, 1),
            message = "Migration started. Call status with this taskId until state is Completed, Failed, RolledBack, PartialRollback, or Cancelled."
        });
    }

    private static async Task<int> StatusAsync(string[] args, IServiceProvider services, TextWriter stdout)
    {
        var parsed = Parse(args, valueOptions: ["task-id"], flags: []);
        EnsureNoPositionals(parsed);
        var taskId = Require(parsed, "task-id");
        var task = await services.GetRequiredService<IMigrationEngine>().GetTaskAsync(taskId);

        if (task is null)
            return await WriteAsync(stdout, CliExitCode.TaskNotFound, Error("TASK_NOT_FOUND", $"Task not found: {taskId}", new { taskId }));

        return await WriteAsync(stdout, CliExitCode.Success, BuildTaskStatus(task, services.GetService<IMigrationTaskStore>()));
    }

    private static async Task<int> PauseAsync(string[] args, IServiceProvider services, TextWriter stdout, CancellationToken cancellationToken)
    {
        var parsed = Parse(args, valueOptions: ["task-id"], flags: []);
        EnsureNoPositionals(parsed);
        var taskId = Require(parsed, "task-id");
        var engine = services.GetRequiredService<IMigrationEngine>();
        var task = await engine.GetTaskAsync(taskId);
        if (task is null)
            return await WriteAsync(stdout, CliExitCode.TaskNotFound, Error("TASK_NOT_FOUND", $"Task not found: {taskId}", new { taskId }));

        await engine.RequestPauseAsync(taskId, cancellationToken);
        return await WriteAsync(stdout, CliExitCode.Success, new { success = true, taskId, requested = "pause" });
    }

    private static async Task<int> ResumeAsync(string[] args, IServiceProvider services, TextWriter stdout, CancellationToken cancellationToken)
    {
        var parsed = Parse(args, valueOptions: ["task-id"], flags: []);
        EnsureNoPositionals(parsed);
        var taskId = Require(parsed, "task-id");
        var engine = services.GetRequiredService<IMigrationEngine>();
        var task = await engine.GetTaskAsync(taskId);
        if (task is null)
            return await WriteAsync(stdout, CliExitCode.TaskNotFound, Error("TASK_NOT_FOUND", $"Task not found: {taskId}", new { taskId }));

        await engine.RequestResumeAsync(taskId, cancellationToken);
        return await WriteAsync(stdout, CliExitCode.Success, new { success = true, taskId, requested = "resume" });
    }

    private static async Task<int> CancelAsync(string[] args, IServiceProvider services, TextWriter stdout, CancellationToken cancellationToken)
    {
        var parsed = Parse(args, valueOptions: ["task-id"], flags: ["yes", "no-rollback"]);
        EnsureNoPositionals(parsed);
        var taskId = Require(parsed, "task-id");
        if (!parsed.HasFlag("yes"))
            return await WriteAsync(stdout, CliExitCode.ArgumentError, Error("CONFIRMATION_REQUIRED", "Cancel requires --yes."));

        var engine = services.GetRequiredService<IMigrationEngine>();
        var task = await engine.GetTaskAsync(taskId);
        if (task is null)
            return await WriteAsync(stdout, CliExitCode.TaskNotFound, Error("TASK_NOT_FOUND", $"Task not found: {taskId}", new { taskId }));

        var rollback = !parsed.HasFlag("no-rollback");
        await engine.RequestCancelAsync(taskId, rollback, cancellationToken);
        return await WriteAsync(stdout, CliExitCode.Success, new { success = true, taskId, requested = "cancel", rollback });
    }

    private static async Task<int> RollbackAsync(
        string[] args,
        IServiceProvider services,
        TextWriter stdout,
        CancellationToken cancellationToken)
    {
        var parsed = Parse(args, valueOptions: ["task-id"], flags: ["yes"]);
        EnsureNoPositionals(parsed);
        var taskId = Require(parsed, "task-id");

        if (!parsed.HasFlag("yes"))
            return await WriteAsync(stdout, CliExitCode.ArgumentError, Error("CONFIRMATION_REQUIRED", "Rollback requires --yes."));

        if (!PrivilegeChecker.CanMigrate())
            return await WriteAsync(stdout, CliExitCode.InsufficientPrivileges, PrivilegeChecker.BuildInsufficientPrivilegesError());

        var engine = services.GetRequiredService<IMigrationEngine>();
        var task = await engine.GetTaskAsync(taskId);

        if (task is null)
            return await WriteAsync(stdout, CliExitCode.TaskNotFound, Error("TASK_NOT_FOUND", $"Task not found: {taskId}", new { taskId }));

        if (task.State != MigrationState.Completed)
        {
            return await WriteAsync(stdout, CliExitCode.BusinessFailure, new
            {
                success = false,
                error = "INVALID_STATE",
                message = $"Can only rollback tasks in Completed state. Current state: {task.State}.",
                taskId,
                currentState = task.State.ToString()
            });
        }

        var result = await engine.RollbackAsync(taskId, cancellationToken);
        var updated = await engine.GetTaskAsync(taskId);
        return await WriteAsync(stdout, result.Success ? CliExitCode.Success : CliExitCode.BusinessFailure, new
        {
            success = result.Success,
            error = result.Success ? null : "ROLLBACK_FAILED",
            message = result.Success
                ? "Rollback completed successfully."
                : result.ErrorMessage ?? "Rollback failed.",
            taskId,
            restoredPath = task.SourcePath,
            state = updated?.State.ToString(),
            isPartial = result.IsPartial ? true : (bool?)null,
            failedStep = result.FailedStep?.ToString(),
            rolledBackSteps = result.RolledBackSteps.Select(s => s.ToString()).ToArray()
        });
    }

    private static async Task<int> ListAsync(string[] args, IServiceProvider services, TextWriter stdout)
    {
        var parsed = Parse(args, valueOptions: ["state"], flags: []);
        EnsureNoPositionals(parsed);
        var stateFilter = (parsed.GetValueOrDefault("state") ?? "all").ToLowerInvariant();

        if (stateFilter is not ("all" or "completed" or "failed" or "running" or "stale"))
            return await WriteAsync(stdout, CliExitCode.ArgumentError, Error("INVALID_STATE_FILTER", "State must be one of: all, completed, failed, running, stale."));

        var engine = services.GetRequiredService<IMigrationEngine>();
        var store = services.GetService<IMigrationTaskStore>();
        var all = await engine.GetAllTasksAsync();
        var now = DateTime.UtcNow;
        var filtered = stateFilter switch
        {
            "completed" => all.Where(t => t.State == MigrationState.Completed),
            "failed" => all.Where(t => t.State is MigrationState.Failed or MigrationState.RolledBack or MigrationState.PartialRollback or MigrationState.Cancelled),
            "running" => all.Where(t => IsRunningState(t) && (store is null || !store.IsStale(t, now))),
            "stale" => all.Where(t => store is not null && store.IsStale(t, now)),
            _ => all
        };

        var tasks = filtered
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                taskId = t.Id,
                name = t.Name,
                sourcePath = t.SourcePath,
                targetPath = t.TargetPath,
                state = t.State.ToString(),
                progressPercent = t.ProgressPercent,
                sizeMb = t.TotalBytes > 0 ? Math.Round(t.TotalBytes / 1_048_576.0, 1) : 0,
                createdAt = t.CreatedAt,
                completedAt = t.CompletedAt,
                workerPid = t.WorkerProcessId,
                workerLogPath = t.WorkerLogPath,
                lastHeartbeatAt = t.LastHeartbeatAt,
                isStale = store?.IsStale(t, now),
                staleReason = store is not null && store.IsStale(t, now) ? store.GetStaleReason(t, now) : null,
                errorMessage = t.ErrorMessage
            })
            .ToList();

        return await WriteAsync(stdout, CliExitCode.Success, new
        {
            success = true,
            stateFilter,
            count = tasks.Count,
            tasks
        });
    }

    private static async Task<int> CleanupAsync(
        string[] args,
        IServiceProvider services,
        TextWriter stdout,
        CancellationToken cancellationToken)
    {
        var parsed = Parse(args, valueOptions: ["state", "older-than-days"], flags: ["yes"]);
        EnsureNoPositionals(parsed);
        if (!parsed.HasFlag("yes"))
            return await WriteAsync(stdout, CliExitCode.ArgumentError, Error("CONFIRMATION_REQUIRED", "Cleanup requires --yes."));

        var state = (parsed.GetValueOrDefault("state") ?? "stale").ToLowerInvariant();
        if (state is not ("stale" or "terminal"))
            return await WriteAsync(stdout, CliExitCode.ArgumentError, Error("INVALID_STATE_FILTER", "Cleanup state must be stale or terminal."));

        var options = new TaskCleanupOptions
        {
            State = state == "stale" ? TaskCleanupState.Stale : TaskCleanupState.Terminal,
            OlderThanDays = ParseInt(parsed.GetValueOrDefault("older-than-days"), defaultValue: 30, "older-than-days")
        };

        var removed = await services.GetRequiredService<IMigrationEngine>().CleanupTasksAsync(options, cancellationToken);
        return await WriteAsync(stdout, CliExitCode.Success, new
        {
            success = true,
            removed,
            state,
            olderThanDays = options.OlderThanDays
        });
    }

    private static async Task<int> RunTaskAsync(
        string[] args,
        IServiceProvider services,
        TextWriter stdout,
        CancellationToken cancellationToken)
    {
        var parsed = Parse(args, valueOptions: ["task-id"], flags: ["quiet"]);
        EnsureNoPositionals(parsed);
        var taskId = Require(parsed, "task-id");
        var quiet = parsed.HasFlag("quiet");
        var engine = services.GetRequiredService<IMigrationEngine>();
        var task = await engine.GetTaskAsync(taskId);

        if (task is null)
        {
            if (quiet)
                return (int)CliExitCode.TaskNotFound;
            return await WriteAsync(stdout, CliExitCode.TaskNotFound, Error("TASK_NOT_FOUND", $"Task not found: {taskId}", new { taskId }));
        }

        var logPath = EnsureWorkerLogPath(task);
        task.WorkerProcessId = Environment.ProcessId;
        task.WorkerStartedAt ??= DateTime.UtcNow;
        task.LastHeartbeatAt = DateTime.UtcNow;
        task.WorkerLogPath = logPath;
        if (services.GetService<IMigrationTaskStore>() is { } store)
            await store.UpsertAsync(task, immediate: true, cancellationToken);
        await AppendWorkerLogAsync(logPath, $"Worker started. taskId={task.Id}; pid={Environment.ProcessId}; source=\"{task.SourcePath}\"; target=\"{task.TargetPath}\".");

        MigrationResult result;
        try
        {
            result = await engine.ExecuteAsync(task, cancellationToken);
        }
        catch (Exception ex)
        {
            task.State = MigrationState.Failed;
            task.CompletedAt = DateTime.UtcNow;
            task.ErrorMessage = ex.Message;
            if (services.GetService<IMigrationTaskStore>() is { } exceptionStore)
                await exceptionStore.UpsertAsync(task, immediate: true, cancellationToken);
            await AppendWorkerLogAsync(logPath, ex.ToString());
            throw;
        }

        var updated = await engine.GetTaskAsync(taskId);
        await AppendWorkerLogAsync(logPath, result.Success
            ? $"Worker completed. taskId={taskId}; state={updated?.State.ToString() ?? result.FinalState.ToString()}."
            : $"Worker failed. taskId={taskId}; state={updated?.State.ToString() ?? result.FinalState.ToString()}; error=\"{result.ErrorMessage ?? "Migration failed."}\".");

        if (quiet)
            return result.Success ? (int)CliExitCode.Success : (int)CliExitCode.BusinessFailure;

        return await WriteAsync(stdout, result.Success ? CliExitCode.Success : CliExitCode.BusinessFailure, new
        {
            success = result.Success,
            taskId,
            state = updated?.State.ToString() ?? result.FinalState.ToString(),
            error = result.Success ? null : "MIGRATION_FAILED",
            message = result.Success ? "Migration completed successfully." : result.ErrorMessage ?? "Migration failed."
        });
    }

    private static async Task<int> WaitForTaskAsync(
        string taskId,
        Func<IServiceProvider>? serviceProviderFactory,
        IServiceProvider fallbackServices,
        TextWriter stdout,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTime.UtcNow;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            MigrationTask? task;

            if (serviceProviderFactory is not null)
            {
                var provider = serviceProviderFactory();
                try
                {
                    task = await provider.GetRequiredService<IMigrationEngine>().GetTaskAsync(taskId);
                }
                finally
                {
                    if (provider is IDisposable disposable)
                        disposable.Dispose();
                }
            }
            else
            {
                task = await fallbackServices.GetRequiredService<IMigrationEngine>().GetTaskAsync(taskId);
            }

            if (task is null)
                return await WriteAsync(stdout, CliExitCode.TaskNotFound, Error("TASK_NOT_FOUND", $"Task not found: {taskId}", new { taskId }));

            if (IsTerminalState(task.State))
            {
                var status = BuildTaskStatus(task);
                if (task.State == MigrationState.Completed)
                    return await WriteAsync(stdout, CliExitCode.Success, status);

                return await WriteAsync(stdout, CliExitCode.BusinessFailure, new
                {
                    success = false,
                    error = "TASK_FAILED",
                    message = $"Task {taskId} reached terminal state {task.State}.",
                    task = status
                });
            }

            if (task.State == MigrationState.Pending &&
                (DateTime.UtcNow - task.CreatedAt).TotalSeconds >= 60 &&
                fallbackServices.GetService<IMigrationTaskStore>() is { } pendingStore &&
                pendingStore.IsStale(task, DateTime.UtcNow))
            {
                return await WriteAsync(stdout, CliExitCode.BusinessFailure, new
                {
                    success = false,
                    error = "STALE_PENDING",
                    message = $"Task {taskId} is still Pending and no live worker was found.",
                    task = BuildTaskStatus(task, pendingStore)
                });
            }

            if (fallbackServices.GetService<IMigrationTaskStore>() is { } store &&
                store.IsStale(task, DateTime.UtcNow))
            {
                return await WriteAsync(stdout, CliExitCode.BusinessFailure, new
                {
                    success = false,
                    error = store.IsWorkerAlive(task) ? "WAIT_TIMEOUT" : "WORKER_EXITED",
                    message = store.IsWorkerAlive(task)
                        ? $"Timed out waiting for task {taskId}."
                        : $"Worker for task {taskId} is not running and the task is not terminal.",
                    task = BuildTaskStatus(task, store)
                });
            }

            if (timeoutSeconds > 0 && (DateTime.UtcNow - startedAt).TotalSeconds >= timeoutSeconds)
            {
                return await WriteAsync(stdout, CliExitCode.BusinessFailure, new
                {
                    success = false,
                    error = "WAIT_TIMEOUT",
                    message = $"Timed out waiting for task {taskId}. The hidden worker may still be running; call status to continue polling.",
                    task = BuildTaskStatus(task, fallbackServices.GetService<IMigrationTaskStore>())
                });
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }

    private static object BuildPrivilegeStatus()
    {
        var level = PrivilegeChecker.GetLevel();
        var isAdmin = level == PrivilegeChecker.Level.Administrator;
        var isDeveloperMode = level == PrivilegeChecker.Level.DeveloperMode;
        // Developer Mode allows symlink creation, but writing to C:\Program Files
        // still requires Administrator. Only report canMigrate=true when admin.
        var canMigrate = isAdmin;

        return new
        {
            success = true,
            privilegeLevel = level.ToString(),
            isAdmin,
            isDeveloperMode,
            canMigrate,
            canCreateSymlink = !isDeveloperMode ? canMigrate : true,
            canScan = true,
            details = level switch
            {
                PrivilegeChecker.Level.Administrator => "Running as administrator. Full migration capability including C:\\Program Files access.",
                PrivilegeChecker.Level.DeveloperMode => "Windows Developer Mode is enabled (symlink creation allowed), but Administrator rights are still required to write to C:\\Program Files and other protected directories.",
                _ => "Running without elevated privileges. Symlink creation and protected-directory access require Administrator rights or Developer Mode + Administrator."
            },
            importantNote = isDeveloperMode
                ? "WARNING: Developer Mode allows symlink creation but NOT writing to C:\\Program Files. Migrations from system-protected directories still require Administrator."
                : null,
            resolutionOptions = canMigrate ? null : PrivilegeChecker.ResolutionOptions
        };
    }

    private static object BuildDiskInfo(IServiceProvider services)
    {
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var systemDrive = Environment.GetEnvironmentVariable("SystemDrive")?.TrimEnd('\\');
        var drives = fileSystem.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
            .Select(d =>
            {
                var letter = d.RootDirectory.FullName.TrimEnd('\\');
                var totalGb = Math.Round(d.TotalSize / 1_073_741_824.0, 1);
                var freeGb = Math.Round(d.AvailableFreeSpace / 1_073_741_824.0, 1);
                var usedGb = Math.Round((d.TotalSize - d.AvailableFreeSpace) / 1_073_741_824.0, 1);
                var isSystemDrive = string.Equals(letter, systemDrive, StringComparison.OrdinalIgnoreCase);
                return new
                {
                    drive = letter,
                    label = string.IsNullOrEmpty(d.VolumeLabel) ? null : d.VolumeLabel,
                    fileSystem = d.DriveFormat,
                    totalGb,
                    usedGb,
                    freeGb,
                    freePercent = d.TotalSize > 0
                        ? (int)Math.Round(d.AvailableFreeSpace * 100.0 / d.TotalSize)
                        : 0,
                    isSystemDrive,
                    recommendedTarget = !isSystemDrive && freeGb >= 1.0
                };
            })
            .OrderBy(d => d.drive)
            .ToList();

        return new
        {
            success = true,
            drives,
            summary = new
            {
                totalDrives = drives.Count,
                recommendedTargets = drives.Where(d => d.recommendedTarget).Select(d => d.drive).ToArray(),
                largestFreeGb = drives.Count > 0 ? drives.Max(d => d.freeGb) : 0,
                largestFreeTarget = drives.Where(d => d.recommendedTarget)
                    .OrderByDescending(d => d.freeGb)
                    .FirstOrDefault()?.drive
            }
        };
    }

    private static object BuildTaskStatus(MigrationTask task, IMigrationTaskStore? store = null)
    {
        var now = DateTime.UtcNow;
        var isStale = store?.IsStale(task, now);
        var isTerminal = IsTerminalState(task.State);
        var taskSucceeded = task.State == MigrationState.Completed;
        return new
        {
            success = true,
            taskId = task.Id,
            name = task.Name,
            state = task.State.ToString(),
            isTerminal,
            taskSucceeded,
            taskFailed = isTerminal && !taskSucceeded,
            progressPercent = task.ProgressPercent,
            copiedBytes = task.CopiedBytes,
            totalBytes = task.TotalBytes,
            copiedFiles = task.CopiedFiles,
            totalFiles = task.TotalFiles,
            currentFile = task.CurrentFile,
            sourcePath = task.SourcePath,
            targetPath = task.TargetPath,
            elapsedSeconds = task.StartedAt.HasValue
                ? (int)(DateTime.UtcNow - task.StartedAt.Value).TotalSeconds
                : 0,
            workerPid = task.WorkerProcessId,
            workerStartedAt = task.WorkerStartedAt,
            lastHeartbeatAt = task.LastHeartbeatAt,
            workerLogPath = task.WorkerLogPath,
            isStale,
            staleReason = isStale == true ? store?.GetStaleReason(task, now) : null,
            errorMessage = task.ErrorMessage,
            completedAt = task.CompletedAt
        };
    }

    private static object BuildUsage() => new
    {
        success = true,
        tool = "winC2D.Cli",
        version = GetVersion(),
        agentInstructions = new
        {
            intent = "Use this CLI when a user asks to move a Windows software folder to another drive.",
            naturalLanguageExample = "用 winC2D 把 \"C:\\Program Files\\TeamSpeak\" 移动到 \"D:\\Program Files\\\" 下",
            pathMapping = new
            {
                source = "Use the quoted source folder as --source.",
                target = "Use the destination root folder as --target, for example D:\\Program Files. The source folder name is appended automatically."
            },
            requiredWorkflow = new[]
            {
                "Run privilege-status first.",
                "Run disk-info to inspect available target drives.",
                "Run migrate with --dry-run before any real migration.",
                "Only run a real migration when dry-run returns success=true and canProceed=true.",
                "For real migration, add --yes and keep the same --source and --target values.",
                "Store the returned taskId.",
                "Poll status --task-id <taskId> until state is Completed, Failed, RolledBack, PartialRollback, or Cancelled.",
                "After Completed, verify that the source path is a symbolic link and the target path exists."
            },
            safetyRules = new[]
            {
                "Do not skip dry-run.",
                "Do not run migrate --yes if blockers is not empty.",
                "Do not migrate system folders such as C:\\Windows.",
                "If the source application is running, ask the user to close it before migration."
            },
            exampleCommands = new[]
            {
                "winC2D.Cli.exe privilege-status",
                "winC2D.Cli.exe disk-info",
                "winC2D.Cli.exe preflight --source \"C:\\Program Files\\TeamSpeak\" --target \"D:\\Program Files\"",
                "winC2D.Cli.exe migrate --source \"C:\\Program Files\\TeamSpeak\" --target \"D:\\Program Files\" --dry-run",
                "winC2D.Cli.exe migrate --source \"C:\\Program Files\\TeamSpeak\" --target \"D:\\Program Files\" --yes",
                "winC2D.Cli.exe status --task-id \"<taskId>\""
            }
        },
        commands = new[]
        {
            "version | --version",
            "help | --help",
            "privilege-status",
            "disk-info",
            "scan [--directories <comma-separated-paths>]",
            "preflight --source <path> --target <path> [--verify true|false]",
            "migrate --source <path> --target <path> [--dry-run] [--verify true|false] [--yes] [--wait] [--timeout-seconds 1800]",
            "status --task-id <id>",
            "pause --task-id <id>",
            "resume --task-id <id>",
            "cancel --task-id <id> [--no-rollback] --yes",
            "rollback --task-id <id> --yes",
            "list [--state all|completed|failed|running|stale]",
            "cleanup --state stale|terminal [--older-than-days 30] --yes"
        }
    };

    private static object BuildVersion() => new
    {
        success = true,
        tool = "winC2D.Cli",
        product = "winC2D",
        version = GetVersion(),
        informationalVersion = GetInformationalVersion(),
        framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        os = System.Runtime.InteropServices.RuntimeInformation.OSDescription
    };

    private static string GetVersion()
    {
        var assembly = typeof(CliApplication).Assembly;
        var version = assembly.GetName().Version;
        if (version is null)
            return "unknown";

        return version.Revision == 0
            ? version.ToString(3)
            : version.ToString();
    }

    private static string? GetInformationalVersion()
    {
        return typeof(CliApplication).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
    }

    private static async Task<WorkerStartResult> StartWorkerProcessAsync(
        string? executablePath,
        string taskId,
        IServiceProvider services,
        TextWriter stderr,
        CancellationToken cancellationToken)
    {
        try
        {
            executablePath ??= Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                return new WorkerStartResult(false, null, "CLI executable path was not found.");

            // Diagnostic: log worker start context
            var isAdmin = false;
            try
            {
                using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch { /* best-effort */ }
            await stderr.WriteLineAsync(
                $"[DIAG] Starting worker process: exe={executablePath}, isAdmin={isAdmin}, pid={Environment.ProcessId}");

            var engine = services.GetRequiredService<IMigrationEngine>();
            var task = await engine.GetTaskAsync(taskId);
            if (task is null)
                return new WorkerStartResult(false, null, $"Task not found: {taskId}");

            task.WorkerLogPath = EnsureWorkerLogPath(task);
            task.WorkerStartedAt = DateTime.UtcNow;

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            startInfo.ArgumentList.Add(InternalRunTaskCommand);
            startInfo.ArgumentList.Add("--task-id");
            startInfo.ArgumentList.Add(taskId);
            startInfo.ArgumentList.Add("--quiet");

            var process = Process.Start(startInfo);
            if (process is null)
                return new WorkerStartResult(false, null, "Process.Start returned null.");

            task.WorkerProcessId = process.Id;
            task.LastHeartbeatAt = DateTime.UtcNow;
            if (services.GetService<IMigrationTaskStore>() is { } store)
                await store.UpsertAsync(task, immediate: true, cancellationToken);

            return new WorkerStartResult(true, process.Id, null);
        }
        catch (Exception ex)
        {
            stderr.WriteLine(ex.ToString());
            return new WorkerStartResult(false, null, ex.Message);
        }
    }

    private static async Task<bool> TryElevateAsync(string? executablePath, string[] args, TextWriter stderr)
    {
        try
        {
            executablePath ??= Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            {
                await stderr.WriteLineAsync("[DIAG] TryElevateAsync: executable path not found.");
                return false;
            }

            await stderr.WriteLineAsync($"[DIAG] Attempting elevation via runas: {executablePath}");

            var psi = new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = true,
                Verb = "runas"
            };

            // Forward the original arguments to the elevated process
            foreach (var arg in BuildForwardedArgs(args))
                psi.ArgumentList.Add(arg);

            Process.Start(psi);
            return true;
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // Error 1223: user clicked "No" on the UAC prompt
            await stderr.WriteLineAsync("[DIAG] User declined UAC elevation prompt.");
            return false;
        }
        catch (Exception ex)
        {
            await stderr.WriteLineAsync($"[DIAG] TryElevateAsync failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Build a PowerShell command that elevates via Start-Process -Verb RunAs.
    /// No extra install required — Windows built-in.
    /// </summary>
    private static string BuildPwshElevationCommand(string? executablePath, string[] args)
    {
        var exe = executablePath ?? "winC2D.Cli.exe";
        var forwarded = BuildForwardedArgs(args);
        var argString = string.Join(" ", forwarded.Select(a => a.Contains(' ') ? $"`\"{a}`\"" : a));
        return $"Start-Process -Verb RunAs -FilePath '{exe}' -ArgumentList '{argString}'";
    }

    /// <summary>
    /// Build a ready-to-execute command string using gsudo for elevation.
    /// The agent can copy-paste this to retry the migration as admin.
    /// </summary>
    private static string BuildGsudoCommand(string? executablePath, string[] args)
    {
        var exe = executablePath ?? "winC2D.Cli.exe";
        var forwarded = BuildForwardedArgs(args);
        return $"gsudo {exe} {string.Join(" ", forwarded.Select(a => a.Contains(' ') ? $"\"{a}\"" : a))}";
    }

    /// <summary>
    /// Build the argument list to forward to the elevated child process,
    /// stripping any --json markers and ensuring --yes is present.
    /// </summary>
    private static List<string> BuildForwardedArgs(string[] args)
    {
        var result = new List<string>();
        var hasYes = false;

        foreach (var arg in args)
        {
            if (string.Equals(arg, "--json", StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.Equals(arg, "--yes", StringComparison.OrdinalIgnoreCase))
                hasYes = true;
            result.Add(arg);
        }

        if (!hasYes)
            result.Add("--yes");

        return result;
    }

    private static string EnsureWorkerLogPath(MigrationTask task)
    {
        if (!string.IsNullOrWhiteSpace(task.WorkerLogPath))
            return task.WorkerLogPath;

        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (string.IsNullOrWhiteSpace(localAppData))
            localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logDir = Path.Combine(localAppData, "winC2D", "logs", "tasks");
        Directory.CreateDirectory(logDir);
        return Path.Combine(logDir, $"{task.Id}.log");
    }

    private static async Task AppendWorkerLogAsync(string path, string message)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.AppendAllTextAsync(path, $"[{DateTime.UtcNow:O}] {message}{Environment.NewLine}");
    }

    private static List<string> NormalizeArgs(string[] args)
    {
        var result = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--json", StringComparison.OrdinalIgnoreCase))
                continue;
            result.Add(args[i]);
        }

        return result;
    }

    private static CliArguments Parse(string[] args, string[] valueOptions, string[] flags)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var parsedFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var positionals = new List<string>();
        var valueSet = valueOptions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var flagSet = flags.ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                positionals.Add(arg);
                continue;
            }

            var name = arg[2..];
            if (flagSet.Contains(name))
            {
                parsedFlags.Add(name);
                continue;
            }

            if (!valueSet.Contains(name))
                throw new CliParseException($"Unknown option: --{name}");

            if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                throw new CliParseException($"Option --{name} requires a value.");

            values[name] = args[++i];
        }

        return new CliArguments(values, parsedFlags, positionals);
    }

    private static void EnsureNoPositionals(CliArguments parsed)
    {
        if (parsed.Positionals.Count > 0)
            throw new CliParseException($"Unexpected positional argument: {parsed.Positionals[0]}");
    }

    private static string Require(CliArguments parsed, string option)
    {
        if (!parsed.TryGetValue(option, out var value) || string.IsNullOrWhiteSpace(value))
            throw new CliParseException($"Missing required option: --{option}");
        return value;
    }

    private static bool ParseBool(string? value, bool defaultValue, string optionName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;
        if (bool.TryParse(value, out var parsed))
            return parsed;
        throw new CliParseException($"Option --{optionName} must be true or false.");
    }

    private static int ParseInt(string? value, int defaultValue, string optionName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;
        if (int.TryParse(value, out var parsed) && parsed >= 0)
            return parsed;
        throw new CliParseException($"Option --{optionName} must be a non-negative integer.");
    }

    private static bool IsRunningState(MigrationTask task) => task.State is
        MigrationState.Pending or
        MigrationState.Preparing or
        MigrationState.Copying or
        MigrationState.CreatingSymlink or
        MigrationState.CleaningUp or
        MigrationState.RollingBack or
        MigrationState.Paused;

    private static bool IsTerminalState(MigrationState state) => state is
        MigrationState.Completed or
        MigrationState.Failed or
        MigrationState.RolledBack or
        MigrationState.PartialRollback or
        MigrationState.Cancelled;

    private static object Error(string error, string message, object? extra = null)
    {
        if (extra is null)
            return new { success = false, error, message };

        return new { success = false, error, message, details = extra };
    }

    private static async Task<int> WriteAsync(TextWriter stdout, CliExitCode exitCode, object payload)
    {
        await stdout.WriteLineAsync(JsonSerializer.Serialize(payload, JsonOptions));
        await stdout.FlushAsync();
        return (int)exitCode;
    }

    private sealed record SoftwareItemDto(
        string Name,
        string InstallLocation,
        double SizeMb,
        string Status,
        bool? IsSymlink);

    private sealed record WorkerStartResult(bool Success, int? ProcessId, string? ErrorMessage);

    private sealed class CliArguments(
        Dictionary<string, string> values,
        HashSet<string> flags,
        List<string> positionals)
    {
        public List<string> Positionals { get; } = positionals;
        public bool HasFlag(string name) => flags.Contains(name);
        public bool TryGetValue(string name, out string? value) => values.TryGetValue(name, out value);
        public string? GetValueOrDefault(string name) => values.GetValueOrDefault(name);
    }

    private sealed class CliParseException(string message) : Exception(message);
}
