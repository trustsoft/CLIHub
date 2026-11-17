# Tray Project Launchers: Implementation Tasks

> **Revision note (2026-11-17).** This is the revised lightweight design. It supersedes the first
> implementation of this change, which is still uncommitted in the working tree (base plan commit
> `4c9efa0`) and is reworked in place. The rework: removes `Show CLIHub`, groups the update surfaces
> under `Updates`, drops the ` *` favorite suffix and the `Run ` action prefix from labels, puts an
> MDL2 glyph on every actionable item, and trims `TrayMenuCommands` to the seven slots the builder binds.
> Every checkbox below is unticked because the rework has not been done.

Restructure the tray context menu from `Recent Projects` + `Launch Agent` + `Add Project…` into one
submenu per favorite project whose items are that project's initialized agents. New capability
`tray-menu`; no MODIFIED requirements.

Reference design: `design.md` (Decisions 1–14; Decision 6 is superseded by Decision 9). Cite `file:line`
from the verified state there; line numbers are from the uncommitted first implementation, so re-read a
file before editing it twice.

## Stage 1: Lighten the labels in the builder

- [x] 1.1 In `src/CLIHub/Tray/TrayMenuBuilder.cs:49`, change the project submenu header from `$"{entry.Project.Name} *"` to `entry.Project.Name` — the project name only (Decision 9).
- [x] 1.2 At `:124` and `:140`, change the default and initialized agent labels from `$"Run {defaultAgent.Name}"` and `$"Run {plugin.Name}"` to the agent name only (`defaultAgent.Name` / `plugin.Name`).
- [x] 1.3 Keep the empty-state labels and their disabled state unchanged: `(no favorite projects)` (`:56`) and `(no agents)` (`:148`).
- [x] 1.4 Confirm no other label in the builder carries an action word or a favorite suffix, and that no string moved out of `TrayMenuBuilder`.

### Gate: build check
```powershell
dotnet build CLIHub.sln
```

## Stage 2: Remove `Show CLIHub`

- [x] 2.1 Delete the `Show CLIHub` block in `src/CLIHub/Tray/TrayMenuBuilder.cs:79-81` and leave no reference to `commands.ShowLaunchWindow` in the builder.
- [x] 2.2 Keep the `Separator` at `:77` — it now separates `Updates` from `Exit` (Decision 10).
- [x] 2.3 Confirm the surviving window entry points are untouched: `TrayIconController.ShowLaunchWindow` (`src/CLIHub/Tray/TrayIconController.cs:57-60`), `ToggleLaunchWindow` (`:65-75`), the left-click handler (`:48`), and the hotkey wiring (`src/CLIHub/Composition/ServiceRegistration.cs:143-146`).

### Gate: build check
```powershell
dotnet build CLIHub.sln
```

## Stage 3: Group the update surfaces into `Updates`

- [x] 3.1 In `src/CLIHub/Tray/TrayMenuBuilder.cs`, remove the top-level `What's New` item (`:67-69`).
- [x] 3.2 Add a private helper that builds the `Updates` submenu: its items are the dynamic update item (from `BuildUpdateItem`, `:88-115`) first and a `What's New` item wired to `commands.ShowReleaseNotes` second (Decision 11).
- [x] 3.3 Insert the `Updates` item after `Settings` (`:63-65`) and before the `Separator` (`:77`), so the top-level tail is `Settings`, `Updates`, separator, `Exit`.
- [x] 3.4 Confirm `BuildUpdateItem` still yields the four states in order — `Check for updates`, disabled `Checking for updates…`, disabled `Downloading update…`, `Download <version> and restart` — and that the submenu is never empty, because the helper returns an item in every state; the guard at `:72` is therefore dead, so either keep it or delete it and record which was chosen.

### Gate: build check
```powershell
dotnet build CLIHub.sln
```

## Stage 4: Put icons on every actionable item

