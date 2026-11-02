# Design: Compact theme option cards

## Verified current state

- `src/CLIHub/Views/SettingsAppearancePage.xaml:20-92` — the theme `ListBox.ItemTemplate`: a grid with
  a `Border Height="56"` preview and a text stack holding the option name (`Sizing.FontSize.Body`,
  medium) and its description (`HintText`, dimmed).
- `src/CLIHub/Views/SettingsAppearancePage.xaml:25-29` — the preview border carries the theme brush,
  the control stroke, and the control corner radius; its content grid holds the accent dot, the
  palette bar, and the selection check badge.
- `src/CLIHub/Themes/ThemePickerData.xaml:36-78` — `ThemeOptionItem` supplies the card chrome
  (`Padding="10"`, chip background, selection ring, hover stroke) and the 8 DIP gap between cards.
- `src/CLIHub/Themes/ThemeDescriptors.cs:49-70` — the four palette descriptions.
- `src/CLIHub/ViewModels/Settings/SettingsOptions.cs:62-68` — `AppThemeOption` carries `Name`,
  `Description`, the two preview brush keys, and `UsesAccentPreviewDot`.
- `openspec/specs/preferences-ui/spec.md:271-293` — "Application theme selector" requires the card to
  show a miniature preview, the option name, and a short description, with the "Restart note" scenario
  relying on the System Theme card's description.

## Decisions

### Preview and gap

The swatch is 48 DIP, the gap under it is 4 DIP, and the name is inset 2 DIP from the left. This keeps
the miniature palette readable while removing 13 DIP from every card; the card padding, fonts, and gaps
between cards are unchanged, so the row keeps its chrome rhythm.

### Description moves to the tooltip

The description is removed from the visible card body and bound to `ToolTip` and
`AutomationProperties.HelpText` on the card root. That keeps the System Theme explanation discoverable
(on hover and for screen readers) instead of deleting the information, and it keeps
`ThemeDescriptor.Description` meaningful for any future surface.

### Preview background

Each preview swatch paints the palette's own main background: `ChromeBgBrush` is `#FF26262C` in
`DarkTheme.xaml`, `#FFF3F3F5` in `LightTheme.xaml`, and `#FF232833` in `BlueTheme.xaml`. The previous
Dark (`#0A0C14`) and Light (`#FFFFFF`) values were approximations, so the card advertised a surface
the theme does not use. The System swatch keeps its horizontal gradient with the Dark and Light values
as its endpoints. The brushes stay fixed (not `DynamicResource`) so all four samples read the same
under either theme, as before.

### Blue palette name

The blue palette's picker name becomes "Deep Blue Theme". The color is a dark, low-saturation blue
(`#232833`), so "Deep Blue" distinguishes it from a generic blue while keeping the "<name> Theme"
pattern the other cards use. The stored token, the enum member, and the description are unchanged, so
there is no migration.

### System palette name

The fourth card's picker name becomes "System Theme" (was "System Sync"), which matches the "<name>
Theme" pattern of the other three cards; the description still states that it follows Windows
personalization. The stored token and enum member are unchanged.

### Template shape

The template root becomes a `StackPanel` (preview, then the name), replacing the two-row `Grid`. The
inner preview content grid, the selection badge, and the `ListBoxItem` control template are untouched.

## Alternatives considered and rejected

- **Keep one line of description with ellipsis.** Rejected: it still costs a full text line, and it
  truncates "High contrast daylight profile" at the default window width — the worst of both.
- **Delete the descriptions from the descriptors.** Rejected: it discards the System Theme explanation
  and changes UI-independent Core data for a layout decision.
- **Shrink the swatch further (40 DIP).** Rejected in review: the palette samples lose readability.

## Open questions

None.
