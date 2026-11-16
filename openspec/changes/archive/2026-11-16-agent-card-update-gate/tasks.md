# Agent Card Update Gate: Implementation Tasks

Spec-conformance fix — no new behavior. `skip_specs: true`.

## Stage 1: Update the derivation and doc comment

- [x] 1.1 In `src/CLIHub/ViewModels/Agents/AgentCardItem.cs:217-220`, add `&& this.IsInstalled` as the fourth condition in `CanUpdate`. Place it last so the three existing conditions short-circuit identically for an installed card.

- [x] 1.2 In the same file at `:212-216`, update the `CanUpdate` XML doc comment to list four conditions: "the latest update-availability result reports `UpdateAvailable`, the agent declares an `update` command, no matching agent process is running, **and the agent is installed on the host**."

### Gate: build check
```powershell
dotnet build CLIHub.sln
```

## Stage 2: Raise CanUpdate notification from InstallState setter

- [x] 2.1 In `src/CLIHub/ViewModels/Agents/AgentCardItem.cs`, inside the `InstallState` setter (`:138-160`), add `this.OnPropertyChanged(nameof(this.CanUpdate));` alongside the existing five derived-property notifications at `:145-149`, immediately before the `lastSettledState` block (`:151`).

### Gate: build check
```powershell
dotnet build CLIHub.sln
```

## Stage 3: Card-level tests

- [x] 3.1 `CanUpdate_WhenNotInstalled_IsFalse` — construct a not-installed card with `UpdateAvailability = UpdateAvailable`, a declared `update` command, and `IsBlockedByRunningAgent = false`; assert `CanUpdate == false`.

- [x] 3.2 `CanUpdate_WhenInstalledWithAllConditions_IsTrue` — construct an installed card with `UpdateAvailable`, a declared `update` command, and no blocked process; assert `CanUpdate == true`.

- [x] 3.3 `InstallState_WhenChangedToNotInstalled_RaisesCanUpdateNotification` — start with an installed card that has `CanUpdate == true` (all conditions met), subscribe to `PropertyChanged`, flip `InstallState` to `NotInstalled`; assert that `nameof(AgentCardItem.CanUpdate)` appears in the raised notifications and `CanUpdate` becomes false.

- [x] 3.4 `CanUpdate_WhenDetecting_IsFalse` — start with an installed card that has `CanUpdate == true`, flip `InstallState` to `Detecting`; assert `CanUpdate == false`.

- [x] 3.5 Confirm all existing `CanUpdate_*` tests (lines 237-298) and `IsBlockedByRunningAgent_WhenSet_RaisesCanUpdateNotification` (`:221`) still pass — they construct installed cards and their assertions are unchanged.

### Gate: test the changes
```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
```

## Stage 4: Resolve the backlog entry

- [x] 4.1 In `docs/backlog.md`, at the "Settings card CanUpdate does not itself test installation state" entry (around line 462), append: `**Status: Resolved by agent-card-update-gate.**`

### Gate: verify backlog
```powershell
# Confirm the resolution line is present
findstr /c:"Resolved by agent-card-update-gate" docs\backlog.md
```

## Stage 5: Final verification

- [x] 5.1 `dotnet build CLIHub.sln` — expect 0 errors, 0 warnings.
- [x] 5.2 `dotnet test CLIHub.sln` — expect all green.
- [x] 5.3 `openspec validate --all` — expect 0 errors.
- [x] 5.4 `graphify update .`

### Gate: all gates passed
```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
openspec validate --all
graphify update .
```