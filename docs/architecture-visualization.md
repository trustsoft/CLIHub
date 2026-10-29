# CLIHub Architecture Visualization
**Date:** 2026-10-29

## Current Layer Architecture

```
┌───────────────────────────────────────────────────────────────────────────┐
│                          CLIHub (WPF Application)                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │   Views      │  │  ViewModels  │  │ Coordinators │  │     Tray     │ │
│  │              │  │              │  │              │  │              │ │
│  │ • Windows    │  │ • Launch     │  │ • Command    │  │ • Icon       │ │
│  │ • Pages      │  │ • Settings   │  │ • Actions    │  │ • Menu       │ │
│  │ • Controls   │  │ • Projects   │  │ • Status     │  │ • State      │ │
│  │              │  │ • Agents     │  │              │  │              │ │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘ │
│                                                                            │
│  Dependencies: WPF, H.NotifyIcon, MS.DI                                   │
└───────────────────────────────────────────────────────────────────────────┘
                                    ↓ depends on
┌───────────────────────────────────────────────────────────────────────────┐
│                     CLIHub.Core.Windows (Adapters)                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │   Registry   │  │   Process    │  │    Theme     │  │  Instance    │ │
│  │              │  │   Runners    │  │  Detection   │  │    Guard     │ │
│  │ • Startup    │  │              │  │              │  │              │ │
│  │   Service    │  │ • Interactive│  │ • System     │  │ • Named      │ │
│  │              │  │ • Captured   │  │   Theme      │  │   Mutex      │ │
│  │              │  │ • Inspector  │  │              │  │ • Pipe       │ │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘ │
│                                                                            │
│  Platform: Windows APIs (Registry, WMI, User32, DwmApi)                  │
└───────────────────────────────────────────────────────────────────────────┘
                                    ↓ depends on
┌───────────────────────────────────────────────────────────────────────────┐
│                       CLIHub.Core (Domain + Infra)                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │ Projects     │  │   Plugins    │  │    Agents    │  │    Config    │ │
│  │              │  │              │  │              │  │              │ │
│  │ • State      │  │ • Catalog    │  │ • Commands   │  │ • Document   │ │
│  │ • Enrichment │  │ • Validator  │  │ • Detection  │  │ • Snapshot   │ │
│  │ • Logos      │  │ • Seeder     │  │ • Versions   │  │ • Migrations │ │
│  │              │  │              │  │ • Updates    │  │              │ │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘ │
│                                                                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │  Scanning    │  │   Updates    │  │   Hotkeys    │  │ Infrastructure│ │
│  │              │  │              │  │              │  │              │ │
│  │ • Watcher    │  │ • Velopack   │  │ • Parser     │  │ • Processes  │ │
│  │ • Discovery  │  │ • Checker    │  │ • Definition │  │ • Paths      │ │
│  │ • Service    │  │ • Downloader │  │              │  │ • Logging    │ │
│  │              │  │ • Installer  │  │              │  │              │ │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘ │
│                                                                            │
│  Platform: .NET 10, System.Text.Json, Serilog                            │
└───────────────────────────────────────────────────────────────────────────┘
```

## Subsystem Dependency Graph (High-Level)

```
              ┌──────────┐
              │  UI      │
              │ (WPF)    │
              └────┬─────┘
                   │
      ┌────────────┼────────────┐
      │            │            │
      ▼            ▼            ▼
┌──────────┐ ┌──────────┐ ┌──────────┐
│  Tray    │ │ Settings │ │ Launch   │
│ Controls │ │ Pages    │ │ Window   │
└────┬─────┘ └────┬─────┘ └────┬─────┘
     │            │            │
     └────────────┼────────────┘
                  │
            ┌─────▼─────┐
            │Coordinators│
            └─────┬─────┘
                  │
     ┌────────────┼────────────┐
     │            │            │
     ▼            ▼            ▼
┌─────────┐  ┌─────────┐  ┌─────────┐
│ Agents  │  │Projects │  │ Updates │
│ Services│  │Services │  │ Services│
└────┬────┘  └────┬────┘  └────┬────┘
     │            │            │
     └────────────┼────────────┘
                  │
            ┌─────▼─────┐
            │  Plugins  │
            └─────┬─────┘
                  │
            ┌─────▼─────┐
            │   Config  │
            └───────────┘
```

