# Change: Default hotkey becomes Ctrl+Shift+Space

## Why

`Ctrl+Shift+A` is a mnemonic for "agents", not for summoning the launcher, and it is a common shortcut in
editors and browsers. `Ctrl+Shift+Space` is easier to associate with the quick launcher, is already accepted
by the named-key parser, and matches the combination shown in the README banner.

## What Changes

- Change the built-in default hotkey from `Ctrl+Shift+A` to `Ctrl+Shift+Space` (virtual key `VK_SPACE`, `0x20`).
- Keep the invalid-configuration fallback, the persisted default, and the startup registration default aligned
  on the same combination.
- Leave explicitly configured hotkeys untouched: no migration rewrites an existing `preferences.hotkey` value.
- Mention `Space` among the accepted named keys in the Settings validation message.
- Update the README, glossary, configuration documentation, README banner, and the `hotkey-support` specification.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `hotkey-support`: the default hotkey referenced by "Global hotkey registration", "Default hotkey", and
  "Invalid hotkey falls back to default" changes to `Ctrl+Shift+Space`.

## Impact

- `src/CLIHub.Core/Hotkeys/HotkeyParser.cs` — `Default` becomes `Control | Shift` plus `0x20`.
- `src/CLIHub.Core/Models/Configuration/AppConfig.cs` — `AppPreferences.Hotkey` default.
- `src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs` — `PreferencesDocument.Hotkey` default used when a
  stored document omits the value.
- `src/CLIHub/Startup/HotkeyStartupRegistrar.cs` — invalid-value warning reports the actual default.
- `src/CLIHub/Views/SettingsHotkeysPage.xaml.cs` — validation hint lists `Space`.
- Tests: `HotkeyParserTests` and configuration serialization defaults.
- Documentation: `README.md`, `docs/glossary.md`, `docs/architecture/configuration.md`,
  `openspec/specs/hotkey-support/spec.md`, `docs/images/banner.png` (and its HTML source).
- Existing users who already stored a hotkey keep their value; only new or missing values use the new default.
