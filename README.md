# winC2D — Windows Storage Migration Assistant

[![Build](https://img.shields.io/github/actions/workflow/status/Aknirex/winC2D/dotnet.yml?branch=main&label=build)](https://github.com/Aknirex/winC2D/actions)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/github/license/Aknirex/winC2D)](LICENSE)
[![Release](https://img.shields.io/github/v/release/Aknirex/winC2D)](https://github.com/Aknirex/winC2D/releases)
[![Downloads](https://img.shields.io/github/downloads/Aknirex/winC2D/total?label=downloads)](https://github.com/Aknirex/winC2D/releases)
[![GitHub stars](https://img.shields.io/github/stars/Aknirex/winC2D?style=social)](https://github.com/Aknirex/winC2D/stargazers)

[简体中文](docs/README.zh-CN.md) · [繁體中文](docs/README.zh-Hant.md) · [日本語](docs/README.ja.md) · [한국어](docs/README.ko.md) · [Русский](docs/README.ru.md) · [Português (Brasil)](docs/README.pt-BR.md)

---

## About

winC2D is a Windows disk migration assistant that helps you move installed applications and folders from your C drive to another disk. It uses standard Windows **symbolic links** and file-copy operations — no modification to application binaries or registry entries.

## Features

- 🗂️ **Unified file browser** — navigate your drives in a familiar Explorer-like interface with breadcrumb navigation and quick-access sidebar
- 📏 **Size scanning** — calculate directory sizes with cache support for fast subsequent scans
- 🔗 **Symbolic link migration** — move folders to another drive, then create a symlink at the original path so everything keeps working
- ↩️ **Rollback support** — full migration history with one-click rollback for completed tasks
- 🌏 **7 languages** — English, 简体中文, 繁體中文, 日本語, 한국어, Русский, Português (Brasil)
- 🌙 **Dark / Light theme** — follows system preference, manually switchable
- 🛡️ **Auto-elevation** — requests administrator privileges on launch (required for symlink creation and Program Files access)
- 🤖 **Agent CLI** — `winC2D.Cli.exe` exposes all migration capabilities as machine-readable JSON for AI agents and scripts

## Tech Stack

- C# · .NET 8.0 · WPF
- [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- CommunityToolkit.Mvvm · Microsoft.Extensions.DependencyInjection

## Download & Run

1. Download the latest installer from [Releases](https://github.com/Aknirex/winC2D/releases) (`winC2D-Setup-vX.Y.Z.exe`)
2. Run the installer — defaults to `D:\Program Files\winC2D` (not C drive)
3. The installer bundles everything: GUI, CLI, gsudo elevation, and AI agent skill
4. Administrator privileges are required for migration (the app auto-elevates)
5. Uninstall via Control Panel → Programs and Features
6. Requires Windows 10 / 11

## How It Works

1. **Scan** — browse to a folder and click "Scan Sizes" to measure directory sizes
2. **Select** — check the folders you want to migrate
3. **Migrate** — winC2D copies the folder to the target drive, then replaces the original path with a symbolic link pointing to the new location
4. **Rollback** — from the Logs page, select a completed task and click Rollback to restore the original state

## Agent CLI Mode

winC2D ships two entry points: `winC2D.App.exe` (GUI) and `winC2D.Cli.exe` (CLI). The CLI writes one JSON object per command to stdout.

### Commands

| Command | Description | Requires Elevation |
|---|---|---|
| `privilege-status` | Check current privilege level | No |
| `disk-info` | List fixed drives with free/total space | No |
| `scan` | Scan installed software with size and status | No |
| `preflight` | Validate a migration before starting | No |
| `migrate` | Move software to another drive with symlink | Yes |
| `status` | Poll migration progress by taskId | No |
| `pause` / `resume` / `cancel` | Control a running worker task | No |
| `rollback` | Restore software to original path | Yes |
| `list` / `cleanup` | Inspect or clean persisted tasks | No |

### Agent Workflow

```powershell
.\winC2D.Cli.exe privilege-status
.\winC2D.Cli.exe disk-info
.\winC2D.Cli.exe scan
.\winC2D.Cli.exe preflight --source "C:\Program Files\App" --target "D:\Program Files"
.\winC2D.Cli.exe migrate --source "C:\Program Files\App" --target "D:\Program Files" --dry-run
.\winC2D.Cli.exe migrate --source "C:\Program Files\App" --target "D:\Program Files" --yes
.\winC2D.Cli.exe status --task-id "<taskId>"
.\winC2D.Cli.exe rollback --task-id "<taskId>" --yes
```

### Privilege Requirements

Creating symbolic links requires **Administrator** rights or **Windows Developer Mode**.

| Privilege Level | Scan | Migrate |
|---|---|---|
| Administrator | ✅ | ✅ |
| Developer Mode | ✅ | ✅ |
| Restricted | ✅ | ❌ |

Enable Developer Mode: **Settings → System → Developer Options → Developer Mode**

For elevated agent runs, use [gsudo](https://github.com/gerardog/gsudo):

```powershell
gsudo .\winC2D.Cli.exe migrate --source "C:\Program Files\App" --target "D:\Program Files" --yes
```

## Contributing

Pull requests are welcome! For major changes, please open an issue first.

## License

[MIT](LICENSE)
