# Plugin feed background sync: Design document

## Verified Current State

### Feed Client (src/CLIHub.Core/Plugins/PluginFeedClient.cs)

**File:lines with evidence:**
- 15–89: Class definition, constructor with optional descriptor reader/validator (for v2 inline descriptors).
- 91–96: `CreateDefaultHandler()` returns `SocketsHttpHandler` with `AllowAutoRedirect = false`, enabling manual redirect control.
- 99–120+: `GetFeedAsync(cancellationToken)` (read full method).

Key facts:
- `ExecuteWithStructuredErrorsAsync<T>` helper at line 200+: wraps HTTP calls with structured error handling (timeout, network, etc.), returns result with error profile.
- `SendFollowingRedirectsAsync` at line 230+: GET request with manual redirect logic, 5-hop cap (line 238: `MaxRedirectHops = 5`), HTTPS enforcement on every hop.
- `ParseFeedIndex` at line 404+: accepts `schemaVersion` 1 and 2 (after v2-client slice), rejects > 2.
- Constants: `MaxIndexSizeBytes = 2 * 1024 * 1024` (line 18), `MaxPackageSizeBytes = 10 * 1024 * 1024` (line 19).
- Response handling: no ETag/Last-Modified reading, no 304 handling, no caching in client today.

### Feed URL and Preferences (src/CLIHub.Core/Plugins/FeedUrlProvider.cs, IPreferencesStore)

- `FeedUrlProvider.DefaultFeedUrl`: Points to the v2 index URL (after v2-client slice).
- `IPreferencesStore.GetOrDefault("PluginFeedUrl", default)`: Returns null (use default), `""` (disabled), or custom URL.
- Timeout: read from preferences via `IPreferencesStore` in the client constructor (line 47-50 in client).

### Registry (src/CLIHub.Core/Plugins/PluginRegistryRepository.cs, IPluginRegistry.cs)

- `PluginRegistryDocument`: Persistent document with `plugins` map (id → `PluginRegistryEntry`).
- `PluginRegistryEntry`: `{ origin, contentHash, logoHash, installedVersion, enabled, installedAt }` (after consolidation slice).
- Atomic write via `AtomicFileWriter` (line 52 in repository).
- Location: `%APPDATA%\CLIHub\registry.json` (per `AppPaths.RegistryFile` at src/CLIHub.Core/Infrastructure/FileSystem/AppPaths.cs:56).

### Cache Directory (AppPaths.cs)

- `CacheDirectory` constant at line 23: `"cache"`.
- `CacheDirectory` property at line 45: `%APPDATA%\CLIHub\cache`.
- `LogosCacheFile` at line 68: `%APPDATA%\CLIHub\cache\logos.json` (persistent JSON state for logo cache).

**Plan:** Add `FeedCacheFile` and `FeedSyncMetadataFile` to `AppPaths.cs`:
- `FeedCacheFile`: `%APPDATA%\CLIHub\cache\feed-index.json` (raw index bytes, max 2 MB).
- `FeedSyncMetadataFile`: `%APPDATA%\CLIHub\cache\feed-sync.json` (ETag, Last-Modified, timestamp).

### Domain Event Bus (src/CLIHub.Core/Events/DomainEventBus.cs)

**File:lines:**
- 9: `DomainEventBus` implements `IDomainEventPublisher, IDomainEventSubscriber`.
- 24–50: `Publish<TEvent>`: synchronous dispatch, handlers copied to list before iteration (line 36), exceptions caught and logged (line 42–48), other handlers continue.
- 53+: `Subscribe<TEvent>`: returns `IDisposable` that removes the handler when disposed.

**Adoption rules** (from docs/architecture/domain-events.md):
- Publish after state is consistent (line 49: "Publish after persistence, after the UI is updated, or after the external process reports success").
- Subscribers are idempotent-safe, must not throw, must not change behavior (lines 54–56).
- Exceptions logged at Error level (line 44–45 in bus).

**Existing events:**
- `ProjectSelected` (published from `ProjectService.SetCurrentProject` at line 269).
- `AgentLaunched` (published from `AgentCommandService.LaunchInteractiveCommandAsync` at line 76).

### Startup Sequence (src/CLIHub/Startup/ApplicationBootstrapper.cs, docs/architecture/startup.md)

