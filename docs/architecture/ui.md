# UI Architecture and Theming

Window inventory and MVVM responsibilities: [../architecture.md](../architecture.md) (Project Responsibilities). This document covers resource organization and theming.

## Resource Organization
- Application icon: `src/CLIHub/app.ico` (embedded; also the exe icon)
- Default project logo: `src/CLIHub/default-project.png` (copied to output)
- Desktop app icon sources: `assets/` at the repository root (not part of the build)
- Embedded seed descriptors/logos: `src/CLIHub.Core/SeedPlugins/`

## XAML Themes and Styles Recipe

Layering: `LaunchTheme.xaml` (dark palette) and `LightTheme.xaml` (light palette, merged at startup when the light theme is active) + `Sizing.xaml` (metrics, font and radius tokens) → `Controls.xaml` (shared keyed styles; merges palette and Sizing itself) → per-window dictionaries (`LaunchWindowStyles.xaml`, `SettingsStyles.xaml`, `WhatsNewStyles.xaml`) that merge only `Controls.xaml`. App.xaml hosts the app-level pieces: the palette (for window attributes), `Controls.xaml`, the implicit `ScrollBar` and `ToolTip` styles, and the shared converters.

To add a window:
1. Create `Themes/<Window>Styles.xaml` merging `/Themes/Controls.xaml` (this brings the palette and sizing tokens transitively — StaticResource inside a dictionary resolves only against that dictionary's own merged dictionaries).
2. Define keyed styles only (no implicit styles — they would not reach popups anyway); build on the shared bases (`IconChipButton`, `QuietChipButton`, `WindowHeader`, `WindowTitle`) via `BasedOn`.
3. Merge it from the window's `Window.Resources`; reference colors exclusively through palette brushes, metrics through `Sizing.*`, glyphs through `IconGlyphs`.
4. Add tooltips as plain `ToolTip="..."` attributes — the app-level implicit style themes them automatically.

## SpacedPanel

Use `CLIHub.Controls.SpacedPanel` for simple vertical or horizontal groups where a uniform gap belongs
between visible siblings. Bind `Spacing` to the shared `Sizing.Spacing` resource when the same
rhythm is shared by more than one view. Keep outer margins, card padding, and optical one-off offsets on
the relevant element.

Keep `Grid` for pane layouts, list rows, star-sized columns, and any composition that depends on explicit
width relationships. Do not use `SpacedPanel` as an item panel for virtualized lists; it is a small
one-dimensional layout helper rather than a replacement for WPF's virtualizing panels.

## Settings Numeric Fields

Use `CLIHub.Controls.NumericField` for single-number settings instead of a plain `TextBox` with
`SettingsInput`. It renders the shared input surface with decrement/increment chips, clamps the value to
`Minimum` and `Maximum`, supports an optional `Unit` suffix and a `Placeholder` that doubles as the
starting value for an empty field, and accepts the Up/Down keys. `Text` stays a string so the settings
view model keeps its existing text validation.

The dark and light palettes (`LaunchTheme.xaml`, `LightTheme.xaml`) define the same brush keys; brushes are referenced with `DynamicResource`, and `App.xaml` merges the light palette on top of the dark base at startup when the effective theme is light.

To add another palette (chosen at startup):
1. Copy `LaunchTheme.xaml` to a new file and define the same brush keys with new colors.
2. Merge it from `App.xaml` when the corresponding theme is active.
Switching the palette without a restart is not supported; a theme change takes effect on the next start.
