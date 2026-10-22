# Design: Tidy Theme Resources

## Verified Current State (commit 0f8bf93)

### Theme layering (App.xaml:8-26, docs/architecture/ui.md:11-13)

Application-level merge order:
1. `/Themes/DarkTheme.xaml` (base palette: 24 brushes)
2. `/Themes/Controls.xaml` (merges `/Themes/Sizing.xaml`; shared keyed styles)
3. `/Themes/DarkScrollBar.xaml` (implicit ScrollBar style; uses DynamicResource ScrollThumbBrush)
4. `/Themes/SettingsStyles.xaml` (1081 lines; mixed roles)

Window dictionaries (`LaunchWindowStyles.xaml`, `WhatsNewStyles.xaml`) merge only `/Themes/Controls.xaml`
from their `Window.Resources` (lines LaunchWindow:7, WhatsNew:7).

SettingsWindow.xaml:30 merges `/Themes/WhatsNewStyles.xaml` into window resources (for the What's
New page embedded in Settings Updates tab).

Palette switching (App.xaml.cs:44-57): `ApplyTheme()` appends Light/Blue palette dictionary to
the END of `application.Resources.MergedDictionaries` (later dictionaries win in WPF lookup).
`ApplyAccent()` (lines 63-73) sets `AccentBrush`/`AccentSelectionBrush` as direct entries on
application resources.

ThemeCatalog.cs:15 declares `DarkPalettePath = "/Themes/DarkTheme.xaml"` but grep confirms zero
references (only the declaration line; App.xaml hardcodes the path).

### Misleading comments (verified line-by-line)

**SettingsStyles.xaml:6-12**:
```
Merged from SettingsWindow.xaml (not from App.xaml) so styling stays scoped to this window.
```
FALSE: App.xaml:26 merges it at application level. The file is not merged by SettingsWindow.xaml
(verified: SettingsWindow.xaml:7-30 merges only WhatsNewStyles.xaml).

**App.xaml:20-25**:
```
Settings window styles: shared with the extracted Settings pages (UserControls).
A UserControl is built before it attaches to the window tree, so window-scoped
resources are not reachable during its load — application-level placement is
required, like the scrollbar and tooltip styles above. All styles are keyed.
```
First sentence through "application-level placement is required" is CORRECT (explains the real
reason). Last sentence "All styles are keyed" is FALSE: 4 implicit styles exist (see below).

### Implicit styles in SettingsStyles.xaml (grep-verified)

Lines 128, 814, 844, 960: implicit (keyless) `Style TargetType="{x:Type controls:...}"` for
custom control types:
- `NumericField` (line 128)
- `SectionHeader` (line 814)
- `SettingCard` (line 844)
- `SettingsGroup` (line 960)

All are used exclusively by `Settings*Page.xaml` views (grep-verified reference sites). Behavior
is correct (custom controls get their default appearance); the App.xaml comment overclaims.

### SettingsStyles.xaml role inventory (verified by reading lines 1-1081)

| Lines | Role | Representative styles |
|-------|------|----------------------|
| 19-40 | Text styles | FieldLabel, HintText, StatusLine, ErrorText |
| 43-256 | Input control templates | SettingsInput, NumericStepButton, NumericField (implicit), HotkeyField, HotkeyChip/Text |
| 259-376 | Toggles + theme preview data | SettingsCheck, ToggleCheckBox, ThemePreview* brushes (8 keys) |
| 378-461 | Theme/accent picker styles | ThemeOptionItem, ThemeOption, AccentOption |
| 464-569 | ComboBox | SettingsComboBox, SettingsComboBoxItem |
| 572-637 | Footer buttons + header chips | AccentButton, NeutralButton, SettingsIconButton, HeaderChip trio |
| 639-686 | Status chip (Updates page) | StatusChip, StatusChipGlyph, StatusChipText |
| 689-798 | Sidebar/link/remove buttons | LinkButton, ChipRemoveButton, FooterStrip, SidebarGroupLabel/Item/List |
| 801-960 | Page chrome | PageHeader, EmptyStateText |
| 814-960 | Custom control implicit styles | SectionHeader (implicit), SettingCard (implicit), SettingsGroup (implicit) |
| 1000-1130 | Settings choice controls | SettingsChoiceChipToggle, SettingsChoicePopover, SettingsChoiceListItem, SettingsChipComboBox |

