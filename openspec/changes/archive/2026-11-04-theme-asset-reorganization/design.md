# Design: Theme Asset Reorganization

## Current State (Verified)

### Directory layout

`src/CLIHub/Themes/` currently contains 16 files flat (verified via directory read):

**Palette files (3):**
- `DarkTheme.xaml`
- `LightTheme.xaml`
- `BlueTheme.xaml`

**Style dictionaries (8):**
- `Controls.xaml`
- `LaunchWindowStyles.xaml`
- `SettingsControls.xaml`
- `SettingsStyles.xaml`
- `Sizing.xaml`
- `SlimScrollBar.xaml`
- `ThemePickerData.xaml`
- `WhatsNewStyles.xaml`

**C# files (5):**
- `IconGlyphs.cs`
- `PaletteSwitcher.cs`
- `SystemThemeFollower.cs`
- `ThemeCatalog.cs`
- `ThemeDescriptors.cs`

All C# files declare `namespace CLIHub.Themes;` (verified at `src/CLIHub/Themes/ThemeDescriptors.cs:1`, `ThemeCatalog.cs:1`, `PaletteSwitcher.cs:1`, `SystemThemeFollower.cs:1`, `IconGlyphs.cs:1`).

### Palette path references

**App.xaml** (line 11):
```xml
<ResourceDictionary Source="/Themes/DarkTheme.xaml"/>
```

**ThemeDescriptors.cs** (lines 57, 64):
```csharp
"/Themes/LightTheme.xaml",  // AppTheme.Light
"/Themes/BlueTheme.xaml",   // AppTheme.Blue
```

Dark and System themes resolve to `null` (keep the base palette merged by App.xaml).

**PaletteSwitcher.cs** (line 121):
```csharp
Source = new Uri($"pack://application:,,,/CLIHub;component{path}", UriKind.Absolute)
```

The pack URI construction prepends the component prefix to the descriptor path.

### ResourceDictionary Source paths

**App.xaml merged dictionaries** (lines 11, 17, 30):
- `/Themes/DarkTheme.xaml`
- `/Themes/SlimScrollBar.xaml`
- `/Themes/SettingsStyles.xaml`

**LaunchWindow.xaml** (line 31):
- `/Themes/LaunchWindowStyles.xaml`

**WhatsNewWindow.xaml** (line 28, verified by grep):
- `/Themes/WhatsNewStyles.xaml`

**SettingsWindow.xaml** (no direct Theme merge; relies on application-level SettingsStyles).

**Style dictionary merges** (from `docs/architecture/ui.md:14-31`, verified by reading those files):

`SettingsStyles.xaml` merges:
- `Controls.xaml`
- `ThemePickerData.xaml`
- `SettingsControls.xaml`

`Controls.xaml`, `ThemePickerData.xaml`, and `SettingsControls.xaml` each merge:
- `Sizing.xaml`

`LaunchWindowStyles.xaml` and `WhatsNewStyles.xaml` merge:
- `Controls.xaml`

### XAML namespace declarations

14 files declare `xmlns:themes="clr-namespace:CLIHub.Themes"` (verified via grep):

**Views (13):**
- `LaunchWindow.xaml:6`
- `WhatsNewWindow.xaml:6`
- `SettingsWindow.xaml:7`
- `SettingsAgentDetectionPage.xaml:7`
- `SettingsAgentsPage.xaml:8`
- `SettingsAppearancePage.xaml:7`
- `SettingsGeneralPage.xaml:7`
- `SettingsLaunchWindowPage.xaml:8`
- `SettingsLogsPage.xaml:8`
- `SettingsProjectRegistryPage.xaml:7`
- `SettingsProjectsPage.xaml:8`
- `SettingsTerminalPage.xaml:8`
- `SettingsUpdatesPage.xaml:7`

**Themes (1):**
- `Themes/SettingsControls.xaml:4`

All use the prefix `themes` (e.g., `{x:Static themes:IconGlyphs.Add}`).

### C# using directives

