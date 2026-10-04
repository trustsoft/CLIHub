# Launch Window Update Button

## Why

The launch window footer separates the current version (a passive chip) from the "Check for updates" button, and neither can act on a found update - the user has to go to the tray menu or the What's New window to install it. Merging the version display and the update actions into one control makes the footer self-explanatory: it always shows the running version and turns into the update action as soon as a newer version is known.

## What Changes

- Merge the footer version chip and the "Check for updates" button into a single update control that shows the current version when idle and runs a manual update check on click.
- When an update is available, the control reads "Update to \<version\>" with an accent visual treatment; clicking starts the download.
- While a download runs, the control shows a "Downloading…" state and does not start a second download - including downloads started from the tray menu or the What's New window.
- When the download completes, the control reads "Restart to update"; clicking applies the update and restarts the application into the new version.
- Verbose outcomes (check failure, timeout, non-managed install) remain in the footer status line.
- The update control state machine is extracted into a shared `UpdateControlViewModel`, so a future surface (for example the Settings window's UPDATES block) can reuse it.
- The Settings window UPDATES block is unchanged in this change.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `update-checking`: the launch window footer gains an update control that combines the current version display, the manual check trigger, and the download-and-restart action in one element, sharing update state with the tray menu and the What's New window.
- `main-window-layout`: the footer requirement changes - the version chip is merged into the update control, so the footer shows the application identity (brand) plus the update control that itself displays the current version.

## Impact

- `src/CLIHub/ViewModels/LaunchWindowViewModel.cs` - delegates the update control to the shared view model.
- `src/CLIHub/ViewModels/UpdateControlViewModel.cs` (new) - shared update control state machine and command.
- `src/CLIHub/Views/LaunchWindow.xaml` and `src/CLIHub/Themes/LaunchWindowStyles.xaml` - merged control, accent states.
- `src/CLIHub.Core/Interfaces/IUpdateService.cs` - no changes; the "downloaded, ready to apply" state is tracked in the shared view model, availability and download states derive from the existing members (`CheckForUpdatesAsync`, `DownloadUpdateAsync`, `ApplyDownloadedUpdateAndRestart`, `IsDownloading`, `LastKnownAvailableVersion`, `UpdateStateChanged`).
- Settings window and tray menu are unchanged.
