## Why

The Settings mockup (`ui/mockups/settings.png`) illustrates a captured hotkey of `Ctrl+Alt+Space`, but the hotkey parser only recognizes letters, digits, and `F1`–`F24`. A Space combination is therefore rejected, so users cannot configure the key the design shows.

## What Changes

- Accept `Space` as a recognized key token when parsing a hotkey string (for example `Ctrl+Alt+Space`), mapped to Win32 virtual key `0x20`.
- Display the Space key as `Space` when a combination is formatted back to text (capture field and `config.json`).
- Everything else about hotkey parsing and validation is unchanged.

## Capabilities

### New Capabilities
<!-- None. -->

### Modified Capabilities
- `hotkey-support`: adds support for the Space key as a hotkey key (a new concern; existing modifier/key validation is unchanged).

## Impact

- `src/CLIHub.Core/Hotkeys/HotkeyParser.cs` — recognize the `Space` token and format `0x20` back to `Space`.
- `tests/CLIHub.Tests/Hotkeys/HotkeyParserTests.cs` — `Win+Space` moves from the invalid set to the valid set; add Space parse/format cases.
- No change to `SettingsWindow` capture logic: `KeyInterop.VirtualKeyFromKey(Key.Space)` already yields `0x20`.
