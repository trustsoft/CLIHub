# Proposal: Selection by border color only

## Why

The selected theme and accent cards signal selection twice: the card chrome draws a separate accent
ring over its edge, and the content draws an extra badge (a check badge inside the theme preview, a
check inside the accent dot). The duplicate marker makes the cards busier than the rest of the
Settings surface, where selection is expressed by a border color, and it costs an extra visual
element on every selected card.

## What changes

- A selected theme or accent card paints its own border with `AccentBrush`. No selection ring, no
  check badge, and no check inside the accent dot is drawn.
- Hover still raises the border to `ControlStrokeBrush`; selection and keyboard focus take precedence
  over hover so the accent border stays visible.
- The accent dot keeps its own fill (accent for named presets, muted glyph for the custom entry), so
  the custom swatch reads the same whether or not it is selected.

## Impact

- `src/CLIHub/Themes/ThemePickerData.xaml` — `ThemeOptionItem` loses the ring element and switches its
  triggers to the card border.
- `src/CLIHub/Views/SettingsAppearancePage.xaml` — removes the theme preview check badge and the accent
  dot check.
- `preferences-ui`: the "Application theme selector" and "Accent color selector" requirements change
  from "accent ring and check" to an accent-colored border.
- No behavior, configuration, or persistence change; selection state is unchanged.

## Explicit non-goals

- Changing the card sizes, preview swatches, card order, or the accent presets.
- Adding a new selection animation or a thicker selected border.

## Dependencies

None.
