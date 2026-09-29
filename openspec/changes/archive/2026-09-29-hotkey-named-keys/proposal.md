## Why

The hotkey parser recognizes only letters, digits, `F1`–`F24`, and `Space`. Common keys that users naturally reach for in a shortcut (Enter, Tab, Escape, Backspace, Delete, Insert, Home, End, PageUp/PageDown, and the arrow keys) are rejected, so they cannot be set from Settings or written into `config.json`. Space support was added one key at a time; this change generalizes the parser so the whole family of named keys is handled consistently.

## What Changes

- Accept additional named key tokens, case-insensitively, in a hotkey string (for example `Ctrl+Enter`, `Alt+Tab`, `Ctrl+Left`):
  - `Enter` (also `Return`) → `VK_RETURN` (0x0D)
  - `Tab` → `VK_TAB` (0x09)
  - `Escape` (also `Esc`) → `VK_ESCAPE` (0x1B)
  - `Backspace` (also `Back`) → `VK_BACK` (0x08)
  - `Delete` (also `Del`) → `VK_DELETE` (0x2E)
  - `Insert` (also `Ins`) → `VK_INSERT` (0x2D)
  - `Home` → `VK_HOME` (0x24)
  - `End` → `VK_END` (0x23)
  - `PageUp` (also `PgUp`) → `VK_PRIOR` (0x21)
  - `PageDown` (also `PgDn`) → `VK_NEXT` (0x22)
  - `Up` / `Down` / `Left` / `Right` → `VK_UP` (0x26) / `VK_DOWN` (0x28) / `VK_LEFT` (0x25) / `VK_RIGHT` (0x27)
- Format these virtual keys back to their canonical names (`Enter`, `Tab`, `Escape`, `Backspace`, `Delete`, `Insert`, `Home`, `End`, `PageUp`, `PageDown`, `Up`, `Down`, `Left`, `Right`) so the capture field and `config.json` stay human-readable and round-trip.
- Refactor the existing `Space` handling and the new key set into a single name↔virtual-key lookup used by both parsing and formatting.
- No change to modifier requirements or to any other key category. Punctuation/symbol keys remain out of scope.

## Capabilities

### New Capabilities
<!-- None. -->

### Modified Capabilities
- `hotkey-support`: replaces the Space-only key coverage with a general named-key set (Enter, Tab, Escape, Backspace, Delete, Insert, Home, End, PageUp/PageDown, arrows) that parses and formats consistently; existing modifier validation and registration behavior are unchanged.

## Impact

- `src/CLIHub.Core/Hotkeys/HotkeyParser.cs` — add named-key lookup for parsing and formatting; `Space` becomes one entry in the shared table.
- `src/CLIHub/Windows/SettingsWindow.xaml.cs` — the capture error message ("Use a letter, digit, or F1-F24...") is updated to mention named keys; `KeyInterop.VirtualKeyFromKey` already yields the correct virtual keys for these keys.
- `tests/CLIHub.Tests/Hotkeys/HotkeyParserTests.cs` — add parse/format/round-trip cases for the new named keys and their aliases.
