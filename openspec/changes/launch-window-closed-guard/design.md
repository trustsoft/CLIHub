# Design: launch-window-closed-guard

## Current state (verified in code)

| Concern | Today | File |
|---|---|---|
| Crash site | `WindowPositioner.Place` resolves the window handle with `new WindowInteropHelper(window).EnsureHandle()`; on a closed window this throws `InvalidOperationException` (`VerifyCanShow` → `CreateSourceWindow`) | `src/CLIHub/Interop/WindowPositioner.cs:28-30` (second `EnsureHandle` at `:178`) |
| `Place` callers | Exactly two, both in `LaunchWindow`: the direct call in `ShowOnPointerMonitor` and the deferred dispatcher callback | `src/CLIHub/Views/LaunchWindow.xaml.cs:66,88` (grep: no other call sites) |
| Show path | `ShowOnPointerMonitor` loads the popup position preference, calls `Place`, refreshes projects/agents, applies the default agent selection, `Show()`, restores a minimized state, records `shownAt`, `Activate()`, then queues a second `Place` at `DispatcherPriority.Loaded` | `src/CLIHub/Views/LaunchWindow.xaml.cs:62-92` (queue at `:86-91`) |
| Deferred placement | Static lambda `(window, popupPosition) => WindowPositioner.Place(window, popupPosition)` runs after the layout pass; nothing checks whether the window is still open when it runs | `src/CLIHub/Views/LaunchWindow.xaml.cs:86-91` |
| Close behavior | `OnClosing` sets `e.Cancel = true` and hides; the window has no `OnClosed` override and no closed-state field anywhere in the file | `src/CLIHub/Views/LaunchWindow.xaml.cs:280-286` (full file `:1-287`) |
| Exit trigger | Tray Exit command: `Application.Current?.Shutdown()`; `App.xaml` sets `ShutdownMode="OnExplicitShutdown"`, so the app only ends through `Application.Shutdown` | `src/CLIHub/Tray/TrayIconController.cs:133`, `src/CLIHub/App.xaml:6` |
| Forced close | WPF application shutdown closes all windows; per the documented `Window.Closing` / `Application.Shutdown` behavior, the `Closing` handler is raised but its cancellation is ignored, so `LaunchWindow` transitions to closed during exit. (`OnClosing`'s `Hide()` still runs on a then-open window and is harmless; Exit works in production today.) | Documented WPF behavior; observable in this repo as: Exit exits, and the crash trace shows a closed window |
| Tray lifetime | `TrayIconController` (and its `TaskbarIcon`) is a DI singleton disposed only when the service provider is disposed, in `App.OnExit` → `ApplicationHost.Shutdown` | `src/CLIHub/App.xaml.cs:71-76`, `src/CLIHub/Startup/ApplicationHost.cs:87-124` (provider disposal `:121`), `src/CLIHub/Tray/TrayIconController.cs:141-146`, `docs/architecture/startup.md:82-102` |
| Tray entry path | `TrayLeftMouseUp` → `ShowLaunchWindow()` → `launchWindow.ShowOnPointerMonitor()` | `src/CLIHub/Tray/TrayIconController.cs:48,57-60` |
| Second-instance entry path | `SingleInstanceGuard.ActivationRequested` → `context.Dispatch(startupUi.ShowLaunchWindow)` → `WpfApplicationStartupUi.ShowLaunchWindow` → tray → `ShowOnPointerMonitor`. The subscription is removed only in `ShellCoordinator.Dispose`, which runs at session disposal — inside `App.OnExit`, after the windows have already closed | `src/CLIHub/Startup/ShellCoordinator.cs:90,108,140-143`, `src/CLIHub/Startup/WpfApplicationStartupUi.cs:37`, `src/CLIHub/Startup/ApplicationHost.cs:101` |
| Menu entry path | Tray menu "Show" click → `commands.ShowLaunchWindow` → `TrayIconController.ShowLaunchWindow` | `src/CLIHub/Tray/TrayMenuBuilder.cs:68`, `src/CLIHub/Tray/TrayIconController.cs:132` |
| Hotkey path (not live after close) | `GlobalHotkeyService` registers `WM_HOTKEY` against the launch window's own HWND and hooks its `HwndSource`; when the window closes the HWND is destroyed, so no hotkey message can invoke `ToggleLaunchWindow` afterwards | `src/CLIHub/Hotkeys/GlobalHotkeyService.cs:36-45` (`EnsureHandle` at `:41`, `AddHook` at `:44`), registration `src/CLIHub/Composition/ServiceRegistration.cs:140-143` |
| Toggle path | `ToggleLaunchWindow` hides when `IsVisible`, otherwise shows. A closed window reports `IsVisible == false`, so the hide branch (whose `Hide()` call would also throw on a closed window) is not reachable after close; the show branch funnels into the same guard | `src/CLIHub/Tray/TrayIconController.cs:65-75` |
| Startup show path | `ApplicationBootstrapper` optionally calls `startupUi.ShowLaunchWindow()` once during startup, long before any close can occur | `src/CLIHub/Startup/ApplicationBootstrapper.cs:72` |

Crash window, concretely: `Application.Shutdown()` → WPF closes `LaunchWindow` (forced; `Closing`
cancellation ignored) → … dispatcher still pumping; tray icon and second-instance subscription still
live … → `App.OnExit` → `ApplicationHost.Shutdown` → session disposal removes the activation
subscription → provider disposal disposes the tray icon. Any `ShowLaunchWindow` delivered in the
first stretch reaches `WindowPositioner.Place` on a closed window and throws; the exception escapes
the tray callback and kills the process.

## Governing requirements (quoted)

- `openspec/specs/app-lifecycle/spec.md:109-147` — *Launch window popup shell*: "The launch window
  SHALL behave as a popup shell that stays out of the way and can be pinned open." Scenario
  **Positioned on the pointer's monitor**: "WHEN the launch window is shown from the tray, the
  global hotkey, a second-instance activation, or startup / THEN it is positioned according to the
  configured popup screen position…". The requirement says nothing about requests that arrive after
  the window has closed — today they crash, which is why the requirement is MODIFIED rather than
  merely extended by clarification.
- `openspec/specs/app-lifecycle/spec.md:57-76` — *Graceful shutdown*: "Normal shutdown: WHEN the
  user chooses Exit / THEN application-scoped operations are cancelled and awaited within the
  shutdown bound / AND the tray icon is disposed, the DI container is disposed, and the
  single-instance mutex is released." The shutdown ordering (tray disposed at provider disposal) is
  contractual; this change must not alter it.
- `openspec/specs/app-lifecycle/spec.md:206-225` — *First-instance session owns application event
  subscriptions*: "later update or activation events do not invoke the disposed coordinator
  handlers" (after session disposal). Subscription removal happens at session disposal — after the
  windows have closed — confirming the second-instance path is live inside the crash window.

## D1 — Track closed state in `LaunchWindow.OnClosed` (DECIDED)

- Add `private bool closed;` and override `OnClosed`:

  ```csharp
  protected override void OnClosed(EventArgs e)
  {
      this.closed = true;
      base.OnClosed(e);
  }
  ```

  The flag is set before `base.OnClosed(e)` so any `Closed`-subscriber reentrancy already observes
  it. XML documentation per the repository style.
- **Why `OnClosed`, not `OnClosing`:** during the forced shutdown close, `Closing` cancellation is
  ignored (verified current state); `Closed` runs exactly once for every real close, forced or not.
  The flag therefore marks the actual terminal state.
- **Why an explicit flag:** WPF exposes no public `IsClosed`; `IsLoaded`, `IsActive`, or handle
  checks are implicit proxies that depend on dispatcher timing. The explicit flag is the actual
  invariant ("this window can no longer be shown").
- **Can `closed` ever be set while the application keeps running?** No close path can produce that
  state in this codebase: `ShutdownMode="OnExplicitShutdown"` (`src/CLIHub/App.xaml:6`) means a
  main-window close never ends or closes anything by itself; `LaunchWindow.OnClosing` cancels every
  ordinary close attempt (`src/CLIHub/Views/LaunchWindow.xaml.cs:280-286`); the only remaining close
  is the forced one during `Application.Shutdown`. So "closed" implies "the process is exiting", and
  ignoring show requests is the correct semantic (there is nothing to show and nothing to keep
  alive for).
- **Thread-safety:** the flag is written on the UI thread (`OnClosed`) and read on the UI thread
  (tray/menu/activation callbacks are dispatcher-marshalled: `TrayLeftMouseUp` arrives on the tray
  icon's dispatcher thread, `ShellCoordinator` routes activation through `context.Dispatch`
  (`src/CLIHub/Startup/ShellCoordinator.cs:90`)). No cross-thread access is introduced; a plain
  field is sufficient.

## D2 — `ShowOnPointerMonitor` ignores requests after close (DECIDED)

`if (this.closed) { return; }` becomes the first statement of `ShowOnPointerMonitor`, before
`this.preferencesStore.Load()` (`src/CLIHub/Views/LaunchWindow.xaml.cs:64`). "Ignore" means no work
at all: no preference read, no placement, no view-model refresh, no `Show`/`Activate`. This directly
removes the production crash path and, through the shared funnel, also covers the second-instance
activation path, the tray menu path, and the show branch of `ToggleLaunchWindow`
(`src/CLIHub/Tray/TrayIconController.cs:73`). The startup call
(`src/CLIHub/Startup/ApplicationBootstrapper.cs:72`) always runs with `closed == false`, so startup
behavior is untouched.

## D3 — Guard the deferred placement callback with the same state (DECIDED)

Replace the ungated static lambda with a guarded instance method used for the deferred placement:

```csharp
internal void PlaceWhenOpen(PopupScreenPosition position)
{
    if (this.closed)
    {
        return;
    }

    WindowPositioner.Place(this, position);
}
```

and queue it from `ShowOnPointerMonitor`:

```csharp
this.Dispatcher.BeginInvoke(
    new Action<PopupScreenPosition>(this.PlaceWhenOpen),
    DispatcherPriority.Loaded,
    position);
```

- **Why an instance method:** the guard reads the same `closed` field as D2 (one predicate, no
  duplicated state exposed to the lambda), and the method is directly testable —
  `InternalsVisibleTo("CLIHub.Tests")` already exists (`src/CLIHub/CLIHub.csproj:25`), matching the
  internal-seam precedent of `ApplicationHost`'s internal test constructor
  (`src/CLIHub/Startup/ApplicationHost.cs:36-39`).
- **Priority unchanged:** the callback stays at `DispatcherPriority.Loaded`; only the guard is
  added. If the window closes between `Show` and the callback, the callback is now a no-op instead
  of an unhandled dispatcher exception.
- **Rejected variant:** keeping the static lambda and pattern-matching `window is LaunchWindow {…}`
  with an exposed property. It widens the type's surface for no benefit over the instance method.

## D4 — No `try/catch` in `WindowPositioner` (DECIDED)

The positioner is a low-level interop helper whose failures (monitor enumeration, `SetWindowPos`)
should stay loud. A catch-all there would mask genuine positioning defects and convert the helper
into a silent-ignore utility for every future caller. The caller-level guard expresses the real
invariant: *do not position a closed window*. `WindowPositioner` is not modified by this change.

## D5 — Leave shutdown ordering, tray lifetime, and `OnClosing` alone (DECIDED)

The brief's non-goals, confirmed by the verification above: the shutdown sequence is contractual
(*Graceful shutdown*; `docs/architecture/startup.md:82-102`), and reordering it (e.g. disposing the
tray icon before windows close) would be a wider behavioral change with its own risks, while still
leaving the deferred-placement fault unguarded. `OnClosing`'s hide-instead-of-close behavior is the
launch window's normal UX and stays exactly as is.

## Alternatives considered and rejected

- **`try/catch` around the `Place` call sites (or inside `Place`):** turns a logic error into a
  swallowed exception; catches more than the known fault (e.g. monitor-API failures) and hides them.
- **Dispose the tray icon earlier / reorder shutdown:** violates the spec'd *Graceful shutdown*
  ordering and the documented shutdown sequence; much larger blast radius (session, operations,
  provider disposal), and does not fix the deferred-placement fault.
- **Check `Dispatcher.HasShutdownStarted` or `Application.Current` in `ShowOnPointerMonitor`:** the
  crash window sits *inside* `Application.DoShutdown` — windows are closed and `App.OnExit` (where
  the tray is still alive) runs before dispatcher shutdown begins — so `HasShutdownStarted` is
  `false` across exactly the interval that crashes. The wrong predicate; `closed` is the precise
  one.
- **Recreate the window when a show request arrives after close:** the window is a DI singleton
  wired into the tray, hotkey, and shell coordinator (`src/CLIHub/Composition/ServiceRegistration.cs:140-143`),
  and the process is exiting when the state occurs; recreation is both wrong and unsafe mid-shutdown.
- **Rely on `WindowInteropHelper.Handle == IntPtr.Zero`:** not a documented "is closed" signal; the
  handle value after close is an implementation detail. The explicit flag is deterministic.

## Test approach

New `tests/CLIHub.Tests/Views/LaunchWindowClosedGuardTests.cs`:

- STA execution via the thread-spawning pattern already used in the suite
  (`tests/CLIHub.Tests/Controls/SpacedPanelTests.cs:173-192`) — no new package, no
  `Application.Current` required.
- Window construction: `LaunchWindowViewModelBuilder.Build()`
  (`tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs:230`), `new PromptState()`
  (parameterless, `src/CLIHub/ViewModels/Shared/PromptState.cs:7-14`), and a Moq
  `IPreferencesStore` returning `new AppPreferences()` (default `PopupScreenPosition.Center`,
  `src/CLIHub.Core/Models/Configuration/AppConfig.cs:125`).
- **A genuinely closed window is required.** Merely invoking `OnClosed` on an open window does not
  put WPF into the closed state (`EnsureHandle` would still succeed), so the tests use a small
  derived `CloseableLaunchWindow : LaunchWindow` whose `OnClosing` override does not cancel (does
  not call the base override), letting `Close()` really close the window — which raises the
  production `OnClosed` and sets the flag.
- Test 1 — `ShowOnPointerMonitor_AfterWindowClosed_IgnoresRequestWithoutLoadingPreferences`:
  construct, close for real, call `ShowOnPointerMonitor()`, assert no exception and
  `preferencesStore.Load` was never called (proves the early return happens before any work; today
  the same call throws the production `InvalidOperationException`).
- Test 2 — `PlaceWhenOpen_AfterWindowClosed_IgnoresDeferredPlacement`: same closed window, call
  `PlaceWhenOpen(PopupScreenPosition.Center)` directly, assert no exception (unguarded, this is the
  exact deferred-callback fault).
- Neither test calls `Show()`, so the window's item templates — which reference application-level
  converters assigned at runtime (`src/CLIHub/Startup/ApplicationHost.cs:131-139`, app-level keys in
  `src/CLIHub/App.xaml:36-47`) — are never instantiated. Window-scoped static resources resolve
  standalone: `LaunchWindow.xaml` merges `LaunchWindowStyles.xaml`
  (`src/CLIHub/Views/LaunchWindow.xaml:27-32`), which merges `Controls.xaml` and transitively the
  sizing tokens (`src/CLIHub/Themes/LaunchWindowStyles.xaml:14-16`); palette brushes are
  `DynamicResource` (`src/CLIHub/Views/LaunchWindow.xaml:9`) and defer resolution.
- If construction without an `Application` still fails at implementation time (open question 2),
  the documented fallback is: keep the regression proof on whatever narrower seam works, mark the
  window-level proof as manual-smoke-only with the reason recorded, and do not add an `Application`
  fixture beyond what the suite already avoids (`tests/CLIHub.Tests/Services/WpfThemePreferenceTargetTests.cs`
  precedent: the test host has no `Application`).
- Manual smoke (user-confirmed) covers the live race and the unchanged normal paths, because the
  production race itself (tray click inside the shutdown gap) is not deterministic in a unit test.

## Deviations

- None from the brief. The brief's fix direction was verified against the code and adopted as
  proposed; the only addition is the named internal seam `PlaceWhenOpen` (D3) so the deferred-guard
  is testable, which is an implementation shape, not a behavior deviation.

## Open questions

1. Manual smoke scope confirmation: the crash race (tray click during exit) is probabilistic — is a
   best-effort attempt (rapid clicks during exit, repeated a few times, plus a second-instance
   launch during exit) with a clean-process-exit check acceptable as the user-confirmed gate?
2. Does `LaunchWindow` construct cleanly under STA without an `Application`? The resource analysis
   above says yes (window-scoped statics resolve; templates deferred), but it must be confirmed the
   first time the test runs; fallback documented in Test approach.
3. CHANGELOG placement: `Fixed` entry under a new version heading vs an Unreleased section — follow
   the maintainer's convention at implementation time (top of `CHANGELOG.md` currently starts at
   `## 1.0.1 — 2026-10-24`).
