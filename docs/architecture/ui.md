# UI Architecture and Theming

Window inventory and MVVM responsibilities: [../architecture.md](../architecture.md) (Project Responsibilities). This document covers resource organization and theming.

## Resource Organization
- Application icon: `src/CLIHub/app.ico` (embedded; also the exe icon)
- Default logo: `src/CLIHub/default-logo.png` (copied to output)
- Desktop app icon sources: `assets/` at the repository root (not part of the build)
- Embedded seed descriptors/logos: `src/CLIHub.Core/SeedPlugins/`

## XAML Themes and Styles Recipe

Layering: `DarkTheme.xaml` (dark base palette, merged by App.xaml) with `LightTheme.xaml` and `BlueTheme.xaml` merged on top at startup through the `ThemeCatalog` registry + `Sizing.xaml` (metrics, font and radius tokens) → `Controls.xaml` (shared keyed styles; merges the sizing tokens only — the palette brushes are referenced with `DynamicResource` and resolve from the application-level palette) → per-window dictionaries (`LaunchWindowStyles.xaml`, `WhatsNewStyles.xaml`) that merge only `Controls.xaml`. App.xaml hosts the app-level pieces: the base palette (for window attributes), `Controls.xaml`, `SettingsStyles.xaml` (shared with the extracted Settings pages, which are built before they attach to the window tree), the implicit `ScrollBar` and `ToolTip` styles, and the shared converters.

To add a window:
1. Create `Themes/<Window>Styles.xaml` merging `/Themes/Controls.xaml` (this brings the sizing tokens transitively — StaticResource inside a dictionary resolves only against that dictionary's own merged dictionaries; palette brushes resolve from the application-level palette through `DynamicResource`).
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

## Settings Page Layout

The Settings window renders the active page's title, description, and hairline divider once in a fixed
header row of the content area. A page may add a summary row above the divider: on Project Discovery, the
projects, agents, and watcher summary cards sit between the description and the hairline; on Projects, the
total, manual, and discovered summary cards use the same row; on CLI Agents, the agents, default-agent, and
probe summary cards use it. The terms are defined in the [glossary](../glossary.md#settings-ui). Only the
content below the divider is hosted by the vertical `ScrollViewer`, so the page header, summary row,
sidebar, drawn window chrome, validation message, and footer stay visible while long pages scroll. Page
views (`Settings*Page.xaml`) contain only their scrolling settings content; the title and description live
in the `SettingsPage` metadata exposed through `SelectedPage`.

## Settings Numeric Fields

Use `CLIHub.Controls.NumericField` for single-number settings instead of a plain `TextBox` with
`SettingsInput`. It renders the shared input surface with decrement/increment chips, clamps the value to
`Minimum` and `Maximum`, supports an optional `Unit` suffix and a `Placeholder` that doubles as the
starting value for an empty field, and accepts the Up/Down keys. The mouse wheel steps the value while the
pointer is over the field and is consumed so the settings page does not scroll at the same time. `Text`
stays a string so the settings view model keeps its existing text validation.

## Project Display Models and Enrichment

Project rows everywhere in the UI (launch window and Settings Projects page) are shared
`ProjectModel` instances owned by the app's `ProjectModelCatalog`, one per project ID, updated in
place on refresh. Core's stateless `ProjectEnricher` produces the enriched snapshots — one
`Directory.Exists` check plus one project-logo-resolver lookup per project — on defined triggers:
a `ProjectsChanged` event, the launch window being shown or activated, and the Settings Projects
page `Load()`. There are no folder watchers or timers: folder availability stays at its last
snapshot until the next trigger. `IProjectService` reads return detached `Project` copies; only the
catalog owns long-lived row instances. The snapshot (not live) semantics and the instance-ownership
split are rationale-level detail: see the `introduce-project-model` change's `design.md` under
`openspec/changes/archive/2026-10-19-introduce-project-model/`.

The registered palettes (`DarkTheme.xaml`, `LightTheme.xaml`, `BlueTheme.xaml`) define the same brush keys; brushes are referenced with `DynamicResource`, and startup merges the palette registered in `ThemeCatalog.ResolvePalettePath` for the stored theme on top of the dark base (a null resolution keeps the base).

To add another theme (chosen at startup):
1. Copy `DarkTheme.xaml` to a new `<Name>Theme.xaml` and define the same brush keys with new colors.
2. Add the `AppTheme` member and its token in Core, then one arm to `ThemeCatalog.ResolvePalettePath` (`src/CLIHub/Themes/ThemeCatalog.cs`) returning the new palette path.
3. Add one `AppThemeOptions` entry in `SettingsViewModel` (order: Dark, Light, Blue, then System) plus the option's preview brushes, and the preview `DataTrigger`s on the Appearance page.
4. The palette parity test enumerates every `Themes/*Theme.xaml`, so the new palette joins the key-set check automatically — no test edit is needed.
Switching the palette without a restart is not supported; a theme change takes effect on the next start.
