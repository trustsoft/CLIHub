# Plugin feed background sync with conditional requests, last-good cache, domain event, and update badges

## Why

The plugin feed is fetched only when the Browse Plugins dialog opens (`BrowsePluginsDialogViewModel.LoadAsync`), so update availability is invisible to users until they navigate to the dialog. Three limitations drive this slice (roadmap slice 3 of ADR-0015 registry v2, documented in `docs/backlog.md:182-191`, following the archived slices `2026-11-14-plugin-feed-v2-client` and `2026-11-14-plugin-registry-consolidation`):

1. Every fetch downloads the entire index regardless of whether it has changed. HTTP conditional requests (ETag + If-None-Match, or Last-Modified + If-Modified-Since) reduce unnecessary data transfer and server load.
2. A failed fetch today results in a hard error in Browse; a cached "last good" index lets Browse degrade to stale-but-usable rather than broken.
3. The UI surface has no way to signal "updates available" outside the Browse dialog; a domain event enables `AgentManagementViewModel` to refresh the badge state when the feed changes, and the agent card gains an "update available" chip (following the existing `UpdateAvailable` pattern for agent versions).

The registry consolidation slice (2026-11-14) established the registry as the single local source of truth and enabled recording `installedVersion` and `enabled` state in one place. This slice adds conditional-fetch metadata and uses the feed-sync event to keep the update badge in sync with the background fetch.

## What Changes

Five items, S-A–S-E. None weakens a security guarantee; the full guarantee inventory is in `design.md`.

- **S-A — Conditional fetch + last-good cache.** `PluginFeedClient.GetFeedAsync` persists the response's `ETag` header (or `Last-Modified` if ETag is absent), stores the raw index bytes in `%APPDATA%\CLIHub\cache\feed-index.json`, and on the next refresh sends `If-None-Match` (or `If-Modified-Since`). A 304 Not Modified response reports success without parsing or writing. A failed fetch (network error, HTTP error, parse error) falls back to the cached index with a `FromCache` flag so the caller can warn the user. Corrupt cache files are deleted with a logged warning; missing cache is tolerated. The sync metadata (ETag, Last-Modified, timestamp) lives in a separate `%APPDATA%\CLIHub\cache\feed-sync.json` file (simple, atomic-writable, independent of the index state). Both cache files are guarded by the existing `2 MB` index size limit: a cached index larger than 2 MB is never used.

- **S-B — Background sync service.** A new `IPluginFeedSyncService` in Core, instantiated at application start (after plugin initialization completes) and disposed at shutdown, refreshes the feed on startup and then on a fixed interval (default 1 hour; interval is a future preference — see design D4). The service runs asynchronously via `IApplicationOperationLifetime.RunAsync`, cancellation-aware, and does not block startup or UI. Silent failures are logged at Warning; successful refresh (including 304) publishes a domain event if the index changed.

- **S-C — Change detection + domain event.** After a successful refresh that is not 304, `PluginFeedSyncService` compares the new index against the last-known one by comparing the set of {id, version, compatibility} tuples (the shape used by compatibility gating and update detection; see design D7). If the sets differ, the service publishes a `PluginFeedUpdated(indexHash: string)` domain event carrying the SHA-256 hash of the index. No event is published if the index is unchanged, on 304, or on error. Domain event subscribers receive the hash and can decide whether to refresh their state (see S-D).

