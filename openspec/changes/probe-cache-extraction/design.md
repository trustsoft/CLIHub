## Context

Branch `backup-before-reset` carries a three-commit refactoring of the agent probe services:
`b8104ad` extracts a shared `ProbeCache<TValue>` from the duplicated TTL-cache + in-flight-dedup
code in `AgentVersionService` and `AgentUpdateAvailabilityService`; `ae9cf02` fixes a real race
in the extracted cache (in-flight eviction by key instead of by key+value pair); `3612a55`
hardens the race regression test (wait for the held continuation's `Post` before asserting).
`backup-port-04-applicable-probe-cache.md` verified the port applies to master @ `8ca4c3f`.
This design re-verified every claim against master directly (all file:line references below are
master @ `8ca4c3f` unless prefixed `backup-before-reset:`).

## Goals / Non-Goals

**Goals:**

- Extract the duplicated cache + in-flight dedup into one internal `ProbeCache<TValue>`.
- Carry the `ae9cf02` compare-and-remove fix: a delayed `finally` must remove only its own
  in-flight entry (`TryRemove(KeyValuePair<...>)`), never an entry by key.
- Rewire both probe services as thin clients; delete their private dictionaries, dedup blocks,
  and `CacheEntry` records.
- Port `ProbeCacheTests` (including the race regression test) and the
  `QueueingSynchronizationContext` fake.
- Zero change to documented behavior, public APIs, DI, and specs.

**Non-Goals:**

- Migrating `AgentDetectionService` (TTL cache only, no in-flight dedup — verified: no
  `inFlight`/`Lazy` in `src/CLIHub.Core/Agents/AgentDetectionService.cs`).
- Porting `AgentProbeDefaults`, changing TTL resolution, timeouts, preferences, or specs.
- Touching the untracked `backup-port-*.md` files or any git history.

---

## Current State (verified on master @ `8ca4c3f`)

### `AgentVersionService.cs` (145 lines)

**File:** `src/CLIHub.Core/Agents/AgentVersionService.cs`

- Line 3: `using System.Collections.Concurrent;` — becomes unused after the rewiring.
- Lines 34-35: the duplicated fields:
  ```csharp
  private readonly ConcurrentDictionary<string, CacheEntry> cache = new();
  private readonly ConcurrentDictionary<string, Lazy<Task<string?>>> inFlight = new();
  ```
- Lines 53-78: `GetVersionAsync` with the inline dedup: TTL check 55-59, `inFlight.GetOrAdd`
  with `LazyThreadSafetyMode.ExecutionAndPublication` 61-65, `await lazy.Value.WaitAsync(ct)`
  67-70, and the **buggy finally** 71-77:
  ```csharp
  finally
  {
      if (lazy.IsValueCreated && lazy.Value.IsCompleted)
      {
          this.inFlight.TryRemove(plugin.Id, out _);   // line 75 — removes by key
      }
  }
  ```
- Lines 80-106: `ProbeAndCacheAsync` — writes `this.cache[plugin.Id] = new CacheEntry(...)` at
  lines 85 (no version command) and 104 (after the probe).
- Line 109: `Invalidate() => this.cache.Clear();`.
- Lines 117-121: `InvalidateFor` — `this.cache.TryRemove(plugin.Id, out _)` at line 120, with
  remarks (111-116) documenting that the in-flight map is deliberately left alone.
- Line 123: `private TimeSpan Ttl => AgentProbePolicy.ResolveTtl(this.preferencesStore.Load().AgentProbeTtlMinutes);`
  — **keep as-is**; the backup's `AgentProbeDefaults.TtlFrom` does not exist on master.
- Line 144: `private readonly record struct CacheEntry(string? Value, DateTimeOffset Timestamp);`.
- Constructor 40-50 assigns `this.timeProvider = timeProvider ?? TimeProvider.System;` at
  line 49 — signature unchanged by this change.

### `AgentUpdateAvailabilityService.cs` (196 lines)

**File:** `src/CLIHub.Core/Agents/AgentUpdateAvailabilityService.cs`

- Line 3: `using System.Collections.Concurrent;`.
- Lines 24-25: the same duplicated field pair (`cache` of `CacheEntry`,
  `inFlight` of `Lazy<Task<AgentUpdateAvailability>>`).
- Lines 49-76: `CheckAsync` dedup: `ArgumentNullException.ThrowIfNull(plugin)` at 51, TTL
  check 53-57, `GetOrAdd` 59-63, try/await 65-68, **buggy finally 69-75** with
  `this.inFlight.TryRemove(plugin.Id, out _);` at line 73.
- Line 79: `Invalidate()`.
- Lines 87-91: `InvalidateFor` (`TryRemove` at 90) with remarks 82-86.
- Lines 93-98: `CheckAndCacheAsync` — awaits `ComputeAvailabilityAsync` with
  `ConfigureAwait(false)` and writes the cache entry at line 96. Deleted by the rewiring;
  `ComputeAvailabilityAsync` (100-159) becomes the probe factory and is otherwise untouched.
- Line 193: `Ttl => AgentProbePolicy.ResolveTtl(...)` — keep.
- Line 195: `private readonly record struct CacheEntry(AgentUpdateAvailability Value, DateTimeOffset Timestamp);`.

### The bug being fixed (race mechanics)

Both services remove the in-flight entry **by key** in a `finally` guarded only by
"the Lazy I awaited has completed". Sequence on master:

1. Caller A awaits in-flight probe L1 for plugin `p`.
2. L1 completes. A's continuation (and thus A's `finally`) is not instantaneous — it is
   scheduled, and with a captured `SynchronizationContext` it is queued behind other work.
