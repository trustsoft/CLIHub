# Tray Menu Separators + Version Header: Implementation Tasks

Two WPF `Separator`s (below the header, below the project list) plus a version header: the disabled
first item shows `Version <version>` instead of the current project. Two MODIFIED requirements in
`specs/tray-menu/spec.md` ("Tray menu structure" and "Add Project is placed below the project list");
two original scenarios rewritten in place (names kept, content retargeted to the version header — the
validator forbids dropping scenarios), one new scenario added ("Header shows the current
version"), and the two separator scenarios
added earlier.

Reference design: `design.md` (Decisions 1 and 2). Line numbers below are from the current working tree
(which already contains the two separators); re-read the files before editing if anything shifted.

Target structure (0-based), one favorite: `[0]` `Version <version>` header · `[1]` separator ·
`[2]` project submenu · `[3]` separator · `[4]` Add Project · `[5]` Settings · `[6]` Updates ·
`[7]` separator · `[8]` Exit = **9 items**. No favorites: same shape with `[2]` =
`(no favorite projects)`.

## Stage 1: Two separators in the builder

- [x] 1.1 In `src/CLIHub/Tray/TrayMenuBuilder.cs`, confirm the separator after the header (`:50`) and
      the separator after the project block (`:68`) are present, with the pre-`Exit` separator (`:80`)
      unchanged — exactly three separators. Add any missing separator rather than duplicating.

### Gate: build check
```powershell
dotnet build CLIHub.sln
```

## Stage 2: Re-index and extend tray menu tests (separators)

- [x] 2.1 Confirm the tray tests already re-index for the three-separator layout (count `9`, project
      submenu at `[2]`, tail at `[4]`–`[8]`) and that `Build_Separators_DivideHeaderProjectListAndExit`
      asserts three separators at `[1]`, `[3]`, `[7]` in both the favorites and no-favorites cases.

### Gate: build and tray menu tests
```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln --filter "FullyQualifiedName~TrayMenuBuilderTests"
```

## Stage 3: Version header — state and builder

- [x] 3.1 In `src/CLIHub/Tray/TrayMenuState.cs`, replace the first record field `Project? CurrentProject`
      (`:23`) with `string Version`, and update the record XML doc (`:18`) from "Currently selected
      project, if any." to describe the version. The `using CLIHub.Core.Models;` (`:3`) stays (still
      needed by `TrayProjectAgents`).
- [x] 3.2 In `src/CLIHub/Tray/TrayStateProjection.cs`, change the first constructor argument in
      `GetState()` from `this.projects.GetCurrentProject()` (`:69`) to `this.updates.GetCurrentVersion()`;
      update the method XML doc (`:58`) to drop "current project".
- [x] 3.3 In `src/CLIHub/Tray/TrayMenuBuilder.cs`, change the header item (`:44-48`) to
      `Header = $"Version {state.Version}"` (keep `IsEnabled = false`). Verify this removes the only read
      of `state.CurrentProject` (grep confirms only `TrayMenuBuilder.cs:46`).
- [x] 3.4 Confirm `IProjectService.GetCurrentProject()` is still used elsewhere (launch flow,
      `TrayCommandHandlers.cs:100`) — only the tray *display* drops the current project.

### Gate: build check
```powershell
dotnet build CLIHub.sln
```
Deviation: this gate cannot be green before Stage 4, because the solution build compiles
`CLIHub.Tests`, whose `TrayActionsTests` still reads the removed `state.CurrentProject`
(`CS1061`). Verified `dotnet build src/CLIHub/CLIHub.csproj` — 0 warnings, 0 errors — and ran the
full-solution build as Stage 4's gate once the tests were updated.

## Stage 4: Version header — test updates

Do not weaken any assertion; re-target header assertions and add version stubs.

- [x] 4.1 `tests/CLIHub.Tests/Services/TrayMenuBuilderTests.cs` `CreateState` (`:545-576`): remove the
      `currentProject` parameter (`:549`) and its XML doc (`:540`), remove
      `projectsMock.Setup(x => x.GetCurrentProject()).Returns(currentProject)` (`:555`), and stub the
      fallback `updates` mock's `GetCurrentVersion()` to return a fixed version (e.g. `"1.2.1"`).
- [x] 4.2 `Build_OnlyFavoriteProjects_AddsProjectSubmenusInStorageOrder` (`:30`): change
      `"No project selected"` to `"Version 1.2.1"`.
- [x] 4.3 `Build_Separators_DivideHeaderProjectListAndExit` (`:471`): change `"No project selected"` to
      `"Version 1.2.1"`. Follow-up (verifier): also asserts the header is disabled and carries no icon in
      both the one-favorite and the no-favorites menus.
- [x] 4.4 `Build_UpdateItemIcons_FollowUpdateState` (`:338-376`): stub `GetCurrentVersion()` on the four
      `Mock<IUpdateWorkflow>` instances (`idle`, `checking`, `downloading`, `available`) so `GetState()`
      gets a non-null version.
- [x] 4.5 `tests/CLIHub.Tests/Services/TrayActionsTests.cs`
      `GetState_ProjectsFavoritesWithResolvedDefaultInitializedAgentsAndUpdateState` (`:18-50`): remove
      `projects.Setup(x => x.GetCurrentProject()).Returns(current)` (`:26`); stub
      `updates.Setup(x => x.GetCurrentVersion()).Returns("1.2.1")` (near `:30`); replace
      `Assert.Same(current, state.CurrentProject)` (`:43`) with `Assert.Equal("1.2.1", state.Version)`.

### Gate: build and tray tests
```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln --filter "FullyQualifiedName~TrayMenuBuilderTests|FullyQualifiedName~TrayActionsTests"
```

## Stage 5: Full verification

- [x] 5.1 `dotnet build CLIHub.sln` — expect 0 errors, 0 warnings.
- [x] 5.2 `dotnet test CLIHub.sln` — expect all green.
- [x] 5.3 `openspec validate --all` — expect 0 errors.
- [x] 5.4 `node .opencode/scripts/pipeline-state.mjs check` — expect 0 warnings (or expected warnings
      only).
- [x] 5.5 `graphify update .`

### Gate: all gates passed
```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
openspec validate --all
node .opencode/scripts/pipeline-state.mjs check
graphify update .
```

## Manual smoke (user-confirmed, before commit/archive)

- [x] 6.1 Launch the app and open the tray context menu; confirm the first item is a disabled
      `Version <version>` header with the current version, no item names the current project, and three
      horizontal separators render — below the header, below the favorite-project list (or
      `(no favorite projects)` when none exist) and above `Add Project...`, and before `Exit` — in both
      Windows light and dark themes.

## Workflow follow-up

- Archive the change after implementation, verification, and explicit user approval.
- After `openspec archive`, edit the main `openspec/specs/tray-menu/spec.md` Purpose sentence
  ("the current-project header" → "the version header") — a delta cannot modify the Purpose, so this
  must happen post-archive.
- Verify the post-archive integrity checks from `AGENTS.md` (empty `git status --porcelain`,
  `openspec list` without the archived change, `openspec validate --all` 0 errors,
  `node .opencode/scripts/pipeline-state.mjs check`).
