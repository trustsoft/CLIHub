# CLIHub Architecture

## Solution Structure

CLIHub uses a three-project architecture separating business logic from UI concerns:

```
CLIHub/
├── CLIHub.sln                 (solution file)
├── src/
│   ├── CLIHub.Core/           (business logic, services, models)
│   │   ├── CLIHub.Core.csproj
│   │   ├── Models/            (domain models: Plugin, Project, AppConfig)
│   │   ├── Services/          (core services: ConfigService)
│   │   └── Interfaces/        (service contracts: IConfigService)
│   │
│   ├── CLIHub/                (WPF application)
│   │   ├── CLIHub.csproj
│   │   ├── App.xaml           (application entry point)
│   │   ├── Windows/           (WPF windows: MainWindow)
│   │   ├── ViewModels/        (MVVM view models)
│   │   └── Resources/         (icons, images, styles)
│   │
│   └── CLIHub.Tests/          (unit and integration tests)
│       ├── CLIHub.Tests.csproj
│       ├── Services/          (service layer tests)
│       └── Models/            (model tests)
│
├── artifacts/                 (build output - git ignored)
│   ├── CLIHub/                (WPF application binaries)
│   ├── CLIHub.Core/           (core library binaries)
│   └── CLIHub.Tests/          (test binaries)
├── obj/                       (intermediate build - git ignored)
├── docs/                      (documentation)
├── openspec/                  (planning artifacts)
└── Directory.Build.props      (centralized build configuration)
```

## Project Responsibilities

### CLIHub.Core

**Purpose:** Platform-agnostic business logic and services

**Contains:**
- **Models:** Domain entities (Plugin, PluginCommand, Project, AppConfig, AppPreferences)
- **Services:** Core functionality (ConfigService, ProjectService, PluginManager, ProcessLauncher, HotkeyManager, UpdateService)
- **Interfaces:** Service contracts (IConfigService, IProjectService, IPluginManager, etc.)

**Dependencies:**
- .NET 8 base class libraries
- Serilog (logging)
- System.Text.Json (serialization)
- No WPF dependencies

**Target:** `netstandard2.1` or `net8.0` (library)

### CLIHub

**Purpose:** WPF user interface and Windows-specific integration

**Contains:**
- **App.xaml/App.xaml.cs:** Application startup, DI container setup, service registration
- **Windows/:** WPF windows (LaunchWindow, SettingsWindow)
- **ViewModels/:** MVVM view models for data binding
- **Resources/:** Icons, styles, brushes, image assets
- **System tray integration:** H.NotifyIcon.Wpf configuration

**Dependencies:**
- CLIHub.Core (project reference)
- WPF framework
- H.NotifyIcon.Wpf (system tray)
- Microsoft.Extensions.DependencyInjection (DI container)

**Target:** `net8.0-windows`

### CLIHub.Tests

**Purpose:** Unit and integration tests

**Contains:**
- Service layer tests (ConfigService, PluginManager, etc.)
- Model validation tests
- Integration tests for plugin loading and config persistence

**Dependencies:**
- CLIHub.Core (project reference)
- xUnit or NUnit
- Moq (mocking framework)

**Target:** `net8.0`

## Dependency Flow

```
CLIHub.Tests ──> CLIHub.Core <── CLIHub
                                   │
                                   └─> System Tray
                                   └─> Windows Terminal (wt.exe)
```

**Rules:**
- CLIHub.Core has NO dependency on CLIHub (UI)
- CLIHub.Tests can reference CLIHub.Core but not CLIHub (UI)
- All business logic lives in CLIHub.Core for testability
- CLIHub (UI) is a thin presentation layer over Core services

## Technology Stack

### Core Technologies
- **.NET 8 LTS** - long-term support through 2026
- **C# 12** with nullable reference types enabled
- **WPF** - Windows Presentation Foundation for UI

### Libraries
- **Microsoft.Extensions.DependencyInjection** - dependency injection container
- **Serilog** with file sink - structured logging
- **H.NotifyIcon.Wpf** - system tray icon support
- **System.Text.Json** - JSON serialization with camelCase policy

