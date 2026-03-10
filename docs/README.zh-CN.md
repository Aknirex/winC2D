# winC2D — Windows 磁盘迁移助手

[English (README)](../README.md) · [繁體中文](README.zh-Hant.md) · [English](README.en.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [Русский](README.ru.md) · [Português](README.pt-BR.md)

---

## 项目简介

winC2D 是一款 Windows 磁盘迁移助手，帮助用户将 C 盘中已安装的软件和常用用户文件夹便捷地迁移至其他磁盘，同时支持修改系统默认安装位置和用户文件夹路径，有效释放 C 盘空间。

## ⚠️ 注意事项

迁移软件后，本工具会在原路径创建**符号链接（Symlink）**，使原路径继续可用。大多数已迁移的软件无需修改本身或快捷方式即可在新位置正常运行。**标准迁移操作不修改注册表。**

> 设置中提供的"修改默认安装路径"功能**会修改系统注册表**，以更改新应用的默认安装位置。若遇到问题，请在设置中还原默认值，或通过系统还原点 / 备份进行回滚。

## 主要功能

- 📦 扫描 C 盘已安装软件，显示占用大小与状态，支持多选批量迁移
- 📁 扫描并迁移常用用户文件夹（文档、图片、下载等）
- 🖱️ 图形化选择目标路径，磁盘下拉列表自动填充
- 🔗 迁移后自动创建符号链接，保持原路径可用
- ↩️ 支持回滚，附带完整迁移日志
- 🌏 界面内切换语言，7 种语言支持
- 🌙 深色 / 浅色主题自动跟随系统，可手动切换
- 🛡️ 启动时自动请求管理员权限

## 技术栈

- C# · .NET 8.0 · WPF
- [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- CommunityToolkit.Mvvm · Microsoft.Extensions.DependencyInjection

## 下载与使用

1. 前往 [Releases](https://github.com/SKR7lex/winC2D/releases) 下载最新版本
2. 以**管理员身份**运行（程序会自动提示提权）
3. 支持 Windows 10 / 11
