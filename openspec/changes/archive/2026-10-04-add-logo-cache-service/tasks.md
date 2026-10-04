## 1. Logo cache service (CLIHub.Core)

- [x] 1.1 Add `ILogoCacheService` (`GetOrResolve(string key, Func<string?> resolve)`, `InvalidateAll()`, `IDisposable`) in `src/CLIHub.Core/Interfaces/` with XML doc comments; verify the project compiles
- [x] 1.2 Implement `LogoCacheService` in `src/CLIHub.Core/Services/`: `ConcurrentDictionary<string, string?>` store, state file path `%APPDATA%\CLIHub\cache\logos.json`, load-on-construct with try/catch (corrupt file → empty cache + warning log); verify with a unit test that a valid state file is loaded and a corrupt one is tolerated
- [x] 1.3 Implement resolution semantics: return cached value without I/O on hit; on miss run the resolve delegate, store the result (including null) write-through; verify with unit tests for hit, miss-store, and negative-result caching
- [x] 1.4 Implement `Dispose()`: atomic save (temp file + `File.Move(overwrite: true)`) guarded by a dirty flag, no write when unchanged; verify with unit tests for save-on-dispose, no-rewrite-when-clean, and atomic temp cleanup
- [x] 1.5 Implement `InvalidateAll()` clearing entries; verify the next `GetOrResolve` re-invokes the delegate and updates the cache by unit test

## 2. Integration into resolution flows

- [x] 2.1 Register `ILogoCacheService` as a singleton in `AddClIHubCoreServices` (`src/CLIHub.Core/ServiceCollectionExtensions.cs`); verify via `ServiceCollectionExtensionsTests`
- [x] 2.2 Route project logo resolution in `ProjectService` (`RefreshLogos`, `GetCurrentProject`, `AddProject`) through `GetOrResolve("project:<Id>", ...)`, keeping the scan loop as the resolve delegate and `DefaultLogoPath` fallback in the caller; verify existing `ProjectServiceTests` still pass and update them for cache-backed behavior (cached hit does not rescan)
- [x] 2.3 Route plugin logo resolution in `PluginManager.LoadPluginLogo` through `GetOrResolve("plugin:<Id>", ...)`; verify `PluginManagerTests` still pass and add a test that a second load reuses the cached path
- [x] 2.4 Wire `_logoCacheService.InvalidateAll()` into the refresh action in `LaunchWindowViewModel.Refresh` next to detection/version invalidation; verify the solution builds (`dotnet build CLIHub.sln`)
- [x] 2.5 Add `Remove(string key)` to `ILogoCacheService`/`LogoCacheService` (deletes the entry, marks dirty) and call it from `ProjectService.RemoveProject`; verify with unit tests: removed key re-resolves, and the removed project's key is absent from the saved state file

## 3. Persistence across restarts

- [x] 3.1 Add a test simulating restart: service instance A resolves a logo and disposes (file written); instance B constructed on the same folder returns the cached value without invoking its delegate; verify test passes
- [x] 3.2 Verify no state file is created on first run exit when nothing was resolved; verify test passes

## 4. Verification

- [x] 4.1 Run the full test suite (`dotnet test CLIHub.sln`) and confirm all tests pass
- [x] 4.2 Run `dotnet build CLIHub.sln` with no warnings introduced; run the app, add a project with a `logo.png`, restart, and confirm the logo displays from cache and `%APPDATA%\CLIHub\cache\logos.json` exists with the `project:` and `plugin:` keys