### External Integration
- **Windows Terminal** (`wt.exe`) - spawns CLI tool sessions
- **Named Mutex** (`Global\CLIHub`) - single instance enforcement

## Key Architectural Decisions

### Decision: Two-layer structure (Core + UI)

**Rationale:**
- Separates business logic from WPF, enabling unit tests without UI dependencies
- Core services can be tested independently of system tray and Windows-specific behavior
- Future-proofs for potential CLI interface or other UI (though not a current goal)
- Maintains simplicity while providing clear boundaries

**Alternatives considered:**
- Monolithic single project: Rejected due to testing difficulty
- Full layered architecture (Domain/Application/Infrastructure): Rejected as overkill for system tray app

### Decision: WPF over Windows Forms or WinUI 3

**Rationale:**
- WPF provides mature system tray support via H.NotifyIcon.Wpf
- Better data binding and MVVM patterns than Windows Forms
- WinUI 3 has immature system tray support and complex deployment
- .NET 8 WPF is stable and well-supported

### Decision: Plugin descriptors only (no DLL loading)

**Rationale:**
- Plugins are JSON descriptors pointing to external CLI executables
- No in-process plugin execution or dynamic assembly loading
- Simpler security model and isolation
- Easier configuration and troubleshooting

### Decision: Delegate to Windows Terminal

**Rationale:**
- No embedded terminal control needed
- Leverages mature Windows Terminal features (tabs, profiles, GPU acceleration)
- Simpler codebase and fewer dependencies
- Users already have Windows Terminal on Windows 11, easy install on Windows 10

## File System Layout

```
%APPDATA%\CLIHub\
├── config.json              (application configuration)
├── logs\
│   └── clihub-YYYYMMDD.log (daily log files, 7-day retention)
├── plugins\
│   ├── plugin-id-1\
│   │   ├── plugin.json     (plugin descriptor)
│   │   └── logo.png        (optional plugin icon)
│   └── plugin-id-2\
│       └── plugin.json
└── cache\                   (reserved for future use)
```

## Configuration Persistence

- **Format:** JSON with camelCase property names
- **Location:** `%APPDATA%\CLIHub\config.json`
- **Atomic writes:** Write to `.tmp` file, then rename to avoid corruption
- **Schema:** AppConfig with projects list, preferences, and currentProjectId
- **Versioning:** Config includes `schemaVersion` field for forward/backward compatibility
  - Current version: `1.0`
  - Version mismatch handling: attempt migration for older versions, fail safely for newer unknown versions
  - Migration strategy: preserve unknown fields when upgrading, log warnings for deprecated fields

## Logging Strategy

- **Framework:** Serilog with file sink
- **Location:** `%APPDATA%\CLIHub\logs\clihub-YYYYMMDD.log`
- **Rotation:** Daily files with 7-day retention
- **Levels:** Configurable via `AppPreferences.LogLevel` (default: Information)
- **Format:** Structured logging with timestamps, levels, and context

## Error Handling Strategy

### Configuration Errors

- **Missing config.json:** Generate default configuration with empty projects list on first run
- **Corrupted config.json:** Log error, backup corrupted file to `config.json.backup`, create fresh default config
- **Invalid JSON schema:** Attempt best-effort parsing, fill missing fields with defaults, log warnings
- **Version mismatch:** See Configuration Persistence versioning strategy above

### File System Errors

- **%APPDATA% write access denied:** Display error dialog to user, fall back to read-only mode (no settings persistence)
- **Log directory creation failure:** Continue operation without file logging, log to Windows Event Log instead
- **Plugin directory missing:** Create on demand when first plugin is added, log info message

### Plugin Errors

- **Invalid plugin.json:** Skip plugin during discovery, log warning with file path and validation error
- **Missing executable path:** Mark plugin as unavailable in UI, display warning icon with tooltip
- **Duplicate plugin IDs:** Load first occurrence, ignore duplicates, log warning

### Process Spawning Errors

- **Windows Terminal (wt.exe) not found:** Display error dialog with download link, offer fallback to cmd.exe
- **Working directory doesn't exist:** Show warning, offer to launch in user's home directory or cancel
- **Command execution failure:** Log full error details, display user-friendly error dialog with command that failed

### System Integration Errors