- [x] 4.1 Add `using CLIHub.Theming;` to `src/CLIHub/Tray/TrayMenuBuilder.cs` and a small private static helper `CreateGlyph(string glyph)` returning a `TextBlock` with `Text = glyph`, `FontFamily = new FontFamily("Segoe MDL2 Assets")`, `FontSize = 14`, `IsHitTestVisible = false`, and `Focusable = false`; set no `Foreground` (it inherits from the `MenuItem`). Comment that the family and size mirror `Sizing.FontFamily.Icon` and `Sizing.FontSize.GlyphSmall` and are hard-coded because the tray popup has no window resource scope (Decision 12).
- [x] 4.2 Set `Icon` on the project submenu to `CreateGlyph(IconGlyphs.FavoriteStar)`.
- [x] 4.3 Set `Icon` on every agent item — the default item and each initialized agent item — to `CreateGlyph(IconGlyphs.Play)`.
- [x] 4.4 Set `Icon` on `Add Project...` (`IconGlyphs.FolderAdd`), `Settings` (`IconGlyphs.Settings`), `Updates` (`IconGlyphs.Sync`), `What's New` (`IconGlyphs.Page`), and `Exit` (`IconGlyphs.Exit`).
- [x] 4.5 Set the dynamic update item's icon inside `BuildUpdateItem`: `IconGlyphs.Sync` for `Check for updates` and `Checking for updates…`, `IconGlyphs.Update` for `Downloading update…` and `Download <version> and restart`.
- [x] 4.6 Leave the header, `(no favorite projects)`, `(no agents)`, and separators without an icon.
- [x] 4.7 Confirm the builder still contains all WPF construction (no glyph type moved into the state, projection, or handlers) and that `System.Windows.Media` is the only new using besides `CLIHub.Theming`.

### Gate: build check
```powershell
dotnet build CLIHub.sln
```

## Stage 5: Trim the command surface and the tests that compile against it

- [x] 5.1 In `src/CLIHub/Tray/TrayMenuState.cs`, reduce `TrayMenuCommands` (`:42-51`) to the seven bound slots in this order: `SelectProjectAndLaunchAgentAsync`, `AddProject`, `ShowSettings`, `ShowReleaseNotes`, `CheckForUpdates`, `RequestUpdateDownload`, `Exit`. Remove `SelectProject`, `LaunchAgentAsync`, and `ShowLaunchWindow` together with their XML doc lines (`:32-33,40`).
- [x] 5.2 In `src/CLIHub/Tray/TrayCommandHandlers.cs`, change `CreateCommands(Action<string> selectProject)` (`:61`) to `CreateCommands()`: drop the parameter, its XML doc line (`:59`), and the `selectProject` argument; keep two inert defaults for `RequestUpdateDownload` and `Exit`.
- [x] 5.3 In `src/CLIHub/Tray/TrayActions.cs:28`, call `this.handlers.CreateCommands()`.
- [x] 5.4 In `src/CLIHub/Tray/TrayStateProjection.cs`, delete the public `SetCurrentProject(string)` member (`:76-80`); `IProjectService.SetCurrentProject` stays.
- [x] 5.5 In `src/CLIHub/Tray/TrayIconController.cs:129-134`, drop the `ShowLaunchWindow = this.ShowLaunchWindow,` line from the `with` mutation; it now overrides only `RequestUpdateDownload` and `Exit`.
- [x] 5.6 Keep the private `TrayCommandHandlers.LaunchAgentAsync(Plugin, Project)` (`:92-99`) and its call from `SelectProjectAndLaunchAgentAsync` (`:110`); the project-scoped select-then-launch behavior is unchanged (Decision 13).
- [x] 5.7 In `tests/CLIHub.Tests/Services/TrayActionsTests.cs`, delete `LaunchAgentCommand_UsesSharedAgentWorkflow` (`:52-72`) — its workflow delegation is covered by `SelectProjectAndLaunchAgentCommand_WhenInvoked_SelectsProjectThenLaunches` (`:74-99`).
- [x] 5.8 Rewrite `CreateCommands_ControllerOwnedSlots_AreInertDefaults` (`:144-168`): call `handlers.CreateCommands()` with no lambda, drop the `selectedProject` flag, invoke `RequestUpdateDownload` and `Exit`, and assert both stay inert with `projects.VerifyNoOtherCalls()` and `updateWorkflow.VerifyNoOtherCalls()`.
- [x] 5.9 In `tests/CLIHub.Tests/Services/TrayMenuBuilderTests.cs`, reduce `CreateRecordingCommands` (`:282-301`) to the seven-argument `TrayMenuCommands` constructor.