## Data Flow: Agent Command Execution

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. User clicks "Launch" on agent card                          │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. AgentCardItem.LaunchCommand → LaunchCommandCoordinator      │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. IAgentCommandWorkflow.ExecuteAsync                          │
│    - Validates project path exists                             │
│    - Checks command availability                               │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. IAgentCommandService.ExecuteAsync (Core)                    │
│    - Resolves command from plugin                              │
│    - Builds command line (ProcessLauncher)                     │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. IInteractiveProcessRunner.LaunchAsync (Windows)             │
│    - Resolves runtime (pwsh, bash, cmd)                       │
│    - Spawns terminal process                                   │
└─────────────────────────────────────────────────────────────────┘
```

## Configuration Persistence Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. User changes setting (e.g., theme, accent, scanning path)   │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. ViewModel updates in-memory state                           │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. PreferenceApplier.ApplyAndSave                              │
│    - Validates changes                                         │
│    - Applies to runtime targets (theme, accent)                │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. IPreferencesStore.Update                                    │
│    - Maps to PreferencesDocument                               │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. IConfigurationRepository.UpdateAsync                        │
│    - Creates snapshot (deep copy)                              │
│    - Serializes to JSON                                        │
│    - Atomic write (temp file + rename)                         │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│ 6. config.json persisted to %APPDATA%\CLIHub\                 │
└─────────────────────────────────────────────────────────────────┘
```

## Test Structure Mirror

```
tests/
├── CLIHub.Core.Tests/          (638 tests - xUnit)
│   ├── Agents/                 (command service, detection, versions)
│   ├── Configuration/          (persistence, migrations, snapshots)
│   ├── Plugins/                (catalog, validation, seeding)
│   ├── Projects/               (state, enrichment, paths)
│   ├── Scanning/               (watcher, discovery)
│   ├── Updates/                (checker, downloader, installer)
│   └── Fakes/                  (process runners, environment)
│
├── CLIHub.Core.Windows.Tests/  (60 tests - xUnit)
│   ├── WindowsAgentProcessInspectorTests
│   ├── WindowsInteractiveProcessRunnerTests
│   ├── WindowsRuntimeEnvironmentTests
│   └── StartupServiceTests
│
└── CLIHub.Tests/               (523 tests - xUnit)
    ├── ViewModels/
    │   ├── Launch/             (window, commands, coordinators)
    │   ├── Settings/           (all settings pages)
    │   ├── Projects/           (pane controller, registry)
    │   └── Agents/             (management, cards, detection)
    ├── Builders/               (ViewModel test builders)
    ├── Themes/                 (palette switching, system theme)
    ├── Composition/            (DI registration tests)
    └── Lifetime/               (startup, session, instance guard)
```

## Proposed Future Architecture (Hexagonal)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          Presentation Layer                             │
│                                                                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                │
│  │     WPF      │  │  Avalonia    │  │     CLI      │                │
│  │   (current)  │  │   (future)   │  │   (future)   │                │
│  └──────────────┘  └──────────────┘  └──────────────┘                │
│                                                                         │
└────────────────────────────────┬────────────────────────────────────────┘
                                 │ uses
┌────────────────────────────────▼────────────────────────────────────────┐
│                        Application Services                             │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │  Use Cases (Application Layer)                                   │ │
│  │  • LaunchAgentUseCase                                           │ │
│  │  • DetectAgentsUseCase                                          │ │
│  │  • UpdateAgentUseCase                                           │ │
│  │  • DiscoverProjectsUseCase                                      │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                                                                         │
└────────────────────────────────┬────────────────────────────────────────┘
                                 │ orchestrates
