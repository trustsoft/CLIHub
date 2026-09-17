# English UI Strings

## Why

The product's user-visible text is Russian while the product targets an
English-facing surface (the approved mockups use English labels: `PROJECTS`,
`AGENTS`, `Actions`). Tooltips, section/field labels, menu items, dialogs,
status messages and validation errors must read in English.

## What Changes

- Translate all user-visible strings to English:
  - popup window tooltips (run, agents actions placeholder, open data folder,
    settings, exit);
  - settings window title, section labels, field labels, hints, checkbox,
    buttons (Save / Cancel / Check for updates), and its status/validation
    messages;
  - hotkey capture placeholder and validation hints;
  - tray menu items (Settings, Exit, Install update), the update notification
    and the plugin-warnings dialog title;
  - core user-facing messages surfaced in the UI: launcher errors/warnings,
    project-registry errors, plugin-loader warnings, settings validation
    errors, hotkey parser errors.
- Update the settings-window spec references to the translated labels
  («Сохранить» → «Save», «Настройки» → «Settings»).
- Update the test that asserts a Russian validation message.

No behavior changes: only the text shown to the user.

## Capabilities

### New Capabilities

- *(none)*

### Modified Capabilities

- `settings-window`: label references in "Save with validation" and "Render
  custom window chrome" change from the Russian captions to their English
  equivalents; no scenario changes.

## Impact

- `src/CLIHub.App/Views/PopupWindow.xaml`, `PopupWindow.xaml.cs`,
  `HotkeyCaptureBox.cs`, `SettingsWindow.xaml`
- `src/CLIHub.App/App.xaml.cs`, `ViewModels/PopupViewModel.cs`,
  `ViewModels/SettingsViewModel.cs`, `ViewModels/AgentItemViewModel.cs`,
  `Interop/HotkeyManager.cs`
- `src/CLIHub.Core/Services/{LauncherCore,ProjectRegistry,PluginLoader,SettingsStore,HotkeyParser}.cs`
- `tests/CLIHub.Core.Tests/SettingsStoreTests.cs` (one assertion)
- `docs/ui.md` label references