- **Mutex acquisition failure (single instance):** Activate existing instance window and exit gracefully
- **System tray registration failure:** Log error, display error dialog, terminate application (tray is core functionality)
- **Hotkey registration failure:** Log warning, continue without hotkeys, display notification to user

### General Error Handling Principles

- **User-facing errors:** Display clear, actionable error messages in dialogs (never raw exception text)
- **Developer errors:** Log full stack traces to file for debugging
- **Graceful degradation:** Continue operation when non-critical features fail
- **Fail-fast for critical errors:** Terminate cleanly when core functionality (tray, mutex, config) fails

## Testing Strategy

### Unit Tests (CLIHub.Tests)

- **ConfigService:** JSON serialization, atomic writes, default config generation
- **PluginManager:** Plugin discovery, JSON parsing, validation
- **ProjectService:** Project tracking, favorites, last-used sorting
- **ProcessLauncher:** Command building, working directory handling

### Integration Tests

- **Plugin loading:** End-to-end plugin discovery from disk
- **Config persistence:** Full load/save/reload cycle
- **Single instance:** Mutex enforcement across processes

### Manual Testing

- **System tray behavior:** Icon appearance, context menu, window activation
- **Windows Terminal spawning:** Correct working directory and command execution
- **Hotkey registration:** Global hotkey response without focus

## Conventions

### Code Style
- **Language:** All code, comments, and UI text in English
- **Nullable types:** Enabled project-wide, use `?` for nullable reference types
- **Naming:** PascalCase for classes/methods, camelCase for JSON properties

### Namespace Structure
- `CLIHub.Core.Models` - domain models
- `CLIHub.Core.Services` - service implementations
- `CLIHub.Core.Interfaces` - service contracts
- `CLIHub.Windows` - WPF windows
- `CLIHub.ViewModels` - MVVM view models

### Resource Organization
- Icons and images in `Resources/` within CLIHub project
- XAML styles in `Resources/Styles.xaml`
- String resources (if needed) in `Resources/Strings.resx`

## Build and Run

```powershell
# Restore dependencies
dotnet restore CLIHub.sln

# Build solution
dotnet build CLIHub.sln

# Run application
dotnet run --project src/CLIHub/CLIHub.csproj

# Run tests
dotnet test CLIHub.sln
```

## Known Constraints

- **Windows 10/11 only** - no cross-platform support
- **Single instance** - enforced via named mutex `Global\CLIHub`
- **No in-app terminal** - delegates to Windows Terminal
- **No Windows Forms** - banned, use WPF equivalents
- **No DLL plugins** - plugins are JSON descriptors only

## Security Considerations

### Single Instance Enforcement

- **Mechanism:** Named mutex `Global\CLIHub` created at application startup
- **Purpose:** Prevent multiple instances from conflicting during config file writes
- **Behavior:** 
  - First instance: Acquires mutex, runs normally
  - Subsequent instances: Detect existing mutex, activate first instance window via named pipe or window enumeration, exit gracefully
- **Security scope:** Global namespace (visible across user sessions) to prevent conflicts even with multiple users

### File System Security

- **Config directory permissions:** Use default Windows ACLs for `%APPDATA%\CLIHub\` (user-only read/write)
- **Atomic writes:** Prevent corruption from crashes or power loss via temp-file-then-rename pattern
- **Log file access:** Restricted to current user via %APPDATA% location
- **Plugin directory:** User-controlled location, no privilege elevation

### Process Spawning Security

- **Command injection prevention:** All command arguments passed to Windows Terminal via properly escaped arguments, never via shell string concatenation
- **Working directory validation:** Validate directory exists and is accessible before spawning process
- **No elevation:** CLIHub runs with standard user privileges, never requests admin rights

### Secrets and Credentials

- **No credential storage:** CLIHub does not store API keys, tokens, or passwords
- **Plugin executables:** Users responsible for securing their AI CLI tool credentials
- **Config file contents:** Project paths and preferences only, no sensitive data

### Update Mechanism (Future)

- **HTTPS only:** Future update checks must use HTTPS endpoints with certificate validation
- **Signature verification:** Downloaded updates must be signed and verified before installation
- **User consent:** Updates require explicit user approval, never auto-install
