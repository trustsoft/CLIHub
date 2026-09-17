# Tasks — English UI Strings

## 1. App UI strings

- [x] 1.1 Translate the popup window tooltips (run, agents-actions placeholder, open data folder, settings, exit) in `PopupWindow.xaml`; verify build succeeds and tooltips show English (manual run)
- [x] 1.2 Translate the settings window strings (title, section labels, field labels, hints, checkbox, Save/Cancel/Check-for-updates buttons) in `SettingsWindow.xaml`; verify the open window shows English labels (manual run)
- [x] 1.3 Translate the hotkey capture box placeholder/validation hints (`HotkeyCaptureBox.cs`) and the folder dialog title (`PopupWindow.xaml.cs`); verify capture flow still shows hints in English (manual run)
- [x] 1.4 Translate tray-menu items, the update notification, the plugin-warnings dialog title, and hotkey registration errors in `App.xaml.cs` and `Interop/HotkeyManager.cs`; verify the tray menu reads English (manual run)

## 2. View model and core messages

- [x] 2.1 Translate `PopupViewModel` status/confirm strings and `AgentItemViewModel` fallback name; verify add/remove/run statuses read English (manual run)
- [x] 2.2 Translate `SettingsViewModel` status and validation strings; verify save/check statuses read English (manual run)
- [x] 2.3 Translate the user-facing core messages in `LauncherCore`, `ProjectRegistry`, `PluginLoader`, `SettingsStore`, `HotkeyParser`; verify `dotnet build CLIHub.sln` succeeds

## 3. Specs, tests, docs

- [x] 3.1 Update the `SettingsStoreTests` assertion that matches a Russian validation message; verify `dotnet test CLIHub.sln` passes
- [x] 3.2 Update the Russian label references in `docs/ui.md` (Save/Cancel, settings window title)
- [x] 3.3 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`; both must pass
