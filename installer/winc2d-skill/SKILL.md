---
name: winc2d
description: "Windows disk migration tool that moves installed applications and folders from C drive to other drives using symbolic links. Use when user asks to free up C drive space, move apps to another drive, migrate Program Files folders, or complains C drive is full. Keywords: C盘满了, C drive full, 释放C盘空间, free up space, migrate app, move software, 迁移软件, 移动C盘, Program Files migration, disk space cleanup, 磁盘空间不足, symlink migration, 符号链接迁移, WeMeet, 腾讯会议, Disk D, D盘."
---

# winC2D — AI Agent Skill

Moves installed applications from C drive to another drive using symbolic links. Provides three entry points: **MCP Server** (preferred), **CLI JSON**, and **WPF GUI**.

---

## Entry Points

| Entry Point | Transport | Audience |
|---|---|---|
| `winC2D.Mcp.exe` | JSON-RPC 2.0 over stdio | AI agents (MCP-compatible) |
| `winC2D.Cli.exe` | JSON over stdout | Scripts, legacy agents |
| `winC2D.App.exe` | WPF GUI (Fluent Design) | End users |

**Prefer MCP** when the agent host supports Model Context Protocol. Fall back to CLI otherwise.

---

## MCP Connection (Preferred)

### Server Configuration

```json
{
  "mcpServers": {
    "winc2d": {
      "command": "D:\\Program Files\\winC2D\\winC2D.Mcp.exe",
      "args": []
    }
  }
}
```

### Lifecycle

```
Client → Server:  initialize     { protocolVersion, capabilities, clientInfo }
Server → Client:  { protocolVersion, capabilities: { tools: {} }, serverInfo }
Client → Server:  notifications/initialized
Client → Server:  tools/list
Server → Client:  { tools: [...] }
Client → Server:  tools/call     { name, arguments }
Server → Client:  { content: [{ type: "text", text: "..." }] }
```

### Available Tools (MCP)

| Tool | Parameters | Elevation |
|---|---|---|
| `privilege_status` | *(none)* | No |
| `disk_info` | *(none)* | No |
| `scan_software` | `directories?` (string, comma-separated) | No |
| `preflight` | `source`*, `target`*, `verify?` (bool) | No |
| `migrate` | `source`*, `target`*, `dry_run?`, `yes?`, `verify?`, `wait?`, `timeout_seconds?` (int) | Yes |
| `migration_status` | `task_id`* | No |
| `pause_migration` | `task_id`* | No |
| `resume_migration` | `task_id`* | No |
| `cancel_migration` | `task_id`*, `rollback?` (bool) | No |
| `rollback` | `task_id`*, `yes`* | Yes |
| `list_tasks` | *(none)* | No |
| `cleanup_tasks` | `older_than_days?` (int, default=30) | No |

\* = required

### Tool Input Schemas (JSON Schema)

#### `preflight`

```json
{
  "type": "object",
  "properties": {
    "source": { "type": "string", "description": "Absolute path of source directory (e.g., C:\\Program Files\\App)" },
    "target": { "type": "string", "description": "Absolute path of target root directory (e.g., D:\\MigratedApps)" },
    "verify": { "type": "boolean", "description": "Enable post-copy file verification. Default: true" }
  },
  "required": ["source", "target"]
}
```

#### `migrate`

```json
{
  "type": "object",
  "properties": {
    "source": { "type": "string", "description": "Absolute path of source directory" },
    "target": { "type": "string", "description": "Absolute path of target root directory" },
    "dry_run": { "type": "boolean", "description": "Validate without making changes. Default: false" },
    "yes": { "type": "boolean", "description": "Confirm real migration (safety gate). Required for non-dry-run" },
    "verify": { "type": "boolean", "description": "Enable post-copy file verification. Default: true" },
    "wait": { "type": "boolean", "description": "Block until migration completes. Default: false" },
    "timeout_seconds": { "type": "integer", "description": "Max wait time in seconds when wait=true. Default: 1800" }
  },
  "required": ["source", "target"]
}
```

#### `migration_status`

```json
{
  "type": "object",
  "properties": {
    "task_id": { "type": "string", "description": "Task ID returned by migrate command" }
  },
  "required": ["task_id"]
}
```

#### `rollback`

```json
{
  "type": "object",
  "properties": {
    "task_id": { "type": "string", "description": "Task ID of completed migration to rollback" },
    "yes": { "type": "boolean", "description": "Confirm rollback. Required" }
  },
  "required": ["task_id", "yes"]
}
```

---

## CLI Fallback (Legacy)

### CLI Location

`D:\Program Files\winC2D\winC2D.Cli.exe`

Run `winC2D.Cli.exe help` for full usage. All output is single-line JSON to stdout.

### Standard Agent Workflow

