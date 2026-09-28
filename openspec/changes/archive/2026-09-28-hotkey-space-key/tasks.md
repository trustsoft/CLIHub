## 1. Parser support

- [x] 1.1 In `src/CLIHub.Core/Hotkeys/HotkeyParser.cs`, recognize the `space` token in `TryGetVirtualKey` (virtual key `0x20`) and format `0x20` back to `Space` in `VirtualKeyName`. Verify with `dotnet build CLIHub.sln`.
- [x] 1.2 In `tests/CLIHub.Tests/Hotkeys/HotkeyParserTests.cs`, move `Win+Space` from the invalid cases to the valid set and add parse/format round-trip cases for `Ctrl+Alt+Space`. Verify with `dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj`.

## 2. Verification

- [x] 2.1 Run `dotnet build CLIHub.sln` and `dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj`, then in the running app open Settings and capture `Ctrl+Alt+Space`, save, and confirm the combination is accepted and registered.
