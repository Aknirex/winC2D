---
name: winc2d
description: "Windows disk migration tool that moves installed applications and folders from C drive to other drives using symbolic links. Use when user asks to free up C drive space, move apps to another drive, migrate Program Files folders, or complains C drive is full. Keywords: C盘满了, C drive full, 释放C盘空间, free up space, migrate app, move software, 迁移软件, 移动C盘, Program Files migration, disk space cleanup, 磁盘空间不足, symlink migration, 符号链接迁移, WeMeet, 腾讯会议, Disk D, D盘."
---

# winC2D

Moves installed applications from C drive to another drive using symbolic links.

## CLI Location

`D:\Program Files\winC2D\winC2D.Cli.exe`

Run `winC2D.Cli.exe help` for full agent workflow. All output is single-line JSON to stdout.

## Workflow

```
1. privilege-status
2. disk-info
3. preflight --source "SOURCE" --target "TARGET_ROOT"
4. migrate --source "SOURCE" --target "TARGET_ROOT" --dry-run
5. migrate --source "SOURCE" --target "TARGET_ROOT" --yes
6. status --task-id "ID"   (poll until Completed)
```

## Elevation via gsudo

CLI requires admin for `C:\Program Files`. Use gsudo with `--%` (PowerShell stop-parsing token):

```powershell
gsudo --% "D:\Program Files\winC2D\winC2D.Cli.exe" migrate --source "C:\Program Files\Tencent\WeMeet" --target "D:\MigratedApps" --yes
```

The `--%` token is CRITICAL. Without it, paths containing spaces (like `C:\Program Files\...`) are split into separate arguments and fail. 5 out of 6 elevation attempts fail without `--%`.

If gsudo is not installed:
```powershell
winget install gerardog.gsudo
```

## Key Rules

- NEVER skip `--dry-run`
- Source folder name is auto-appended to target path
- `--yes` is required for real migrations
- `status.taskSucceeded` (not `status.success`) = migration result
- ALWAYS use `gsudo --%` when elevating
