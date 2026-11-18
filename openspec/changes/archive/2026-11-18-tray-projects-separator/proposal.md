## Why

The tray context menu runs the disabled header, the favorite-project submenus (or the disabled
`(no favorite projects)` placeholder), and `Add Project…` back to back with no visual grouping, so two
horizontal separators are added — one below the header and one below the project list. Separately, the
header itself currently shows the current project (`Current: <name>` / `No project selected`), which is
redundant with the launch window; the user wants the tray's disabled first item to show the application
version (`Version <version>`) instead, and the current project to no longer appear in the tray menu.

## What Changes

- `TrayMenuBuilder.Populate` (`src/CLIHub/Tray/TrayMenuBuilder.cs:36-85`) inserts two WPF `Separator`s:
  one directly after the disabled header (`:50`), before the first favorite project submenu (or the
  `(no favorite projects)` placeholder); and one after the favorite-project block (`:68`), before the
  `Add Project…` item. The pre-existing separator before `Exit` (`:80`) is untouched — three separators
  in total.
- The header becomes the version header. `TrayMenuState` replaces its first field
  `Project? CurrentProject` with `string Version` (`src/CLIHub/Tray/TrayMenuState.cs:23-27`);
  `TrayStateProjection.GetState()` supplies `this.updates.GetCurrentVersion()` instead of
  `this.projects.GetCurrentProject()` (`src/CLIHub/Tray/TrayStateProjection.cs:68-73`); and
  `TrayMenuBuilder` renders `Header = $"Version {state.Version}"` (still `IsEnabled = false`,
  `TrayMenuBuilder.cs:44-48`). The version comes from `IUpdateWorkflow`, which is an
  `IUpdateVersionProvider` (`src/CLIHub/Updates/IUpdateWorkflow.cs:8`) whose `GetCurrentVersion()`
  returns a normalized version or an `unknown` fallback, never empty
  (`src/CLIHub.Core/Updates/UpdateService.cs:103-114`). The tray no longer reads or displays the
  current project.
- Spec delta: two MODIFIED requirements — "Tray menu structure"
  (`openspec/specs/tray-menu/spec.md:11-38`) and "Add Project is placed below the project list"
  (`:253-266`). "Tray menu structure" changes its first element to a `Version <version>` header,
  rewrites the two current-project header scenarios to the version header (their names are kept because
  a MODIFIED requirement cannot drop scenarios), and adds one new scenario "Header shows the current
  version" alongside the two separator scenarios and the pre-existing tail/removal scenarios; "Add
  Project is placed below the project list" is unchanged from the prior round.

## Impact

- `src/CLIHub/Tray/TrayMenuState.cs` — record shape: `Project? CurrentProject` → `string Version`.
- `src/CLIHub/Tray/TrayStateProjection.cs` — `GetState()` supplies the version; the projection already
  holds `IUpdateWorkflow`, so no new dependency is introduced.
- `src/CLIHub/Tray/TrayMenuBuilder.cs` — the header item formats the version; the two separators.
- `tests/CLIHub.Tests/Services/TrayMenuBuilderTests.cs` — `CreateState` drops the `currentProject`
  plumbing and stubs `GetCurrentVersion()`; the two `"No project selected"` header assertions become the
  version header; the four update-state mocks stub `GetCurrentVersion()`.
- `tests/CLIHub.Tests/Services/TrayActionsTests.cs` — `GetState_…` replaces its `state.CurrentProject`
  assertion with a `state.Version` assertion and stubs the update workflow's `GetCurrentVersion()`.
- `openspec/specs/tray-menu/spec.md` — after archive, the modified requirement text replaces the
  current top-to-bottom order, and the Purpose sentence is updated post-archive (see `tasks.md`).

`skip_specs` is **not** set: this is an observable tray-menu behavior change with a MODIFIED delta at
`specs/tray-menu/spec.md`.

## Non-Goals

- Not removing the current-project concept from the application: `IProjectService.GetCurrentProject()`
  and the launch flow that selects a project still track it; only the tray menu stops surfacing it.
- Not changing icons, labels, commands, the `Updates` submenu, refresh-on-open, or the themed
  rendering.
- Not touching the existing separator before `Exit`.
- Not adding a `Show CLIHub` item or otherwise restructuring the menu.
- Not changing `TrayMenuCommands` or `TrayProjectAgents`; the state-shape change is `TrayMenuState` only.

## Dependencies

None. Self-contained inside the tray stack (`TrayMenuState`, `TrayStateProjection`, `TrayMenuBuilder`)
and their tests; no other change is assumed.
