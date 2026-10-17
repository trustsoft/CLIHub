## 1. View model and composition

- [x] 1.1 Inject `IReleaseNotesLauncher` into `SettingsViewModel` and register `ShowReleaseNotesCommand`
- [x] 1.2 Add update-status properties derived from `UpdateCheckResult` and `LastKnownAvailableVersion`, keeping `UpdateMessage` in sync
- [x] 1.3 Reset the status from the shared availability state when `Load` runs; update the Updates page description to match the new page

## 2. Updates page layout

- [x] 2.1 Add the Updates page-header slots with version and channel chips, the update-status chip, the "Check for updates" action, and "View release notes"
- [x] 2.2 Rename the automatic section to "UPDATE PREFERENCES" and keep the four existing toggles in one group
- [x] 2.3 Remove the status card, the separate "MANUAL" section, and the old version line

## 3. Tests and verification

- [x] 3.1 Add view-model tests for status mapping (`UpToDate`, `UpdateAvailable`, `NotInstalled`, failed) and the release-notes command
- [x] 3.2 Update all `SettingsViewModel` construction sites in tests and DI for the new dependency
- [x] 3.3 Run `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, and `openspec validate --all`
