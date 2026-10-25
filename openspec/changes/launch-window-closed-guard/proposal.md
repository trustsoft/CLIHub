# Ignore show requests after the launch window has closed (exit crash)

## Why

Production crash: an unhandled `InvalidOperationException` kills the process during exit.

```text
System.InvalidOperationException: Cannot set Visibility or call Show, ShowDialog, or
WindowInteropHelper.EnsureHandle after a Window has closed.
   at System.Windows.Window.VerifyCanShow()
   at System.Windows.Window.CreateSourceWindow(Boolean duringShow)
   at System.Windows.Interop.WindowInteropHelper.EnsureHandle()
   at CLIHub.Interop.WindowPositioner.Place(...) in src/CLIHub/Interop/WindowPositioner.cs:line 30
   at CLIHub.Views.LaunchWindow.ShowOnPointerMonitor() in src/CLIHub/Views/LaunchWindow.xaml.cs:line 66
   at CLIHub.Tray.TrayIconController.ShowLaunchWindow() in src/CLIHub/Tray/TrayIconController.cs:line 59
   at CLIHub.Tray.TrayIconController.<.ctor>b__7_0(...) in src/CLIHub/Tray/TrayIconController.cs:line 48
```

The race, verified in code:

1. The tray Exit command calls `Application.Current?.Shutdown()`
   (`src/CLIHub/Tray/TrayIconController.cs:133`); `App.xaml` uses
   `ShutdownMode="OnExplicitShutdown"` (`src/CLIHub/App.xaml:6`), so this is the normal exit path.
2. WPF application shutdown first closes every window; documented WPF behavior is that the `Closing`
   handler is raised but its cancellation is ignored, so the singleton `LaunchWindow` — whose
   `OnClosing` normally cancels and hides (`src/CLIHub/Views/LaunchWindow.xaml.cs:280-286`) —
   transitions to closed. The tray icon is still alive at this point: it is disposed only later, in
   `App.OnExit` → `ApplicationHost.Shutdown` → service-provider disposal
   (`src/CLIHub/App.xaml.cs:71-76`, `src/CLIHub/Startup/ApplicationHost.cs:87-124`, documented in
   `docs/architecture/startup.md:82-102`).
3. During that gap the dispatcher still pumps, and a tray left-mouse-up
   (`src/CLIHub/Tray/TrayIconController.cs:48`) — or a second-instance activation dispatched by
   `ShellCoordinator` (`src/CLIHub/Startup/ShellCoordinator.cs:90`, unsubscribed only at session
   disposal) — reaches `LaunchWindow.ShowOnPointerMonitor()`, which calls
   `WindowPositioner.Place` on the closed window. `Place` calls
   `WindowInteropHelper.EnsureHandle()` (`src/CLIHub/Interop/WindowPositioner.cs:30`), and WPF
   throws.

A second, latent instance of the same fault: `ShowOnPointerMonitor` queues a
`DispatcherPriority.Loaded` callback that calls `WindowPositioner.Place` again after layout settles
(`src/CLIHub/Views/LaunchWindow.xaml.cs:86-91`); if the window closes before that callback runs, it
throws the same way.

`WindowPositioner.Place` has exactly two callers, both in `LaunchWindow`
(`src/CLIHub/Views/LaunchWindow.xaml.cs:66,88`).

## What Changes

1. `LaunchWindow` tracks that it has closed: a `closed` field set in an `OnClosed` override.
   `Closed` is the single funnel that runs exactly once when the window really closes — including
   the forced close during application shutdown — and a closed window is never reopened in this
   application (the only close path is application exit; see design D1).
2. `ShowOnPointerMonitor` ignores the request when the window is already closed: an early return
   as the first statement, before the preferences load and before any placement, so a late request
   does no work at all.
3. The deferred placement callback is guarded by the same state: the queued operation calls a
   guarded `PlaceWhenOpen(PopupScreenPosition)` instance method instead of the ungated static
   lambda.
4. No `try/catch` is added anywhere. `WindowPositioner` is untouched; real positioning errors stay
   loud.

Normal show behavior is unchanged: on a window that has not closed, every step runs exactly as
today. The only observable change is that a request which today crashes the process is ignored.

Spec delta: MODIFIED requirement *Launch window popup shell* in `app-lifecycle` — every existing
scenario name preserved verbatim, one new scenario for the ignored-after-close contract.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `app-lifecycle`: requirement *Launch window popup shell* (`openspec/specs/app-lifecycle/spec.md:109-147`)
  gains the contract that a show request arriving after the launch window has closed (application
  shutdown) is ignored without positioning, showing, or throwing.

## Impact

- Code: `src/CLIHub/Views/LaunchWindow.xaml.cs` only (field + `OnClosed` override, guard in
  `ShowOnPointerMonitor`, guarded deferred placement). `WindowPositioner`, `TrayIconController`,
  shutdown ordering, and `OnClosing` are untouched.
- Tests: new `tests/CLIHub.Tests/Views/LaunchWindowClosedGuardTests.cs` using the existing STA
  thread pattern (`tests/CLIHub.Tests/Controls/SpacedPanelTests.cs:173-192`), the
  `LaunchWindowViewModelBuilder`, and a mocked `IPreferencesStore`; internal seam is visible to the
  test assembly (`src/CLIHub/CLIHub.csproj:25`).
- Docs: `CHANGELOG.md` Fixed entry. `docs/architecture/startup.md` needs no change — the shutdown
  ordering it documents is unchanged; the change only makes the launch window tolerate the
  documented ordering.
- Specs: `app-lifecycle` delta in this change.
- Risk: low. The guard is additive and reads a flag that is false for the entire normal lifetime of
  the window; the single regression risk (a close that is not application exit) is ruled out by the
  close paths in this codebase (design D1). Residual risk: automated tests must construct a real
  `LaunchWindow` under STA without an `Application`; the fallback is documented in the tasks and
  design (open question 2).

## Non-goals

- No change to shutdown ordering and no earlier tray-icon disposal — the documented
  `app-lifecycle` *Graceful shutdown* sequence stays as is.
- No change to `LaunchWindow.OnClosing` (hide-instead-of-close) or to any normal
  show/positioning behavior.
- No broad `catch` in `WindowPositioner` and no swallowed exceptions anywhere — the fix prevents
  the call, it does not mask failures after it happens.
- No window recreation after close, and no dispatcher-shutdown detection
  (`Dispatcher.HasShutdownStarted` is false across the crash window; design "Alternatives").
- No reworking of the hotkey path: `GlobalHotkeyService` hooks the launch window's own HWND
  (`src/CLIHub/Hotkeys/GlobalHotkeyService.cs:41`), which is destroyed when the window closes, so
  the hotkey cannot deliver a show request after close.

## Dependencies

None on other active changes (`openspec/changes/` holds no other active change). The change is
self-contained in the launch window and aligns with the shutdown contract already specified by
`app-lifecycle` *Graceful shutdown* (`openspec/specs/app-lifecycle/spec.md:57-76`).
