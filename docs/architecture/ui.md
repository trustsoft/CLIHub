# UI Architecture and Theming

Architecture overview: [../architecture.md](../architecture.md). This document covers resource organization and
theming.

## Resource Organization
- Application icon: `src/CLIHub/app.ico` (embedded; also the exe icon)
- Default logo: `src/CLIHub/default-logo.png` (copied to output)
- Desktop app icon sources: `assets/` at the repository root (not part of the build)
- Embedded seed descriptors/logos: `src/CLIHub.Core/SeedPlugins/`

## XAML Themes and Styles Recipe

Layering, in merge order: `Common/Sizing.xaml` (metrics, font and radius tokens) → `Common/Controls.xaml` (shared keyed styles; merges `Sizing.xaml` — palette brushes use `DynamicResource` and resolve from the application-level palette) → `Common/PickerData.xaml` (theme/accent picker preview brushes and option styles; merges `Sizing.xaml`, because it uses only tokens) and `Common/SettingsControls.xaml` (shared input/control templates; merges `Sizing.xaml`, because it uses only tokens) → `Common/SettingsStyles.xaml` (settings window/page chrome and custom-control implicit styles; merges `Controls.xaml`, `Common/PickerData.xaml`, then `Common/SettingsControls.xaml`) → `Common/ActionsMenuStyles.xaml` (the shared Actions-menu popover, entry, and header-toggle styles; merges `Controls.xaml`, and is merged both by `SettingsStyles.xaml` at application level — so Settings page `UserControl`s reach it — and by `LaunchWindow.xaml` window-scoped) and per-window dictionaries (`Common/LaunchWindowStyles.xaml`, `Common/WhatsNewStyles.xaml`), which merge `Controls.xaml` only. App.xaml hosts the app-level pieces: the base palette (for window attributes), `Common/SlimScrollBar.xaml` (implicit `ScrollBar`), `Common/SettingsStyles.xaml`, the implicit `ToolTip` style, and the shared converters.

The application-level merge tree is therefore exactly:

```
App.xaml
├─ Dark.xaml                  base palette (Light/Blue merged on top by PaletteSwitcher)
├─ Common/SlimScrollBar.xaml  implicit ScrollBar style
└─ Common/SettingsStyles.xaml Settings chrome + SectionHeader/SettingCard/SettingsGroup implicit styles
   ├─ Common/Controls.xaml    shared keyed styles
   │  └─ Common/Sizing.xaml   Sizing.* tokens
   ├─ Common/PickerData.xaml  ThemePreview*Brush + ThemeOption*/AccentOption* styles
   │  └─ Common/Sizing.xaml
   ├─ Common/SettingsControls.xaml input/control templates + NumericField implicit style
   │  └─ Common/Sizing.xaml
   └─ Common/ActionsMenuStyles.xaml shared Actions-menu styles (also merged window-scoped by LaunchWindow.xaml)
      └─ Common/Controls.xaml
```

`SettingsStyles.xaml` is merged at application level because Settings pages are UserControls built before they attach to the window tree, so window-scoped resources are unreachable during their XAML load. With `App.xaml` merging only the palette, the scrollbar, and `SettingsStyles.xaml`, `Controls.xaml` is instantiated once at application level (previously three times). `Sizing.xaml` is instantiated once per dictionary that uses a token and cannot reach application-level siblings — `Controls`, `PickerData`, and `SettingsControls` each merge it — which is a few dozen immutable token objects and the price of WPF's `StaticResource` scoping. Every one of those merge lists must stay: a loose dictionary resolves `StaticResource` only against its own keys and its own merges, never against application-level siblings.

