# Proposal: centralize-app-data-paths

## Why

The `%APPDATA%\CLIHub` root and its subfolder structure are re-derived in five places (`ConfigService`, `PluginManager`, `LogoCacheService`, `PluginSeeder`, `DirectoryInitializer`), each repeating the `"CLIHub"` literal and the subdirectory names. Adding a new file to the layout means editing several places and risks drifting from the documented layout in `docs/architecture.md → File System Layout`.

## What Changes

- Add a static `AppPaths` class in `CLIHub.Core` exposing the ready-computed data layout: `Root`, `LogsDirectory`, `PluginsDirectory`, `CacheDirectory`, `ConfigFile`, `LogosCacheFile` — the single source of truth mirroring `architecture.md → File System Layout`.
- `DirectoryInitializer` keeps its creation side effect but consumes `AppPaths` instead of hardcoding subdirectory names.
- `ConfigService`, `PluginManager`, `LogoCacheService`, and `PluginSeeder` switch their default path construction to `AppPaths.*`; their optional override parameters (used by tests) are unchanged.
- `App.ConfigureLogging` and `LaunchWindowViewModel.OpenDataFolder` use `AppPaths` instead of `DirectoryInitializer.GetAppDataRoot()` string building.

No behavior change: same paths, same layout, same file names. Pure refactor — specs are skipped (`skip_specs: true`).

## Capabilities

### New Capabilities

(none — no spec-level behavior changes)

### Modified Capabilities

(none)

## Impact

- **New code**: `src/CLIHub.Core/Services/AppPaths.cs`.
- **Modified code**: `src/CLIHub.Core/Services/DirectoryInitializer.cs`, `ConfigService.cs`, `PluginManager.cs`, `LogoCacheService.cs`, `PluginSeeder.cs`, `src/CLIHub/App.xaml.cs`, `src/CLIHub/ViewModels/LaunchWindowViewModel.cs`.
- **Tests**: no changes required — services keep their explicit path overrides; `DirectoryInitializerTests` keep passing.
- The `AgentVersionService` `UserProfile` working directory is intentionally out of scope (a different concern: agent probe context, not the CLIHub data layout).
