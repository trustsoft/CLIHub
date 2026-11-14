# Accessibility Sweep: Settings and Launch Windows

## Why

Three accessibility gaps affect users with assistive technology (Windows Narrator and screen readers):

1. **Decorative icon glyphs are announced redundantly.** WPF's TextBlockAutomationPeer exposes all TextBlock text content, including Unicode glyphs that duplicate adjacent text labels (page headers, sidebar items, SettingCard icons, SummaryCard icons, status indicators, input-field decorations, and menu glyphs). The peer falls back to the `Text` property even when `AutomationProperties.Name=""` is set. WPF has no equivalent to the `AccessibilityView` property available in UWP, so the recommended approach is an element that returns `null` from `OnCreateAutomationPeer()` to exclude it from the UIA tree.

2. **Actions-menu entries lack accessible names and tooltips.** The `MenuAction` ViewModel has `Label` and `Glyph` properties, but no `Tooltip` or `AccessibleName` fields. The menu item templates in both SettingsAgentsPage.xaml and LaunchWindow.xaml bind only `Content` to `Label`; tooltips and accessible-name bindings were not added when the data-driven menu model was adopted. At minimum, the ACTIONS toggle button in SettingsAgentsPage has no accessible name beyond its glyph, leaving screen readers unable to announce its function.

3. **Ignored-pattern chips give no hint that Tab reveals a remove button.** The chip's focusable Border reveals a remove button on hover or keyboard focus (via triggers added in change `2026-11-11-pattern-chip-hover-remove`). The chip's automation peer currently announces only the pattern text; a keyboard/screen-reader user has no affordance hint until after they Tab into the chip.

## What Changes

- **DecorativeGlyph element (new):** A sealed class `DecorativeGlyph : TextBlock` in `src/CLIHub/Controls/DecorativeGlyph.cs` that overrides `OnCreateAutomationPeer()` to return `null`, hiding the glyph from the UIA tree while preserving identical visual rendering and inheriting all TextBlock properties and styles.

- **MenuAction extension:** Add init-only properties `Tooltip` (string?) and `AccessibleName` (string?) to `src/CLIHub/ViewModels/Shared/MenuAction.cs`, plus a computed property `EffectiveAccessibleName` that falls back to `Label` when `AccessibleName` is null or empty.

- **Template bindings:** Update action-item templates in SettingsAgentsPage.xaml and LaunchWindow.xaml to bind `ToolTip="{Binding Tooltip}"` and `AutomationProperties.Name="{Binding EffectiveAccessibleName}"` on button/toggle-button elements. Add `AutomationProperties.Name="Plugin catalog actions"` to the ACTIONS toggle in SettingsAgentsPage.xaml.

- **Pattern-chip accessible name:** Add `AutomationProperties.Name="{Binding StringFormat='Ignored pattern {0}, Tab reveals the remove button'}"` to the focusable ChipBorder in SettingsProjectsPage.xaml (line 226).

- **Decorative-glyph replacement:** Replace 20 TextBlock elements that render Segoe MDL2 glyphs adjacent to text labels with DecorativeGlyph elements across 8 view files and 2 template styles.

## Impact

- **Affected specs:** `preferences-ui` (3 ADDED requirements), `launch-window-layout` (1 ADDED requirement).
- **Affected files:**
  - `src/CLIHub/Controls/DecorativeGlyph.cs` (new)
  - `src/CLIHub/ViewModels/Shared/MenuAction.cs` (extension)
  - `src/CLIHub/Views/SettingsAgentsPage.xaml` (templates, toggle name, 3 button glyphs)
  - `src/CLIHub/Views/LaunchWindow.xaml` (templates, 2 action glyphs)
  - `src/CLIHub/Views/SettingsProjectsPage.xaml` (chip accessible name, 3 glyphs)
  - `src/CLIHub/Themes/Common/SettingsStyles.xaml` (2 template glyphs)
  - `src/CLIHub/Themes/Common/SettingsControls.xaml` (3 template glyphs)
  - `src/CLIHub/Views/SettingsWindow.xaml` (3 glyphs)
  - `src/CLIHub/Views/SettingsAgentDetectionPage.xaml` (1 glyph)
  - `src/CLIHub/Views/SettingsAppearancePage.xaml` (1 glyph)
  - `src/CLIHub/Views/SettingsProjectRegistryPage.xaml` (2 glyphs)
  - `tests/CLIHub.Tests/ViewModels/MenuActionTests.cs` (new, 3 unit tests)
  - `tests/CLIHub.Tests/Controls/DecorativeGlyphTests.cs` (new, 2 STA unit tests)

- **Non-goals:**
  - No changes to icon-only buttons (Windows header, action buttons with tooltips and no adjacent text, Launch window footer buttons) — these remain TextBlocks with proper `AutomationProperties.Name`.
  - No animation, visual change, or new configuration options.
  - No changes to other windows or dialogs.
  - No changes to the ChipRemoveButton, SettingCard, or SectionHeader styles beyond replacing decorative glyphs.

## Dependencies

Depends on change `2026-11-11-pattern-chip-hover-remove` (archived; change 2026-11-11-pattern-chip-hover-remove) for the focusable pattern chip Border structure.
