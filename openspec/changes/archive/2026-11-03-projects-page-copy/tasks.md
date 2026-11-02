# Tasks: revise the Projects page copy

## Stage 1: Page description and notes

- [x] 1.1 Page description: `Where projects come from and how to manage them.` (`SettingsNavigation.cs:58`)
- [x] 1.2 Immediate-apply note: `Changes apply immediately. Save and Cancel only affect the other pages.` (`SettingsProjectRegistryPage.xaml:29`, was `:33`)
- [x] 1.3 Discovered note: `Discovered projects are read-only – manage them in Project Scanning.` (`SettingsProjectRegistryPage.xaml:34`, was `:38`)
- [x] 1.4 Move both notes above the `REGISTERED PROJECTS` header, the immediate-apply note first at `Margin="0,2,0,0"` and the discovered note at `Margin="0,2,0,0"`, matching Project Scanning (`SettingsProjectRegistryPage.xaml:28-41`)

**Gate:** `dotnet build CLIHub.sln` — expect 0 warnings, 0 errors.

## Stage 2: Add row, summary cards, and unused property cleanup

- [x] 2.1 Add-row placeholder: `Absolute folder path, e.g. C:\repos` (`SettingsProjectRegistryPage.xaml:68`)
- [x] 2.2 Add button: `Add` (`SettingsProjectRegistryPage.xaml:81`)
- [x] 2.3 PROJECTS card: replace `Detail="{Binding ProjectRegistry.ProjectMixSummary}"` with `Detail="In the launch window"` (`SettingsWindow.xaml:198`)
- [x] 2.4 PROJECTS card: remove the `DetailToolTip="Found by scanning or added manually"` line (`SettingsWindow.xaml:199`)
- [x] 2.5 PROJECTS card: alert tooltip `Folders that no longer exist` (`SettingsWindow.xaml:197`)
- [x] 2.6 MANUAL card: `Added by you` (`SettingsWindow.xaml:205`)
- [x] 2.7 DISCOVERED card: `From watched folders` (`SettingsWindow.xaml:210`)
- [x] 2.8 Remove `ProjectMixSummary` property and XML doc from `IProjectRegistrySection.cs:38-40`
- [x] 2.9 Remove `ProjectMixSummary` property implementation from `ProjectRegistryViewModel.cs:78`
- [x] 2.10 Remove `OnPropertyChanged(nameof(this.ProjectMixSummary))` call from `ProjectRegistryViewModel.cs:310`
- [x] 2.11 Remove `ProjectMixSummary` assertion from `ProjectRegistryViewModelTests.cs:72`

**Gate:** `dotnet build CLIHub.sln` — expect 0 warnings, 0 errors; `dotnet test CLIHub.sln`.

## Stage 3: Empty state, error copy, and test expectation

- [x] 3.1 Empty state: `No projects yet – add a folder above.` (`SettingsProjectRegistryPage.xaml:206`)
- [x] 3.2 Duplicate error: `This folder is already registered.` (`ProjectRegistryViewModel.cs:208`)
- [x] 3.3 Change-folder error: `Folder not found: {ex.Message}` (`ProjectRegistryViewModel.cs:264`)
- [x] 3.4 Update change-folder error test expectation from `The selected folder does not exist:` to `Folder not found:` (`ProjectRegistryViewModelTests.cs:364`)

**Gate:** `dotnet build CLIHub.sln` — expect 0 warnings, 0 errors; `dotnet test CLIHub.sln`.

## Stage 4: Backlog cleanup and full gates

- [x] 4.1 Remove backlog entry "Projects page add row still uses `Register` and the old placeholder" (`docs/backlog.md:349-358`, including heading, severity, evidence, proposed remedy, and trigger)
- [x] 4.2 Gate: `dotnet build CLIHub.sln`
- [x] 4.3 Gate: `dotnet test CLIHub.sln`
- [x] 4.4 Gate: `openspec validate --all`
- [x] 4.5 Gate: `graphify update .`
- [x] 4.6 Update the stale `FolderStatusSummary` XML doc from "shown under the split" to "shown as the PROJECTS card's alert line" (`IProjectRegistrySection.cs`)

## Stage 5: Manual smoke and commit/archive approval

- [x] 5.1 Manual smoke: user opens Settings → Projects and confirms the revised copy, including:
  - Page description under the page title
  - Immediate-apply note (first hint)
  - Discovered note (second hint)
  - Add-row placeholder and `Add` button
  - Summary cards: PROJECTS detail "In the launch window", MANUAL detail "Added by you", DISCOVERED detail "From watched folders"
  - Empty state (if visible, or confirm in the XAML)
  - Test the duplicate error by entering an existing folder path
  - Verify PROJECTS card alert tooltip on hover (when a folder is missing)

Confirmed by the user ("Smoke ok — коммить", 2026-11-03). The confirmed state includes the en dash
(U+2013) and the two notes placed above the `REGISTERED PROJECTS` header.

**Gate:** Wait for user's smoke confirmation before commit/archive approval.

## Notes

- The test at `ProjectRegistryViewModelTests.cs:175` asserts `Contains("already registered")` for
  the duplicate error; it still passes with the new wording "This folder is already registered."
- The three shared validation messages (`Enter a folder path or browse for one.`, `Enter an existing
  absolute folder path.`, `Enter a valid absolute folder path.`) remain unchanged; they are
  explicitly out of scope and planned for a separate follow-up on both pages together.
- Stage gates assume a clean build and passing tests; exact test counts depend on the current test
  suite state and are verified at execution time.
