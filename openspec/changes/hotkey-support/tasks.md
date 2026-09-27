## 1. Core Parsing (testable, no Win32)

- [ ] 1.1 Add `HotkeyModifiers` flags enum with values pinned to Win32 (`Alt=1, Control=2, Shift=4, Win=8`) in `src/CLIHub.Core/Hotkeys/HotkeyModifiers.cs`, and verify a unit test asserts the numeric values
- [ ] 1.2 Add `HotkeyDefinition(HotkeyModifiers Modifiers, int VirtualKey)` record and `HotkeyParser.TryParse(string?, out HotkeyDefinition?)` handling `Ctrl`/`Control`, `Shift`, `Alt`, `Win`/`Windows`, and keys `A-Z`, `0-9`, `F1-F24` (case-insensitive), returning null for no-modifier or unknown-key input, and verify `CLIHub.Core` compiles
- [ ] 1.3 Add `HotkeyParser.ParseOrDefault(string?, HotkeyDefinition fallback)` returning the parsed definition or the fallback, and verify unit tests cover the default `Ctrl+Shift+A` and invalid input

## 2. Global Registration (UI)

- [ ] 2.1 Add `src/CLIHub/Hotkeys/GlobalHotkeyService.cs` obtaining the `HwndSource` from a `Window` via `WindowInteropHelper`, calling `RegisterHotKey`/`UnregisterHotKey`, hooking `WM_HOTKEY` (0x0312), and invoking a callback on match, and verify it compiles
- [ ] 2.2 Make `GlobalHotkeyService` return/log failure when `RegisterHotKey` fails and expose `IDisposable` to unregister, and verify no exception is thrown when the combo is already taken

## 3. Toggle Integration

- [ ] 3.1 Add `ToggleMainWindow()` to `TrayIconController` (hidden → show/normalize/activate; visible → hide) and refactor `ShowMainWindow()` to share the window-state logic, and verify it compiles
- [ ] 3.2 In `App.OnStartup`, read `AppPreferences.Hotkey` from the config service, parse with `HotkeyParser` (fallback default, logging a warning on invalid), register via `GlobalHotkeyService` against the main window, and wire the callback to `TrayIconController.ToggleMainWindow()`, and verify the app starts
- [ ] 3.3 Dispose the `GlobalHotkeyService` in `App.OnExit` and verify the hotkey is released after exit

## 4. Tests

- [ ] 4.1 Add `HotkeyParserTests` covering `Ctrl+Shift+A`, lowercase, reordered modifiers, `Alt+F4`, `Win+Space`-style inputs, missing modifier, unknown key, null, and empty
- [ ] 4.2 Verify `dotnet test` passes for the whole solution

## 5. Verification

- [ ] 5.1 Build the full solution with 0 warnings/errors
- [ ] 5.2 Manually verify: pressing `Ctrl+Shift+A` toggles the CLIHub window from within another application
- [ ] 5.3 Manually verify: with `preferences.hotkey` set to an invalid value, the app still starts and logs a fallback warning
- [ ] 5.4 Manually verify: exiting the app releases the hotkey so it can be used by another process