**Source files** (10 files import `using CLIHub.Themes;`, verified by grep):
- `src/CLIHub/App.xaml.cs:11`
- `src/CLIHub/Composition/ServiceRegistration.cs:23`
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:9`
- `src/CLIHub/ViewModels/Settings/SettingsUpdateCard.cs:4`
- `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:3`
- `src/CLIHub/ViewModels/Settings/AccentSelection.cs:4`
- `src/CLIHub/Lifetime/ApplicationSession.cs:6`
- `src/CLIHub/Settings/WpfThemePreferenceTarget.cs:7`
- `src/CLIHub/Startup/ShellCoordinator.cs:6`
- `src/CLIHub/ViewModels/Launch/LaunchWindowActionBuilder.cs:5`

**XML doc reference** (1 file):
- `src/CLIHub/Settings/AccentApplier.cs:24` — `CLIHub.Themes.PaletteSwitcher` → `CLIHub.Theming.PaletteSwitcher`

**Test files** (6 files import `using CLIHub.Themes;`, verified by grep):
- `tests/CLIHub.Tests/Themes/PaletteSwitcherTests.cs:6`
- `tests/CLIHub.Tests/Themes/SystemThemeFollowerTests.cs:8`
- `tests/CLIHub.Tests/Themes/ThemePreviewResourceKeyTests.cs:5`
- `tests/CLIHub.Tests/Services/WpfThemePreferenceTargetTests.cs:7`
- `tests/CLIHub.Tests/Services/ShellCoordinatorTests.cs:14`
- `tests/CLIHub.Tests/Services/ApplicationSessionTests.cs:13`

### Test guards

**ThemePaletteParityTests.cs** (lines 12-19):
```csharp
var paletteFiles = Directory.GetFiles(themesDirectory, "*Theme.xaml")
    .OrderBy(Path.GetFileName, StringComparer.Ordinal)
    .ToArray();

