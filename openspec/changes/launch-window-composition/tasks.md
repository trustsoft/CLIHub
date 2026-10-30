# Tasks: launch-window-composition

Each stage leaves the tree compilable and all tests passing. Build and test after every stage.
Do not touch `docs/backlog.md` or the four deleted `docs/architecture-analysis-*` files; they are
unrelated working-tree changes.

## Stage 1: Remove dead IUserNotificationService dependency

- [x] 1.1 Delete `private readonly IUserNotificationService notifications;` field from `src/CLIHub/ViewModels/Launch/LaunchWindowViewModel.cs` (line 26)
- [x] 1.2 Remove `IUserNotificationService notifications` constructor parameter (line 61) and its XML doc `<param name="notifications">` (line 49)
- [x] 1.3 Remove `this.notifications = notifications;` assignment (line 71)
- [x] 1.4 Remove `userNotificationService.Object` argument from `LaunchWindowViewModel` construction in `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs` at line 301 (`Build()` method). Keep the `userNotificationService` mock: `LaunchCommandCoordinator` still needs it.
- [x] 1.5 Remove `userNotificationService.Object` argument from the same construction at line 380 (`BuildWithMocks()` method).
- [x] 1.6 Prove the field was dead: `git grep -n "this\.notifications" -- src/CLIHub/ViewModels/Launch/LaunchWindowViewModel.cs` returns no matches.
- [x] 1.7 Verify build: `dotnet build CLIHub.sln`
- [x] 1.8 Verify tests: `dotnet test CLIHub.sln` — expect 1,271 passing (544 CLIHub.Tests + 60 CLIHub.Core.Windows.Tests + 667 CLIHub.Core.Tests)

## Stage 2: Add view model policy methods and tick seam

- [x] 2.1 Add `private long shownAt;` field after `suppressFilterChange` (line 37) in `src/CLIHub/ViewModels/Launch/LaunchWindowViewModel.cs`
- [x] 2.2 Add `private const long DeactivationGraceMilliseconds = 400;` after `shownAt`
- [x] 2.3 Add the internal test seam, keeping the public constructor at 11 parameters:
      `internal Func<long> TickCountProvider { get; set; } = () => Environment.TickCount64;`
      `CLIHub.csproj` already declares `<InternalsVisibleTo Include="CLIHub.Tests" />`, so the test builder can override it. Add a short comment stating it exists so the grace policy is testable without sleeping. Do NOT add a constructor parameter and do NOT change production DI.
- [x] 2.4 Add public `PrepareForShow()` with XML doc: "Prepares the window for display by refreshing both panes and applying the default agent selection. Version re-population is served by the version cache within its TTL. The default-agent lookup runs against the fresh agent rows." Body: `this.RefreshProjects(); this.RefreshAgents(); this.ApplyDefaultAgentSelection();`
- [x] 2.5 Add public `PrepareForActivation()` with XML doc: "Refreshes both panes when the window is activated. Version re-population is cache-served within the TTL." Body: `this.RefreshProjects(); this.RefreshAgents();`
- [x] 2.6 Add public `MarkShown()` with XML doc: "Records the time the window was shown, resetting the deactivation grace period." Body: `this.shownAt = this.TickCountProvider();`
- [x] 2.7 Add public `ShouldHideOnDeactivate(bool isPromptOpen)` with XML doc: "Determines whether the window should hide on deactivation. Returns false if the window is pinned, a modal prompt is open, or the deactivation grace period has not elapsed." Body: `if (this.IsPinned || isPromptOpen) { return false; } if (this.TickCountProvider() - this.shownAt < DeactivationGraceMilliseconds) { return false; } return true;`
- [x] 2.8 Verify build: `dotnet build CLIHub.sln`
- [x] 2.9 Verify tests: `dotnet test CLIHub.sln` — expect 1,271 passing

## Stage 3: Delegate code-behind to view model policy

