# winC2D -- Windows Storage Migration Assistant

[English (README)](../README.md) · [简体中文](README.zh-CN.md) · [繁體中文](README.zh-Hant.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [Русский](README.ru.md) · [Português](README.pt-BR.md)

---

## About

winC2D is a Windows disk migration assistant that helps you move installed software and common user folders from your C drive to another disk. It uses standard Windows **symbolic links** and file-copy operations -- no modification to application binaries or registry entries.

## Important Notes

After migrating software, winC2D creates **symbolic links (symlinks)** at the original paths so they remain accessible. Most migrated software will continue to work from its new location without modifying the application or its shortcuts. **Standard migration does not touch the registry.**

> The "Change default install location" option in Settings **does modify the system registry** to redirect where new apps are installed. If issues arise, restore the default value in Settings or roll back via a system restore point / backup.

## Features

- **Scan installed software** -- display C drive software with size and status; multi-select batch migration
- **User folder migration** -- scan and migrate common user folders (Documents, Pictures, Downloads, etc.)
- **Graphical path picker** -- target drive drop-down auto-populated
- **Symbolic links** -- created automatically after migration to preserve original paths
- **Rollback support** -- full migration log with one-click rollback
- **7 languages** -- in-app language switching
- **Dark / Light theme** -- follows system, manually switchable
- **Auto-elevation** -- requests administrator privileges on launch
- **Agent-ready** -- ships with a machine-readable CLI for AI agents and scripts; see [README.ai.md](README.ai.md)

## Tech Stack

- C# · .NET 8.0 · WPF
- [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- CommunityToolkit.Mvvm · Microsoft.Extensions.DependencyInjection

## Download

1. Download `winC2D-Setup.exe` from [Releases](https://github.com/Aknirex/winC2D/releases)
2. Run the installer -- defaults to `D:\Program Files\winC2D` (not C drive)
3. The installer bundles the GUI, CLI, gsudo elevation utility, and installs the AI agent skill to `%USERPROFILE%\.agents\skills\winc2d\`
4. Administrator privileges are required for migration (the app auto-elevates)
5. Uninstall via Control Panel -> Programs and Features
6. Requires Windows 10 / 11

## How It Works

1. **Scan** -- browse to a folder and click "Scan Sizes" to measure directory sizes
2. **Select** -- check the folders you want to migrate
3. **Migrate** -- winC2D copies the folder to the target drive, then creates a symlink at the original path
4. **Rollback** -- from the Logs page, select a completed task and click Rollback

## Agent CLI Mode

winC2D provides a machine-readable CLI for AI agents and scripts. See the full reference at [README.ai.md](README.ai.md).

Creating symbolic links requires **Administrator** rights or **Windows Developer Mode**. For elevated agent runs, use [gsudo](https://github.com/gerardog/gsudo).
