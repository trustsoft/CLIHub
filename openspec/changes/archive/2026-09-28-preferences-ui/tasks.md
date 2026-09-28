## 1. Preferences model and runtime

- [x] 1.1 Add `DefaultRuntime`, `AgentProbeTtlMinutes`, `AgentProbeTimeoutSeconds`, `CheckForUpdatesOnStartup` to `AppPreferences` with the agreed defaults, and add a `RuntimeKind` enum (`WindowsTerminal`/`CommandPrompt`/`PowerShell`) plus runtime-name mapping in `CLIHub.Core`. Verify with `dotnet build CLIHub.sln` and a Core test for defaults/round-trip.
- [x] 1.2 Replace `SetTerminalExecutable`/`GetTerminalExecutable` on `IProcessLauncher` with `SetRuntime(RuntimeKind)`/`GetRuntime()`, and implement per-runtime argument building in `ProcessLauncher` (wt / cmd / ps) with quoting. Update `FakeProcessLauncher` and `ProcessLauncherTests`; verify per-runtime argument tests pass.
- [x] 1.3 Wire the runtime at startup: `App.OnStartup` reads `AppPreferences.DefaultRuntime` and calls `IProcessLauncher.SetRuntime`. Verify the app launches an agent in `wt`, `cmd`, and `ps` (manual).

## 2. Agents probe caching and timeout

- [x] 2.1 Inject current preferences into `AgentDetectionService` and cache results per `(plugin, scope, project)` with the configured TTL; add Core tests for cache hit, TTL expiry, and default TTL.
- [x] 2.2 Make `AgentVersionService` cache entries expire on the TTL and add an optional `TimeSpan? timeout` to `IProcessLauncher.CaptureOutputAsync`; pass the configured probe timeout. Update tests for TTL expiry, timeout termination, and default timeout.

## 3. Hotkey runtime re-registration

- [x] 3.1 Add `ReRegister(HotkeyDefinition)` to `GlobalHotkeyService` (unregister then register; on failure restore the previous combination and log a warning). Verify by registering, re-registering a different combination, and confirming the old one no longer triggers.

## 4. Settings window

- [x] 4.1 Add `IPreferenceApplier` (UI layer) with `ApplyRuntime`, `ApplyHotkey`, `ApplyStartupUpdateCheck`, and implement it in `App`. Verify `dotnet build CLIHub.sln`.
- [x] 4.2 Implement `SettingsViewModel` (load/populate, validation, Save/Cancel, manual update check) and register it + `SettingsWindow` in DI. Verify with `dotnet build CLIHub.sln` and unit tests for validation (hotkey invalid, probe field invalid/blank).
- [x] 4.3 Build `SettingsWindow.xaml` per `ui/mockups/settings.png`: the four sections, hotkey capture field with Esc-cancel, probe fields, updates toggle + check button + status, version pill, Save/Cancel; singleton, discard on close. Verify visually against the mockup.

## 5. Integration and startup gating

- [x] 5.1 Add a **Settings** item to the tray menu and open the window (bring-to-front if open). Verify from the running app.
- [x] 5.2 Gate the startup update check in `App.OnStartup` on `CheckForUpdatesOnStartup`. Verify the check is skipped in logs when disabled.

## 6. Docs, tests, verification

- [x] 6.1 Update docs (`README.md`, `docs/architecture.md`, `docs/vision.md`, `ui/mockups/README.md`) for the Settings window and new preferences.
- [x] 6.2 Run `dotnet build CLIHub.sln` and `dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj`, then manually verify in the running app: change runtime / hotkey / probe values / updates toggle, save, and confirm each takes effect without restart, and that Cancel discards.
