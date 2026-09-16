# Proposal: add-settings-window

## Why

All global preferences (`settings.json`) are currently editable only by hand in a
text editor: there is no UI for changing the runtime, the hotkey, probe tuning,
or the update check, and no way to run an update check on demand. The popup is a
launcher, not a manager (it hides on focus loss), so it cannot host these flows.
`docs/vision.md` fixes the decisions for a settings window; `docs/ui.md` defers
its composition to "before implementation" — that point is now.

## What Changes

- Add a **settings window** (WPF, MVVM-lite) reachable from the tray menu:
  - sections for `runtime`, `hotkey`, `probe`, `update` (settings.json);
  - hotkey edited via a press-to-capture control producing the canonical string;
  - flow load-on-open → edit → single «Сохранить»; validation (TTL/timeout > 0,
    hotkey parses);
  - on save: hotkey is re-registered at runtime; if the new combination is taken
    by another application, other fields are saved, the hotkey is rolled back
    and a warning is shown;
  - a «Проверить обновления» action with inline feedback (checking / up-to-date /
    ready — install via tray / error), independent of `update.checkOnStartup`.
- Add a **Save path to `SettingsStore`** in Core that updates the shared
  in-memory document as well as the file, so the popup and background services
  observe saved values immediately.
- Add a **manual check method to `UpdateService`** that bypasses the
  `checkOnStartup` gate used by the startup check.
- Add a **«Настройки» item to the tray menu**.
- **Out of scope (follow-up change):** the projects section (project CRUD,
  rename, logo override UI). Decided during exploration: the settings window
  ships first; project editing lands in the same window as a second step.
- **Out of scope:** a settings entry point in the popup footer (the footer
  itself is only partially implemented; tray menu is the only entry point in
  this change).

## Capabilities

### New Capabilities

- `settings-window`: the settings window surface — entry point, single-instance
  lifecycle, sections and their editing controls, validation, and the save
  flow with its apply semantics. (The manual update-check behavior is
  specified under `app-update`; the window merely hosts its action.)

### Modified Capabilities

- `tray-menu`: the menu-items requirement — the menu gains a «Настройки» item
  that opens the settings window (previously: exit item only, plus the
  conditional update item).
- `global-hotkey`: the hotkey can now be changed at runtime — re-registration
  on save, with rollback to the previous combination when the new one cannot be
  registered (previously: registration happens once at startup).
- `app-update`: a new manual-check behavior — a user-initiated check runs
  regardless of `checkOnStartup` and reports its result inline in the settings
  window instead of silently (startup checks keep their silent semantics).

## Impact

- **Core (`src/CLIHub.Core`)**: `SettingsStore` gains a validated save path
  (in-memory document + atomic file write); `UpdateService` gains
  `CheckNowAsync` (shared check/download pipeline, no `checkOnStartup` gate).
  Both covered by xUnit tests in `tests/CLIHub.Core.Tests`.
- **App (`src/CLIHub.App`)**: new `SettingsWindow` (+ `SettingsViewModel`),
  hotkey capture control, tray menu item, wiring in the composition root
  (`App.xaml.cs`); `HotkeyManager` gains unregister/re-register support.
- **Specs**: new `settings-window`; deltas for `tray-menu`, `global-hotkey`,
  `app-update`.
- **Docs**: `docs/ui.md` settings-window section updated during apply; projects
  section stays listed as planned (follow-up change).
