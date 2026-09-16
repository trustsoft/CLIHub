# Design: add-settings-window

## Context

`SettingsStore` (Core) is read-only today: it loads `settings.json` once into a
cached document and exposes getters; the popup, `AgentDetector`, and
`UpdateService` share one instance from the composition root. `UpdateService`
gates its only check on `checkOnStartup`, and `HotkeyManager` registers once at
startup (a fresh manager is created per registration call, but nothing
unregisters at runtime). The popup window demonstrates the established UI
pattern: thin code-behind, MVVM-lite view model, UI services (folder picker,
confirm, dispatcher) injected as delegates. See proposal.md for motivation.

## Goals / Non-Goals

**Goals:**

- A save path for settings that keeps the shared in-memory document and the
  file consistent, with validation testable in Core.
- Runtime hotkey re-registration with a defined conflict story.
- A manual update check with inline, user-facing feedback, separate from the
  silent startup check.
- A settings window that follows the existing MVVM-lite patterns and stays
  extensible for the follow-up projects section.

**Non-Goals:**

- Projects editing UI (follow-up change; the layout only reserves room).
- Live validation per keystroke; validation runs on save.
- Inline "is this combination free" probing during hotkey capture (save-time
  check only, for v1).

## Decisions

### D1. Single-page window layout

One scrolling page: Runtime (combo cmd/ps/wt), Hotkey (capture control), Probe
(TTL + timeout), Update (check-on-startup toggle + «Проверить обновления» +
status text), footer buttons «Сохранить» / «Отмена». Tabs or sidebar nav pay
off only when the projects section arrives; a single page tolerates its
addition. *Alternative considered:* tabs from day one — rejected as ceremony
for five small sections.

### D2. Save path in `SettingsStore`, not in the window

`SettingsStore.Save(SettingsDocument)` (or an equivalent mutate delegate)
validates, replaces the cached `_document` in memory, and writes the file
atomically through the existing `JsonDocumentStore<T>`. Validation (positive
probe numbers, hotkey parses via `HotkeyParser`) returns typed errors that the
view model surfaces inline.

*Why:* every consumer (popup, detector, updater) already holds this instance;
updating the cached document is what makes "applies immediately" true without
any eventing. *Alternative considered:* window writes the file and calls
`Reload()` on the shared store — rejected: a stale read window between write
and reload, and two writers to one file.

### D3. Hotkey capture control

A focused control (WPF `PreviewKeyDown`) that accumulates
`Keyboard.Modifiers` + `Key` and renders the canonical string in the
`HotkeyParser` notation (`Ctrl+Alt+Space`). Modifier-only presses are ignored;
Esc ends the capture and is marked handled so it does not close the window;
other keys while not capturing behave normally. The control only ever produces
parser-valid strings, but the save path still validates (D2) as a safety net.

*Alternative considered:* free-text field + parse on save — rejected: users
must know the notation, and the notation is an internal format.

### D4. Save-time hotkey re-registration orchestrated by the composition root

`HotkeyManager` gains `Unregister()` and keeps a single instance for the app
lifetime (composition root stops creating a new manager per registration). On
save, the view model asks the app (via delegate) to:

1. unregister the current combination;
2. try-register the new one; on success, save;
3. on failure (`ERROR_HOTKEY_ALREADY_REGISTERED` or parse failure), re-register
   the previous combination, save the other fields, and report the conflict in
   the window.

*Why save-time and not capture-time probing:* probing would need a temporary
second registration (extra id/hwnd machinery) and still cannot guarantee the
save-time outcome; v1 keeps one check. *Risk:* a short span with no hotkey
registered during the switch — acceptable for an explicit user action.

### D5. `UpdateService.CheckNowAsync` with a result outcome

The check → download → `UpdateReady` pipeline is extracted and shared. The
startup path keeps its gates (`IsInstalled`, `EffectiveCheckOnStartup`) and
silence. `CheckNowAsync` skips only the `checkOnStartup` gate and returns an
outcome (`UpToDate`, `Ready`, `Failed`, `NotInstalled`) instead of a bool; the
`UpdateReady` event still drives the tray item, so both paths feed the same
ready state. An in-flight flag guards concurrent startup + manual checks. The
window's status text binds to the returned outcome, and also reflects the
ready state if `UpdateReady` fired earlier.

### D6. Window lifecycle: recreate per open

The window uses `Close()` (not the popup's `Hide()` pattern); the composition
root keeps the reference, and the tray item activates the live window or
creates a new one — construction loads current values from the store, which is
the load-on-open flow. `ShutdownMode.OnExplicitShutdown` plus tray ownership
of the app lifetime make closing safe. No unsaved-changes confirmation in v1;
«Отмена» and Close both discard.

*Why not warm singleton like the popup:* the popup is hotkey-summoned and
latency-sensitive; the settings window opens from a menu, where construction
cost is irrelevant and fresh state is a feature.

### D7. Apply semantics come for free — except the hotkey

Runtime resolution runs per launch (`RuntimeResolver.Resolve` at each agent
start), probe tuning is read per round — after D2 both observe saved values
with no extra code. Only the hotkey needs an explicit action (D4);
`checkOnStartup` is read at startup by design.

## Risks / Trade-offs

- [Shared `SettingsStore` document replaced mid-read by the popup thread]
  → save happens on the UI dispatcher, readers run on the UI thread or copy
  values per use; document replacement is a single reference swap.
- [User switches hotkey and the new one is taken; confusion about what was
  saved] → the window reports the conflict explicitly and keeps showing the
  effective (previous) hotkey after reload of the field.
- [Manual check overlapping the startup check] → in-flight guard in
  `UpdateService`; the button is disabled while a check runs.
- [Capture control fights with window-level Esc handling] → capture control
  marks Esc handled first; covered by a manual UI check during apply.
- [Velopack `UpdateReady` fires while the window is open] → status text
  subscribes to the outcome/readiness rather than only the button result.

## Migration Plan

Additive change: no existing settings move or change format; `settings.json`
schema is untouched. Rollback = revert the change. The only observable
migration concern is the tray menu gaining an item, covered by the `tray-menu`
delta.

## Open Questions

None — remaining choices (control visuals, exact wording) are implementation
details settled during apply.
