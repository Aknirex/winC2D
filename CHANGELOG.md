# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
