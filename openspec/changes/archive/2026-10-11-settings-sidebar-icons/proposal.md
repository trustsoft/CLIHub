## Why

The target Settings design gives every sidebar page a leading icon, which makes the navigation scannable and matches the Windows 11 Fluent shell. The current sidebar is a plain text list. The target also renames the hotkeys page to "Hotkeys & Quick Launcher". Both are low-risk navigation-presentation changes that move the window toward the target.

## What Changes

- Add a **leading icon** to each sidebar page item (General & Startup, Hotkeys & Quick Launcher, Projects & Paths, CLI Agents, Terminal Profiles, Appearance, Telemetry & Logs, Updates). Icons only; no trailing badges.
- Rename the **Hotkeys & Launchers** page to **Hotkeys & Quick Launcher** in the sidebar and its page header.
- No other page labels change.

## Capabilities

### New Capabilities
<!-- None: this change restyles the existing sidebar and renames one page. -->

### Modified Capabilities
- `settings-theme`: each sidebar item shows a leading icon in the shared dark palette.
- `preferences-ui`: the hotkeys page label is "Hotkeys & Quick Launcher".

## Impact

- `src/CLIHub/ViewModels/SettingsViewModel.cs` (`SettingsPage` gains an icon; `AllPages` supplies icons)
- `src/CLIHub/Views/SettingsWindow.xaml` (sidebar item template shows the icon)
- `src/CLIHub/Themes/SettingsStyles.xaml` (sidebar item layout with an icon column)
- `src/CLIHub/Themes/IconGlyphs.cs` (page icons)
- `src/CLIHub/Views/SettingsHotkeysPage.xaml` (page header text)
- `tests/CLIHub.Tests` (navigation tests)
- No `config.json` change; no `CLIHub.Core` change.

## Non-goals

- Trailing badges (hotkey hint, `Latest`), the search field, and the profile card.
- Renaming any other page.
