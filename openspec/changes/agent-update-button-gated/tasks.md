# Implementation Tasks: gate Settings card Update button on update availability

## Stage 1: Add IsBlockedByRunningAgent property and make CanUpdate computed

- [x] 1.1 Add a new property `IsBlockedByRunningAgent` to `AgentCardItem` (`src/CLIHub/ViewModels/Agents/AgentCardItem.cs`) with type `bool`, default `false`, a backing field, and XML docs stating it is written by `PopulateBlockedStateAsync` and is part of the Update enablement derivation. The setter raises `OnPropertyChanged(nameof(CanUpdate))` after setting the backing field.
- [x] 1.2 Remove the `canUpdate` backing field from `AgentCardItem` (`:206-210`).
- [x] 1.3 Change `CanUpdate` (`:206-210`) from a settable property to a computed read-only property that returns `this.UpdateAvailability?.Status == AgentUpdateStatus.UpdateAvailable && this.Plugin.Commands?.Get(AgentCommandKind.Update) is not null && !this.IsBlockedByRunningAgent`.
- [x] 1.4 Extend the `UpdateAvailability` setter (`:216-227`) to raise `OnPropertyChanged(nameof(CanUpdate))` after it raises for `UpdateAvailabilityText` and `HasUpdateAvailable`.
- [x] 1.5 Remove the `card.CanUpdate = declaresUpdate` assignment from `AgentManagementViewModel.Load` (`:541`).
- [x] 1.6 Change `AgentManagementViewModel.PopulateBlockedStateAsync` (`:793`) from `card.CanUpdate = declaresUpdate && running.Count == 0` to `card.IsBlockedByRunningAgent = running.Count > 0`. Leave the block message behavior (`:797-800`) unchanged.
- [x] 1.7 Build: `dotnet build CLIHub.sln`. Expect zero errors.

## Stage 2: Add UpdateCommand CanExecute predicate and OnCardPropertyChanged raise

- [x] 2.1 Change `AgentManagementViewModel.UpdateCommand` (`:115`) from `new RelayCommand<AgentCardItem>(card => _ = this.RunAsync(...))` to `new RelayCommand<AgentCardItem>(card => _ = this.RunAsync(...), card => card?.CanUpdate == true)`.
- [x] 2.2 Extend `AgentManagementViewModel.OnCardPropertyChanged` (`:597-610`) to respond to `nameof(AgentCardItem.CanUpdate)` changes (in addition to `InstallState`) by calling `this.UpdateCommand.RaiseCanExecuteChanged()`.
- [x] 2.3 Build: `dotnet build CLIHub.sln`. Expect zero errors.

## Stage 3: Clear availability after update starts

- [x] 3.1 In `AgentManagementViewModel.UpdateAsync` (`:1088-1104`), add `card.UpdateAvailability = null;` before the `this.availability.InvalidateFor(card.Plugin)` call (`:1102`) when `result.Started` is true.
- [x] 3.2 Build: `dotnet build CLIHub.sln`. Expect zero errors.

## Stage 4: Update advisory XML comments

- [x] 4.1 Update `src/CLIHub.Core/Agents/AgentUpdateAvailability.cs:26` — change "The result is advisory: it never gates or alters the Update action" to "The result gates the Settings card Update action; it is advisory for the launch-window Update action".
- [x] 4.2 Update `src/CLIHub.Core/Agents/IAgentUpdateAvailabilityService.cs:14-15` — update the interface comment to state the result gates the Settings card action.
- [x] 4.3 Update `src/CLIHub/ViewModels/Agents/AgentCardItem.cs:212-215` — change "`UpdateAvailability` … is advisory only: it never gates or alters the Update action" to "gates the Settings card Update action; see CanUpdate".
- [x] 4.4 Update `src/CLIHub/ViewModels/Agents/AgentCardItem.cs:238-241` — change "`HasUpdateAvailable` … Advisory only; it does not enable, disable, or block the Update action" to "reflects the availability status; see CanUpdate for the gated enablement rule".
- [x] 4.5 Update `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:517-523` — change "Cards render with declarative availability (Update enabled exactly when the plugin declares an update command)" to "Cards render with gated availability (Update enabled when availability is UpdateAvailable AND the plugin declares an update command AND no running agent)".
- [x] 4.6 Build: `dotnet build CLIHub.sln`. Expect zero errors.

