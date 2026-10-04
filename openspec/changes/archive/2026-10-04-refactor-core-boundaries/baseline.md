# Core Refactoring Baseline

Recorded 2026-10-04 before implementation changes.

## Verification

- `dotnet build CLIHub.sln -c Release --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet test CLIHub.sln -c Release --no-build` — passed: 318 tests, 0 failed, 0 skipped.
- `deviations.md` — no approved deviations.

## Current service dependency map

```text
ProjectService
  -> IConfigService
  -> ILogoCacheService

PluginManager
  -> ILogoCacheService

AgentCommandService
  -> IProcessLauncher

AgentVersionService
  -> IProcessLauncher
  -> IConfigService

AgentDetectionService
  -> IConfigService

AgentListComposer
  -> IAgentDetectionService

ConfigService, LogoCacheService, PluginSeeder, DirectoryInitializer
  -> filesystem / JSON / AppPaths

ProcessLauncher
  -> System.Diagnostics.Process

StartupService
  -> IStartupRegistry / Windows Registry

SingleInstanceGuard
  -> Windows Mutex / named pipe

UpdateService
  -> Velopack
```

The direct service graph is acyclic at baseline. The main shared-state coupling is through the mutable `AppConfig` returned by `IConfigService`.

## Configuration ownership map

```text
Projects subsystem
  -> AppConfig.Projects
  -> AppConfig.CurrentProjectId

Launch preferences
  -> DefaultRuntime
  -> Hotkey
  -> ShowWindowOnStartup
  -> PinLaunchWindow
  -> PathDisplayStyle

Agent probing
  -> AgentProbeTtlMinutes
  -> AgentProbeTimeoutSeconds
  -> ShowOnlyProjectAgents

Updates
  -> CheckForUpdatesOnStartup
  -> LastSeenReleaseNotesVersion

Startup integration
  -> StartWithWindows

Logging
  -> LogLevel

Compatibility-only field
  -> TerminalExecutable
```

The migration must preserve the existing flat `config.json` shape and one atomic persistence path while moving these ownership boundaries into explicit state objects and narrower access contracts.
