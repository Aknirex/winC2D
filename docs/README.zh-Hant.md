# winC2D -- Windows 磁碟遷移助手

[English (README)](../README.md) · [简体中文](README.zh-CN.md) · [English](README.en.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [Русский](README.ru.md) · [Português](README.pt-BR.md)

---

## 專案簡介

winC2D 是一款 Windows 磁碟遷移助手，協助使用者將 C 碟已安裝的軟體及常用使用者資料夾遷移至其他磁碟。透過標準 Windows **符號連結（Symlink）** 與檔案複製實現，不修改應用程式二進位檔或登錄檔。

## 注意事項

遷移軟體後，本工具會在原路徑建立**符號連結（Symlink）**，使原路徑繼續可用。大多數已遷移的軟體無需修改本身或捷徑即可在新位置正常執行。**標準遷移操作不修改登錄檔。**

> 設定中提供的「修改預設安裝路徑」功能**會修改系統登錄檔**，以變更新應用程式的預設安裝位置。若遇到問題，請在設定中還原預設值，或透過系統還原點 / 備份進行回復。

## 主要功能

- **掃描已安裝軟體** -- 顯示 C 碟軟體占用大小與狀態，支援多選批次遷移
- **使用者資料夾遷移** -- 掃描並遷移常用資料夾（文件、圖片、下載等）
- **圖形化路徑選擇** -- 磁碟下拉清單自動填充
- **符號連結** -- 遷移後自動建立，保持原路徑可用
- **支援回復** -- 附帶完整遷移日誌，一鍵回復
- **7 種語言** -- 介面內切換語言
- **深色 / 淺色主題** -- 自動跟隨系統，可手動切換
- **自動提權** -- 啟動時自動要求系統管理員權限
- **Agent 就緒** -- 附帶機器可讀 CLI，供 AI Agent 與腳本使用；詳見 [README.ai.md](README.ai.md)

## 技術堆疊

- C# · .NET 8.0 · WPF
- [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- CommunityToolkit.Mvvm · Microsoft.Extensions.DependencyInjection

## 下載與使用

1. 前往 [Releases](https://github.com/Aknirex/winC2D/releases) 下載 `winC2D-Setup.exe`
2. 執行安裝程式 -- 預設安裝至 `D:\Program Files\winC2D`（不占用 C 碟）
3. 安裝程式包含 GUI、CLI、gsudo 提權工具，並可將同一份 AI Agent skill 連結至 Codex、Claude Code、Antigravity、OpenCode、OpenClaw 等常用 Agent
4. 遷移需要**系統管理員權限**（程式自動提權）
5. 透過控制台 -> 程式和功能解除安裝
6. 支援 Windows 10 / 11

## 工作流程

1. **掃描** -- 瀏覽資料夾，點擊「掃描大小」計算目錄占用
2. **選擇** -- 勾選需要遷移的資料夾
3. **遷移** -- winC2D 將資料夾複製到目標磁碟，然後在原路徑建立符號連結指向新位置
4. **回復** -- 在日誌頁面選擇已完成任務，點擊回復還原原始狀態

## Agent CLI 模式

winC2D 提供機器可讀 CLI 供 AI Agent 與腳本使用。完整參考見 [README.ai.md](README.ai.md)。

建立符號連結需要**系統管理員**權限或 **Windows 開發者模式**。Agent 提權遷移可使用 [gsudo](https://github.com/gerardog/gsudo)。
