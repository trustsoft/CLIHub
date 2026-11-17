# Tray Project Launchers: Implementation Tasks

Restructure the tray context menu from `Recent Projects` + `Launch Agent` + `Add Project…` into one
submenu per favorite project whose items are that project's initialized agents. New capability
`tray-menu`; no MODIFIED requirements.

Reference design: `design.md` (Decisions 1–8). Cite `file:line` from the verified state there.

## Stage 1: Reshape the tray state and command records

- [ ] 1.1 In `src/CLIHub/Tray/TrayMenuState.cs`, add a new public sealed record `TrayProjectAgents(Project Project, Plugin? DefaultAgent, IReadOnlyList<Plugin> InitializedAgents)` with XML docs on the type and each parameter.
- [ ] 1.2 In the same file, replace `TrayMenuState`'s parameters with `(Project? CurrentProject, IReadOnlyList<TrayProjectAgents> FavoriteProjects, string? AvailableUpdateVersion, bool IsDownloadingUpdate, bool IsCheckingForUpdates)`; update the XML docs and remove `RecentProjects`/`LaunchableAgents`.
- [ ] 1.3 In the same file, add the `Func<string, Plugin, Task> SelectProjectAndLaunchAgentAsync` parameter to `TrayMenuCommands` immediately after `LaunchAgentAsync`, with an XML doc line; keep `SelectProject`, `LaunchAgentAsync`, and all other slots (Decision 2).
- [ ] 1.4 Confirm no `System.Windows.*` using is added to `TrayMenuState.cs` (Core models only, Decision 1/7).

### Gate: build check
```powershell
dotnet build CLIHub.sln
```
Expect compile errors only in the files touched in later stages (`TrayStateProjection`, `TrayCommandHandlers`, `TrayMenuBuilder`, `TrayActions`, and the two test files).

## Stage 2: Compose the new state in the projection

- [ ] 2.1 In `src/CLIHub/Tray/TrayStateProjection.cs`, add constructor parameters `IAgentDetectionService detection` and `IPreferencesStore preferences` after `updates`, assign them with `?? throw new ArgumentNullException`, and add the matching XML docs.
- [ ] 2.2 Rewrite `GetState()` (`:49-55`) to build `FavoriteProjects`: read `this.projects.GetAllProjects()`, filter `IsFavorite`, and for each project compose its agent list — plugins with `plugin.Commands?.Launch is not null` that pass `this.detection.IsInstalledInSystem(plugin)` **and** `this.detection.IsAvailableInProject(plugin, project.Path)`, preserving `GetAllPlugins()` order (Decision 4).
- [ ] 2.3 In `GetState()`, resolve the default: read `this.preferences.Load().DefaultAgentId`; when null/whitespace or not found in the **unfiltered** `this.pluginCatalog.GetAllPlugins()`, use `null` (Decision 5). Carry the resolved `Plugin?` into each `TrayProjectAgents`.
- [ ] 2.4 In the same method, keep the update fields (`LastKnownAvailableVersion`, `IsDownloading`, `IsCheckingForUpdates`) unchanged.
- [ ] 2.5 Add a private helper for the per-project agent composition so `GetState()` stays a small composition; keep it free of WPF types.

### Gate: build check
```powershell
dotnet build CLIHub.sln
```

## Stage 3: Project-scoped launch handler and action wiring

- [ ] 3.1 In `src/CLIHub/Tray/TrayCommandHandlers.cs`, add the `SelectProjectAndLaunchAgentAsync(string projectId, Plugin plugin)` method: call `this.projects.SetCurrentProject(projectId)`, re-read `this.projects.GetCurrentProject()`, return when it is null or its id differs, otherwise `await this.LaunchAgentAsync(plugin, project)` (Decision 2).
- [ ] 3.2 In `CreateCommands(Action<string> selectProject)` (`:61-70`), pass `this.SelectProjectAndLaunchAgentAsync` into the new slot after `this.LaunchAgentAsync`; keep the inert defaults for the controller-owned slots.
- [ ] 3.3 In `src/CLIHub/Tray/TrayActions.cs` and `src/CLIHub/Tray/ITrayActions.cs`, keep `Commands = this.handlers.CreateCommands(this.projection.SetCurrentProject)` and `GetState()` as-is unless the compiler requires a change; no new WPF dependency.

