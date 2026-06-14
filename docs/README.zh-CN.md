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
- 🤖 **Agent CLI 模式** — 通过 `winC2D.Cli.exe` 将迁移能力开放给 AI Agent 与脚本

## 技术栈

- C# · .NET 8.0 · WPF
- [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- CommunityToolkit.Mvvm · Microsoft.Extensions.DependencyInjection
- 与 GUI 共用 Core / Infrastructure 服务的机器可读 CLI

## 下载与使用

1. 前往 [Releases](https://github.com/Aknirex/winC2D/releases) 下载最新版本

   | 版本 | 大小 | 适用场景 |
   | --- | --- | --- |
   | **独立版**（`-standalone.exe`） | ~70–80 MB | ⭐ 推荐 — 内含 .NET 8 运行时，下载即用 |
   | **依赖版**（`-framework-dependent.exe`） | ~10–15 MB | 系统已安装 .NET 8 运行时的用户 |

2. 以**管理员身份**运行（程序会自动提示提权）
3. 支持 Windows 10 / 11

## Agent CLI 模式

winC2D 提供两个明确入口：普通用户使用 `winC2D.App.exe` 打开 GUI；AI Agent 或脚本使用 `winC2D.Cli.exe ...` 获取单个 JSON 对象输出。

### 常用命令

| 命令 | 说明 | 是否需要提权 |
|---|---|---|
| `privilege-status` | 查询当前权限级别和可用操作 | 否 |
| `disk-info` | 列出所有固定磁盘的可用/总空间 | 否 |
| `scan` | 扫描已安装软件，含大小和迁移状态 | 否 |
| `preflight` | 迁移前预检 | 否 |
| `migrate` | 迁移软件到其他磁盘，支持 dry-run 预检 | 是 |
| `status` | 轮询迁移任务进度 | 否 |
| `pause` / `resume` / `cancel` | 控制正在运行的 worker 任务 | 否 |
| `rollback` | 回滚迁移，还原到原路径 | 是 |
| `list` / `cleanup` | 查看或清理持久化迁移任务 | 否 |

### Agent 标准工作流

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

`migrate` 默认启动隐藏 worker 并立即返回 `taskId`，之后使用 `status` 轮询。需要命令等待时可加 `--wait`。

### 权限要求

创建符号链接需要**管理员**权限或**Windows 开发者模式**。

| 权限级别 | 扫描 | 迁移 |
|---|---|---|
| 管理员（Administrator） | ✅ | ✅ |
| 开发者模式（Developer Mode） | ✅ | ✅ |
| 普通用户（Restricted） | ✅ | ❌（返回结构化错误，含解决方案指引） |

开启开发者模式：**设置 → 系统 → 开发者选项 → 开发者模式**（推荐：永久生效，无 UAC 弹窗）

Agent/脚本需要提权迁移时，可用管理员终端、Windows 开发者模式，或 [gsudo](https://github.com/gerardog/gsudo)：

```powershell
gsudo .\winC2D.Cli.exe migrate --source "C:\Program Files\App" --target "D:\Program Files" --yes
```
