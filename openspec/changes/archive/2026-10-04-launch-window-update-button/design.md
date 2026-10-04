# Design: Launch Window Update Button

## Context

The footer today has two elements: a passive version chip (`VersionChipBorder`/`VersionChipText`) and a "Check for updates" button (`FooterTextButton` + `LaunchWindowViewModel.CheckForUpdatesCommand`) that only runs `IUpdateService.CheckForUpdatesAsync()` and reports the outcome in `StatusMessage`.

Existing update surfaces share state through `IUpdateService`: `LastKnownAvailableVersion`, `IsDownloading`, and the `UpdateStateChanged` event (raised on a background thread). `App.xaml.cs` subscribes to it, refreshes the tray menu, and owns the one-step tray/What's New flow (`DownloadAndApplyUpdateAsync`: download then immediately restart). `WhatsNewViewModel` shows the established pattern for a state-reflecting consumer: subscribe to `UpdateStateChanged`, marshal to the dispatcher, re-read the service properties.

## Goals / Non-Goals

**Goals:**

- One footer control that shows the current version when idle and acts per the states defined in the `update-checking` delta spec (idle, checking, update available, downloading, restart ready).
- The control reflects updates found by the automatic startup check without user action.
- A two-step flow for the launch window only: click 1 downloads, click 2 applies and restarts.
- Shared-state correctness: no second download when one was started from the tray or What's New.

**Non-Goals:**

- Changing the tray menu or the What's New window flows (they stay one-step download-and-restart).
- Changing the Settings window UPDATES block.
- Adding download progress percentages (`IUpdateService` has no progress API; a textual "Downloading…" state is enough).
- Persisting "update downloaded" across application restarts.

## Decisions

### 1. View-model state machine, no new Core abstraction

`LaunchWindowViewModel` owns a private state machine: `Idle`, `Checking`, `Available`, `Downloading`, `ReadyToApply`. Exposed to the view as bound properties (`UpdateButtonText`, `IsUpdateActionAvailable` for accent styling, `UpdateControlCommand`).

- *Why*: the states are presentation concerns of one control; `WhatsNewViewModel` already demonstrates this per-consumer pattern. Alternative (a shared `UpdateStateCoordinator` service) is rejected as over-engineering while only two shapes of consumption exist.
- `Available`/`Downloading` derive from the service (`LastKnownAvailableVersion`, `IsDownloading`) so downloads started anywhere are reflected. `ReadyToApply` is tracked view-model-locally: the VM keeps the completed `DownloadUpdateAsync` result and flips to `ReadyToApply` when it reports success.
- The pure transition logic lives in `CLIHub.Core` (`UpdateControlState` enum in Models, static `UpdateControlLogic.Derive`/`AfterDownload` in Services) so `CLIHub.Tests` - which references Core only - can cover the transitions without referencing the WPF project. The VM owns the async orchestration and status messages; the Core class is side-effect-free state derivation.
- *Why VM-local for `ReadyToApply`*: the tray/What's New flows restart immediately on completion, so no other surface needs a persistent "downloaded" flag, and `IUpdateService` stays unchanged (smaller blast radius). Edge case - a completed download with the window never reopened before restart - is acceptable: restarting applies the pending update anyway via Velopack.

### 2. Two-step flow, reusing existing service methods

Click on `Available` calls `_updateService.DownloadUpdateAsync()` (the guard against double downloads is `IsDownloading`, shared with tray/What's New). On success the state moves to `ReadyToApply`; a second click calls `_updateService.ApplyDownloadedUpdateAndRestart()` inside try/catch - failures land in `StatusMessage` and the state returns to `Available`. On download failure the state returns to `Available` and `StatusMessage` reports it.

### 3. State refresh via `UpdateStateChanged`

The VM subscribes to `IUpdateService.UpdateStateChanged` and re-derives its state on the dispatcher (the `WhatsNewViewModel` pattern). This covers the automatic startup check (fired before or around window creation - the VM also derives its initial state once in the constructor after the subscription), downloads started elsewhere, and download completion/failure. The subscription lives as long as the window; the window is created once per application run, so no explicit unsubscribe lifetime problem arises (matching `WhatsNewViewModel`, which unsubscribes on window close - the launch window never closes, so the VM keeps the subscription).

### 4. XAML: merge into one styled button

The version chip and the button are replaced by a single `Button` bound to `UpdateButtonText`/`UpdateControlCommand`, styled by an extended `FooterTextButton` with a `DataTrigger` on `IsUpdateActionAvailable` that switches border and text to the accent brush (same accent treatment as the hotkey field focus border - quieter than a filled accent button, consistent with the footer's chip language). Idle look stays the current muted chip look, so the merged control reads exactly like today's version chip in the resting state.

- *Alternative considered*: two overlaid elements with visibility switching - rejected as redundant; one control with triggers is simpler.

### 5. Status messages stay where they are

Verbose outcomes (up to date, check failure/timeout, not-installed note) keep using the existing `StatusMessage` footer line and wording; the control itself only carries the short state labels. `CheckForUpdatesCommand` is renamed to `UpdateControlCommand` (it now routes by state); no other consumers exist.

### 6. Shared `UpdateControlViewModel` ready for more surfaces

The state machine, labels, and command live in a dedicated `UpdateControlViewModel` (CLIHub project, `ObservableObject`-based) that wraps `IUpdateService` and exposes `UpdateButtonText`, `IsUpdateActionAvailable`, `UpdateControlCommand`, and an `OutcomeReported` event carrying verbose outcome strings. The launch window forwards the messages to its `StatusMessage` line and styles the accent treatment with its own trigger. The pure transition math stays in Core (`UpdateControlLogic`), so the shared component is thin orchestration. The component is host-agnostic on purpose: the Settings window's UPDATES block is out of scope for this change, but adopting it later means constructing the same component and styling the trigger.

- *Alternative considered*: keeping the state machine inline in `LaunchWindowViewModel` - rejected; extracting now keeps the future Settings integration to wiring only.

## Risks / Trade-offs

- [Startup check may finish before the VM subscribes] → the VM derives its initial state from `LastKnownAvailableVersion`/`IsDownloading` in the constructor, not only from events.
- [`ApplyDownloadedUpdateAndRestart` throws on failure] → wrapped in try/catch; failure is reported in `StatusMessage` and the control returns to `Available`.
- [Non-managed install (dev builds)] → checks return `NotInstalled`, `LastKnownAvailableVersion` stays null, so the control never leaves idle; `StatusMessage` keeps the existing "Updates apply to installed builds only." note.
- [Accent treatment may read as two different controls] → accent is applied only to border and text on the same chip shape; verified visually against the footer palette.