3. Another caller sharing L1 completes normally and its `finally` removes L1; the completed
   cache entry for `p` is invalidated (`InvalidateFor` / forced check) or expires.
4. Caller C misses and `GetOrAdd` installs a **new** in-flight entry L2 for `p`.
5. A's delayed `finally` finally runs: `lazy.IsValueCreated && lazy.Value.IsCompleted` holds
   for L1, but `inFlight.TryRemove(plugin.Id, out _)` removes **L2** — an entry A never
   awaited.
6. Caller D arrives, `GetOrAdd` finds no entry and starts L3. L2 and L3 now run concurrently
   for the same agent: a duplicate version command process or update-source request, exactly
   what the specs forbid.

The fix (`ae9cf02`): `this.inFlight.TryRemove(new KeyValuePair<string, Lazy<Task<TValue>>>(key, lazy));`
— `ConcurrentDictionary`'s atomic compare-and-remove overload removes the pair only if the
stored value is still the caller's own `Lazy`, so a delayed `finally` is a harmless no-op.

### Spec constraints (why no delta is needed)

- `openspec/specs/agent-version/spec.md:91-101` — Requirement "Concurrent version probes are
  coalesced": SHALL at line 93 ("share one in-flight probe rather than starting duplicate
  processes"), scenarios "Concurrent cache miss" (95) and "Different agents probe
  independently" (99). The race above is a defect **against** this requirement, not a gap in
  it; the fixed behavior is what the requirement already states.
- `openspec/specs/agent-update-detection/spec.md:113-135` — Requirement "Availability results
  are cached per agent": "Concurrent checks for the same agent SHALL share one in-flight
  request" (line 115), scenario "Concurrent checks are coalesced" (133), per-agent
  invalidation scenarios (121-131). Line 220 additionally pins that the availability service's
  "in-flight coalescing ... paths are unchanged".
- `openspec/specs/agent-detection/spec.md:51-83` constrains the *detection* cache (different
  service, not touched — see Non-Goals).

No requirement or scenario text changes, so the change ships `skip_specs: true`.

### DI wiring (no change needed)

`src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:127`
(`services.AddSingleton<IAgentVersionService>(sp => new AgentVersionService(...))`) and
`:140` (`services.AddSingleton<IAgentUpdateAvailabilityService, AgentUpdateAvailabilityService>()`).
Both registrations are type-based and the constructor signatures are untouched, so DI adapts
automatically.

### Test infrastructure (verified)

- `tests/CLIHub.Core.Tests/Fakes/FakeTimeProvider.cs:6-14` — master already has a public
  `FakeTimeProvider` with exactly the API the ported tests use: constructor taking
  `DateTimeOffset` (line 10), `GetUtcNow()` (12), `Advance(TimeSpan)` (14). No new package
  (the test csproj has only coverlet, DI, Test.Sdk, Moq, xunit — lines 14-19).
- No `ProbeCache*.cs` and no `QueueingSynchronizationContext*.cs` exist anywhere under
  `src/` or `tests/` (file search) — both are new files.
- Test conventions match the port: plain `public class` test classes
  (`tests/CLIHub.Core.Tests/Agents/AgentVersionServiceTests.cs:13`), naming
  `MethodOrScenario_Condition_ExpectedResult` (e.g. `AgentVersionServiceTests.cs:122`
  `GetVersion_WithinCacheTtl_ReturnsCachedVersionWithoutRunning`), `Fakes/` for hand-rolled
  fakes. Existing service tests (`AgentVersionServiceTests` TTL/expiry/invalidate at 122, 137,
  155; `AgentUpdateAvailabilityServiceTests` uses `FakeTimeProvider`) pin the service-level
  contract and must stay green without edits.

