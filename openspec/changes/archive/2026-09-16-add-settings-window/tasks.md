# Tasks: add-settings-window

## 1. Core — settings save path

- [x] 1.1 Add `Save` to `SettingsStore` that validates (positive probe TTL/timeout, hotkey parses via `HotkeyParser`), replaces the cached in-memory document, and writes `settings.json` atomically via `JsonDocumentStore<T>`; return typed validation errors. Verify with new `SettingsStoreTests` cases: valid save round-trips, invalid probe values rejected, invalid hotkey rejected, cached document updated after save (getter returns new value without reload).
- [x] 1.2 Add a save-consumer test proving services observe saved values: after `Save` with a new runtime, `SettingsStore.Runtime` (as passed to `RuntimeResolver.Resolve`) returns the new value. Verify in `tests/CLIHub.Core.Tests`.

## 2. Core — manual update check

- [x] 2.1 Extract the check → download → `UpdateReady` pipeline in `UpdateService` and add `CheckNowAsync` returning an outcome (`UpToDate`/`Ready`/`Failed`/`NotInstalled`), gated only on `IsInstalled`, not on `checkOnStartup`; keep startup gates and silence unchanged; add an in-flight guard. Verify with new `UpdateServiceTests` cases for all four outcomes, the bypass (check runs with `checkOnStartup=false`), and the concurrent-call guard.

## 3. App — hotkey infrastructure

- [x] 3.1 Add `Unregister()` to `HotkeyManager` and switch the composition root to a single app-lifetime manager instance (no new manager per registration). Verify with `dotnet build CLIHub.sln` and a manual check that the popup hotkey still works after startup.

## 4. App — settings window

- [x] 4.1 Create `SettingsWindow` (single-page layout: Runtime combo, Hotkey capture, Probe fields, Update toggle + check button + status text, «Сохранить»/«Отмена») and `SettingsViewModel` following the popup's MVVM-lite pattern (UI services as delegates, dispatcher post). Verify the window constructs and binds by opening it manually from a temporary hook.
- [x] 4.2 Implement the hotkey capture control: `PreviewKeyDown` builds the canonical `HotkeyParser`-notation string; modifier-only presses ignored; Esc ends capture and is marked handled so the window stays open. Verify by manual capture of `Ctrl+Alt+K`, bare `Ctrl`, and `Esc`.
- [x] 4.3 Wire save: view model loads current values on construction, validates via the Core save path, surfaces errors inline; on success calls the save delegate. Wire hotkey re-registration in `App.xaml.cs`: unregister old → try new → on failure re-register old, save other fields, report conflict in the window. Verify manually: save runtime change → next agent launch uses it; save taken combination → other fields saved, old hotkey still works, conflict reported.
- [x] 4.4 Wire the manual update check button to `CheckNowAsync`: disable while running, status text per outcome (`UpToDate`/`Ready`/`Failed`/`NotInstalled`), status also reflects an already-ready update from the startup check. Verify manually with the button (dev build shows the not-installed message).
- [x] 4.5 Window lifecycle: `Close()` discards edits without confirmation; composition root keeps the reference; tray item activates the live window or creates a new one (fresh load from store). Verify manually: open → edit → close → reopen shows stored values; double tray click does not open two windows.

## 5. Tray menu

- [x] 5.1 Add the «Настройки» item to the tray menu (opens the settings window) alongside exit and the conditional update item. Verify manually: item opens/activates the window; menu dismissal behavior unchanged.

## 6. Verification and docs

- [x] 6.1 Run the full suite `dotnet test CLIHub.sln` and `dotnet build CLIHub.sln`; all existing and new tests pass.
- [x] 6.2 Update `docs/ui.md`: settings-window section from "будет добавлено" to the implemented composition (tray entry point, sections, save flow, check-updates button); note projects section and popup-footer entry as planned follow-ups. Verify the doc snapshot section reflects reality.
