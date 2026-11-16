## Why

`docs/backlog.md` records "Settings card CanUpdate does not itself test installation state."
`AgentCardItem.CanUpdate` (`src/CLIHub/ViewModels/Agents/AgentCardItem.cs:217-220`) derives from the
availability status, the declared `update` command, and `IsBlockedByRunningAgent` *only*. The
not-installed gate relies on every production caller clearing the availability result — a call-site
discipline that any new availability writer can break. Meanwhile the durable spec already requires
the gate:

- `openspec/specs/agent-update-detection/spec.md:230-252` — Requirement "Update availability gates
  the Settings card Update action": "When the agent is not installed on the host, the Update action
  is disabled" and scenario "Not installed disables the action."
- `openspec/specs/agent-ui/spec.md:27-31` — Scenario "Settings card CanUpdate is gated": the
  three-condition derivation (`UpdateAvailable` status + declared `update` command + no matching
  agent process running) does not mention `IsInstalled`, but the narrative text at
  `agent-update-detection/spec.md:232` ("When the agent is not installed on the host, the Update
  action is disabled") covers it, and the dedicated scenario at `:249-252` pins it.

The change makes `CanUpdate` itself enforce the "not installed" gate that the spec already requires,
so the card is the single source of truth.

## What Changes

1. **`AgentCardItem.CanUpdate`** gains `&& this.IsInstalled`; the XML doc comment adds the
   installation-state condition. The three existing conditions stay unchanged.

2. **The `InstallState` setter** raises `OnPropertyChanged(nameof(this.CanUpdate))` alongside the
   existing `IsInstalled` notification, so flipping installation state propagates to every bound
   consumer — the XAML `IsEnabled="{Binding CanUpdate}"` binding
   (`src/CLIHub/Views/SettingsAgentsPage.xaml:301`) and the relay command predicate
   (`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:155`).

3. **Tests** at the `AgentCardItem` card level pin:
   - `CanUpdate` is false for a not-installed card even with `UpdateAvailable` + declared command +
     no running process.
   - Flipping `InstallState` from `Installed` to `NotInstalled` makes `CanUpdate` false and fires
     the property-changed notification.
   - The `Detecting` state makes `CanUpdate` false (a "don't update during probe" safety choice).
   - Existing `CanUpdate_*` tests stay valid (they construct installed cards).

## Capabilities

### Unchanged Capabilities

- `agent-update-detection` — the existing Requirement at `:230-252` and its "Not installed disables
  the action" scenario (`:249-252`) already cover this behavior. The change makes the implementation
  match the spec.
- `agent-ui` — the Settings card "CanUpdate is gated" scenario (`spec.md:27-31`) states the
  three-condition derivation; the installation gate is a fourth condition already required by
  `agent-update-detection`, not a new requirement.

**Spec deltas: none.** The change is a spec-conformance fix, not new behavior. The requirements and
scenarios that the change satisfies already exist at the lines above; this change adds no new
observable contract. The change uses `skip_specs: true` because the spec was written to require
exactly this behavior, and this change closes the gap between the spec and the implementation.

## Impact

- `src/CLIHub/ViewModels/Agents/AgentCardItem.cs` — `CanUpdate` derivation, doc comment, and
  `InstallState` setter notification.
- `tests/CLIHub.Tests/ViewModels/Agents/AgentCardItemTests.cs` — new card-level test methods.
- No consumer or test relies on `CanUpdate == true` for a not-installed card. The load-time
  availability population pass (`AgentManagementViewModel.cs:859-869`) already skips not-installed
  agents and clears their availability; the per-card Update command check also gates on installation.
  Every existing test that asserts `CanUpdate == true` constructs an installed card (verified:
  all `CanUpdate_*` tests in `AgentCardItemTests.cs` pass `AgentInstallState.Installed` to
  `CreateCard`; the `AgentManagementViewModel` tests use `Detection()` which returns
  `IsInstalledInSystem => true` by default). The `UpdateCommand` relay predicate
  (`AgentManagementViewModel.cs:155`) is `card?.CanUpdate == true` — a pure delegation that
  automatically follows the card-level change.

## Non-Goals

- Changing the launch-window `AgentItem.CanUpdate` (`src/CLIHub/ViewModels/Agents/AgentItem.cs:67`),
  which tests only the declared `update` command and is an independent, out-of-scope concern.
- Adding a gate to `AgentManagementViewModel` or any other caller — the card-level gate makes the
  call-site discipline redundant. The existing caller-side gates (load-time skip for not-installed,
  probe-time clear) remain in place and are harmless.
- Changing any XAML binding — `IsEnabled="{Binding CanUpdate}"` at `SettingsAgentsPage.xaml:301`
  already binds to the card's `CanUpdate`.

## Dependencies

None. The change is self-contained in `AgentCardItem` plus its test file.