### Style constraints (verified)

- `.editorconfig:15` `csharp_style_namespace_declarations = file_scoped:warning`;
  `:20` `csharp_using_directive_placement = inside_namespace:warning` (usings after the
  namespace); `:48` private instance fields `lower_camel_case` (no underscore);
  `:106-109` `dotnet_style_qualification_for_*  = true:warning` (`this.`).
- No file-header rule exists, and no master source file carries a header
  (`AgentVersionService.cs:1` and `FakeTimeProvider.cs:1` both start with the namespace) —
  the backup's `// ---` header blocks are dropped in the port.
- XML docs on internal members follow `AgentProbePolicy.cs` (documented internal static
  class); the ported `ProbeCache` keeps its full XML docs including the remarks that explain
  the cancellation and poison-avoidance semantics.

---

## Port Source (read-only references for the implementer)

| Master target | Port from (final state) | Adaptations |
|---|---|---|
| `src/CLIHub.Core/Agents/ProbeCache.cs` (new) | `git show backup-before-reset:src/CLIHub.Core/Agents/ProbeCache.cs` (~102 lines; already contains the `ae9cf02` fix and final XML docs) | Drop the 4-line `// ---` file header. Everything else ports verbatim. |
| `tests/CLIHub.Core.Tests/Agents/ProbeCacheTests.cs` (new) | `git show backup-before-reset:tests/CLIHub.Core.Tests/Agents/ProbeCacheTests.cs` (~264 lines, 10 tests incl. the race regression test with the `3612a55` `FirstPost` wait) | Drop the file header; relies on master's existing `Fakes/FakeTimeProvider` — no change needed. |
| `tests/CLIHub.Core.Tests/Fakes/QueueingSynchronizationContext.cs` (new) | `git show backup-before-reset:tests/CLIHub.Core.Tests/Fakes/QueueingSynchronizationContext.cs` (final state with `FirstPost`) | Drop the file header. |
| `AgentVersionService.cs` rewiring | `git show b8104ad -- src/CLIHub.Core/Agents/AgentVersionService.cs` | Keep master's probe body (`result.Output`/`result.Error`, `LaunchRequest.Create`), `DefaultTtl`/`DefaultProbeTimeout` statics (lines 21-26), and master's `Ttl` property (line 123) — the backup's `AgentProbeDefaults.TtlFrom` line is **not** ported. |
| `AgentUpdateAvailabilityService.cs` rewiring | `git show b8104ad -- src/CLIHub.Core/Agents/AgentUpdateAvailabilityService.cs` | Same TTL note; keep the `InvalidateFor` remarks (82-86) and the null guard (51). |

Commit roles: `b8104ad` = extraction + service rewiring; `ae9cf02` = the compare-and-remove fix
(already merged into the final `ProbeCache.cs` above) + race regression test +
`QueueingSynchronizationContext`; `3612a55` = bounded `FirstPost` wait hardening (already
merged into the final test + fake above). The ported final states already fold `ae9cf02` and
`3612a55` in, so the implementer ports two file states + two rewiring diffs.

---

## Decisions

### Decision 1: Port the merged final state, not three sequential commits

The change ports `ProbeCache.cs` in its final form (with the fix) rather than first replaying
`b8104ad`'s buggy intermediate and then patching it with `ae9cf02`. One atomic change with the
fix baked in is smaller, never leaves the tree carrying the known race, and matches the brief
("as one change"). The three-commit history is referenced in this design instead.
**Rejected:** replaying commit-by-commit — more stages, no review benefit, and an intermediate
state that fails the new regression test.

### Decision 2: `skip_specs: true`

The capabilities that constrain probe caching exist and were verified
(`agent-version:91-101`, `agent-update-detection:113-135`, `:220`), but this change modifies
no requirement: the existing SHALLs already state the corrected coalescing behavior, and the
fix brings the implementation into conformance in a race window. OpenSpec deltas are for
changed contracts, not implementation conformance.
**Rejected:** adding a MODIFIED delta with a new "delayed continuation" scenario to
`agent-version`/`agent-update-detection` — it would restate the existing "Concurrent cache
miss"/"Concurrent checks are coalesced" scenarios at an implementation level (sync-context
queuing is not observable product behavior), and would force renumbered deltas across two
capabilities for zero contract change. Flagged as an open question for the reviewer in case
they prefer the spec strengthened anyway.