┌────────────────────────────────▼────────────────────────────────────────┐
│                           Domain Layer (Core)                           │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │  Domain Models                                                   │ │
│  │  • Project (aggregate root)                                     │ │
│  │  • Agent (aggregate root)                                       │ │
│  │  • Plugin (aggregate root)                                      │ │
│  │  • Configuration (aggregate root)                               │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │  Domain Services                                                 │ │
│  │  • AgentDetectionService                                        │ │
│  │  • ProjectEnrichmentService                                     │ │
│  │  • PluginValidationService                                      │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │  Domain Events                                                   │ │
│  │  • ProjectSelected                                              │ │
│  │  • AgentDetected                                                │ │
│  │  • ThemeChanged                                                 │ │
│  │  • ConfigurationMigrated                                        │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │  Ports (Interfaces)                                             │ │
│  │  • IProcessRunner                                               │ │
│  │  • IPersistenceService                                          │ │
│  │  • IFileSystem                                                  │ │
│  │  • IHttpClient                                                  │ │
│  │  • ISystemEnvironment                                           │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                                                                         │
└────────────────────────────────┬────────────────────────────────────────┘
                                 │ implemented by
┌────────────────────────────────▼────────────────────────────────────────┐
│                      Infrastructure Adapters                            │
│                                                                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌───────────┐ │
│  │   Windows    │  │  FileSystem  │  │     HTTP     │  │  Velopack │ │
│  │              │  │              │  │              │  │           │ │
│  │ • Registry   │  │ • JSON Store │  │ • Release    │  │ • Update  │ │
│  │ • WMI        │  │ • Logo Cache │  │   Client     │  │   Manager │ │
│  │ • Processes  │  │              │  │              │  │           │ │
│  └──────────────┘  └──────────────┘  └──────────────┘  └───────────┘ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Key Architectural Patterns

### 1. Repository Pattern (Configuration)
```
ConfigurationRepository
  └─> Snapshot (immutable state)
       └─> AppConfig (document)
            └─> PreferencesDocument
            └─> ProjectStateDocument
            └─> ScanningSettingsDocument
```

### 2. Service Layer Pattern (Agents)
```
AgentManagementService (orchestrator)
  ├─> AgentDetectionService
  ├─> AgentVersionService
  ├─> AgentCommandService
  ├─> AgentUpdateAvailabilityService
  └─> AgentProcessMonitor
```

### 3. MVVM Pattern (UI)
```
View (XAML)
  └─> ViewModel (state + commands)
       ├─> Controller (pane/section orchestration)
       ├─> Coordinator (action dispatch)
       └─> Core Services (business logic)
```

### 4. Adapter Pattern (Windows)
```
Core Port (interface)          Windows Adapter (implementation)
  IInteractiveProcessRunner ──> WindowsInteractiveProcessRunner
  IProcessOutputRunner      ──> WindowsProcessOutputRunner
  IStartupService           ──> StartupService (registry)
  ISingleInstanceGuard      ──> SingleInstanceGuard (named mutex)
```

## Dependency Rules (ADR-0001, ADR-0006)

```
✅ Allowed:
   UI ──depends on──> Windows ──depends on──> Core
   Test ──depends on──> Production

❌ Forbidden:
   Core ──depends on──> Windows
   Core ──depends on──> UI
   Windows ──depends on──> UI
   Production ──depends on──> Test
```

## Cross-Cutting Concerns (Current vs. Proposed)

### Current (Scattered)
```
Logging:        Serilog injected into services
Persistence:    Direct ConfigurationRepository calls
Caching:        Separate LogoCacheService, PluginCatalog
Error Handling: Try-catch in callers
```

### Proposed (Unified)
```
Logging:        ILoggingContextProvider (correlation IDs)
Persistence:    IPersistenceService<T> (generic boundary)
Caching:        ICacheService<TKey, TValue> (pluggable)
Error Handling: Result<T> types + domain events
Telemetry:      Domain event subscribers (observers)
```

---

## Legend

```
┌─────┐  Component/Layer
│     │
└─────┘

────>    Dependency (uses)
═══>     Strong coupling
- - ->    Weak coupling
```
