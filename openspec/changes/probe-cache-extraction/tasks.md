# Implementation Tasks

All `git show` commands are read-only views of branch `backup-before-reset`; no merges,
cherry-picks, or branch switches. Do not touch the untracked `backup-port-*.md` files in the
repository root.

## Stage 1: Add `ProbeCache` to Core

- [ ] 1.1 View the port source: `git show backup-before-reset:src/CLIHub.Core/Agents/ProbeCache.cs` (~102 lines; final state — already contains the `ae9cf02` compare-and-remove fix and the final XML docs).
- [ ] 1.2 Create `src/CLIHub.Core/Agents/ProbeCache.cs` with that content, dropping the four-line `// ---` file header (master files carry no header — compare `src/CLIHub.Core/Agents/AgentProbePolicy.cs:1`; `.editorconfig` has no header rule).
- [ ] 1.3 Keep verbatim: file-scoped `namespace CLIHub.Core.Agents;`, `using System.Collections.Concurrent;` placed after the namespace, `internal sealed class ProbeCache<TValue>`, XML docs on every member (including the remarks explaining caller-scoped cancellation and poison-avoidance), camelCase private fields (`timeProvider`, `entries`, `inFlight`), and `this.` qualification.
- [ ] 1.4 Verify the `finally` in `GetOrProbeAsync` removes only the caller's own entry: `this.inFlight.TryRemove(new KeyValuePair<string, Lazy<Task<TValue>>>(key, lazy));` — it must NOT be `TryRemove(key, out _)` (this is the `ae9cf02` correctness fix the change carries; a key-based remove reintroduces the race).
- [ ] 1.5 Build gate: `dotnet build CLIHub.sln` — succeeds with no new warnings from the file.

## Stage 2: Port the synchronization fake and `ProbeCacheTests`

- [ ] 2.1 Create `tests/CLIHub.Core.Tests/Fakes/QueueingSynchronizationContext.cs` from `git show backup-before-reset:tests/CLIHub.Core.Tests/Fakes/QueueingSynchronizationContext.cs` (final state, includes the `FirstPost` task from `3612a55`), dropping the file header; keep `namespace CLIHub.Core.Tests.Fakes;`, `internal sealed class`, `using System.Collections.Concurrent;` after the namespace, and XML-doc-free style matching the neighboring fakes (none of the existing files under `tests/CLIHub.Core.Tests/Fakes/` carry headers).
- [ ] 2.2 Create `tests/CLIHub.Core.Tests/Agents/ProbeCacheTests.cs` from `git show backup-before-reset:tests/CLIHub.Core.Tests/Agents/ProbeCacheTests.cs` (~264 lines, 10 `[Fact]` tests), dropping the file header.
- [ ] 2.3 Confirm the tests use the existing master fake `CLIHub.Core.Tests.Fakes.FakeTimeProvider` (`tests/CLIHub.Core.Tests/Fakes/FakeTimeProvider.cs:6-14`: public, constructor takes `DateTimeOffset`, `Advance(TimeSpan)`) — the ported file already does; do NOT add a TimeProvider testing package or a new fake.
- [ ] 2.4 Confirm the race regression test `GetOrProbe_HeldCallerContinuationAfterNewerProbePublished_DoesNotEvictNewerInFlightProbe` is present verbatim, including the `await context.FirstPost.WaitAsync(TimeSpan.FromSeconds(5));` hardening and the comment explaining why the wait is required.
- [ ] 2.5 Confirm every test name follows `MethodOrScenario_Condition_ExpectedResult` (already true in the ported file) and the class is a plain `public class ProbeCacheTests` matching `tests/CLIHub.Core.Tests/Agents/AgentVersionServiceTests.cs:13` style.
- [ ] 2.6 Build gate: `dotnet build CLIHub.sln`.
- [ ] 2.7 Test gate: `dotnet test CLIHub.sln --filter "FullyQualifiedName~ProbeCacheTests"` — all 10 pass against the fixed `ProbeCache` (the race regression test must pass; if it fails, the `finally` is removing by key — see task 1.4).

## Stage 3: Rewire `AgentVersionService`