- [x] 3.1 In `src/CLIHub/Views/LaunchWindow.xaml.cs`, remove `private const long DeactivationGraceMilliseconds = 400;` (line 28)
- [x] 3.2 Remove `private long shownAt;` field (line 35)
- [x] 3.3 In `ShowOnPointerMonitor()`, replace lines 75-81 (`this.viewModel.RefreshProjects();` through `this.viewModel.ApplyDefaultAgentSelection();`) with `this.viewModel.PrepareForShow();`. Keep the placement, `Show`, window-state normalization, `Activate`, closed guard, and deferred `PlaceWhenOpen` exactly where they are.
- [x] 3.4 Replace `this.shownAt = Environment.TickCount64;` (line 89) with `this.viewModel.MarkShown();` (same position: after window-state normalization, before `Activate`).
- [x] 3.5 In `OnWindowActivated()`, replace lines 122-125 (`this.viewModel.RefreshProjects();` through `this.viewModel.RefreshAgents();`) with `this.viewModel.PrepareForActivation();`
- [x] 3.6 In `OnDeactivated()`, replace lines 130-138 (pin/prompt/grace checks and early returns) with `if (!this.viewModel.ShouldHideOnDeactivate(this.promptState.IsPromptOpen)) { return; }` followed by the existing `this.Hide();`
- [x] 3.7 Confirm `OnClosing` still cancels and hides; `OnKeyDown`/Escape and splitter/popup code are untouched.
- [x] 3.8 Verify build: `dotnet build CLIHub.sln`
- [x] 3.9 Verify tests: `dotnet test CLIHub.sln` — expect 1,271 passing

## Stage 4: Collapse the builder object graph into Compose()

- [x] 4.1 In `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs`, extract `private (LaunchWindowViewModel ViewModel, TestMocks Mocks) Compose()` from the duplicated construction shared by `Build()` (lines 234-305) and `BuildWithMocks()` (lines 313-405).
- [x] 4.2 Update `Build()` to `var (viewModel, _) = this.Compose(); return viewModel;`
- [x] 4.3 Update `BuildWithMocks()` to `return this.Compose();`
- [x] 4.4 Keep `TestMocks` shape and all required properties unchanged, including `UserNotificationService`; keep both public method signatures unchanged.
- [x] 4.5 Verify build: `dotnet build CLIHub.sln`
- [x] 4.6 Verify tests: `dotnet test CLIHub.sln` — expect 1,271 passing

## Stage 5: Add policy tests

