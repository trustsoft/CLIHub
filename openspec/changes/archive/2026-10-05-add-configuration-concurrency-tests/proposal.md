## Why

`ConfigService` already coordinates debounced background writes, synchronous flushes, worker restarts, write failures, and atomic replacement, but the current tests cover only basic persistence paths. Without deterministic concurrency coverage, changes to configuration ownership or the future snapshot/repository work can silently reintroduce lost writes, stale output, or shutdown races.

## What Changes

- Add focused tests for consecutive and concurrent `Save` calls, asserting that the final JSON represents the latest save.
- Add tests for `Flush` while a background write is pending or active.
- Add tests proving a save arriving as the worker exits starts a replacement worker and is eventually persisted.
- Add tests for write failures and recovery/last-known JSON behavior using controlled file-system conditions.
- Add tests for atomic-write expectations, including preservation of the previous valid JSON when a replacement write fails.
- Keep production configuration behavior unchanged; this change strengthens the executable test contract.

## Capabilities

### New Capabilities

None. This is a test-only change for existing configuration persistence behavior.

### Modified Capabilities

None.

## Impact

- Primary test surface: `tests/CLIHub.Core.Tests/ConfigServiceTests.cs` and supporting test helpers.
- Tests may need deterministic synchronization primitives or an injectable/controlled file-system seam if the current implementation cannot be exercised reliably with timing alone.
- No public runtime API, configuration format, or application behavior is intended to change.
- Full Core and WPF test projects must remain green.
