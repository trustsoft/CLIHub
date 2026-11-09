# Tasks: Tidy Application Folder Structure

## Stage 1: Dissolve Helpers/ (source + tests + usings)

- [x] 1.1 Move `src/CLIHub/Helpers/AgentVersionText.cs` → `src/CLIHub/ViewModels/Agents/AgentVersionText.cs`
  - Change namespace from `namespace CLIHub.Helpers;` to `namespace CLIHub.ViewModels;` (line 1)
  - Keep type name, behavior, and XML documentation unchanged

- [x] 1.2 Move `tests/CLIHub.Tests/Helpers/AgentVersionTextTests.cs` → `tests/CLIHub.Tests/ViewModels/Agents/AgentVersionTextTests.cs`
  - Change namespace from `namespace CLIHub.Tests.Helpers;` to `namespace CLIHub.Tests.ViewModels.Agents;` (line 1)
  - Remove `using CLIHub.Helpers;` (line 3)
  - Add `using CLIHub.ViewModels;` after the namespace declaration

- [x] 1.3 Update `src/CLIHub/ViewModels/Agents/AgentCardItem.cs`
  - Remove `using CLIHub.Helpers;` (line 6)
  - No replacement needed (source now in same namespace)

- [x] 1.4 Move `src/CLIHub/Helpers/PreferenceSyncHelper.cs` → `src/CLIHub/ViewModels/Shared/PreferenceSyncHelper.cs`
  - Change namespace from `namespace CLIHub.Helpers;` to `namespace CLIHub.ViewModels;` (line 1)
  - Keep existing using directives and type behavior unchanged

- [x] 1.5 Move `tests/CLIHub.Tests/Helpers/PreferenceSyncHelperTests.cs` → `tests/CLIHub.Tests/ViewModels/Shared/PreferenceSyncHelperTests.cs`
  - Change namespace from `namespace CLIHub.Tests.Helpers;` to `namespace CLIHub.Tests.ViewModels.Shared;` (line 1)
  - Replace `using CLIHub.Helpers;` (line 5) with `using CLIHub.ViewModels;`
  - Keep existing Core imports unchanged

- [x] 1.6 Update `src/CLIHub/ViewModels/Launch/LaunchWindowViewModel.cs`
  - Remove `using CLIHub.Helpers;` (line 9)
  - No replacement needed (source now in same namespace)

- [x] 1.7 Delete empty folders
  - Delete `src/CLIHub/Helpers/`
  - Delete `tests/CLIHub.Tests/Helpers/`

- [x] 1.8 Build gate: `dotnet build CLIHub.sln`
  - Expect: Clean build with 0 errors

- [x] 1.9 Test gate: `dotnet test CLIHub.sln`
  - Expect: All tests pass (same count as baseline)

## Stage 2: Move Diagnostics/ file + App.xaml.cs + architecture guard

- [x] 2.1 Create `src/CLIHub/Diagnostics/` directory

- [x] 2.2 Move `src/CLIHub/Views/ReadmeImageCapture.cs` → `src/CLIHub/Diagnostics/ReadmeImageCapture.cs`
  - Change namespace from `namespace CLIHub.Views;` (line 2) to `namespace CLIHub.Diagnostics;`
  - Keep `#if DEBUG` guard and all implementation unchanged

- [x] 2.3 Update `src/CLIHub/App.xaml.cs`
  - Change line 37 from `CLIHub.Views.ReadmeImageCapture.TryStart(this)` to `CLIHub.Diagnostics.ReadmeImageCapture.TryStart(this)`

- [x] 2.4 Update architecture guard: `tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs`
  - Add `"Diagnostics" + Path.DirectorySeparatorChar + "ReadmeImageCapture.cs"` to the `WpfAllowedApplicationFiles` array (after line 23, before the closing bracket at line 24)
  - Maintain alphabetical or logical ordering within the array

- [x] 2.5 Build gate: `dotnet build CLIHub.sln`
  - Expect: Clean build with 0 errors

- [x] 2.6 Test gate: `dotnet test CLIHub.sln`
  - Expect: All tests pass, including `ApplicationWpfReferences_AreLimitedToExplicitUiAdapters`

## Stage 3: Merge ViewModels/Menu/

- [x] 3.1 Create `src/CLIHub/ViewModels/Shared/` directory if it does not exist (it will exist after Stage 1.4)

- [x] 3.2 Move `src/CLIHub/ViewModels/Menu/MenuAction.cs` → `src/CLIHub/ViewModels/Shared/MenuAction.cs`
  - Keep namespace `namespace CLIHub.ViewModels;` unchanged (line 1)
  - Keep all type behavior unchanged

- [x] 3.3 Delete empty folder
  - Delete `src/CLIHub/ViewModels/Menu/`

- [x] 3.4 Build gate: `dotnet build CLIHub.sln`
  - Expect: Clean build with 0 errors

- [x] 3.5 Test gate: `dotnet test CLIHub.sln`
  - Expect: All tests pass

## Stage 4: Documentation updates + final gates

- [x] 4.1 Update `docs/repo-structure.md:53`
  - Remove `Helpers/` from the listed subsystems
  - Add `Diagnostics/` to the listed subsystems
  - Note the removal of the one-file `ViewModels/Menu/` folder (implicit in the updated description)
  - Keep the documented flat `CLIHub.ViewModels` namespace convention unchanged

- [x] 4.2 Update `docs/images/README.md:79`
  - Change `src/CLIHub/Views/ReadmeImageCapture.cs` to `src/CLIHub/Diagnostics/ReadmeImageCapture.cs`

- [x] 4.3 Update `docs/backlog.md:450`
  - Change `src/CLIHub/ViewModels/Menu/MenuAction.cs` to `src/CLIHub/ViewModels/Shared/MenuAction.cs`

- [x] 4.4 Build gate: `dotnet build CLIHub.sln`
  - Expect: Clean build with 0 errors

- [x] 4.5 Test gate: `dotnet test CLIHub.sln`
  - Expect: All tests pass, same count as baseline

- [x] 4.6 OpenSpec validation gate:
  ```powershell
  & "C:\Users\Maxim\AppData\Local\npm-cache\_npx\20f2b75ddc8bce88\node_modules\.bin\openspec.cmd" validate --all
  ```
  - Expect: `--all` reports 0 failed (this `skip_specs` change is validated separately and is not counted in the `--all` item total)
  - Also run `openspec validate tidy-application-folders` and expect "Change 'tidy-application-folders' is valid"
  - Pre-existing warnings are acceptable

## Completion

After all tasks pass:
- All file moves complete
- All using directives updated
- Architecture guard allows `Diagnostics/ReadmeImageCapture.cs`
- Documentation references updated
- Build, test, and OpenSpec validation gates pass
- No source behavior changes; pure internal refactor verified

Ready for commit and archive (requires explicit user approval per AGENTS.md).
