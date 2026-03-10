# winC2D — Windows Storage Migration Assistant

[![Build](https://img.shields.io/github/actions/workflow/status/SKR7lex/winC2D/dotnet.yml?branch=main&label=build)](https://github.com/SKR7lex/winC2D/actions)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/github/license/SKR7lex/winC2D)](LICENSE)
[![Release](https://img.shields.io/github/v/release/SKR7lex/winC2D)](https://github.com/SKR7lex/winC2D/releases)
[![Downloads](https://img.shields.io/github/downloads/SKR7lex/winC2D/total?label=downloads)](https://github.com/SKR7lex/winC2D/releases)
[![GitHub stars](https://img.shields.io/github/stars/SKR7lex/winC2D?style=social)](https://github.com/SKR7lex/winC2D/stargazers)

---

> 选择语言 / Choose language / 言語を選択 / 언어 선택 / Выбор языка / Selecionar idioma

[简体中文](#简体中文) · [繁體中文](#繁體中文) · [English](#english) · [日本語](#日本語) · [한국어](#한국어) · [Русский](#русский) · [Português (Brasil)](#português-brasil)

---

## 简体中文

### 项目简介

winC2D 是一款 Windows 磁盘迁移助手，帮助用户将 C 盘中已安装的软件和常用用户文件夹便捷地迁移至其他磁盘，同时支持修改系统默认安装位置和用户文件夹路径，有效释放 C 盘空间。

### ⚠️ 注意事项

迁移软件后，本工具会在原路径创建**符号链接（Symlink）**，使原路径继续可用。大多数已迁移的软件无需修改本身或快捷方式即可在新位置正常运行。**标准迁移操作不修改注册表。**

> 设置中提供的"修改默认安装路径"功能**会修改系统注册表**，以更改新应用的默认安装位置。若遇到问题，请在设置中还原默认值，或通过系统还原点/备份进行回滚。

### 主要功能

- 📦 扫描 C 盘已安装软件，显示占用大小与状态，支持多选批量迁移
- 📁 扫描并迁移常用用户文件夹（文档、图片、下载等）
- 🖱️ 图形化选择目标路径，磁盘下拉列表自动填充
- 🔗 迁移后自动创建符号链接，保持原路径可用
- ↩️ 支持回滚，附带完整迁移日志
- 🌏 界面内切换语言，7 种语言支持
- 🌙 深色 / 浅色主题自动跟随系统，可手动切换
- 🛡️ 启动时自动请求管理员权限

### 技术栈

- C# · .NET 8.0 · WPF
- [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- CommunityToolkit.Mvvm · Microsoft.Extensions.DependencyInjection

### 下载与使用

1. 前往 [Releases](https://github.com/SKR7lex/winC2D/releases) 下载最新版本
2. 以**管理员身份**运行（程序会自动提示提权）
3. 支持 Windows 10 / 11

---

## 繁體中文

### 專案簡介

winC2D 是一款 Windows 磁碟遷移助手，協助使用者將 C 碟已安裝的軟體及常用使用者資料夾遷移至其他磁碟，並支援修改系統預設安裝位置與使用者資料夾路徑，有效釋放 C 碟空間。

### ⚠️ 注意事項

遷移軟體後，本工具會在原路徑建立**符號連結（Symlink）**，使原路徑繼續可用。大多數已遷移的軟體無需修改本身或捷徑即可在新位置正常執行。**標準遷移操作不修改登錄檔。**

> 設定中提供的「修改預設安裝路徑」功能**會修改系統登錄檔**，以變更新應用程式的預設安裝位置。若遇到問題，請在設定中還原預設值，或透過系統還原點 / 備份進行回復。

### 主要功能

- 📦 掃描 C 碟已安裝軟體，顯示占用大小與狀態，支援多選批次遷移
- 📁 掃描並遷移常用使用者資料夾（文件、圖片、下載等）
- 🖱️ 圖形化選擇目標路徑，磁碟下拉清單自動填充
- 🔗 遷移後自動建立符號連結，保持原路徑可用
- ↩️ 支援回復，附帶完整遷移日誌
- 🌏 介面內切換語言，支援 7 種語言
- 🌙 深色 / 淺色主題自動跟隨系統，可手動切換
- 🛡️ 啟動時自動要求系統管理員權限

### 技術堆疊

- C# · .NET 8.0 · WPF
- [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- CommunityToolkit.Mvvm · Microsoft.Extensions.DependencyInjection

### 下載與使用

1. 前往 [Releases](https://github.com/SKR7lex/winC2D/releases) 下載最新版本
2. 以**系統管理員身分**執行（程式會自動提示提權）
3. 支援 Windows 10 / 11

---

## English

### About

winC2D is a Windows disk migration assistant that helps you move installed software and common user folders from your C drive to another disk. It also lets you change the system's default installation path and user folder locations — freeing up C drive space without reinstalling anything.

### ⚠️ Important Notes

After migrating software, winC2D creates **symbolic links (symlinks)** at the original paths so they remain accessible. Most migrated software will continue to work from its new location without modifying the application or its shortcuts. **Standard migration does not touch the registry.**

> The "Change default install location" option in Settings **does modify the system registry** to redirect where new apps are installed. If issues arise, restore the default value in Settings or roll back via a system restore point / backup.

### Features

- 📦 Scan installed software on C drive with size and status columns; select multiple entries for batch migration
- 📁 Scan and migrate common user folders (Documents, Pictures, Downloads, etc.)
- 🖱️ Graphical target path picker with auto-populated drive drop-down
- 🔗 Symlinks created automatically after migration to preserve original paths
- ↩️ Rollback support with full migration log
- 🌏 In-app language switching — 7 languages supported
- 🌙 Dark / Light theme follows system, switchable manually
- 🛡️ Automatically requests administrator elevation on launch

### Tech Stack

- C# · .NET 8.0 · WPF
- [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- CommunityToolkit.Mvvm · Microsoft.Extensions.DependencyInjection

### Download & Run

1. Download the latest release from [Releases](https://github.com/SKR7lex/winC2D/releases)
2. Run as **Administrator** (the app will prompt for elevation automatically)
3. Requires Windows 10 / 11

---

## 日本語

### 概要

winC2D は Windows 向けのディスク移行ツールです。C ドライブにインストールされたソフトウェアやユーザーフォルダーを別のディスクへ簡単に移行し、システムのデフォルトインストール先やユーザーフォルダーのパスを変更することで、C ドライブの空き容量を確保できます。

### ⚠️ 注意事項

ソフトウェアの移行後、元のパスに**シンボリックリンク（Symlink）**を作成し、引き続きアクセスできるようにします。移行後もほとんどのソフトウェアは、アプリ本体やショートカットを変更せずに新しい場所から正常に動作します。**標準の移行操作はレジストリを変更しません。**

> 設定の「デフォルトのインストール先を変更」機能は、新しいアプリのインストール先を変更するため**システムレジストリを変更します**。問題が発生した場合は、設定でデフォルト値に戻すか、システムの復元ポイント / バックアップでロールバックしてください。

### 主な機能

- 📦 C ドライブのインストール済みソフトウェアをサイズ・状態付きで一覧表示、複数選択で一括移行
- 📁 ユーザーフォルダー（ドキュメント・画像・ダウンロードなど）のスキャンと移行
- 🖱️ GUI でターゲットパスを選択、ドライブ一覧は自動入力
- 🔗 移行後に自動でシンボリックリンクを作成し元パスを維持
- ↩️ ロールバック対応、完全な移行ログ付き
- 🌏 アプリ内で言語切り替え可能（7 言語対応）
- 🌙 ダーク / ライトテーマをシステムに合わせて自動切替、手動変更も可
- 🛡️ 起動時に自動で管理者権限を要求

### 技術スタック

- C# · .NET 8.0 · WPF
- [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- CommunityToolkit.Mvvm · Microsoft.Extensions.DependencyInjection

### ダウンロードと実行

1. [Releases](https://github.com/SKR7lex/winC2D/releases) から最新版をダウンロード
2. **管理者として実行**（アプリが自動的に昇格を要求します）
3. Windows 10 / 11 対応

---

## 한국어

### 소개

winC2D 는 Windows 디스크 마이그레이션 도구입니다. C 드라이브에 설치된 소프트웨어와 사용자 폴더를 다른 디스크로 손쉽게 이동하고, 시스템 기본 설치 경로 및 사용자 폴더 경로를 변경하여 C 드라이브 공간을 확보할 수 있습니다.

### ⚠️ 주의 사항

소프트웨어 마이그레이션 후 원래 경로에 **심볼릭 링크(Symlink)** 를 생성하여 기존 경로가 계속 작동하도록 합니다. 대부분의 마이그레이션된 소프트웨어는 앱 본체나 바로 가기를 수정하지 않고도 새 위치에서 정상 실행됩니다. **표준 마이그레이션은 레지스트리를 변경하지 않습니다.**

> 설정의 "기본 설치 위치 변경" 기능은 새 앱의 기본 설치 위치를 변경하기 위해 **시스템 레지스트리를 수정합니다**. 문제가 발생하면 설정에서 기본값으로 복원하거나 시스템 복원 지점 / 백업을 통해 롤백하십시오.

### 주요 기능

- 📦 C 드라이브의 설치된 소프트웨어를 크기·상태와 함께 표시, 다중 선택 일괄 마이그레이션
- 📁 사용자 폴더(문서·사진·다운로드 등) 스캔 및 마이그레이션
- 🖱️ GUI로 대상 경로 선택, 드라이브 목록 자동 채움
- 🔗 마이그레이션 후 자동으로 심볼릭 링크 생성, 원래 경로 유지
- ↩️ 롤백 지원, 완전한 마이그레이션 로그 제공
- 🌏 앱 내 언어 전환 (7개 언어 지원)
- 🌙 다크 / 라이트 테마 시스템 자동 추적, 수동 전환 가능
- 🛡️ 실행 시 자동으로 관리자 권한 요청

### 기술 스택

- C# · .NET 8.0 · WPF
- [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- CommunityToolkit.Mvvm · Microsoft.Extensions.DependencyInjection

### 다운로드 및 실행

1. [Releases](https://github.com/SKR7lex/winC2D/releases) 에서 최신 버전 다운로드
2. **관리자 권한으로 실행** (앱이 자동으로 권한 상승 요청)
3. Windows 10 / 11 지원

---

## Русский

### О программе

winC2D — инструмент миграции дисков для Windows. Позволяет перенести установленные программы и пользовательские папки с диска C на другой диск, а также изменить путь установки новых приложений и расположение пользовательских папок, освобождая место на системном диске.

### ⚠️ Важные замечания

После миграции программ winC2D создаёт **символические ссылки (symlinks)** по исходным путям, чтобы они продолжали работать. Большинство перенесённых программ запускаются с нового места без изменения самих приложений или ярлыков. **Стандартная миграция не затрагивает реестр.**

> Функция «Изменить путь установки по умолчанию» в настройках **изменяет системный реестр**, чтобы перенаправить установку новых приложений. При возникновении проблем восстановите значение по умолчанию в настройках или откатите изменения через точку восстановления системы / резервную копию.

### Основные возможности

- 📦 Сканирование установленных программ на диске C с отображением размера и статуса; поддержка множественного выбора и пакетной миграции
- 📁 Сканирование и перенос пользовательских папок (Документы, Изображения, Загрузки и др.)
- 🖱️ Графический выбор целевого пути с автозаполнением списка дисков
- 🔗 Автоматическое создание символических ссылок после миграции для сохранения исходных путей
- ↩️ Поддержка отката с полным журналом миграции
- 🌏 Переключение языка прямо в интерфейсе — 7 языков
- 🌙 Тёмная / светлая тема следует за системой, переключается вручную
- 🛡️ Автоматический запрос прав администратора при запуске

### Стек технологий

- C# · .NET 8.0 · WPF
- [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- CommunityToolkit.Mvvm · Microsoft.Extensions.DependencyInjection

### Загрузка и запуск

1. Скачайте последнюю версию со страницы [Releases](https://github.com/SKR7lex/winC2D/releases)
2. Запускайте **от имени администратора** (программа запросит повышение прав автоматически)
3. Требуется Windows 10 / 11

---

## Português (Brasil)

### Sobre

winC2D é uma ferramenta de migração de disco para Windows. Permite mover softwares instalados e pastas de usuário do drive C para outro disco, além de alterar o caminho de instalação padrão do sistema e os locais das pastas do usuário, liberando espaço no disco do sistema.

### ⚠️ Avisos Importantes

Após migrar softwares, o winC2D cria **links simbólicos (symlinks)** nos caminhos originais para mantê-los funcionando. A maioria dos programas migrados continua funcionando no novo local sem alterar os aplicativos ou atalhos. **A migração padrão não modifica o registro do Windows.**

> A função "Alterar caminho de instalação padrão" nas configurações **modifica o registro do sistema** para redirecionar onde novos aplicativos são instalados. Em caso de problemas, restaure o valor padrão nas configurações ou reverta via ponto de restauração do sistema / backup.

### Funcionalidades

- 📦 Escaneia softwares instalados no drive C com tamanho e status; suporta seleção múltipla para migração em lote
- 📁 Escaneia e migra pastas de usuário comuns (Documentos, Imagens, Downloads, etc.)
- 🖱️ Seleção gráfica do caminho de destino com lista de drives preenchida automaticamente
- 🔗 Criação automática de links simbólicos após migração para preservar os caminhos originais
- ↩️ Suporte a rollback com log completo de migração
- 🌏 Troca de idioma dentro do app — 7 idiomas suportados
- 🌙 Tema escuro / claro segue o sistema, com opção manual
- 🛡️ Solicita privilégios de administrador automaticamente na inicialização

### Tecnologias

- C# · .NET 8.0 · WPF
- [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- CommunityToolkit.Mvvm · Microsoft.Extensions.DependencyInjection

### Download e Execução

1. Baixe a versão mais recente em [Releases](https://github.com/SKR7lex/winC2D/releases)
2. Execute como **Administrador** (o app solicitará elevação automaticamente)
3. Requer Windows 10 / 11

---

## Contributing / 贡献 / 貢獻 / コントリビュート / 기여 / Вклад / Contribuição

Pull requests are welcome! For major changes, please open an issue first.  
欢迎 Pull Request！重大改动请先提 Issue 讨论。

## License

[MIT](LICENSE)
