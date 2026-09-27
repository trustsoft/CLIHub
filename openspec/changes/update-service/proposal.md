## Why

CLIHub has no way to learn that a newer build exists. Users must check the repository manually. Adding a Velopack-backed update check (with a tray notification and a visible app version) keeps installs current and gives users a clear manual path to check.

## What Changes

- Add an update-checking service backed by Velopack (`UpdateManager`)
- Check for updates asynchronously on startup without blocking the UI
- Notify via the tray when a newer version is available (including the new version number)
- Show the current application version in the window
- Provide a manual "Check for updates" action
- Handle gracefully when the app was not installed by Velopack (development/unpackaged) or when the check times out or fails

**Out of scope (deferred to a packaging change):** downloading and applying updates. This change only detects and reports.

## Capabilities

### New Capabilities

- `update-checking`: startup and manual update detection, current-version display, and update-available notification

### Modified Capabilities

<!-- None. -->

## Impact

**New code:**
- `src/CLIHub.Core/Interfaces/IUpdateService.cs`
- `src/CLIHub.Core/Services/UpdateService.cs`

**Modified code:**
- `src/CLIHub.Core/CLIHub.Core.csproj` — add the `Velopack` package
- `src/CLIHub.Core/ServiceCollectionExtensions.cs` — register `IUpdateService`
- `src/CLIHub/App.xaml.cs` — run the startup check and show a tray notification on availability
- `src/CLIHub/TrayIconController.cs` — expose a notification method
- `src/CLIHub/Windows/MainWindow.xaml(.cs)` — show the app version and a "Check for updates" button

**No breaking changes.**
