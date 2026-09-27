## Context

See `proposal.md` - Why. Today `src/CLIHub/Hotkeys/GlobalHotkeyService.cs` declares two `[DllImport]` methods and the `WM_HOTKEY` private constant inline. Constraints that shape the approach:

- `AGENTS.md`: `CLIHub.Core` is platform-independent (no WPF/Win32), so all native interop must live in the `CLIHub` WPF project.
- `src/.editorconfig`: `private_constants_rule` requires PascalCase private constants, and `EnforceCodeStyleInBuild=true` turns violations into build warnings, so `WM_HOTKEY` cannot stay a private constant.
- `.NET 8` project; `[LibraryImport]` source generation is available without extra packages.

## Goals / Non-Goals

**Goals:**

- Isolate all `user32.dll` interop in one `internal` type under `src/CLIHub/Interop/`.
- Use `[LibraryImport]` source-generated marshalling instead of `[DllImport]`.
- Preserve observable behavior exactly (no spec change).
- Resolve the `WM_HOTKEY` naming conflict while keeping the canonical Win32 symbol name.

**Non-Goals:**

- Extracting managed Windows-specific code (named mutex/pipe in `SingleInstanceGuard`, which lives in `CLIHub.Core` and uses no P/Invoke).
- Creating a general native-interop framework or covering other Win32 APIs.
- Adding tests for native calls (no test project references the WPF app).

## Decisions

- **Location `src/CLIHub/Interop/`** rather than `CLIHub.Core`: `AGENTS.md` requires Core to be platform-independent; interop belongs to the WPF app.
- **Type `User32`** (grouped per native library) instead of a generic `NativeMethods`: scopes declarations to the owning DLL and scales to a second library without ambiguity.
- **Visibility `internal static partial class`**: interop is an implementation detail. Because the `.editorconfig` `private_constants_rule` targets `applicable_accessibilities = private`, an `internal const int WM_HOTKEY` is not governed by the PascalCase rule, so the canonical Win32 name is preserved. `HotkeyId` stays `private` in `GlobalHotkeyService` (app-specific, not a Win32 symbol).
- **`[LibraryImport]` over `[DllImport]`**: source-generated marshalling, trimming/AOT-friendly. Requires `partial` class and methods and `[return: MarshalAs(UnmanagedType.Bool)]` for `bool` returns. Alternative considered: keep `[DllImport]` (simpler, but legacy runtime marshalling and a `SYSLIB1054` suggestion).
- **`AllowUnsafeBlocks` enabled in the WPF project**: the `[LibraryImport]` generator emits `unsafe` marshalling code and fails with `SYSLIB1062`/`CS0227` otherwise, so `src/CLIHub/CLIHub.csproj` sets `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`. Scoped to `CLIHub`; `CLIHub.Core` remains without unsafe code.
- **Signatures preserved**: `RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk)`, `UnregisterHotKey(IntPtr hWnd, int id)`, both with `SetLastError = true`.

## Risks / Trade-offs

- [`LibraryImport` generator requirements] -> mark the class and methods `partial`; the build verifies code generation succeeded.
- [`internal` hides interop from future tests] -> acceptable; the WPF app has no test project. Add `InternalsVisibleTo` only if tests are introduced later.
- [BOOL marshalling regression] -> `[return: MarshalAs(UnmanagedType.Bool)]` matches the Win32 `BOOL` return; verify by running the app and confirming registration logs and hotkey toggle.
- [Documentation drift] -> update `docs/architecture.md` and `docs/repo-structure.md` within the same change.

## Migration Plan

Single-commit pure refactor; rollback is a revert. No config or data migration.
