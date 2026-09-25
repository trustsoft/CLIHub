# design: popup-focus-pin

## Context

The popup is a WPF `PopupWindow` living as a warm singleton. Today
`OnDeactivated` hides the window unless `_suppressHide` is set (set around the
modal folder picker and confirm dialogs). `OnKeyDown` hides on Esc always.
There is an established pattern for conditionally suppressing the focus-loss
hide; this design extends the same mechanism with a user-controlled, persistent
per-session flag. See `proposal.md` - Why.

## Goals / Non-Goals

**Goals:**
- A visible pin toggle in the popup footer that suppresses hide-on-focus-loss.
- Default off; sticky for the application session; not persisted to disk.
- Esc and item actions keep hiding the popup regardless of the pin state.

**Non-Goals:**
- Persisting the pin to `settings.json`.
- Changing launch/action clicks, modal-dialog suppression, or hotkey show/activate
  behavior.
- Any change to `CLIHub.Core` or the test suite (UI code-behind is manually
  verified per project convention).

## Decisions

**1. Reuse the `_suppressHide` pattern with a sticky `_pinned` flag.**
`OnDeactivated` currently hides unless `_suppressHide`. Introduce a separate
`_pinned` field: `Hide()` when `!_suppressHide && !_pinned`. Keep the two flags
independent so a modal dialog still stays on top of a pinned popup without
fighting the pin state.
Alternative considered: collapsing pin into `_suppressHide` (a "sticky
suppress") — rejected because the dialog flows toggle `_suppressHide` around
their own scope and would clobber an unrelated user setting.

**2. Toggle is plain code-behind, not a ViewModel command.**
The pin is window-behavior state with no data semantics; the tray and hotkey
surfaces already follow this code-behind convention. A code-behind click handler
flips `_pinned` and switches the glyph. The footer's three system actions stay on
existing ViewModel commands.

**3. Button in the footer's right action area, reusing `PopupIconButton`.**
The popup has no title bar (headerless panes), so the footer is the only chrome
strip. Place the pin as the first button of the right action stack, before "Open
data folder". Glyphs: Segoe MDL2 `E718` (Pin) when disabled, `E196` (Unpin) when
enabled. A bound style/toggle makes the enabled state visually distinct. The
button remains keyboard-accessible (a `Button`, so tab/space work as usual).

**4. Sticky per-session semantics live in the flag, not in reset logic.**
`_pinned` is a plain field on the singleton window, so it naturally survives
hide/show cycles and hotkey re-opens for the whole session (decision A in
exploration). No reset in `ShowForHotkey`. Being a field, it also resets on
application restart by construction — satisfying "default off".

**5. Esc and pin are orthogonal.**
`OnKeyDown` keeps hiding unconditionally (`Hide()`), leaving `_pinned` intact.
A hidden pinned popup re-appears already pinned when the hotkey shows it again
(scenario "Pin persists across shows").

## Risks / Trade-offs

- A pinned popup is `Topmost` and stays above a window the user switched to; it
  intentionally floats while pinned. → Expected launcher behavior; Esc or the
  hotkey are the exits. Visual animation (if any) is out of scope.
- `OnDeactivated` fires for the folder picker/confirm flows; `_suppressHide`
  already covers them and the new flag does not interfere because the flags are
  independent. → Covered by the "modal dialogs still hide-guard" cases in
  `PickFolder`/`Confirm`.
- The glyph differs by state but the toggle has no label; tooltip should change
  with state ("Pin" / "Unpin") to avoid ambiguity. → Set `ToolTip` in the toggle
  handler.

## Migration Plan

N/A — additive UI behavior on an existing window; no data layout or API change.
Rollback is trivial (revert the toggle handling).

## Open Questions

None deferred.