# Remove AvailabilitySummary: Implementation Tasks

Internal cleanup — no user-visible behavior change. `skip_specs: true`.

## Stage 1: Remove the interface and implementation surface

- [x] 1.1 In `src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs`, delete the `AvailabilitySummary` declaration **and its doc comment** (lines 45-48). Leave `InstalledAgentCount` and `SupportedAgentCount` in place.
- [x] 1.2 In `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs`, delete the `NotInstalledAgentCount` property **and its doc comment** (lines 238-241).
- [x] 1.3 In `AgentManagementViewModel.cs`, delete the `AvailabilitySummary` property (lines 243-249).

### Gate: compile-check the surface removal
```powershell
dotnet build CLIHub.sln
```

## Stage 2: Rename the refresh method

- [x] 2.1 Rename `private void RefreshAvailabilitySummary()` → `private void RefreshSummarySurfaces()` (definition at `AgentManagementViewModel.cs:800`).
- [x] 2.2 Update all 10 call sites to `RefreshSummarySurfaces()`: lines 599, 686, 903, 988, 1038, 1054, 1138, 1153, 1181, 1198.
- [x] 2.3 In the renamed method body, remove the two `OnPropertyChanged` calls for the deleted members: `OnPropertyChanged(nameof(this.NotInstalledAgentCount))` (line 804) and `OnPropertyChanged(nameof(this.AvailabilitySummary))` (line 805). Keep the remaining nine raises.

### Gate: compile-check the rename
```powershell
dotnet build CLIHub.sln
```

## Stage 3: Migrate the test assertions

- [x] 3.1 `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Load.cs:37` — delete the `Assert.Equal("2 supported · 1 not installed", viewModel.AvailabilitySummary);` line (counts already pinned at lines 35-36).
- [x] 3.2 `Load.cs:161` — replace `Assert.Equal("2 supported · all installed", viewModel.AvailabilitySummary);` with `Assert.Equal(2, viewModel.SupportedAgentCount);` and `Assert.Equal(2, viewModel.InstalledAgentCount);`.
- [x] 3.3 `Load.cs:174` — delete the `Assert.Equal("No agent plugins found", viewModel.AvailabilitySummary);` line (counts already pinned at lines 172-173).
- [x] 3.4 `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Detection.cs:27` — replace `Assert.Equal("1 supported · all installed", viewModel.AvailabilitySummary);` with `Assert.Equal(1, viewModel.SupportedAgentCount);` and `Assert.Equal(1, viewModel.InstalledAgentCount);`.
- [x] 3.5 `Detection.cs:40` — replace `Assert.Equal("1 supported · all installed", viewModel.AvailabilitySummary);` with `Assert.Equal(1, viewModel.InstalledAgentCount);`.
- [x] 3.6 `Detection.cs:64` — replace `Assert.Equal("2 supported · 1 not installed", viewModel.AvailabilitySummary);` with `Assert.Equal(2, viewModel.SupportedAgentCount);` and `Assert.Equal(1, viewModel.InstalledAgentCount);`.
- [x] 3.7 `Detection.cs:71` — replace `Assert.Equal("2 supported · all installed", viewModel.AvailabilitySummary);` with `Assert.Equal(2, viewModel.InstalledAgentCount);`.
- [x] 3.8 `Detection.cs:286` — replace `Assert.Contains(nameof(AgentManagementViewModel.AvailabilitySummary), raised);` with `Assert.Contains(nameof(AgentManagementViewModel.InstalledAgentCount), raised);`.

### Gate: build and test the migrated tests
```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
```

## Stage 4: Resolve the backlog entry

- [x] 4.1 In `docs/backlog.md`, append a line to the "AvailabilitySummary is declared but unbound" entry (lines 327-339): `**Status: Resolved by remove-availability-summary.**`

## Stage 5: Final verification

- [x] 5.1 `dotnet build CLIHub.sln` — expect 0 errors, 0 warnings.
- [x] 5.2 `dotnet test CLIHub.sln` — expect all green.
- [x] 5.3 `openspec validate --all` — expect `Totals: 36 passed, 0 failed (36 items)`.
- [x] 5.4 `graphify update .`
- [x] 5.5 `node .gitnexus/run.cjs detect-changes --scope all --repo .` — confirm clean (no `partial`/`truncated`; re-run if not clean).

### Gate: all gates passed
```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
openspec validate --all
graphify update .
node .gitnexus/run.cjs detect-changes --scope all --repo .
```