# Tasks: Default hotkey becomes Ctrl+Shift+Space

## 1. Defaults

- [x] 1.1 Change `HotkeyParser.Default` to `Control | Shift` and virtual key `0x20`, updating its XML comment.
- [x] 1.2 Change the `AppPreferences.Hotkey` default and its example comment.
- [x] 1.3 Change the `PreferencesDocument.Hotkey` default used for stored documents that omit the property.
- [x] 1.4 Make the invalid-configuration warning report `HotkeyParser.Default` instead of a hard-coded text.

## 2. Settings

- [x] 2.1 List `Space` among the accepted named keys in the Settings validation message.

## 3. Tests

- [x] 3.1 Update `Format_Default_IsCanonical` to assert `Ctrl+Shift+Space` and virtual key `0x20`.
- [x] 3.2 Add a configuration test proving that an omitted `preferences.hotkey` reads as `Ctrl+Shift+Space`.
- [x] 3.3 Add a model test proving that a new `AppConfig` uses `Ctrl+Shift+Space`.

## 4. Documentation

- [x] 4.1 Update the README feature line and Basic Use step.
- [x] 4.2 Update the glossary entry and the configuration schema documentation.
- [x] 4.3 Regenerate `docs/images/banner.png` from the updated HTML source.

## 5. Verification

- [x] 5.1 Run `dotnet build CLIHub.sln`.
- [x] 5.2 Run `dotnet test CLIHub.sln`.
- [x] 5.3 Run `openspec validate --all`.
- [x] 5.4 Confirm a fresh profile registers `Ctrl+Shift+Space` (log reports `vk 0x20`).
