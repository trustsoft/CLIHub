## Why

The target Settings design renders each setting as a card — a bordered rounded row with the setting's title and hint on the left and its control inline on the right (wide controls stacked below) — grouped under section headers, under a page title and a short description. The current pages use a stack of muted field labels with controls beneath and hints below, which does not match the target. This change introduces the card layout on the simpler pages; the data-heavy pages (Projects & Paths, CLI Agents) follow in a later change.

## What Changes

- Add a reusable **`SettingCard`** control (a `ContentControl`) with the parameters `Title`, `Hint`, `Icon`, `Badge`, `BadgeTone`, `Placement`, and `Grouped`, so every setting row shares one card layout and supports the target compositions: a leading icon, an inline status badge, a compact control on the right or a wide control stacked, and either a standalone card or a row inside a shared surface.
- Add a **`SettingsGroup`** container that renders several `Grouped` rows on one rounded bordered surface separated by hairlines (first row without a separator).
- Add a **`SectionHeader`** with an uppercase muted `Text` and an optional accent `ShowMarker`.
- Populate a **leading icon** for each setting row and render compact controls right-aligned (checkboxes, short segmented selectors, numeric fields) and wide/compound controls stacked (the hotkey field, popup-position/runtime/path/log-level selectors, the accent swatches, the retention/size fields, and the log-file row).
- Apply the layout to the **six simpler pages**: General & Startup, Hotkeys & Quick Launcher, Terminal Profiles, Appearance, Telemetry & Logs, Updates, each with a one-line **page description** and, where a page has more than one group, **section headers**.
- Controls, bindings, and Save/Cancel behavior are unchanged; this is a presentational re-layout.

## Capabilities

### New Capabilities
<!-- None: this change restyles the settings pages. -->

### Modified Capabilities
- `settings-theme`: settings are presented as cards with an inline control, page descriptions, and section headers, in the shared dark palette.

## Impact

- New `src/CLIHub/Views/SettingCard.cs` (control) and its style in `src/CLIHub/Themes/SettingsStyles.xaml`.
- `src/CLIHub/Views/SettingsGeneralPage.xaml`, `SettingsHotkeysPage.xaml`, `SettingsTerminalPage.xaml`, `SettingsAppearancePage.xaml`, `SettingsTelemetryPage.xaml`, `SettingsUpdatesPage.xaml`.
- `tests/CLIHub.Tests` (any view-model/navigation tests touched; mostly XAML-only).
- No `config.json` change; no `CLIHub.Core` change.

## Non-goals

- The data-heavy pages Projects & Paths and CLI Agents (a later change).
- Per-page "Apply Changes", the status footer, and search (other later phases).
- Changing any control's behavior or any stored preference.
