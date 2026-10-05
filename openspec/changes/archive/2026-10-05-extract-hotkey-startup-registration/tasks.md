## 1. Add Hotkey Startup Boundary

- [x] 1.1 Add `IGlobalHotkeyService`, implement it with `GlobalHotkeyService`, and add `IHotkeyStartupRegistrar` with valid parsing, invalid-value warning, default fallback, and one registration call; verify valid and invalid inputs with focused tests.
- [x] 1.2 Register the interface alias and startup registrar in WPF composition, then replace the inline `App.OnStartup` hotkey workflow while preserving the concrete service field for shutdown; verify the alias resolves to the existing singleton.

## 2. Verify Preserved Behavior

- [x] 2.1 Verify that `HotkeyParser`, `GlobalHotkeyService`, `PreferenceApplier`, and hotkey disposal behavior remain unchanged.
- [x] 2.2 Run `dotnet build CLIHub.sln -c Release` and `dotnet test CLIHub.sln -c Release`; verify the full solution remains green.
- [x] 2.3 Run `openspec validate extract-hotkey-startup-registration` and verify the change has no spec deltas.
