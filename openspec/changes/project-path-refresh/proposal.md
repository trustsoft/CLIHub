# Project Path Refresh: immediate DisplayPath update after path change

## Why

After the Settings → Projects "Change project folder" action, the project row immediately shows the new name — but keeps the old path text until the application restarts. Verified at HEAD `be19281` (working tree clean):

- Smoke test: changing a manual project from `...\smoke2-A` to `...\smoke2-B` persisted both name and path in `config.json` and the service log (`Changed project smoke2-B path to ...\smoke2-B`), and the row immediately showed the new name `smoke2-B` — but its path text remained `...\smoke2-A` until restart.
- `src/CLIHub/ViewModels/Projects/ProjectModel.cs`: `DisplayPath` is a computed `public string DisplayPath => this.Path;` (`:134`) with no property-change notification. The `Path` property setter (`:63-67`) calls `SetValue`, which raises `PropertyChanged` for `Path` only.
- `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:166,168` binds the row path TextBlock's `Text` and `ToolTip` to `DisplayPath`, so the binding never updates when `Path` changes in place.
- The launch window binds `Path` directly (not `DisplayPath`) and is unaffected.
- The project-management spec already requires project changes to apply immediately and every surface to reflect them without a restart (`openspec/specs/project-management/spec.md:198`, `:264-267`: "Other surfaces follow the registry"). This is a defect fix, not a new capability.

This change restores the specified immediate-update contract by ensuring `DisplayPath` notifications reach the Settings XAML when the path changes.

## What Changes

- **Model:** `ProjectModel.Path` setter raises `PropertyChanged(nameof(DisplayPath))` when the path changes, so `DisplayPath` bindings update in place.
- **Test coverage:** add regression tests verifying `DisplayPath` raises `PropertyChanged` and returns the new path after a path update, and that `ProjectRegistryViewModel`'s post-`ChangePathCommand` load reflects the new path in the row's `DisplayPath`.

## Impact

- Affected code:
  - `src/CLIHub/ViewModels/Projects/ProjectModel.cs` — add `this.OnPropertyChanged(nameof(DisplayPath))` in the `Path` setter (`:66`) when `SetValue` returns `true`
  - `tests/CLIHub.Core.Tests/Projects/ProjectModelCatalogTests.cs` or `tests/CLIHub.Tests/ViewModels/Projects/ProjectModelTests.cs` (if it exists) — add `ProjectModel_PathChanged_RaisesDisplayPathChanged` and `ProjectModel_PathChanged_DisplayPathReturnsNewPath`
  - `tests/CLIHub.Tests/ViewModels/Projects/ProjectRegistryViewModelTests.cs` — add or extend a test confirming `DisplayPath` equals the new folder after `ChangePathCommand`/`Load`
- No XAML change: the existing `DisplayPath` bindings remain correct and now receive the update event.
- No config schema change, no service API change.

## Non-goals

- No launch window change (it binds `Path` directly and already works correctly).
- No change to the passthrough shape of `DisplayPath` (it remains a computed property returning `Path`).
- No new spec scenarios; this restores the behavior the spec already requires.

## Dependencies

None. The change is a one-line notification fix in the existing model.
