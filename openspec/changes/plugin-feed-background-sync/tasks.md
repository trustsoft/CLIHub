# Plugin feed background sync: Implementation Tasks

## Prerequisites

- Working tree clean at commit 7b95c0e.
- `openspec validate --all` passes before starting.
- All existing tests pass: `dotnet test CLIHub.sln`.

## Stage 1: Cache Infrastructure and Conditional Request Support

### Objective
Add ETag/Last-Modified handling, cache file paths, and conditional-request mechanics to `PluginFeedClient`. No service integration yet; focus on client-level read/write of sync metadata and cache fallback.

### Tasks

- [x] 1.1 — Update `AppPaths.cs`
  - Add `FeedCacheFile` property: `%APPDATA%\CLIHub\cache\feed-index.json`
  - Add `FeedSyncMetadataFile` property: `%APPDATA%\CLIHub\cache\feed-sync.json`
  - Document both in the class comment
  - Verify: `dotnet build CLIHub.sln`

- [x] 1.2 — Add `PluginFeedSyncMetadata.cs` record (in CLIHub.Core/Plugins)
  - Record: `PluginFeedSyncMetadata { ETag, LastModified, Timestamp, IndexHash }`
  - All fields nullable (tolerate partial reads)
  - JSON-serializable with `[JsonPropertyName]` attributes (snake_case in JSON)
  - Verify: `dotnet build CLIHub.sln`

- [x] 1.3 — Modify `PluginFeedResult.cs`
  - Add `FromCache: bool` property (default false)
  - Add `CachedAt: DateTimeOffset?` property (when the cache was stored)
  - Document the fields
  - Verify: `dotnet build CLIHub.sln`

- [x] 1.4 — Modify `IPluginFeedClient.cs`
  - Update `GetFeedAsync` return type to include the `FromCache` flag (already in `PluginFeedResult`)
  - No signature change needed if result already includes the flag
  - Verify: `dotnet build CLIHub.sln`

