# Tasks: project-path-correction

Implementation happens in a fresh session. `design.md` carries the full rationale and file references. All tasks are sequential; each stage ends buildable and green.

## 1. Service API and Core tests

- [x] 1.1 Add `void ChangeProjectPath(string projectId, string newFolderPath)` to `IProjectService` (`src/CLIHub.Core/Projects/IProjectService.cs`) with XML documentation covering the preserved fields (ID, provenance, favorite, last-used), the re-derived name, logo cache invalidation, and the exception contract (`ArgumentException` for invalid/non-manual/duplicate; `DirectoryNotFoundException` for missing folder)
- [x] 1.2 Implement `ChangeProjectPath` in `ProjectService` (`src/CLIHub.Core/Projects/ProjectService.cs`):
  - Validate `projectId` exists and has `Source == ProjectSource.Manual` (throw `ArgumentException` otherwise)
  - Validate `newFolderPath` non-blank, normalize via `ProjectPathPolicy.Normalize`, require `Directory.Exists` (throw `DirectoryNotFoundException` otherwise)
  - Deduplicate: find existing project with `ProjectPathPolicy.AreEqual(p.Path, normalized)` — same project ID → return early (no-op); different project ID → throw `ArgumentException`
  - Update `project.Path` to normalized, re-derive `project.Name` via `Path.GetFileName(normalized.TrimEnd(...))` (fallback to normalized if empty, same as `CreateProject` `:168,175-178`)
  - Call `this.logoResolver.Remove(projectId)`
  - Preserve `Id`, `Source`, `IsFavorite`, `LastUsed`, and `CurrentProjectId` unchanged
  - Call `this.Persist()`
- [x] 1.3 Add Core tests in `tests/CLIHub.Core.Tests/Projects/ProjectServiceTests.cs`:
  - `ChangeProjectPath_ValidPath_UpdatesPathAndName` — verify path normalized, name derived from folder, ID/Source/IsFavorite/LastUsed preserved
  - `ChangeProjectPath_SameProjectPath_NoOp` — call with current path (normalized), verify no event raised (mock `ProjectsChanged` subscriber)
  - `ChangeProjectPath_DifferentProjectPath_ThrowsArgumentException` — register two projects, attempt to change one to the other's path
  - `ChangeProjectPath_DiscoveredProject_ThrowsArgumentException` — create discovered project, verify change rejected
  - `ChangeProjectPath_NonExistentProject_ThrowsArgumentException` — invalid project ID
  - `ChangeProjectPath_BlankPath_ThrowsArgumentException` — empty/whitespace `newFolderPath`
  - `ChangeProjectPath_MissingFolder_ThrowsDirectoryNotFoundException` — non-existent folder
  - `ChangeProjectPath_InvalidatesLogoCache` — mock or verify `logoResolver.Remove(id)` called (if possible with existing test infrastructure)
  - `ChangeProjectPath_PersistsImmediately` — verify config written (use `FakeConfigurationRepository` and check `SavedConfig`)
  - `ChangeProjectPath_PreservesCurrentSelection` — set project as current, change its path, verify still current
- [x] 1.4 `dotnet build CLIHub.sln && dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj` — verify green

## 2. ViewModel command and UI tests

- [x] 2.1 Add `public RelayCommand<ProjectModel> ChangePathCommand { get; }` to `ProjectRegistryViewModel` (`src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs`)
- [x] 2.2 Initialize in constructor: `this.ChangePathCommand = new RelayCommand<ProjectModel>(this.ChangePath, item => item.IsManual);`
- [x] 2.3 Add private `ChangePath(ProjectModel item)` method:
  - Guard: return if `item is null || !item.IsManual`
  - Clear `this.ErrorText = null`
  - Call `this.dialogs.SelectFolder("Change Project Folder")`
  - If result is null/whitespace, return (user canceled)
  - Try `this.projects.ChangeProjectPath(item.Id, folder)`, then `this.Load()`
  - Catch `DirectoryNotFoundException` → `this.ErrorText = "The selected folder does not exist: {ex.Message}"`
  - Catch `ArgumentException` → `this.ErrorText = "Could not change project path: {ex.Message}"`
  - Catch generic `Exception` → `this.ErrorText = "Could not change project path: {ex.Message}"`
