## 1. Registry section view model

- [x] 1.1 Add `ProjectRegistryItem` (id, name, full path, manual provenance, provenance label, missing-folder flag) with the folder check recomputed on refresh; verify with unit tests that manual and discovered rows carry the right provenance and that a deleted folder is flagged.
- [x] 1.2 Add `IProjectRegistrySection` and `ProjectRegistryViewModel` with `Load()`, a row collection, add and remove commands, an error line, a `ProjectsChanged` subscription, and `Dispose()` that unsubscribes; verify XML documentation on public members and a unit test that `Dispose` removes the subscription.
- [x] 1.3 Implement add as a validated path field with a browse action plus `IProjectService.AddProject` without changing the current project; verify with a unit test that the current project is untouched and that a duplicate folder creates no second row.
- [x] 1.4 Implement remove as `ConfirmProjectRemoval` followed by `IProjectService.RemoveProject`; verify with unit tests that confirmation removes the row, cancellation leaves it, and discovered rows expose no remove action.
- [x] 1.5 Add `ProjectRegistryViewModelTests` following `MethodOrScenario_Condition_ExpectedResult`; verify `dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj` passes.
- [x] 1.6 Add an open-folder command backed by `IExternalLauncher`, enabled for every project whose folder exists; verify with unit tests that it opens the path and is unavailable for a missing folder.

## 2. Settings page and composition

- [x] 2.1 Register the `projects-registry` page (title "Projects", group "Engines & Repos", implemented) before the existing `projects` entry in `SettingsViewModel.AllPages`; verify `SettingsNavigationTests` is updated to nine pages and the new key order and passes.
- [x] 2.2 Create `SettingsProjectRegistryPage` with a registered-projects section, list rows showing name, full path, provenance and a missing-folder note, a remove action for manual rows only, a hint that discovery manages discovered projects, a dashed add row, and a note that changes apply immediately; verify it builds and renders with the existing settings styles.
- [x] 2.3 Add the page control to the `SettingsWindow.xaml` content switch with a visibility trigger on `SelectedPage.Key = "projects-registry"`; verify the page appears when "Projects" is selected.
- [x] 2.4 Inject the section into `SettingsViewModel` (constructor, `Load()`, `Dispose()`) and register it in `ServiceRegistration.cs`; verify the application starts and the page lists projects.
- [x] 2.5 Add a per-row open-folder button using the `FolderOpen` glyph, placed with the row actions; verify the page builds and the action is disabled for a missing folder.

## 3. Integration and verification

- [x] 3.1 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`; verify no errors or failures.
- [x] 3.2 Run `openspec validate --all`; verify the change and all specs are valid.
- [x] 3.3 Run `graphify update .` and `node .gitnexus/run.cjs detect-changes --scope all --repo .`; verify the change set contains only the intended files and review any reported risk.
- [x] 3.4 Manually smoke-test: open Settings, add a manual project (confirm the launch window selection does not change), remove it with confirmation, confirm a discovered project has no remove action, and verify the Launch Window and tray reflect each change without restart.