- [x] 1.5 — Extend `PluginFeedClient.cs` with conditional-request and cache logic
  - Add private methods:
    - `LoadSyncMetadataAsync(CancellationToken)`: Read `feed-sync.json` (tolerant parse, return null on missing/corrupt).
    - `SaveSyncMetadataAsync(metadata, CancellationToken)`: Write `feed-sync.json` (atomic via temp+rename or direct overwrite for small file).
    - `LoadCachedIndexAsync()`: Read `feed-index.json` (check 2 MB bound, return null if missing/corrupt).
    - `SaveCachedIndexAsync(indexBytes, CancellationToken)`: Write `feed-index.json`.
    - `ReadResponseHeaderValue(response, headerName)`: Helper to read ETag or Last-Modified from response headers.
  - Modify `GetFeedAsync`:
    - Load sync metadata at start.
    - Build request headers: if ETag present, add `If-None-Match: <etag>`; else if Last-Modified present, add `If-Modified-Since: <date>`.
    - Send request.
    - Handle 304: return success with cached index (or error if cache missing) and `FromCache: false` (304 is not from cache; it's a fresh confirmation).
    - On successful 200: parse index, save raw bytes to cache, read response headers (ETag, Last-Modified), save metadata, return success with `FromCache: false`.
    - On error (network, timeout, parse error): load cached index, if available return success with `FromCache: true` and `CachedAt` timestamp; else return error.
    - Corrupt cache handling: delete corrupt files with a logged warning.
  - Verify: `dotnet build CLIHub.sln`

- [x] 1.6 — Add `PluginFeedClientTests` covering conditional requests and cache
  - Test: valid request with ETag returns 304, cache used (mock 304 response).
  - Test: valid request with Last-Modified returns 304.
  - Test: successful fetch stores cache and metadata.
  - Test: fetch error with cache available falls back to cache, `FromCache: true`.
  - Test: fetch error with no cache returns error.
  - Test: corrupt cache file deleted with warning.
  - Test: cached index exceeding 2 MB not used.
  - All existing 24+ `PluginFeedClientTests` pass unchanged.
  - Run: `dotnet test CLIHub.sln`

- [x] Gate: All tests pass (`dotnet test CLIHub.sln`); `openspec validate --all`

---

## Stage 2: Domain Event and Background Sync Service

### Objective
Implement the `PluginFeedUpdatedEvent` domain event and the background sync service (`IPluginFeedSyncService`). Service runs asynchronously, publishes events on feed changes, but is not yet wired into startup.

### Tasks

- [x] 2.1 — Add `PluginFeedUpdatedEvent.cs` (new record in CLIHub.Core/Events)
  - Record: `PluginFeedUpdatedEvent(string IndexHash) : IDomainEvent`
  - Document the event purpose
  - Verify: `dotnet build CLIHub.sln`

- [x] 2.2 — Add `IPluginFeedSyncService.cs` and `PluginFeedSyncService.cs` (new in CLIHub.Core/Plugins)
  - Interface: `IPluginFeedSyncService`
    - `void Start()`: Begin background refresh loop (or no-op if already started).
    - `async Task StopAsync(TimeSpan timeout)`: Cancel pending refresh and wait for completion.
  - Service class:
    - Constructor: `IPluginFeedClient`, `IDomainEventPublisher`, `ILogger`, `TimeProvider` (optional), `CancellationToken` (for testing).
    - Private fields: timer (PeriodicTimer or System.Timers.Timer), CancellationTokenSource, started flag, sync lock.
    - `Start()`: Initialize timer to trigger immediately, then every 1 hour; run async without awaiting.
    - Background refresh logic:
      - Call `IPluginFeedClient.GetFeedAsync`.
      - If successful and not 304: compute change hash (SHA-256 of sorted {id, version, compatibility} tuples).
      - If hash differs from stored hash, publish `PluginFeedUpdatedEvent(newHash)` and save new hash.
      - If 304 or error: do not publish event.
      - Errors logged at Warning; exceptions caught (do not throw).
    - `StopAsync(timeout)`: Cancel timer, wait for any in-flight refresh with timeout, dispose timer.
    - Implement `IDisposable`: dispose timer and CTS.
  - Verify: `dotnet build CLIHub.sln`

- [x] 2.3 — Add `PluginFeedSyncServiceTests.cs` (in tests/CLIHub.Core.Tests/Plugins)
  - Test: `Start()` initializes timer and triggers first refresh.
  - Test: `StopAsync(timeout)` cancels in-flight refresh and disposes timer.
  - Test: successful fetch with changed index publishes `PluginFeedUpdatedEvent`.
  - Test: 304 or unchanged index does not publish event.
  - Test: fetch error does not publish event, logged at Warning.
  - Test: change detection uses correct hash (sorted {id, version, compatibility}).
  - Test: service is idempotent (multiple `Start()` calls are safe).
  - Test: service is cancellation-aware (StopAsync cancels pending refresh).
  - Run: `dotnet test CLIHub.sln`

- [x] 2.4 — Register `IPluginFeedSyncService` in DI (src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs or src/CLIHub/Composition/ServiceRegistration.cs)
  - Add singleton registration: `services.AddSingleton<IPluginFeedSyncService, PluginFeedSyncService>();`
  - Verify: `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`

- [x] Gate: All tests pass; `openspec validate --all`

---

## Stage 3: Startup Integration and Subscription

### Objective
Wire the sync service into application startup and shutdown, and add the event subscription in `AgentManagementViewModel`.

### Tasks

- [x] 3.1 — Integrate `IPluginFeedSyncService` into startup (src/CLIHub/Startup/ApplicationBootstrapper.cs)
  - Inject `IPluginFeedSyncService` via constructor.
  - In `Start(context)`, after `this.optionalStartup.RunOptionalStartup(...)` (line 71) and before or after `this.projectScanner.Start()` (line 73), call `this.pluginFeedSyncService.Start()`.
  - The sync service runs asynchronously; it does not block startup.
  - Document the startup order in code comment.
  - Verify: `dotnet build CLIHub.sln`

- [x] 3.2 — Integrate `IPluginFeedSyncService` into shutdown (src/CLIHub/Startup/ApplicationHost.cs)
  - The service is a singleton and will be disposed by the DI container at step 4 of shutdown (line 81 in docs/architecture/startup.md).
  - Verify that the service's `Dispose` is called and any in-flight refresh is cancelled.
  - No code change needed if the service is registered as singleton; verify via test.
  - Verify: `dotnet build CLIHub.sln`

- [x] 3.3 — Add event subscription in `AgentManagementViewModel` (src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs)
  - Inject `IDomainEventSubscriber` via constructor (alongside existing `IDomainEventPublisher` if present, or inject the bus directly).
  - In constructor, subscribe to `PluginFeedUpdatedEvent`:
    ```csharp
    this.eventSubscriber.Subscribe<PluginFeedUpdatedEvent>(evt => this.OnPluginFeedUpdated(evt));
    ```
  - Add handler method `OnPluginFeedUpdated(PluginFeedUpdatedEvent evt)`:
    - Reload the plugin catalog (via existing `this.catalog.ReloadPlugins()`).
    - Refresh agent card badges (call existing refresh logic or add a dedicated method).
  - In `Dispose`, if using `Subscribe` that returns `IDisposable`, dispose the subscription.
  - Verify: `dotnet build CLIHub.sln`

- [x] 3.4 — Update `AgentManagementViewModelTests.Plugins.cs` to cover the subscription
  - Test: `OnPluginFeedUpdated` calls `ReloadCatalog()` and refreshes badges.
  - Test: event handlers are registered and disposed correctly.
  - All existing plugin-related tests pass unchanged.
  - Run: `dotnet test CLIHub.sln`

- [ ] Gate: All tests pass; `openspec validate --all`; manual smoke: launch app, verify sync starts in background (check logs or wait 5 seconds and see badge update if feed has changes).

---

## Stage 4: Browse Dialog Cache Fallback Message

### Objective
Update `BrowsePluginsDialogViewModel` to handle the `FromCache` flag and display a stale-data message.

### Tasks

- [x] 4.1 — Modify `BrowsePluginsDialogViewModel.cs`
  - Search for `LoadFeedAsync` or the method that calls `this.pluginFeedClient.GetFeedAsync`.
  - After fetching, check `result.FromCache`.
  - If `result.FromCache && result.Success`, set a status message (or UI field) to: "Using cached plugin list (last updated {cachedAt:format}). Check your internet connection to refresh."
  - Format `cachedAt` in a user-friendly way (e.g., "2 hours ago" or "Nov 14, 2:30 PM").
  - If `result.FromCache && !result.Success`, use the existing error string (cache not available).
  - If `!result.FromCache`, do not show the stale-data message (show existing success or error as today).
  - Verify: `dotnet build CLIHub.sln`

- [x] 4.2 — Update Browse Plugins UI (XAML or code-behind in src/CLIHub/Views or ViewModels)
  - Locate the Browse Plugins dialog view/ViewModel (e.g., `BrowsePluginsDialog.xaml` and its code-behind or the ViewModel).
  - Add a field/property to hold the cache status message (e.g., `CacheStatusMessage: string`).
  - Bind the message to a UI element (e.g., a `TextBlock` below the plugin list or in a status bar).
  - The message is shown only when `BrowsePluginsDialogViewModel.CacheStatusMessage` is non-empty.
  - Verify: `dotnet build CLIHub.sln`

- [x] 4.3 — Add tests for cache-fallback message (BrowsePluginsDialogViewModelTests.cs)
  - Test: `LoadFeedAsync` with `FromCache: true` sets cache-status message.
  - Test: cache-status message is formatted correctly (time-ago or readable timestamp).
  - Test: cache-status message is not set if `FromCache: false`.
  - All existing Browse tests pass unchanged.
  - Run: `dotnet test CLIHub.sln`

- [ ] Gate: All tests pass; `openspec validate --all`; manual smoke: disable internet, open Browse, verify cache message appears (or fake a 304 response to test cache load without disabling internet).

---

## Stage 5: Spec Deltas

### Objective
Write the OpenSpec spec deltas for `plugin-feed` (modified) and `plugin-feed-sync` (new capability).

### Tasks

- [x] 5.1 — Create `specs/plugin-feed-sync/spec.md` (new capability)
  - Copy the structure from an existing spec (e.g., `plugin-feed` or `plugin-registry`).
  - **Purpose:** Allows automatic refresh of the plugin feed in the background with HTTP conditional requests to reduce network usage, maintains a last-good index cache for resilience, and publishes domain events when the feed changes so subscribers can update UI state.
  - **Requirements:**
    - Background feed refresh: service starts on application startup (after plugins initialized), refreshes immediately, then every 1 hour, cancellation-aware shutdown.
    - Conditional fetch: sends ETag (If-None-Match) or Last-Modified (If-Modified-Since); handles 304 Not Modified; falls back to cache on network error.
    - Last-good index cache: stores raw index bytes and sync metadata in separate files under `%APPDATA%\CLIHub\cache\`; tolerant reads (missing/corrupt files deleted with warning); 2 MB bound.
    - Change detection: compares {id, version, compatibility} across plugins; publishes `PluginFeedUpdatedEvent` on change; no event on 304 or error.
  - Scenarios: (see design.md `plugin-feed-sync` section for the list)
  - Verify: `openspec validate --all`

- [x] 5.2 — Modify `specs/plugin-feed/spec.md` (delta)
  - Preserve all 16 original scenario names.
  - Add new scenarios: conditional-request 304, cache fallback.
  - Update "Network unavailable" scenario to note cache fallback behavior.
  - Update "Fetch and parse feed index" requirement to mention conditional headers.
  - Verify: `openspec validate --all`

- [x] 5.3 — Investigate and possibly modify agent-update or agent-ui spec for plugin-update badge
  - If `UpdateAvailable` chip is covered by `agent-update-detection`, add a delta scenario.
  - If not, clarify where the chip behavior is documented and add an appropriate delta.
  - Scenarios: "Plugin update available", "Agent update takes precedence over plugin update", "Update chip shows when installed version < feed version and compatible".
  - Verify: `openspec validate --all`

- [x] Gate: `openspec validate --all` passes; all spec deltas are coherent with code changes.

---

## Stage 6: Verification and Cleanup

### Objective
Final integration tests, graphify update, and verification that all guarantees hold.

### Tasks

- [x] 6.1 — Integration tests
  - Full `dotnet test CLIHub.sln` (all tests pass).
  - Verify no regressions in existing feed, agent, or plugin tests.

- [x] 6.2 — Graphify update
  - Run `graphify update .` to refresh the knowledge graph.
  - Verify: no errors, graph reflects new service and event.

- [ ] 6.3 — Manual smoke testing
  - Launch CLIHub with the updated code.
  - Verify app starts normally and sync service begins in background (check logs or wait 5 seconds).
  - Open Browse Plugins; verify fresh feed loads (no cache message).
  - Disable internet; refresh Browse; verify cache-fallback message appears or hard error (as before).
  - Re-enable internet; verify next refresh (within 1 hour, or manually trigger via a test interval) refreshes feed and clears cache message.
  - Check that agent card badges update when feed changes (compare before/after sync).

- [x] 6.4 — Code review checklist
  - ✓ HTTPS-only enforced on every hop (no new HTTP calls).
  - ✓ 2 MB index bound respected (cache check in place).
  - ✓ Startup not blocked (sync runs via `IApplicationOperationLifetime.RunAsync`).
  - ✓ Shutdown cancels cleanly (`IApplicationOperationLifetime.StopAsync` called).
  - ✓ Existing error strings preserved (only new cache message).
  - ✓ Tolerant cache behavior (missing/corrupt files deleted, no exceptions).
  - ✓ Core WPF-free (sync service in Core; subscription in ViewModel).
  - ✓ Domain events follow adoption rules (idempotent, exceptions caught, no behavior change).

- [ ] 6.5 — Commit and archive
  - Stage changes for commit (verify no extraneous edits).
  - Commit with message summarizing the feature (background sync, conditional requests, cache, event, badges).
  - Run `openspec archive plugin-feed-background-sync --message "Background feed sync with conditional requests, cache, domain event, and update badges; satisfies ADR-0015 roadmap slice 3."` (or similar).
  - Verify: change is moved to `openspec/changes/archive/`, no errors.
  - Verify post-archive integrity: `git status --porcelain` is empty, `openspec list` does not include the change, `openspec validate --all` passes, `node .opencode/scripts/pipeline-state.mjs check` reports no warnings (or expected warnings only).

- [ ] Gate: All tests pass; smoke tests confirm feature works; archive successful; integrity checks pass.

---

## Build and Test Gates

After each stage, verify:

```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
openspec validate --all
```

If any stage fails a gate, stop and diagnose before proceeding to the next stage. Do not skip or defer gates.

## Success Criteria

- All tests pass (no new failures, all existing tests still pass).
- `openspec validate --all` produces zero errors (warnings are acceptable).
- `graphify update .` completes without error.
- Manual smoke tests confirm: background sync runs, feed is cached, fallback message appears on network failure, badges update on feed change.
- Feature adheres to all non-negotiable guarantees (HTTPS, bounds, startup/shutdown, tolerant errors, Core WPF-free).
- Spec deltas are coherent with implementation.