- [x] 2.4 Add tests in `tests/CLIHub.Tests/ViewModels/Projects/ProjectRegistryViewModelTests.cs`:
  - `ChangePath_ManualProject_CallsServiceAndReloads` — mock dialog returns valid path, verify `ChangeProjectPath` called with correct ID and path, `Load()` re-enriches
  - `ChangePath_UserCancelsDialog_NoServiceCall` — dialog returns null, verify no service call
  - `ChangePath_ServiceThrowsDirectoryNotFound_DisplaysError` — mock service throws, verify `ErrorText` set
  - `ChangePath_ServiceThrowsArgumentException_DisplaysError` — duplicate path scenario, verify error displayed
  - `ChangePath_DiscoveredProject_CommandDisabled` — verify `ChangePathCommand.CanExecute(discoveredModel)` is false
  - `ChangePath_ManualProject_CommandEnabled` — verify `ChangePathCommand.CanExecute(manualModel)` is true
- [x] 2.5 `dotnet build CLIHub.sln && dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj` — verify green

## 3. XAML UI integration

- [x] 3.1 Open `src/CLIHub/Views/SettingsProjectRegistryPage.xaml`, locate the row action panel (`SpacedPanel` at `:171-201`)
- [x] 3.2 Insert a new `Button` **before** the "Open project folder" button (currently `:177-187`):
  - `Width`/`Height` from `Sizing.IconButton.*` resources
  - `AutomationProperties.Name="Change project folder"`
  - `Command="{Binding DataContext.ProjectRegistry.ChangePathCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"`
  - `CommandParameter="{Binding}"`
  - `Style="{StaticResource ChipRemoveButton}"` (reuse existing style)
  - `ToolTip="Change project folder"`
  - `Visibility="{Binding IsManual, Converter={StaticResource BoolToVisibility}}"` (only manual projects)
  - Icon: `<TextBlock FontFamily="{StaticResource Sizing.FontFamily.Icon}" FontSize="{StaticResource Sizing.FontSize.Glyph}" Text="{x:Static themes:IconGlyphs.Edit}"/>` (pencil, distinct from the adjacent open-folder glyph)
- [x] 3.3 Verify the button appears before "Open project folder" and after the project metadata in the row
- [x] 3.4 `dotnet build CLIHub.sln` — verify green

## 4. Integration and smoke tests

- [x] 4.1 `dotnet test CLIHub.sln` — verify all tests green
- [ ] 4.2 Manual smoke:
  - Run the application (`dotnet run --project src/CLIHub/CLIHub.csproj`)
  - Navigate to Settings → Projects
  - Verify a manual project row shows three buttons: "Change folder…" (new), "Open project folder", "Remove project"
  - Verify a discovered project row shows only "Open project folder" (no "Change folder…", no "Remove project")
  - Click "Change folder…" on a manual project, select a different existing folder, verify:
    - Project name updates to the new folder name
    - Project path updates to the new folder path
    - Logo refreshes (if the new folder has a different logo or no logo, the row reflects it)
    - Favorite flag and last-used timestamp unchanged
    - Error line clears if previously set
  - Click "Change folder…" and cancel the dialog — verify no change occurs
  - Click "Change folder…" and select a folder already registered to a **different** project — verify error: "Could not change project path: The selected folder is already registered to a different project."
  - Click "Change folder…" and select a non-existent folder (manually type a bad path if the dialog prevents it) — verify error: "The selected folder does not exist"
  - Change a project's path while it is the current selection — verify it remains selected in the launch window
  - Change a project's path, then navigate to the launch window — verify the row reflects the new name/path/logo without restarting
- [x] 4.3 Record the smoke results and report them; do NOT run any git write command. The orchestrator verifies independently and asks the user for commit approval.

## 5. Validation gate

- [x] 5.1 Run `npx --yes openspec validate project-path-correction` from the repository root
- [x] 5.2 Verify the output reports **no errors** for the `project-path-correction` change
- [x] 5.3 If validation fails, diagnose and fix before proceeding

## 6. Documentation and finalization

- [x] 6.1 No doc changes required (behavior is self-evident from the UI; no new concepts introduced)
- [x] 6.2 Report completion: files written, validation result, API decisions, open questions (if any)