### Gate: build check
```powershell
dotnet build CLIHub.sln
```

## Stage 4: Rebuild the menu shape in the builder

- [ ] 4.1 In `src/CLIHub/Tray/TrayMenuBuilder.cs`, keep the header block (`:22-26`) unchanged.
- [ ] 4.2 Replace the Recent Projects block and `BuildLaunchAgentMenu` call (`:28-45`) with a loop over `state.FavoriteProjects`: each becomes a `MenuItem { Header = $"{project.Name} *" }` whose items come from a private `PopulateProjectSubmenu(items, entry, commands)` helper (Decision 6).
- [ ] 4.3 In the helper: when `entry.DefaultAgent` is not null, add its item first (`Header = $"Run {entry.DefaultAgent.Name}"`, `IsEnabled = entry.DefaultAgent.Commands?.Launch is not null`, click → `commands.SelectProjectAndLaunchAgentAsync(project.Id, defaultAgent)`), then a `Separator`; then append the initialized agents **excluding** the default when it is also initialized, each labelled `Run {plugin.Name}` and clicking `commands.SelectProjectAndLaunchAgentAsync(project.Id, plugin)` (Decision 2/6).
- [ ] 4.4 In the helper: when there is no default and `InitializedAgents` is empty, add one disabled `(no agents)` item.
- [ ] 4.5 After the project loop, when `state.FavoriteProjects.Count == 0`, add one disabled `(no favorite projects)` item.
- [ ] 4.6 Keep `Add Project...`, `Settings`, `What's New`, `BuildUpdateItem`, `Separator`, `Show CLIHub`, and `Exit` exactly as today and in the same relative order (`:47-73`); delete `BuildLaunchAgentMenu` (`:107-130`).
- [ ] 4.7 Add `public void Populate(ContextMenu menu, TrayMenuState state, TrayMenuCommands commands)` that clears `menu.Items` and repopulates it with the built items; refactor `Build` so both paths share the `Populate` item-building code (Decision 3/8).

### Gate: build check
```powershell
dotnet build CLIHub.sln
```

## Stage 5: Refresh the menu content when it opens

