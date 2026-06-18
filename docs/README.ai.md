# winC2D Agent CLI Reference

> Versioning: release versions are driven by Git tags. To prepare a release, run:
> ```powershell
> git tag -a vX.Y.Z -m "Release vX.Y.Z" && git push origin vX.Y.Z
> ```
> The build system (Nerdbank.GitVersioning) injects the tag version into all assemblies automatically. No manual version bump needed.

This document describes the machine-readable CLI interface for AI agents and automation scripts. For the user-facing GUI, see the main [README](../README.md).

## Overview

winC2D ships two entry points:

| Entry Point | Audience | Interface |
|---|---|---|
| `winC2D.App.exe` | End users | WPF GUI (Fluent Design) |
| `winC2D.Cli.exe` | AI agents, scripts | JSON over stdout |

The CLI writes exactly one JSON object per command to stdout. All data is structured and machine-parseable.

### When to Invoke winC2D

The user is likely asking for disk migration when they say:

- "move this app from C to D drive" / "move C drive apps to another drive"
- "C drive is full" / "free up C drive space"
- "migrate software" / "migrate folder to another disk"


## Commands

| Command | Description | Requires Elevation |
|---|---|---|
| `privilege-status` | Check current privilege level and available operations | No |
| `disk-info` | List fixed drives with free/total space | No |
| `scan` | Scan installed software with size and migration status | No |
| `preflight` | Validate a migration before starting | No |
| `migrate` | Move a folder to another drive and create a symlink | Yes |
| `status` | Poll migration progress by taskId | No |
| `pause` | Pause a running migration task | No |
| `resume` | Resume a paused migration task | No |
| `cancel` | Cancel a running migration task | No |
| `rollback` | Restore a migrated folder to its original path | Yes |
| `list` | List persisted migration tasks | No |
| `cleanup` | Remove completed/failed tasks from the store | No |

## Standard Agent Workflow

```powershell
# 1. Check environment
.\winC2D.Cli.exe privilege-status

# 2. List available target drives
.\winC2D.Cli.exe disk-info

# 3. Scan installed software
.\winC2D.Cli.exe scan

# 4. Preflight validation (no changes made)
.\winC2D.Cli.exe preflight --source "C:\Program Files\App" --target "D:\Program Files"

# 5. Dry-run (validates without migrating)
.\winC2D.Cli.exe migrate --source "C:\Program Files\App" --target "D:\Program Files" --dry-run

# 6. Real migration (requires --yes)
.\winC2D.Cli.exe migrate --source "C:\Program Files\App" --target "D:\Program Files" --yes

# 7. Poll until terminal state
.\winC2D.Cli.exe status --task-id "<taskId>"

# 8. Rollback if needed
.\winC2D.Cli.exe rollback --task-id "<taskId>" --yes
```

### Migration Flow Details

- `migrate` spawns a background worker and returns immediately with a `taskId`.
- Use `status --task-id "<id>"` to poll progress.
- Terminal states: `Completed`, `Failed`, `RolledBack`, `PartialRollback`, `Cancelled`.
- `status` output fields:
  - `success`: whether the status *query* succeeded (not the task itself)
  - `taskSucceeded`: whether the migration task completed successfully
  - `taskFailed`: whether the migration task failed
  - `isTerminal`: whether the task has reached a final state
  - `state`: current task state string
- Add `--wait` to `migrate` to block until completion.

### Key Behaviors

- Source folder name is automatically appended to the target path. Migrating `C:\Program Files\App` to `D:\Program Files` results in `D:\Program Files\App`.
- `--dry-run` validates preconditions without making any changes.
- `--yes` is required for real migrations and rollbacks (safety gate).
- All output is one JSON object per command, written to stdout.

## Privilege Requirements

Creating symbolic links requires **Administrator** rights or **Windows Developer Mode**.

| Privilege Level | Scan | Migrate |
|---|---|---|
| Administrator | Yes | Yes |
| Developer Mode | Yes | Yes |
| Restricted | Yes | No |

Restricted users receive a structured error with remediation guidance.

Enable Developer Mode: **Settings -> System -> Developer Options -> Developer Mode** (recommended: permanent, no UAC prompts).

## Elevation for Agent Runs

For automated elevation, use [gsudo](https://github.com/gerardog/gsudo):

```powershell
gsudo .\winC2D.Cli.exe migrate --source "C:\Program Files\App" --target "D:\Program Files" --yes
```

Alternatively, run the CLI from an already-elevated terminal, or use `run-elevated.ps1` (bundled with the CLI).
