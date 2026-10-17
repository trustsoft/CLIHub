## Why

The Stitch target for the CLIHub Settings window presents Updates as a status-first page: the current version and check outcome are prominent, the check action sits next to them, and release notes are reachable from the page. The current implementation instead shows a plain "Current version" line, puts the check in a separate bottom card, and exposes release notes only from the tray.

## What Changes

- Add version, channel, and update-status chips to the Updates page header.
- Move the "Check for updates" action into the page header and remove the separate status and manual-check cards.
- Move the "View release notes" action to the bottom-right of the page header; it opens the existing What's New window through `IReleaseNotesLauncher`.
- Rename the automatic-updates section to "UPDATE PREFERENCES" and keep the existing four update toggles.
- Do not add update channels, build metadata, commit/protocol tags, or a changelog copy; those are not supported by the current update model or duplicate What's New.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `preferences-ui`: The "Updates page options" requirement now describes the status card, the update-preferences section, and the release-notes action. A new requirement covers opening release notes from Settings.

## Impact

- `src/CLIHub/Views/SettingsWindow.xaml` — page-header metadata, status, and action slots for Updates.
- `src/CLIHub/Views/SettingsUpdatesPage.xaml` — update preferences section; the old status card is removed.
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` — update-status properties, release-notes command, and dependency injection.
- `src/CLIHub/Composition/ServiceRegistration.cs` — pass `IReleaseNotesLauncher` to the settings view model.
- Tests that construct `SettingsViewModel` — supply the release-notes launcher.
- `openspec/specs/preferences-ui/spec.md` — updated through the change archive.
