# Proposal: Persistence/Cache Cross-Cutting Slice

## Why

Reconnaissance for Proposal 1's remaining slices (`IPersistenceService<T>`, `ICacheService`) from the architecture analysis (backlog entry, 2026-10-29). The slice must find genuinely duplicated persistence or caching mechanics shared by at least two real consumers before extracting. No duplication = no slice.

## What Changes

REJECTED. No code changes. The evidence shows that the shared mechanics are too narrow to justify extraction:

**Persistence:** ConfigurationRepository and LogoCacheService share 3-line atomic write pattern (temp file + write + move) but differ fundamentally in concurrency model (debounced async writer with triple lock vs synchronous dispose-only write). A generic service covering both needs optional debounce, optional generation tracking, optional locking, optional dirty flag - speculative architecture with no real duplication.

**Caching:** AgentDetectionService, AgentVersionService, AgentUpdateAvailabilityService share TTL+invalidation contract (2-line expiry check). LogoCacheService and LogoImageSourceCache do not match (no TTL, or UI-only). An `ICacheService` unifying all 5 needs optional TTL, optional persistence, optional in-flight dedup, optional prefix semantics - again speculative.

The mechanics inventory (design.md) shows file:line evidence for every candidate. The shared code is 3 lines (atomic write) and 2 lines (TTL check). The surrounding policies (debounce, locking, key scheme, invalidation semantics) do not duplicate.

## Non-Goals

Delivered decision with evidence, not code. The slice was "delivery or rejection with evidence" - rejection with evidence is the delivery.

## Dependencies

None
