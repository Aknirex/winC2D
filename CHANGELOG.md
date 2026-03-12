# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **File Metadata Preservation** — File copy operations now preserve original creation time, last write time, and file attributes during migration
- **About Page Localization** — Full internationalization support for About/Info page across 7 languages (English, 中文简体, 中文繁體, 日本語, 한국어, Русский, Português Brasileiro)
- **Language-Aware Documentation Links** — About page automatically routes documentation links to language-specific wiki pages based on application language selection

### Changed
- About page UI simplified by removing Features and Technology Stack expandable cards, improving clarity and reducing clutter
- Pause/Resume behavior redesigned to use gate-based implementation instead of cancel-based semantics, preventing unintended rollbacks
- Enhanced localization service to synchronize both `CurrentCulture` and `CurrentUICulture` for consistent number and date formatting across the application

### Fixed
- **Critical:** RollbackPoint.BackupPath no longer lost during serialization—now properly recorded and restored on application restart
- Thread-safety issue: Replaced non-thread-safe `Dictionary` with `ConcurrentDictionary` in migration state tracking (fixes potential race conditions)
- SizeCellTooltipConverter dead code check (changed from `SizeBytes == -1` to proper empty folder detection)
- Migration engine: Fixed 8+ indentation errors in SaveTasks() method that could affect file persistence
- Rollback manager: Fixed indentation errors in SaveState() method
- AboutView localization service injection: Service now properly injected via constructor rather than deferred setup
- Build process: Verified all changes compile with 0 warnings and 0 errors

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

[4.1.0]: https://github.com/Aknirex/winC2D/releases/tag/v4.1.0
[4.0.0]: https://github.com/Aknirex/winC2D/releases/tag/v4.0.0
[3.3.0]: https://github.com/Aknirex/winC2D/releases/tag/v3.3.0
[3.2.2]: https://github.com/Aknirex/winC2D/releases/tag/v3.2.2
[3.2.0]: https://github.com/Aknirex/winC2D/releases/tag/v3.2.0
[2.1.0]: https://github.com/Aknirex/winC2D/releases/tag/v2.1.0
[2.0.2]: https://github.com/Aknirex/winC2D/releases/tag/v2.0.2
[2.0.1]: https://github.com/Aknirex/winC2D/releases/tag/v2.0.1
[1.1.1]: https://github.com/Aknirex/winC2D/releases/tag/v1.1.1