- [ ] 3.1 In `src/CLIHub.Core/Agents/AgentVersionService.cs`, remove `using System.Collections.Concurrent;` (line 3).
- [ ] 3.2 Replace the two fields at lines 34-35 (`cache`, `inFlight`) with `private readonly ProbeCache<string?> cache;`, and initialize it in the constructor after line 49 (`this.timeProvider = ...`): `this.cache = new ProbeCache<string?>(this.timeProvider);`.
- [ ] 3.3 Replace the dedup body of `GetVersionAsync` (lines 55-77: TTL check, `inFlight.GetOrAdd`, try/`await lazy.Value.WaitAsync`/finally including the buggy `this.inFlight.TryRemove(plugin.Id, out _);` at line 75) with the single delegation `return await this.cache.GetOrProbeAsync(plugin.Id, this.Ttl, () => this.ProbeAsync(plugin), cancellationToken);` (wrap the line if it reads better; no length gate applies).
- [ ] 3.4 Rename `ProbeAndCacheAsync` (line 80) to `ProbeAsync` and delete its two cache writes — `this.cache[plugin.Id] = new CacheEntry(null, ...)` (line 85) and `this.cache[plugin.Id] = new CacheEntry(version, ...)` (line 104). `ProbeCache` stores the returned value (including `null`) with a post-probe timestamp itself; keep the rest of master's probe body (`result.Output`/`result.Error` handling) untouched.
- [ ] 3.5 Change `Invalidate()` (line 109) to `this.cache.Clear();` and the `InvalidateFor` body (line 120) to `this.cache.Remove(plugin.Id);` — keep the null guard (line 119) and the remarks (lines 111-116) unchanged.
- [ ] 3.6 Delete `private readonly record struct CacheEntry(...)` (line 144). Keep `DefaultTtl`/`DefaultProbeTimeout` (lines 21-26), `VersionPattern`/`ExtractVersion` (line 28, 129-142), and the `Ttl => AgentProbePolicy.ResolveTtl(this.preferencesStore.Load().AgentProbeTtlMinutes)` property (line 123) unchanged — do NOT port the backup's `AgentProbeDefaults.TtlFrom` (it does not exist on master).
- [ ] 3.7 Build gate: `dotnet build CLIHub.sln`.
- [ ] 3.8 Test gate: `dotnet test CLIHub.sln` — the existing `AgentVersionServiceTests` (TTL hit at line 122, expiry at 137, invalidate at 155, concurrent callers) pass unchanged.

## Stage 4: Rewire `AgentUpdateAvailabilityService`

- [ ] 4.1 In `src/CLIHub.Core/Agents/AgentUpdateAvailabilityService.cs`, remove `using System.Collections.Concurrent;` (line 3).
- [ ] 4.2 Replace the two fields at lines 24-25 with `private readonly ProbeCache<AgentUpdateAvailability> cache;`, and initialize it in the constructor after line 45 (`this.timeProvider = ...`): `this.cache = new ProbeCache<AgentUpdateAvailability>(this.timeProvider);`.
- [ ] 4.3 Replace the dedup body of `CheckAsync` (lines 53-75: TTL check, `GetOrAdd`, try/finally including the buggy `this.inFlight.TryRemove(plugin.Id, out _);` at line 73) with `return await this.cache.GetOrProbeAsync(plugin.Id, this.Ttl, () => this.ComputeAvailabilityAsync(plugin), cancellationToken);` — keep the `ArgumentNullException.ThrowIfNull(plugin)` guard (line 51).
- [ ] 4.4 Delete `CheckAndCacheAsync` (lines 93-98) entirely; `ComputeAvailabilityAsync` (lines 100-159) becomes the probe factory and is not otherwise modified.
- [ ] 4.5 Change `Invalidate()` (line 79) to `this.cache.Clear();` and the `InvalidateFor` body (line 90) to `this.cache.Remove(plugin.Id);` — keep the null guard (line 89) and the remarks (lines 82-86) unchanged.
- [ ] 4.6 Delete `private readonly record struct CacheEntry(...)` (line 195). Keep the `Ttl => AgentProbePolicy.ResolveTtl(...)` property (line 193) and everything else in the file unchanged.
- [ ] 4.7 Build gate: `dotnet build CLIHub.sln`.
- [ ] 4.8 Test gate: `dotnet test CLIHub.sln` — the existing `AgentUpdateAvailabilityServiceTests` (caching, scoped invalidation, coalescing, degrade-to-Unknown) pass unchanged.

## Stage 5: Final gates

- [ ] 5.1 Build: `dotnet build CLIHub.sln` — full solution, zero warnings introduced.
- [ ] 5.2 Test: `dotnet test CLIHub.sln` — full suite green (Core tests + ProbeCacheTests + Windows/application tests).
- [ ] 5.3 Validate: `openspec validate --all` — reports 0 errors.
- [ ] 5.4 Refresh the knowledge graph: `graphify update .`.
- [ ] 5.5 Confirm `git status --porcelain` lists only this change's files: the new `src/CLIHub.Core/Agents/ProbeCache.cs`, the two rewired services, the new `tests/CLIHub.Core.Tests/Agents/ProbeCacheTests.cs` and `tests/CLIHub.Core.Tests/Fakes/QueueingSynchronizationContext.cs`, and `openspec/changes/probe-cache-extraction/**` — and that the untracked `backup-port-*.md` files remain present and untouched.
