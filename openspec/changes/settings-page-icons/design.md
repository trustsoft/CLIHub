# Design: Settings page icons

## Verified current state

- `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:41-116` — `SettingsPageCatalog.AllPagesData`
  holds eleven `SettingsPage` records in sidebar display order; each record carries a `Key`, `Title`,
  `Description`, `Group`, and `Icon` (a Segoe MDL2 glyph constant).
- `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:54` — `hotkeys` page: `IconGlyphs.KeyboardShortcut`
- `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:60` — `launch-window` page: `IconGlyphs.OpenInNewWindow`
- `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:66` — `projects-registry` page: `IconGlyphs.CheckList`
- `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:73` — `projects` page: `IconGlyphs.SyncFolder`
- `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:88` — `agent-detection` page: `IconGlyphs.Process`
- `src/CLIHub/Themes/IconGlyphs.cs:21` — `KeyboardShortcut = "\uEDA7"`
- `src/CLIHub/Themes/IconGlyphs.cs:25` — `Process = "\uE9F3"`
- `src/CLIHub/Themes/IconGlyphs.cs:29` — `OpenInNewWindow = "\uE8A7"`
- `src/CLIHub/Themes/IconGlyphs.cs:37` — `SyncFolder = "\uE8F7"`
- `src/CLIHub/Themes/IconGlyphs.cs:49` — `CheckList = "\uE9D5"`
- `src/CLIHub/Views/SettingsWindow.xaml:367` — sidebar item template binds `Text="{Binding Icon}"` to
  the page record's `Icon` property.
- `src/CLIHub/Views/SettingsWindow.xaml:430` — page header binds `Text="{Binding SelectedPage.Icon}"` to
  the selected page's `Icon` property.
- `openspec/specs/preferences-ui/spec.md:715-807` — "Agent Detection page content" requirement: the
  scenario "Icon and description" (lines 757-760) specifies that the sidebar item shows the Diagnostic
  icon (`IconGlyphs.Diagnostic`) and the page description "Caching and timeout behavior for agent
  detection."
- The XAML icon bindings read the catalog's `Icon` property; no XAML edit is required to reflect a
  catalog swap.

## Decisions

### Agent Detection page icon: Process

The Agent Detection page sidebar item and page header now use the Process icon (`IconGlyphs.Process`,
glyph `\uE9F3`). This icon depicts a gear with a circular motion arrow, which better represents the
detection process and caching behavior than the Diagnostic icon's bar-chart metaphor. The spec
scenario "Icon and description" updates from `IconGlyphs.Diagnostic` to `IconGlyphs.Process` to match
the approved UI.

**Rationale**: The Process icon emphasizes the active detection and caching workflow, while Diagnostic
suggests analysis or measurement, which is not the primary metaphor for this page. The user reviewed
the UI and approved Process.

### Other icon swaps

- **Global Hotkey page**: `KeyboardShortcut` (keyboard with overlaid shortcut keys) replaces `Keyboard`
  (generic keyboard) to emphasize the hotkey-specific function.
- **Launch Window page**: `OpenInNewWindow` (window with outward arrow) replaces `Equalizer` (horizontal
  sliders) to directly represent the launch window concept.
- **Projects (registry) page**: `CheckList` (list with checkmarks) replaces `BulletedList` (plain list)
  to convey the curated/registered status of project entries.
- **Project Scanning page**: `SyncFolder` (folder with circular sync arrows) replaces `FolderOpen`
  (generic open folder) to emphasize the background scanning and watching behavior.

All five swaps preserve the page key, title, description, group, and XAML bindings; only the glyph
constant changes.

## Alternatives considered and rejected

- **Keep Diagnostic for Agent Detection.** Rejected: the user explicitly approved the Process icon for
  the Agent Detection page, and it better matches the page's purpose (detection and caching, not
  analysis).
- **Leave the scenario unpinned (no spec delta).** Rejected: the scenario explicitly names
  `IconGlyphs.Diagnostic`, so the spec is now incorrect; updating it to `IconGlyphs.Process` reconciles
  the spec with the approved implementation.
- **Add a configuration option for icon themes.** Out of scope: this change is purely presentational and
  does not require configuration.

## Open questions

None.
