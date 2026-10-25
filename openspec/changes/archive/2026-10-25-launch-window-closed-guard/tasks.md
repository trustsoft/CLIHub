# Tasks: launch-window-closed-guard

Stages are sized for a fresh session. Each stage ends green: `dotnet build CLIHub.sln` and
`dotnet test CLIHub.sln`. No stage commits; the user decides about commit and archive.

## Stage 1: Closed-state tracking and guards in `LaunchWindow`

**Goal**: a show request that arrives after the launch window has closed is ignored, at both the
entry point and the deferred placement, with no `try/catch` added anywhere.

- [x] 1.1 In `src/CLIHub/Views/LaunchWindow.xaml.cs`: add `private bool closed;` and an
      `OnClosed(EventArgs e)` override that sets it (before `base.OnClosed(e)`, per design D1) with
      XML documentation explaining that the window cannot be reopened after application shutdown
      has closed it.
- [x] 1.2 Make `ShowOnPointerMonitor` return immediately when `this.closed` is true, as the first
      statement — before `this.preferencesStore.Load()` (`src/CLIHub/Views/LaunchWindow.xaml.cs:64`)
      — so an ignored request does no work at all (design D2). Update the method's XML doc to state
      that requests after close are ignored.
- [x] 1.3 Add `internal void PlaceWhenOpen(PopupScreenPosition position)` that returns when
      `this.closed` and otherwise calls `WindowPositioner.Place(this, position)` (design D3; XML
      doc: used for the deferred placement that runs after `Show` and may arrive after close).
      Change the deferred queue in `ShowOnPointerMonitor` (`:86-91`) to
      `this.Dispatcher.BeginInvoke(new Action<PopupScreenPosition>(this.PlaceWhenOpen),
      DispatcherPriority.Loaded, position)` — priority and behavior for an open window unchanged.
- [x] 1.4 Confirm `WindowPositioner`, `TrayIconController`, `OnClosing`
      (`src/CLIHub/Views/LaunchWindow.xaml.cs:280-286`), and the shutdown sequence are untouched.
- [x] 1.5 Gate: `dotnet build CLIHub.sln` then `dotnet test CLIHub.sln` — zero failures
      (existing suite unchanged and green).

## Stage 2: Regression tests

**Goal**: automated proof of both guarded paths on a genuinely closed window, using the existing
STA pattern; no new packages, no `Application` fixture.

- [x] 2.1 Create `tests/CLIHub.Tests/Views/LaunchWindowClosedGuardTests.cs` with a private STA
      runner copied from the suite's pattern (`tests/CLIHub.Tests/Controls/SpacedPanelTests.cs:173-192`),
      a private `CloseableLaunchWindow : LaunchWindow` whose `OnClosing` override does not cancel
      (so `Close()` really closes the window and raises the production `OnClosed`), and a private
      factory building the window with `LaunchWindowViewModelBuilder.Build()`
      (`tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs:230`), `new PromptState()`, and a
      Moq `IPreferencesStore` whose `Load()` returns `new AppPreferences()` (default
      `PopupScreenPosition.Center`). Follow the `MethodOrScenario_Condition_ExpectedResult` naming.
      *(Superseded by the 2.4 fallback — see the recorded reason below. The STA runner is in place;
      the `CloseableLaunchWindow` factory is not.)*
- [x] 2.2 Add `ShowOnPointerMonitor_AfterWindowClosed_IgnoresRequestWithoutLoadingPreferences`:
      close the window for real, call `ShowOnPointerMonitor()`, assert no exception and
      `Verify(x => x.Load(), Times.Never)` on the preferences store. Sanity-check the proof: with
      the guard removed this call must throw the production `InvalidOperationException`
      (temporarily comment the guard, watch it fail, restore).
      *(Superseded by the 2.4 fallback — a `LaunchWindow` cannot exist in the test host, so this
      window-level test and its comment-the-guard sanity check are not automatable; the window-level
      proof is manual-smoke-only in task 3.3, whose "no crash dialog" check is the live equivalent
      of the sanity check.)*
- [x] 2.3 Add `PlaceWhenOpen_AfterWindowClosed_IgnoresDeferredPlacement`: on the same kind of
      closed window, call `PlaceWhenOpen(PopupScreenPosition.Center)` directly and assert no
      exception (the internal seam is visible via `src/CLIHub/CLIHub.csproj:25`).
      *(Superseded by the 2.4 fallback — same reason as 2.2; the `PlaceWhenOpen` seam itself is
      unchanged in the product code and covered by the task 3.3 manual smoke.)*
