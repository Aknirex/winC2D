---
name: winc2d
description: "Windows disk migration tool that moves installed applications and folders from C drive to other drives using symbolic links. Use when user asks to free up C drive space, move apps to another drive, migrate Program Files folders, or complains C drive is full. Keywords: C盘满了, C drive full, 释放C盘空间, free up space, migrate app, move software, 迁移软件, 移动C盘, Program Files migration, disk space cleanup, 磁盘空间不足, symlink migration, 符号链接迁移, WeMeet, 腾讯会议, Disk D, D盘."
---

# winC2D — Windows Storage Migration Assistant

Moves installed applications and folders from one drive to another on Windows. Copies files, then replaces the original path with a symbolic link. Applications continue to work.

## CLI Location

After installation, the CLI is at:
```
D:\Program Files\winC2D\winC2D.Cli.exe
```

All JSON output goes to stdout. Run `winC2D.Cli.exe help` for the full agent workflow.

## Required Workflow

```
1. winC2D.Cli.exe privilege-status     — check if admin
2. winC2D.Cli.exe disk-info            — list drives
3. winC2D.Cli.exe preflight --source "C:\Program Files\App" --target "D:\MigratedApps"
4. winC2D.Cli.exe migrate --source "..." --target "..." --dry-run   — validate first
5. winC2D.Cli.exe migrate --source "..." --target "..." --yes         — dry-run ok → real
6. winC2D.Cli.exe status --task-id "<id>"   — poll until Completed
```

## Elevation

The CLI requires admin for `C:\Program Files`. Use gsudo (bundled) or install via winget:

```powershell
winget install gerardog.gsudo
gsudo "D:\Program Files\winC2D\winC2D.Cli.exe" migrate --source "C:\Program Files\WeMeet" --target "D:\MigratedApps" --yes
```

## Key Rules

- NEVER skip `--dry-run`
- Source folder name is auto-appended to target path
- `--yes` is always required for real migrations
- `status.taskSucceeded` (not `status.success`) indicates migration result
- If CLI returns `INSUFFICIENT_PRIVILEGES`, use gsudo
- If `blockers` is non-empty, do NOT proceed with `--yes`
