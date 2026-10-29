# ADR-0013: No Generic Persistence or Cache Abstraction

**Date:** 2026-10-29  
**Status:** Accepted  
**Related:** ADR-0002 (single configuration document), Proposal 1 (cross-cutting services), Proposal 6 (configuration decomposition)

## Context

Proposal 1 planned a generic `IPersistenceService<T>` and `ICacheService` for
cross-cutting infrastructure. Before building them, a reconnaissance pass inventoried every real
consumer and compared the mechanics they actually share.

**Persistence consumers:**

- `ConfigurationRepository` (`src/CLIHub.Core/Configuration/ConfigurationRepository.cs`): a debounced
  asynchronous writer (about 250 ms), two nested locks (`writeLock`, `writerGate`), a generation
  counter that discards superseded writes, temp file + `File.Move(overwrite: true)`.
- `LogoCacheService` (`src/CLIHub.Core/Infrastructure/Persistence/LogoCacheService.cs`):
  synchronous, dispose-time write guarded by a dirty flag, no locks, no debounce, no generation.
- Nothing else carries the atomic temp+rename pattern (`PluginSeeder` has no atomicity by design).

The duplicated code is two statements - write the temp file, move it over the target. The
surrounding policies (debounce, locking, generation, dirty flag) are mutually exclusive and each
exists for a concrete consumer.

**Cache consumers:**

- `AgentDetectionService`, `AgentVersionService`, `AgentUpdateAvailabilityService`: TTL entries with
  per-plugin keys, prefix invalidation, and in-flight deduplication - but each with different key
  composition and refresh triggers.
- `LogoCacheService`: no TTL, persisted entries, prefix invalidation on add/remove.
- `LogoImageSourceCache`: UI-only frozen image sources, no TTL, no invalidation.

The shared fragment is a two-line expiry check; a unifying contract would need optional TTL,
optional persistence, optional deduplication, optional prefix semantics and optional lifetime.

## Decision

Do not introduce `IPersistenceService<T>` or `ICacheService`. Keep the atomic write inline where
each policy lives (configuration and logo cache), and keep each cache with its own explicit
invalidation contract.

A generic abstraction here would be an over-parameterized wrapper around two statements, with
optional behaviours for every consumer - the exact speculative abstraction the architecture analysis
warned about. The concrete policies stay legible, and the wire format and atomicity contracts are
already pinned by `ConfigurationRepositoryTests`, `PersistenceCloneRoundTripTests` and the logo cache
tests.

## Consequences

**Easier:** no indirection layer to reason about; a reader sees the real concurrency policy at the
call site; the ADR-0002 invariants stay untouched.

**Harder:** a future storage backend change (for example the `config-format-v2` decision) will need
its own design; if a third consumer appears with the same policy shape, the extraction will be
revisited with three real data points.

**Committed to:** recording reopen triggers so this is a decision, not an omission.

## Reopen triggers

1. A third JSON-persisted consumer appears whose concurrency model matches an existing one
   (debounced writer or dispose-only writer), making the extraction a deduplication rather than a
   generalization.
2. The `config-format-v2` work explicitly needs a persistence abstraction to support multiple
   backends or a directory-swap transaction.
3. A fourth agent-domain cache appears with the exact TTL + invalidation shape, and the boilerplate
   becomes the dominant cost of adding a cache.
