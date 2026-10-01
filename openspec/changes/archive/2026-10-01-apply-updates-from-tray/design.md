# Design

## Context

`UpdateService` (Core) is the only place that touches Velopack: it owns an `UpdateManager` built lazily from the GitHub source, exposes `GetCurrentVersion` and `CheckForUpdatesAsync`, and has an internal test constructor that accepts an injected manager (`TestVelopackLocator` + `SimpleFileSource` in tests). The WPF layer only consumes results: `App.xaml.cs` runs startup/manual checks and forwards "update available" to `TrayIconController`, which shows a balloon and rebuilds its menu. The What's New window (`WhatsNewViewModel`) is display-only today.

Velopack's `UpdateManager` already provides the pieces we need: `CheckForUpdatesAsync()` returns an `UpdateInfo` that must be kept and passed to `DownloadUpdatesAsync(info)`, and `ApplyUpdatesAndRestart()` applies the downloaded package and relaunches the app. No new dependency is needed.

## Goals / Non-Goals

**Goals:**
- One-click download + restart reachable from the tray menu and the What's New window.
- Menu/state feedback while downloading; notifications on completion and failure.
- Guard rails: single concurrent download, no action for non-managed installs.
- Keep all Velopack interaction in `CLIHub.Core`, preserving the existing test seam.

**Non-Goals:**
- Progress percentages (byte-level progress is not reliably surfaced by the Velopack API we use; state-level feedback is enough).
- Auto-download without user action, update rollback UI, scheduling changes, packaging/CI changes.

## Decisions

### 1. Extend `IUpdateService` instead of exposing `UpdateManager`

Add to the interface:
- `Task<UpdateDownloadResult> DownloadUpdateAsync(CancellationToken ct = default)` — checks for an update (reusing `CheckForUpdatesAsync` internally), downloads it, returns a result (`Downloaded` with the version, `NoUpdate`, `NotInstalled`, `Failed`, `AlreadyDownloading`).
- `void ApplyDownloadedUpdateAndRestart()` — calls `ApplyUpdatesAndRestart()`; used only after a `Downloaded` result.

Alternatives considered: exposing the `UpdateManager` itself to the WPF layer (rejected — Velopack would leak into the UI project and break the Core test seam); a full state machine service with events (rejected as over-built — the UI has two small surfaces, tray menu and one window, and both can be told to refresh on state change through a simple callback).

### 2. Download state owned by `UpdateService`, fanned out to the UI

`UpdateService` holds an `IsDownloading` flag (guarded so a second `DownloadUpdateAsync` returns `AlreadyDownloading` immediately). The WPF layer subscribes once at startup (the same place it already handles update checks in `App.xaml.cs`) and forwards state changes to `TrayIconController` and `WhatsNewViewModel`:
- Idle + update available → item "Download update and restart".
- Downloading → item "Downloading update…" (disabled).
- Completion → balloon "Update installed, restarting…", then `ApplyDownloadedUpdateAndRestart()`.
- Failure → balloon, item returns to "Download update and restart".

Both surfaces read the same service state, so they cannot disagree. The What's New window gets an install button bound to `WhatsNewViewModel`, which calls the same service methods.

Alternative considered: polling from the view models — rejected; a single callback path is simpler and matches how update-check results already flow.

### 3. Restart without a prompt

The user explicitly invoked the action, so no confirmation dialog; the download balloon says the restart is about to happen. Configuration is saved atomically on every change (existing `ConfigService`), and `LastSeenReleaseNotesVersion` is written at startup, so an abrupt restart loses nothing. If `ApplyUpdatesAndRestart` throws or the process survives, the exception is logged and the menu item returns to the download state (the spec's "restart does not happen" scenario); the downloaded package stays in Velopack's cache, so retrying is cheap.

### 4. Tray menu refresh timing

Windows does not redraw an open `ContextMenuStrip`, so state changes while the menu is open take effect the next time it opens. `TrayIconController.RefreshMenu()` already rebuilds on demand; it will consult the update state, and the completion/failure balloons keep the user informed regardless. This is accepted behavior, not a bug to work around.

### 5. Testing strategy

The `TestVelopackLocator` + `SimpleFileSource` seam lets `DownloadUpdateAsync` be tested against a local packages folder: no-update (`NoUpdate`), already-downloading (`AlreadyDownloading`), and not-installed (`NotInstalled`) paths are unit-testable in `UpdateServiceTests`. `ApplyDownloadedUpdateAndRestart` genuinely restarts the process and cannot be unit-tested; it stays a one-line call and is verified manually. UI wiring (menu states, What's New button) is verified manually against the spec scenarios.

## Risks / Trade-offs

- [Restart closes the app abruptly mid-use] → User consented by invoking the action; no unsaved state exists in CLIHub; balloon announces the restart.
- [Velopack API shape changes across versions] → Velopack is already pinned and the interaction is confined to `UpdateService`; adaptation cost stays local.
- [Download size/time on slow connections with only state-level feedback] → Accepted for now; a progress window remains a possible follow-up and does not change the specs.
- [Menu state invisible while the menu is open] → Covered by balloons and next-open refresh (decision 4).

## Migration Plan

No data or configuration migration. Rollback is a plain revert; the new interface members are additive and unused elsewhere.

## Open Questions

None.
