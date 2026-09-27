## Why

CLIHub needs a foundational application structure to support its core mission: providing quick access to AI agent CLI tools from the Windows system tray. Currently, the repository is empty aside from vision documentation. This change establishes the WPF application scaffold with dependency injection, logging infrastructure, and the core service architecture needed to build the system tray launcher.

## What Changes

- Create WPF application project with .NET 8 target
- Configure dependency injection container (Microsoft.Extensions.DependencyInjection)
- Set up Serilog logging to `%APPDATA%\CLIHub\logs\` with 7-day rotation
- Establish core service layer interfaces and implementations:
  - ConfigService for application settings persistence
  - ProjectService for project directory management
  - PluginManager for AI agent plugin discovery and loading
  - ProcessLauncher for spawning CLI tool processes
  - HotkeyManager for global keyboard shortcuts
  - UpdateService for version checking
- Configure system tray icon infrastructure (H.NotifyIcon.Wpf)
- Implement single-instance application enforcement (mutex-based)
- Create basic shell windows (LaunchWindow for project/agent selection, SettingsWindow for configuration)
- Establish AppData directory structure (`%APPDATA%\CLIHub\{config.json, plugins\, logs\, cache\}`)

## Capabilities

### New Capabilities

- `app-lifecycle`: Application startup, shutdown, single-instance enforcement, and service initialization
- `logging`: Structured logging to file with configurable levels and rotation
- `configuration`: Application settings persistence and retrieval from JSON config file
- `plugin-system`: Plugin discovery, loading, validation, and management for AI agent CLI tools
- `project-management`: Project directory detection, tracking, and context awareness
- `process-launch`: Spawning and managing external CLI tool processes with proper working directory context
- `tray-integration`: System tray icon, context menu, and window show/hide coordination
- `hotkey-support`: Global keyboard shortcut registration and handling
- `update-checking`: Version comparison and update availability notification

### Modified Capabilities

<!-- None - this is the initial scaffold -->

## Impact

**New code:**
- `CLIHub/` - WPF application project
  - `App.xaml` / `App.xaml.cs` - Application entry point with DI configuration
  - `Services/` - Core service implementations
  - `Models/` - Domain models (Plugin, Project, Config, etc.)
  - `Windows/` - WPF windows (LaunchWindow, SettingsWindow)
  - `Resources/` - Icons, styles, templates

**New dependencies:**
- Microsoft.Extensions.DependencyInjection (DI container)
- Microsoft.Extensions.Logging (logging abstractions)
- Serilog (structured logging implementation)
- Serilog.Sinks.File (file logging sink)
- H.NotifyIcon.Wpf (system tray icon for WPF)
- System.Text.Json (JSON serialization for config)

**File system:**
- Creates `%APPDATA%\CLIHub\` directory structure
- Writes logs to `%APPDATA%\CLIHub\logs\clihub-YYYYMMDD.log`
- Persists config to `%APPDATA%\CLIHub\config.json`
- Establishes plugin directory at `%APPDATA%\CLIHub\plugins\`

**No breaking changes** - this is the initial scaffold.
