## Why

Each watched-directory row repeats one global summary line: the discovery mode and "recursive depth N". It tells the user how scanning is configured, but not what the scan actually found in that folder, so a row with many repositories looks the same as an empty one. "Recursive depth" is also more wording than the field needs.

## What Changes

- Append the number of discovered repositories beneath that specific watched root to each row's secondary line, with singular/plural wording ("1 repository", "N repositories").
- Refresh the per-row count whenever the watched list or the scanner status changes, so a completed scan updates every row.
- Shorten the label "recursive depth" to "depth".
- Rename the discovery options from "Git repositories", "Agent folders", and "Both" to "Git only", "Agents only", and "Git and agents", so the selected value reads clearly in the row summary.
- Rewrite the "Repository discovery" hint with concrete agent-marker examples instead of the internal "plugin markers" wording.
- Keep the rest of the row unchanged: path, folder icon, scan and remove actions.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `preferences-ui`: The "Projects and Paths page options" requirement describes the per-row secondary line with the discovery mode, depth, and the number of discovered repositories beneath that root, and names the discovery options "Git only", "Agents only", and "Git and agents".

## Impact

- `src/CLIHub.Core/Scanning/IProjectScanner.cs` — a query for the discovered-project count beneath a root.
- `src/CLIHub.Core/Scanning/ProjectScanningService.cs` — implements the query through the project service.
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` — per-root counts refreshed from the scanner; "depth" label.
- `src/CLIHub/Converters/WatchedRootSummaryConverter.cs` — joins the summary and the count for the row.
- `src/CLIHub/Views/SettingsProjectsPage.xaml` — row summary binding.
- `docs/architecture/scanning.md` — describe the count query.
- Tests: view-model summary text and per-root counts, scanning-service count query.
