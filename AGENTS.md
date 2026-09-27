# CLIHub Agent Instructions

## Project Status

**Greenfield WPF project** - currently in scaffold phase with planning artifacts in `openspec/changes/application-scaffold/`.

## Critical Structure Requirements

1. **Source code MUST live in `src/` subdirectory** - do not create projects in the repo root
2. **Solution file required** - create `CLIHub.sln` at repo root with projects under `src/`
3. **Project structure:**
   ```
   CLIHub/              (repo root)
   ├── CLIHub.sln       (solution file)
   ├── src/
   │   └── CLIHub/      (WPF application project)
   │       ├── CLIHub.csproj
   │       ├── Models/
   │       ├── Services/
   │       ├── Windows/
   │       └── Resources/
   ├── docs/
   └── openspec/
   ```

## Technology Stack (per design.md)

- **.NET 8 LTS** targeting `net8.0-windows`
- **WPF** (not Windows Forms, not WinUI 3)
- **H.NotifyIcon.Wpf** for system tray (not Hardcodet, not WinForms NotifyIcon)
- **Serilog** with file sink (not NLog)
- **Microsoft.Extensions.DependencyInjection** (not Autofac)
- **Windows Terminal integration** via `wt.exe` spawning

## Key Conventions

- **All code, comments, and UI text in English** - even though Russian is allowed for docs
- **Nullable reference types enabled** - use `?` for nullable types
- **JSON config** at `%APPDATA%\CLIHub\config.json` with camelCase serialization
- **Plugin system** uses JSON descriptors in `%APPDATA%\CLIHub\plugins\<plugin-id>/plugin.json`
- **Log location:** `%APPDATA%\CLIHub\logs\clihub-YYYYMMDD.log` with 7-day retention

## Build & Run

```powershell
# From repo root
dotnet build CLIHub.sln
dotnet run --project src/CLIHub/CLIHub.csproj
```

## OpenSpec Workflow

Active change: `application-scaffold`

```bash
# Check planning status
openspec status --change application-scaffold

# View implementation instructions
openspec instructions apply --change application-scaffold --json

# Continue implementation
# Use /opsx-apply skill or manually implement from tasks.md
```

**Before implementing:** Discuss structure decisions first. User wants to review architecture before code generation.

## Known Constraints

- **Windows 10/11 only** - no cross-platform
- **Single instance enforcement** via named mutex `Global\CLIHub`
- **No in-app terminal** - delegate to Windows Terminal
- **Plugins are descriptors only** - no DLL loading, no in-process execution
- **No Windows Forms** - banned, use WPF equivalents

## File System Layout

- **User data:** `%APPDATA%\CLIHub\` with subdirs: `logs/`, `plugins/`, `cache/`
- **Config persistence:** `config.json` with atomic write (temp file → rename)
- **Plugin discovery:** subdirectories under `plugins/`, each with `plugin.json` + optional `logo.png`

## What to Avoid

- Do NOT use Windows Forms components (`System.Windows.Forms.*`)
- Do NOT create WPF projects without a `.sln` file
- Do NOT put source code in repo root instead of `src/`
- Do NOT implement before discussing structure with user
