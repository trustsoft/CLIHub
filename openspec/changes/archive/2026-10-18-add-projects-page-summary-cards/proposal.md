## Why

The Projects page manages the project registry but opens with no at-a-glance summary, unlike Project Discovery, which shows count cards in its page header. Users cannot see how many projects are manual versus discovered, and stale registry entries are only visible by scanning the list.

## What Changes

- Add three summary cards to the Projects page header, matching the Project Discovery card style: **PROJECTS** (total), **MANUAL**, and **DISCOVERED**.
- The total card shows the manual/discovered split and a folder-health line: "All folders found" when every folder exists, or "N folders missing" in the danger tone.
- The manual and discovered cards show their counts with an ownership hint ("Registered by you" / "Managed by Project Discovery").
- Counts refresh whenever the project registry changes, including a manual add or removal made on the page.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `settings-theme`: add the Projects page summary cards to the page-header presentation contract.

## Impact

- `src/CLIHub/Views/SettingsWindow.xaml`: a new summary-card template and the header trigger for the `projects-registry` page.
- `src/CLIHub/ViewModels/Projects/IProjectRegistrySection.cs` and `ProjectRegistryViewModel.cs`: registry counts and folder-health summaries refreshed from `Load()`.
- `tests/CLIHub.Tests/ViewModels/Projects/ProjectRegistryViewModelTests.cs`: count and summary assertions.
- No configuration or persistence changes.
