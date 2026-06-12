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

winC2D is a Windows disk migration assistant that helps you move installed software and common user folders from your C drive to another disk. It also lets you change the system default installation path and user folder locations — freeing up C drive space without reinstalling anything.

## ⚠️ Important Notes

After migrating software, winC2D creates **symbolic links (symlinks)** at the original paths so they remain accessible. Most migrated software will continue to work from its new location without modifying the application or its shortcuts. **Standard migration does not touch the registry.**

> The "Change default install location" option in Settings **does modify the system registry** to redirect where new apps are installed. If issues arise, restore the default value in Settings or roll back via a system restore point / backup.

## Features

- 📦 Scan installed software on C drive with size and status columns; select multiple entries for batch migration
- 📁 Scan and migrate common user folders (Documents, Pictures, Downloads, etc.)
- 🖱️ Graphical target path picker with auto-populated drive drop-down
- 🔗 Symlinks created automatically after migration to preserve original paths
- ↩️ Rollback support with full migration log
- 🌏 In-app language switching — 7 languages supported
- 🌙 Dark / Light theme follows system, switchable manually
- 🛡️ Automatically requests administrator elevation on launch
- 🤖 **Agent CLI mode** — expose migration capabilities to AI agents and scripts through `winC2D.exe --cli`

## Tech Stack

- C# · .NET 8.0 · WPF
- [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- CommunityToolkit.Mvvm · Microsoft.Extensions.DependencyInjection
- Machine-readable Agent CLI built on the same Core / Infrastructure services as the GUI

## Download & Run

1. Download the latest release from [Releases](https://github.com/Aknirex/winC2D/releases)

   | Version | Size | Use Case |
   | --- | --- | --- |
   | **Standalone** (`-standalone.exe`) | ~70–80 MB | ⭐ Recommended — includes .NET 8 runtime, works immediately |
   | **Framework-Dependent** (`-framework-dependent.exe`) | ~10–15 MB | Requires .NET 8 Runtime pre-installed |

2. Run as **Administrator** (the app will prompt for elevation automatically)
3. Requires Windows 10 / 11

## Agent CLI Mode

winC2D v4.2 ships a built-in CLI mode for AI agents, scripts, and terminal automation. The same `winC2D.exe` can be opened normally for the GUI or called with `--cli` for machine-readable JSON output.

### Commands

| Command | Description | Requires Elevation |
|---|---|---|
| `privilege-status` | Check current privilege level and available operations | No |
| `disk-info` | List all fixed drives with free/total space | No |
| `scan` | Scan installed software with size and migration status | No |
| `migrate` | Move software to another drive with symlink + optional dry-run | Yes |
| `status` | Poll migration progress by taskId | No |
| `rollback` | Restore software to original path | Yes |
| `list` | List persisted migration tasks | No |

### Agent Workflow

```powershell
.\winC2D.exe --cli privilege-status
.\winC2D.exe --cli disk-info
.\winC2D.exe --cli scan
.\winC2D.exe --cli migrate --source "C:\Program Files\App" --target-drive D: --dry-run
.\winC2D.exe --cli migrate --source "C:\Program Files\App" --target-drive D: --yes
.\winC2D.exe --cli status --task-id "<taskId>"
.\winC2D.exe --cli rollback --task-id "<taskId>" --yes
```

`migrate` starts a hidden worker process and returns a `taskId` immediately. Use `status` until the state is `Completed`, `Failed`, `RolledBack`, or `PartialRollback`. Add `--wait` if you want the command to poll before returning.

### Privilege Requirements

Creating symbolic links requires either **Administrator** rights or **Windows Developer Mode**.

| Privilege Level | Scan | Migrate |
|---|---|---|
| Administrator | ✅ | ✅ |
| Developer Mode | ✅ | ✅ |
| Restricted | ✅ | ❌ (returns structured error with fix instructions) |

Enable Developer Mode: **Settings → System → Developer Options → Developer Mode**

For elevated agent runs, start the shell / agent host as Administrator, enable Developer Mode, or use [gsudo](https://github.com/gerardog/gsudo):

```powershell
gsudo .\winC2D.exe --cli migrate --source "C:\Program Files\App" --target-drive D: --yes
```

## Contributing

Pull requests are welcome! For major changes, please open an issue first.

## License

[MIT](LICENSE)
