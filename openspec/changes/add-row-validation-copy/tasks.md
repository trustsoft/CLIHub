# Tasks: revise the three shared add-row validation messages

## Stage 1: Projects view model (3 strings)

- [x] 1.1 Empty input: `Choose a folder or type its path.` (`ProjectRegistryViewModel.cs:182`)
- [x] 1.2 Not fully qualified OR missing folder: `Use a full folder path that exists.` (`ProjectRegistryViewModel.cs:188`)
- [x] 1.3 Normalization failure: `That path isn't valid.` (`ProjectRegistryViewModel.cs:199`)

**Gate:** `dotnet build CLIHub.sln` — expect 0 warnings, 0 errors.

## Stage 2: Project Scanning view model (3 strings)

- [x] 2.1 Empty input: `Choose a folder or type its path.` (`WatchedDirectoryEditor.cs:470`)
- [x] 2.2 Not fully qualified OR missing folder: `Use a full folder path that exists.` (`WatchedDirectoryEditor.cs:476`)
- [x] 2.3 Normalization failure: `That path isn't valid.` (`WatchedDirectoryEditor.cs:487`)

**Gate:** `dotnet build CLIHub.sln` — expect 0 warnings, 0 errors.

## Stage 3: Test assertion update

- [x] 3.1 Update test assertion from `Assert.Contains("existing absolute", section.ErrorText)` to `Assert.Equal("Use a full folder path that exists.", section.ErrorText)` (`ProjectRegistryViewModelTests.cs:200`)

**Gate:** `dotnet build CLIHub.sln` — expect 0 warnings, 0 errors; `dotnet test CLIHub.sln` — all tests pass.

## Stage 4: Full gates

- [x] 4.1 Gate: `dotnet build CLIHub.sln`
- [x] 4.2 Gate: `dotnet test CLIHub.sln`
- [x] 4.3 Gate: `openspec validate --all`
- [x] 4.4 Gate: `graphify update .`

## Stage 5: Final verification

- [x] 5.1 Verify both view models contain the identical new strings (all three messages match between `ProjectRegistryViewModel` and `WatchedDirectoryEditor`)
- [x] 5.2 Verify no GUI smoke is required: this change only replaces string literals; the error-line binding and layout are unchanged, and the exact strings are covered by the view-model test (per design.md "Manual smoke rationale")
