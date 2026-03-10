# winC2D — Windows Storage Migration Assistant

[![Build](https://img.shields.io/github/actions/workflow/status/SKR7lex/winC2D/dotnet.yml?branch=main&label=build)](https://github.com/SKR7lex/winC2D/actions)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/github/license/SKR7lex/winC2D)](LICENSE)
[![Release](https://img.shields.io/github/v/release/SKR7lex/winC2D)](https://github.com/SKR7lex/winC2D/releases)
[![Downloads](https://img.shields.io/github/downloads/SKR7lex/winC2D/total?label=downloads)](https://github.com/SKR7lex/winC2D/releases)
[![GitHub stars](https://img.shields.io/github/stars/SKR7lex/winC2D?style=social)](https://github.com/SKR7lex/winC2D/stargazers)

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

## Tech Stack

- C# · .NET 8.0 · WPF
- [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- CommunityToolkit.Mvvm · Microsoft.Extensions.DependencyInjection

## Download & Run

1. Download the latest release from [Releases](https://github.com/SKR7lex/winC2D/releases)
2. Run as **Administrator** (the app will prompt for elevation automatically)
3. Requires Windows 10 / 11

## Contributing

Pull requests are welcome! For major changes, please open an issue first.

## License

[MIT](LICENSE)
