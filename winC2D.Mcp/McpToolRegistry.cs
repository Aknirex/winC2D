using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using winC2D.Core.Models;
using winC2D.Core.Services;
using winC2D.Mcp.Protocol;

namespace winC2D.Mcp;

/// <summary>
/// Registry of all MCP tools exposed by winC2D. Defines tool schemas and
/// dispatches tool calls to the underlying migration engine and services.
/// </summary>
public sealed class McpToolRegistry
{
    private readonly IServiceProvider _services;
    private readonly List<ToolDefinition> _tools;

    public McpToolRegistry(IServiceProvider services)
    {
        _services = services;
        _tools = BuildToolDefinitions();
    }

    public List<ToolDefinition> GetTools() => _tools;

    public async Task<ToolCallResult> CallToolAsync(
        string toolName,
        Dictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return toolName switch
            {
                "privilege_status" => await HandlePrivilegeStatusAsync(cancellationToken),
                "disk_info" => await HandleDiskInfoAsync(cancellationToken),
                "scan_software" => await HandleScanAsync(arguments, cancellationToken),
                "preflight" => await HandlePreflightAsync(arguments, cancellationToken),
                "migrate" => await HandleMigrateAsync(arguments, cancellationToken),
                "migration_status" => await HandleStatusAsync(arguments, cancellationToken),
                "pause_migration" => await HandlePauseAsync(arguments, cancellationToken),
                "resume_migration" => await HandleResumeAsync(arguments, cancellationToken),
                "cancel_migration" => await HandleCancelAsync(arguments, cancellationToken),
                "rollback" => await HandleRollbackAsync(arguments, cancellationToken),
                "list_tasks" => await HandleListTasksAsync(cancellationToken),
                "cleanup_tasks" => await HandleCleanupAsync(arguments, cancellationToken),
                _ => ErrorResult($"Unknown tool: {toolName}")
            };
        }
        catch (Exception ex)
        {
            return ErrorResult($"Tool execution failed: {ex.Message}");
        }
    }

    // ── Tool Definitions ────────────────────────────────────────

    private static List<ToolDefinition> BuildToolDefinitions()
    {
        return new List<ToolDefinition>
        {
            new()
            {
                Name = "privilege_status",
                Description = "Check current privilege level (Administrator/Developer Mode/Restricted) and which operations are available. Call this first to verify the environment can perform migrations.",
                InputSchema = new ToolInputSchema
                {
                    Properties = new Dictionary<string, ToolProperty>(),
                    Required = new List<string>()
                }
            },
            new()
            {
                Name = "disk_info",
                Description = "List all fixed drives with free space, total capacity, and drive letter. Use this to identify viable target drives for migration.",
                InputSchema = new ToolInputSchema
                {
                    Properties = new Dictionary<string, ToolProperty>(),
                    Required = new List<string>()
                }
            },
            new()
            {
                Name = "scan_software",
                Description = "Scan installed software on the system. Returns application name, install path, size (MB), and migration status (Normal/Symlink/Unknown). Use --directories to specify custom scan paths.",
                InputSchema = new ToolInputSchema
                {
                    Properties = new Dictionary<string, ToolProperty>
                    {
                        ["directories"] = new()
                        {
                            Type = "string",
                            Description = "Comma-separated list of directories to scan. Defaults to common install locations (Program Files, Program Files (x86))."
                        }
                    },
                    Required = new List<string>()
                }
            },
            new()
            {
                Name = "preflight",
                Description = "Validate whether a migration can proceed without making any changes. Checks source existence, target space, path conflicts, and permission issues. Always call this before migrate.",
                InputSchema = new ToolInputSchema
                {
                    Properties = new Dictionary<string, ToolProperty>
                    {
                        ["source"] = new()
                        {
                            Type = "string",
                            Description = "Absolute path of the source directory to migrate (e.g., C:\\Program Files\\App)."
                        },
                        ["target"] = new()
                        {
                            Type = "string",
                            Description = "Absolute path of the target root directory (e.g., D:\\MigratedApps). The source folder name is auto-appended."
                        },
                        ["verify"] = new()
                        {
                            Type = "boolean",
                            Description = "Enable post-copy file verification. Default: true."
                        }
                    },
                    Required = new List<string> { "source", "target" }
                }
            },
            new()
            {
                Name = "migrate",
                Description = "Execute a software/AppData migration. Copies the source directory to the target drive and creates a symbolic link at the original path. Requires --yes to confirm non-dry-run execution. Use --dry-run first.",
                InputSchema = new ToolInputSchema
                {
                    Properties = new Dictionary<string, ToolProperty>
                    {
                        ["source"] = new()
                        {
                            Type = "string",
                            Description = "Absolute path of the source directory."
                        },
                        ["target"] = new()
                        {
                            Type = "string",
                            Description = "Absolute path of the target root directory."
                        },
                        ["dry_run"] = new()
                        {
                            Type = "boolean",
                            Description = "Validate preconditions without making changes. Default: false."
                        },
                        ["yes"] = new()
                        {
                            Type = "boolean",
                            Description = "Confirm real migration (safety gate). Required for non-dry-run."
                        },
                        ["verify"] = new()
                        {
                            Type = "boolean",
                            Description = "Enable post-copy file verification. Default: true."
                        },
                        ["wait"] = new()
                        {
                            Type = "boolean",
                            Description = "Block until migration completes. Default: false (returns taskId immediately)."
                        },
                        ["timeout_seconds"] = new()
                        {
                            Type = "integer",
                            Description = "Maximum wait time in seconds when --wait is set. Default: 1800 (30 min)."
                        }
                    },
                    Required = new List<string> { "source", "target" }
                }
            },
            new()
            {
                Name = "migration_status",
                Description = "Poll migration progress by task ID. Returns current state, progress percentage, bytes/files copied, and whether the task is in a terminal state.",
                InputSchema = new ToolInputSchema
                {
                    Properties = new Dictionary<string, ToolProperty>
                    {
                        ["task_id"] = new()
                        {
                            Type = "string",
                            Description = "Task ID returned by the migrate command."
                        }
                    },
                    Required = new List<string> { "task_id" }
                }
            },
            new()
            {
                Name = "pause_migration",
                Description = "Pause a running migration task. The task can be resumed later with resume_migration.",
                InputSchema = new ToolInputSchema
                {
                    Properties = new Dictionary<string, ToolProperty>
                    {
                        ["task_id"] = new()
                        {
                            Type = "string",
                            Description = "Task ID to pause."
                        }
                    },
                    Required = new List<string> { "task_id" }
                }
            },
            new()
            {
                Name = "resume_migration",
                Description = "Resume a paused migration task from where it stopped.",
                InputSchema = new ToolInputSchema
                {
                    Properties = new Dictionary<string, ToolProperty>
                    {
                        ["task_id"] = new()
                        {
                            Type = "string",
                            Description = "Task ID to resume."
                        }
                    },
                    Required = new List<string> { "task_id" }
                }
            },
            new()
            {
                Name = "cancel_migration",
                Description = "Cancel a running migration task. Optionally triggers rollback of any changes made so far.",
                InputSchema = new ToolInputSchema
                {
                    Properties = new Dictionary<string, ToolProperty>
                    {
                        ["task_id"] = new()
                        {
                            Type = "string",
                            Description = "Task ID to cancel."
                        },
                        ["rollback"] = new()
                        {
                            Type = "boolean",
                            Description = "Whether to rollback changes. Default: true."
                        }
                    },
                    Required = new List<string> { "task_id" }
                }
            },
            new()
            {
                Name = "rollback",
                Description = "Restore a migrated folder to its original path and remove the symbolic link. Requires --yes for safety.",
                InputSchema = new ToolInputSchema
                {
                    Properties = new Dictionary<string, ToolProperty>
                    {
                        ["task_id"] = new()
                        {
                            Type = "string",
                            Description = "Task ID of the completed migration to rollback."
                        },
                        ["yes"] = new()
                        {
                            Type = "boolean",
                            Description = "Confirm rollback. Required."
                        }
                    },
                    Required = new List<string> { "task_id", "yes" }
                }
            },
            new()
            {
                Name = "list_tasks",
                Description = "List all persisted migration tasks with their current state, source/target paths, and timestamps.",
                InputSchema = new ToolInputSchema
                {
                    Properties = new Dictionary<string, ToolProperty>(),
                    Required = new List<string>()
                }
            },
            new()
            {
                Name = "cleanup_tasks",
                Description = "Remove completed/failed/cancelled tasks from the persistent store. Use --older_than_days to keep recent history.",
                InputSchema = new ToolInputSchema
                {
                    Properties = new Dictionary<string, ToolProperty>
                    {
                        ["older_than_days"] = new()
                        {
                            Type = "integer",
                            Description = "Only remove tasks older than this many days. Default: 30."
                        }
                    },
                    Required = new List<string>()
                }
            }
        };
    }

    // ── Tool Handlers ───────────────────────────────────────────

    private Task<ToolCallResult> HandlePrivilegeStatusAsync(CancellationToken ct)
    {
        var level = Cli.PrivilegeChecker.GetLevel();
        var canMigrate = Cli.PrivilegeChecker.CanMigrate();

        object status;
        if (canMigrate)
        {
            status = new
            {
                success = true,
                privilegeLevel = level.ToString(),
                canScan = true,
                canMigrate = true,
                message = level == Cli.PrivilegeChecker.Level.Administrator
                    ? "Running with full administrator privileges."
                    : "Running with Developer Mode enabled (no elevation required)."
            };
        }
        else
        {
            status = Cli.PrivilegeChecker.BuildInsufficientPrivilegesError();
        }

        return Task.FromResult(JsonResult(status));
    }

    private async Task<ToolCallResult> HandleDiskInfoAsync(CancellationToken ct)
    {
        var fs = _services.GetRequiredService<Core.FileSystem.IFileSystem>();
        var drives = await Task.Run(() =>
            DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
                .Select(d => new
                {
                    name = d.Name.TrimEnd('\\'),
                    label = string.IsNullOrWhiteSpace(d.VolumeLabel) ? "Local Disk" : d.VolumeLabel,
                    totalBytes = d.TotalSize,
                    freeBytes = d.TotalFreeSpace,
                    usedBytes = d.TotalSize - d.TotalFreeSpace,
                    totalGb = Math.Round(d.TotalSize / 1_073_741_824.0, 1),
                    freeGb = Math.Round(d.TotalFreeSpace / 1_073_741_824.0, 1),
                    usedGb = Math.Round((d.TotalSize - d.TotalFreeSpace) / 1_073_741_824.0, 1),
                    usagePercent = Math.Round((d.TotalSize - d.TotalFreeSpace) * 100.0 / d.TotalSize, 1)
                })
                .ToList(),
            ct);

        return JsonResult(new { success = true, drives });
    }

    private async Task<ToolCallResult> HandleScanAsync(Dictionary<string, object?> args, CancellationToken ct)
    {
        var scanner = _services.GetRequiredService<ISoftwareScanner>();
        var directories = GetStringArg(args, "directories");
        var scanDirs = !string.IsNullOrWhiteSpace(directories)
            ? directories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : scanner.GetDefaultScanDirectories();

        var items = new List<object>();
        var byStatus = new Dictionary<string, int>();
        long totalBytes = 0;

        await foreach (var sw in scanner.ScanStreamAsync(scanDirs, cancellationToken: ct))
        {
            var status = sw.Status.ToString();
            byStatus[status] = byStatus.GetValueOrDefault(status) + 1;
            totalBytes += sw.SizeBytes;
            items.Add(new
            {
                name = sw.Name,
                installLocation = sw.InstallLocation,
                sizeMb = Math.Round(sw.SizeBytes / 1_048_576.0, 1),
                status,
                isSymlink = sw.IsSymlink ? true : (bool?)null
            });
        }

        var sorted = items
            .OrderBy(i => ((dynamic)i).status == "Normal" ? 0 : 1)
            .ThenByDescending(i => ((dynamic)i).sizeMb)
            .ToList();

        return JsonResult(new
        {
            success = true,
            scannedDirectories = scanDirs,
            summary = new
            {
                total = items.Count,
                byStatus,
                totalSizeGb = Math.Round(totalBytes / 1_073_741_824.0, 2),
                migratableSizeMb = Math.Round(
                    items.Where(i => ((dynamic)i).status == "Normal").Sum(i => ((dynamic)i).sizeMb), 1)
            },
            software = sorted
        });
    }

    private async Task<ToolCallResult> HandlePreflightAsync(Dictionary<string, object?> args, CancellationToken ct)
    {
        var sourcePath = RequireArg(args, "source");
        var targetRoot = RequireArg(args, "target");
        var verify = GetBoolArg(args, "verify") ?? true;

        var engine = _services.GetRequiredService<IMigrationEngine>();
        var validation = await engine.ValidateAsync(new MigrationRequest
        {
            Name = Path.GetFileName(sourcePath),
            SourcePath = sourcePath,
            TargetRootPath = targetRoot,
            Type = MigrationType.Software,
            VerifyFiles = verify
        }, ct);

        return JsonResult(new
        {
            success = validation.CanProceed,
            canProceed = validation.CanProceed,
            sourcePath = validation.SourcePath,
            targetPath = validation.TargetPath,
            sourceSizeBytes = validation.SourceSizeBytes,
            sourceSizeMb = Math.Round(validation.SourceSizeBytes / 1_048_576.0, 1),
            targetFreeBytes = validation.TargetFreeBytes,
            targetDriveFreeGb = Math.Round(validation.TargetFreeBytes / 1_073_741_824.0, 1),
            hasEnoughSpace = validation.HasEnoughSpace,
            isSourceSymlink = validation.IsSourceSymlink,
            blockers = validation.Blockers,
            warnings = validation.Warnings
        });
    }

    private async Task<ToolCallResult> HandleMigrateAsync(Dictionary<string, object?> args, CancellationToken ct)
    {
        var sourcePath = RequireArg(args, "source");
        var targetRoot = RequireArg(args, "target");
        var dryRun = GetBoolArg(args, "dry_run") ?? false;
        var yes = GetBoolArg(args, "yes") ?? false;
        var verify = GetBoolArg(args, "verify") ?? true;
        var wait = GetBoolArg(args, "wait") ?? false;
        var timeoutSeconds = GetIntArg(args, "timeout_seconds") ?? 1800;

        var engine = _services.GetRequiredService<IMigrationEngine>();

        // Always validate first
        var validation = await engine.ValidateAsync(new MigrationRequest
        {
            Name = Path.GetFileName(sourcePath),
            SourcePath = sourcePath,
            TargetRootPath = targetRoot,
            Type = MigrationType.Software,
            VerifyFiles = verify
        }, ct);

        if (!validation.CanProceed)
        {
            return JsonResult(new
            {
                success = false,
                error = "PRECHECK_FAILED",
                message = "Preflight validation failed. Fix the blockers and retry.",
                blockers = validation.Blockers,
                warnings = validation.Warnings
            });
        }

        if (dryRun)
        {
            return JsonResult(new
            {
                success = true,
                dryRun = true,
                message = "Dry-run passed. All preconditions are satisfied.",
                sourcePath = validation.SourcePath,
                targetPath = validation.TargetPath,
                sourceSizeMb = Math.Round(validation.SourceSizeBytes / 1_048_576.0, 1),
                targetDriveFreeGb = Math.Round(validation.TargetFreeBytes / 1_073_741_824.0, 1)
            });
        }

        if (!yes)
        {
            return JsonResult(new
            {
                success = false,
                error = "CONFIRMATION_REQUIRED",
                message = "Real migrations require --yes. Use --dry_run first to validate, then --yes to confirm."
            });
        }

        var task = await engine.CreateTaskAsync(new MigrationRequest
        {
            Name = Path.GetFileName(sourcePath),
            SourcePath = sourcePath,
            TargetRootPath = targetRoot,
            Type = MigrationType.Software,
            CreateSymlink = true,
            VerifyFiles = verify
        }, ct);

        if (wait)
        {
            using var waitCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, waitCts.Token);
            var result = await engine.ExecuteAsync(task, linked.Token);
            return JsonResult(new
            {
                success = result.Success,
                taskId = task.Id,
                finalState = result.FinalState.ToString(),
                durationSeconds = Math.Round(result.Duration.TotalSeconds, 1),
                errorMessage = result.ErrorMessage,
                wasRolledBack = result.WasRolledBack
            });
        }
        else
        {
            // Fire-and-forget: return taskId immediately, migration runs in background
            _ = Task.Run(async () =>
            {
                try { await engine.ExecuteAsync(task, CancellationToken.None); }
                catch { /* logged by engine */ }
            });

            return JsonResult(new
            {
                success = true,
                taskId = task.Id,
                message = "Migration started. Poll migration_status with this taskId to track progress.",
                state = task.State.ToString()
            });
        }
    }

    private async Task<ToolCallResult> HandleStatusAsync(Dictionary<string, object?> args, CancellationToken ct)
    {
        var taskId = RequireArg(args, "task_id");
        var engine = _services.GetRequiredService<IMigrationEngine>();
        var task = await engine.GetTaskAsync(taskId);

        if (task == null)
            return ErrorResult($"Task not found: {taskId}");

        var isTerminal = task.State is MigrationState.Completed
            or MigrationState.Failed
            or MigrationState.RolledBack
            or MigrationState.PartialRollback
            or MigrationState.Cancelled;

        return JsonResult(new
        {
            success = true,
            taskId = task.Id,
            taskSucceeded = task.State == MigrationState.Completed,
            taskFailed = task.State is MigrationState.Failed or MigrationState.PartialRollback,
            isTerminal,
            state = task.State.ToString(),
            name = task.Name,
            sourcePath = task.SourcePath,
            targetPath = task.TargetPath,
            progressPercent = task.ProgressPercent,
            bytesCopied = task.CopiedBytes,
            totalBytes = task.TotalBytes,
            filesCopied = task.CopiedFiles,
            totalFiles = task.TotalFiles,
            currentFile = task.CurrentFile,
            errorMessage = task.ErrorMessage,
            createdAt = task.CreatedAt,
            startedAt = task.StartedAt,
            completedAt = task.CompletedAt
        });
    }

    private async Task<ToolCallResult> HandlePauseAsync(Dictionary<string, object?> args, CancellationToken ct)
    {
        var taskId = RequireArg(args, "task_id");
        var engine = _services.GetRequiredService<IMigrationEngine>();
        await engine.PauseAsync(taskId);
        return JsonResult(new { success = true, taskId, message = "Pause requested." });
    }

    private async Task<ToolCallResult> HandleResumeAsync(Dictionary<string, object?> args, CancellationToken ct)
    {
        var taskId = RequireArg(args, "task_id");
        var engine = _services.GetRequiredService<IMigrationEngine>();
        var result = await engine.ResumeAsync(taskId, ct);
        return JsonResult(new { success = result.Success, taskId, state = result.FinalState.ToString() });
    }

    private async Task<ToolCallResult> HandleCancelAsync(Dictionary<string, object?> args, CancellationToken ct)
    {
        var taskId = RequireArg(args, "task_id");
        var rollback = GetBoolArg(args, "rollback") ?? true;
        var engine = _services.GetRequiredService<IMigrationEngine>();
        var result = await engine.CancelAsync(taskId, rollback, ct);
        return JsonResult(new
        {
            success = true,
            taskId,
            cancelled = true,
            wasRolledBack = result.WasRolledBack,
            finalState = result.FinalState.ToString()
        });
    }

    private async Task<ToolCallResult> HandleRollbackAsync(Dictionary<string, object?> args, CancellationToken ct)
    {
        var taskId = RequireArg(args, "task_id");
        var yes = GetBoolArg(args, "yes") ?? false;

        if (!yes)
            return ErrorResult("Rollback requires --yes confirmation.");

        var engine = _services.GetRequiredService<IMigrationEngine>();
        var canRollback = await engine.CanRollbackAsync(taskId);
        if (!canRollback)
            return ErrorResult($"Task {taskId} cannot be rolled back (no rollback point or already rolled back).");

        var result = await engine.RollbackAsync(taskId, ct);
        return JsonResult(new
        {
            success = result.Success,
            taskId,
            wasRolledBack = result.Success,
            errorMessage = result.ErrorMessage
        });
    }

    private async Task<ToolCallResult> HandleListTasksAsync(CancellationToken ct)
    {
        var engine = _services.GetRequiredService<IMigrationEngine>();
        var tasks = await engine.GetAllTasksAsync();
        var list = tasks.Select(t => new
        {
            taskId = t.Id,
            name = t.Name,
            state = t.State.ToString(),
            sourcePath = t.SourcePath,
            targetPath = t.TargetPath,
            totalSizeMb = Math.Round(t.TotalBytes / 1_048_576.0, 1),
            progressPercent = t.ProgressPercent,
            errorMessage = t.ErrorMessage,
            createdAt = t.CreatedAt,
            completedAt = t.CompletedAt
        }).ToList();

        return JsonResult(new { success = true, total = list.Count, tasks = list });
    }

    private async Task<ToolCallResult> HandleCleanupAsync(Dictionary<string, object?> args, CancellationToken ct)
    {
        var olderThanDays = GetIntArg(args, "older_than_days") ?? 30;
        var engine = _services.GetRequiredService<IMigrationEngine>();
        var removed = await engine.CleanupTasksAsync(new TaskCleanupOptions
        {
            OlderThanDays = olderThanDays
        }, ct);

        return JsonResult(new { success = true, removed, olderThanDays });
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static string RequireArg(Dictionary<string, object?> args, string key)
    {
        if (!args.TryGetValue(key, out var value) || value == null)
            throw new ArgumentException($"Missing required argument: {key}");
        return value.ToString()!;
    }

    private static string? GetStringArg(Dictionary<string, object?> args, string key)
    {
        return args.TryGetValue(key, out var value) && value != null ? value.ToString() : null;
    }

    private static bool? GetBoolArg(Dictionary<string, object?> args, string key)
    {
        if (!args.TryGetValue(key, out var value) || value == null) return null;
        if (value is bool b) return b;
        if (value is JsonElement je && je.ValueKind == JsonValueKind.True) return true;
        if (value is JsonElement je2 && je2.ValueKind == JsonValueKind.False) return false;
        if (bool.TryParse(value.ToString(), out var parsed)) return parsed;
        return null;
    }

    private static int? GetIntArg(Dictionary<string, object?> args, string key)
    {
        if (!args.TryGetValue(key, out var value) || value == null) return null;
        if (value is int i) return i;
        if (value is JsonElement je && je.ValueKind == JsonValueKind.Number && je.TryGetInt32(out var ji)) return ji;
        if (int.TryParse(value.ToString(), out var parsed)) return parsed;
        return null;
    }

    private static ToolCallResult JsonResult(object data)
    {
        return new ToolCallResult
        {
            Content = new List<ContentBlock>
            {
                new()
                {
                    Type = "text",
                    Text = JsonSerializer.Serialize(data, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    }),
                    MimeType = "application/json"
                }
            }
        };
    }

    private static ToolCallResult ErrorResult(string message)
    {
        return new ToolCallResult
        {
            IsError = true,
            Content = new List<ContentBlock>
            {
                new()
                {
                    Type = "text",
                    Text = JsonSerializer.Serialize(new { error = message }, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    })
                }
            }
        };
    }
}