- [x] 2.4 If `LaunchWindow` cannot be constructed without an `Application` (design open question 2):
      record the failure reason in this change's tasks, keep the guard tests on the narrowest seam
      that works, and mark the window-level proof as manual-smoke-only rather than adding an
      `Application` fixture (precedent: the test host has no `Application`,
      `tests/CLIHub.Tests/Services/WpfThemePreferenceTargetTests.cs`).
      **Recorded reason (2026-10-25, two independent failures):**
      1. The design's `CloseableLaunchWindow : LaunchWindow` cannot be constructed at all: WPF's
         generated `InitializeComponent` calls `Application.LoadComponent(this,
         "/CLIHub;component/views/launchwindow.xaml")`, which rejects a derived-type instance with
         `System.Exception: The component '...CloseableLaunchWindow' does not have a resource
         identified by the URI '/CLIHub;component/views/launchwindow.xaml'`.
      2. The exact type `new LaunchWindow(...)` also cannot be constructed in the test host: the
         XAML root element (`LaunchWindow.xaml:10`) resolves
         `FontFamily="{StaticResource Sizing.FontFamily.Text}"` before any window-scoped merged
         dictionary exists, and that key lives only in application-level resources
         (`Themes/Sizing.xaml`, merged in `App.xaml`), so BAML load throws
         `XamlParseException: Cannot find resource named 'Sizing.FontFamily.Text'` when
         `Application.Current` is null. Adding an `Application` fixture is ruled out by this task.
      *Narrowest working seam:* `tests/CLIHub.Tests/Views/LaunchWindowClosedGuardTests.cs` keeps
      the automated proof on plain `Window` instances (no XAML, no `Application`):
      `EnsureHandle_AfterWindowClosed_ThrowsInvalidOperationException` proves that the exact interop
      call `WindowPositioner.Place` makes (`EnsureHandle`) throws the production
      `InvalidOperationException` on a genuinely closed window while succeeding on an open one —
      the discriminating state the guards exploit — and
      `Closed_AfterWindowReallyCloses_RunsExactlyOnce` proves `Closed` runs exactly once for a real
      close, the funnel the `OnClosed` override consumes. The `LaunchWindow`-level guard behavior
      (`ShowOnPointerMonitor` ignoring the request before `preferencesStore.Load()` and
      `PlaceWhenOpen` no-oping) is manual-smoke-only in task 3.3.*
- [x] 2.5 Gate: `dotnet build CLIHub.sln` then `dotnet test CLIHub.sln` — zero failures, new tests
      included.

## Stage 3: Docs and verification

**Goal**: recorded, validated, and user-verified; graph refreshed.

- [x] 3.1 Add a `Fixed` entry to `CHANGELOG.md` (placement per the maintainer's convention at the
      top of the file, design open question 3): a show request arriving after the launch window has
      closed during application shutdown (tray click or second-instance activation before the tray
      icon is disposed) no longer crashes the process with `InvalidOperationException`; normal show
      and positioning behavior is unchanged (`app-lifecycle`).
- [x] 3.2 Gate: `openspec validate --all` passes.
- [x] 3.3 Manual smoke (user-confirmed; blocking for commit/archive): run the application and
      verify —
      - exit from the tray while clicking the tray icon rapidly/repeatedly (several attempts): the
        process exits cleanly, no crash dialog or unhandled-exception event;
      - launch a second instance while the first is exiting (a few attempts): no crash in the first
        instance, it still exits;
      - normal paths unchanged: tray click, global hotkey, second-instance activation, and startup
        each show the launch window positioned per the configured popup screen position; Escape
        hides; pinning still keeps it visible.
      Record the result and date on this task.
      Result 2026-10-25: attempted; the shutdown race was not reproducible in several attempts.
      The user approved proceeding on code-level evidence: the deterministic closed-state guard at
      both `WindowPositioner.Place` call sites plus the regression tests.
- [x] 3.4 After source edits: `graphify update .` to refresh the knowledge graph.
- [x] 3.5 Report and ask the user about `git commit` and `openspec archive` — neither happens
      without explicit approval, and only after 3.3 is confirmed.