- **S-D — Update surface on agent cards.** `AgentManagementViewModel` subscribes to `PluginFeedUpdated` and refreshes the badge state (via `RefreshAgentCardBadges()`) whenever the feed changes. The existing `UpdateAvailable` chip logic applies: if an installed plugin (or its backing feed entry) has a newer version *and* the app can install it, the card shows an "Update available" chip. The case where both agent and plugin updates are available is handled by the existing chip-precedence logic (agent updates take the chip slot, but plugin updates are represented in the agent's update check; see design D3). No new dialogs, no new preferences, no breaking UI changes.

- **S-E — Browse fallback UX.** `BrowsePluginsDialogViewModel.LoadAsync` prefers a fresh fetch but on network failure falls back to the cached index (if available) with an inline message: "Using cached plugin list (last updated {time}). Check your internet connection to refresh." The message is only shown on fallback, not on successful 304 or successful fetch. If both fetch and cache fail, the existing error string is used unchanged.

Tests: new `PluginFeedSyncServiceTests` covering refresh, conditional logic, change detection, event publishing, and cancellation; modified `PluginFeedClientTests` to cover conditional headers and cache fallback; modified `AgentManagementViewModelTests` to cover the subscription and card refresh; modified `BrowsePluginsDialogViewModelTests` to cover the cache-fallback message. All existing feed-fetch tests pass with fixture updates only.

## Capabilities

### Modified Capability: `plugin-feed`

Delta in `specs/plugin-feed/spec.md` (this change):

- **MODIFIED "Fetch and parse feed index"** — new scenarios pinning conditional request headers (ETag/If-None-Match, Last-Modified/If-Modified-Since), 304 Not Modified response, and cache fallback on fetch failure. All 16 original scenario names preserved; "Network unavailable" gains a note about cache fallback; new "Conditional request returns 304" and "Fetch fails, cached index used" scenarios.
- **MODIFIED "Display available plugins with status"** — existing scenarios unchanged; new scenario pinning that a stale cached index shows a "last updated {time}" note to the user.
- No changes to package download, logo download, or checksum verification; no spec delta for `plugin-installation` or `plugin-seeding`.

### Added Capability: `plugin-feed-sync`

Delta in `specs/plugin-feed-sync/spec.md` (this change):

- **ADDED "Background feed refresh"** — service lifecycle (start on application startup after plugin initialization, stop on shutdown), refresh on startup + fixed interval, cancellation-aware, non-blocking.
- **ADDED "Conditional fetch metadata"** — ETag/Last-Modified storage and If-None-Match/If-Modified-Since usage; 304 handling.
- **ADDED "Last-good index cache"** — storage, fallback on error, corrupt-cache recovery.
- **ADDED "Change detection and domain event"** — comparison semantics, event publishing conditions, event payload.

### Modified Capability: `agent-ui` or `agent-update-detection`

Scope TBD in design (see D3, D6): the existing `UpdateAvailable` chip for agent versions is reused or extended for plugin versions. Verify whether the existing spec or a new delta is needed.

## Impact

**Modified code:**

- `src/CLIHub.Core/Plugins/PluginFeedClient.cs` — conditional request headers, cache fallback, `FromCache` flag in result.
- `src/CLIHub.Core/Plugins/PluginFeedResult.cs` — `FromCache` flag added; new `PluginFeedUpdatedEvent` record.
- `src/CLIHub.Core/Plugins/IPluginFeedClient.cs` — `FromCache` flag on result.
- `src/CLIHub.Core/Plugins/IPluginFeedSyncService.cs`, `PluginFeedSyncService.cs` — new service; published into Core.
- `src/CLIHub.Core/Events/PluginFeedUpdatedEvent.cs` — new domain event record.
- `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs` — register the sync service.
- `src/CLIHub.Core/Infrastructure/FileSystem/AppPaths.cs` — `FeedCacheFile` and `FeedSyncMetadataFile` added.
- `src/CLIHub/Startup/ApplicationBootstrapper.cs` — integrate sync service start after plugin initialization.
- `src/CLIHub/Startup/ApplicationHost.cs` — ensure sync service disposal at shutdown.
- `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` — subscribe to `PluginFeedUpdated`, refresh badge state.
- `src/CLIHub/ViewModels/Agents/BrowsePluginsDialogViewModel.cs` — cache fallback message, `FromCache` handling.
- Tests: `tests/CLIHub.Core.Tests/Plugins/PluginFeedSyncServiceTests.cs` (new), `PluginFeedClientTests.cs` (conditional + cache), `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Plugins.cs` (event subscription), `BrowsePluginsDialogViewModelTests.cs` (fallback).
- Docs: `docs/architecture/plugins.md` (feed sync section, conditional headers, cache, event), `docs/project-context.md` (runtime facts).

**Unchanged:** the registry document schema, the registry adoption logic, plugin descriptor format, install/seeding flows, all user-facing feed error strings except the new cache-fallback message, HTTPS/redirect/size/checksum rules, configuration and preferences (no new config fields in this slice — interval is a future preference), the Launch/Browse/Detect/Install dialogs, `CLIHub.Core` WPF-free constraint.

**Verification:** `dotnet build CLIHub.sln` + `dotnet test CLIHub.sln` after every stage; `openspec validate --all`; `graphify update .` at the end; manual smoke test of a background refresh (set interval to 5 seconds in a test build, launch app, verify event fires and card badges update).

## Non-Goals

- No preference for the interval in this slice; a fixed default (1 hour) is sufficient. Interval preference deferred with a comment on future work.
- No retry/backoff on transient errors; the next interval refresh will retry.
- No index signing or ETag signature verification (reserved for the signing slice, ADR-0015).
- No new settings UI; the interval is not exposed in this slice.
- No changes to the feed format, the registry schema, or plugin install/seeding behavior.
- No breaking changes to the Browse Plugins UX; the cache-fallback message is a new, optional element.

## Dependencies

- Base: HEAD at commit 7b95c0e (clean working tree). No other active changes (`openspec list` reports none). No file overlap with concurrent work.
- Follows: archived `2026-11-14-plugin-feed-v2-client` and `2026-11-14-plugin-registry-consolidation`.
- Consumes, does not modify: `PluginFeedClient` (adds conditional + cache fallback, no existing path changes), `PluginRegistry` (reads `installedVersion` to check for updates, no modifications), `DomainEventBus` + `IDomainEventSubscriber` (existing adoption rules apply).
- Blocks: later registry v2 slices (index signing) can proceed; no other changes are blocked.

## Risk

- **Silent failures hide network issues (S-B):** failed refreshes are logged but do not bubble to the UI. Mitigated by the warning-level log and the cache-fallback message in Browse, which surfaces stale state when the network is down.
- **Event subscriber coupling (S-C):** `AgentManagementViewModel` assumes the event bus is wired correctly and that the event carries the intended hash. Mitigated by the domain-event adoption rules (subscribers are idempotent-safe, exceptions caught and logged) and tests covering both event publication and subscription.
- **Cache corruption (S-A):** a corrupt `feed-sync.json` or `feed-index.json` could degrade gracefully but lose metadata. Mitigated by tolerant reads (corrupt files are deleted with a warning), sync metadata separation (a corrupt sync file does not corrupt the index cache), and a corrupt index cache that is smaller than 2 MB is never used.
- **Interval too frequent or too rare (S-B):** a default of 1 hour is a guess; if users expect faster updates, they open Browse to force a refresh. Mitigated by making the interval configurable in the next slice.

## Non-negotiable Guarantees

1. **HTTPS-only + redirect rules + hop cap:** Every conditional-request fetch uses the same `SendFollowingRedirectsAsync` GET with manual HTTPS enforcement and 5-hop cap as today.
2. **2 MB index bound:** Cached index is never used if it exceeds 2 MB; live index load is still bounded by the existing 2 MB check.
3. **Startup not blocked:** The background sync service runs via `IApplicationOperationLifetime.RunAsync` and does not await on the initial fetch; the app is fully interactive before the first background refresh completes.
4. **Shutdown cancels cleanly:** `ApplicationHost.Shutdown` calls `IApplicationOperationLifetime.StopAsync(timeout)`, which cancels the sync timer and waits for the in-flight refresh with the configured timeout (existing behavior from the shutdown sequence, `docs/architecture/startup.md:74-94`).
5. **Existing user-facing strings preserved:** The Browse Plugins error path uses existing error strings; the cache-fallback message is new. No rewording of existing feed errors.
6. **Tolerant cache behavior:** Missing cache is tolerated (first run, or cache deleted by user); corrupt cache files are deleted with a logged warning and never crash the app.
7. **Core WPF-free:** All sync logic is in Core; the subscription and UI updates are in the ViewModel.