**StatusChip reference sites** (grep output):
- SettingsStyles.xaml:639, 654, 671 (definitions)
- SettingsWindow.xaml:65, 67, 69 (3 StaticResource references, all inside SettingsWindow.xaml
  DataTemplates for the status indicator in the Updates page tab header)

All three StatusChip* styles can move to SettingsWindow.xaml resources (no Settings*Page.xaml
references found).

**ThemePreview brush reference sites** (grep output):
- SettingsStyles.xaml:363-376 (8 definitions)
- SettingsAppearancePage.xaml:36-81 (9 StaticResource references inside DataTriggers)

ThemePreview brushes MUST stay application-level (SettingsAppearancePage is a UserControl;
its DataTriggers evaluate during XAML load before window-tree attachment).

### Repeated literals (verified by grep and manual inspection)

| Value | Files | Lines | Usage |
|-------|-------|-------|-------|
| Height `30` | SettingsStyles.xaml | 49, 133, 495 | SettingsInput TextBox, NumericField, SettingsComboBox |
| `CornerRadius="4"` | SettingsStyles.xaml | 107, 474, 728 | NumericStepButton, SettingsComboBoxItem, ChipRemoveButton |
| FontSize `12.5` | LaunchWindowStyles.xaml, SettingsStyles.xaml | Launch:58,91; Settings:1062 | FooterInfoText, FooterVersionText (Launch); ChoicePopoverItem (Settings) |
| Padding `18,7` | SettingsStyles.xaml | 578, 609 | AccentButton, NeutralButton |
| FontSize `15` | Controls.xaml, WhatsNewStyles.xaml | Controls:31,38; WhatsNew:28 | WindowTitle, BrandText, NotesVersionHeading |

FontSize `15` serves different semantic roles (window chrome, branding, release note heading) —
keep as literals per the brief's guidance (no design coupling implied).

### Duplicate radius tokens (Sizing.xaml:79-82, grep-verified)

```xml
<sys:Double x:Key="Sizing.CornerRadius.Chip">6</sys:Double>
<sys:Double x:Key="Sizing.Chip.CornerRadius">6</sys:Double>
```

Usage counts:
- `Sizing.CornerRadius.Chip`: 20 references (grep output: 18 SettingsStyles, 1 Controls, 1 LaunchWindowStyles)
- `Sizing.Chip.CornerRadius`: 3 references (grep output: 2 SettingsStyles, 1 Controls)

Decision: keep `Sizing.CornerRadius.Chip` (more-used; matches `Sizing.CornerRadius.*` convention
for other radii in the file).

### Sizing.xaml convention (verified from header comment, lines 4-10)

Existing token naming pattern: `Sizing.<Category>.<Property>` where Category groups related
metrics:
- `Sizing.FontFamily.*` (Icon, Mono)
- `Sizing.FontSize.*` (Body, Section, Caption)
- `Sizing.CornerRadius.*` (Card, Chip)
- `Sizing.Spacing` (scalar)

New tokens follow the same pattern:
- `Sizing.Input.Height` (new category for input control metrics)
- `Sizing.CornerRadius.Control` (extends existing CornerRadius category)
- `Sizing.FontSize.Hint` (extends existing FontSize category)
- `Sizing.Button.FooterPadding` (new category for button metrics; Thickness value "18,7")

## WPF Constraints (verified semantics, documented in design to prevent backtracking)

1. **StaticResource resolution**: inside a ResourceDictionary, StaticResource resolves only
   against that dictionary's own entries plus its merged dictionaries (via `<ResourceDictionary.MergedDictionaries>`).
   It does NOT walk up to application resources at parse time.
2. **DynamicResource resolution**: defers to runtime; walks up the logical tree to window resources
   and then application resources.