**File:lines:**
- 53: `startupStateLoader.Load()` (plugin seeding + preferences load).
- 55: `this.session.Start(context)` (UI creation).
- 71: `this.optionalStartup.RunOptionalStartup(...)` (release notes, update check).
- 73: `this.projectScanner.Start()` (project scanning background service).

**Current flow:** Plugin initialization happens in `StartupStateLoader` (before line 53 returns); optional operations run at lines 71; project scanner starts at line 73.

**Plan:** Add sync service start between lines 71 and 73 (after optional startup, after UI is ready, before project scanner — or anywhere in the best-effort region). Sync service will start its background task without awaiting.

### Application Shutdown (src/CLIHub/Startup/ApplicationHost.cs, docs/architecture/startup.md:74-94)

**Sequence:**
1. Logs shutdown.
2. Disposes `ApplicationSession` (line 79 in docs).
3. Cancels tracked application operations and waits with timeout (line 80).
4. Disposes service provider (line 81), which disposes singletons (sync service disposal happens here).

**Plan:** Sync service must be registered as a singleton and implement `IDisposable` or `IAsyncDisposable`. Shutdown calls `StopAsync(timeout)` via `IApplicationOperationLifetime` to cancel the background timer; service disposal at step 4 can clean up any remaining state.

### Agent Management ViewModel (src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs)

**File:lines with relevant context:**
- 111: `this.catalog.PluginsChanged += this.OnPluginsChanged;` (subscribes to plugin catalog change event).
- 583–590: `OnPluginsChanged(object? sender, EventArgs e)` handler (refreshes agent cards).
- 1180–1199: `ReloadCatalog()` and related methods (refresh agent list on user action).

**Plan:** Add subscription to `PluginFeedUpdated` event in constructor (similar to `PluginsChanged` subscription), and call `RefreshAgentCardBadges()` or similar in the event handler.

### Browse Plugins ViewModel (src/CLIHub/ViewModels/Agents/BrowsePluginsDialogViewModel.cs)

**File:lines:**
- 25–50: Constructor with `IPluginFeedClient`, `IApplicationOperationLifetime`, etc.
- Search for `LoadFeedAsync` or `LoadAsync` method to see where the feed is fetched.

**Plan:** Modify to check `result.FromCache` flag and show a stale-data message if true.

### ProjectScanningService Pattern (src/CLIHub.Core/Scanning/ProjectScanningService.cs)

**File:lines:**
- 16–72: Constructor showing the pattern: multiple dependencies, `ILoggingContextProvider`, `ILogger`, time source.
- 106–120: `Start()` method (initializes, guards against double-start).
- 150+: `Dispose()` method (cleanup).

**Key pattern for sync service:**
- Use `IApplicationOperationLifetime.RunAsync(name, async (cts) => { ... })` to run the background operation.
- Timer for interval (can use `System.Timers.Timer` or `PeriodicTimer` from .NET 8+).
- Cancellation-aware: pass the provided `CancellationToken` to the timer and any HTTP calls.
- Idempotent stop: track `started` and `disposed` flags, guard with a lock.

## Decisions

### D1: Conditional Request Strategy (ETag vs Last-Modified)

**Decision:** Store and prefer ETag; fall back to Last-Modified if ETag is absent.

**Rationale:**
- ETag is more reliable for detecting changes (it can incorporate multiple server state factors).
- Last-Modified is a fallback for servers that don't generate ETags or for cache-invalidation strategies that rely on timestamps.
- Both headers are optional in HTTP; we handle either or neither (tolerate servers that provide neither).

**Implementation:**
- Read the `ETag` header from the response; if present, store it in `feed-sync.json` as the primary sync key.
- If no ETag, read `Last-Modified`; store as secondary sync key (or convert to `If-Modified-Since`).
- On the next fetch, send `If-None-Match: <etag>` if we have an ETag, else `If-Modified-Since: <date>` if we have a Last-Modified.
- A 304 response means "not modified" — use the cached index without parsing or writing.
- Store the sync metadata (ETag, Last-Modified, timestamp) in a separate file to avoid updating the cache file on 304.

### D2: Cache File Locations and Naming

