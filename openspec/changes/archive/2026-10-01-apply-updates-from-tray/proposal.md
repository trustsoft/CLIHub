# Proposal

## Why

CLIHub only detects and announces updates: the tray notification tells the user a new version exists, but installing it means leaving the app, opening the GitHub releases page, and replacing the installation manually. Closing this loop — download and apply from the tray — is the last remaining update capability in `docs/vision.md`, and the release-notes work that landed with it (What's New window) is a natural place to surface the action.

## What Changes

- Extend the update service (`IUpdateService` / `UpdateService`) with download and apply operations built on Velopack's `UpdateManager.DownloadUpdatesAsync` and `ApplyUpdatesAndRestart`, so the whole flow stays in-process with no user hand-off.
- Add a one-click **Download update and restart** action to the tray menu, shown only when an update is available and the app is a Velopack install.
- While the download runs, the tray menu item reflects progress ("Downloading update…"); on completion the application restarts into the new version.
- Show a tray notification when the update is downloaded and the restart begins, and when the download fails.
- Add an install action to the **What's New** window (same one-click behavior, sharing the same state so both surfaces stay consistent).
- Guard against concurrent downloads and handle failures gracefully: errors are logged and reported to the user without crashing or blocking the app.
- Update project documentation (vision status, README, architecture) to reflect that updates can be applied from the app.

## Capabilities

### New Capabilities

<!-- None: downloading and applying extend the existing update-checking capability. -->

### Modified Capabilities

- `update-checking`: add requirements for downloading an available update, applying it with an application restart, reflecting download progress in the tray menu, surfacing the action from the What's New window, and handling download/apply failures and concurrent requests.

## Impact

- **Modified:** `src/CLIHub.Core/Interfaces/IUpdateService.cs` and `src/CLIHub.Core/Services/UpdateService.cs` (download/apply operations, progress states); `src/CLIHub.Core/Models/` (result/progress record for the new operations); `src/CLIHub/TrayIconController.cs` (update menu item with state, notifications); `src/CLIHub/App.xaml.cs` (wiring the action and state); `src/CLIHub/Windows/WhatsNewWindow.xaml(.cs)` and `src/CLIHub/ViewModels/WhatsNewViewModel.cs` (install action); `docs/vision.md`, `docs/architecture.md`, `README.md`.
- **Tests:** extended `tests/CLIHub.Tests/Services/UpdateServiceTests.cs` using the existing `TestVelopackLocator` + `SimpleFileSource` seam for guard paths (not installed, no update, already downloading); UI behavior stays manual.
- **Dependencies:** none added (Velopack already provides download and apply).
- **Not included:** CI/packaging changes (`vpk pack` flags, delta-only publishing); automatic downloads without user action; in-app progress window; update rollback UI; changing the update source or the check schedule.
