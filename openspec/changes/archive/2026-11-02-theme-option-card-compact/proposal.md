# Proposal: Compact theme option cards

## Why

The theme selector cards reserve a 56 DIP preview swatch plus a two-line description, so the theme
row dominates the "Theme & Accent" page and pushes the accent selector below the fold at the default
window size. The description is secondary: it explains the palette in one short line, but the four
cards are identified by their preview and name while scanning.

## What changes

- The preview swatch height drops from 56 to 48 DIP, the gap between the swatch and the option name
  drops from 9 to 4 DIP, and the name gains a 2 DIP left offset.
- Each preview swatch shows its palette's main background color instead of an approximation: Dark
  `#26262C`, Light `#F3F3F5`, Blue `#232833` (already correct), and System gradients between the Dark
  and Light values.
- The blue palette's picker name becomes "Deep Blue Theme" (was "Blue Theme"); the stored token
  (`appTheme: "blue"`), the `AppTheme.Blue` identity, and its description stay unchanged.
- The system palette's picker name becomes "System Theme" (was "System Sync"), matching the other
  card names; the page hint and the stored token (`appTheme: "system"`) keep their meaning.
- The card shows the palette preview and the option name only. The palette description moves to the
  card's tooltip and to its accessible help text, so it stays available on hover and to assistive
  technology.
- `ThemeDescriptor.Description` and `AppThemeOption.Description` stay in place, and the view model
  keeps projecting them; nothing in Core, persistence, or the view model changes.

## Impact

- `preferences-ui`: the "Application theme selector" requirement is restated — cards show the preview
  and name, and expose the description as a tooltip and accessible help text.
- `src/CLIHub/Views/SettingsAppearancePage.xaml` — the theme option template.
- No configuration, persistence, or behavior change; no test asserts the card text, so no test change
  is needed.
- Documentation: no change required; the README caption describes the page as theme and accent only.

## Explicit non-goals

- Changing the palettes themselves (`DarkTheme.xaml`, `LightTheme.xaml`, `BlueTheme.xaml`); only the
  fixed preview swatch values are aligned with them.
- Changing the accent color cards or the card chrome.

## Dependencies

None.
