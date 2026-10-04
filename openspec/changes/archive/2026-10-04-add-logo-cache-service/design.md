# Design: add-logo-cache-service

## Context

`ProjectService` resolves project logos on every query (`RefreshLogos`/`ResolveLogo` probe up to 7 candidate filenames per project per call) and `PluginManager.LoadPluginLogo` scans each plugin folder for `logo.png` (plus a placeholder fallback) on every application start. Both run on the UI thread. A per-project/per-plugin unique key already exists (`Project.Id`, `Plugin.Id`), which makes a key-based cache natural. The `%APPDATA%\CLIHub\cache\` folder is reserved in the documented layout but unused.

## Goals / Non-Goals

**Goals:**

- **One shared core service that both project and agent logo resolution go through, keyed by `project:<projectId>` / `plugin:<pluginId>`.**
- **Per-key removal**: removing a project deletes its cache entry so stale keys do not accumulate in the state file.
- Persistence: state file loaded at service construction, saved on container disposal (application shutdown).
- Write-through updates: resolutions during a run update the loaded state in memory; the state file is written once at shutdown.
- Negative caching (resolved "no logo" is cached too).
- Atomic save; skip rewrite when unchanged; tolerate missing/corrupt state file.

**Non-Goals:**

- Caching image bytes or WPF `BitmapImage` instances (UI-layer concern; `PathToImageConverter` is untouched).
- Watching the file system for logo changes outside the manual refresh action.
- TTL-based expiry (unlike detection/version caches): logos change rarely and refresh is explicit.
- Removing `DefaultLogoPath` fallback logic from callers.

## Decisions

### 1. Single service with namespaced keys, not two caches

`ILogoCacheService` exposes `GetOrResolve(string key, Func<string?> resolve)`, `Remove(string key)`, `InvalidateAll()`, and persistence via `IDisposable`. Keys are `project:<id>` and `plugin:<id>`.

*Why*: one store, one persistence mechanism, one invalidation entry point; both domains get identical semantics. *Alternative considered*: separate `IProjectLogoCache`/`IAgentLogoCache` — rejected as duplication with no behavioral gain.

### 2. Cache the resolved path string (and negative results), not image data

Value type is `string?`: a concrete logo path, or null for "scanned, none found". The default-logo fallback remains in the callers, outside the cache.

*Why*: the state file stays small, human-readable, and stable across UI changes; null results are what eliminate the repeated 7-probe scans. *Alternative considered*: caching image bytes — rejected (large file, redraw/skinning concerns).

### 3. State file: `%APPDATA%\CLIHub\cache\logos.json`, JSON map of key → path

`ConcurrentDictionary<string, string?>` serialized as a flat camelCase JSON object. Loaded in the constructor; `Dispose()` writes `{ "logos": { ... } }` back atomically (temp file + `File.Move(overwrite: true)`, same pattern as `ConfigService.Save`) guarded by a dirty flag.

*Why*: consistent with existing config persistence; the reserved `cache\` folder keeps user data organized. *Alternative considered*: extending `config.json` — rejected (cache state is disposable and rebuildable; it must not bloat or corrupt the user-authored config).

### 4. Lifetime: DI singleton, save via `IDisposable`

`ServiceProvider.Dispose()` in `App.OnExit` already disposes singletons, so the save requires no new shutdown wiring. Writes only happen on disposal — resolution calls never touch the state file.

*Why*: zero new lifecycle code in `App`; matches how `TrayIconController` and others are cleaned up. *Alternative considered*: save-on-change — rejected (a logo change is rare; per-change disk writes would reintroduce UI-thread I/O this change is meant to remove).

### 5. Integration points

- `ProjectService`: `RefreshLogos`, `GetCurrentProject`, and `AddProject` call `GetOrResolve($"project:{p.Id}", () => ScanLogo(p.Path))`; the existing scan loop becomes the resolve delegate. `ResolveLogo(string projectPath)` stays on the public interface for compatibility but no longer implies a scan when a cached value exists (callers that only have a path, not an ID, keep scanning). `RemoveProject` calls `Remove($"project:{id}")` so removed projects leave no stale entries.
- `PluginManager.LoadPluginLogo` wraps its scan in `GetOrResolve($"plugin:{plugin.Id}", ...)`.
- `LaunchWindowViewModel.Refresh` adds `_logoCacheService.InvalidateAll()` next to detection/version invalidation; the next lookups re-resolve and refresh the cache.
- DI: registered in `AddClIHubCoreServices` as a singleton; injected into `ProjectService`, `PluginManager`, and the launch view model.

### 6. Corrupt/missing file tolerance

Constructor wraps the load in try/catch: any read/parse failure logs a warning and starts with an empty cache. Same non-fatal posture as `ConfigService`.

## Risks / Trade-offs

- [Stale cached path after a logo file is deleted or a project logo appears] → Manual refresh invalidates and re-resolves; trade-off accepted because logos change rarely and TTL expiry would add preference surface without user demand.
- [Stale cached path after a plugin ID is reused by a different folder] → Plugin IDs map 1:1 to folder names and are treated as stable; `PluginManager` seeds/loads by ID, so reuse implies the same logical agent.
- [Cache file grows with removed projects] → `Remove(key)` deletes the entry when the project is removed, and the deleted entry is not persisted at the next shutdown. Plugins have no removal flow, so no plugin-side cleanup is needed.
- [Crash before disposal loses run-time resolutions] → Only the first resolution after each invalidation is lost; the next run re-resolves once and re-persists.

## Migration Plan

No migration: the state file is new and optional. First run creates it on exit; deleting `%APPDATA%\CLIHub\cache\logos.json` at any time reverts to scan-on-demand behavior.

## Open Questions

None.
