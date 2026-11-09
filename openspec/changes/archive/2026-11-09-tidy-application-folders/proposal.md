# Tidy Application Folder Structure (Slice 1)

## Why

The `src/CLIHub` application project accumulated a 2-file `Helpers/` catch-all, a DEBUG-only README image-capture tool misplaced in `Views/`, and a one-file `ViewModels/Menu/` folder. This change dissolves the catch-all, relocates the DEBUG tool, and merges the orphan folder, improving navigability and tightening the boundaries already guarded by `ArchitectureBoundaryTests`.

## What Changes

This is a pure internal refactor with no user-visible behavior change. The set of files, their behavior, tests, and the flat `CLIHub.ViewModels` namespace policy all remain unchanged; only their locations shift.

### Justification for `skip_specs: true`

This is a pure internal refactor: no durable behavior contract changes, no new capabilities, no user-facing effects. The file moves and test relocations are fully covered by the architecture-boundary tests, which verify the WPF-usage guards and project-reference invariants. The change does not introduce, modify, or retire a capability, so a delta spec would document file moves rather than behavior.

### File Moves

| From | To | Reason |
|------|-----|--------|
| `src/CLIHub/Helpers/AgentVersionText.cs` | `src/CLIHub/ViewModels/Agents/AgentVersionText.cs` | Agent display logic colocation; used by `AgentCardItem` |
| `src/CLIHub/Helpers/PreferenceSyncHelper.cs` | `src/CLIHub/ViewModels/Shared/PreferenceSyncHelper.cs` | ViewModel preference utility; used by `LaunchWindowViewModel` |
| `src/CLIHub/Views/ReadmeImageCapture.cs` | `src/CLIHub/Diagnostics/ReadmeImageCapture.cs` | DEBUG-only tooling, not a user-facing View |
| `src/CLIHub/ViewModels/Menu/MenuAction.cs` | `src/CLIHub/ViewModels/Shared/MenuAction.cs` | Merges one-file folder; shared ViewModel contract |
| `tests/CLIHub.Tests/Helpers/AgentVersionTextTests.cs` | `tests/CLIHub.Tests/ViewModels/Agents/AgentVersionTextTests.cs` | Mirrors production move |
| `tests/CLIHub.Tests/Helpers/PreferenceSyncHelperTests.cs` | `tests/CLIHub.Tests/ViewModels/Shared/PreferenceSyncHelperTests.cs` | Mirrors production move |

### Namespace Changes

- `CLIHub.Helpers` → `CLIHub.ViewModels` for `AgentVersionText` and `PreferenceSyncHelper`
- `CLIHub.Views` → `CLIHub.Diagnostics` for `ReadmeImageCapture`
- `CLIHub.ViewModels` unchanged for `MenuAction` (remains flat per repository convention)
- Test namespaces mirror production namespaces

### Using Directive Updates

- `src/CLIHub/ViewModels/Agents/AgentCardItem.cs:6`: Remove `using CLIHub.Helpers;` (source now in same namespace)
- `src/CLIHub/ViewModels/Launch/LaunchWindowViewModel.cs:9`: Remove `using CLIHub.Helpers;` (source now in same namespace)
- `src/CLIHub/App.xaml.cs:37`: Change call site from `CLIHub.Views.ReadmeImageCapture.TryStart` to `CLIHub.Diagnostics.ReadmeImageCapture.TryStart`
- Test files: Replace `using CLIHub.Helpers;` with `using CLIHub.ViewModels;` where needed

### Architecture Guard Update

`tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs` allows `System.Windows` usage in files under `Views/` (line 157). The moved `ReadmeImageCapture.cs` uses `System.Windows`, so the guard must allow `Diagnostics/` files. The guard will add an explicit allowlist entry for `Diagnostics\ReadmeImageCapture.cs` to the `WpfAllowedApplicationFiles` array, matching the pattern used for other WPF adapters outside `Views/`.

### Documentation Updates

- `docs/repo-structure.md:53`: Remove `Helpers/` from `CLIHub` subsystems list; add `Diagnostics/` to the list; note `ViewModels/Menu/` removal
- `docs/images/README.md:79`: Update `ReadmeImageCapture.cs` path reference
- `docs/backlog.md:450`: Update `MenuAction` path reference

## Impact

- **Compilation:** All using directives and namespaces are updated; the change compiles cleanly.
- **Tests:** Test files move with their production counterparts and keep test coverage unchanged.
- **Architecture guards:** `ArchitectureBoundaryTests.ApplicationWpfReferences_AreLimitedToExplicitUiAdapters` continues to pass with the added `Diagnostics\ReadmeImageCapture.cs` allowlist entry.
- **XAML:** No XAML references to moved files exist; XAML pack URIs are relative to the assembly root, not folder-aware.
- **Runtime behavior:** Zero runtime impact; file paths do not affect compiled IL or WPF resource URIs.
- **Git history:** `git log --follow <file>` tracks through renames.

## Non-Goals

Explicitly out of scope for this slice:

- Moving theming runtime files from `Settings/` to `Theming/` (Slice 2)
- Splitting `Launching/` dialogs into `Interaction/` (Slice 2)
- Grouping `Views/` into subfolders (Slice 3)
- Adding a `FolderLayoutTests` guard (Slice 4)
- Changing the flat `CLIHub.ViewModels` namespace convention or any per-folder namespace policy

## Dependencies

None. This change is self-contained and does not depend on or block other changes.

## Verification

1. `dotnet build CLIHub.sln` — confirms all using directives and namespaces resolve
2. `dotnet test CLIHub.sln` — confirms test coverage remains intact
3. `openspec validate --all` — confirms no OpenSpec regressions (0 errors, pre-existing warnings only)
4. Architecture boundary test pass — confirms WPF usage guard accepts the `Diagnostics/` file
