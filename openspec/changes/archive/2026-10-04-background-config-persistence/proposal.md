# Proposal: background-config-persistence

## Why

Every configuration change (project add/remove/favorite/select, filter and pin toggles, Settings save, release-notes bookkeeping) serializes `config.json` and writes it to disk synchronously on the UI thread, and each of these changes also triggers a full rebuild of the launch window's project list. This adds avoidable UI-thread stalls and redundant list churn to the most frequent interactions in the app.

## What Changes

- `ConfigService.Save` becomes asynchronous at the disk level: the calling thread serializes the configuration (a cheap in-memory snapshot that avoids mutation races with the shared config object graph), and a single background worker writes the file with a short debounce that coalesces rapid consecutive saves.
- `IConfigService` gains `Flush()`, which synchronously drains pending writes; the service also flushes on disposal, and container disposal at application exit already disposes singletons — so a graceful shutdown never loses pending changes.
- The on-disk write stays atomic (temp file + rename); in-memory `Load()` semantics are unchanged.
- `Project` becomes observable: `IsFavorite`, `LastUsed`, and `LogoPath` raise property-change notifications, so launch-window rows update in place (for example, the favorite star).
- The launch window's project refresh switches from clear-and-rebuild to a diff-based sync that only adds/removes changed membership, keeping item (and selection) identity stable.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `project-management`: The "Project persistence" requirement's "Persist on change" scenario now persists promptly via a background write instead of a synchronous write on the calling thread, with pending writes flushed before the application exits.
- `app-lifecycle`: The "Graceful shutdown" requirement gains a scenario guaranteeing pending configuration writes are flushed to `config.json` during shutdown.

## Impact

- **Modified code**: `src/CLIHub.Core/Interfaces/IConfigService.cs` (new `Flush()`), `src/CLIHub.Core/Services/ConfigService.cs` (background writer, debounce, flush, dispose), `src/CLIHub.Core/Models/Project.cs` (INotifyPropertyChanged), `src/CLIHub/ViewModels/LaunchWindowViewModel.cs` (diff-based project refresh).
- **Tests**: `tests/CLIHub.Tests` — new ConfigService background-save/flush tests; `FakeConfigService` implements `Flush()`; existing ProjectService tests unchanged.
- No new dependencies; no breaking API changes beyond the additive `IConfigService.Flush()`.
- Callers of `Save` (`ProjectService`, `LaunchWindowViewModel`, `SettingsViewModel`, `ReleaseNotesLauncher`) keep their code unchanged — the async behavior is encapsulated in `ConfigService`.
