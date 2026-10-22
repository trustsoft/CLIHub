# Tidy Theme Resources

## Why

The WPF theme layer under `src/CLIHub/Themes/` has accumulated technical debt that creates
maintenance risk and readability problems:

1. **Misleading comments**: App.xaml and SettingsStyles.xaml comments claim scoping and style
   characteristics that contradict the actual XAML structure, creating traps for future editors.
2. **No integrity guard**: WPF silently ignores missing DynamicResource keys at runtime, so a
   typo in a resource reference causes a visual regression (e.g., a null brush) that only
   human inspection catches. The existing ThemePaletteParityTests guards palette key consistency
   but nothing verifies that *references* resolve.
3. **1081-line grab-bag**: SettingsStyles.xaml mixes unrelated roles (input templates, theme
   picker preview data, footer buttons, sidebar chrome, custom-control implicit styles) with no
   visible organization, making it hard to find the relevant style when editing a view.
4. **Repeated literals**: Input height `30`, radius `4`, font size `12.5`, and button padding
   `18,7` appear as literals across multiple files instead of using the shared Sizing.xaml
   tokens, creating inconsistency risk when adjusting metrics.
5. **Duplicate radius tokens**: Sizing.xaml defines both `Sizing.CornerRadius.Chip` and
   `Sizing.Chip.CornerRadius` with the same value (6), confusing which to use.
6. **Dead code**: ThemeCatalog declares an unused `DarkPalettePath` constant (App.xaml
   hardcodes the path in XAML, so a C# const cannot be referenced).
7. **Misleading file name**: DarkScrollBar.xaml implies dark-only styling, but it dynamically
   adapts to any palette via DynamicResource.

This change eliminates all seven issues through a four-stage refactor without changing any
visible behavior (no color, size, or layout value changes). The new test from stage 2 guards
stages 3-4 and remains as a CI gate for future edits.

## What Changes

Four staged work items:

### Stage 1: Fix comments and remove dead code

- Rewrite App.xaml:20-24 comment to explain the *real* reason SettingsStyles is app-level:
  Settings pages are UserControls built before they attach to the window tree, so window-scoped
  resources are unreachable during their load.
- Rewrite SettingsStyles.xaml:6-12 header comment to match App.xaml's explanation and remove
  the false "not merged from App.xaml" claim.
- Clarify that SettingsStyles contains 4 intentional implicit styles (NumericField, SectionHeader,
  SettingCard, SettingsGroup) for CLIHub.Controls custom types, currently used only by Settings
  pages.
- Delete `ThemeCatalog.DarkPalettePath` (unused; nothing can reference it from App.xaml XAML).
- Rename `DarkScrollBar.xaml` → `SlimScrollBar.xaml` and update App.xaml:19 Source reference
  plus docs/architecture/ui.md mentions to reflect its palette-neutral behavior.

### Stage 2: Add static XAML resource reference integrity test

New test: `tests/CLIHub.Tests/Themes/XamlResourceReferenceTests.cs`

Scan all `.xaml` under `src/CLIHub`, extract StaticResource/DynamicResource references and
x:Key definitions, model per-file reachability (local definitions + merged-dictionary chain
via Source attributes + application-level set from App.xaml's merges), and assert every
referenced key resolves.

The test must be green on the current tree *before* any refactor (baseline). Handles:
- Self-references within one dictionary
- Window-local keys (e.g., converters defined in Window.Resources used by that window's content)
- Merged dictionary chains resolved via Source attributes
- Application-level resources merged by App.xaml

Out of scope (compile-time checked or not soundly modelable):
- x:Static references to code constants
- TemplateBinding (resolved by WPF control template infrastructure)
- Binding expressions with runtime-evaluated property paths

### Stage 3: Split SettingsStyles.xaml by role

Create a new app-level `SettingsControls.xaml` (merged by App.xaml after Controls.xaml)
containing shared input/control templates used across Settings pages:
- Text styles: FieldLabel, HintText, StatusLine, ErrorText
- Input templates: SettingsInput (TextBox), NumericStepButton, NumericField (implicit),
  HotkeyField, HotkeyChip, HotkeyChipText
- Shared controls: SettingsCheck, ToggleCheckBox, SettingsComboBox + item, SettingsIconButton,
  LinkButton, ChipRemoveButton, SettingsChoiceChipToggle, SettingsChoicePopover,
  SettingsChoiceListItem, SettingsChipComboBox

Keep SettingsStyles.xaml for window/page chrome (footer buttons, HeaderChip trio,
FooterStrip, SidebarGroupLabel/SidebarItem/SidebarList, PageHeader, EmptyStateText) and
custom-control implicit styles (SectionHeader, SettingCard, SettingsGroup).

**ThemePreview brushes stay app-level** (StaticResource references from SettingsAppearancePage.xaml
DataTriggers cannot reach window-scoped resources).

**Move StatusChip/StatusChipGlyph/StatusChipText to SettingsWindow.xaml resources** (grep-verified:
all three are referenced only from SettingsWindow.xaml:65-70, so window scope is correct and
reduces app-level surface).

Update App.xaml merge order, inline comments, and docs/architecture/ui.md (lines 11-19, 66-72)
layering description to reflect the new split.

### Stage 4: Tokenize repeated literals and consolidate radius keys

Add Sizing.xaml tokens (exact same values, following the existing `Sizing.<Category>.<Property>`
convention documented in Sizing.xaml's header comment):
- `Sizing.Input.Height` = 30 (used by SettingsInput TextBox, NumericField, SettingsComboBox)
- `Sizing.CornerRadius.Control` = 4 (used by NumericStepButton, SettingsComboBoxItem,
  ChipRemoveButton, plus existing ScrollBar references)
- `Sizing.FontSize.Hint` = 12.5 (used by FooterInfoText, NotesItemText, ChoicePopoverItem)
- `Sizing.Button.FooterPadding` = "18,7" (used by AccentButton, NeutralButton)

**Consolidate duplicate radius tokens**: keep `Sizing.CornerRadius.Chip` (20 usages) and delete
`Sizing.Chip.CornerRadius` (3 usages). Update all references to the kept token and revise
Sizing.xaml's header comment to document the final naming convention.

**FontSize="15" (WindowTitle, BrandText, NotesVersionHeading)**: keep as literals — different
semantic roles (window chrome, branding, release note heading) that happen to share a size today
but may diverge. A shared token would imply a design coupling that does not exist.

## Impact

- **Users**: no visible change (verified by the new integrity test staying green).
- **Theme contributors**: easier to find the relevant style when editing a view; the new test
  catches reference typos before commit.
- **Settings view authors**: clearer documentation of which styles are app-level (required for
  UserControl pre-attach) vs window-scoped.
- **Build/CI**: one additional test (`XamlResourceReferenceTests`) runs on every test pass.

## Non-goals

- Renaming existing styles or consolidating similar styles (e.g., AccentButton vs NeutralButton
  are semantically distinct).
- Moving Controls.xaml styles into per-window dictionaries (shared across Launch/WhatsNew/Settings).
- Changing any color, size, or layout value (pure refactor; visual behavior unchanged).
- Fixing IconGlyphs.cs semantic aliases (Version/Info both use `\uE946` — intentional for
  conceptually related glyphs from the same icon font).

## Dependencies

None — this change is self-contained within the theme layer and does not depend on or block
other changes.
