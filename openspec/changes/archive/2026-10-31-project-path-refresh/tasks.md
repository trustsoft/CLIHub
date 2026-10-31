# Tasks: project-path-refresh

Implementation happens in a fresh session. `design.md` carries the full rationale and file references. All tasks are sequential; each stage ends buildable and green.

## 1. Model notification fix

- [x] 1.1 Open `src/CLIHub/ViewModels/Projects/ProjectModel.cs`, locate the `Path` property setter (`:63-67`)
- [x] 1.2 Modify the setter to raise `PropertyChanged(nameof(DisplayPath))` when `SetValue` returns `true`:
  ```csharp
  public string Path
  {
      get => this.path;
      private set
      {
          if (this.SetValue(ref this.path, value))
          {
              this.OnPropertyChanged(nameof(DisplayPath));
          }
      }
  }
  ```
- [x] 1.3 Verify the `DisplayPath` property (`:134`) remains unchanged (computed property returning `this.Path`)
- [x] 1.4 `dotnet build CLIHub.sln` — verify green

## 2. Model tests

- [x] 2.1 Determine the correct test file:
  - Check if `tests/CLIHub.Core.Tests/Projects/ProjectModelCatalogTests.cs` exists (Core test)
  - Otherwise use `tests/CLIHub.Tests/ViewModels/Projects/ProjectModelTests.cs` (if it exists)
  - If neither exists, create `tests/CLIHub.Tests/ViewModels/Projects/ProjectModelTests.cs` with xUnit `public class ProjectModelTests` and required usings
- [x] 2.2 Add `ProjectModel_PathChanged_RaisesDisplayPathChanged`:
  - Create a `ProjectModel` from an enriched snapshot (use `ProjectModel`'s public constructor with a mock or fake `EnrichedProject`)
  - Subscribe to `PropertyChanged` event, tracking raised property names
  - Call `Update(snapshot with new path)` (mutate the project's `Path` in the snapshot)
  - Assert `PropertyChanged` was raised for both `"Path"` and `"DisplayPath"`
- [x] 2.3 Add `ProjectModel_PathChanged_DisplayPathReturnsNewPath`:
  - Create a `ProjectModel` with an initial path
  - Call `Update(snapshot with new path)`
  - Assert `model.DisplayPath` returns the new path
- [x] 2.4 `dotnet build CLIHub.sln && dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj` (or `CLIHub.Core.Tests` if that's where the tests landed) — verify green

## 3. View-model test extension

- [x] 3.1 Open `tests/CLIHub.Tests/ViewModels/Projects/ProjectRegistryViewModelTests.cs`
- [x] 3.2 Locate or create a `ChangePathCommand` test (likely named `ChangePath_ManualProject_CallsServiceAndReloads` or similar, added by the `project-path-correction` change)
- [x] 3.3 Extend the test to assert `DisplayPath` after `Load()`:
  - After the test calls `viewModel.Load()` (or the command that triggers `Load`), find the changed project model in `viewModel.Projects`
  - Assert `changedProject.DisplayPath` equals the new folder path (not the old path)
  - If no such test exists, create `ChangePath_UpdatesDisplayPath`:
    - Mock `IProjectService.ChangeProjectPath` and `IProjectDialogService.SelectFolder`
    - Create a view-model with a manual project
    - Invoke `ChangePathCommand` with a new folder
    - Call `Load()` (or verify the command calls it)
    - Assert the project's `DisplayPath` reflects the new path
- [x] 3.4 `dotnet build CLIHub.sln && dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj` — verify green

## 4. Integration verification

- [x] 4.1 `dotnet test CLIHub.sln` — verify all tests green
- [x] 4.2 Manual smoke (verify the defect is fixed):
  - Run the application (`dotnet run --project src/CLIHub/CLIHub.csproj`)
  - Navigate to Settings → Projects
  - Create or select a manual project
  - Note its current path text in the row
  - Click "Change project folder…", select a different existing folder, confirm
  - Verify the project row immediately shows:
    - The new project name (derived from the new folder)
    - The new full path text in the path line (`:166`) and its tooltip (`:168`)
  - Verify no restart is required to see the new path text
  - Verify the launch window also reflects the new path (if visible)
- [x] 4.3 Record the smoke result; do NOT run git write commands

## 5. Validation gate

- [x] 5.1 Run `npx --yes openspec validate project-path-refresh` from the repository root
- [x] 5.2 Verify the output reports **no errors** for the `project-path-refresh` change
- [x] 5.3 If validation fails, diagnose and fix before proceeding; quote the exact failure in the report

## 6. Finalization

- [x] 6.1 No documentation changes required (the fix restores existing specified behavior; no new concepts)
- [x] 6.2 Report completion: change id, files written, validation result, chosen fix, open questions (if any)