3. **Implicit styles from window resources do not reach**: popups, controls inside templates,
   or tooltips (rendered outside the window's visual tree). Only application-level implicit styles
   reach these.
4. **UserControl XAML load timing**: Settings pages are UserControls instantiated BEFORE they
   attach to the window tree (via ContentPresenter in SettingsWindow.xaml's content area).
   During their XAML parse, they can see only their own `UserControl.Resources` and
   application-level resources — NOT the window's resources. Therefore, EVERY style referenced
   by ANY `Settings*Page.xaml` MUST remain at application level.
5. **Parse-order dependency**: in window resources, a style must be defined BEFORE its first
   StaticResource use (parse order matters; no forward references for StaticResource).
6. **Palette merge order**: base palette first (DarkTheme.xaml in App.xaml:11), then active
   palette appended to the end of MergedDictionaries by `ApplyTheme()` (later wins). This order
   must remain unchanged.

## Stage 1: Fix Comments and Remove Dead Code

### Changes

1. **App.xaml:20-25 comment**: rewrite to clarify "All styles are keyed" is false (4 implicit
   custom-control styles exist) and explain they are intentional for CLIHub.Controls types.
2. **SettingsStyles.xaml:6-12 header comment**: rewrite to match App.xaml's explanation (app-level
   because of UserControl pre-attach timing); remove the "not from App.xaml" false claim.
3. **ThemeCatalog.cs:15**: delete the unused `DarkPalettePath` constant (App.xaml hardcodes the
   path in XAML; nothing can reference it).
4. **DarkScrollBar.xaml → SlimScrollBar.xaml**: rename file and update:
   - App.xaml:19 Source attribute: `/Themes/SlimScrollBar.xaml`
   - SlimScrollBar.xaml:6-7 header comment: remove "dark" reference; emphasize palette-neutral
     behavior via DynamicResource
   - docs/architecture/ui.md:13 mention (inside layering paragraph)
   - docs/architecture/ui.md:17 mention (implicit styles explanation)

No code behavior changes; purely comment/naming/dead-code cleanup.

## Stage 2: XAML Resource Reference Integrity Test

### Test design

New file: `tests/CLIHub.Tests/Themes/XamlResourceReferenceTests.cs`

Single test: `XamlFiles_AllResourceReferencesResolve()`

Algorithm:
1. Find all `.xaml` files under `src/CLIHub` (recursive Directory.GetFiles).
2. For each file:
   - Extract x:Key definitions (regex: `x:Key="([^"]+)"`)
   - Extract StaticResource/DynamicResource references (regex: `{(?:Static|Dynamic)Resource\s+([^}]+)}`)
   - Parse Source attributes in MergedDictionaries (regex: `Source="([^"]+)"`)
3. Model reachability per file:
   - Local keys: x:Key definitions in the file itself
   - Merged keys: recursively walk Source paths (resolve relative to `src/CLIHub/` root),
     collect their local keys
   - Application keys: if the file is App.xaml OR is referenced by App.xaml's MergedDictionaries,
     include the application-level key set (App.xaml's local keys + App.xaml's merged keys)
   - Window keys: if the file is a Window (ends with `Window.xaml`), include its local +
     merged keys for references inside that window's content
4. Assert: every referenced key resolves against the file's reachable key set.

**Handling**: Window-local keys (e.g., converters defined in Window.Resources and used by
that window's Grid/StackPanel content) are modeled by treating the window file as both a
dictionary (defines keys) and a consumer (references keys from its own definitions + merges
+ app-level).

**Out of scope** (compile-time checked or not soundly modelable without full XAML parser):
- `x:Static` references (e.g., `{x:Static themes:IconGlyphs.Settings}`) — compile-time checked
- `TemplateBinding` (resolved by WPF control template infrastructure, not resource lookup)
- `Binding` expressions with property paths (runtime-evaluated; not static resource references)

**Exclusions**: if a reference form cannot be modeled soundly (e.g., dynamic key construction
via string concatenation in code-behind, or multi-part keys like `{DynamicResource {x:Static ...}}`),
narrow the test to what it can verify and document the exclusion in a comment. The test must NOT
pass for the wrong reasons (e.g., by skipping all references that would fail).

**Baseline requirement**: the test MUST be green on commit 0f8bf93 (current tree) before any
refactor. This proves it models the existing structure correctly.

### Error reporting

On assertion failure, report:
- File path and line number (if extractable from regex match position)
- Referenced key that does not resolve
- Reachable key set for that file (for diagnosis)

Use `Assert.True()` with a detailed failure message rather than bare `Assert.Contains()` to give
implementers actionable context.

## Stage 3: Split SettingsStyles.xaml by Role

### New file: SettingsControls.xaml

Merge order: placed AFTER Controls.xaml in App.xaml (new line 26; SettingsStyles moves to line 27).

Header comment:
```
Shared input and control templates for Settings pages (extracted from SettingsStyles.xaml).
Merged at application level because Settings pages are UserControls built before they attach
to the window tree — window-scoped resources are unreachable during their XAML load.
```

Content: all shared input/control templates that appear in SettingsStyles.xaml and are used
across multiple Settings pages:
- Text styles: `FieldLabel`, `HintText`, `StatusLine`, `ErrorText` (lines 19-40)
- Input templates: `SettingsInput`, `NumericStepButton`, `NumericField` (implicit),
  `HotkeyField`, `HotkeyChip`, `HotkeyChipText` (lines 43-256)
- Toggles: `SettingsCheck`, `ToggleCheckBox` (lines 259-356)
- ComboBox: `SettingsComboBox`, `SettingsComboBoxItem` (lines 464-569)
- Buttons: `SettingsIconButton`, `LinkButton`, `ChipRemoveButton` (lines 613, 689, 719)
- Choice controls: `SettingsChoiceChipToggle`, `SettingsChoicePopover`, `SettingsChoiceListItem`,
  `SettingsChipComboBox` (lines 1000-1130)

These are cut from SettingsStyles.xaml (exact XAML, preserving all attributes/properties) and
pasted into SettingsControls.xaml with no changes.

### Shrunken SettingsStyles.xaml

Revised header comment:
```
Settings window and page chrome: theme picker preview data, footer buttons, sidebar navigation,
page headers, and implicit styles for CLIHub.Controls custom types (SectionHeader, SettingCard,
SettingsGroup). Merged at application level because Settings pages are UserControls built before
they attach to the window tree.
```

Remaining content (after extracting shared controls to SettingsControls.xaml):
- ThemePreview brushes (lines 363-376) — MUST stay app-level (StaticResource references from
  SettingsAppearancePage.xaml DataTriggers)
- Theme/accent picker styles: `ThemeOptionItem`, `ThemeOption`, `AccentOption` (lines 378-461)
- Footer buttons: `AccentButton`, `NeutralButton` (lines 572-609)
- Header chips: `HeaderChip`, `HeaderChipIcon`, `HeaderChipText` (lines 618-637)
- Sidebar/footer chrome: `FooterStrip`, `SidebarGroupLabel`, `SidebarItem`, `SidebarList` (lines 749-798)
- Page chrome: `PageHeader`, `EmptyStateText` (lines 801-807)
- Custom control implicit styles: `SectionHeader`, `SettingCard`, `SettingsGroup` (lines 814-960)

StatusChip styles are removed (moved to SettingsWindow.xaml; see below).

### Move StatusChip to SettingsWindow.xaml

Add to SettingsWindow.xaml's `<Window.Resources>` block (after the existing merged
WhatsNewStyles.xaml dictionary, before closing `</Window.Resources>`):

```xml
<!-- Status indicator chip for the Updates tab header; styles are defined here
     because all references are inside this window's DataTemplates (line 65-70). -->
<Style x:Key="StatusChip" TargetType="Border" BasedOn="{StaticResource HeaderChip}">
    ... (exact XAML from SettingsStyles.xaml:639-652)
</Style>
<Style x:Key="StatusChipGlyph" TargetType="TextBlock">
    ... (exact XAML from SettingsStyles.xaml:654-669)
</Style>
<Style x:Key="StatusChipText" TargetType="TextBlock">
    ... (exact XAML from SettingsStyles.xaml:671-686, including DataTriggers)
</Style>
```

All three styles cut from SettingsStyles.xaml (lines 639-686 removed).

**Parse-order safety (corrected after verifier review)**: inside one resource dictionary,
`StaticResource` has no forward reference. The first consumer is the `Border` inside the
`UpdatesHeaderAction` DataTemplate at SettingsWindow.xaml:65, and that DataTemplate is itself a value
of the same `Window.Resources` dictionary — so "defined in `<Window.Resources>` before the `<Grid>`"
is not sufficient. The three styles are declared immediately after the `UpperCase` converter, before
`UpdatesHeaderTitleExtra` and `UpdatesHeaderAction`. `HeaderChip`, `HeaderChipLabel`, and
`HeaderChipValue` come from the application-level `SettingsStyles.xaml`; a window resource reaches
application resources at load time, which the pre-existing `UpdatesHeaderTitleExtra` template already
demonstrates.

### App.xaml merge order update

Old (lines 11-26):
```xml
<ResourceDictionary Source="/Themes/DarkTheme.xaml"/>
<ResourceDictionary Source="/Themes/Controls.xaml"/>
<ResourceDictionary Source="/Themes/DarkScrollBar.xaml"/>  <!-- renamed in Stage 1 -->
<ResourceDictionary Source="/Themes/SettingsStyles.xaml"/>
```

New (after Stage 3):
```xml
<ResourceDictionary Source="/Themes/DarkTheme.xaml"/>
<ResourceDictionary Source="/Themes/Controls.xaml"/>
<ResourceDictionary Source="/Themes/SlimScrollBar.xaml"/>  <!-- renamed in Stage 1 -->
<ResourceDictionary Source="/Themes/SettingsControls.xaml"/>  <!-- NEW -->
<ResourceDictionary Source="/Themes/SettingsStyles.xaml"/>  <!-- SHRUNKEN -->
```

Order matters: SettingsControls before SettingsStyles ensures SettingsStyles can BasedOn styles
from SettingsControls if needed (e.g., AccentButton could later base on a shared button style).

### Documentation updates

**docs/architecture/ui.md:11-13** (layering paragraph):
OLD:
```
Layering: `DarkTheme.xaml` ... → `Controls.xaml` ... → per-window dictionaries ...
App.xaml hosts the app-level pieces: the base palette (for window attributes), `Controls.xaml`,
`SettingsStyles.xaml` (shared with the extracted Settings pages, which are built before they
attach to the window tree), the implicit `ScrollBar` and `ToolTip` styles, and the shared converters.
```

NEW:
```
Layering: `DarkTheme.xaml` ... → `Controls.xaml` → `SettingsControls.xaml` (shared input/control
templates for Settings pages) → `SettingsStyles.xaml` (Settings window/page chrome + custom-control
implicit styles) → per-window dictionaries ... App.xaml hosts the app-level pieces: the base palette,
`Controls.xaml`, `SettingsControls.xaml`, `SettingsStyles.xaml` (both merged at application level
because Settings pages are UserControls built before they attach to the window tree), the implicit
`ScrollBar` and `ToolTip` styles, and the shared converters.
```

**docs/architecture/ui.md:66-72** (add a theme paragraph):
No structural changes required (the "add a theme" recipe does not reference SettingsStyles).
The layering update in lines 11-13 is sufficient.

## Stage 4: Tokenize Repeated Literals and Consolidate Radius Keys

### Sizing.xaml additions

Insert after existing token definitions (before `</ResourceDictionary>`), grouped by category
per the existing convention:

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

**Note**: `Sizing.Button.FooterPadding` is a Thickness (not sys:Double) because padding is a
multi-value property. Requires `xmlns:sys` declaration (already present in Sizing.xaml).

### Sizing.xaml header comment update

OLD (lines 4-10, approximate):
```
Sizing tokens: metrics, font families, and radius values referenced by Controls.xaml and
per-window dictionaries via StaticResource. Naming convention: Sizing.<Category>.<Property>
(e.g., Sizing.FontSize.Body, Sizing.CornerRadius.Card). The two Chip radius keys are
duplicates (historical) — use Sizing.CornerRadius.Chip going forward.
```

NEW (after Stage 4):
```
Sizing tokens: metrics, font families, and radius values referenced by Controls.xaml,
SettingsControls.xaml, and per-window dictionaries via StaticResource. Naming convention:
Sizing.<Category>.<Property> (e.g., Sizing.FontSize.Body, Sizing.CornerRadius.Card,
Sizing.Input.Height, Sizing.Button.FooterPadding). Categories: FontFamily, FontSize,
CornerRadius, Input, Button, plus scalar Spacing.
```

### Delete duplicate radius token

Remove line 82 in Sizing.xaml:
```xml
<sys:Double x:Key="Sizing.Chip.CornerRadius">6</sys:Double>
```

Update all 3 references to use `Sizing.CornerRadius.Chip`:
- SettingsStyles.xaml: 2 occurrences (grep to find exact lines after Stage 3 split)
- Controls.xaml: 1 occurrence (grep to verify)

### Replace literals with tokens

| File | Old | New | Lines (approximate, verify after Stage 3) |
|------|-----|-----|------------------------------------------|
| SettingsControls.xaml | `Height="30"` | `Height="{StaticResource Sizing.Input.Height}"` | ~3 occurrences (SettingsInput, NumericField, SettingsComboBox) |
| SettingsControls.xaml | `CornerRadius="4"` | `CornerRadius="{StaticResource Sizing.CornerRadius.Control}"` | ~3 occurrences (NumericStepButton, ChipRemoveButton, SettingsComboBoxItem) |
| LaunchWindowStyles.xaml | `FontSize="12.5"` | `FontSize="{StaticResource Sizing.FontSize.Hint}"` | 2 occurrences (FooterInfoText:58, FooterVersionText:91) |
| SettingsControls.xaml | `FontSize="12.5"` | `FontSize="{StaticResource Sizing.FontSize.Hint}"` | 1 occurrence (ChoicePopoverItem) |
| SettingsStyles.xaml | `Padding="18,7"` | `Padding="{StaticResource Sizing.Button.FooterPadding}"` | 2 occurrences (AccentButton:578, NeutralButton:609) |

**Verification step**: after replacements, grep for `Height="30"`, `CornerRadius="4"`,
`FontSize="12.5"`, and `Padding="18,7"` in the Themes/ directory to confirm no remaining
literals (except intentional outliers like window chrome sizes).

**FontSize="15" stays as literal** per brief guidance (WindowTitle, BrandText, NotesVersionHeading
are different semantic roles; no design coupling).

## Implementation Record (Deviations and Plan Corrections)

Recorded during execution on 2026-10-21 against baseline `0f8bf93`, plus a verifier follow-up pass on
the same day. The following plan statements were stale or unsound; the implementation made the
closest correct change that preserves each stage goal and the visible behavior.

Gate status after the follow-up pass: `dotnet build CLIHub.sln` succeeded with 0 warnings and
0 errors; `dotnet test CLIHub.sln` passed 1011 tests (605 + 354 + 52) with 0 failures, including
`XamlResourceReferenceTests`; `openspec validate --all` reported 29 passed, 0 failed;
`graphify update .` exited 0 and reported "Code graph updated" (with an informational
  "21 code file(s) yielded no symbols" warning on stderr). The user confirmed the manual GUI smoke
  test passed on 2026-10-21 (Launch window, Settings pages including Appearance previews and Updates
  status chip, theme switch, and restart).

1. **`ThemeCatalog.DarkPalettePath` is `public const string`** (the task text said `private`). The
   declaration and its XML documentation were removed; a repository-wide search found no references.
2. **`SettingsWindow.xaml` does not merge `WhatsNewStyles.xaml`.** `WhatsNewStyles.xaml` is merged
   only by `WhatsNewWindow.xaml`. The three `StatusChip*` styles were added to its existing inline
   `Window.Resources` `ResourceDictionary` (no merged dictionary was added), positioned immediately
   after the `UpperCase` converter so they precede the DataTemplates that use them — see item 9.
3. **`SettingsIconButton` stays in `SettingsStyles.xaml`.** It is
   `BasedOn="{StaticResource NeutralButton}"`, and `NeutralButton` stays in SettingsStyles. A
   dictionary-level `StaticResource` cannot reach an application-level sibling dictionary, so moving
   it to SettingsControls would either break parse-time resolution or force a
   SettingsControls → SettingsStyles dependency. Keeping the two button variants together preserves
   the one-way layering.
4. **`SettingsStyles.xaml` merges `SettingsControls.xaml`.** The remaining `SettingCard` template
   statically references `HintText`, which moved to SettingsControls. SettingsStyles therefore merges
   SettingsControls (which merges `Controls.xaml` and the Sizing tokens) instead of merging
   `Controls.xaml` directly. App.xaml keeps the order Controls → SettingsControls → SettingsStyles;
   the graph has no cycle.
5. **`docs/architecture/ui.md`** had no `DarkScrollBar.xaml` mention at the old line 17; only the
   layering paragraph named the implicit ScrollBar style. That sentence now names
   `SlimScrollBar.xaml`. App.xaml's scrollbar comment also no longer calls the style "dark".
6. **Stage 4 literal inventory was inaccurate.** The repeated values are setter values
   (`Value="30"`), not attribute literals (`Height="30"`), and the real sites/counts differ:
   - `Sizing.Input.Height` (30) replaces the three input setters (`SettingsInput`, `NumericField`,
     `SettingsComboBox`). The menu-row height 30 (`SettingsChoiceListItem`'s setter and the two
     LaunchWindowStyles action-menu `Height` attributes) stays literal: it is a menu-row metric, not
     an input metric, so a shared token would imply a false design coupling.
   - `Sizing.CornerRadius.Control` (4) replaces four occurrences, including the `SettingsCheck` box
     border that the plan's inventory missed.
   - `Sizing.FontSize.Hint` (12.5) replaces the two LaunchWindowStyles action-menu entry styles (the
     plan named `FooterInfoText`/`FooterVersionText`) and `SettingsChoiceListItem` (the plan called
     it `ChoicePopoverItem`).
   - `Sizing.Button.FooterPadding` replaces the two footer button paddings (`AccentButton`,
     `NeutralButton`).
   - `Sizing.CornerRadius.Control` is declared as `<CornerRadius>`, not `<sys:Double>` as tasks.md
     said: it is assigned to `CornerRadius` properties, and a StaticResource does not apply a string
     converter to a raw double.
7. **Integrity test implementation.** The test parses XAML with `XDocument` and applies the
   reference regex only to attribute values. It models loose-dictionary `StaticResource` as
   self + merges, `DynamicResource` as additionally application-level, and
   Window/UserControl/Application references as local + merges + application. `PathToImage` is the
   single documented runtime-provided key (`ApplicationHost` injects it from the DI container).
   Nested markup-extension keys fail loudly instead of being skipped. Declaration order inside one
   file is not modeled (keys are visible file-wide): strict line-order would produce false failures
   for deferred template content, but the trade-off is that a direct forward reference inside a
   dictionary could be missed. A temporary missing-key probe was observed to fail and was reverted;
   the baseline run passed before the Stage 3 split.
8. **Key-set preservation:** the union of `SettingsControls.xaml` + `SettingsStyles.xaml` +
   `SettingsWindow.xaml` keys equals the pre-change `SettingsStyles.xaml` + `SettingsWindow.xaml`
   key set (54 keys, no additions, no duplicates). The three `StatusChip*` keys moved from
   application to window scope by design.
9. **Verifier follow-up (2026-10-21): `StatusChip*` declaration order in `SettingsWindow.xaml`.**
   The first implementation appended the three styles after `NavPages`, i.e. after the
   `UpdatesHeaderAction` DataTemplate that references them. `StaticResource` cannot forward-reference
   inside one dictionary, and the consuming `<Border>` is a value of that same dictionary, so the
   styles were moved to immediately after the `UpperCase` converter, before `UpdatesHeaderTitleExtra`.
   Setters, trigger logic, and `BasedOn="{StaticResource HeaderChip}"` are byte-for-byte unchanged;
   `HeaderChip`/`HeaderChipLabel`/`HeaderChipValue` still resolve from application-level
   `SettingsStyles.xaml`. The integrity test does not model intra-file order (item 7), so this class
   of defect is caught by review, not by the test.
10. **Verifier follow-up (2026-10-21): theme-preview sample radius.**
   `SettingsAppearancePage.xaml` had an un-tokenized `CornerRadius="4"` on the palette sample border —
   the same metric the change already tokenized as `Sizing.CornerRadius.Control`. It now uses
   `{StaticResource Sizing.CornerRadius.Control}` (value 4, unchanged). Reachability is the same as
   the page's existing `Sizing.*` references: those tokens are app-level through
   `SettingsControls.xaml` → `Controls.xaml` → `Sizing.xaml`, and a `UserControl` resolves
   `StaticResource` against its own resources plus the application set.
11. **Historical baseline evidence.** The pre-split `XamlResourceReferenceTests` run was observed green
   by the implementer on the tree at `0f8bf93` before Stage 3. It cannot be independently replayed
   from the final working tree, because the pre-split `SettingsStyles.xaml` no longer exists there;
   any re-run now validates only the post-refactor layout. The record treats it as an implementer
   observation, not an independently reproduced result. No separate worktree was reconstructed.
12. **Header chip key names.** The plan text calls the trio `HeaderChip`, `HeaderChipIcon`,
   `HeaderChipText`; the actual keys in `SettingsStyles.xaml` are `HeaderChip`, `HeaderChipLabel`, and
   `HeaderChipValue`. The implementation kept the existing names (renaming was a non-goal);
   `SettingsWindow.xaml` references `HeaderChipLabel`/`HeaderChipValue`.

## Alternatives Considered

1. **Move SettingsControls into Controls.xaml**: rejected because Controls.xaml is merged by
   every window dictionary (Launch/WhatsNew/Settings), and settings-only styles would bloat
   non-Settings windows. A separate SettingsControls.xaml at app-level keeps shared settings
   controls accessible to Settings pages (UserControl timing) without affecting other windows.

2. **Move all SettingsStyles to SettingsWindow.xaml resources**: rejected because Settings
   pages are UserControls that load before window-tree attachment. They cannot see window-scoped
   resources. Only styles whose reference sites are ALL inside SettingsWindow.xaml itself
   (like StatusChip) can move to window scope.

3. **Consolidate AccentButton and NeutralButton**: rejected (non-goal); they are semantically
   distinct (primary action vs secondary action) and may diverge beyond padding/foreground.

4. **Create Sizing.FontSize.WindowChrome for FontSize="15"**: rejected because WindowTitle,
   BrandText, and NotesVersionHeading serve different roles (window chrome, branding, release
   note heading) that happen to share a size today but may diverge. A shared token would imply
   a design coupling that does not exist.

## Open Questions

None — all decisions are grounded in verified current state and WPF constraints.

## Risks and Mitigations

### Risk: test false positive (passes but does not catch real issues)

**Mitigation**: the test must be green on the current tree (commit 0f8bf93) BEFORE any refactor.
This proves it models the existing structure correctly. After Stage 3 split, re-run the test
to confirm it catches any reference breaks introduced by the refactor.

### Risk: StatusChip move breaks if DataTrigger binding fails at parse time

**Mitigation**: DataTrigger `Binding` expressions are evaluated at runtime, not parse time.
The `UpdateStatusBadgeTone` property is on the SettingsViewModel (window DataContext), which
is available when the DataTrigger evaluates. The parse-order requirement is met by declaring the
three styles before the first DataTemplate that references them inside `Window.Resources`
(see the corrected parse-order note in Stage 3); being inside `<Window.Resources>` alone is not
enough, because the referencing DataTemplate is a sibling value of the same dictionary.

### Risk: tokenization breaks visual appearance

**Mitigation**: Stage 4 replaces literals with tokens of the EXACT SAME VALUE. The new test
from Stage 2 will stay green if all references resolve correctly. A visual regression (e.g.,
height changes) would be caused by a typo in token name or value, which the test catches.

## Implementation Notes

- Stage 1 and 2 are independent (can run in either order); Stage 2 must complete and pass
  before Stage 3 begins.
- Stage 3 (split) is verified by the Stage 2 test staying green after the split.
- Stage 4 (tokenization) is verified by the Stage 2 test staying green after token replacements
  AND by manual inspection (no visual changes).
- Each stage ends with `dotnet build` (0 warnings/errors), `dotnet test` (all green), and
  `openspec validate --all` gates.
- Final stage additionally runs `graphify update .` to keep the knowledge graph current.
