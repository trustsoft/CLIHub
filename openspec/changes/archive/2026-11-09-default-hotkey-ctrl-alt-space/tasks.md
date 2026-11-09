# Tasks: Default hotkey becomes Ctrl+Alt+Space

## 1. Defaults

- [x] 1.1 Change `HotkeyParser.Default` to `Control | Alt` and virtual key `0x20`, updating its XML comment to say `Ctrl+Alt+Space`.
- [x] 1.2 Change the `AppPreferences.Hotkey` default to `"Ctrl+Alt+Space"` and update its example comment.
- [x] 1.3 Change the `PreferencesDocument.Hotkey` default to `"Ctrl+Alt+Space"`.

## 2. Tests

- [x] 2.1 Update `Format_Default_IsCanonical` in `HotkeyParserTests.cs` to assert `"Ctrl+Alt+Space"` and verify virtual key `0x20`.
- [x] 2.2 Update the two assertions in `AppConfigSerializationTests.cs` that expect `"Ctrl+Shift+Space"` to assert `"Ctrl+Alt+Space"` instead.

## 3. Documentation

- [x] 3.1 Update `README.md` feature line (line 59) and Basic Use step (line 87) to reference `Ctrl+Alt+Space`.
- [x] 3.2 Update the glossary entry in `docs/glossary.md` (line 124) to reference `Ctrl+Alt+Space` as the default.
- [x] 3.3 Update the configuration schema documentation in `docs/architecture/configuration.md` (line 6) to reference `Ctrl+Alt+Space`.
- [x] 3.4 Update the mock configuration value in `docs/images/tools/Capture-ReadmeImages.ps1` (line 417) to `'Ctrl+Alt+Space'`.

## 4. Verification

- [x] 4.1 Run `dotnet build CLIHub.sln` and confirm no errors.
- [x] 4.2 Run `dotnet test CLIHub.sln` and confirm all tests pass.
- [x] 4.3 Run `npx -y @fission-ai/openspec@latest validate --all` from the repository root and confirm 0 errors.
- [x] 4.4 Confirm a fresh profile (delete `%APPDATA%\CLIHub\config.json`) registers `Ctrl+Alt+Space` (log reports `vk 0x20` with `Control | Alt`).
- [x] 4.5 Confirm an existing profile with a stored hotkey keeps its value (the migration does not rewrite existing configs).

## 5. Finalize

- [x] 5.1 Run `graphify update .` to refresh the repository knowledge graph after code changes.
