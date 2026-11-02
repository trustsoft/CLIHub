# Revise the Projects page copy

## Why

The Settings → Projects page carries redundant copy and inconsistent vocabulary with its sibling
Project Scanning page. The PROJECTS summary card repeats the discovered/manual split already shown
by two sibling cards, the page description uses the internal term "provenance", the immediate-apply
note and discovered-project hint use slightly different wording than the already-approved Project
Scanning page, and the add row still uses `Register` and the old placeholder (`Enter absolute
directory path (e.g., C:\repos)…`) while Project Scanning moved to `Add` and `Absolute folder path,
e.g. C:\repos`.

The user reviewed an HTML mockup of the revised page (`projects-page-copy-mockup.html`) and approved
the wording changes. The backlog entry "Projects page add row still uses `Register` and the old
placeholder" (`docs/backlog.md:349-358`) identified the add-row inconsistency; this change closes
that entry.

## What changes

- Rewrite the page description, immediate-apply note, discovered-project hint, add-row placeholder
  and button label, summary-card details and tooltips, empty state, and two error messages.
- Replace the PROJECTS card's `ProjectMixSummary` binding (which repeats the discovered/manual split)
  with the static text `In the launch window`.
- Remove the PROJECTS card's detail tooltip (`Found by scanning or added manually`), which duplicated
  the two sibling cards.
- Update the PROJECTS card's alert tooltip (`Projects with missing folders` → `Folders that no
  longer exist`).
- Remove the unused `ProjectMixSummary` property from `IProjectRegistrySection` and
  `ProjectRegistryViewModel`, along with the `OnPropertyChanged` call and the test assertion.
- Remove the resolved backlog entry at `docs/backlog.md:349-358`.
- Keep the page title `Projects`, section header `REGISTERED PROJECTS`, card labels
  `PROJECTS` / `MANUAL` / `DISCOVERED`, button labels `Browse…` / `Save` / `Cancel`, row tooltips
  (`Change project folder`, `Open project folder`, `Remove project`), the `Folder not found` chip,
  provenance tooltips (`Manual` / `Discovered`), shared validation messages, and all folder-picker /
  removal-confirmation dialog text unchanged.

## Impact

- UI: `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs`,
  `src/CLIHub/Views/SettingsProjectRegistryPage.xaml`, `src/CLIHub/Views/SettingsWindow.xaml`,
  `src/CLIHub/ViewModels/Projects/IProjectRegistrySection.cs`,
  `src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs`
- Tests: `tests/CLIHub.Tests/ViewModels/Projects/ProjectRegistryViewModelTests.cs` (remove the
  `ProjectMixSummary` assertion; update the change-path error expectation)
- Docs: `docs/backlog.md` (remove the resolved entry)
- No spec deltas: these strings are not quoted in any durable specification, and behavior is
  unchanged.
- No configuration-key or scanning change. The only layout change is moving the two notes above the
  `REGISTERED PROJECTS` header, so they sit exactly where the immediate-apply note sits on Project
  Scanning.
