## Context

See `proposal.md` — Why. `AppPreferences` has `StartWithWindows` but nothing consumes it. `App.OnStartup` (`src/CLIHub/App.xaml.cs`) always calls `mainWindow.Show()` then registers the hotkey. The Settings window (`preferences-ui`) reads/writes preferences through `SettingsViewModel` and applies process-wide changes through `IPreferenceApplier`. Windows autostart for a per-user app is conventionally a value under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (no admin required).

## Goals / Non-Goals

**Goals:**
- Enable/disable Windows autostart from Settings and keep the toggle in sync with the actual registration.
- A single `ShowWindowOnStartup` preference that decides whether the main window is shown at startup, for both manual and autostart launches.

**Non-Goals:**
- Task Scheduler / Startup-folder shortcuts / delayed start (Run key only).
- Per-machine (HKLM) registration or elevation.
- Starting a specific project or minimizing to a different UI on autostart.

## Decisions

- **Registry, not a shortcut**: write `CLIHub` = `"<exe>"` (quoted) under `HKCU\...\CurrentVersion\Run`. The executable path comes from `Environment.ProcessPath` (fallback `Process.GetCurrentProcess().MainModule.FileName`). Alternative considered: a Startup-folder shortcut — rejected; the Run key is simpler and standard for a per-user tray app.
- **`IStartupService` in `CLIHub.Core`** with `bool IsEnabled()` and `bool SetEnabled(bool)`. It reads/writes through a thin seam so it is unit-testable without touching the real registry:
  - `IStartupRegistry` (internal): `string? GetValue(string name)`, `void SetValue(string name, string value)`, `void DeleteValue(string name)`.
  - `CurrentUserRegistryStartup` (default) uses `Microsoft.Win32.Registry.CurrentUser`.
  - Tests use an in-memory fake; `IsEnabled` treats a missing value as disabled, and registry errors are logged and reported as `false` (never thrown to the UI).
- **`AppPreferences.ShowWindowOnStartup`** (`bool`, default `true`) preserves current behavior. The window is only `Show()`n when true; otherwise the app runs from the tray. The tray icon, global hotkey (`EnsureHandle` creates the handle without showing), single-instance activation, and a subsequent manual launch all still surface the window.
- **Config is authoritative for the Run entry.** On startup, call `SetEnabled(prefs.StartWithWindows)` so a stale entry is removed and an outdated path (for example after a Velopack update changes the install directory) is refreshed.
- **Apply through `IPreferenceApplier`**: add `ApplyStartWithWindows(bool)` (delegating to `IStartupService`) and `ApplyStartupWindowVisibility` is not needed (only read at startup). `SettingsViewModel` loads the toggle from `IsEnabled()` and saves it to both the registry and `AppPreferences.StartWithWindows`.

## Risks / Trade-offs

- [Registry write denied (policy/AV)] → `SetEnabled` returns false, the view model shows a validation error and re-reads the actual state; the app keeps running.
- [Path has spaces / quoting] → always store the value quoted; test the formatting.
- [Running from build output registers the dev path] → acceptable; the entry is refreshed on each start from whatever executable is running.
- [User expected only autostart to open hidden] → the preference is documented as applying to any startup; a re-launch always raises the window so the app is never unreachable.
