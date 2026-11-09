# Design: Tidy Application Folder Structure

## Current State (Verified)

### Files to Move

#### `src/CLIHub/Helpers/AgentVersionText.cs`
- **Location:** `src/CLIHub/Helpers/AgentVersionText.cs:1`
- **Namespace:** `namespace CLIHub.Helpers;` (line 1)
- **Type:** Static class with 3 public const strings (lines 8-23)
- **Dependencies:** None (standalone constants)
- **Usage:** `src/CLIHub/ViewModels/Agents/AgentCardItem.cs:6` imports `using CLIHub.Helpers;`
- **Test:** `tests/CLIHub.Tests/Helpers/AgentVersionTextTests.cs:1-26`
  - Namespace: `namespace CLIHub.Tests.Helpers;` (line 1)
  - Imports: `using CLIHub.Helpers;` (line 3)

#### `src/CLIHub/Helpers/PreferenceSyncHelper.cs`
- **Location:** `src/CLIHub/Helpers/PreferenceSyncHelper.cs:1`
- **Namespace:** `namespace CLIHub.Helpers;` (line 1)
- **Type:** Static class with one public method `SyncPreference` (lines 9-28)
- **Dependencies:** `using CLIHub.Core.Configuration;` and `using CLIHub.Core.Models;` (lines 3-4)
- **Usage:** `src/CLIHub/ViewModels/Launch/LaunchWindowViewModel.cs:9` imports `using CLIHub.Helpers;`
- **Test:** `tests/CLIHub.Tests/Helpers/PreferenceSyncHelperTests.cs:1-101`
  - Namespace: `namespace CLIHub.Tests.Helpers;` (line 1)
  - Imports: `using CLIHub.Core.Configuration;`, `using CLIHub.Core.Models;`, `using CLIHub.Helpers;` (lines 3-5)

#### `src/CLIHub/Views/ReadmeImageCapture.cs`
- **Location:** `src/CLIHub/Views/ReadmeImageCapture.cs:1-209`
- **Conditional compilation:** Entire file is `#if DEBUG` (line 1) through `#endif` (line 209)
- **Namespace:** `namespace CLIHub.Views;` (line 2)
- **Type:** Internal static class (line 18)
- **WPF dependencies:** Uses `System.Windows` (lines 5-8) for `Application`, `Window`, `RenderTargetBitmap`, etc.
- **Call site:** `src/CLIHub/App.xaml.cs:37` — `CLIHub.Views.ReadmeImageCapture.TryStart(this)`
- **No test:** DEBUG-only utility, no test file exists

#### `src/CLIHub/ViewModels/Menu/MenuAction.cs`
- **Location:** `src/CLIHub/ViewModels/Menu/MenuAction.cs:1-50`
- **Namespace:** `namespace CLIHub.ViewModels;` (line 1)
- **Type:** Public sealed class inheriting from `ObservableObject` (line 9)
- **Dependencies:** `using System.Windows.Input;` (line 3)
- **Usage:** Multiple ViewModels and Views across the project (ViewModel-level contract)
- **Documentation reference:** `docs/backlog.md:450` mentions `src/CLIHub/ViewModels/Menu/MenuAction.cs`
- **No test:** No dedicated test file; tested indirectly through ViewModel tests

### Architecture Guard (Verified)

`tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs`:

```csharp
// Lines 7-24: WpfAllowedApplicationFiles array
private static readonly string[] WpfAllowedApplicationFiles =
[
    "App.xaml.cs",
    "AssemblyInfo.cs",
    "Imaging" + Path.DirectorySeparatorChar + "LogoImageSourceCache.cs",
    "Launching" + Path.DirectorySeparatorChar + "PluginDialogService.cs",
    "Launching" + Path.DirectorySeparatorChar + "ProjectDialogService.cs",
    "Startup" + Path.DirectorySeparatorChar + "ApplicationHost.cs",
    "Tray" + Path.DirectorySeparatorChar + "TrayIconController.cs",
    "Launching" + Path.DirectorySeparatorChar + "UserNotificationService.cs",
    "Lifetime" + Path.DirectorySeparatorChar + "WpfApplicationLifetime.cs",
    "Startup" + Path.DirectorySeparatorChar + "WpfApplicationStartupUi.cs",
    "Settings" + Path.DirectorySeparatorChar + "AccentApplier.cs",
    "Settings" + Path.DirectorySeparatorChar + "WpfAccentPreferenceTarget.cs",
    "Settings" + Path.DirectorySeparatorChar + "WpfThemePreferenceTarget.cs",
    "Theming" + Path.DirectorySeparatorChar + "PaletteSwitcher.cs",
    "Theming" + Path.DirectorySeparatorChar + "SystemThemeFollower.cs"
];

// Lines 154-166: IsWpfAllowed helper
private static bool IsWpfAllowed(string path, string sourceRoot)
{
    var relative = Path.GetRelativePath(sourceRoot, path);
    return relative.StartsWith("Views" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
        || relative.StartsWith("Behaviors" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
        || relative.StartsWith("Controls" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
        || relative.StartsWith("Converters" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
        || relative.StartsWith("Interop" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
        || relative.StartsWith("Hotkeys" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
        || WpfAllowedApplicationFiles.Contains(relative, StringComparer.OrdinalIgnoreCase)
        || string.Equals(relative, "Tray" + Path.DirectorySeparatorChar + "TrayMenuBuilder.cs", StringComparison.OrdinalIgnoreCase)
        || relative.StartsWith("ViewModels" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
```

