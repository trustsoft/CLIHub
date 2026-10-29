# Design: Persistence and Caching Mechanics Investigation

## Current State

### Persistence Consumers

**ConfigurationRepository** (`src/CLIHub.Core/Configuration/ConfigurationRepository.cs`)
- Atomic write: Lines 265-284 (temp `.tmp` + `File.WriteAllText` + `File.Move(overwrite: true)`)
- Directory creation: Line 270 (`Directory.CreateDirectory`)
- Serialization: Lines 80, 142-143 (`JsonSerializer.Serialize` with `CoreJson.Options`)
- Concurrency: Triple lock (stateGate :19, writerGate :20, writeLock :21)
- Debounced writer: Lines 218-258 (250ms delay, background task, cancellable)
- Generation counter: Lines 26, 149, 277-280 (prevents stale writes)
- Failure handling: Lines 289-291 (logged warning, no throw)
- On load missing/corrupt: Lines 93-139 (defaults + log, no throw)
- Dispose flush: Lines 194-204
- Tests: `ConfigurationRepositoryTests` (24 tests, atomicity + concurrent writes + debounce)

**LogoCacheService** (`src/CLIHub.Core/Infrastructure/Persistence/LogoCacheService.cs`)
- Atomic write: Lines 165-177 (temp `.tmp` + `File.WriteAllText` + `File.Move(overwrite: true)`)
- Directory creation: Lines 167-171 (`Directory.CreateDirectory`)
- Serialization: Lines 163, 180-187 (`JsonSerializer.Serialize` with `CoreJson.Options`, custom `CacheState`)
- Concurrency: Single-threaded write (dispose only), `ConcurrentDictionary` for reads
- Debounced writer: NO (synchronous write on dispose)
- Generation counter: NO
- Dirty flag: Lines 21, 53-54, 66, 81, 96, 151 (prevents no-op writes)
- Failure handling: Lines 116-122 (logged warning, no throw)
- On load missing/corrupt: Lines 125-152 (empty + log, dirty on corrupt)
- Dispose flush: Lines 101-123 (write only if dirty)
- Tests: `LogoCacheServiceTests` (18 tests, atomicity + restart persistence + invalidation)

**PluginSeeder** (`src/CLIHub.Core/Plugins/PluginSeeder.cs`)
- Atomic write: NONE (direct `File.WriteAllText` :89)
- Directory creation: Line 88
- Serialization: embedded resource text written verbatim
- Concurrency: NO (single-threaded startup only)
- Failure handling: NO try/catch at write
- Verdict: Seed-only path, no atomicity, no failure contract. NOT a persistence candidate.

### Caching Consumers

**AgentDetectionService** (`src/CLIHub.Core/Agents/AgentDetectionService.cs`)
- Key: `string` (composite `"sys:<pluginId>"` or `"proj:<pluginId>:<projectPath>"`) :57-62
- Value: `bool` (detection result)
- Storage: `ConcurrentDictionary<string, CacheEntry>` :18 (timestamp :129)
- TTL: YES (config-driven, default 15min) :14, :66-83 (`now - entry.Timestamp < this.Ttl`)
- Invalidation: `Invalidate()` :38, `InvalidateFor(plugin)` :41-55 (prefix-based removal)
- Thread-safety: `ConcurrentDictionary`
- In-flight dedup: NO
- Persistence: NO

**AgentVersionService** (`src/CLIHub.Core/Agents/AgentVersionService.cs`)
- Key: `string` (pluginId)
- Value: `string?` (version)
- Storage: `ConcurrentDictionary<string, CacheEntry>` :35 (timestamp :161)
- TTL: YES (config-driven, default 15min) :22, :56-59, :124-131
- Invalidation: `Invalidate()` :110, `InvalidateFor(plugin)` :118-122 (exact key)
- Thread-safety: `ConcurrentDictionary` + `Lazy<Task>` in-flight dedup :36, :62-78
- In-flight dedup: YES (`inFlight` map :36, :62-78)
- Persistence: NO

**AgentUpdateAvailabilityService** (`src/CLIHub.Core/Agents/AgentUpdateAvailabilityService.cs`)
- Key: `string` (pluginId)
- Value: `AgentUpdateAvailability` (enum)
- Storage: `ConcurrentDictionary<string, CacheEntry>` :23 (timestamp :204)
- TTL: YES (config-driven, same as version service) :53-55, :195-201
- Invalidation: `Invalidate()` :78, `InvalidateFor(plugin)` :89 (exact key)
- Thread-safety: `ConcurrentDictionary` + `Lazy<Task>` in-flight dedup
- In-flight dedup: YES (same pattern as version service)
- Persistence: NO

**LogoCacheService** (caching aspect, see persistence above)
- Key: `string` (namespaced `"project:<id>"`, `"plugin:<id>"`)
- Value: `string?` (path or negative result)
- Storage: `ConcurrentDictionary<string, string?>` :19
- TTL: NO
- Invalidation: `InvalidateAll(prefix?)` :71-98, `Remove(key)` :60-68
- Thread-safety: `ConcurrentDictionary`
- In-flight dedup: NO
- Persistence: YES (atomic write on dispose)

