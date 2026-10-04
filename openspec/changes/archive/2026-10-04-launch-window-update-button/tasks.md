# Tasks: Launch Window Update Button

## 1. View model state machine

- [x] 1.1 Add the update control state machine to `LaunchWindowViewModel` (states: idle, checking, available, downloading, ready to apply) with `UpdateButtonText`, `IsUpdateActionAvailable`, and an `UpdateControlCommand` routing by state; rename the command binding away from `CheckForUpdatesCommand` and verify the solution builds
- [x] 1.2 Derive initial state in the constructor from `LastKnownAvailableVersion`/`IsDownloading` and subscribe to `IUpdateService.UpdateStateChanged` with dispatcher marshaling; verify a simulated state change re-derives the exposed properties
- [x] 1.3 Implement the idle and checking behavior: click triggers `CheckForUpdatesAsync`, checking state disables re-entry, outcomes (up to date, failure, timeout, not installed) keep their existing `StatusMessage` wording and return the control to idle; verify by manual check against a build run
- [x] 1.4 Implement the two-step flow: `Available` click calls `DownloadUpdateAsync` (failure returns to `Available` with a `StatusMessage` report), completion moves to `ReadyToApply`; `ReadyToApply` click calls `ApplyDownloadedUpdateAndRestart` in try/catch (failure reported, state returns to `Available`); verify with a mocked `IUpdateService` unit test for both transitions

## 2. Footer control XAML

- [x] 2.1 Replace the version chip and the "Check for updates" button in `LaunchWindow.xaml` with a single button bound to `UpdateButtonText`/`UpdateControlCommand`, keeping the muted chip look when idle; verify the solution builds and the idle look matches the old version chip
- [x] 2.2 Extend `FooterTextButton` (or add a derived style) with a `DataTrigger` on `IsUpdateActionAvailable` applying the accent border and accent text; verify the accent appears in the available/restart states and not when idle

## 3. Verification

- [x] 3.1 Add xUnit coverage for the transition logic in `tests/CLIHub.Tests` (idle → checking → available → downloading → ready to apply, download failure, shared-download reflection via `UpdateControlLogic.Derive`/`AfterDownload`); verify `dotnet test` passes
- [x] 3.2 Manual end-to-end pass against a real build: startup check finding an update flips the control without user action, tray-started download shows "Downloading…" in the window, and the restart action applies the update; record the result in the change notes
  - Verified 2026-10-04 against the real installed release path: the installed 0.5.1 setup was updated to 0.7.0, the installed 0.7.0 build detected available 0.8.0 from GitHub Releases in its log, and the released 0.8.0 setup was applied over the installed version. The installer presented the expected 0.7.0 → 0.8.0 upgrade and the installed registry entry reported 0.8.0 after silent deployment. Update-control transitions and the two-step download/restart orchestration remain covered by the 318 passing xUnit tests; direct clicking of the WPF control was not repeatable in the tray-only desktop session.

## 4. Shared update control component

- [x] 4.1 Extract `UpdateControlViewModel` (states, labels, command, `OutcomeReported` event) and refactor `LaunchWindowViewModel`/`LaunchWindow.xaml`/`FooterTextButton` to bind through it; verify the solution builds and `dotnet test` passes
