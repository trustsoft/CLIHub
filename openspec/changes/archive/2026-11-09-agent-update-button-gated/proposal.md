# Settings → CLI Agents: gate Update button on update availability

## Why

The Update action on agent cards (Settings → CLI Agents) is currently enabled whenever the agent declares an `update` command and no matching agent process is running (`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:793`). The update availability check (`AgentCardItem.UpdateAvailability`, `:216-227`) is advisory only: it never gates the action (`openspec/specs/agent-update-detection/spec.md:110-129`, `openspec/specs/agent-ui/spec.md:286-290`). An `Unknown` result — from a failed check, a timed-out check, or an agent that declares no update source — leaves the button enabled, which can surprise users who see "Update" as available when the system cannot verify an update exists.

The launch-window Update action (`WindowActionCoordinator.cs`, `AgentItem.CanUpdate`) is declaration-based and stays unchanged; this change is explicitly scoped to the Settings → CLI Agents surface and does not bring the launch window into line (that would require plumbing availability into `AgentItem`).

Five seed plugins (`src/CLIHub.Core/SeedPlugins/*/plugin.json`) declare both `update` and `updateCheck`, so strict gating does not strand built-in agents; an `Unknown` result arises from failed checks, not missing declarations.

## What Changes

### Update button enablement rule

`AgentCardItem.CanUpdate` (`src/CLIHub/ViewModels/Agents/AgentCardItem.cs:206-210`) becomes a computed property derived from existing state:

- enabled exactly when `UpdateAvailability?.Status == AgentUpdateStatus.UpdateAvailable` AND the agent declares an `update` command AND no matching agent process is running;
- disabled when availability is `UpToDate` or `Unknown`, including before the first availability result arrives, and disabled when the agent is not installed on the host.

The running-agent block message ("Agent is running — stop it to update", `AgentManagementViewModel.cs:799`) stays unchanged and is still written to the card.

`AgentManagementViewModel.Load` (`:541`) no longer sets `CanUpdate` to `declaresUpdate` because the property is now computed. `PopulateBlockedStateAsync` (`:793`) no longer writes `CanUpdate = declaresUpdate && running.Count == 0` for the same reason. The property's backing field is removed, and every write to `UpdateAvailability` or the running-agent state re-raises `PropertyChanged` for `CanUpdate` so the XAML `IsEnabled` binding updates.

### UpdateCommand CanExecute predicate

`AgentManagementViewModel.UpdateCommand` (`:115`) gains a `CanExecute` predicate that mirrors the button enablement: the command is executable exactly when the card's `CanUpdate` is true. `OnCardPropertyChanged` (`:597`), which already observes every card, raises `CanExecuteChanged` for `UpdateCommand` when a card's `CanUpdate` changes.

### Post-update availability handling

`UpdateAsync` (`:1088-1104`) currently invalidates the cached availability (`:1102`) but leaves the card's last result, which would keep the button enabled. The method is extended to clear the card's `UpdateAvailability` to null when the update is started, so the button disables until the next availability check resolves a new result.

### Spec deltas

- `openspec/specs/agent-ui/spec.md` — MODIFIED requirements "Agent card actions" (Update gated on `UpdateAvailable` status), "Agent card displayed content" (the update indicator and Update enablement now derive from the same result), and "Update command support detection" (declaration detection scoped to the launch-window model; the Settings card rule points to "Agent card actions").
- `openspec/specs/agent-update-detection/spec.md` — the "Update availability is advisory" requirement is removed and replaced with "Update availability gates the Settings card Update action"; the Purpose line is corrected directly in the main spec.
- `openspec/specs/preferences-ui/spec.md` — unchanged: its overlapping "Agent card actions" requirement remains true under strict gating, and deduplication is out of scope.

### Tests

- `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Load.cs` — update `Load_UnknownAvailability_LeavesCanUpdateUnchanged` (`:262`) to assert `CanUpdate` is false when availability is `Unknown`, keep `Load_UpdateAvailableWithoutUpdateCommand_LeavesCanUpdateDisabled` (`:281`) asserting false, and add tests for `UpToDate` disabling the button and `UpdateAvailable` enabling it.
- `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.UpdateAvailability.cs` — update availability-result scenarios to assert `CanUpdate` reflects the gated rule.
- `tests/CLIHub.Tests/ViewModels/Agents/AgentCardItemTests.cs` — add tests for `CanUpdate` deriving from `UpdateAvailability` status, the declared `update` command, and `IsBlockedByRunningAgent`.
- New test file `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.UpdateCommand.cs` — test `UpdateCommand.CanExecute` mirroring `CanUpdate`, and that a card's `CanUpdate` change raises `CanExecuteChanged`.
- Update any test that sets `CanUpdate` directly: the property is no longer settable.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `agent-ui` — MODIFIED requirements "Agent card actions", "Agent card displayed content", and "Update command support detection": the Settings card Update action is gated on availability.
- `agent-update-detection` — the advisory requirement is removed; ADDED "Update availability gates the Settings card Update action".

## Impact

- `src/CLIHub/ViewModels/Agents/AgentCardItem.cs` — `CanUpdate` becomes a computed property (availability, declaration, running-agent block); backing field removed; `IsBlockedByRunningAgent` added; `UpdateAvailability` and `IsBlockedByRunningAgent` setters raise `PropertyChanged` for `CanUpdate`.
- `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` — `Load` and `PopulateBlockedStateAsync` no longer set `CanUpdate`; `UpdateCommand` gains a `CanExecute` predicate; `OnCardPropertyChanged` raises `CanExecuteChanged` for `UpdateCommand`; `UpdateAsync` clears the card's `UpdateAvailability` when the update starts.
- `src/CLIHub.Core/Agents/AgentUpdateAvailability.cs` and `IAgentUpdateAvailabilityService.cs` — XML docs no longer claim the result is advisory for the Settings card.
- `src/CLIHub/Views/SettingsAgentsPage.xaml` — no change; the XAML `IsEnabled` binding already observes `CanUpdate`.
- Tests — existing tests updated to assert the gated behavior; new tests for `UpdateCommand.CanExecute`.
- `docs/project-context.md` and `README.md` — describe the gated Update action.
- `openspec/specs/agent-ui/spec.md`, `openspec/specs/agent-update-detection/spec.md` — modified requirements and the corrected Purpose line.

## Non-goals

- No change to the launch-window Update action (`AgentItem.CanUpdate`, `WindowActionCoordinator`) — explicit non-goal per the brief.
- No change to the running-agent block message or its behavior.
- No change to the availability check scheduling, caching, or forced-check paths.
- No change to the card's Detect or Check Updates actions.
- No new availability status values or fallback logic.
- No persistence of the gated state; it is recomputed from live availability and plugin declaration on every state change.

## Dependencies

Before this change was created, `openspec list` reported no active changes, and no other open change touches `AgentManagementViewModel`, `AgentCardItem`, or the affected specs.
