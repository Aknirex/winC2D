# winC2D — Windows 磁碟遷移助手

[English (README)](../README.md) · [简体中文](README.zh-CN.md) · [English](README.en.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [Русский](README.ru.md) · [Português](README.pt-BR.md)

---

## 專案簡介

winC2D 是一款 Windows 磁碟遷移助手，協助使用者將 C 碟已安裝的軟體及常用使用者資料夾遷移至其他磁碟，並支援修改系統預設安裝位置與使用者資料夾路徑，有效釋放 C 碟空間。

## ⚠️ 注意事項

遷移軟體後，本工具會在原路徑建立**符號連結（Symlink）**，使原路徑繼續可用。大多數已遷移的軟體無需修改本身或捷徑即可在新位置正常執行。**標準遷移操作不修改登錄檔。**

> 設定中提供的「修改預設安裝路徑」功能**會修改系統登錄檔**，以變更新應用程式的預設安裝位置。若遇到問題，請在設定中還原預設值，或透過系統還原點 / 備份進行回復。

## 主要功能

- 📦 掃描 C 碟已安裝軟體，顯示占用大小與狀態，支援多選批次遷移
- 📁 掃描並遷移常用使用者資料夾（文件、圖片、下載等）
- 🖱️ 圖形化選擇目標路徑，磁碟下拉清單自動填充
- 🔗 遷移後自動建立符號連結，保持原路徑可用
- ↩️ 支援回復，附帶完整遷移日誌
- 🌏 介面內切換語言，支援 7 種語言
- 🌙 深色 / 淺色主題自動跟隨系統，可手動切換
- 🛡️ 啟動時自動要求系統管理員權限

## 技術堆疊

- C# · .NET 8.0 · WPF
- [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- CommunityToolkit.Mvvm · Microsoft.Extensions.DependencyInjection

## 下載與使用

1. 前往 [Releases](https://github.com/Aknirex/winC2D/releases) 下載最新版本

   | 版本 | 大小 | 適用場景 |
   | --- | --- | --- |
   | **獨立版**（`-standalone.exe`） | ~70–80 MB | ⭐ 推薦 — 內含 .NET 8 執行時，下載即用 |
   | **依賴版**（`-framework-dependent.exe`） | ~10–15 MB | 系統已安裝 .NET 8 執行時的使用者 |

2. 以**系統管理員身分**執行（程式會自動提示提權）
3. 支援 Windows 10 / 11
