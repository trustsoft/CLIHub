## Why

Projects can be added from the launch window but never removed from it. `IProjectService.RemoveProject` exists and is already covered by the `project-management` spec, but the window exposes no control to reach it, so users must edit `%APPDATA%\CLIHub\config.json` to drop a project.

## What Changes

- Add a "Remove Project" button beside "Add Project..." in the launch window's Projects pane.
- Require a confirmation prompt before removing; the project folder and its files are never deleted.
- Refresh the project list and agent availability after removal (removing the current project clears the launch context).

## Capabilities

### New Capabilities
<!-- None. -->

### Modified Capabilities
- `project-management`: the "Project removal" requirement gains a scenario making removal available from the launch window with a confirmation prompt.

## Impact

- `src/CLIHub/Windows/MainWindow.xaml` — add the button to the Projects pane action row.
- `src/CLIHub/Windows/MainWindow.xaml.cs` — add the click handler (confirm + `RemoveProject` + refresh).
- No changes to `CLIHub.Core`, services, configuration schema, or dependencies.
