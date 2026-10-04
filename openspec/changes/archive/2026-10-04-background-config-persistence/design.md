# Design: background-config-persistence

## Context

`ConfigService.Save` performs serialize + `WriteAllText` + `File.Move` synchronously on the caller (UI) thread, and every `ProjectService.Persist()` raises `ProjectsChanged`, to which the launch view model responds with a full clear-and-rebuild of the project list. The configuration object graph (`AppConfig` → `List<Project>`, `AppPreferences`) is a shared mutable singleton: `ProjectService` holds and mutates the same instance that `Load()` returns. See proposal.md for motivation.

## Goals / Non-Goals

**Goals:**

- No disk I/O on the UI thread for configuration saves; rapid consecutive saves coalesce into one write.
- No lost changes on graceful shutdown (flush at container disposal).
- Launch-window project rows update in place for favorite/selection mutations; collection churn only for actual membership changes.
- `Save` call sites stay unchanged (async behavior encapsulated in `ConfigService`).

**Non-Goals:**

- Reworking `IConfigService`'s project-related members (separate improvement).
- Making `Save` awaitable (`Task`-returning API) — no caller needs to await; flush covers determinism.
- Diff-sync for the agents pane (unchanged; small list).
- Changing what triggers a save or the config file format.

## Decisions

### 1. Snapshot-serialize on the calling thread; only disk I/O moves to the background

`Save` serializes `AppConfig` to a JSON string immediately, then hands the immutable payload to the background writer.

*Why*: the config object graph is shared and mutated in place; serializing a copy in the background would race with UI mutations (for example, a `List` re-ordered during enumeration). JSON serialization of this tiny document is sub-millisecond, so the calling thread keeps only cheap work. *Alternative considered*: deep-copy then background-serialize — extra allocation and copying code with no practical gain.

### 2. Single debounced background worker inside `ConfigService`

A lazily started worker task owns all writes: it waits a short debounce (250 ms), takes the latest pending payload (earlier pending payloads are simply replaced), writes it atomically, and repeats until the queue is empty. Guards are a lock over a `_pendingJson` field plus a worker-running flag; at most one worker exists.

*Why*: coalesces bursts (for example, select-project followed by refresh churn) into one disk write; simpler and more robust than a channel pipeline for a write every few seconds. *Alternative considered*: `Channel<string>` with a consumer loop — equivalent behavior, more machinery.

### 3. `Flush()` on `IConfigService` + flush on dispose

`Flush()` waits for the running worker and then synchronously writes any payload that arrived in the meantime, so tests and shutdown get deterministic persistence. `ConfigService` also implements `IDisposable` → `Flush`; `ServiceProvider.Dispose()` in `App.OnExit` already disposes singletons, so no new shutdown wiring is needed.

*Why*: `Save` alone can no longer guarantee "on disk now"; flush restores that guarantee where it matters (exit, tests). Matches the proven `LogoCacheService` lifecycle pattern.

### 4. `Project` implements `INotifyPropertyChanged` for mutable display state

`IsFavorite`, `LastUsed`, and `LogoPath` raise notifications; `Id`, `Name`, and `Path` stay plain (assigned once at creation).

*Why*: the launch window binds directly to these properties (favorite star, logo, path display); in-place updates remove the need to rebuild rows after toggles. `System.Text.Json` ignores events, so serialization is unaffected.

### 5. Diff-based project sync in `LaunchWindowViewModel.RefreshProjects`

Instead of `Clear()` + re-add, remove items whose IDs vanished and append new IDs, keeping existing `Project` instances in the `ObservableCollection`. `ProjectService` returns the same instances it mutates, so in-place property changes flow through INPC without any collection event. Selection restoration logic is unchanged.

*Why*: preserves item identity (no row flicker, stable `SelectedProject` reference) and eliminates per-persist rebuild churn. *Alternative considered*: raising granular events per mutation from `ProjectService` — more surface in the service for the same visual result.

## Risks / Trade-offs

- [Crash within the debounce window loses the last ≤250 ms of configuration changes] → Graceful exit flushes; the risk window only covers hard crashes (power loss, kill), where losing a "last used" timestamp is acceptable.
- [Background write fails (file locked by an editor, disk error)] → Logged as a warning; the change is dropped from the queue and is re-persisted by the next save or at flush. `Load()` state in memory is unaffected.
- [Worker races with `Flush`] → `Flush` waits for the worker task, then drains any payload that arrived after; a final lock check inside the worker's exit path restarts it if a save slipped in, so no payload is stranded.
- [`Project` now has UI-facing concerns in a Core model] → `INotifyPropertyChanged` is UI-agnostic; Core keeps no WPF dependency, and this is the standard MVVM trade-off for bound models.

## Migration Plan

No migration: the config file format and location are unchanged; first run after the update behaves identically. Rollback is a plain revert.

## Open Questions

None.
