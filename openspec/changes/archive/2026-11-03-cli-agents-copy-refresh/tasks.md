# Tasks — CLI Agents Copy Refresh

## Stage 1: Update Page Description and Summary Cards

- [x] 1.1 Update page description in `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:73` to `"Agent status, updates, and the default agent."`
- [x] 1.2 Remove `DetailToolTip="Installed agents and supported plugin count"` from AGENTS summary card in `src/CLIHub/Views/SettingsWindow.xaml:220`
- [x] 1.3 Remove `DetailToolTip="Agent update availability"` from UPDATES summary card in `src/CLIHub/Views/SettingsWindow.xaml:233`
- [x] 1.4 Update `DefaultAgentDetail` property in `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:133-150`:
  - Line 139: Change `"No default agent — choose one"` to `"Choose an agent below"`
  - Line 147: Change `"Preselected in the launch window"` to `"In the launch window"`
- [x] 1.5 Update `UpdatesDetail` property in `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:311-329`:
  - Lines 315-320: Replace with singular/plural pattern: `count == 1 ? $"1 of {installed} agent{(installed == 1 ? "" : "s")} has an update" : $"{count} of {installed} agent{(installed == 1 ? "" : "s")} have updates"`
  - Line 327: Change `"Run Check updates"` to `"Check updates"`

**Gate**: `dotnet build CLIHub.sln` (0 errors, 0 warnings)

## Stage 2: Update Default Agent Hint and Header Actions

- [x] 2.1 Update default agent hint in `src/CLIHub/Views/SettingsAgentsPage.xaml:52`: Change `"Preselected when the launch window opens."` to `"Preselected in the launch window."`
- [x] 2.2 Update header Detect action tooltips and automation names in `src/CLIHub/Views/SettingsAgentsPage.xaml:90,94`:
  - Line 90: Change automation name to `"Recheck each agent and its version"`
  - Line 94: Change tooltip to `"Recheck each agent and its version"`
- [x] 2.3 Update header Check updates action tooltips and automation names in `src/CLIHub/Views/SettingsAgentsPage.xaml:106,110`:
  - Line 106: Change automation name to `"Check all installed agents for updates"`
  - Line 110: Change tooltip to `"Check all installed agents for updates"`

**Gate**: `dotnet build CLIHub.sln` (0 errors, 0 warnings)

## Stage 3: Update Per-Card Tooltips and Status Messages

- [x] 3.1 Update per-card action tooltips in `src/CLIHub/Views/SettingsAgentsPage.xaml`:
  - Line 163: Change update chip tooltip to `"Newer version available"`
  - Line 188: Change Detect tooltip to `"Recheck this agent and its version"`
  - Line 212: Change Update tooltip to `"Run this agent's update command"`
  - Line 224: Change Edit config tooltip to `"Open this agent's plugin.json"`
- [x] 3.2 Update status messages in `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs`:
  - Line 608: Change `"Installed · version {version}"` to `$"Installed · v{version}"`
  - Line 610: Change `"Not installed on the host"` to `"Not installed on this PC"`
  - Line 583: Change `"Update blocked while running"` to `"Agent is running — stop it to update"`
  - Line 804: Change `"Not installed on the host"` to `"Not installed on this PC"` (CheckCardUpdatesAsync defensive branch)
  - Line 820: Change `$"Update available · {installed} → {latest}"` to `"Update available"`
  - Line 823: Change `"Update availability unknown"` to `"Update status unknown"`
  - Line 881: Change `"Update started."` to `"Update started"`
  - Line 882: Change `"Update failed."` to `"Update failed"` (in the fallback expression `result.Message ?? "Update failed"`)
- [x] 3.3 Update Core service status message in `src/CLIHub.Core/Agents/AgentManagementService.cs:95`: Change `"Update blocked while running"` to `"Agent is running — stop it to update"`

**Gate**: `dotnet build CLIHub.sln` (0 errors, 0 warnings)

## Stage 4: Update Tests

- [x] 4.1 Update test assertions in `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Load.cs`:
  - Line 46: `"Preselected in the launch window"` -> `"In the launch window"` (`DefaultAgentDetail`)
  - Line 132: `"No default agent — choose one"` -> `"Choose an agent below"` (`DefaultAgentDetail`)
  - Lines 313, 331, 427: `"Update blocked while running"` -> `"Agent is running — stop it to update"` (`card.Message`)
- [x] 4.2 Update test assertions in `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Detection.cs`:
  - Line 49: `"Installed · version 9.9"` -> `"Installed · v9.9"`
  - Line 172: `"Not installed on the host"` -> `"Not installed on this PC"`
- [x] 4.3 Update test assertions in `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.UpdateAvailability.cs`:
  - Line 678: `"Update available · 1.0.0 → 2.0.0"` -> `"Update available"`
  - Line 681: `"Update availability unknown"` -> `"Update status unknown"`
  - Line 748: `"Not installed on the host"` -> `"Not installed on this PC"`
  - Line 957: `"Run Check updates"` -> `"Check updates"`
  - Lines 986-987: `"1 of 3 installed agents"` -> `"1 of 3 agents has an update"`; `"2 of 3 installed agents"` -> `"2 of 3 agents have updates"`
  - Lines 1095, 1131: `"1 of 2 installed agents"` -> `"1 of 2 agents has an update"`
