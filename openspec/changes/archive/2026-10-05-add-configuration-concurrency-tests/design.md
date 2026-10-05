## Context

`ConfigService` serializes `AppConfig` to JSON on the calling thread, stores the latest pending JSON under `_gate`, and uses one debounced worker for disk writes. `Flush` waits for the observed worker, drains any remaining pending JSON synchronously, and `ProcessPendingAsync` starts a replacement worker when a save arrives while the worker is exiting. `WriteConfig` writes a `.tmp` file and moves it over the target while holding `_writeLock`; write exceptions are logged and swallowed.

Existing `ConfigServiceTests` verify initial save/flush, rapid saves, load behavior, and a basic write failure. The new tests need to verify the synchronization contract without relying on arbitrary sleeps that make the suite flaky.

## Goals / Non-Goals

**Goals:**

- Test the observable persistence guarantees of the current `ConfigService` implementation.
- Make concurrency tests deterministic and isolated per temporary config path.
- Cover latest-save-wins, flush completion, worker restart, write failure, and atomic replacement behavior.
- Detect lost pending JSON and invalid/truncated final files.

**Non-Goals:**

- Change `ConfigService` behavior or redesign its synchronization in this change.
- Add the configuration snapshot/repository abstraction planned for later changes.
- Test unrelated stores such as `PreferencesStore` or `ProjectStateStore` beyond their existing coverage.
- Depend on production timing values or machine-specific filesystem paths.

## Decisions

- Extend the existing Core `ConfigServiceTests` rather than create a second test fixture.
- Use unique temporary directories and dispose each service after assertions so pending writes are flushed and files do not interfere across tests.
- Prefer observable barriers (file content transitions, repeated polling with bounded timeouts, or controlled file access) over fixed sleeps. Any polling helper must have a short bounded timeout and a useful failure message.
- Verify latest JSON by reading and deserializing the persisted document, not by asserting incidental write counts.
- Cover concurrent calls through `Task.Run`/barriers and assert that `Flush` returns only after the latest pending configuration is durable.
- Exercise worker restart by arranging a save while the worker is draining a previous write, then call `Flush` and assert the second value persists.
- Exercise atomicity by keeping a valid baseline file, forcing the replacement write to fail, and asserting the baseline remains parseable and unchanged; restore the filesystem condition before disposing the service.
- If deterministic failure injection is impossible through the current constructor, introduce the smallest internal test seam needed and keep it Core-only; do not broaden the public interface for this test change.

## Risks / Trade-offs

- **Timing-based tests become flaky** -> Use barriers/polling and bounded timeouts; do not use unbounded waits.
- **Filesystem failure behavior differs by Windows environment** -> Use a controlled path/lock or the smallest internal seam, and document the exact failure mechanism in the test.
- **Tests expose an actual race** -> Stop and update the design/tasks before changing production synchronization; the intended first outcome is a reproducible failing test.

## Migration Plan

1. Review existing `ConfigServiceTests` helpers and identify deterministic synchronization points.
2. Add concurrency, flush, restart, failure, and atomicity tests with bounded timeouts.
3. Add only a minimal internal test seam if required to control filesystem timing/failure.
4. Run Core tests, the full solution build/test, and OpenSpec validation.
5. Rollback consists of removing the new tests and any test-only seam.

## Open Questions

None.
