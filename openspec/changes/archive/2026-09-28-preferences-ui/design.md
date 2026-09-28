## Context

See `proposal.md` — Why. Preferences live in `AppPreferences` (camelCase JSON) loaded/saved by `ConfigService`; `App` wires services and owns the hotkey and startup update check; `TrayIconController` builds the tray menu. `ProcessLauncher` currently ignores the runtime and always starts `wt.exe` with `-d "<dir>" cmd /k <command>`; `IProcessLauncher.SetTerminalExecutable` is never called. `AgentDetectionService` does uncached file checks; `AgentVersionService` caches versions indefinitely and `CaptureOutputAsync` hardcodes a 10 s timeout. There is no Settings window and no `ViewModels/` usage yet.

## Goals / Non-Goals

**Goals:**
- A Settings window matching `ui/mockups/settings.png` that reads and writes preferences and applies changes without restart.
- Wire the previously-dead runtime preference and add probe TTL/timeout and a startup-update toggle.

**Non-Goals:**
- Downloading/applying updates ("install from tray") — still deferred to a packaging change.
- Editing log level, filters, or start-with-Windows here (not in the mockup).
- Migrating `MainWindow` to MVVM (only the new window is MVVM).

## Decisions

- **New preferences in `AppPreferences`** (all with defaults, forward-compatible):
  - `DefaultRuntime` (string: `wt` | `cmd` | `ps`), default `wt`. The legacy `terminalExecutable` key is left in place for compatibility but is no longer authoritative; on load, a legacy value naming a known shell maps to the runtime, otherwise `wt`.
  - `AgentProbeTtlMinutes` (`int?`), null → default **15**.
  - `AgentProbeTimeoutSeconds` (`int?`), null → default **10** (preserves the current hardcoded probe timeout; the mockup's `3` is illustrative).
  - `CheckForUpdatesOnStartup` (`bool`), default `true` (preserves today's always-check-at-startup behavior).
- **Runtime via `IProcessLauncher`**: replace `SetTerminalExecutable`/`GetTerminalExecutable` with `SetRuntime(RuntimeKind)`/`GetRuntime()` (`RuntimeKind` enum in Core). Argument building per runtime:
  - WindowsTerminal: `FileName=wt.exe`, args `-d "{dir}" cmd /k {commandLine}`
  - CommandPrompt: `FileName=cmd.exe`, args `/k "{commandLine}"`, `WorkingDirectory=dir`
  - PowerShell: `FileName=powershell.exe`, args `-NoExit -Command "{commandLine}"`, `WorkingDirectory=dir`
  - `{commandLine}` = `executable` + escaped `arguments`. Alternative considered: keep a free-form executable path — rejected because the mockup calls for a fixed 3-way choice and a path field was never wired.
- **Probe caching/timeout**: inject the current preferences into `AgentDetectionService`/`AgentVersionService` (read at call time, so Settings changes apply live). Detection caches results per `(plugin, scope, project)` with the TTL; version cache entries store a timestamp and expire on the TTL. Add an optional `TimeSpan? timeout` to `IProcessLauncher.CaptureOutputAsync` and pass the configured timeout from `AgentVersionService`. `Invalidate()`/Refresh still clear caches.
- **Hotkey re-registration**: add `ReRegister(HotkeyDefinition)` to `GlobalHotkeyService` (unregister then register). If the new combination fails to register (in use), log a warning and restore the previous combination so the user is not left without a hotkey.
- **Applying changes from MVVM**: add a small UI-layer `IPreferenceApplier` (`ApplyRuntime`, `ApplyHotkey`, `ApplyStartupUpdateCheck`) implemented in `App`, injected into `SettingsViewModel` along with `IConfigService`, `IUpdateService`, and `IPluginManager` (for nothing yet) — keeps the view model free of WPF/Win32 types. Alternative considered: calling `GlobalHotkeyService`/`ProcessLauncher` directly from the view model — rejected to keep Core/UI boundaries clean.
- **Window & lifecycle**: `SettingsWindow` (XAML) + `SettingsViewModel`; opened from a new tray **Settings** menu item; singleton (bring-to-front if open). Save → validate → persist → apply → close. Cancel/close → discard.
- **Hotkey capture control**: a small WPF control/behavior that records modifier+key presses; Esc cancels; validation reuses `HotkeyParser` rules (≥1 modifier, exactly one recognized key). The stored string uses the existing `Ctrl+Shift+A` format.

## Risks / Trade-offs

- [Runtime argument quoting differs across cmd/PowerShell/wt] → unit-test `BuildTerminalArguments` per runtime with quotes in the agent arguments; keep the existing `cmd /k` behavior for the default `wt`.
- [Re-registering a hotkey can fail when the new combo is taken] → restore the previous combination and surface a warning; the saved config keeps the user's choice so it applies on next start.
- [Caching detection results can hide newly added project indicators] → modest default TTL (15 min) and Refresh still bypasses the cache.
- [Changing `IProcessLauncher` signature breaks `FakeProcessLauncher`/tests] → update the fake and `ProcessLauncherTests` in the same change.
- [New preferences must not break existing `config.json`] → missing fields deserialize to defaults; legacy `terminalExecutable` mapped, not deleted.
