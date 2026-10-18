## Why

The Projects & Paths page offers one header action, "Scan Now", which re-reads the saved scanning preferences and rescans every watched root. When a user has just changed a single large folder, that rescans unrelated trees and delays the result they asked for. The watched-directories list has no way to refresh one entry.

## What Changes

- Add a per-directory "Scan" action to each watched-directory row, placed before the remove action.
- Rescanning one root enumerates only that root with the stored scanning settings (depth, ignored patterns, discovery mode).
- Reconcile the pass against that root only: add newly discovered repositories beneath it, remove discovered projects beneath it that no longer exist, and leave projects under other roots untouched.
- Keep manually registered projects, and remove nothing when the root is missing or unreadable.
- Report the pass through the existing scanner status: scanning while it runs, then the total discovered-project count.
- Keep the header "Scan Now" as the full rescan of all saved roots.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `project-scanning`: Adds a "Single-root rescan" requirement covering the scoped enumeration and reconciliation.
- `preferences-ui`: The "Projects and Paths page options" requirement gains the per-row Scan action in the watched-directories list.

## Impact

- `src/CLIHub.Core/Scanning/IProjectScanner.cs` — a `Rescan(string root)` operation.
- `src/CLIHub.Core/Scanning/ProjectScanningService.cs` — scoped scan pass and scoped removal computation.
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` — a command that forwards a row's root to the scanner.
- `src/CLIHub/Views/SettingsProjectsPage.xaml` — the row action button.
- `docs/architecture/scanning.md` — describe the scoped pass.
- Tests: scanning-service scoped reconciliation and the view-model command.
- No configuration schema changes.
