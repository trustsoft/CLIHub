# Implementation Tasks

## Stage 1: Retype the two consumers and remove the interface

- [ ] 1.1 In `src/CLIHub/Startup/IApplicationStartupUi.cs:16`, change `IStartupWindow LaunchWindow { get; }`
      to `LaunchWindow LaunchWindow { get; }`. Add `using CLIHub.Views;` to this file if it is not
      already present (confirm by reading the file's current `using` block before editing).
- [ ] 1.2 In `src/CLIHub/Startup/ApplicationStartupContext.cs:9`, change
      `Action<IStartupWindow> SetMainWindow` to `Action<LaunchWindow> SetMainWindow`. Add
      `using CLIHub.Views;` to this file (it currently has no `using` directives — confirm before editing).
- [ ] 1.3 In `src/CLIHub/Startup/WpfApplicationStartupUi.cs:31`, change
      `public IStartupWindow LaunchWindow => this.launchWindow;` to
      `public LaunchWindow LaunchWindow => this.launchWindow;`. `using CLIHub.Views;` already exists in
      this file (`WpfApplicationStartupUi.cs:3`) — do not add a duplicate.
- [ ] 1.4 In `src/CLIHub/Views/LaunchWindow.xaml.cs:21`, change
      `public partial class LaunchWindow : Window, IStartupWindow` to
      `public partial class LaunchWindow : Window`. Remove the now-unused `using CLIHub.Startup;` at
      line 15 — first confirm no other `CLIHub.Startup` symbol is referenced anywhere else in this file.
- [ ] 1.5 Delete `src/CLIHub/Startup/IStartupWindow.cs`.
- [ ] 1.6 Build: `dotnet build CLIHub.sln`. Expect compile errors only in
      `tests/CLIHub.Tests/Services/ApplicationBootstrapperTests.cs` (the known, required test-file
      dependency documented in `design.md`'s "Test file disposition" section). Any other error means an
      `IStartupWindow` reference was missed — stop and re-run
      `grep -r "IStartupWindow" src tests` (or equivalent) before continuing.

## Stage 2: Fix the one test file that must change to compile

- [ ] 2.1 Open `tests/CLIHub.Tests/Services/ApplicationBootstrapperTests.cs`. The retyped property
      cannot be served by a mock of a concrete `LaunchWindow`: constructing one is impossible in this
      test host — `LaunchWindow.xaml.cs` calls `InitializeComponent()`, whose XAML root resolves
      `StaticResource Sizing.FontFamily.Text` from application-level merged dictionaries, so it throws
      `XamlParseException` without an `Application` (this is documented in
      `tests/CLIHub.Tests/Views/LaunchWindowClosedGuardTests.cs:12-20`, and that test uses a plain
      `Window`, not a `LaunchWindow`). Do not attempt a real instance or a subclass.
- [ ] 2.2 Apply the null-window fixture strategy, which is valid because nothing in these tests reads
      the window: delete the `Mock<IStartupWindow> Window` member (`:106`), drop the unused
      `CreateContext(IStartupWindow window)` parameter (`:94`) so it becomes `CreateContext()`, update
      the three call sites (`:35`, `:47`, `:69`), and configure the two strict UI setups (`:30`, `:65`)
      as `fixture.Ui.SetupGet(x => x.LaunchWindow).Returns((LaunchWindow)null!);`. Add a short comment at
      the first setup explaining that the startup context's `SetMainWindow` delegate is a no-op in these
      tests, so the window value is never read and null is intentional. Do not weaken or reorganize any
      assertion.
- [ ] 2.3 Confirm the three existing test methods still assert the same behavior they did before this
      change: `Start_FirstInstance_ExecutesStartupInOrder`, `Start_RequiredStartupFailure_LogsAndThrows`,
      `Start_OptionalReleaseNotesFailure_ContinuesStartup`. Do not add, remove, or rename test methods —
      only the fixture's window type and the unused helper parameter change.
- [ ] 2.4 Build: `dotnet build CLIHub.sln` — expect 0 errors, 0 warnings.
- [ ] 2.5 Test: `dotnet test CLIHub.sln` — expect all tests passing, including
      `ApplicationBootstrapperTests`, `OptionalStartupCoordinatorTests`, and `ApplicationHostTests`
      (the latter two were verified in `design.md` to need no source changes; this run confirms that
      verification held).

## Stage 3: Confirm the null-argument guard is unaffected

- [ ] 3.1 Re-run just the guard tests to isolate them from the full suite:
      `dotnet test CLIHub.sln --filter "FullyQualifiedName~Start_WithNullSetMainWindow"`. Confirm both
      `Start_WithNullSetMainWindow_ThrowsArgumentNullException` and
      `Start_WithNullSetMainWindow_DisposesGuardAcquiredBeforeTheFailure` still pass and still assert
      `ex.ParamName == "setMainWindow"` — the guard's behavior must be byte-for-byte unchanged by this
      retyping.

## Stage 4: Documentation cleanup

- [ ] 4.1 Mark the `IStartupWindow` backlog entry ("`IStartupWindow` is an empty marker interface after
      the tier 2 cleanup") as done by appending ` - **[done 2026-11-01: remove-startup-window-marker]**`
      to its heading, following the established convention for every other resolved entry in
      `docs/backlog.md` (for example the `provenance-mark-contrast` and `agent-row-inset-alignment`
      entries). Keep the evidence text and add a one-line status noting the interface was deleted and
      both members retyped to `LaunchWindow`. Do not delete the entry: the file's own header says closed
      items keep their evidence trail.

## Stage 5: Final gates

- [ ] 5.1 Build: `dotnet build CLIHub.sln` — expect 0 errors, 0 warnings.
- [ ] 5.2 Test: `dotnet test CLIHub.sln` — expect full suite passing.
- [ ] 5.3 Validate: `openspec validate --all`.
- [ ] 5.4 Run `node .opencode/scripts/check-model-policy.mjs` if the implementing session dispatched any
      subagents for this work (per `AGENTS.md`'s subagent model policy); skip if the work was done inline.
- [ ] 5.5 `graphify update .` to refresh the knowledge graph after the source edits.
