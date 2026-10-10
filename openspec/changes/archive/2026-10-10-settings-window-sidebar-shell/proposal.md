## Why

The Settings window is a single scrolling page. Upcoming work adds many settings (project scanning, agent management, terminal profiles, appearance, logging, updates) that do not fit a flat list. A sidebar-navigated shell gives each area a stable home and lets later changes land as self-contained, independently reviewable slices.

## What Changes

- Replace the single-page Settings content with a sidebar-navigated shell: three groups and eight pages.
  - **General**: General & Startup, Hotkeys & Launchers
  - **Engines & Repos**: Projects & Paths, CLI Agents, Terminal Profiles
  - **System**: Appearance, Telemetry & Logs, Updates
- Re-home existing preferences onto their pages **without changing their behavior**:
  - Start with Windows / Show window on startup → General & Startup
  - Global hotkey → Hotkeys & Launchers
  - Default runtime (`cmd` / `ps` / `wt`) → Terminal Profiles
  - Agents probe (TTL / timeout) → CLI Agents
  - Path display style → Appearance
  - Check for updates (toggle + manual check) → Updates
- Pages whose settings are not yet implemented SHALL render a neutral empty state.
- Preserve Save/Cancel semantics, single-window behavior, the version footer, and the dark drawn chrome.
- No preference is added, removed, or renamed by this change. This change is **non-breaking**.

## Capabilities

### New Capabilities
<!-- None: this change only restructures the existing Settings window. -->

### Modified Capabilities
- `preferences-ui`: The Settings window SHALL present a sidebar-navigated set of pages instead of a flat section list, opening on a default page and showing an empty state for pages without implemented settings; existing settings are re-homed to pages without behavior change.
- `settings-theme`: The Settings window SHALL render a sidebar navigation region (group labels, page items, active-page highlight) and page headers in the shared dark visual language.

## Impact

- `src/CLIHub/Views/SettingsWindow.xaml`, `src/CLIHub/Views/SettingsWindow.xaml.cs`
- `src/CLIHub/ViewModels/SettingsViewModel.cs` (navigation state, selected page)
- New page views/view models under `src/CLIHub/Views/` and `src/CLIHub/ViewModels/`
- `src/CLIHub/Themes/SettingsStyles.xaml` (sidebar navigation styles)
- `tests/CLIHub.Tests` (Settings navigation tests)
- No `config.json` schema change; no `CLIHub.Core` change.

## Non-goals

- Adding the new settings themselves (Projects & Paths, CLI Agents management, Terminal Profiles host list, Appearance theme, Logging, Updates) — those land in later changes per `openspec/plans/2026-10-10-settings-window-roadmap.md`.
