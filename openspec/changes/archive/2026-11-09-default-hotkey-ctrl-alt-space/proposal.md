# Change: Default hotkey becomes Ctrl+Alt+Space

## Why

`Ctrl+Shift+Space` is a common shortcut in Windows and many applications (e.g., IntelliSense in Visual Studio, input method switching). `Ctrl+Alt+Space` reduces conflict potential with existing shortcuts while remaining easy to press and semantically appropriate for a launcher. The parser and registration infrastructure already support `Alt` as a modifier with the `Space` key, so no logic changes are required.

## What Changes

- Change the built-in default hotkey from `Ctrl+Shift+Space` to `Ctrl+Alt+Space` (virtual key `VK_SPACE`, `0x20`).
- Keep the invalid-configuration fallback, the persisted default, and the startup registration default aligned on the same combination.
- Leave explicitly configured hotkeys untouched: no migration rewrites an existing `preferences.hotkey` value.
- Update the README, glossary, configuration documentation, the README screenshot harness mock config value, and the `hotkey-support` specification.
- Update test expectations that assert the old default.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `hotkey-support`: the default hotkey referenced by "Global hotkey registration", "Default hotkey", and "Invalid hotkey falls back to default" changes to `Ctrl+Alt+Space`.

## Impact

- `src/CLIHub.Core/Hotkeys/HotkeyParser.cs` — `Default` becomes `Control | Alt` plus `0x20`, with XML comment updated.
- `src/CLIHub.Core/Models/Configuration/AppConfig.cs` — `AppPreferences.Hotkey` default and example comment.
- `src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs` — `PreferencesDocument.Hotkey` default used when a stored document omits the value.
- `tests/CLIHub.Core.Tests/Hotkeys/HotkeyParserTests.cs` — `Format_Default_IsCanonical` asserts `Ctrl+Alt+Space`.
- `tests/CLIHub.Core.Tests/Configuration/AppConfigSerializationTests.cs` — tests asserting the default hotkey value.
- Documentation: `README.md` (feature line and basic-use step), `docs/glossary.md`, `docs/architecture/configuration.md`, `docs/images/tools/Capture-ReadmeImages.ps1` (mock config value), and `openspec/specs/hotkey-support/spec.md`.
- Existing users who already stored a hotkey keep their value; only new or missing values use the new default.

## Non-goals

- Do not change the Settings UI copy beyond what verification shows is required. The Settings page does not hard-code the default combination in its validation message or labels.
- Do not migrate existing `preferences.hotkey` values. This follows the precedent set in `openspec/changes/archive/2026-10-17-default-hotkey-space/`.
- Do not change parser, capture, or registration logic. `Ctrl+Alt+Space` is already parseable and registerable.

## Dependencies

None. This change is self-contained.
