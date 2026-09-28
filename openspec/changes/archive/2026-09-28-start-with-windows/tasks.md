## 1. Core startup service

- [x] 1.1 Add `ShowWindowOnStartup` (default `true`) to `AppPreferences`. Verify with `dotnet build CLIHub.sln`.
- [x] 1.2 Add `IStartupService` (`IsEnabled`, `SetEnabled`) and `StartupService` in `CLIHub.Core` that read/write the `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` value `CLIHub` (quoted executable path), through an `IStartupRegistry` seam; register it in `AddClIHubCoreServices`. Verify with `dotnet build CLIHub.sln`.
- [x] 1.3 Add `StartupServiceTests` using an in-memory registry fake: enable/disable, `IsEnabled` reflects missing value, and the stored value quotes the path. Verify with `dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj`.

## 2. Startup window visibility and reconciliation

- [x] 2.1 In `App.OnStartup`, show the main window only when `ShowWindowOnStartup` is enabled (otherwise run in the tray), and call `IStartupService.SetEnabled(preferences.StartWithWindows)` to refresh/remove the Run entry. Verify the app starts hidden with the setting off and that the tray/hotkey/second launch still show the window.

## 3. Settings window

- [x] 3.1 Add `ApplyStartWithWindows(bool)` to `IPreferenceApplier`/`PreferenceApplier` (delegating to `IStartupService`). Verify with `dotnet build CLIHub.sln`.
- [x] 3.2 Add the two toggles to `SettingsViewModel` (load "Start with Windows" from `IStartupService.IsEnabled()` and "Show window on startup" from config; save persists both, applies autostart, and reports a failure via the validation error) and add the "Startup" section to `SettingsWindow.xaml`. Verify with `dotnet build CLIHub.sln` and unit tests for load/save.

## 4. Docs and verification

- [x] 4.1 Update docs (`README.md`, `docs/architecture.md`) for the Startup settings and `IStartupService`.
- [x] 4.2 Run `dotnet build CLIHub.sln` and `dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj`, then verify manually: enabling "Start with Windows" creates the Run entry, disabling removes it, and "Show window on startup" controls window visibility on the next start.