- [x] 4.4 Update test assertions in `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.CardActions.cs`:
  - Line 78: `"Update started."` -> `"Update started"`
  - Lines 90, 96: `"Update blocked while running"` -> `"Agent is running — stop it to update"`
- [x] 4.5 Update test assertion in `tests/CLIHub.Core.Tests/Agents/AgentManagementServiceTests.cs:425`: Change `"Update blocked while running"` to `"Agent is running — stop it to update"`
- [x] 4.6 Grep the entire tests tree for any remaining old strings and update them:
  - `"Installed · version"`
  - `"Not installed on the host"`
  - `"Update blocked while running"`
  - `"Update available ·"`
  - `"Update availability unknown"`
  - `"Update started."`
  - `"Update failed."`
  - `"Preselected when the launch window opens"`
  - `"No default agent — choose one"`
  - `"Run Check updates"`
  - `"of N installed agents"` (where N is a digit)

**Gate**: `dotnet test CLIHub.sln` (all pass)

## Stage 5: Update Documentation

- [x] 5.1 Grep `README.md` for old strings and update any current user-facing quotations (ignore CHANGELOG.md and openspec/changes/archive/)
- [x] 5.2 Grep `docs/` for old strings and update any current documentation that quotes them (ignore CHANGELOG.md and openspec/changes/archive/)

**Gate**: `dotnet build CLIHub.sln` (0 errors, 0 warnings)

## Stage 6: Final Validation

- [x] 6.1 Run `openspec validate --all` and confirm 0 errors
- [x] 6.2 Run `node .opencode/scripts/pipeline-state.mjs check` and confirm 0 warnings
- [x] 6.3 Run `graphify update .` to refresh the knowledge graph
- [x] 6.4 Manual smoke test: Open Settings → CLI Agents and verify all text matches the approved mockup (page description, summary cards, default agent hint, header and per-card tooltips, status messages). Smoke confirmed by the user.

**Gate**: All validation passes; manual smoke confirmed by user before commit/archive approval.

## Implementation Notes

- All stages completed as prescribed; gates observed: `dotnet build CLIHub.sln` 0 errors/0 warnings after Stages 1-3 and 5; `dotnet test CLIHub.sln` all green (574 + 67 + 703 = 1,344 passed) after Stage 4; `openspec validate --all` 0 errors (30 passed — the printed ⚠ lines are advisory lint warnings about long requirement texts inside this change's pre-approved delta specs, not validation errors); `pipeline-state.mjs check` 0 warnings; `graphify update .` rebuilt (18,335 nodes / 32,417 edges).
- Task 6.4 (manual smoke) was confirmed by the user: Settings → CLI Agents matches the approved mockup. Commit/archive approval was given after that confirmation.
- `AgentManagementViewModel.CheckCardUpdatesAsync` (was lines 816-824): the two `AgentUpdateStatus.UpdateAvailable` switch arms collapsed into the single arm `AgentUpdateStatus.UpdateAvailable => "Update available"`. Reason: after replacing the version-detailed arm with `"Update available"`, both arms returned the identical string, so the `when` pattern matching on `InstalledVersion`/`LatestVersion` was dead duplication. Same output for every input; no behavior change.
- Task 4.6 found one additional assertion beyond the corrected mapping: `AgentManagementViewModelTests.UpdateAvailability.cs:1036` (`UpdatesAggregation_WhenOneInstalledAgentHasAnUpdate_UsesSingularAgentNoun`) expected the old singular-noun form `"1 of 1 installed agent"`, which the mapping's grep pattern `of N installed agents` (plural) did not match. Updated to `"1 of 1 agent has an update"` per the approved count==1 template. The test's purpose (singular noun when installed==1) is preserved.
- Task 5.1/5.2: greps of `README.md` and `docs/` for every old string (including the old tooltip strings and `DetailToolTip`) found no matches — no documentation edits were required. `CHANGELOG.md` and `openspec/changes/archive/` were not touched, per the brief.
- GitNexus pre-edit impact (per repository policy): `UpdatesDetail` 24 upstream impacted / CRITICAL and `UpdateAsync` max 38 / CRITICAL — the risk is the wide string-asserting consumer surface (XAML bindings + tests), which this change updates in the same pass; signatures and binding contracts are unchanged. Post-edit `detect-changes --scope all`: 10 files, 26 symbols, 0 affected processes, risk level: low, no partial/truncated flags.
- The untracked approved mockup `cli-agents-copy-mockup.html` was not modified.
- Verifier finding resolved: the `agent-ui` delta's "Agent card actions" requirement paragraph had drifted from the durable spec (it dropped the glyph-parity sentence and added contradictory header-gating / Update / Edit-config sentences). The paragraph was restored byte-for-byte from `openspec/specs/agent-ui/spec.md:319`; only the approved per-card outcome THEN clause and its `**Delta**` note remain changed. Re-validated: `openspec validate --all` 30/30, 0 errors; normalized requirement text now matches the main spec for `agent-ui`, `preferences-ui`, and `settings-theme`.
