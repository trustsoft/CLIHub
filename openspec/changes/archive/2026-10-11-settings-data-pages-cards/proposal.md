## Why

`settings-page-cards` moved the six simpler settings pages to the target card layout (cards, page descriptions, section headers, leading icons). The two data-heavy pages — **Projects & Paths** and **CLI Agents** — still use the old stack of muted labels, so the Settings window is inconsistent. This change applies the same card vocabulary to those two pages, including their list editors, and aligns the custom per-agent cards to the same visual language.

## What Changes

- **Projects & Paths** gains a page description, a `REPOSITORIES` section, and a `BACKGROUND WATCHER` section; its settings become cards:
  - **Watched directories** and **Ignored directory patterns** become stacked cards carrying their list and add/remove controls.
  - **Repository scan depth** becomes a card with the numeric field.
  - **Repository discovery** becomes a stacked card with the segmented selector.
  - **Background watcher** becomes a card showing the read-only status.
- **CLI Agents** gains a page description, a `MANAGEMENT` section, and a `PROBE` section:
  - **Default agent** becomes a stacked card with a dropdown selector (a combo box listing "None" and the agents).
  - The **agent list** stays a list of custom per-agent cards, but their visuals are aligned to the card vocabulary (shared surface tokens, title typography, and a rounded logo block).
  - **Agents probe** becomes a stacked card with the TTL/timeout fields.
- Controls, bindings, and Save/Cancel behavior are unchanged; this is a presentational re-layout plus the visual alignment of the agent cards.

## Capabilities

### New Capabilities
<!-- None: this applies the existing card layout to the remaining pages. -->

### Modified Capabilities
- `settings-theme`: the Projects & Paths and CLI Agents pages present their settings as cards (including list editors and the agent list), with page descriptions and section headers, and the per-agent cards follow the shared card visual language.

## Impact

- `src/CLIHub/Views/SettingsProjectsPage.xaml`, `src/CLIHub/Views/SettingsAgentsPage.xaml`.
- `src/CLIHub/Themes/IconGlyphs.cs` (a couple of setting icons).
- `src/CLIHub/Themes/SettingsStyles.xaml` (only if a shared asset is needed for the agent card logo block).
- No new control primitives; reuses `SettingCard`/`SettingsGroup`/`SectionHeader`.
- No `config.json` change; no `CLIHub.Core` change.

## Non-goals

- Changing any control's behavior or any stored preference.
- Per-page "Apply Changes", the status footer, search, and the profile card (later phases).
