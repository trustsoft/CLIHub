## 1. Parser generalization

- [x] 1.1 In `src/CLIHub.Core/Hotkeys/HotkeyParser.cs`, replace the Space-only special case with a single shared named-key lookup covering canonical names (`Enter`, `Tab`, `Escape`, `Backspace`, `Delete`, `Insert`, `Home`, `End`, `PageUp`, `PageDown`, `Up`, `Down`, `Left`, `Right`, `Space`) and aliases (`Return`, `Esc`, `Back`, `Del`, `Ins`, `PgUp`, `PgDn`), and use it for both `TryGetVirtualKey` and `VirtualKeyName`. Verify with `dotnet build CLIHub.sln`.
- [x] 1.2 In `src/CLIHub/Windows/SettingsWindow.xaml.cs`, update the unsupported-key error message so it mentions named keys alongside letters, digits, and F-keys. Verify with `dotnet build CLIHub.sln`.

## 2. Tests

- [x] 2.1 In `tests/CLIHub.Tests/Hotkeys/HotkeyParserTests.cs`, add parse cases for each canonical named key and each alias, asserting the expected virtual key; verify with `dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj`.
- [x] 2.2 Add format and round-trip cases asserting named keys are written using their canonical name and re-parse to the same definition, and that alias input formats to the canonical name; verify with `dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj`.

## 3. Verification

- [x] 3.1 Run `dotnet build CLIHub.sln` and `dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj`, then in the running app open Settings, capture `Ctrl+Enter` on its own, save, and confirm it is accepted and registered. Repeat separately with `Ctrl+Right`.
