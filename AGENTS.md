# winC2D — Windows Storage Migration Assistant (for AI Agents)

## What this tool does

winC2D moves installed applications and folders from one drive to another on Windows.
It copies files, then replaces the original path with a symbolic link pointing to the new location.
Applications continue to work as if nothing changed.

## When to use it

User asks to:
- "移动 C 盘软件到 D 盘" / "move this app to another drive"
- "C 盘满了" / "C drive is full"
- "释放 C 盘空间" / "free up space on C drive"
- "把 Program Files 里的东西移到别的盘"

## How to use it

Two entry points:
- `winC2D.App.exe` — GUI (for direct user interaction)
- `winC2D.Cli.exe` — CLI (for automation / AI agents)

### CLI workflow (run `winC2D.Cli.exe help` for full instructions)

```
1. privilege-status          — check if admin + developer mode
2. disk-info                 — list available drives
3. preflight --source "..." --target "..."      — validate before migrating
4. migrate --source "..." --target "..." --dry-run  — dry run
5. migrate --source "..." --target "..." --yes       — real migration
6. status --task-id "<id>"  — poll until Completed
```

### Elevation

The CLI requires administrator rights for `C:\Program Files` operations.
Use `run-elevated.ps1` (bundled with the CLI) for automatic elevation.
Alternatively: `winget install gerardog.gsudo` then `gsudo winC2D.Cli.exe ...`

### Key behaviors

- Source folder name is automatically appended to target path
- `--dry-run` validates without making changes
- `--yes` is required for real migrations
- `status` returns `taskSucceeded: true/false` — only `Completed` state = success
- All output is JSON, one object per command to stdout

## Location

The CLI and GUI executables are in the same directory as this file when extracted from the release zip.
