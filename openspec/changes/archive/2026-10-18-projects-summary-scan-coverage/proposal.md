## Why

The "Projects & Paths" page header shows a PROJECTS summary card whose only number is the discovered-project
count, captioned "Scanned repositories". That caption is misleading: the scanner counts a repository exactly when
it finds one, so "found projects" and "scanned repositories" are the same number. A user tuning scan depth,
ignored patterns, or discovery mode therefore cannot tell whether the scan actually reached the configured
folders or stopped at the first level.

The same card also silently excludes projects added manually, so its number disagrees with the project list in
the launch window and does not answer "how many projects do I have, and where did they come from?".

Scan coverage — how many folders the last pass examined — is the missing diagnostic number. It answers "did the
scan look at my folders?" and makes misconfigured depth or ignored patterns visible at a glance.

## What Changes

- `RepositoryDiscovery` reports, per successfully scanned root, how many directories the walk examined.
- `ProjectScanStatus` carries the aggregated folder count of the last scan so every status surface can show it.
- `ProjectScanningService` aggregates coverage per watched root: a full scan replaces the per-root counts, a
  single-root rescan updates only that root, and stopping the scanner or removing every watched root clears them.
- The scanner exposes the number of manually added projects beside the existing discovered-project count, so the
  page can present the full project inventory.
- The PROJECTS summary card shows the total number of registered projects (discovered plus manual) as its number
  and the provenance split beneath it ("31 discovered · 4 manual"), refreshed on every scanner status change.
- The watcher (DAEMON) card keeps the scanner state and appends the examined-folder count to its last scan time
  ("Last scan 15:04:12 · 412 folders"), using the singular "folder" for one.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `project-scanning`: The "Background watcher with status" requirement gains the number of folders examined by
  the last scan, aggregated across the watched roots, and a scenario stating that a single-root rescan refreshes
  only that root's coverage.
- `settings-theme`: The "Persistent page header" requirement gains the summary card content: the PROJECTS card
  shows the total project count with the discovered/manual split, and the watcher card shows the examined-folder
  count after its last scan time.

## Impact

- `src/CLIHub.Core/Scanning/RepositoryDiscovery.cs`, `DiscoveryResult.cs` — per-root examined-folder counts.
- `src/CLIHub.Core/Scanning/ProjectScanStatus.cs`, `ProjectScanningService.cs` — status metric, per-root
  aggregation, and the manual-project count query.
- `src/CLIHub.Core/Scanning/IProjectScanner.cs` — manual-project count on the page-support query surface.
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` — total project count, provenance split, and the
  watcher detail suffix.
- `src/CLIHub/Views/SettingsWindow.xaml` — PROJECTS card number and hint; the watcher hint keeps its binding.
- Tests: `RepositoryDiscoveryTests`, `ProjectScanningServiceTests`, `SettingsViewModelApplicationTests`.
- `docs/architecture/scanning.md` — status surface and summary description.
- No configuration, persistence, or schema changes: coverage and project counts are runtime-only.