**LogoImageSourceCache** (`src/CLIHub/Imaging/LogoImageSourceCache.cs`)
- Key: `string` (path or special key)
- Value: `ImageSource` (frozen WPF image)
- Storage: `ConcurrentDictionary<string, ImageSource>` :21
- TTL: NO
- Invalidation: NONE (lifetime = application)
- Thread-safety: `ConcurrentDictionary`
- In-flight dedup: NO
- Persistence: NO
- Verdict: UI-only, never persisted, no invalidation. NOT a cache-service candidate.

## Decision

**REJECT the slice.** No `IPersistenceService<T>` or `ICacheService` extraction.

### Rationale

#### Persistence

The atomic write pattern (3 lines: temp file + `File.WriteAllText` + `File.Move`) is shared by ConfigurationRepository and LogoCacheService, but the surrounding concurrency and lifecycle policies differ fundamentally:

1. **ConfigurationRepository:** Debounced async writer (250ms, background task, cancellable), triple lock (state/writer/writeLock), generation counter prevents stale writes, hot-path mutations.
2. **LogoCacheService:** Synchronous dispose-only write, single-threaded at write time, simple dirty flag, cold-path shutdown only.

A generic `IPersistenceService<T>` covering both needs:
- Optional debounce (250ms or none)
- Optional generation tracking (present or absent)
- Optional triple lock vs single-threaded (policy mismatch)
- Optional dirty flag (optimization vs not)

This is speculative architecture. The shared code is 3 lines (atomic write). The differences (debounce/locking/generation) are not duplication - they are design choices per consumer. Extracting the 3-line pattern into a service adds indirection without removing duplication.

**Trigger:** A third JSON-persisted consumer with temp-file-then-rename atomicity AND a shared concurrency model (debounced or dispose-only), or the `config-format-v2` decision explicitly requires a persistence abstraction to unify JSON/binary backends.

#### Caching

The 3 agent services (Detection, Version, UpdateAvailability) share TTL+invalidation contract:
- TTL expiry check: `now - entry.Timestamp < this.Ttl` (2 lines)
- Invalidation methods: `Invalidate()`, `InvalidateFor(plugin)`

But:
1. **Key scheme:** AgentDetectionService uses composite keys (`sys:`/`proj:` prefixes), others use simple pluginId.
2. **In-flight dedup:** AgentVersionService and AgentUpdateAvailabilityService dedup concurrent probes via `Lazy<Task>`, AgentDetectionService does not (detection is synchronous).
3. **Non-agent caches:** LogoCacheService (no TTL, persisted), LogoImageSourceCache (UI-only, no TTL, no invalidation) do not match the agent pattern.

An `ICacheService` unifying all 5 needs:
- Optional TTL (present or absent)
- Optional persistence (present or absent)
- Optional in-flight dedup (present or absent)
- Optional key-prefix semantics (composite or simple)

This is speculative. The shared code is 2 lines (TTL check). The invalidation contract is similar in name but differs in semantics (prefix-based vs exact, with/without in-flight dedup).

**Trigger:** A new cache consumer sharing the agent services' TTL+invalidation contract (not a UI-only or persisted cache), or a request to unify the agent caches' boilerplate when a fourth service appears with the exact same pattern.

## Alternatives Rejected

### Extract only the atomic write helper

```csharp
internal static class AtomicJsonWriter
{
    public static void Write<T>(string path, T value, JsonSerializerOptions options)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        var temp = path + ".tmp";
        var json = JsonSerializer.Serialize(value, options);
        File.WriteAllText(temp, json);
        File.Move(temp, path, overwrite: true);
    }
}
```

**Rejected:** A 7-line static helper is not a cross-cutting service. The 3-line duplication (temp + write + move) is too narrow to justify a helper. ConfigurationRepository's debounced writer and LogoCacheService's dispose-time writer would still wrap this in their own policies.

### Extract a minimal `IPersistenceService<T>` with only Save/Load

**Rejected:** ConfigurationRepository's debounced writer and generation counter are not optional - they are its contract. LogoCacheService's dirty flag prevents no-op writes. A service with only `Save<T>(path, value)` and `Load<T>(path)` covers neither consumer's actual needs.

### Extract TTL cache helper

**Rejected:** The 2-line TTL check (`now - entry.Timestamp < this.Ttl`) is not duplication. The invalidation semantics (prefix vs exact, with/without in-flight dedup) differ per consumer. A helper would replicate each consumer's key scheme and dedup policy as optional.

## Follow-Ups

None. This reconnaissance closes Proposal 1's remaining slices. The logging context provider slice is complete (commit `7d92bf3`), and this rejection shows that `IPersistenceService<T>` and `ICacheService` lack real duplication.

## Open Questions

None. The decision is reject with evidence.
