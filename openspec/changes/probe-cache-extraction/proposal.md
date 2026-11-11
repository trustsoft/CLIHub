## Why

Master duplicates the same TTL-cache + in-flight-dedup pattern in two Core services,
`AgentVersionService` and `AgentUpdateAvailabilityService` (each carries its own `cache` /
`inFlight` dictionaries, inline dedup block, and `CacheEntry` record), and the duplicated
code contains a real concurrency bug: the `finally` of an awaited probe removes the in-flight
entry **by key** (`inFlight.TryRemove(plugin.Id, out _)`). When a caller's `finally` runs
delayed (its continuation is queued behind other work) and a **newer** probe for the same
plugin id has already started, the delayed `finally` removes the newer probe's in-flight
entry, so the next concurrent caller starts a duplicate probe. That violates requirements the
specs already state: "concurrent requests for the same agent version share one in-flight probe
rather than starting duplicate processes"
(`openspec/specs/agent-version/spec.md:93`) and "Concurrent checks for the same agent SHALL
share one in-flight request" (`openspec/specs/agent-update-detection/spec.md:115`).

The refactoring already exists on branch `backup-before-reset` (commits `b8104ad` extraction,
`ae9cf02` compare-and-remove fix, `3612a55` test hardening) and was verified applicable to
master @ `8ca4c3f` (`backup-port-04-applicable-probe-cache.md`, background only). This change
ports it as one unit: extraction + correctness fix + tests.

## What Changes

1. **New `src/CLIHub.Core/Agents/ProbeCache.cs`** — internal generic
   `ProbeCache<TValue>`: a per-key TTL entry cache plus a `Lazy<Task<T>>` in-flight map with
   `GetOrProbeAsync(key, ttl, probe, cancellationToken)`, `Clear()`, and `Remove(key)`. The
   `finally` removes only its own in-flight entry via
   `TryRemove(KeyValuePair<string, Lazy<Task<TValue>>>)` — the `ae9cf02` fix — never by key.
   Ported from `git show backup-before-reset:src/CLIHub.Core/Agents/ProbeCache.cs` (final
   state, fix included), minus the backup branch's file-header comments.
2. **Rewire the two services** — `AgentVersionService` and `AgentUpdateAvailabilityService`
   delegate to `ProbeCache` and delete their private `cache`/`inFlight` dictionaries, inline
   dedup blocks, and `CacheEntry` records. Constructor signatures, DI registration
   (`ServiceCollectionExtensions.cs:127,140`), TTL resolution
   (`AgentProbePolicy.ResolveTtl`), and all public behavior stay unchanged.
3. **Port tests** — `tests/CLIHub.Core.Tests/Agents/ProbeCacheTests.cs` (10 tests, ~264
   lines, including the race regression test
   `GetOrProbe_HeldCallerContinuationAfterNewerProbePublished_DoesNotEvictNewerInFlightProbe`)
   and the fake `tests/CLIHub.Core.Tests/Fakes/QueueingSynchronizationContext.cs` (absent on
   master). The tests use master's existing `Fakes/FakeTimeProvider`; no new package.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. The coalescing behavior this fix enforces is already contractually pinned by
`openspec/specs/agent-version/spec.md` ("Concurrent version probes are coalesced", lines
91-101) and `openspec/specs/agent-update-detection/spec.md` ("Availability results are cached
per agent", lines 113-135, and the forced-check scenario at line 220). The fix makes the
implementation conform to those existing requirements under a race window; no requirement or
scenario text changes. The change therefore opts out of spec deltas (`skip_specs: true` in
`.openspec.yaml`).

## Impact

- **Code**: 1 new Core file (`ProbeCache.cs`, ~100 lines), 2 Core services rewired (net
  about -80 lines of duplicated concurrency-sensitive code), 2 new test files.
- **Behavior**: one latent defect fixed — a delayed `finally` can no longer evict a newer
  probe's in-flight entry, so duplicate concurrent version commands / update-source requests
  for one agent can no longer start through that window. All documented behavior (TTL
  caching, per-agent invalidation, coalescing, caller-scoped cancellation, degrade paths) is
  preserved; existing service tests stay green unchanged.
- **Specs**: none modified (see above).
- **Dependencies**: none on master changes; the port source is `backup-before-reset`
  `b8104ad` + `ae9cf02` + `3612a55` (read-only `git show`; no merge, no cherry-pick).
- **Build**: no project-file, package, or DI changes.

## Non-Goals

- Do not migrate `AgentDetectionService` to `ProbeCache`: it has a TTL cache but no in-flight
  dedup (file-system checks, no process spawning); migrating it would *add* dedup behavior
  that does not exist today.
- Do not change any public API, constructor signature, DI registration, TTL semantics, probe
  timeout behavior, or preference shape.
- Do not port the backup branch's `AgentProbeDefaults` (does not exist on master); the
  services keep `AgentProbePolicy.ResolveTtl`.
- Do not add spec deltas or modify `openspec/specs/**`.
- Do not edit `docs/backlog.md` (no probe-cache entry exists there) or `CHANGELOG.md`.
- Do not touch the untracked `backup-port-*.md` analysis files in the repository root.
- No commits, branches, or history operations as part of preparing this change.

## Dependencies

None — standalone. Origin: branch `backup-before-reset`, commits `b8104ad` (extraction),
`ae9cf02` (fix), `3612a55` (test hardening), analyzed in
`backup-port-04-applicable-probe-cache.md`; ported as one unit per that analysis.