- [ ] 5.1 In `src/CLIHub/Tray/TrayIconController.cs`, create the `ContextMenu` **once** via `this.menuBuilder.Build(...)`, assign it to `this.taskbarIcon.ContextMenu` (single instance for the icon's lifetime), and change `RefreshMenu()` (`:126-135`) to repopulate that same instance via `this.menuBuilder.Populate(this.taskbarIcon.ContextMenu, this.actions.GetState(), commands)`; never swap the instance.
- [ ] 5.2 Attach `this.taskbarIcon.ContextMenu.Opened += (_, _) => this.RefreshMenu();` **once** as part of the one-time creation, guarded so later refreshes cannot re-attach it (Decision 3); `Populate` must not attach or re-attach handlers.
- [ ] 5.3 Confirm `OnStateChanged` (`:146-155`) still calls `RefreshMenu` on the dispatcher and that the `with`-mutated command set (`:128-133`) still replaces only `RequestUpdateDownload`, `ShowLaunchWindow`, `Exit`.

### Gate: build check
```powershell
dotnet build CLIHub.sln
```

## Stage 6: Update the existing tests to the new shape

- [ ] 6.1 In `tests/CLIHub.Tests/Services/TrayActionsTests.cs`, rewrite `GetState_ProjectsCurrentRecentLaunchableAndUpdateState` (`:17-40`) to assert the new state: current project, one `TrayProjectAgents` per favorite with resolved default and initialized agents, and the update fields.
- [ ] 6.2 Update `CreateActions`/`CreateProjection` helpers (`:133-151`) to pass mocked `IAgentDetectionService` and `IPreferencesStore` into `TrayStateProjection`, setting `IsInstalledInSystem → true` and `IsAvailableInProject → true` and `Load() → new AppPreferences()` as defaults.
- [ ] 6.3 Add `SelectProjectAndLaunchAgentCommand_WhenInvoked_SelectsProjectThenLaunches`: with a strict `IProjectService` (assert `SetCurrentProject(P.Id)` is called, `GetCurrentProject()` returns P) and a strict `IAgentCommandWorkflow`, invoke `commands.SelectProjectAndLaunchAgentAsync(P.Id, plugin)` and verify `ExecuteAsync(plugin, P, AgentCommandKind.Launch, ...)` ran once.
- [ ] 6.4 Keep `LaunchAgentCommand_UsesSharedAgentWorkflow` (`:43-62`), `CreateCommands_ControllerOwnedSlots_AreInertDefaults` (`:108-131`), and `Dispose_UnsubscribesFromStateChanges` (`:91-105`) passing; update them only where the record/constructor changed.
- [ ] 6.5 In `tests/CLIHub.Tests/ViewModels/Updates/SharedUpdatePresentationTests.cs`, update `CreateProjection` (`:145-152`) for the two new constructor arguments.

### Gate: test the tray stack
```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln --filter "FullyQualifiedName~TrayActionsTests|FullyQualifiedName~SharedUpdatePresentationTests"
```

## Stage 7: Tray menu behavior tests

- [ ] 7.1 Add `tests/CLIHub.Tests/Services/TrayMenuBuilderTests.cs`; use `TestHelpers.RunOnSta` (`tests/CLIHub.Tests/Support/TestHelpers.cs:26`) for every test that touches WPF elements.
- [ ] 7.2 `Build_OnlyFavoriteProjects_AddsProjectSubmenusInStorageOrder` — two favorites plus one non-favorite; assert the top-level project submenus are the favorites in `GetAllProjects()` order and headers end with ` * `.
- [ ] 7.3 `Build_NoFavoriteProjects_AddsDisabledNoFavoriteProjectsItem`.
- [ ] 7.4 `Build_ProjectSubmenu_ListsOnlyInitializedAgentsWithRunLabels` — a launch-capable installed+available plugin, an uninstalled one, a not-available-in-project one, and a plugin without a launch command; assert only the first appears, labelled `Run <Name>`.
- [ ] 7.5 `Build_ConfiguredDefaultAgent_IsFirstFollowedBySeparatorEvenWhenNotInitialized`.
- [ ] 7.6 `Build_InitializedDefaultAgent_AppearsOnceAsFirstItemWithSeparator`.
- [ ] 7.7 `Build_NoDefaultConfigured_HasNoDefaultItemOrSeparator` and `Build_DefaultIdNotInCatalog_HasNoDefaultItemOrSeparator`.
- [ ] 7.8 `Build_ProjectWithNoDefaultAndNoInitializedAgents_ShowsDisabledNoAgentsItem`.
- [ ] 7.9 `Build_MenuTail_KeepsAddProjectSettingsWhatsNewShowAndExitInOrder` — assert `Add Project...` sits directly below the project list and before `Settings`.
- [ ] 7.10 `InvokeInitializedAgent_InvokesSelectProjectAndLaunch` — assert the item's click calls `commands.SelectProjectAndLaunchAgentAsync(project.Id, plugin)`.
- [ ] 7.11 `Populate_ExistingMenu_ReplacesItemsWithFreshState` — populate once, then `Populate` again with different state; assert the item set reflects the new state (no duplicated or stale items).

### Gate: test the changes
```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
```

## Stage 8: Full verification

- [ ] 8.1 `dotnet build CLIHub.sln` — expect 0 errors, 0 warnings.
- [ ] 8.2 `dotnet test CLIHub.sln` — expect all green.
- [ ] 8.3 `openspec validate --all` — expect 0 errors.
- [ ] 8.4 `graphify update .`

### Gate: all gates passed
```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
openspec validate --all
graphify update .
```

## Manual smoke (user-confirmed, before commit/archive)

- [ ] 9.1 Launch the app; open the tray menu and confirm: header, favorite-only project submenus in storage order with ` * `, `Add Project…`, `Settings`, `What's New`, update item, separator, `Show CLIHub`, `Exit`.
- [ ] 9.2 Confirm each project submenu's agent items are `Run <Agent>` and that invoking one makes that project current (header updates on the next open) and launches the agent in that folder.
- [ ] 9.3 Confirm the default agent is first with a separator in every project submenu, and that a project with no default and no agents shows `(no agents)`.
- [ ] 9.4 Favorite a project and change the default agent in Settings; reopen the tray menu and confirm both are reflected without restarting.
