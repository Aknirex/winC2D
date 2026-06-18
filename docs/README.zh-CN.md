# winC2D -- Windows 磁盘迁移助手

[English (README)](../README.md) · [繁體中文](README.zh-Hant.md) · [English](README.en.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [Русский](README.ru.md) · [Português](README.pt-BR.md)

---

## 项目简介

winC2D 是一款 Windows 磁盘迁移助手，帮助用户将 C 盘中已安装的软件和常用用户文件夹迁移至其他磁盘。通过标准 Windows **符号链接（Symlink）** 和文件复制操作实现，不修改应用程序二进制文件或注册表。

## 注意事项

迁移软件后，本工具会在原路径创建**符号链接（Symlink）**，使原路径继续可用。大多数已迁移的软件无需修改本身或快捷方式即可在新位置正常运行。**标准迁移操作不修改注册表。**

> 设置中提供的"修改默认安装路径"功能**会修改系统注册表**，以更改新应用的默认安装位置。若遇到问题，请在设置中还原默认值，或通过系统还原点 / 备份进行回滚。

## 主要功能

- **扫描已安装软件** -- 显示 C 盘软件占用大小与状态，支持多选批量迁移
- **用户文件夹迁移** -- 扫描并迁移常用用户文件夹（文档、图片、下载等）
- **图形化路径选择** -- 目标路径磁盘下拉列表自动填充
- **符号链接** -- 迁移后自动创建符号链接，保持原路径可用
- **回滚支持** -- 附带完整迁移日志，支持一键回滚
- **7 种语言** -- 界面内切换语言
- **深色 / 浅色主题** -- 自动跟随系统，可手动切换
- **自动提权** -- 启动时自动请求管理员权限
- **Agent 就绪** -- 附带机器可读 CLI，供 AI Agent 和脚本使用；详见 [README.ai.md](README.ai.md)

## 技术栈

- C# · .NET 8.0 · WPF
- [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- CommunityToolkit.Mvvm · Microsoft.Extensions.DependencyInjection

## 下载与使用

1. 前往 [Releases](https://github.com/Aknirex/winC2D/releases) 下载 `winC2D-Setup.exe`
2. 运行安装器 -- 默认安装至 `D:\Program Files\winC2D`（不占用 C 盘）
3. 安装器包含 GUI、CLI、gsudo 提权工具，并将 AI Agent skill 安装至 `%USERPROFILE%\.agents\skills\winc2d\`
4. 迁移需要**管理员权限**（程序自动提权）
5. 通过控制面板 -> 程序和功能卸载
6. 支持 Windows 10 / 11

## 工作流程

1. **扫描** -- 浏览文件夹，点击"扫描大小"计算目录占用
2. **选择** -- 勾选需要迁移的文件夹
3. **迁移** -- winC2D 将文件夹复制到目标磁盘，然后在原路径创建符号链接指向新位置
4. **回滚** -- 在日志页面选择已完成任务，点击回滚恢复原始状态

## Agent CLI 模式

winC2D 提供机器可读 CLI 供 AI Agent 与脚本使用。完整参考见 [README.ai.md](README.ai.md)。

创建符号链接需要**管理员**权限或 **Windows 开发者模式**。Agent 提权迁移可使用 [gsudo](https://github.com/gerardog/gsudo)。
