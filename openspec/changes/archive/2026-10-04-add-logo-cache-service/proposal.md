# Proposal: add-logo-cache-service

## Why

Logo resolution currently scans the file system on every request: each project query probes up to 7 well-known filenames (`ProjectService.RefreshLogos`/`ResolveLogo`), and every application start rescans each plugin folder for its logo — all on the UI thread. A persistent, key-based logo cache removes these repeated scans and makes logo lookups O(1) dictionary hits.

## What Changes

- Introduce a new core service `ILogoCacheService`/`LogoCacheService` that resolves logos by a unique key (`project:<projectId>` for projects, `plugin:<pluginId>` for agents) and caches the result, including negative results (a resolved "no logo found").
- The cache is loaded from `%APPDATA%\CLIHub\cache\logos.json` at application startup and saved back to that file on application shutdown, so cached values persist between runs. During a run, fresh resolutions update the loaded values ("write-through" on top of loaded state).
- `ProjectService` resolves project logos through the cache (keyed by project ID); the default-logo fallback stays outside the cache.
- `PluginManager` resolves plugin logos through the cache (keyed by plugin ID).
- Cache invalidation is wired to the manual refresh action alongside detection and version cache invalidation, so refresh re-checks the file system and updates the cache.
- Cache writes are atomic (temp file + rename) and skipped when nothing changed (dirty tracking).

## Capabilities

### New Capabilities

- `logo-cache`: Persistent key-based cache for project and agent logo paths, loaded from and saved to a state file, with write-through updates during a run and on-demand invalidation.

### Modified Capabilities

- `project-management`: The "Project logo detection" requirement now resolves logos through the persistent logo cache keyed by project ID; repeated lookups return cached results without file-system scans, results survive restarts, and a manual refresh re-resolves.

## Impact

- **New code**: `src/CLIHub.Core/Interfaces/ILogoCacheService.cs`, `src/CLIHub.Core/Services/LogoCacheService.cs`, unit tests under `tests/CLIHub.Tests`.
- **Modified code**: `src/CLIHub.Core/Services/ProjectService.cs` (logo resolution path), `src/CLIHub.Core/Services/PluginManager.cs` (`LoadPluginLogo`), `src/CLIHub.Core/ServiceCollectionExtensions.cs` (DI registration), `src/CLIHub/ViewModels/LaunchWindowViewModel.cs` (refresh invalidates the logo cache).
- **New file at runtime**: `%APPDATA%\CLIHub\cache\logos.json` (the `cache\` folder is already reserved in the documented layout).
- No dependencies added; no breaking changes to existing public APIs beyond internal resolution flow.
