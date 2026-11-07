# Tasks: unify the missing-folder labels on the Projects page

## Stage 1: XAML chip text and comment

- [x] 1.1 Change the row chip `TextBlock` text from `Folder not found` to `Folder missing` (`SettingsProjectRegistryPage.xaml:144`)
- [x] 1.2 Update the explanatory comment above the `ProjectMissingChip` style to quote the new chip text `"Folder missing"` (`SettingsProjectRegistryPage.xaml:10-11`)

**Gate:** `dotnet build CLIHub.sln` — expect 0 warnings, 0 errors.

## Stage 2: Full gates

- [x] 2.1 Gate: `dotnet build CLIHub.sln` — expect 0 warnings, 0 errors
- [x] 2.2 Gate: `dotnet test CLIHub.sln` — all tests pass
- [x] 2.3 Gate: `openspec validate --all` — 0 errors
- [x] 2.4 Gate: `graphify update .`
