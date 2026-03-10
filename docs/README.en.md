# winC2D — Windows Storage Migration Assistant

[English (README)](../README.md) · [简体中文](README.zh-CN.md) · [繁體中文](README.zh-Hant.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [Русский](README.ru.md) · [Português](README.pt-BR.md)

---

## About

winC2D is a Windows disk migration assistant that helps you move installed software and common user folders from your C drive to another disk. It also lets you change the system's default installation path and user folder locations — freeing up C drive space without reinstalling anything.

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

## Tech Stack

- C# · .NET 8.0 · WPF
- [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- CommunityToolkit.Mvvm · Microsoft.Extensions.DependencyInjection

## Download & Run

1. Download the latest release from [Releases](https://github.com/SKR7lex/winC2D/releases)
2. Run as **Administrator** (the app will prompt for elevation automatically)
3. Requires Windows 10 / 11
