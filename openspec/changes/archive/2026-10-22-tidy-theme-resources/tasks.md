# Tasks: Tidy Theme Resources

## Stage 1: Fix Comments and Remove Dead Code

**Goal**: Correct misleading comments, rename DarkScrollBar.xaml → SlimScrollBar.xaml, and
remove unused ThemeCatalog.DarkPalettePath.

### 1.1 Fix App.xaml comment about SettingsStyles

**File**: `src/CLIHub/App.xaml`

Replace lines 20-25 comment with:

```xml
<!--
    Settings window styles and page chrome: shared with the extracted Settings pages (UserControls).
    A UserControl is built before it attaches to the window tree, so window-scoped resources are
    not reachable during its load — application-level placement is required, like the scrollbar
    and tooltip styles above. Note: this dictionary contains 4 implicit (keyless) styles for
    CLIHub.Controls custom types (NumericField, SectionHeader, SettingCard, SettingsGroup);
    they are intentional and currently used only by Settings pages.
-->
```

### 1.2 Fix SettingsStyles.xaml header comment

**File**: `src/CLIHub/Themes/SettingsStyles.xaml`

Replace lines 6-12 header comment with:

```xml
<!--
    Settings window styles and page chrome. Merged at application level (from App.xaml, not from
    SettingsWindow.xaml) because Settings pages are UserControls built before they attach to the
    window tree — window-scoped resources are not reachable during their XAML load. Contains 4
    implicit (keyless) styles for CLIHub.Controls custom types (NumericField, SectionHeader,
    SettingCard, SettingsGroup); they apply application-wide but are currently used only by
    Settings pages.
-->
```

### 1.3 Delete unused ThemeCatalog.DarkPalettePath

**File**: `src/CLIHub/Themes/ThemeCatalog.cs`

Delete line 15:

```csharp
private const string DarkPalettePath = "/Themes/DarkTheme.xaml";
```

No other changes needed (grep-verified: no references to this constant).

### 1.4 Rename DarkScrollBar.xaml → SlimScrollBar.xaml

**Actions**:
1. Rename file `src/CLIHub/Themes/DarkScrollBar.xaml` → `SlimScrollBar.xaml`
2. Update App.xaml:19 Source attribute:
   ```xml
   <ResourceDictionary Source="/Themes/SlimScrollBar.xaml"/>
   ```
3. Update SlimScrollBar.xaml header comment (lines 6-9):
   OLD:
   ```xml
   <!--
       Dark scrollbar: slim thumb, no arrows, transparent track. The thumb is themed via
       DynamicResource so it adapts to palette switches at startup.
   -->
   ```
   NEW:
   ```xml
   <!--
       Slim scrollbar: minimal thumb, no arrows, transparent track. Palette-neutral: the thumb
       uses DynamicResource ScrollThumbBrush and adapts to any theme selected at startup.
   -->
   ```
4. Update `docs/architecture/ui.md`:
   - Line 13 (inside layering paragraph): change "the implicit `ScrollBar` and `ToolTip` styles"
     to "the implicit `ScrollBar` (SlimScrollBar.xaml) and `ToolTip` styles"
   - Line 17 (implicit styles explanation): change "`DarkScrollBar.xaml`" to "`SlimScrollBar.xaml`"

