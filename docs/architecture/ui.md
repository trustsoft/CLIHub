# UI Architecture and Theming

Window inventory and MVVM responsibilities: [../architecture.md](../architecture.md) (Project Responsibilities). This document covers resource organization and theming.

## Resource Organization
- Application icon: `src/CLIHub/app.ico` (embedded; also the exe icon)
- Default project logo: `src/CLIHub/default-project.png` (copied to output)
- Desktop app icon sources: `assets/` at the repository root (not part of the build)
- Embedded seed descriptors/logos: `src/CLIHub.Core/SeedPlugins/`

## XAML Themes and Styles Recipe

Layering: `LaunchTheme.xaml` (palette — the only place with colors) + `Sizing.xaml` (metrics, font and radius tokens) → `Controls.xaml` (shared keyed styles; merges palette and Sizing itself) → per-window dictionaries (`LaunchWindowStyles.xaml`, `SettingsStyles.xaml`, `WhatsNewStyles.xaml`) that merge only `Controls.xaml`. App.xaml hosts the app-level pieces: the palette (for window attributes), `Controls.xaml`, the implicit `ScrollBar` and `ToolTip` styles, and the shared converters.

To add a window:
1. Create `Themes/<Window>Styles.xaml` merging `/Themes/Controls.xaml` (this brings the palette and sizing tokens transitively — StaticResource inside a dictionary resolves only against that dictionary's own merged dictionaries).
2. Define keyed styles only (no implicit styles — they would not reach popups anyway); build on the shared bases (`IconChipButton`, `QuietChipButton`, `WindowHeader`, `WindowTitle`) via `BasedOn`.
3. Merge it from the window's `Window.Resources`; reference colors exclusively through palette brushes, metrics through `Sizing.*`, glyphs through `IconGlyphs`.
4. Add tooltips as plain `ToolTip="..."` attributes — the app-level implicit style themes them automatically.

To create a new palette (compile-time theme choice, applied at startup):
1. Copy `LaunchTheme.xaml` to a new file and change the colors.
2. Point the palette merges in `App.xaml` and `Controls.xaml` at the new file.
Runtime re-theming without restart would require brushes referenced via `DynamicResource` — deliberately deferred until a second theme ships.
