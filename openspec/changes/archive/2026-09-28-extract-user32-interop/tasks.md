## 1. Interop extraction

- [x] 1.1 Add `src/CLIHub/Interop/User32.cs` with `internal static partial class User32`, `internal const int WM_HOTKEY = 0x0312;`, and `[LibraryImport("user32.dll", SetLastError = true)]` partial declarations `RegisterHotKey`/`UnregisterHotKey` with `[return: MarshalAs(UnmanagedType.Bool)]`; verify `dotnet build CLIHub.sln` succeeds and the source generator reports no diagnostics.
- [x] 1.2 Update `src/CLIHub/Hotkeys/GlobalHotkeyService.cs` to call `User32.RegisterHotKey`, `User32.UnregisterHotKey`, and `User32.WM_HOTKEY`; remove the local `[DllImport]` methods and the local `WM_HOTKEY`; keep `private const int HotkeyId`; verify the project compiles.

## 2. Verification

- [x] 2.1 Run `dotnet build CLIHub.sln` and confirm 0 warnings / 0 errors with `EnforceCodeStyleInBuild=true` (no naming warning for the interop type).
- [x] 2.2 Run `dotnet test CLIHub.sln` and confirm all tests pass.
- [x] 2.3 Run the application and confirm the global hotkey (`Ctrl+Shift+A`) still toggles the window and startup logs report successful registration.

## 3. Documentation

- [x] 3.1 Update `docs/architecture.md` and `docs/repo-structure.md` to record `src/CLIHub/Interop/` and the `User32` interop type.
