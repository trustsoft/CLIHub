## Why

The Hotkeys & Quick Launcher page labels the launcher-placement block "POPUP" and its selector "Popup screen position". "Popup" is internal shorthand: it does not say which window moves or that the setting is about positioning, and it conflicts with the monitor-oriented option labels. The option labels also mix naming patterns: "Primary screen" names a monitor without saying where the window lands, "Center of active monitor" leads with the placement, and "Top-center" omits the monitor entirely.

## What Changes

- Rename the page section header "POPUP" to "LAUNCHER POSITIONING".
- Rename the card title "Popup screen position" to "Launcher window position"; keep its hint "Where the launch window appears when it is shown.".
- Rename the monitor-targeting option labels to the "<monitor> — <placement>" template:
  - "Center of active monitor" becomes "Active monitor — centered".
  - "Top-center" becomes "Active monitor — top-center".
  - "Primary screen" becomes "Primary monitor — centered".
- Keep "Cursor-relative" unchanged, because it targets the pointer rather than a monitor.
- Keep the stored preference tokens, the `PopupScreenPosition` enum members, and every placement behavior unchanged, so existing `config.json` values keep working and no migration is needed.
- Keep the default selection on the same position (the active-monitor centered placement).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `preferences-ui`: The "Popup screen position" requirement changes the section header, the selector name, and the option label list to the launcher-window naming, and states that "Cursor-relative" keeps its pointer-relative wording.
- `configuration-snapshot`: The "Popup screen position defaults safely" requirement changes the label it names for the default position to "Active monitor — centered"; the default token and parsing behavior are unchanged.

## Impact

- `src/CLIHub/Views/SettingsHotkeysPage.xaml` — section header and card title text only.
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` — the `PopupPositionOptions` display labels only.
- `openspec/specs/preferences-ui/spec.md` and `openspec/specs/configuration-snapshot/spec.md` — selector, section, and option label text (applied on archive).
- No changes to `CLIHub.Core` models, `WindowPositioner`, persistence tokens, configuration schema, or tests.