## Stage 5: Update existing tests for gated behavior

- [x] 5.1 Update `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Load.cs:262` (`Load_UnknownAvailability_LeavesCanUpdateUnchanged`) to mock availability to return `Unknown`, wait for the pass with `SpinWait.SpinUntil(() => card.UpdateAvailability is not null, TimeSpan.FromSeconds(5))`, assert `Assert.False(card.CanUpdate)`, and rename the test to `Load_UnknownAvailability_DisablesCanUpdate`.
- [x] 5.2 Add a new test in `AgentManagementViewModelTests.Load.cs`: `Load_UpdateAvailable_EnablesCanUpdate` that loads an agent with `withUpdate: true`, mocks availability to return `UpdateAvailable`, waits for the pass with `SpinWait.SpinUntil(() => card.UpdateAvailability?.Status == AgentUpdateStatus.UpdateAvailable, TimeSpan.FromSeconds(5))`, and asserts `card.CanUpdate` is true after waiting for blocked-state pass with `SpinWait.SpinUntil(() => !card.IsBlockedByRunningAgent || card.CanUpdate, TimeSpan.FromSeconds(5))`.
- [x] 5.3 Add a new test in `AgentManagementViewModelTests.Load.cs`: `Load_UpToDate_DisablesCanUpdate` that loads an agent with `withUpdate: true`, mocks availability to return `UpToDate`, waits for the pass with `SpinWait.SpinUntil(() => card.UpdateAvailability?.Status == AgentUpdateStatus.UpToDate, TimeSpan.FromSeconds(5))`, and asserts `card.CanUpdate` is false.
- [x] 5.4 Update `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.UpdateAvailability.cs` to assert `CanUpdate` reflects the gated rule (availability status AND declaration AND running-agent) in every scenario that assigns `UpdateAvailability`. Add `SpinWait` as needed.
- [x] 5.5 Add tests to `tests/CLIHub.Tests/ViewModels/Agents/AgentCardItemTests.cs` for `CanUpdate` deriving from `UpdateAvailability` status, the declared `update` command, and `IsBlockedByRunningAgent` (test card with no update command, card with update command and `Unknown` availability, card with update command and `UpdateAvailable` availability and not blocked, card with update command and `UpdateAvailable` availability but blocked, card with update command and `UpToDate` availability).
- [x] 5.6 Check `AgentManagementViewModelBuilder.WithUpdate`/`WithAvailability` helpers and update any test that directly set `CanUpdate` (which is no longer settable) to set `UpdateAvailability` and `IsBlockedByRunningAgent` instead.
- [x] 5.7 Test: `dotnet test CLIHub.sln --filter "FullyQualifiedName~AgentManagementViewModelTests.Load"`. Expect all updated tests to pass.
- [x] 5.8 Test: `dotnet test CLIHub.sln --filter "FullyQualifiedName~AgentCardItemTests"`. Expect all card item tests to pass.

## Stage 6: Add UpdateCommand CanExecute tests

