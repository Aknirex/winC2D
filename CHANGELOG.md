# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- Release artifacts are now zip bundles containing both `winC2D.App.exe` and `winC2D.Cli.exe`.

### Fixed
- Improved Agent CLI task observability by reporting stale task reasons and writing worker start/final result entries to task logs.
- Fixed task status JSON so cancelled or rolled-back migrations are not reported as successful task outcomes.
- Clarified Agent CLI status semantics: `success` now represents command/query success, while task outcome is exposed through `taskSucceeded`, `taskFailed`, `isTerminal`, and `state`.

---

## [4.2.0] - 2026-05-31

### Added
- Added Agent CLI mode via `winC2D.exe --cli` with JSON output for privilege checks, disk info, scanning, migration, task status, rollback, and task listing.
- Added asynchronous CLI migrations that return a `taskId` immediately and execute through a hidden worker process.
- Added CLI tests for argument validation, JSON output, task queries, and redirected process execution.

### Changed
- The same published executable now supports both the GUI and the agent-facing CLI mode.
- Release documentation now describes `--cli` usage for AI agents and scripts.

### Removed
- Removed MCP server mode, `--mcp`, `--mcp-poc`, and the ModelContextProtocol dependency.

---

## [4.1.6] - 2026-05-31

### Changed
- Removed the top-level Software Migration navigation item so the file browser is the primary migration entry point.
- AppData migrations now place `Local`, `Roaming`, and `LocalLow` items under separate target folders to avoid same-name collisions.

### Fixed
- Restored reliable migration behavior by copying into a temporary target directory, finalizing atomically, and preserving rollback data after successful migrations.
- Fixed rollback for completed migrations so the original path can be restored from the migrated target.
- Removed the same-drive migration hard block; unsafe nested or conflicting paths are still rejected by the migration engine.
- Fixed list checkbox interactions so a single click toggles selection immediately and fast clicks do not open folders.

---

## [4.1.5] - 2026-05-09

### Added
- **Resizable navigation pane** — Main window left navigation sidebar now supports drag-to-resize (180px–500px). Cursor changes to ↔ when hovering near the right edge of the pane.

### Changed
- **FileSystemBrowser sidebar splitter** — GridSplitter widened from 4px to 6px for easier grabbing; splitter column changed to `Auto` sizing for proper resize behavior.
- **Removed back button** — NavigationView's built-in back button hidden (`IsBackButtonVisible="Collapsed"`) as it served no useful purpose in the single-level navigation structure.
- **Version bumped** to 4.1.5

### Fixed
- Fixed ambiguous type references between WPF and WinForms namespaces (`Point`, `Cursors`, `Brushes`, `Panel`, `SolidColorBrush`, `Color`) in `MainWindow.xaml.cs`

---

## [4.1.1] - 2026-03-12

### Changed
- **Status Bar Redesigned** — Bottom status indicator now displays real-time operation status (scanning, migrating, errors) with visual feedback
  - Idle state: Accent-color filled circle
  - Busy state: Animated ProgressRing (spinning circle) with status message

### Fixed
- Status messages were only shown in scan/migration views, not visible in the main window status bar
- Scan progress text and migration progress were displayed redundantly in both view and bottom bar
- SoftwareMigrationViewModel now properly propagates status changes to MainViewModel for unified status display
- Removed duplicate scan/migration status displays from view (now consolidated in status bar for better UX)

---

## [4.1.0] - 2026-03-12

### Changed
- MCP Server mode performance optimizations
- Improved error handling in migration tasks

### Fixed
- Edge case handling in privilege detection
- Minor stability improvements

---

## [4.0.0] - 2026-03-12

### Added
- **🤖 MCP (Model Context Protocol) Server Mode** — Built-in server exposing all migration capabilities to AI agents (Claude, GitHub Copilot, etc.) via the Model Context Protocol
  - `get_privilege_status` - Check current privilege level and available operations
  - `get_disk_info` - List all fixed drives with free/total space
  - `scan_software` - Stream all installed software with size and migration status
  - `migrate_software` - Move software to another drive with symlink + optional dry-run
  - `get_task_status` - Poll migration progress by taskId
  - `rollback_migration` - Restore software to original path
  - `list_migrations` - List all migration tasks in the current session
- Support for autonomous software scanning and migration by AI agents with proper privilege handling
- Claude Desktop configuration examples and documentation
- Integrated ModelContextProtocol SDK for AI agent communication

---

## [3.3.0] - 2026-03-11

### Changed
- Infrastructure and tooling improvements

### Fixed
- Build process stability enhancements

---

## [3.2.2] - 2026-03-11

### Changed
- Release build improvements

### Fixed
- Build automation enhancements

---

## [3.2.0] - 2026-03-10

### Changed
- Updated build and release infrastructure
- Improved executable distribution format

### Fixed
- Build configuration improvements

---

## [2.1.0] - 2026-01-01

### Added
- Right-click menu options:
  - Copy name/path
  - Open in Explorer
  - Check directory

### Improved
- List initial load performance - no longer checks directory volume during initial loading to avoid program unresponsiveness

---

## [2.0.2] - 2025-11-26

### Fixed
- Fixed "parameter error" caused by improper handling of registry path symbols

---

## [2.0.1] - 2025-11-25

### Fixed
- Fixed interference of software display name on migration path

---

## [1.1.1] - 2025-10-28

### Initial Release
- Basic disk migration functionality
- Software scanning and migration
- Symbolic link creation
- Rollback support

---

[4.2.0]: https://github.com/Aknirex/winC2D/releases/tag/v4.2.0
[4.1.6]: https://github.com/Aknirex/winC2D/releases/tag/v4.1.6
[4.1.5]: https://github.com/Aknirex/winC2D/releases/tag/v4.1.5
[4.1.0]: https://github.com/Aknirex/winC2D/releases/tag/v4.1.0
[4.0.0]: https://github.com/Aknirex/winC2D/releases/tag/v4.0.0
[3.3.0]: https://github.com/Aknirex/winC2D/releases/tag/v3.3.0
[3.2.2]: https://github.com/Aknirex/winC2D/releases/tag/v3.2.2
[3.2.0]: https://github.com/Aknirex/winC2D/releases/tag/v3.2.0
[2.1.0]: https://github.com/Aknirex/winC2D/releases/tag/v2.1.0
[2.0.2]: https://github.com/Aknirex/winC2D/releases/tag/v2.0.2
[2.0.1]: https://github.com/Aknirex/winC2D/releases/tag/v2.0.1
[1.1.1]: https://github.com/Aknirex/winC2D/releases/tag/v1.1.1