Assert.Contains("DarkTheme.xaml", paletteFiles.Select(Path.GetFileName));
Assert.Contains("LightTheme.xaml", paletteFiles.Select(Path.GetFileName));
```

The glob `*Theme.xaml` selects all three palettes; the assertions verify their presence.

**ApplicationResourceKeyTests.cs** (lines 53, 116):
```csharp
var paletteFiles = Directory.GetFiles(themesDirectory, "*Theme.xaml")
```

Two identical globs for palette discovery. Both enumerate at the root only (`Directory.GetFiles` is non-recursive). After the move, the root contains only the three palettes, so changing to `*.xaml` keeps the same result set. The test asserts `paletteFiles.Length >= 3` (loud failure).

**ThemeColorLiteralTests.cs** (lines 14-15, 49, 100-108):
```csharp
/// <para>
///     Two kinds of file are allowlisted by design: the palettes (<c>Themes/*Theme.xaml</c>, colours only)
///     and <c>Themes/ThemePickerData.xaml</c> (theme-identity preview swatches that must read the same
///     under every theme).
/// </para>
...
Assert.Contains(xamlFiles, path => IsFile(path, appRoot, "Themes", "ThemePickerData.xaml"));
...
private static bool IsAllowlisted(string relativePath)
{
    var normalized = relativePath.Replace('\\', '/');
    if (string.Equals(normalized, "Themes/ThemePickerData.xaml", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    return Regex.IsMatch(normalized, "^Themes/[^/]+Theme\\.xaml$", RegexOptions.IgnoreCase);
}
```

The allowlist covers `Themes/ThemePickerData.xaml` and the three palette files via the pattern `^Themes/[^/]+Theme\.xaml$`. Line 49 explicitly asserts `ThemePickerData.xaml` was seen. Lines 14-15 document the allowlist rationale.

**PaletteSwitcherTests.cs** (lines 6, 40, 61):
```csharp
using CLIHub.Themes;
...
Assert.Contains("LightTheme.xaml", path, StringComparison.Ordinal);
...
Assert.Contains("BlueTheme.xaml", requested[1], StringComparison.Ordinal);
```

Namespace import and two assertions that check the resolved palette paths contain the old file names.

**XamlResourceReferenceTests.cs** (line 92):
```csharp
Assert.Contains(xamlFiles, path => path.EndsWith(Path.Combine("Themes", "SettingsStyles.xaml"), StringComparison.Ordinal));
```

The assertion checks for `Themes\SettingsStyles.xaml` (path separator normalized by `Path.Combine`).

### Documentation references

**docs/architecture/ui.md** (lines 121, 127, 129, 138-141):
- Table "Theme Data Locations" lists `src/CLIHub/Themes/ThemeDescriptors.cs`, `DarkTheme.xaml`, `LightTheme.xaml`, `BlueTheme.xaml`, `ThemePickerData.xaml`, `ThemeCatalog.cs`, `PaletteSwitcher.cs`, `SystemThemeFollower.cs`.
- Line 121: "The registered palettes (`DarkTheme.xaml`, `LightTheme.xaml`, `BlueTheme.xaml`) define the same brush keys..."
- Line 127: Recipe says "Create `Themes/<Name>Theme.xaml`".
- Line 129: Palette parity test "enumerates every `Themes/*Theme.xaml`".

**docs/repo-structure.md** (line 55):
- "CLIHub` contains `Views/`, `ViewModels/`, `Themes/`, `Hotkeys/`, `Converters/`, ..."

No specific mention of the flat Themes structure in that file.

**docs/architecture/startup.md** (line 53, verified by grep):
- `src/CLIHub/Themes/PaletteSwitcher.cs` reference.

**docs/backlog.md** (line 367, verified by grep):
- `src/CLIHub/Themes/SettingsStyles.xaml:214-230` reference in decorative glyph accessibility item.

**ui/mockups/settings-page-emblem.html** (lines 9-10, 20, verified by grep):
- `Themes/DarkTheme.xaml`, `Themes/Sizing.xaml`, `Themes/SettingsStyles.xaml` references.

**ui/mockups/README.md** (line 48, verified by grep):
- `Themes/DarkTheme.xaml` and `Themes/Sizing.xaml` references.

## Approved Target Structure

User-approved layout (from brief):

```
src/CLIHub/Themes/
├─ Dark.xaml                       ← was Themes/DarkTheme.xaml
├─ Light.xaml                      ← was Themes/LightTheme.xaml
├─ Blue.xaml                       ← was Themes/BlueTheme.xaml
└─ Common/
   ├─ Controls.xaml
   ├─ LaunchWindowStyles.xaml
   ├─ PickerData.xaml              ← was Themes/ThemePickerData.xaml
   ├─ SettingsControls.xaml
   ├─ SettingsStyles.xaml
   ├─ SlimScrollBar.xaml
   ├─ Sizing.xaml
   └─ WhatsNewStyles.xaml

src/CLIHub/Theming/
├─ IconGlyphs.cs                   ← was Themes/IconGlyphs.cs
├─ PaletteSwitcher.cs              ← was Themes/PaletteSwitcher.cs
├─ SystemThemeFollower.cs          ← was Themes/SystemThemeFollower.cs
├─ ThemeCatalog.cs                 ← was Themes/ThemeCatalog.cs
└─ ThemeDescriptors.cs             ← was Themes/ThemeDescriptors.cs
```

## Decisions

### 1. Palette files at Themes root, no "Theme" suffix

The three palette files (`Dark.xaml`, `Light.xaml`, `Blue.xaml`) live directly under `Themes/`, not in per-theme subfolders. This keeps them immediately visible and avoids an extra nesting level. The "Theme" suffix is dropped: `DarkTheme.xaml` → `Dark.xaml`. This aligns with their role as palette identities rather than generic theme containers.

**Rationale:** The primary capability is the palette; the root of `Themes/` should show what palettes exist. One level of nesting (for `Common/`) is the maximum before the gain inverts.

### 2. Shared styles in Themes/Common/

All non-palette XAML (8 files) moves to `Themes/Common/`. This groups them by role without creating per-window or per-style-type subdirectories. `ThemePickerData.xaml` is renamed to `PickerData.xaml` ("Theme" dropped for consistency).

**Rationale:** A flat Common subdirectory is simpler than `Themes/Styles/`, `Themes/Controls/`, etc., and preserves the single-level nesting. The "Common" name signals shared resources that apply across windows.

### 3. C# in a new Theming namespace

All five C# files move to `src/CLIHub/Theming/` and their namespace changes from `CLIHub.Themes` to `CLIHub.Theming`. File and type names remain unchanged (`ThemeCatalog`, `ThemeDescriptors`, `PaletteSwitcher`, `SystemThemeFollower`, `IconGlyphs`).

**Rationale:** XAML resources and C# code are different concerns. Separating them makes both directories easier to scan. The namespace change reflects the new location.

### 4. XAML xmlns prefix stays "themes"

The 14 files declaring `xmlns:themes="clr-namespace:CLIHub.Themes"` change to `"clr-namespace:CLIHub.Theming"`, but the prefix `themes` is unchanged. This minimizes churn: only the namespace value updates, not the usages (`{x:Static themes:IconGlyphs.Add}`).

**Rationale:** The prefix is a local alias; changing it would touch every glyph reference, tooltip, and style application. The namespace change is already mechanical; prefix stability reduces the diff.

### 5. System theme has no palette file

System resolves to Dark or Light at runtime (`ThemeCatalog.ResolvePalettePath`, line 23-25). Its descriptor palette path is `null`, and no `System.xaml` file is created. Its preview brushes stay in `Common/PickerData.xaml`.

**Rationale:** System is a resolution policy, not a palette. Creating an empty or duplicate file would obscure the runtime behavior.

### 6. Test globs update to new names, keeping loud failure

`ThemePaletteParityTests` glob changes from `*Theme.xaml` to `*.xaml`, excluding `Common/`. `ThemeColorLiteralTests` allowlist matches `Themes/Dark.xaml`, `Themes/Light.xaml`, `Themes/Blue.xaml`, and `Themes/Common/PickerData.xaml`. Both tests keep their loud-failure assertions (explicit file-name checks, expected-count guards).

**Rationale:** The tests exist to fail loudly on accidental file renames or missing palettes. Weakening them (removing the explicit assertions or the allowlist count check) would defeat that purpose.

## Alternatives Considered and Rejected

### Alternative 1: Per-theme subdirectories (e.g., Themes/Dark/, Themes/Light/)

**Rejected:** This would move each palette and its related styles into a subfolder (`Themes/Dark/Dark.xaml`, `Themes/Dark/Preview.xaml`). The shared styles (`Controls.xaml`, `Sizing.xaml`, `SlimScrollBar.xaml`) would need to sit outside those folders or be duplicated. Two-level nesting for 16 files is overkill, and the "related styles" premise is false: every style dictionary is theme-agnostic and resolves brushes through `DynamicResource`.

### Alternative 2: C# files stay in Themes/

**Rejected:** Mixing XAML resources and C# types in one directory obscures the distinction. The Theming namespace makes it clear that these are code-based services, not declarative resources.

### Alternative 3: Rename palette files to Palette.Dark.xaml, etc.

**Rejected:** This inverts the visual sort order (P before controls/sizing) and introduces a prefix convention where none exists elsewhere. The simple names (`Dark.xaml`, `Light.xaml`, `Blue.xaml`) are shorter, clearer, and sortable.

### Alternative 4: Keep the "Theme" suffix in XAML file names

**Rejected:** `DarkTheme.xaml` → `Themes/DarkTheme.xaml` would preserve the current names, but "Theme" is redundant when the files already live under `Themes/`. Dropping it makes the file purpose clearer: these are palette definitions.

### Alternative 5: Put all XAML under Themes/Xaml/, C# under Themes/Code/

**Rejected:** This groups by file type rather than by role. The split between palettes (identity-defining) and shared styles (window-agnostic) is meaningful; the XAML-vs-C# split is not.

## Migration Order

1. Create `src/CLIHub/Themes/Common/` and `src/CLIHub/Theming/` directories.
2. Move and rename the 11 XAML files (3 palettes + 8 styles) and the 5 C# files to their new locations.
3. Update all compile-affecting changes in one stage:
   - Update namespace declarations in the 5 moved C# files (`CLIHub.Themes` → `CLIHub.Theming`).
   - Update `ThemeDescriptors.cs` palette paths (2 sites).
   - Update `using CLIHub.Themes;` directives in 10 source C# files outside Theming/.
   - Update 1 XML doc comment reference in `AccentApplier.cs`.
   - Update `xmlns:themes` namespace in all 14 XAML files (`clr-namespace:CLIHub.Themes` → `clr-namespace:CLIHub.Theming`).
   - Update `ResourceDictionary Source` paths in App.xaml, window files, and all style dictionaries in `Themes/Common/`.
   - **Build gate:** `dotnet build CLIHub.sln` — must pass with 0 errors, 0 warnings.
4. Update test files (globs, allowlists, assertions, namespace imports in 6 test files).
   - **Test gate:** `dotnet test CLIHub.sln` — must pass with 0 failed tests.
5. Update documentation paths in `docs/architecture/ui.md`, `docs/repo-structure.md`, `docs/architecture/startup.md`, `docs/backlog.md`, and mockup files.
6. Final verification: build, test, OpenSpec validation, graphify update, directory layout check.

## Risks and Mitigations

### Risk 1: WPF pack URI resolution breaks

**Mitigation:** All pack URIs are relative to the component root (`/Themes/Dark.xaml`, `/Themes/Common/Controls.xaml`). MSBuild discovers them as embedded resources automatically. Build after the compile-affecting stage (namespaces plus Source paths) to confirm resolution.

### Risk 2: XAML namespace typo causes silent reference failures

**Mitigation:** The change updates 14 files to `clr-namespace:CLIHub.Theming`. All references use `x:Static`, which is compile-time checked. A typo fails the build with `XamlParseException`.

### Risk 3: Test glob or allowlist mismatch causes false pass

**Mitigation:** Both tests assert the expected file count and specific file names. A missing palette or a broken glob fails loudly with the assertion message.

### Risk 4: Overlooked Source path or using directive causes runtime failure

**Mitigation:** Grep for all occurrences of `Themes/DarkTheme.xaml`, `Themes/LightTheme.xaml`, etc., and `clr-namespace:CLIHub.Themes` before committing. Build and test gate catches any missed reference.

## Open Questions

None. The approved structure, migration order, and test updates are fully specified. Implementation is mechanical.

## Implementation Deviations

The approved structure was implemented as described; the following execution-level deviations were
recorded during implementation and verification:

1. **Stage 2 build gate (D1).** The first `dotnet build CLIHub.sln` after the namespace move failed
   with 7 `CS0234`/`CS0246` errors in `tests/CLIHub.Tests/` because the six test `using
   CLIHub.Themes;` directives are scheduled for Stage 3. The implementer gated Stage 2 on
   `dotnet build src/CLIHub/CLIHub.csproj` (0 warnings, 0 errors) and the full solution build passed
   in Stage 5 (`0 Warning(s)`, `0 Error(s)`).
2. **Two additional test guards (D2).** `ApplicationResourceKeyTests.IsPaletteFile` matched the old
   `*Theme.xaml` shape and `ArchitectureBoundaryTests.WpfAllowedApplicationFiles` pinned the old
   `Themes\PaletteSwitcher.cs` / `Themes\SystemThemeFollower.cs` paths. Both were updated to the new
   names/locations with the same enforcement scope; the first test run exposed them (2 failed).
3. **Stale file names in comments (D3).** Comments and XML docs that named the renamed files were
   updated in files already on the change surface: `ThemeDescriptors.cs`, `App.xaml`,
   `LaunchWindow.xaml`, `Blue.xaml`, `Light.xaml`, `SettingsStyles.xaml`, and `Sizing.xaml`.
4. **Backlog path (D4).** `docs/backlog.md:369` referenced `themes/IconGlyphs.cs`; it now points at
   `Theming/IconGlyphs.cs`.

`CHANGELOG.md:165` still names `ThemePickerData` because it is a historical entry for a past change;
history is deliberately not rewritten.
