using System.Diagnostics;
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

    private static readonly string[] Blacklist =
    [
        @"C:\Windows",
        @"C:\Program Files\Windows NT",
        @"C:\Program Files\Common Files",
        @"C:\Program Files\Windows Defender",
        @"C:\Program Files (x86)\Windows NT",
        @"C:\Program Files (x86)\Common Files",
    ];

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
                return await WriteAsync(stdout, CliExitCode.ArgumentError, Error("MISSING_COMMAND", "A CLI command is required."));

            var command = normalized[0].ToLowerInvariant();
            var commandArgs = normalized.Skip(1).ToArray();

            return command switch
            {
                "privilege-status" => await WriteAsync(stdout, CliExitCode.Success, BuildPrivilegeStatus()),
                "disk-info" => await WriteAsync(stdout, CliExitCode.Success, BuildDiskInfo(services)),
                "scan" => await ScanAsync(commandArgs, services, stdout, cancellationToken),
                "migrate" => await MigrateAsync(commandArgs, services, stdout, stderr, serviceProviderFactory, executablePath, cancellationToken),
                "status" => await StatusAsync(commandArgs, services, stdout),
                "rollback" => await RollbackAsync(commandArgs, services, stdout, cancellationToken),
                "list" => await ListAsync(commandArgs, services, stdout),
                "help" or "--help" or "-h" => await WriteAsync(stdout, CliExitCode.Success, BuildUsage()),
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
            valueOptions: ["source", "target-drive", "target-subfolder", "verify", "timeout-seconds"],
            flags: ["dry-run", "yes", "wait"]);
        EnsureNoPositionals(parsed);

        var sourcePath = Require(parsed, "source").TrimEnd('\\', '/');
        var targetDrive = NormalizeDrive(Require(parsed, "target-drive"));
        var targetSubFolder = parsed.GetValueOrDefault("target-subfolder") ?? "MigratedApps";
        var dryRun = parsed.HasFlag("dry-run");
        var wait = parsed.HasFlag("wait");
        var verifyFiles = ParseBool(parsed.GetValueOrDefault("verify"), defaultValue: true, "verify");
        var timeoutSeconds = ParseInt(parsed.GetValueOrDefault("timeout-seconds"), defaultValue: 0, "timeout-seconds");

        if (!dryRun && !parsed.HasFlag("yes"))
            return await WriteAsync(stdout, CliExitCode.ArgumentError, Error("CONFIRMATION_REQUIRED", "Real migrations require --yes. Use --dry-run first to validate."));

        if (!dryRun && !PrivilegeChecker.CanMigrate())
            return await WriteAsync(stdout, CliExitCode.InsufficientPrivileges, PrivilegeChecker.BuildInsufficientPrivilegesError());

        var validation = ValidateMigrationRequest(services, sourcePath, targetDrive, targetSubFolder);

        if (dryRun)
        {
            return await WriteAsync(stdout, validation.Blockers.Count == 0 ? CliExitCode.Success : CliExitCode.BusinessFailure, new
            {
                success = validation.Blockers.Count == 0,
                dryRun = true,
                sourcePath,
                targetPath = validation.TargetPath,
                sourceSizeMb = Math.Round(validation.SourceSizeBytes / 1_048_576.0, 1),
                targetDriveFreeGb = Math.Round(validation.TargetDriveFreeBytes / 1_073_741_824.0, 1),
                hasEnoughSpace = validation.HasEnoughSpace,
                isSourceSymlink = validation.IsSourceSymlink,
                canProceed = validation.Blockers.Count == 0,
                blockers = validation.Blockers
            });
        }

        if (validation.Blockers.Count > 0)
        {
            return await WriteAsync(stdout, CliExitCode.BusinessFailure, new
            {
                success = false,
                error = "VALIDATION_FAILED",
                message = "Migration validation failed.",
                blockers = validation.Blockers
            });
        }

        var engine = services.GetRequiredService<IMigrationEngine>();
        var task = await engine.CreateTaskAsync(new MigrationRequest
        {
            Name = Path.GetFileName(sourcePath),
            SourcePath = sourcePath,
            TargetRootPath = Path.Combine(targetDrive.TrimEnd('\\') + "\\", targetSubFolder),
            Type = MigrationType.Software,
            CreateSymlink = true,
            VerifyFiles = verifyFiles
        }, cancellationToken);

        var workerStarted = StartWorkerProcess(executablePath, task.Id, stderr);
        if (!workerStarted)
        {
            return await WriteAsync(stdout, CliExitCode.UnhandledException, Error("WORKER_START_FAILED", "Could not start the hidden CLI worker process."));
        }

        if (wait)
            return await WaitForTaskAsync(task.Id, serviceProviderFactory, services, stdout, timeoutSeconds, cancellationToken);

        return await WriteAsync(stdout, CliExitCode.Success, new
        {
            success = true,
            taskId = task.Id,
            status = "running",
            sourcePath,
            targetPath = task.TargetPath,
            sizeMb = Math.Round(validation.SourceSizeBytes / 1_048_576.0, 1),
            message = "Migration started. Call status with this taskId until state is Completed, Failed, RolledBack, or PartialRollback."
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

        return await WriteAsync(stdout, CliExitCode.Success, BuildTaskStatus(task));
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

        if (stateFilter is not ("all" or "completed" or "failed" or "running"))
            return await WriteAsync(stdout, CliExitCode.ArgumentError, Error("INVALID_STATE_FILTER", "State must be one of: all, completed, failed, running."));

        var all = await services.GetRequiredService<IMigrationEngine>().GetAllTasksAsync();
        var filtered = stateFilter switch
        {
            "completed" => all.Where(t => t.State == MigrationState.Completed),
            "failed" => all.Where(t => t.State is MigrationState.Failed or MigrationState.RolledBack or MigrationState.PartialRollback),
            "running" => all.Where(IsRunningState),
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

        var result = await engine.ExecuteAsync(task, cancellationToken);
        var updated = await engine.GetTaskAsync(taskId);

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
                return await WriteAsync(stdout, task.State == MigrationState.Completed ? CliExitCode.Success : CliExitCode.BusinessFailure, BuildTaskStatus(task));

            if (timeoutSeconds > 0 && (DateTime.UtcNow - startedAt).TotalSeconds >= timeoutSeconds)
            {
                return await WriteAsync(stdout, CliExitCode.BusinessFailure, new
                {
                    success = false,
                    error = "WAIT_TIMEOUT",
                    message = $"Timed out waiting for task {taskId}. The hidden worker may still be running; call status to continue polling.",
                    task = BuildTaskStatus(task)
                });
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }

    private static MigrationValidation ValidateMigrationRequest(
        IServiceProvider services,
        string sourcePath,
        string targetDrive,
        string targetSubFolder)
    {
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var folderName = Path.GetFileName(sourcePath);
        var targetPath = Path.Combine(targetDrive.TrimEnd('\\') + "\\", targetSubFolder, folderName);
        var blockers = new List<string>();

        var sourceExists = fileSystem.DirectoryExists(sourcePath);
        if (!sourceExists)
            blockers.Add($"Source path does not exist: {sourcePath}");

        var isSymlink = sourceExists && fileSystem.IsSymlink(sourcePath);
        if (isSymlink)
            blockers.Add("Source is already a symbolic link. Use rollback to undo an existing migration first.");

        foreach (var blocked in Blacklist)
        {
            if (sourcePath.StartsWith(blocked, StringComparison.OrdinalIgnoreCase))
            {
                blockers.Add($"Source path is in the system blacklist: {blocked}");
                break;
            }
        }

        long sourceSizeBytes = 0;
        if (sourceExists && !isSymlink)
        {
            try
            {
                sourceSizeBytes = fileSystem.GetDirectorySize(sourcePath);
            }
            catch (Exception ex)
            {
                blockers.Add($"Cannot read source directory: {ex.Message}");
            }
        }

        var targetDriveInfo = fileSystem.GetDrives()
            .FirstOrDefault(d => d.IsReady &&
                                 d.RootDirectory.FullName.StartsWith(
                                     targetDrive.TrimEnd('\\') + "\\",
                                     StringComparison.OrdinalIgnoreCase));

        if (targetDriveInfo is null)
            blockers.Add($"Target drive not found or not ready: {targetDrive}");

        var targetDriveFreeBytes = targetDriveInfo?.AvailableFreeSpace ?? 0L;
        var hasEnoughSpace = targetDriveFreeBytes > (long)(sourceSizeBytes * 1.1);
        if (targetDriveInfo is not null && !hasEnoughSpace)
        {
            blockers.Add($"Insufficient disk space on {targetDrive}. Need {sourceSizeBytes / 1_048_576.0:F0} MB, available {targetDriveFreeBytes / 1_048_576.0:F0} MB.");
        }

        if (fileSystem.DirectoryExists(targetPath) || fileSystem.FileExists(targetPath))
            blockers.Add($"Target path already exists: {targetPath}");

        return new MigrationValidation(
            targetPath,
            sourceSizeBytes,
            targetDriveFreeBytes,
            hasEnoughSpace,
            isSymlink,
            blockers);
    }

    private static object BuildPrivilegeStatus()
    {
        var level = PrivilegeChecker.GetLevel();
        var canMigrate = level != PrivilegeChecker.Level.Restricted;

        return new
        {
            success = true,
            privilegeLevel = level.ToString(),
            canMigrate,
            canScan = true,
            details = level switch
            {
                PrivilegeChecker.Level.Administrator => "Running as administrator. All operations are available.",
                PrivilegeChecker.Level.DeveloperMode => "Windows Developer Mode is enabled. Symlink creation does not require elevation.",
                _ => "Running without elevated privileges and Developer Mode is not enabled. Scan and disk info are available; migration requires elevated privileges."
            },
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

    private static object BuildTaskStatus(MigrationTask task)
    {
        var success = task.State is not (MigrationState.Failed or MigrationState.PartialRollback);
        return new
        {
            success,
            taskId = task.Id,
            name = task.Name,
            state = task.State.ToString(),
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
            errorMessage = task.ErrorMessage,
            completedAt = task.CompletedAt
        };
    }

    private static object BuildUsage() => new
    {
        success = true,
        commands = new[]
        {
            "privilege-status",
            "disk-info",
            "scan [--directories <comma-separated-paths>]",
            "migrate --source <path> --target-drive <drive> [--target-subfolder MigratedApps] [--dry-run] [--verify true|false] [--yes] [--wait] [--timeout-seconds 0]",
            "status --task-id <id>",
            "rollback --task-id <id> --yes",
            "list [--state all|completed|failed|running]"
        }
    };

    private static bool StartWorkerProcess(string? executablePath, string taskId, TextWriter stderr)
    {
        try
        {
            executablePath ??= Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                return false;

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = $"--cli {InternalRunTaskCommand} --task-id {taskId} --quiet"
            };

            Process.Start(startInfo);
            return true;
        }
        catch (Exception ex)
        {
            stderr.WriteLine(ex.ToString());
            return false;
        }
    }

    private static List<string> NormalizeArgs(string[] args)
    {
        var result = new List<string>();
        var start = 0;
        var cliIndex = Array.FindIndex(args, a => string.Equals(a, "--cli", StringComparison.OrdinalIgnoreCase));
        if (cliIndex >= 0)
            start = cliIndex + 1;

        for (var i = start; i < args.Length; i++)
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

    private static string NormalizeDrive(string drive)
    {
        drive = drive.Trim().TrimEnd('\\', '/');
        if (drive.Length == 1 && char.IsLetter(drive[0]))
            drive += ":";
        if (drive.Length != 2 || drive[1] != ':' || !char.IsLetter(drive[0]))
            throw new CliParseException("--target-drive must be a drive letter such as D:.");
        return drive.ToUpperInvariant();
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

    private sealed record MigrationValidation(
        string TargetPath,
        long SourceSizeBytes,
        long TargetDriveFreeBytes,
        bool HasEnoughSpace,
        bool IsSourceSymlink,
        List<string> Blockers);

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