- [x] 5.1 Create `tests/CLIHub.Tests/ViewModels/Launch/LaunchWindowViewModelPolicyTests.cs` (namespace `CLIHub.ViewModels`, matching `LaunchWindowDefaultAgentTests`). Use `LaunchWindowViewModelBuilder`, Moq, and xUnit.
- [x] 5.2 Add `WithTickCount(Func<long> tickCount)` to `LaunchWindowViewModelBuilder`: store it in a private field; in `Compose()`, after constructing the view model, assign `viewModel.TickCountProvider = tickCount` when the field is set. Return `this` for chaining.
- [x] 5.3 Add `PrepareForShow_RefreshesProjectsThenAgentsThenSelectsDefault`: configure projects, two installed plugins, `DefaultAgentId` = second plugin. After construction, clear invocations and attach callbacks to a shared `List<string>` order recorder on `IProjectService.GetAllProjects` ("projects"), `IAgentDetectionService.IsInstalledInSystem` ("agents"), and `IPreferencesStore.Load` ("default"). Call `PrepareForShow()`. Assert `order[0] == "projects"`, an "agents" entry precedes the "default" entry, and `SelectedAgent?.Plugin.Id` is the configured default. Do NOT mock the pane controllers — `ProjectPaneController` and `AgentPaneController` are sealed; assert through the service mocks and observable VM state.
- [x] 5.4 Add `PrepareForActivation_RefreshesProjectsAndAgentsWithoutDefaultSelection`: configure a stored default agent, clear invocations, call `PrepareForActivation()`. Assert the project service and detection service were invoked and `IPreferencesStore.Load` was NOT called (the default-selection step must not run on activation), and the previous `SelectedAgent` is unchanged.
- [x] 5.5 Add `ShouldHideOnDeactivate_PinnedReturnsFalse`: `WithTickCount` returning 1000; build; set `IsPinned = true`; call `MarkShown()`; advance to 2000; assert `ShouldHideOnDeactivate(false)` is false.
- [x] 5.6 Add `ShouldHideOnDeactivate_PromptOpenReturnsFalse`: tick 1000; `MarkShown()`; advance to 2000; assert `ShouldHideOnDeactivate(true)` is false.
- [x] 5.7 Add `ShouldHideOnDeactivate_WithinGraceReturnsFalse`: tick 1000; `MarkShown()`; advance to 1300 (300 ms); assert false.
- [x] 5.8 Add `ShouldHideOnDeactivate_AfterGraceReturnsTrue`: tick 1000; `MarkShown()`; advance to 1401 (401 ms); assert true.
- [x] 5.9 Add `MarkShown_ResetsGraceWindow`: tick 1000; `MarkShown()`; advance to 1300; `MarkShown()`; advance to 1600 (300 ms after the second mark); assert false.
- [x] 5.10 Do NOT add a new filter test. `LaunchWindowViewModelFilterTests.ToggleFilterAction_UpdatesShowOnlyProjectAgents_PersistsPreference_RefreshesAgents` and `ToggleFilterAction_OnePersistenceCall_OneRefresh` already drive `AgentsActions`' checkable action (the same instance as `FilterAction`) and assert `ShowOnlyProjectAgents` plus one refresh; `SetShowOnlyProjectAgents_MirrorsToFilterAction` covers the reverse direction. Record this evidence in the final report.
- [x] 5.11 Verify build: `dotnet build CLIHub.sln`
- [x] 5.12 Verify tests: `dotnet test CLIHub.sln` — expect 1,278 passing (7 new tests in CLIHub.Tests)

## Stage 6: Final gates

- [x] 6.1 Verify build: `dotnet build CLIHub.sln` — succeed with no warnings and no errors
- [x] 6.2 Verify tests: `dotnet test CLIHub.sln` — expect 1,278 passing (551 CLIHub.Tests + 60 CLIHub.Core.Windows.Tests + 667 CLIHub.Core.Tests)
- [x] 6.3 Validate OpenSpec: `openspec validate launch-window-composition --type change` reports valid; `openspec validate --all` reports 30 passed / 1 failed where the only failure is the foreign `fix-settings-state-sync` change (no deltas, no `skip_specs`), which is out of scope and untouched.
- [x] 6.4 Confirm `MenuActionCoordinator` is gone: `git grep -n "MenuActionCoordinator" -- src tests` returns no matches.
- [x] 6.5 Confirm `WindowActionCoordinator` is still constructed inline in `LaunchWindowViewModel` and `WindowActionCoordinator` has no DI registration (`ServiceRegistration.cs` unchanged).
- [x] 6.6 Run `git diff --check` (no whitespace errors) and `git status --porcelain`. Expected changes: the three source/test files plus the new policy test file and this change's four files. `docs/backlog.md` and the four deleted `docs/architecture-analysis-*` files must remain untouched (they were already dirty before the change).

## Notes

- The tick seam is an internal property, not a constructor parameter: the public constructor stays at 11 parameters and production DI is unchanged. `CLIHub.csproj` already exposes internals to `CLIHub.Tests`.
- The baseline test count is 1,271; Stage 5 adds 7 tests (1,278). No filter test is added because the existing tests already pin both directions.
- Each stage is independently compilable and testable; if a stage fails, the previous stage is a known-good state.