**Verification**: grep for `DarkScrollBar` in the repository — should find zero matches after
this step (except in git history and this change's docs).

### Gate: Build and Test

```powershell
dotnet build CLIHub.sln
# Expect: 0 warnings, 0 errors

dotnet test CLIHub.sln
# Expect: all tests pass (ThemePaletteParityTests, plus any others)

openspec validate --all
# Expect: validation passes (change has skip_specs: true, so no specs/)
```

---

## Stage 2: Add XAML Resource Reference Integrity Test

**Goal**: Create a new xUnit test that statically verifies every StaticResource/DynamicResource
reference in `src/CLIHub/**/*.xaml` resolves against its reachable key set. The test MUST be
green on the current tree (baseline) before any refactor.

### 2.1 Create test file

**File**: `tests/CLIHub.Tests/Themes/XamlResourceReferenceTests.cs`

**Implementation notes**:
- Single test method: `XamlFiles_AllResourceReferencesResolve()`
- Find all `.xaml` files under `src/CLIHub` recursively
- Extract x:Key definitions: regex `x:Key="([^"]+)"`
- Extract resource references: regex `{(?:Static|Dynamic)Resource\s+([^}]+)}`
- Extract merged dictionary Source paths: regex `Source="([^"]+)"` inside `<ResourceDictionary.MergedDictionaries>`
- Model reachability:
  - **Local keys**: x:Key definitions in the file itself
  - **Merged keys**: recursively walk Source paths (resolve relative to `src/CLIHub/`), collect
    keys from those files (and their transitive merges)
  - **Application keys**: if file is App.xaml OR is referenced by App.xaml's MergedDictionaries,
    include App.xaml's local + merged keys
  - **Window keys**: if file ends with `Window.xaml`, treat it as both a dictionary (defines keys)
    and a consumer (can reference its own keys + merges + app-level)
- Assert: every referenced key exists in the file's reachable set
- On failure: report file path, referenced key, and available keys (for diagnosis)

**Out of scope** (document in test comments):
- `x:Static` references (compile-time checked by XAML compiler)
- `TemplateBinding` (resolved by WPF template infrastructure)
- `Binding` property paths (runtime-evaluated, not static resource references)

**Baseline requirement**: test MUST pass on current tree (commit 0f8bf93) before proceeding to
Stage 3. If it fails, diagnose whether:
- The test logic is wrong (fix the test)
- A real reference issue exists (fix the XAML, then document the pre-existing issue in design.md)

### 2.2 Run the new test

```powershell
dotnet test CLIHub.sln --filter "FullyQualifiedName~XamlResourceReferenceTests"
# Expect: test passes (green) on current tree
```

If the test fails, diagnose and fix before continuing. The test is the baseline that guards
Stage 3 and 4 refactors.

### Gate: Build and Test

```powershell
dotnet build CLIHub.sln
# Expect: 0 warnings, 0 errors

dotnet test CLIHub.sln
# Expect: all tests pass, including the new XamlResourceReferenceTests

openspec validate --all
# Expect: validation passes
```

---

## Stage 3: Split SettingsStyles.xaml by Role

**Goal**: Extract shared input/control templates into a new `SettingsControls.xaml` (app-level),
shrink SettingsStyles.xaml to window/page chrome + custom-control implicit styles, move
StatusChip styles to SettingsWindow.xaml resources, and update App.xaml merge order + docs.

### 3.1 Create SettingsControls.xaml

**File**: `src/CLIHub/Themes/SettingsControls.xaml`

**Header** (lines 1-10):
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:controls="clr-namespace:CLIHub.Controls"
                    xmlns:themes="clr-namespace:CLIHub.Themes">
    <!--
        Shared input and control templates for Settings pages (extracted from SettingsStyles.xaml).
        Merged at application level because Settings pages are UserControls built before they attach
        to the window tree — window-scoped resources are unreachable during their XAML load.
    -->

    <!-- (content cut from SettingsStyles.xaml follows) -->
```

**Content to cut from SettingsStyles.xaml** (EXACT XAML, no changes):
- Lines 19-40: `FieldLabel`, `HintText`, `StatusLine`, `ErrorText`
- Lines 43-256: `SettingsInput`, `NumericStepButton`, `NumericField` (implicit), `HotkeyField`,
  `HotkeyChip`, `HotkeyChipText`
- Lines 259-356: `SettingsCheck`, `ToggleCheckBox`
- Lines 464-569: `SettingsComboBox`, `SettingsComboBoxItem`
- Lines 613-615: `SettingsIconButton` (BasedOn NeutralButton, so NeutralButton stays in SettingsStyles)
- Lines 689-718: `LinkButton`
- Lines 719-747: `ChipRemoveButton`
- Lines 1000-1130: `SettingsChoiceChipToggle`, `SettingsChoicePopover`, `SettingsChoiceListItem`,
  `SettingsChipComboBox`

Paste these into SettingsControls.xaml (after the header comment, before `</ResourceDictionary>`).

### 3.2 Shrink SettingsStyles.xaml

**File**: `src/CLIHub/Themes/SettingsStyles.xaml`

**Actions**:
1. Replace header comment (lines 6-12) with:
   ```xml
   <!--
       Settings window and page chrome: theme picker preview data, footer buttons, sidebar navigation,
       page headers, and implicit styles for CLIHub.Controls custom types (SectionHeader, SettingCard,
       SettingsGroup). Merged at application level because Settings pages are UserControls built before
       they attach to the window tree.
   -->
   ```
2. Delete the lines cut to SettingsControls.xaml (see 3.1 above).
3. Delete StatusChip styles (lines 639-686: `StatusChip`, `StatusChipGlyph`, `StatusChipText`)
   — these move to SettingsWindow.xaml (see 3.3).

**Remaining content** (after cuts):
- Lines 363-376: `ThemePreview*` brushes (8 keys; MUST stay app-level)
- Lines 378-461: `ThemeOptionItem`, `ThemeOption`, `AccentOption`
- Lines 572-609: `AccentButton`, `NeutralButton`
- Lines 618-637: `HeaderChip`, `HeaderChipIcon`, `HeaderChipText`
- Lines 749-798: `FooterStrip`, `SidebarGroupLabel`, `SidebarItem`, `SidebarList`
- Lines 801-807: `PageHeader`, `EmptyStateText`
- Lines 814-960: implicit styles for `SectionHeader`, `SettingCard`, `SettingsGroup`

### 3.3 Move StatusChip to SettingsWindow.xaml

**File**: `src/CLIHub/Views/SettingsWindow.xaml`

**Action**: add after the existing merged dictionary (WhatsNewStyles.xaml), inside `<Window.Resources>`:

```xml
<!-- Status indicator chip for the Updates tab header; styles are defined here
     because all references are inside this window's DataTemplates (line 65-70). -->
<Style x:Key="StatusChip" TargetType="Border" BasedOn="{StaticResource HeaderChip}">
    <!-- (exact XAML from SettingsStyles.xaml:639-652) -->
</Style>
<Style x:Key="StatusChipGlyph" TargetType="TextBlock">
    <!-- (exact XAML from SettingsStyles.xaml:654-669) -->
</Style>
<Style x:Key="StatusChipText" TargetType="TextBlock">
    <!-- (exact XAML from SettingsStyles.xaml:671-686, including DataTriggers) -->
</Style>
```

Verify parse order: the three StatusChip definitions must be declared BEFORE the DataTemplates that
reference them inside the same `Window.Resources` dictionary. `StaticResource` has no forward
reference inside one resource dictionary, and the referencing `<Border>` lives in the
`UpdatesHeaderAction` DataTemplate, which is itself a value of that dictionary. They therefore sit
immediately after the `UpperCase` converter, before `UpdatesHeaderTitleExtra` and
`UpdatesHeaderAction`. `StatusChip` keeps `BasedOn="{StaticResource HeaderChip}"`; `HeaderChip`,
`HeaderChipLabel`, and `HeaderChipValue` resolve from the application-level `SettingsStyles.xaml`,
which a window resource can reach at load time (the same reachability the existing
`UpdatesHeaderTitleExtra` template already relies on).

### 3.4 Update App.xaml merge order

**File**: `src/CLIHub/App.xaml`

**Action**: insert new SettingsControls.xaml merge AFTER Controls.xaml (line 26), BEFORE
SettingsStyles.xaml:

```xml
<ResourceDictionary.MergedDictionaries>
    <!-- Palette only: shared brushes, no implicit styles, so other windows are unaffected. -->
    <ResourceDictionary Source="/Themes/DarkTheme.xaml"/>
    <!-- Shared keyed styles (button templates, typography, DarkToolTip). -->
    <ResourceDictionary Source="/Themes/Controls.xaml"/>
    <!--
        The slim scrollbar lives here, not in a window dictionary: implicit styles
        from window resources do not reach the scrollbars inside ScrollViewer and ListBox
        templates — only application-level implicit styles do.
    -->
    <ResourceDictionary Source="/Themes/SlimScrollBar.xaml"/>
    <!--
        Settings input/control templates: shared across Settings pages (UserControls).
        Merged at application level because UserControls are built before they attach
        to the window tree — window-scoped resources are not reachable during XAML load.
    -->
    <ResourceDictionary Source="/Themes/SettingsControls.xaml"/>
    <!--
        Settings window styles and page chrome: shared with the extracted Settings pages (UserControls).
        A UserControl is built before it attaches to the window tree, so window-scoped resources are
        not reachable during its load — application-level placement is required, like the scrollbar
        and tooltip styles above. Note: this dictionary contains 4 implicit (keyless) styles for
        CLIHub.Controls custom types (NumericField, SectionHeader, SettingCard, SettingsGroup);
        they are intentional and currently used only by Settings pages.
    -->
    <ResourceDictionary Source="/Themes/SettingsStyles.xaml"/>
</ResourceDictionary.MergedDictionaries>
```

### 3.5 Update documentation

**File**: `docs/architecture/ui.md`

**Line 11-13** (layering paragraph), replace:
OLD:
```
Layering: `DarkTheme.xaml` (dark base palette, merged by App.xaml) with `LightTheme.xaml` and `BlueTheme.xaml` merged on top at startup through the `ThemeCatalog` registry + `Sizing.xaml` (metrics, font and radius tokens) → `Controls.xaml` (shared keyed styles; merges the sizing tokens only — the palette brushes are referenced with `DynamicResource` and resolve from the application-level palette) → per-window dictionaries (`LaunchWindowStyles.xaml`, `WhatsNewStyles.xaml`) that merge only `Controls.xaml`. App.xaml hosts the app-level pieces: the base palette (for window attributes), `Controls.xaml`, `SettingsStyles.xaml` (shared with the extracted Settings pages, which are built before they attach to the window tree), the implicit `ScrollBar` and `ToolTip` styles, and the shared converters.
```

NEW:
```
Layering: `DarkTheme.xaml` (dark base palette, merged by App.xaml) with `LightTheme.xaml` and `BlueTheme.xaml` merged on top at startup through the `ThemeCatalog` registry + `Sizing.xaml` (metrics, font and radius tokens) → `Controls.xaml` (shared keyed styles; merges the sizing tokens only — the palette brushes are referenced with `DynamicResource` and resolve from the application-level palette) → `SettingsControls.xaml` (shared input/control templates for Settings pages) → `SettingsStyles.xaml` (Settings window/page chrome + custom-control implicit styles) → per-window dictionaries (`LaunchWindowStyles.xaml`, `WhatsNewStyles.xaml`) that merge only `Controls.xaml`. App.xaml hosts the app-level pieces: the base palette (for window attributes), `Controls.xaml`, `SettingsControls.xaml`, `SettingsStyles.xaml` (both merged at application level because Settings pages are UserControls built before they attach to the window tree), the implicit `ScrollBar` (SlimScrollBar.xaml) and `ToolTip` styles, and the shared converters.
```

### Gate: Build and Test

```powershell
dotnet build CLIHub.sln
# Expect: 0 warnings, 0 errors

dotnet test CLIHub.sln
# Expect: all tests pass, INCLUDING XamlResourceReferenceTests from Stage 2
# (if the split broke any references, this test will catch it)

openspec validate --all
# Expect: validation passes
```

**Visual verification** (manual) — NOT RUN (pending user):
1. Run `dotnet run --project src/CLIHub/CLIHub.csproj`
2. Open Settings window → verify all pages render correctly (no missing styles)
3. Check theme picker on Appearance page → verify preview cards render
4. Check Updates tab header → verify status chip renders (should be window-scoped now)
5. Close app

No GUI session was started during implementation, so this list is unverified.

---

## Stage 4: Tokenize Repeated Literals and Consolidate Radius Keys

**Goal**: Add Sizing.xaml tokens for repeated literals (input height, radius, font size, button
padding), consolidate duplicate radius tokens, and update all reference sites.

### 4.1 Add new Sizing.xaml tokens

**File**: `src/CLIHub/Themes/Sizing.xaml`

**Action**: insert before `</ResourceDictionary>` (after existing tokens):

```xml
<!-- Input control metrics -->
<sys:Double x:Key="Sizing.Input.Height">30</sys:Double>

<!-- Additional corner radius tokens -->
<sys:Double x:Key="Sizing.CornerRadius.Control">4</sys:Double>

<!-- Additional font size tokens -->
<sys:Double x:Key="Sizing.FontSize.Hint">12.5</sys:Double>

<!-- Button metrics -->
<Thickness x:Key="Sizing.Button.FooterPadding">18,7</Thickness>
```

Verify `xmlns:sys="clr-namespace:System;assembly=mscorlib"` is present in the file header
(should already exist for other sys:Double tokens).

### 4.2 Update Sizing.xaml header comment

**File**: `src/CLIHub/Themes/Sizing.xaml`

Replace lines 4-10 (approximate) header comment with:

```xml
<!--
    Sizing tokens: metrics, font families, and radius values referenced by Controls.xaml,
    SettingsControls.xaml, SettingsStyles.xaml, and per-window dictionaries via StaticResource.
    Naming convention: Sizing.<Category>.<Property> (e.g., Sizing.FontSize.Body,
    Sizing.CornerRadius.Card, Sizing.Input.Height, Sizing.Button.FooterPadding).
    Categories: FontFamily, FontSize, CornerRadius, Input, Button, plus scalar Spacing.
-->
```

### 4.3 Delete duplicate radius token

**File**: `src/CLIHub/Themes/Sizing.xaml`

Delete line 82:
```xml
<sys:Double x:Key="Sizing.Chip.CornerRadius">6</sys:Double>
```

Keep line 79 (`Sizing.CornerRadius.Chip`).

### 4.4 Update radius token references

**Files affected**: grep for `Sizing.Chip.CornerRadius` in `src/CLIHub/Themes/`:
- SettingsStyles.xaml (after Stage 3 split): 2 occurrences
- Controls.xaml: 1 occurrence

**Action**: replace all 3 occurrences with `{StaticResource Sizing.CornerRadius.Chip}`.

### 4.5 Replace input height literals

**File**: `src/CLIHub/Themes/SettingsControls.xaml` (after Stage 3 extraction)

Find all `Height="30"` (approximately 3 occurrences: SettingsInput TextBox, NumericField,
SettingsComboBox) and replace with:
```xml
Height="{StaticResource Sizing.Input.Height}"
```

### 4.6 Replace radius "4" literals

**File**: `src/CLIHub/Themes/SettingsControls.xaml`

Find all `CornerRadius="4"` (approximately 3 occurrences: NumericStepButton,
SettingsComboBoxItem, ChipRemoveButton) and replace with:
```xml
CornerRadius="{StaticResource Sizing.CornerRadius.Control}"
```

Verify no other `CornerRadius="4"` in Themes/ (grep after replacement).

### 4.7 Replace font size "12.5" literals

**Files affected**:
- `src/CLIHub/Themes/LaunchWindowStyles.xaml`: 2 occurrences (FooterInfoText:58, FooterVersionText:91)
- `src/CLIHub/Themes/SettingsControls.xaml`: 1 occurrence (ChoicePopoverItem; find after Stage 3)

**Action**: replace all 3 occurrences with:
```xml
FontSize="{StaticResource Sizing.FontSize.Hint}"
```

### 4.8 Replace padding "18,7" literals

**File**: `src/CLIHub/Themes/SettingsStyles.xaml` (after Stage 3 shrink)

Find `Padding="18,7"` (2 occurrences: AccentButton, NeutralButton) and replace with:
```xml
Padding="{StaticResource Sizing.Button.FooterPadding}"
```

### 4.9 Verify no remaining literals

**Actions**:
```powershell
# Should find ZERO matches in src/CLIHub/Themes/ (except intentional outliers documented in design.md)
Select-String -Path "src/CLIHub/Themes/*.xaml" -Pattern 'Height="30"'
Select-String -Path "src/CLIHub/Themes/*.xaml" -Pattern 'CornerRadius="4"'
Select-String -Path "src/CLIHub/Themes/*.xaml" -Pattern 'FontSize="12\.5"'
Select-String -Path "src/CLIHub/Themes/*.xaml" -Pattern 'Padding="18,7"'
```

If any matches found, verify they are intentional outliers (not the ones targeted by this stage).

### Gate: Build, Test, and Graphify Update

```powershell
dotnet build CLIHub.sln
# Expect: 0 warnings, 0 errors

dotnet test CLIHub.sln
# Expect: all tests pass, INCLUDING XamlResourceReferenceTests from Stage 2
# (if tokenization broke any references, this test will catch it)

openspec validate --all
# Expect: validation passes

graphify update .
# Expect: graph updated (no errors; repository knowledge graph stays current)
```

**Visual verification** (manual) — NOT RUN (pending user):
1. Run `dotnet run --project src/CLIHub/CLIHub.csproj`
2. Open Launch window → verify footer text size (12.5) unchanged
3. Open Settings window → verify:
   - Input heights (30) unchanged
   - Button padding (18,7) unchanged
   - Control radii (4) unchanged
   - Hint text sizes (12.5) unchanged
4. Toggle through all Settings pages → verify no visual regressions
5. Close app

No GUI session was started during implementation, so this list is unverified.

---

## Final Verification

After all 4 stages complete:

```powershell
# Full build and test
dotnet build CLIHub.sln
dotnet test CLIHub.sln

# OpenSpec validation (should pass with skip_specs: true)
openspec validate --all

# Verify no remaining target literals
Select-String -Path "src/CLIHub/Themes/*.xaml" -Pattern 'Height="30"|CornerRadius="4"|FontSize="12\.5"|Padding="18,7"'
# Expect: zero matches (or only documented intentional outliers)

# Verify no DarkScrollBar references
Select-String -Path . -Pattern "DarkScrollBar" -Exclude "*.md","CHANGELOG.txt"
# Expect: zero matches (only in change docs and git history)

# Verify no Sizing.Chip.CornerRadius references
Select-String -Path "src/CLIHub/Themes/*.xaml" -Pattern "Sizing\.Chip\.CornerRadius"
# Expect: zero matches
```

**Manual smoke test** — NOT RUN (pending user):
1. Launch CLIHub
2. Open Launch window → verify visual appearance unchanged
3. Open Settings → cycle through all pages → verify no visual regressions
4. Change theme (Appearance page) → restart → verify new theme applies
5. Close app

This smoke test was not executed by the implementer or the follow-up fix pass; no GUI session was
started. It remains the only outstanding verification item.

---

## Implementation Notes (Deviations Recorded During Execution)

These task details were stale. The implementation follows the corrected notes while preserving the
stage goals; design.md's "Implementation Record" carries the rationale.

- 1.3: `ThemeCatalog.DarkPalettePath` is `public const string`; the declaration and its XML docs
  were removed.
- 1.4: `docs/architecture/ui.md` had no `DarkScrollBar` mention at the old line 17. Only the layering
  paragraph was updated (the implicit `ScrollBar` now names `SlimScrollBar.xaml`).
- 2.1: the test uses `XDocument` for structure and a reference regex on attribute values. It has one
  documented runtime-provided key allowlist (`PathToImage`, injected by `ApplicationHost`) and fails
  loudly on nested markup-extension keys instead of skipping them.
- 3.1/3.3: `SettingsWindow.xaml` does not merge `WhatsNewStyles.xaml`. The `StatusChip*` styles were
  added to its existing inline `Window.Resources` dictionary, immediately after the `UpperCase`
  converter, before the `UpdatesHeaderTitleExtra`/`UpdatesHeaderAction` templates that use them
  (verifier follow-up; the first placement after `NavPages` was a forward reference).
- 3.1: `SettingsIconButton` stays in `SettingsStyles.xaml` beside its `NeutralButton` base.
- 3.1/3.2: `SettingsStyles.xaml` merges `SettingsControls.xaml` (needed for the remaining
  `SettingCard` template's `StaticResource HintText`) instead of merging `Controls.xaml` directly.
- 4.1: `Sizing.CornerRadius.Control` is a `CornerRadius` resource, not `sys:Double`.
- 4.5/4.6/4.7: these literals are setter `Value` attributes; four `CornerRadius="4"` sites were
  tokenized. Menu-row height 30 sites (`SettingsChoiceListItem`, two LaunchWindowStyles action-menu
  rows) remain literals as intentional role-specific outliers. `FontSize="15"` remains literal as
  planned.
- 4.9: verification must also check `Value="..."` forms. `Height="30"` still matches the two
  documented LaunchWindowStyles menu-row outliers and `SettingsChoiceListItem`'s setter.
- 4.6 (verifier follow-up): `SettingsAppearancePage.xaml`'s theme-preview sample radius now uses
  `Sizing.CornerRadius.Control` instead of a `CornerRadius="4"` literal; the page already resolved
  other app-level `Sizing.*` keys, so reachability is unchanged.

## Completion Criteria

Status after the implementer pass plus the verifier follow-up on 2026-10-21. A checked item was
observed in this working tree; an unchecked item has not been done.

- ✅ All comments are accurate (SettingsStyles is app-level; 4 implicit styles documented)
- ✅ `ThemeCatalog.DarkPalettePath` deleted
- ✅ `DarkScrollBar.xaml` renamed to `SlimScrollBar.xaml` (all references updated)
- ✅ New `XamlResourceReferenceTests.cs` exists and passes post-refactor
  (baseline greenness: see "Historical baseline evidence" below — observed once by the implementer,
  not reproducible from this tree)
- ✅ `SettingsControls.xaml` created and merged in App.xaml
- ✅ `SettingsStyles.xaml` shrunken to window/page chrome + implicit styles
- ✅ `StatusChip` styles moved to `SettingsWindow.xaml` resources, declared before the first
  DataTemplate that uses them
- ✅ Sizing.xaml tokens added for input height, radius, font size, button padding
- ✅ Duplicate `Sizing.Chip.CornerRadius` deleted; all references use `Sizing.CornerRadius.Chip`
- ✅ All repeated literals in the documented scope replaced with tokens (verified by grep), including
  the theme-preview sample radius on `SettingsAppearancePage.xaml` (verifier finding)
- ✅ `docs/architecture/ui.md` updated to reflect new layering
- ✅ `dotnet build CLIHub.sln` → 0 warnings, 0 errors (observed 2026-10-21, follow-up pass)
- ✅ `dotnet test CLIHub.sln` → 1011 passed, 0 failed (605 + 354 + 52), including the new integrity
  test (observed 2026-10-21, follow-up pass)
- ✅ Focused `dotnet test CLIHub.sln --filter "FullyQualifiedName~XamlResourceReferenceTests"` →
  1 passed, 0 failed (observed 2026-10-21, follow-up pass)
- ✅ `openspec validate --all` → 29 passed, 0 failed (observed 2026-10-21, follow-up pass)
- ✅ `graphify update .` → exit code 0, "Code graph updated" (final re-run 2026-10-21 after the XAML
  and doc fixes; the command also prints an informational "21 code file(s) yielded no symbols"
  warning to stderr)
- ✅ Manual visual verification → user confirmed smoke test OK (2026-10-21): Launch window, Settings
  pages including Appearance (theme previews) and Updates (status chip), theme switch, and restart.

## Historical baseline evidence

The Stage 2 baseline requirement was that `XamlResourceReferenceTests` pass on the pre-split tree at
`0f8bf93` before Stage 3. The implementer reported observing that run (green) before the split. That
observation is not independently reproducible from the current working tree: the pre-split
`SettingsStyles.xaml` no longer exists in it, so re-running the test now exercises the post-refactor
layout only. The verifier correctly could not substantiate the historical result from this tree. The
record keeps it as an implementer observation, not as a reproduced check; no separate worktree was
created to replay it.

## Verifier Follow-up (2026-10-21)

Independent review raised four items; all were resolved in the working tree:

1. `SettingsWindow.xaml` declared `StatusChip`/`StatusChipGlyph`/`StatusChipText` after the
   `UpdatesHeaderAction` DataTemplate that uses them (`StaticResource` has no forward reference inside
   one dictionary). The three styles moved to immediately after the `UpperCase` converter, before
   `UpdatesHeaderTitleExtra`/`UpdatesHeaderAction`. Trigger logic, setters, and the
   `BasedOn="{StaticResource HeaderChip}"` base are unchanged.
2. `SettingsAppearancePage.xaml` used an un-tokenized `CornerRadius="4"` on the theme preview sample;
   it now uses `{StaticResource Sizing.CornerRadius.Control}` (same value 4). The page already resolves
   other `Sizing.*` keys through app-level resources, so reachability is the same.
3. The completion criteria above record the user's confirmed successful manual smoke test.
4. The Stage 2 baseline is recorded as an implementer observation with the reproducibility caveat
   above.
