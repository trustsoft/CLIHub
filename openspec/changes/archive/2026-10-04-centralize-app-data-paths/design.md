# Design: centralize-app-data-paths

## Context

`%APPDATA%\CLIHub` is assembled from `Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)` plus string building in five places. See proposal.md for motivation.

## Goals / Non-Goals

**Goals:**

- One code location describing the data layout, mirroring `docs/architecture.md → File System Layout`.
- Consumers keep their optional path-override parameters (test isolation from the real `%APPDATA%` is preserved).

**Non-Goals:**

- No DI interface / `IAppDataPaths` abstraction (YAGNI; reconsider if a portable-mode or alternate-root requirement appears).
- No change to what paths are produced, when directories are created, or any persistence behavior.
- The `AgentVersionService` `UserProfile` probe working directory (different concern).

## Decisions

### 1. Static class, not a DI service

`AppPaths` is a `static sealed` class with computed `string` properties. Paths are needed before the DI container exists (`App.OnStartup` → `EnsureAppDataLayout` → `ConfigureLogging` with `PreferenceReader` reading `config.json` raw), so a pure DI singleton could not serve the bootstrap path; a static class serves both phases with one form. A wrapper interface would be ceremony without a consumer that needs substitution — every service already takes explicit path overrides.

*Alternative considered*: `IAppDataPaths` singleton injected into the four services — rejected for the bootstrap asymmetry and YAGNI.

### 2. Data and side effects stay split

`AppPaths` is pure data (path computation). `DirectoryInitializer` keeps the creation side effect (`EnsureAppDataLayout`) but reads names from `AppPaths`, so subdirectory names exist in exactly one place. `GetAppDataRoot()` delegates to `AppPaths.Root` and is kept for compatibility while its callers move over.

### 3. Members mirror the documented layout

`Root`, `LogsDirectory`, `PluginsDirectory`, `CacheDirectory`, `ConfigFile`, `LogosCacheFile` — no speculative members.

## Risks / Trade-offs

- [A static path source is harder to redirect wholesale in tests than an injected one] → Every consumer keeps its constructor override; tests already use them, and `DirectoryInitializer`/`AppPaths` are trivial enough to not need their own substitution seam.

## Migration Plan

Pure internal refactor; no data, config, or on-disk changes. Rollback is a plain revert.

## Open Questions

None.
