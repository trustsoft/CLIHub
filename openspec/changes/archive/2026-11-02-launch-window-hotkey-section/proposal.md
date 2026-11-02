## Why

The Global Hotkey Settings page contains a single section — the hotkey capture field. Having one standalone page for one control creates unnecessary navigation depth and separates the hotkey from other activation-related settings. The Launch Window page is the natural home for this, since the global hotkey's purpose is to activate the launch window.

## What Changes

- Move the `ACTIVATION` section header and the global hotkey capture card from the dedicated "Global Hotkey" page onto the "Launch Window" page as its first block
- Delete the now-empty "Global Hotkey" Settings page and remove it from the sidebar navigation
- The Settings sidebar will show ten pages instead of eleven; "Launch Window" will be the first page under "General" (immediately after "General & Startup")
- Update the "Launch Window" page description to mention that it now owns the global hotkey setting

## Capabilities

### Modified Capabilities

- `preferences-ui`: the global hotkey capture field moves from the "Global Hotkey" page to "Launch Window" as its first (`ACTIVATION`) section; the "Global Hotkey" page is removed from the sidebar, reducing the total from eleven pages to ten

## Impact

**Modified code:**
- `src/CLIHub/Views/SettingsLaunchWindowPage.xaml` — prepend the `ACTIVATION` section header and the hotkey capture card
- `src/CLIHub/Views/SettingsLaunchWindowPage.xaml.cs` — gain the hotkey capture handlers (`HotkeyField_MouseLeftButtonDown`, `HotkeyField_PreviewKeyDown`) and helpers (`IsModifierKey`, `ToHotkeyModifiers`) verbatim from the deleted page
- `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs` — delete the `hotkeys` catalog entry; update the `launch-window` description to reflect that it now includes the global hotkey
- `src/CLIHub/Views/SettingsWindow.xaml` — remove the `<views:SettingsHotkeysPage>` line

**Deleted code:**
- `src/CLIHub/Views/SettingsHotkeysPage.xaml` — the entire XAML file (36 lines)
- `src/CLIHub/Views/SettingsHotkeysPage.xaml.cs` — the entire code-behind file (101 lines)

**Modified tests:**
- `tests/CLIHub.Tests/ViewModels/Settings/SettingsNavigationTests.cs` — update the page count assertion from 11 to 10, remove `"hotkeys"` from the key order assertion, remove the `"hotkeys"` title assertion line, and remove the `("hotkeys", null, null, null)` tuple row

**No breaking changes:** The hotkey capture behavior, validation, SettingsViewModel public members, configuration persistence, and Save/Cancel semantics are unchanged. The control moves location but retains its logic, UI structure, and data binding.

## Non-Goals

- No changes to hotkey capture logic, validation messages, defaults, or configuration keys
- No changes to SettingsViewModel's public API or hotkey-related commands
- No visual restyling of the hotkey capture field or its chips
- No changes to the Save/Cancel workflow
- No README, CHANGELOG, or RELEASE-NOTES updates (consistent with recent UI-only commits)
- No changes to ReadmeImageCapture tooling

## Dependencies

None. This is a standalone UI reorganization.
