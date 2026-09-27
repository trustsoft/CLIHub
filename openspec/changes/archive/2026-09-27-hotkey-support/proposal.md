## Why

CLIHub is a tray companion, but the only way to reach its window is the tray icon. Power users expect a global shortcut to summon (and dismiss) the launcher without leaving the keyboard. This change adds a configurable global hotkey (Ctrl+Shift+A by default) that works from any application.

## What Changes

- Register a global hotkey with Windows at startup (`RegisterHotKey`), defaulting to `Ctrl+Shift+A`
- Toggle the CLIHub window when the hotkey is pressed: show/activate it if hidden, hide it if visible
- Parse a human-readable hotkey string (`Ctrl+Shift+A`) from `AppPreferences.Hotkey` into modifiers + key
- Validate that a configured hotkey includes at least one modifier; fall back to the default otherwise
- Handle registration conflicts gracefully (another app owns the combo) by logging and continuing without a hotkey
- Unregister the hotkey on shutdown

## Capabilities

### New Capabilities

- `hotkey-support`: global keyboard shortcut registration, parsing/validation, activation handling, and clean release

### Modified Capabilities

<!-- None: no existing main-spec requirement changes. -->

## Impact

**New code:**
- `src/CLIHub.Core/Hotkeys/HotkeyParser.cs` — parse/validate `Ctrl+Shift+A`-style strings into a `HotkeyDefinition` (modifiers + key); pure, testable, no Win32
- `src/CLIHub/Hotkeys/GlobalHotkeyService.cs` — Win32 `RegisterHotKey`/`UnregisterHotKey` + `HwndSource` hook for `WM_HOTKEY`

**Modified code:**
- `src/CLIHub/App.xaml.cs` — register the hotkey after the window exists; dispose on exit
- `src/CLIHub/TrayIconController.cs` — expose a `ToggleMainWindow()` used by both the hotkey and the tray click
- `src/CLIHub/ServiceRegistration.cs` — register the hotkey service

**Data:**
- `%APPDATA%\CLIHub\config.json` — `AppPreferences.Hotkey` (already exists, default `"Ctrl+Shift+A"`) is now consumed

**No breaking changes.**