To add a window:
1. Create `Themes/Common/<Window>Styles.xaml` merging `/Themes/Common/Controls.xaml` (this brings the sizing tokens transitively — StaticResource inside a dictionary resolves only against that dictionary's own merged dictionaries; palette brushes resolve from the application-level palette through `DynamicResource`).
2. Define keyed styles only (no implicit styles — they would not reach popups anyway); build on the shared bases (`IconChipButton`, `QuietChipButton`, `WindowHeader`, `WindowTitle`) via `BasedOn`.
3. Merge it from the window's `Window.Resources`; reference colors exclusively through palette brushes, metrics through `Sizing.*`, glyphs through `IconGlyphs`.
4. Add tooltips as plain `ToolTip="..."` attributes — the app-level implicit style themes them automatically.

## SpacedPanel

Use `CLIHub.Controls.SpacedPanel` for simple vertical or horizontal groups where a uniform gap belongs
between visible siblings. Bind `Spacing` to the shared `Sizing.Spacing.Standard` resource when the same
rhythm is shared by more than one view. Keep outer margins, card padding, and optical one-off offsets on
the relevant element.

Keep `Grid` for pane layouts, list rows, star-sized columns, and any composition that depends on explicit
width relationships. Do not use `SpacedPanel` as an item panel for virtualized lists; it is a small
one-dimensional layout helper rather than a replacement for WPF's virtualizing panels.

## Settings Page Layout

The Settings window renders the active page's title, description, and hairline divider once in a fixed
header row of the content area. Before the title it shows the active page's own glyph — `SettingsPage.Icon`
in the icon font, as a plain static emblem in `TextMutedBrush` with no tile, border, or accent — so the
header carries the same section identity as its sidebar entry. A page may add a summary row above the
divider: on Project Scanning, the projects, agents, and watcher summary cards sit between the description
and the hairline; on Projects, the total, manual, and discovered summary cards use the same row; on CLI
Agents, the agents, default-agent, and updates summary cards use it; on Agent Detection, the cache TTL,
timeout, and defaults summary cards use it. The terms are defined in the
[glossary](../glossary.md#settings-ui). Only the
content below the divider is hosted by the vertical `ScrollViewer`, so the page header, summary row,
sidebar, drawn window chrome, validation message, and footer stay visible while long pages scroll. Page
views (`Settings*Page.xaml`) contain only their scrolling settings content; the title and description live
in the `SettingsPage` metadata exposed through `SelectedPage`.

## Footer Insets

The launch and Settings footers share their horizontal insets: the brand block starts 12 DIP from
the left edge and the action block ends 8 DIP from the right edge. Both windows read
`Sizing.Footer.BrandMargin` and `Sizing.Footer.ActionsMargin` from `Sizing.xaml`; do not reintroduce
literal margins. The Settings version chip adds `Sizing.Footer.VersionChipMargin`, which lowers it
2 DIP against the brand label for optical alignment.

## Adding a Settings Page

1. Add a `views:Settings<Name>Page` `UserControl` containing only its scrolling settings content,
   and declare it in the page host grid in `SettingsWindow.xaml`:
   `Visibility="{Binding SelectedPage.Key, Converter={StaticResource PageKeyVisibility}, ConverterParameter=<key>}"`.
2. Add the `SettingsPage` entry to the `AllPagesData` array exposed as `SettingsPageCatalog.AllPages` with the
   key, title, description, group, icon, and the optional `HeaderTitleExtraKey`, `HeaderActionKey`, and
   `SummaryRowKey`. The sidebar groups and the page order derive from that array.
3. If the page needs a header or summary slot, declare the `DataTemplate` in
   `SettingsWindow.Resources` and name it in the matching metadata property.
4. `tests/CLIHub.Tests/Views/SettingsPageMetadataTests.cs` fails when the two sides disagree: a page
   key missing from or duplicated in the page host, a slot key that names no declared template, or a
   host `ConverterParameter` with no metadata entry.
5. Smoke-test the page and its slots: navigate to it and confirm only that page is visible and its
   header title/actions and summary row appear only there.

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

The launch-window project row uses a fixed-width right marker rail inside the text column: the favorite
star occupies the name row and the provenance mark the path row. Both rail markers right-align to the
rail's trailing edge, so the project row's leading and trailing content insets are symmetric (12 DIP
each), and the rail stays fixed-width so hiding the star does not shift the path text. The name and path
share the `*` text column, and the path display converter measures the name text's width so the rail
never eats into the trimmed path. The `ProjectProvenanceMark` style lives in `Controls.xaml` (manual uses
`TextPrimaryBrush`, discovered `TextMutedBrush`), so the Settings registry and the launch window render
the same mark; the mark's tooltip and accessible name carry the full `Manual`/`Discovered` label.

The launch-window project list orders favorites first and then the remaining projects, preserving the
stored order within each group: `ProjectListOrdering.FavoritesFirst` is a pure Core policy applied by
`ProjectPaneController.Refresh()` before enrichment, so row identity and selection survive the reorder.
The Settings Projects registry keeps its own ordering (manual projects first, then name and path).

The registered palettes (`Dark.xaml`, `Light.xaml`, `Blue.xaml`) define the same brush keys and brushes are referenced with `DynamicResource`. The three files differ ONLY in colour values; their structure (comments, key order, whitespace) is identical and mechanically guarded by the parity test. `ThemeCatalog.ResolvePalettePath` resolves the palette for the effective theme, and `PaletteSwitcher` keeps it as one tracked overlay merged on top of the dark base: a non-null resolution replaces the tracked overlay in place (or appends it on first use), and a null resolution removes it so the dark base stays.

The accent is application-scoped and independent of the theme. The palettes declare chrome and surface brushes only — neither `AccentBrush` nor `AccentSelectionBrush` is a palette key — and `AccentApplier` writes both brushes into `Application.Resources` for the startup call in `App` and the Settings-save call in `WpfAccentPreferenceTarget`. A theme switch therefore leaves the accent untouched, and an accent save never changes the palette. The stored accent is always applied, the default Sky included; an invalid or missing value normalizes back to Sky.

To add another theme:
1. Copy `Dark.xaml` to a new `<Name>.xaml` and preserve the exact file structure and replace only the Color attribute values (the parity test enforces this).
2. Add the `AppTheme` member and its token in Core, then one `ThemeDescriptors.All` entry (`src/CLIHub/Theming/ThemeDescriptors.cs`, order: Dark, Light, Blue, then System) holding the new palette path. `ThemeCatalog.ResolvePalettePath` is descriptor-driven, so no switch arm is needed.
3. Give the descriptor its preview brush keys (`ThemePreview*Brush` definitions live in `Themes/Common/PickerData.xaml`). `SettingsViewModel` derives `AppThemeOptions` from the descriptor and the Theme & Accent page binds the swatch brushes directly, so the page and its templates need no edit.
4. The palette parity test enumerates every `Themes/*.xaml` at the root of `Themes/` (non-recursive, so `Common/` is excluded), so the new palette joins the key-set check automatically — no test edit is needed.
A saved theme applies to every open window immediately through `PaletteSwitcher`; the stored value still applies on the next start, before any window is created. Both startup and a Settings save use the same switcher, so exactly one overlay exists at any time and open windows repaint through their `DynamicResource` references. While the stored theme is System, `SystemThemeFollower` also re-applies the palette through the same switcher when the Windows apps light/dark mode changes during the session; fixed themes ignore personalization changes.

## Theme Data Locations

| Data | Location |
|---|---|
| Theme identity and tokens | `src/CLIHub.Core/Models/Appearance/AppTheme.cs` (`AppTheme`, `AppThemes`; UI-independent) |
| Accent identity and presets | `src/CLIHub.Core/Models/Appearance/AccentPalette.cs` (`AccentPalette`: the Sky default and the preset names/hex values; UI-independent) |
| WPF descriptor registry | `src/CLIHub/Theming/ThemeDescriptors.cs` (`ThemeDescriptor`, `ThemeDescriptors.All` in picker order) |
| Palettes | `src/CLIHub/Themes/Dark.xaml`, `Light.xaml`, `Blue.xaml` (identical structure and key order, differing only in Color values) |
| Picker preview data | `src/CLIHub/Themes/Common/PickerData.xaml` (`ThemePreview*Brush` swatches and the picker option styles) |
| Runtime palette resolution | `src/CLIHub/Theming/ThemeCatalog.cs` (`ResolvePalettePath`, descriptor-driven; System resolves to Dark or Light from the Windows mode) and `src/CLIHub/Theming/PaletteSwitcher.cs` (`Shared.Apply` applies the resolved palette as the single tracked overlay; startup calls it directly and a Settings save reaches it through `src/CLIHub/Settings/WpfThemePreferenceTarget.cs`). While the stored theme is System, `src/CLIHub/Theming/SystemThemeFollower.cs` follows the Windows personalization setting for the running first-instance session and re-applies through the same switcher |
| Runtime accent application | `src/CLIHub/Settings/AccentApplier.cs` (writes `AccentBrush` and `AccentSelectionBrush` into `Application.Resources`, never `MergedDictionaries`; startup calls it directly and a Settings save reaches it through `src/CLIHub/Settings/WpfAccentPreferenceTarget.cs`) |

The static guards that keep those files coherent:

- `tests/CLIHub.Tests/Themes/ThemePaletteParityTests.cs` — palettes have identical structure (key order, comments, whitespace) differing only in Color values.
- `tests/CLIHub.Tests/Themes/ThemePreviewResourceKeyTests.cs` — preview keys referenced by name
  (descriptor values and XAML `StaticResource`) exist in the application-level tree, and no declared
  preview key is orphaned.
- `tests/CLIHub.Tests/Themes/ThemeColorLiteralTests.cs` — colour literals are rejected outside the
  palettes and `Themes/Common/PickerData.xaml`.
- `tests/CLIHub.Tests/Themes/ApplicationResourceKeyTests.cs` — palette keys are never referenced with
  `StaticResource`, and no `x:Key` is declared twice across the non-palette application-level merge
  tree.
- `tests/CLIHub.Tests/Settings/AccentApplierTests.cs` — the accent stays application-scoped and survives a
  palette swap unchanged; both the startup and the Settings-save paths apply the stored accent, the default
  included.