### Gate: build and tray action tests
```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln --filter "FullyQualifiedName~TrayActionsTests"
```

## Stage 6: Update the tray menu behavior tests

- [x] 6.1 In `tests/CLIHub.Tests/Services/TrayMenuBuilderTests.cs`, update the header assertions to the project name only (`:30-33`, `:268`) and the agent-label assertions to the agent name only (`:75,98,101,123,126,150,171,188,242,271`); rename `Build_ProjectSubmenu_ListsOnlyInitializedAgentsWithRunLabels` to `...WithAgentNameLabels`.
- [x] 6.2 Replace `Build_MenuTail_KeepsAddProjectSettingsWhatsNewShowAndExitInOrder` (`:209-228`) with a tail test asserting the seven top-level items — header, project submenu, `Add Project...`, `Settings`, `Updates`, separator, `Exit` — and that no `Show CLIHub` or top-level `What's New` item exists.
- [x] 6.3 Add `Build_UpdatesSubmenu_ContainsUpdateItemThenWhatsNew` — assert the `Updates` items are the `Check for updates` item first and `What's New` second, and that `What's New` invokes `ShowReleaseNotes`.
- [x] 6.4 Add `Build_UpdateItemIcons_FollowUpdateState` — cover `Sync` on the idle `Check for updates` item (and, if the fixture is convenient, on the checking and downloading states) and `Update` on the `Download <version> and restart` item; extend the `CreateState` helper (`:315-345`) to accept a mocked `IUpdateWorkflow` (`LastKnownAvailableVersion`, `IsDownloading`, `IsCheckingForUpdates`), because it currently supplies a fresh mock with no update state.
- [x] 6.5 Add `Build_ActionableItems_CarryMdl2Glyphs` — assert each actionable item's `Icon` is a `TextBlock` with `FontFamily.Source == "Segoe MDL2 Assets"`, `FontSize == 14`, and `Text` equal to the matching `IconGlyphs` constant (`CLIHub.csproj:25` exposes internals to `CLIHub.Tests`); assert the header and the disabled placeholders have a null `Icon`.
- [x] 6.6 Keep the default-agent and empty-state tests passing with the new labels (`:81-207`), and keep `Populate_ExistingMenu_ReplacesItemsWithFreshState` (`:252-276`) asserting the new labels and item count.

### Gate: test the changes
```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
```

## Stage 7: Full verification

- [x] 7.1 `dotnet build CLIHub.sln` — expect 0 errors, 0 warnings.
- [x] 7.2 `dotnet test CLIHub.sln` — expect all green.
- [x] 7.3 `openspec validate --all` — expect 0 errors.
- [x] 7.4 `graphify update .`

### Gate: all gates passed
```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
openspec validate --all
graphify update .
```

## Manual smoke (user-confirmed, before commit/archive)

- [x] 8.1 Launch the app; open the tray menu and confirm top to bottom: header, favorite-only project submenus in storage order with the project name alone, `Add Project…`, `Settings`, `Updates`, separator, `Exit` — and no `Show CLIHub`.
- [x] 8.2 Confirm every actionable item renders its MDL2 glyph (star on project submenus, play on agent items, folder-add, settings, sync, page, exit) with no clipped or missing glyph, and that the header and placeholders show none; note whether the icon-less header rows line up acceptably.
- [x] 8.3 Open `Updates` and confirm the dynamic item first and `What's New` second; run `Check for updates` and confirm the checking and (if an update is published) downloading labels and their `Sync`/`Update` icons.
- [x] 8.4 Confirm each project submenu's agent items are labelled with the agent name alone and that invoking one makes that project current (header updates on the next open) and launches the agent in that folder.
- [x] 8.5 Confirm the default agent is first with a separator in every project submenu, and that a project with no default and no agents shows `(no agents)`.
- [x] 8.6 Confirm left-clicking the tray icon and pressing the global hotkey still show the launch window, since the menu no longer offers `Show CLIHub`.
- [x] 8.7 Favorite a project and change the default agent in Settings; reopen the tray menu and confirm both are reflected without restarting.
