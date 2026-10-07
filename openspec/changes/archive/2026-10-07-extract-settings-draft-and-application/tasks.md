## 1. Typed Draft And Application Service

- [x] 1.1 Add typed `SettingsInput`, `SettingsDraft`, save result, and settings application service; verify loading and validation cover hotkey, blank/default probe values, positive numbers, runtime, path style, and all boolean preferences.
- [x] 1.2 Implement ordered system application, single persistence update, and best-effort rollback of prior runtime/path/hotkey/startup state; verify sequence, failure, and rollback tests without WPF windows.

## 2. ViewModel Integration

- [x] 2.1 Delegate Settings load/save to the application service while preserving the existing public binding properties, validation messages, Cancel behavior, and close events; verify direct ViewModel tests for Cancel, valid Save, invalid draft, and failed application.
- [x] 2.2 Register the application service in DI and verify Settings composition resolves without introducing a second configuration owner.

## 3. Verification

- [x] 3.1 Run `dotnet build CLIHub.sln -c Release` and verify the solution builds without warnings or errors.
- [x] 3.2 Run `dotnet test CLIHub.sln -c Release` and verify all existing and new tests pass.
- [x] 3.3 Run `graphify update .`, validate the change, update `improvements.md`, and archive the completed change.