**Decision:** Two separate files in `%APPDATA%\CLIHub\cache\`:
- `feed-index.json`: The raw index JSON (copied directly from the HTTP response body).
- `feed-sync.json`: Metadata { etag, lastModified, timestamp, indexHash } for sync tracking and change detection.

**Rationale:**
- Separation keeps sync metadata independent of the index content (a 304 response doesn't touch the index file).
- `feed-sync.json` can be small and simple (JSON record), making atomic writes fast and reliable.
- `feed-index.json` can be large (up to 2 MB), but it's only written on a successful fresh fetch (not 304).
- File names are clear and consistent with `logos.json` naming in the cache directory.

**Corrupt-cache handling:**
- If either file is corrupt (e.g., truncated JSON), delete it with a logged warning.
- If `feed-sync.json` is missing or corrupt, treat as "no prior sync" (fetch unconditionally).
- If `feed-index.json` is missing but `feed-sync.json` exists, delete the sync file (they're out of sync).
- If both are missing, treat as "first run" (fetch unconditionally, cache result).

### D3: Update Badge Chip Precedence (Agent Update vs Plugin Update)

**Decision:** Reuse the existing `UpdateAvailable` chip logic from agent version updates. When both agent and plugin updates are available:
- The agent-update chip takes the slot (as today for agent-update-available).
- Plugin updates are visible in the agent's version info or a secondary indicator (investigate existing `AgentCardItem` structure; see D6).

**Rationale:**
- The chip slot is occupied by one "most important" update; prioritizing agent updates is consistent with today's behavior.
- Plugin updates are less critical than agent CLI version mismatches, so the agent update takes precedence.
- No new UI elements are introduced; the existing card layout is preserved.

**Implementation:** Modify the `UpdateAvailable` derivation in `AgentCardItem` or `AgentManagementViewModel` to check both:
  1. Agent version update available (existing logic).
  2. Installed plugin version < feed entry version AND compatibility gating allows the feed version.
  
  If both are true, agent update takes the chip. If only plugin update is true, show the plugin-update chip. If neither, show the version chip as today.

### D4: Interval Mechanics (Fixed vs Preference)

**Decision:** Fixed default interval (1 hour) in this slice; deferred preference-based interval to a later slice.

**Rationale:**
- A fixed interval is simple to implement and reason about.
- 1 hour is a reasonable balance: frequent enough for active developers, infrequent enough to avoid constant network noise.
- Adding a preference requires a Settings page entry, and this slice is focused on core sync + event mechanics.
- Future slice can introduce a preference without breaking the current implementation (comment the interval as a future constant to extract).

**Implementation:**
- Constant in the service: `private static readonly TimeSpan SyncInterval = TimeSpan.FromHours(1);`.
- Use `PeriodicTimer` (or `System.Timers.Timer` for .NET 10 compat) to trigger the refresh on a 1-hour cadence.
- On startup, do an immediate sync, then schedule the next one 1 hour later (not immediately after the first one completes).

### D5: Sync Metadata Placement

**Decision:** Sync metadata (`ETag`, `Last-Modified`, `indexHash`) lives in a separate `feed-sync.json` file, not in the registry document.

**Rationale:**
- The registry is already the single local source of truth for plugin state and is read/written by multiple parties (installer, seeder, catalog, etc.).
- Feed sync metadata is specific to the background-sync service and is not consulted by other components.
- Keeping it separate avoids coupling the registry schema to sync concerns and simplifies concurrent access.
- The registry is already atomic-written frequently; adding feed-sync metadata to it would increase the scope of every registry write.

**Alternative rejected:** Embed sync metadata in the registry document under a `FeedSync` section (e.g., `{ etag, lastModified, timestamp }`). This was rejected because it couples two independent concerns (plugin state vs feed sync tracking) and makes registry writes heavier.

### D6: Update Badge Visibility and No New UI

**Decision:** The "update available" indicator reuses the existing chip on the agent card (no new badge, no new tooltip line).

**Rationale:**
- The existing `UpdateAvailable` pattern (replacing the version chip with an "Update available" chip) already signals that something needs attention.
- Extending it to plugin updates preserves the card layout and interaction model.
- No new tooltips or badges required; the existing agent-version tooltip continues to show agent update info; plugin updates are inferred from the chip state and the feed load.

**Limitation:** Users cannot distinguish "agent update available" from "plugin update available" at a glance. The Browse Plugins dialog will still clarify which plugins have updates. This is acceptable because:
- Plugin updates are typically less critical than agent updates.
- The card chip signals "something is updated"; clicking Browse reveals the details.

**Future enhancement:** A future slice could add a plugin-specific indicator (e.g., a small icon or badge on the card), but this is deferred to keep this slice focused.

### D7: Change Detection Semantics

**Decision:** Compare plugin feed indices by the set of `{ id, version, compatibility }` tuples.

**Rationale:**
- This tuple is sufficient to detect meaningful changes: new plugins, removed plugins, version updates, and compatibility changes.
- It matches the shape used by the update-detection logic (`PluginVersionComparator`, compatibility gating in `BrowsePluginsDialogViewModel`).
- We do not track logo or description changes (they don't affect compatibility or updates).
- We do not track order changes (plugins can be reordered without affecting functionality).

**Implementation:**
- Compute `indexHash = SHA256(sorted JSON of { id, version, compatibility } for each plugin)`.
- Store `indexHash` in `feed-sync.json`.
- On the next sync, if the new fetch is not 304, compute the new hash.
- If `newHash != storedHash`, the feed changed; publish the event. If `newHash == storedHash`, the feed is identical; do not publish.

### D8: Event Payload (Hash vs Full Index)

**Decision:** Event carries the index hash (SHA-256 of the comparison key), not the full parsed index.

**Rationale:**
- The hash is sufficient for subscribers to know "the feed changed" and re-load it if needed.
- Subscribers who care about the details will fetch fresh via the client (same path as Browse does).
- Passing the full index would be wasteful (subscribers don't need it) and would couple the event to the index structure.
- The hash is stable and identifies the state uniquely.

**Implementation:**
- `PluginFeedUpdatedEvent` record: `public record PluginFeedUpdatedEvent(string IndexHash) : IDomainEvent;`
- Subscribers (e.g., `AgentManagementViewModel`) receive the hash and decide what to do (typically: refresh agent cards without re-fetching the index immediately — the next Browse or UI update will fetch fresh if needed).

### D9: Browse Fallback Message Wording

**Decision:** "Using cached plugin list (last updated {timestamp}). Check your internet connection to refresh."

**Rationale:**
- Clearly states that the data is stale and from cache.
- Suggests a user action (check internet) without being alarming.
- Shows the timestamp so users know how old the data is.
- Non-modal; inline message in the Browse dialog, not a dialog or error.

**Implementation:**
- Add a `FromCache` flag to `PluginFeedResult`.
- In `BrowsePluginsDialogViewModel.LoadAsync`, after a successful load, check `result.FromCache`.
- If true, set a `StatusMessage` or similar to the cache message.

## Alternatives Rejected

### A1: Embed sync metadata in the registry (rejected in favor of separate feed-sync.json)

**Why:** Would couple feed concerns to plugin-state concerns and increase registry-write overhead. Registry is already heavily used; feeding sync metadata into it adds unnecessary churn.

### A2: Always re-fetch on every refresh, store ETag but never use it (rejected)

**Why:** Defeats the purpose of conditional requests. We want to reduce network usage and server load. Without using ETags/Last-Modified, every fetch downloads the full index even if unchanged.

### A3: Cache the parsed index as PluginFeedIndex objects (rejected in favor of raw bytes)

**Why:** Raw bytes are simpler to cache (no serialization complexity) and allow re-parsing if needed. Storing parsed objects would couple the cache format to the `PluginFeedIndex` structure, making future schema changes harder.

### A4: Skip stale cache on Browse error, show hard error instead (rejected in favor of fallback)

**Why:** Improves resilience; a user on an unreliable network can still see the last-known plugins even if they can't reach the feed right now. This is better UX than "Feed unavailable" with no data.

### A5: Publish an event on every sync (including 304 and errors) (rejected in favor of event only on change)

**Why:** Noisy; subscribers would refresh unnecessarily. We only publish when the feed actually changed, reducing unnecessary work.

### A6: Use a preference for the interval from day one (rejected in favor of fixed default with comment for future preference)

**Why:** Adds Settings UI complexity and exposes an implementation detail. A fixed default is sufficient; a preference can be added later as a cosmetic change (no code restructuring needed).

## Open Questions for Implementation

### OQ-1: Subscription Order in AgentManagementViewModel

Should the `PluginFeedUpdated` subscription be added in the constructor alongside `PluginsChanged`, or should it be registered as a separate method? The existing pattern uses constructor subscription (line 111 in the ViewModel), so we'll follow that.

### OQ-2: Refresh Throttling

If a feed sync and a user-triggered refresh happen close together, should we throttle the UI refresh (e.g., coalesce multiple refresh calls into one)? The current `PluginsChanged` event-driven refresh should handle this, but we should verify that rapid feed updates don't cause excessive card redraws. If they do, consider adding a short debounce (100-200 ms) to `RefreshAgentCardBadges`.

### OQ-3: Handling Feed Disabled

When the feed is disabled (`pluginFeedUrl == ""`), should the sync service skip refreshes or stop entirely? Current plan: Stop the service if the feed URL is empty; on a future preference change that re-enables the feed, restart the service. This requires the sync service to listen to preference changes (add a subscription in the constructor).

Actually, simpler alternative: The sync service checks the feed URL on every refresh and skips if it's disabled. Restarts on next interval when the feed is re-enabled. This avoids the need for a preference-change subscription and is more resilient.

### OQ-4: Error Logging Level

Should a failed refresh log at Warning or Info? Current plan: Warning, because a failed fetch might indicate a network issue the user should be aware of. But if refreshes happen every hour, hourly warnings might be noisy. Alternative: Log at Info, only promote to Warning if multiple consecutive refreshes fail. Deferred to implementation; start with Warning and adjust if feedback indicates noise.

### OQ-5: Cache Metadata File Format

Should `feed-sync.json` be a simple flat record or a nested structure? Current plan: Flat record `{ etag, lastModified, timestamp, indexHash }` for simplicity. Alternative: Nested `{ fetch: { etag, lastModified, timestamp }, change: { hash } }` for semantic grouping. The flat structure is simpler and sufficient for now.

## Spec Coverage and Deltas

### plugin-feed (modified)

**Existing scenarios preserved (names and substance):**
- "Valid index fetched successfully"
- "Index schema version unsupported" (bound updated from 1 to 2, error string unchanged)
- "Index missing required field"
- "Index contains unknown fields"
- "Index entry with invalid fields skipped"
- "Duplicate plugin IDs in index"
- "Index exceeds size limit"
- "HTTP URL rejected"
- "Redirect to HTTP rejected"
- "Network timeout"
- "Network unavailable" (new note: cache fallback applies)
- "Schema version 2 index parsed"
- "Descriptor-only v2 entry parsed"
- "Non-stable channel entry skipped"
- "Malformed compatibility range entry skipped"
- "Invalid inline descriptor entry skipped"

**New scenarios:**
- "Conditional request with ETag returns 304" — behavior, no re-parse, success.
- "Conditional request with Last-Modified returns 304" — behavior.
- "Fetch fails, cached index used" — fallback to cache, `FromCache: true`, message shown.
- "Cached index exceeds 2 MB, not used" — corrupt or oversized cache is deleted.

### plugin-feed-sync (new capability)

**Scenarios:**
- "Background feed refresh on startup"
- "Background feed refresh on interval"
- "Refresh cancellation on shutdown"
- "ETag sent on subsequent fetch"
- "Last-Modified sent if ETag absent"
- "304 Not Modified handled"
- "Failed fetch falls back to cache"
- "Corrupt cache deleted"
- "Change detected, domain event published"
- "No change, event not published"
- "304, event not published"
- "Fetch error, event not published"

### agent-update-detection or agent-ui (likely modified, TBD)

Investigate whether the existing `UpdateAvailable` chip is already covered by `agent-update-detection` spec. If so, add a delta scenario "Plugin update available on installed plugin" to that spec. If not, investigate whether a new scenario belongs in `agent-ui` or should be a separate delta. Design D3 and D6 suggest we reuse the existing chip, so likely just one delta scenario added to whichever spec covers the chip.

## Guarantees Checklist

✓ HTTPS-only + redirect rules + hop cap on every fetch (reuse `SendFollowingRedirectsAsync`)  
✓ 2 MB index bound (existing check in client; cached index also checked)  
✓ Startup not blocked (async via `IApplicationOperationLifetime.RunAsync`)  
✓ Shutdown cancels cleanly (service.StopAsync called from shutdown sequence)  
✓ Existing user-facing strings preserved (new cache-fallback message only)  
✓ Tolerant cache behavior (missing/corrupt files deleted with warning, never throw)  
✓ Core WPF-free (sync service in Core; subscription/UI in ViewModel)  
✓ Atomic writes (use existing `AtomicFileWriter` for metadata, or tolerate overwrites for small JSON)  
✓ Silent failures logged (failures logged at Warning level, no exceptions thrown)  
✓ Domain event adoption rules followed (synchronous dispatch, exceptions caught/logged, subscribers idempotent)