- [x] 6.1 Create `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.UpdateCommand.cs`.
- [x] 6.2 Add test `UpdateCommand_WhenCanUpdateIsFalse_CannotExecute`: load an agent with `withUpdate: true`, mock availability to return `Unknown`, wait for the pass with `SpinWait.SpinUntil(() => card.UpdateAvailability?.Status == AgentUpdateStatus.Unknown, TimeSpan.FromSeconds(5))`, assert `viewModel.UpdateCommand.CanExecute(card)` is false.
- [x] 6.3 Add test `UpdateCommand_WhenCanUpdateIsTrue_CanExecute`: load an agent with `withUpdate: true`, mock availability to return `UpdateAvailable`, wait for the pass with `SpinWait.SpinUntil(() => card.UpdateAvailability?.Status == AgentUpdateStatus.UpdateAvailable, TimeSpan.FromSeconds(5))`, wait for blocked state with `SpinWait.SpinUntil(() => !card.IsBlockedByRunningAgent || card.CanUpdate, TimeSpan.FromSeconds(5))`, assert `viewModel.UpdateCommand.CanExecute(card)` is true.
- [x] 6.4 Add test `UpdateCommand_WhenCanUpdateChanges_RaisesCanExecuteChanged`: load an agent, subscribe to `UpdateCommand.CanExecuteChanged`, change `card.UpdateAvailability` from null to a `UpdateAvailable` result, assert the event was raised.
- [x] 6.5 Test: `dotnet test CLIHub.sln --filter "FullyQualifiedName~AgentManagementViewModelTests.UpdateCommand"`. Expect all UpdateCommand tests to pass.

## Stage 7: Full test suite

- [x] 7.1 Test: `dotnet test CLIHub.sln`. Expect zero test failures. If any test fails because it directly set `CanUpdate` (which is no longer settable), update that test to set `UpdateAvailability` and `IsBlockedByRunningAgent` instead or remove the write.
- [x] 7.2 If failures remain after 7.1, investigate and fix. No test should be weakened; tests that encoded the advisory behavior are updated to assert the gated behavior.

## Stage 8: Verify and update spec deltas

- [x] 8.1 Verify `openspec/changes/agent-update-button-gated/specs/agent-ui/spec.md` includes three MODIFIED requirements: "Agent card actions" (with all 21 original scenarios plus the 5 gated scenarios), "Agent card displayed content" (with the advisory sentence and scenario body replaced), and "Update command support detection" (scoped to the launch-window model, plus the gated Settings-card scenario).
- [x] 8.2 Verify `openspec/changes/agent-update-button-gated/specs/agent-update-detection/spec.md` removes "Update availability is advisory" and adds "Update availability gates the Settings card Update action".
- [x] 8.3 Verify `openspec/changes/agent-update-button-gated/specs/preferences-ui/` directory and delta do NOT exist (deduplication is out of scope).
- [x] 8.4 Verify `openspec/specs/agent-update-detection/spec.md:6` (the Purpose line) already reads that the result gates the Settings card Update action and is advisory for the launch-window action — the plan applied this direct main-spec edit; do not re-edit it.
- [x] 8.5 Validate: `openspec validate --all`. Expect zero errors. Fix any validation errors reported.
- [x] 8.6 Update `docs/project-context.md:77` to state that the Settings card Update action is enabled only when an update is known to be available, in addition to the running-agent block.
- [x] 8.7 Update the README's Basic Use step 5 (`README.md:93-94`) so it says the Update action is available only when CLIHub knows an update is available, and still blocked while the agent's processes run.

## Stage 9: Final verification

- [x] 9.1 Build: `dotnet build CLIHub.sln`. Expect zero errors.
- [x] 9.2 Test: `dotnet test CLIHub.sln`. Expect zero test failures.
- [x] 9.3 Validate: `openspec validate --all`. Expect zero errors. Quote the exact output in the implementation report.
- [x] 9.4 Run `graphify update .` to refresh the knowledge graph after code changes (per AGENTS.md:186).
- [x] 9.5 Record any implementation deviations, surprises, or open questions in design.md under "Implementation notes".
- [x] 9.6 Report completion: change id, files created, validation result, decisions made beyond the brief, deviations, open questions.
- [x] 9.7 Manual GUI smoke confirmed by the user on 2026-11-09: Update enabled only for `UpdateAvailable`, disabled for up-to-date/unknown, disabled with the running-agent block message, disabled immediately after starting an update, launch-window action unchanged.