```powershell
# 1. Check environment
.\winC2D.Cli.exe privilege-status

# 2. List available target drives
.\winC2D.Cli.exe disk-info

# 3. Scan installed software
.\winC2D.Cli.exe scan

# 4. Preflight validation (no changes made)
.\winC2D.Cli.exe preflight --source "C:\Program Files\App" --target "D:\Program Files"

# 5. Dry-run (validates without migrating)
.\winC2D.Cli.exe migrate --source "C:\Program Files\App" --target "D:\Program Files" --dry-run

# 6. Real migration (requires --yes)
.\winC2D.Cli.exe migrate --source "C:\Program Files\App" --target "D:\Program Files" --yes

# 7. Poll until terminal state
.\winC2D.Cli.exe status --task-id "<taskId>"

# 8. Rollback if needed
.\winC2D.Cli.exe rollback --task-id "<taskId>" --yes
```

### Elevation via gsudo

CLI requires admin for `C:\Program Files`. Use gsudo with `--%` (PowerShell stop-parsing token):

```powershell
gsudo --% "D:\Program Files\winC2D\winC2D.Cli.exe" migrate --source "C:\Program Files\Tencent\WeMeet" --target "D:\MigratedApps" --yes
```

The `--%` token is **CRITICAL**. Without it, paths containing spaces fail. 5 out of 6 elevation attempts fail without `--%`.

If gsudo is not installed:
```powershell
winget install gerardog.gsudo
```

---

## Migration State Machine

```
Pending → Preparing → Copying → CreatingSymlink → CleaningUp → Completed
                ↓          ↓              ↓
              Failed    Paused        RollingBack
                          ↓                ↓
                       Copying         RolledBack
                                       PartialRollback
```

### Terminal States

| State | Meaning | Next Action |
|---|---|---|
| `Completed` | Migration succeeded, symlink active | None or rollback |
| `Failed` | Pre-check failed (no changes made) | Fix blockers, retry |
| `RolledBack` | Full rollback succeeded | Retry or abandon |
| `PartialRollback` | Rollback incomplete — manual recovery needed | Check error, manual fix |
| `Cancelled` | Cancelled by user (optionally rolled back) | Retry or abandon |

---

## Error Codes

| Code | Meaning | Resolution |
|---|---|---|
| `INSUFFICIENT_PRIVILEGES` | Not admin, no Developer Mode | Run as admin or enable Developer Mode |
| `CONFIRMATION_REQUIRED` | `--yes` not provided | Add `--yes` to confirm |
| `UNKNOWN_COMMAND` | Invalid CLI command | Check `help` for valid commands |
| `ARGUMENT_ERROR` | Missing/invalid argument | Check parameter spelling and format |
| `PRECHECK_FAILED` | Preflight validation blocked | Review `blockers` array, fix issues, retry |
| `SOURCE_NOT_FOUND` | Source directory does not exist | Verify path is correct |
| `TARGET_INSUFFICIENT_SPACE` | Not enough free space on target drive | Choose a different target drive |
| `FILE_LOCKED` | Files locked by running processes | Close the application (including tray icons), retry |
| `SYMLINK_FAILED` | Cannot create symbolic link | Check permissions, free space |
| `ROLLBACK_FAILED` | Rollback incomplete | Check `rollbackResult` for details |
| `UNHANDLED_EXCEPTION` | Unexpected internal error | Report with full error details |

---

## Key Rules

- **NEVER skip `--dry-run` / `preflight`** before real migration
- Source folder name is auto-appended to target path. Migrating `C:\Program Files\App` to `D:\Program Files` results in `D:\Program Files\App`
- `--yes` is required for real migrations and rollbacks (safety gate)
- `status.taskSucceeded` (not `status.success`) = migration result
- ALWAYS use `gsudo --%` when elevating CLI on PowerShell
- For MCP: call `preflight` first, then `migrate` with `dry_run: true`, then `migrate` with `yes: true`
- Poll `migration_status` until `isTerminal: true` before reporting final result to user
- Terminal states ARE: `Completed`, `Failed`, `RolledBack`, `PartialRollback`, `Cancelled`
- DO NOT assume migration failed if `taskFailed` is false — check `isTerminal`

---

## Privilege Requirements

Creating symbolic links requires **Administrator** rights or **Windows Developer Mode**.

| Privilege Level | Scan | Migrate |
|---|---|---|
| Administrator | Yes | Yes |
| Developer Mode | Yes | Yes |
| Restricted | Yes | No |

Enable Developer Mode: **Settings → System → Developer Options → Developer Mode** (permanent, no UAC prompts).

---

## MCP Server Implementation Notes

The MCP server (`winC2D.Mcp.exe`) implements the Model Context Protocol (2024-11-05) with:

- **Transport**: JSON-RPC 2.0 over stdio (stdin/stdout)
- **Logging**: stderr only (stdout is reserved for protocol messages)
- **Capabilities**: `tools` (listChanged: false)
- **Initialization**: Requires `initialize` handshake before `tools/list` or `tools/call`

### Debugging MCP

To test the MCP server manually:

```powershell
# Send initialize
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}' | D:\Program Files\winC2D\winC2D.Mcp.exe

# Send tools/list
echo '{"jsonrpc":"2.0","id":2,"method":"tools/list"}' | D:\Program Files\winC2D\winC2D.Mcp.exe
```
