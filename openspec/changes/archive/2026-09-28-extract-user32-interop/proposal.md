## Why

The global-hotkey service embeds Win32 P/Invoke declarations and the `WM_HOTKEY` message constant directly inside UI-service logic. This mixes native interop with application behavior, and the constant conflicts with the repository's `.editorconfig` rule that requires PascalCase private constants. Isolating the native surface keeps the service focused and makes the interop audit-safe.

## What Changes

- Add a dedicated interop type `src/CLIHub/Interop/User32.cs` (`internal static partial class User32`) holding the `user32.dll` declarations for `RegisterHotKey`/`UnregisterHotKey` and the `WM_HOTKEY` message constant.
- Replace the legacy `[DllImport]` declarations with source-generated `[LibraryImport]` (partial methods, `[return: MarshalAs(UnmanagedType.Bool)]`).
- Update `GlobalHotkeyService` to call `User32.*`; keep only the app-specific `HotkeyId` constant.
- Record the new interop location in the repository/architecture docs.
- No behavior change: hotkey registration, activation, and unregistration stay identical.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a pure refactor with no spec-level behavior change, so the change opts out of specs (`skip_specs: true` in `.openspec.yaml`).

## Impact

- Code: `src/CLIHub/Hotkeys/GlobalHotkeyService.cs`; new `src/CLIHub/Interop/User32.cs`.
- Docs: `docs/architecture.md`, `docs/repo-structure.md`.
- Build: no new package dependencies; `[LibraryImport]` is provided by the .NET 8 SDK and requires `partial` declarations. No public API changes; `CLIHub.Core` is unaffected and remains platform-independent.