**Current rule:** `System.Windows` is allowed in `Views/`, `Behaviors/`, `Controls/`, `Converters/`, `Interop/`, `Hotkeys/`, `ViewModels/`, `Tray/TrayMenuBuilder.cs`, and the 14 files in `WpfAllowedApplicationFiles`.

**After move:** `ReadmeImageCapture.cs` moves from `Views/` (where it's implicitly allowed by line 157) to `Diagnostics/` (not covered by any rule). The guard must be updated.

### Documentation References (Verified)

#### `docs/repo-structure.md:53`
```markdown
- `CLIHub` contains `Views/`, `ViewModels/`, `Themes/`, `Theming/`, `Hotkeys/`, `Converters/`, `Interop/`, `Controls/`,
  `Behaviors/`, `Helpers/`, `Imaging/`, and `Assets/`, plus subsystem folders that mirror ownership: `Tray/`,
  `Startup/`, `Lifetime/`, `Settings/`, `Updates/`, `Launching/`, and `Composition/` (DI
  registration). Only the entry point (`Program.cs`, `App.xaml.cs`, `AssemblyInfo.cs`) stays at the project root.
```

- Mentions `Helpers/` — needs removal
- Does not mention `Diagnostics/` — needs addition
- Mentions `ViewModels/` but does not list subfolders — the `Menu/` removal is implicit

#### `docs/images/README.md:79`
```markdown
The app captures are produced by `tools\Capture-ReadmeImages.ps1` through the debug-only
`CLIHUB_CAPTURE_DIR` mode (`src/CLIHub/Views/ReadmeImageCapture.cs`). The debug build renders
```

- References `src/CLIHub/Views/ReadmeImageCapture.cs` — needs path update to `src/CLIHub/Diagnostics/ReadmeImageCapture.cs`

#### `docs/backlog.md:450`
```markdown
  replacements reuse `MenuAction` (`src/CLIHub/ViewModels/Menu/MenuAction.cs`), which has no tooltip
```

- References `src/CLIHub/ViewModels/Menu/MenuAction.cs` — needs path update to `src/CLIHub/ViewModels/Shared/MenuAction.cs`

## Decisions

### Architecture Guard: Explicit File Allowlist vs. Prefix Rule

**Decision:** Add `Diagnostics\ReadmeImageCapture.cs` to the `WpfAllowedApplicationFiles` array rather than allowing all files under `Diagnostics/`.

**Rationale:**
- The allowlist already uses the explicit-file form for 14 WPF adapters outside `Views/` (lines 7-23).
- A `Diagnostics/` prefix rule would allow any future file under `Diagnostics/` to use WPF without review, weakening the guard.
- `ReadmeImageCapture.cs` is the only file in `Diagnostics/` and is DEBUG-only; the folder is not expected to grow.
- The explicit form matches the existing guard pattern and requires intentional changes to the allowlist.

**Alternative rejected:** Adding `relative.StartsWith("Diagnostics" + Path.DirectorySeparatorChar, ...)` to `IsWpfAllowed` (line 166) would allow the entire folder, which contradicts the guard's goal of limiting WPF usage to declared UI surfaces and named adapters.

### Namespace for Moved ViewModels Helpers

**Decision:** Change namespace from `CLIHub.Helpers` to `CLIHub.ViewModels` for both `AgentVersionText` and `PreferenceSyncHelper`.

**Rationale:**
- The repository uses a flat `CLIHub.ViewModels` namespace regardless of subfolders (`ViewModels/Agents/`, `ViewModels/Shared/`, etc.) — documented in `docs/repo-structure.md:66-68` and `docs/architecture.md:66-68`.
- `MenuAction` already lives in `ViewModels/Menu/` with the flat `namespace CLIHub.ViewModels;` (line 1 of `MenuAction.cs`).
- Both helpers are ViewModel utilities: `AgentVersionText` is used by `AgentCardItem`, and `PreferenceSyncHelper` is used by `LaunchWindowViewModel`.
- Moving them into `CLIHub.ViewModels` removes the `using CLIHub.Helpers;` directive from their call sites (they become same-namespace references).

**Alternative rejected:** Keeping `CLIHub.Helpers` after the move. This would preserve the namespace but leave the helpers in the wrong conceptual ownership and require retaining the `using CLIHub.Helpers;` directives.

### Test Namespace Changes

**Decision:** Test namespaces mirror production namespaces:
- `CLIHub.Tests.Helpers` → `CLIHub.Tests.ViewModels.Agents` for `AgentVersionTextTests`
- `CLIHub.Tests.Helpers` → `CLIHub.Tests.ViewModels.Shared` for `PreferenceSyncHelperTests`

**Rationale:**
- `docs/testing.md:50-53` states test classes are named `<TypeUnderTest>Tests` and folders mirror source subsystems.
- The test project already uses namespace-per-subsystem (`CLIHub.Core.Tests.<Subsystem>` and `CLIHub.Tests.<Area>`).
- Mirroring the production structure keeps the test layout predictable.

## Compatibility Notes

### XAML Pack URIs

No XAML files reference the moved types by pack URI. WPF pack URIs use the assembly root, not folder paths (`pack://application:,,,/<assembly>;<component>/<path>`), and none of the moved files are XAML resources or referenced by `StaticResource` or `DynamicResource` bindings.

### Type Identity and Reflection

The moved types' fully-qualified names change:
- `CLIHub.Helpers.AgentVersionText` → `CLIHub.ViewModels.AgentVersionText`
- `CLIHub.Helpers.PreferenceSyncHelper` → `CLIHub.ViewModels.PreferenceSyncHelper`
- `CLIHub.Views.ReadmeImageCapture` → `CLIHub.Diagnostics.ReadmeImageCapture`

**Impact:** None. The codebase does not use reflection or `Type.GetType()` with string-based type names for these types, and no serialization or plugin contracts reference them.

### Git History

`git log --follow <new-path>` tracks history through renames when files move with ≥50% similarity (Git's default rename-detection threshold). The moves preserve file content except for namespace lines, so history tracking works.

## Alternatives Considered

### Alternative: Delete `PreferenceSyncHelper` Instead of Moving It

`PreferenceSyncHelper` is a single-method wrapper around `IPreferencesStore.Update`. Its only caller is `LaunchWindowViewModel`, and it adds minimal abstraction (one optional callback parameter).

**Rejected reason:** The helper has 6 tests in `PreferenceSyncHelperTests.cs` (lines 13-100), demonstrating intentional coverage of edge cases (null arguments, callback invocation, exception propagation). Deleting it would require either moving those tests into `LaunchWindowViewModel` tests or accepting coverage loss. The move preserves the abstraction and its tests without behavioral change, which aligns with the non-goal of avoiding behavior changes in Slice 1.

### Alternative: Merge All of `ViewModels/Shared/` into `ViewModels/` Root

`ViewModels/Shared/` will hold `PreferenceSyncHelper` and `MenuAction` after this change. An alternative is to merge the entire `Shared/` subfolder into the flat `ViewModels/` root, matching the flat namespace.

**Rejected reason:** The repository uses subfolders under `ViewModels/` for navigation (`ViewModels/Agents/`, `ViewModels/Launch/`, `ViewModels/Menu/`, `ViewModels/Shared/`), but keeps the namespace flat (`namespace CLIHub.ViewModels;` across all of them) — documented in `docs/architecture.md:66-68`. Flattening the folder structure to match the namespace is a larger refactor and contradicts the explicit non-goal of limiting this change to the decided moves.

## Risks

### Risk: Empty Folders After Move

**Impact:** Git does not track empty directories. After moving the files, `src/CLIHub/Helpers/`, `tests/CLIHub.Tests/Helpers/`, and `src/CLIHub/ViewModels/Menu/` will be empty.

**Mitigation:** The tasks include explicit folder deletion steps. Git will stage the deletions implicitly when the move commits are created.

### Risk: Architecture Guard Test Fails During Implementation

**Impact:** If the `Diagnostics\ReadmeImageCapture.cs` allowlist entry is added after the file moves, the `ApplicationWpfReferences_AreLimitedToExplicitUiAdapters` test will fail between the move and the guard update.

**Mitigation:** Stage 2 of the tasks moves `ReadmeImageCapture.cs` and updates the architecture guard in the same step, so the guard update is atomic with the move. The build and test gates run after Stage 2 completes.

### Risk: Stale `using` Directives in Unmapped Files

**Impact:** If a file other than `AgentCardItem.cs` or `LaunchWindowViewModel.cs` imports `using CLIHub.Helpers;`, the build will fail after the namespace change.

**Mitigation:** Verified by grep that only those two files import `CLIHub.Helpers;`. The search was:

```powershell
Get-ChildItem -Path src/CLIHub -Filter *.cs -Recurse | Select-String "using CLIHub.Helpers"
```

**Result:** Two matches only:
- `src/CLIHub/ViewModels/Agents/AgentCardItem.cs:6`
- `src/CLIHub/ViewModels/Launch/LaunchWindowViewModel.cs:9`

Both are addressed in the tasks.

## Open Questions

None. All decisions are resolved:
- Architecture guard update form: explicit file allowlist (decided)
- Namespace for moved helpers: `CLIHub.ViewModels` (decided)
- Test namespace mapping: mirrors production (decided)
- Whether to delete `PreferenceSyncHelper`: no, preserve with its tests (decided)