### Decision 3: `GetOrProbeAsync` takes `ttl` per call, not in the constructor

Both services resolve the TTL from preferences on **every** call today
(`AgentVersionService.cs:123`, `AgentUpdateAvailabilityService.cs:193`), so a changed
`AgentProbeTtlMinutes` preference applies to the next probe without recreating the service.
The backup design kept the per-call `ttl` parameter for exactly this reason; the port
preserves it. **Rejected:** fixing the TTL at cache construction — would freeze the preference
until restart, a behavior change.

### Decision 4: `ProbeCache<TValue>` stays `internal sealed`

Both consumers are in `CLIHub.Core.Agents`; nothing outside needs it, and the repo direction
is a slimmer Core API surface (see archived `2026-10-04-slim-core-api-surface`). Matches the
visibility of `AgentProbePolicy` (internal static, `AgentProbePolicy.cs:7`). Not listed in any
`InternalsVisibleTo` — tests use the public behavior of the services plus the cache's own
internal accessibility within the test project via... they cannot; see Decision 5.

### Decision 5: Tests construct `ProbeCache` directly via the existing `InternalsVisibleTo`

The ported `ProbeCacheTests` instantiates `new ProbeCache<int>(this.time)`. Verified:
`src/CLIHub.Core/CLIHub.Core.csproj:28-31` already declares
`<InternalsVisibleTo Include="CLIHub.Core.Tests" />` (line 30), which is how the existing
`AgentProbePolicyTests` exercises the `internal static class AgentProbePolicy`
(`AgentProbePolicy.cs:7`). No project-file change is needed; the class stays `internal`.

### Decision 6: Keep the services' XML doc remarks on `InvalidateFor` and `Invalidate`

The remarks ("the in-flight map is deliberately left alone so concurrent callers keep sharing
one probe...") document observable service behavior that remains true after the rewiring; the
backup kept them. The mechanism-level explanation now lives in `ProbeCache.Clear`/`Remove`/
`GetOrProbeAsync` docs. No remark text changes.

### Decision 7: `AgentDetectionService` is explicitly out of scope

It caches TTL entries (`AgentDetectionService.cs:18`) but has no in-flight dedup (verified: no
`inFlight`/`Lazy` in the file) because detection is cheap synchronous file-system checks.
Rewiring it would *add* dedup — a behavior change and spec question (`agent-detection`
requirement "Detection result caching" says nothing about coalescing), unrelated to this fix.

### Decision 8: Drop the backup branch's `// ---` file headers

`.editorconfig` has no file-header rule and no master file carries one. Porting the headers
would introduce a style the repo doesn't use (and `.editorconfig`/format sweeps would flag
the inconsistency later).

---

## Risks

- **Concurrency code, ported not rewritten**: mitigated by porting the exact reviewed backup
  implementation and its full test file (including the race regression test) rather than
  retyping; `dotnet test` gates every stage.
- **Subtle semantic drift in the rewiring** (e.g. accidentally dropping the
  `ArgumentNullException.ThrowIfNull(plugin)` in `CheckAsync`, or caching a null-returning
  probe differently): mitigated by keeping master's probe bodies untouched except for removing
  the cache writes (the moved `ProbeAndCacheAsync` caching semantics are identical: value
  cached with a post-probe timestamp, including `null` values), and by the existing
  service-level tests (`AgentVersionServiceTests`, `AgentUpdateAvailabilityServiceTests`)
  staying green without edits.
- **Flaky race test**: the `3612a55` hardening (bounded `FirstPost.WaitAsync`) already
  addresses the known flake mode; the test is deterministic by construction (queueing
  context holds the continuation until `RunPending`).

---

## Open Questions

1. **Spec strengthening (Decision 2)**: should a reviewer nonetheless want the
   "delayed continuation cannot evict a newer in-flight probe" invariant written into
   `agent-version`/`agent-update-detection` as a new scenario, that would be a small follow-up
   delta at archive time — the implementer should not block on it (`skip_specs: true` stands
   unless the reviewer says otherwise).
2. **Service-level race coverage**: the regression is pinned at `ProbeCache` level only (both
   services delegate 1:1). If review wants belt-and-braces, an `AgentVersionService`-level
   coalescing test would be additive — not required by this change.
3. **Line length of the delegation call**: `.editorconfig` sets no `max_line_length`; the
   single-statement delegation (`return await this.cache.GetOrProbeAsync(...)`) may exceed
   120 columns as written in the backup. Wrap per surrounding-file taste; no gate enforces
   it.
