---
name: winc2d
description: "Windows disk migration tool that moves installed applications and folders from C drive to other drives using symbolic links. Use when user asks to free up C drive space, move apps to another drive, migrate Program Files folders, or complains C drive is full. Keywords: C盘满了, C drive full, 释放C盘空间, free up space, migrate app, move software, 迁移软件, 移动C盘, Program Files migration, disk space cleanup, 磁盘空间不足, symlink migration, 符号链接迁移, WeMeet, 腾讯会议, Disk D, D盘."
---

# winC2D — Windows Storage Migration Assistant

Moves installed applications and folders from one drive to another on Windows. Copies files, then replaces the original path with a symbolic link. Applications continue to work.

## How to use

Two entry points in the release package:
- `winC2D.App.exe` — GUI
- `winC2D.Cli.exe` — CLI for automation

### Agent CLI workflow

```
1. privilege-status          — check admin + developer mode
2. disk-info                 — list available drives
3. preflight --source "C:\Path\To\App" --target "D:\TargetDrive"   — validate
4. migrate --source "C:\Path\To\App" --target "D:\TargetDrive" --dry-run  — dry run
5. migrate --source "C:\Path\To\App" --target "D:\TargetDrive" --yes     — real migration
6. status --task-id "<id>"  — poll until Completed
```

### Elevation

The CLI requires administrator rights for `C:\Program Files` operations. Use the bundled `run-elevated.ps1`:

```
pwsh -File run-elevated.ps1 migrate --source "C:\Program Files\Tencent\WeMeet" --target "D:\MigratedApps" --yes
```

If `run-elevated.ps1` is not available, install gsudo (`winget install gerardog.gsudo`) and run:

```
gsudo winC2D.Cli.exe migrate --source "C:\Program Files\Tencent\WeMeet" --target "D:\MigratedApps" --yes
```

### Key behaviors

- Source folder name is automatically appended to target path
- `--dry-run` validates without making changes (use before every real migration)
- `--yes` is required for real migrations
- `status` returns `taskSucceeded: true/false` — only `Completed` state means success
- All output is JSON, one object per command to stdout
- If you get `INSUFFICIENT_PRIVILEGES`, use `run-elevated.ps1` or gsudo
