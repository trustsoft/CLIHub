## 1. Registry counts

- [x] 1.1 Add `ManualCount`, `DiscoveredCount`, `TotalCount`, `MissingFolderCount`, `ProjectMixSummary`, `FolderStatusSummary`, and `HasMissingFolders` to `IProjectRegistrySection`; compute them from the rebuilt rows in `Load()` and raise property changes; verify with unit tests for the counts, the split text, the empty zero-missing text, and the pluralized missing text.
- [x] 1.2 Verify the counts update after a manual add and after a manual removal in the section tests.

## 2. Summary cards

- [x] 2.1 Add a `RegistrySummaryCards` data template to `SettingsWindow.xaml` with the PROJECTS, MANUAL, and DISCOVERED cards, using the existing card padding, icon block, number, and hint styles; verify the page builds.
- [x] 2.2 Show the template for `SelectedPage.Key = "projects-registry"` in the fixed summary area; verify the cards appear under the Projects page header.
- [x] 2.3 Show the folder-health line in the danger tone only when a folder is missing and hide it when every folder exists; verify both states.

## 3. Verification

- [x] 3.1 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`; verify no errors or failures.
- [x] 3.2 Run `openspec validate --all`; verify the change and all specs are valid.
- [x] 3.3 Manually check the Projects page: three cards, split text, folder-health line, and immediate refresh after add/remove.
