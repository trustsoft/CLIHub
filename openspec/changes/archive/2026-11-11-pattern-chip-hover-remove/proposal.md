# Pattern Chip Hover-Reveal Remove Button

## Why

On the Settings → Project Scanning page, each ignored-pattern chip currently shows a small "×" remove button that is always visible (verified at HEAD `37ef2dc`):

- The chip's DataTemplate (`src/CLIHub/Views/SettingsProjectsPage.xaml:222-246`) renders a Border containing a horizontal StackPanel with a TextBlock (the pattern text) and a Button (the "×" close glyph, Style=`ChipRemoveButton`).
- The Button is always visible and occupies 20×20 pixels plus a 4px left margin, creating visual clutter when multiple pattern chips are displayed.
- The watched-folder rows above (`:89-111`) use the same `ChipRemoveButton` style for their icon buttons (Rescan, Remove), which remain always-visible by design since they have distinct glyph identities (Refresh, Delete) and a wider horizontal layout.
- No other chip usage in the application currently employs hover-reveal; all ChipRemoveButton instances (SettingsProjectRegistryPage, SettingsAgentsPage) are always-visible.

The current always-visible "×" is a reasonable default for general-purpose chips, but the ignored-pattern chips benefit from a cleaner at-rest appearance because:
1. The remove affordance is discoverable on hover without training (the chip is an interactive surface, and hovering reveals the available action).
2. Pattern text is often longer than the chip width allows, and the always-present button reduces the visible pattern characters before truncation.
3. The chips are displayed in a WrapPanel and often appear in groups; reducing their visual weight when not hovered improves scanability.

This change introduces a **hover-reveal remove button** for ignored-pattern chips ONLY: the "×" is hidden by default, appears centered over the chip when the mouse hovers that specific chip, and the pattern text is hidden underneath while the button is visible. The chip's outer dimensions remain constant to avoid WrapPanel reflow. All other ChipRemoveButton usages (watched-folder rows, other pages) remain always-visible as today.

## What Changes

- **UI (XAML only):** The ignored-pattern chip's DataTemplate (`SettingsProjectsPage.xaml:222-246`) is restructured to support hover-reveal:
  - Replace the current StackPanel layout with a Grid.
  - Place the pattern TextBlock directly in the Grid (no StackPanel, no placeholder Border) — the TextBlock determines the chip's content size.
  - Place the remove Button (existing ChipRemoveButton style, unmodified) as a second Grid child with `HorizontalAlignment="Center"`, `VerticalAlignment="Center"`, and `Visibility="Hidden"` by default (layout slot preserved so the chip height is constant), overlaying the center of the chip.
  - Change the chip Border's Padding from the original "8,4,4,4" (asymmetric, accounting for a now-removed always-visible button on the right) to "8,4" (symmetric horizontal padding), making the chip hug its text.
  - Wrap the chip in a ContentControl with a custom ControlTemplate (required because DataTemplate.Triggers cannot target named elements), and add two independent triggers to the ControlTemplate:
    - `<Trigger Property="IsMouseOver">` on the chip Border: sets PatternText `Visibility="Hidden"` (invisible but preserving layout slot) and RemoveButton `Visibility="Visible"`.
    - `<Trigger Property="IsKeyboardFocusWithin">` on the chip Border: applies the same setters, revealing the button when the chip receives keyboard focus via Tab navigation.
  - The chip Border is marked `Focusable="True"` (reachable via Tab) and `FocusVisualStyle="{x:Null}"` (no dotted rectangle; the revealed "×" serves as the focus indicator).
  - The chip Border's existing properties (Margin, Background, BorderBrush, BorderThickness, CornerRadius) are unchanged except for Padding.
  - No changes to the Button's Command binding, glyph, or tooltip.
- **Scope:** ONLY the ignored-pattern chips on `SettingsProjectsPage.xaml`. The watched-folder row buttons (`:89-111`) and every other ChipRemoveButton usage elsewhere in the application remain always-visible.
- **No code changes:** The ViewModel (`WatchedDirectoryEditor.cs`), the ChipRemoveButton style (`Themes/Common/SettingsControls.xaml:501-530`), and all other XAML/C# files are untouched.
- **Keyboard accessibility:** The chip Border is keyboard-focusable (`Focusable="True"`), and tabbing to a chip reveals the "×" button using the same visual treatment as hover (via an `IsKeyboardFocusWithin` trigger). The revealed button is then tabbable, and pressing Enter or Space removes the pattern. When the button is hidden (at rest, `Visibility="Hidden"`), it is excluded from the tab order; it remains in the UIA tree as offscreen, providing mild automation discoverability.
- **Behavior:** The chip dimensions do not change between hover/focus and at-rest states: RemoveButton defaults to `Visibility="Hidden"` (layout slot preserved, so the Grid measures the 20×20px button in both states → constant chip height), and PatternText uses `Visibility="Hidden"` on hover/focus (layout slot preserved → constant chip width), preventing WrapPanel reflow. Hovering or tabbing to one chip does not affect other chips.
- **Durability:** The spec delta modifies the existing "Projects and Paths page options" requirement to add scenarios for the hover-reveal and focus-reveal behavior (chip at-rest state, hover/focus reveals button, text hidden, chip size unchanged, keyboard interaction, other chips unaffected).

## Impact

- Affected specs: `preferences-ui` (MODIFIED: "Projects and Paths page options" requirement; ADDED scenarios for hover-reveal on ignored-pattern chips).
- Affected code:
  - `src/CLIHub/Views/SettingsProjectsPage.xaml` — restructure the ignored-pattern chip DataTemplate (`:222-246`) to use a ContentControl with a custom ControlTemplate: Grid layout with TextBlock (sizing) and overlaid Button (centered, `Visibility="Hidden"` by default so the Grid always measures it for constant chip height), chip Border marked `Focusable="True"` with `FocusVisualStyle="{x:Null}"`, Padding changed from "8,4,4,4" to "8,4", two independent triggers (`IsMouseOver` and `IsKeyboardFocusWithin`) that set PatternText `Visibility="Hidden"` and RemoveButton `Visibility="Visible"`.
- Non-goals:
  - No changes to the ChipRemoveButton style itself; it remains a shared, always-20×20 button with hover/pressed overlays.
  - No hover-reveal for watched-folder row buttons (`:89-111`); those stay always-visible with distinct glyphs.
  - No hover-reveal for chips on other pages (SettingsProjectRegistryPage, SettingsAgentsPage); those keep their always-visible remove buttons.
  - No new control abstractions or code-behind; the change is XAML-only using WPF's built-in ControlTemplate triggers.
  - No animation or transition effects on visibility toggling; the button appears/disappears instantly on hover enter/leave and focus gain/loss.
  - No config option to toggle hover-reveal vs always-visible; the behavior is hard-coded in the DataTemplate.

## Dependencies

None. The change is isolated to one DataTemplate in one XAML file and does not alter the ViewModel, shared styles, or any other component.
