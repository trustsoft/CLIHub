## Context

See proposal.md - Why. `AppPreferences.Hotkey` already exists (default `"Ctrl+Shift+A"`) but is unused. The app now starts through a DI container (`app-lifecycle`), logs through Serilog (`logging`), and owns its window via `TrayIconController` (`TrayIconController.ShowMainWindow()`). The window hides instead of closing. Core must stay WPF-free, so Win32 hotkey registration belongs in the UI project while string parsing can live in Core.

## Goals / Non-Goals

**Goals:**
- A global hotkey that toggles the window from any application
- Configurable via `AppPreferences.Hotkey` with a safe default
- Testable parsing/validation with no Win32 or WPF dependency
- Graceful behavior when the combo is taken by another app
- Clean unregistration on exit

**Non-Goals:**
- A hotkey capture/recorder UI (users edit `config.json` for now)
- Multiple simultaneous hotkeys
- Changing the hotkey at runtime without restart
- Mouse gestures or tray-only accelerators

## Decisions

### Decision 1: Split parsing (Core) from registration (UI)

**Chosen:** `CLIHub.Core.Hotkeys.HotkeyParser` parses/validates strings into a `HotkeyDefinition`; `CLIHub.Hotkeys.GlobalHotkeyService` performs the Win32 registration.

**Rationale:**
- Parsing is pure logic and benefits from unit tests, so it belongs in Core
- `RegisterHotKey` needs a window handle (`HwndSource`), so it belongs in the UI project
- Keeps Core free of P/Invoke and WPF

**Alternatives considered:**
- Put everything in Core with a message-only Win32 window: Rejected — extra native window plumbing for no benefit here
- Put everything in the UI: Rejected — parsing would be untestable without a UI test host

### Decision 2: `HotkeyDefinition` carries modifiers + a virtual-key code

```csharp
[Flags]
public enum HotkeyModifiers { None = 0, Alt = 1, Control = 2, Shift = 4, Win = 8 }

public sealed record HotkeyDefinition(HotkeyModifiers Modifiers, int VirtualKey);
```

**Rationale:**
- Modifier values match Win32 `MOD_*` constants (Alt=1, Control=2, Shift=4, Win=8), so the UI passes them straight to `RegisterHotKey`
- A raw virtual-key code avoids depending on `System.Windows.Input.KeyInterop` in Core
- The parser maps key tokens itself: `A`–`Z` → `0x41`–`0x5A`, `0`–`9` → `0x30`–`0x39`, `F1`–`F24` → `0x70`–`0x87`

**Alternatives considered:**
- Store a key name string and let the UI map it: Rejected — splits one concept across layers
- Use `System.Windows.Forms.Keys`: Rejected — Windows Forms is banned in this project

### Decision 3: Parse rules

**Chosen:** Case-insensitive `+`-separated tokens. Recognized modifiers: `Ctrl`/`Control`, `Shift`, `Alt`, `Win`/`Windows`. The last non-modifier token is the key. A definition is valid only if it has at least one modifier and exactly one key.

```csharp
// examples
"Ctrl+Shift+A" -> (Control|Shift, 0x41)
"alt+F4"       -> (Alt, 0x73)
"A"            -> null   // no modifier
"Ctrl+Foo"     -> null   // unknown key
```

**Rationale:** Matches the spec's modifier requirement and the default value format; unknown inputs fall back to the default.

**Alternatives considered:**
- Reject order-dependent strings: Rejected — being lenient about token order is friendlier and harmless

### Decision 4: Registration via `HwndSource` hook on the main window

**Chosen:** `GlobalHotkeyService` obtains the `HwndSource` from the main window (`WindowInteropHelper.Handle`), calls `RegisterHotKey` with a private id, and watches for `WM_HOTKEY` (0x0312) in an added hook; it invokes a callback on the UI thread.

**Rationale:**
- The main window already exists at startup and keeps its handle while hidden, so messages arrive even when the window is not visible
- No extra native window or message loop needed
- The callback marshals naturally because the hook runs on the UI thread

**Alternatives considered:**
- A dedicated message-only window: Rejected — more moving parts than the existing window
- `LowLevelKeyboardHook`: Rejected — intrusive, can be throttled, and harder to reason about

### Decision 5: Toggle semantics

**Chosen:** The hotkey toggles: hidden → show/normalize/activate; visible → hide. Implemented by extending `TrayIconController` with `ToggleMainWindow()` and having `ShowMainWindow()` call into the same window-state logic.

**Rationale:** A toggle is the expected behavior for a launcher hotkey and reuses one code path for tray and hotkey activation.

**Alternatives considered:**
- Always show (never hide): Rejected — less useful for a tray companion

### Decision 6: Failure handling

**Chosen:** If `RegisterHotKey` returns false (combo taken), log a warning and continue with no hotkey. Parsing failures similarly log a warning and fall back to the default; if the default also fails to register, the app continues without a hotkey.

**Rationale:** A missing hotkey must never prevent the app from starting (spec: conflict handling).

**Alternatives considered:**
- Fail startup: Rejected — the tray icon still provides access

## Risks / Trade-offs

**[Risk] Another app owns `Ctrl+Shift+A`** → Mitigation: graceful degradation with a logged warning; the tray icon remains usable.

**[Risk] The window handle is not yet created when registration runs** → Mitigation: register after the window is shown during startup; `WindowInteropHelper.Handle` forces handle creation if needed.

**[Risk] Hidden window does not receive `WM_HOTKEY`** → Mitigation: the window is hidden, not destroyed, so its `HwndSource` stays alive and receives messages.

**[Risk] Modifier values drift from Win32 constants** → Mitigation: the enum values are pinned to `MOD_ALT/CONTROL/SHIFT/WIN` and covered by a unit test.

**[Trade-off] No hotkey recorder UI** → Benefit: no new UI surface. Cost: users edit `config.json`; a future change can add a capture control.

**[Trade-off] Hotkey read only at startup** → Benefit: simple. Cost: change requires restart (documented; matches `app-lifecycle` level behavior).

## Migration Plan

1. Add `CLIHub.Core.Hotkeys` (`HotkeyModifiers`, `HotkeyDefinition`, `HotkeyParser`)
2. Add `CLIHub.Hotkeys.GlobalHotkeyService` with Win32 interop and a `WM_HOTKEY` hook
3. Extend `TrayIconController` with `ToggleMainWindow()`
4. In `App.OnStartup`, parse `AppPreferences.Hotkey` (fallback default), register, and wire the toggle; dispose on exit
5. Tests: parser validity/fallbacks; modifier values match Win32 constants

Rollback: revert code; `config.json` is unaffected.

## Open Questions

- **Hotkey recorder UI?** Deferred; editing `config.json` is acceptable for now.
- **Multiple hotkeys (for example, a separate "launch last agent")?** Deferred; out of scope.
