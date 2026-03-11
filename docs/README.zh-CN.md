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
- 🤖 **MCP Server 模式** — 通过 [Model Context Protocol](https://modelcontextprotocol.io/) 将迁移能力开放给 AI Agent

## 技术栈

- C# · .NET 8.0 · WPF
- [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- CommunityToolkit.Mvvm · Microsoft.Extensions.DependencyInjection
- [ModelContextProtocol SDK](https://github.com/modelcontextprotocol/csharp-sdk)（MCP Server 模式）

## 下载与使用

1. 前往 [Releases](https://github.com/Aknirex/winC2D/releases) 下载最新版本

   | 版本 | 大小 | 适用场景 |
   | --- | --- | --- |
   | **独立版**（`-standalone.exe`） | ~70–80 MB | ⭐ 推荐 — 内含 .NET 8 运行时，下载即用 |
   | **依赖版**（`-framework-dependent.exe`） | ~10–15 MB | 系统已安装 .NET 8 运行时的用户 |

2. 以**管理员身份**运行（程序会自动提示提权）
3. 支持 Windows 10 / 11

## MCP Server 模式（AI Agent 使用）

winC2D v4.0 内置 **MCP (Model Context Protocol) Server**，让 Claude、GitHub Copilot 等 AI Agent 可以直接调用迁移功能，自动完成 C 盘软件搬迁。

### 可用工具

| 工具名 | 说明 | 是否需要提权 |
|---|---|---|
| `get_privilege_status` | 查询当前权限级别和可用操作 | 否 |
| `get_disk_info` | 列出所有固定磁盘的可用/总空间 | 否 |
| `scan_software` | 扫描已安装软件，含大小和迁移状态 | 否 |
| `migrate_software` | 迁移软件到其他磁盘，支持 dryRun 预检 | 是 |
| `get_task_status` | 轮询迁移任务进度 | 否 |
| `rollback_migration` | 回滚迁移，还原到原路径 | 是 |
| `list_migrations` | 列出本次会话的所有迁移任务 | 否 |

### Agent 标准工作流

```
get_privilege_status → get_disk_info → scan_software
  → migrate_software(dryRun=true)    # 预检，确认无阻碍项
  → migrate_software(dryRun=false)   # 提交迁移，获取 taskId
  → get_task_status（每 3-5 秒轮询，直到 Completed / Failed）
  → rollback_migration（可选，撤销迁移）
```

### 权限要求

创建符号链接需要**管理员**权限或**Windows 开发者模式**。

| 权限级别 | 扫描 | 迁移 |
|---|---|---|
| 管理员（Administrator） | ✅ | ✅ |
| 开发者模式（Developer Mode） | ✅ | ✅ |
| 普通用户（Restricted） | ✅ | ❌（返回结构化错误，含解决方案指引） |

开启开发者模式：**设置 → 系统 → 开发者选项 → 开发者模式**（推荐：永久生效，无 UAC 弹窗）

### Claude Desktop 配置

```json
{
  "mcpServers": {
    "winC2D": {
      "command": "C:\\path\\to\\winC2D.exe",
      "args": ["--mcp"]
    }
  }
}
```

> **注意**：进程必须具备创建符号链接的权限，可选方案：
> - 以管理员身份启动 Claude Desktop，**或**
> - 开启 Windows 开发者模式（推荐），**或**
> - 安装 [gsudo](https://github.com/gerardog/gsudo)：将 `command` 改为 `"gsudo"`，`args` 首项改为 winC2D.exe 路径

### VS Code / GitHub Copilot 配置

在 VS Code `settings.json` 中添加：

```json
{
  "mcp": {
    "servers": {
      "winC2D": {
        "type": "stdio",
        "command": "C:\\path\\to\\winC2D.exe",
        "args": ["--mcp"]
      }
    }
  }
}
```
