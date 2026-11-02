# Design: Selection by border color only

## Verified current state

- `src/CLIHub/Themes/ThemePickerData.xaml:37-79` — `ThemeOptionItem`: the card is a `Border` named
  `Card`, and a second `Border` named `SelectionRing` (margin `-1`, `AccentBrush`, collapsed) is drawn
  over the card edge when the item is selected or keyboard-focused.
- `src/CLIHub/Views/SettingsAppearancePage.xaml:58-82` — the theme preview carries an 18 DIP accent
  circle with a checkmark, shown while the owning `ListBoxItem` is selected.
- `src/CLIHub/Views/SettingsAppearancePage.xaml:111-118` and `:134-137` — the accent card draws a
  checkmark over the swatch dot while selected and hides the custom glyph with it.
- `openspec/specs/preferences-ui/spec.md:273-299` and `:301-331` — both selectors require the ring and
  the check as the selection marker.

## Decisions

### One marker: the card border

Selection paints `Card.BorderBrush` with `AccentBrush`. The card already owns a 1 DIP border, so the
marker costs no extra element and no content shift, and the selected card reads exactly like the other
selected controls in Settings.

### Trigger order

`IsMouseOver` is declared first, then `IsSelected`, then `IsKeyboardFocused`. Later triggers win for
the same property, so hovering a selected or focused card keeps the accent border instead of falling
back to the hover stroke. Keyboard focus keeps a visible marker even when the item is not selected,
which preserves the focus indication the old ring supplied.

### Custom accent entry

Removing the check means the custom swatch must keep showing its palette glyph while selected. The
`IsCustom` trigger still swaps the color dot for the glyph treatment; the selection trigger that hid
the glyph is removed.

## Alternatives considered and rejected

- **Keep the ring, drop only the checks.** Rejected: the ring is the extra element the user asked to
  remove; the card border already provides the same outline.
- **Thicken the selected border to 2 DIP.** Rejected: it changes layout metrics and is not needed to
  make the selection legible; the accent color already separates the card from its neighbours.

## Open questions

None.
