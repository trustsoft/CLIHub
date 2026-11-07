# Proposal: remove-prerelease-updates-option

## Why

The application ships only stable releases — no pre-release builds are published and no beta channel exists. A stored `notifyPrereleaseUpdates: true` preference would keep enabling pre-release update checks with no UI remaining to turn it off. The setting card and channel chip are disabled or hardcoded on the Updates page with a hint reading "Not available yet. Stable releases only."

Because the preference is inert and cannot be turned on again once removed from the UI, the full removal simplifies the configuration surface, eliminates dead paths in the update logic, and makes the intent ("stable-only") explicit in the code.

## What Changes

- The "Notify about pre-releases" SettingCard and the "Channel: Stable" chip are removed from the Settings → Updates page UI
- `AppPreferences.NotifyPrereleaseUpdates` is removed from the configuration model and document mappings
- `UpdateService` stops reading the prerelease preference and drops its cached-manager prerelease flag; it always constructs the stable-only source (`GithubSource(RepositoryUrl, null, prerelease: false)`, the only constructor Velopack 1.2.161 offers)
- `UpdateStartupCoordinator` removes the prerelease notification branch and notifies only stable updates per `NotifyStableUpdates`
- `VersionClassification.IsPrerelease` is removed (it becomes unused after the coordinator change)
- Tests, specs, and architecture documentation reflect stable-only update checking and the streamlined notification policy

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `preferences-ui`: Updates page shows only stable-channel UI (version chip, three toggles: startup check, auto-download, stable notifications)
- `update-checking`: Update checks are always stable-only; notification preferences control only stable-update notifications
- `configuration-snapshot`: `notifyPrereleaseUpdates` is absent from the round-trip contract

## Impact

**Modified code:**
- `src/CLIHub/Views/SettingsWindow.xaml` — remove "Channel: Stable" chip
- `src/CLIHub/Views/SettingsUpdatesPage.xaml` — remove disabled "Notify about pre-releases" card
- `src/CLIHub.Core/Models/Configuration/AppConfig.cs` — remove `AppPreferences.NotifyPrereleaseUpdates` property
- `src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs` — remove `PreferencesDocument.NotifyPrereleaseUpdates` and its mappings
- `src/CLIHub.Core/Configuration/ConfigurationSnapshot.cs` — remove `NotifyPrereleaseUpdates` clone mapping
- `src/CLIHub/Settings/SettingsApplicationService.cs` — remove `NotifyPrereleaseUpdates` from `SettingsInput`, `SettingsDraft`, and all mappings
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` — remove `NotifyPrereleaseUpdates` property, field, and PropertyChanged wiring
- `src/CLIHub.Core/Updates/UpdateService.cs` — remove `IPreferencesStore` dependency, cached-manager prerelease flag, and `GithubSource` prerelease parameter; construct `GithubSource(RepositoryUrl)` unconditionally
- `src/CLIHub/Startup/UpdateStartupCoordinator.cs` — remove prerelease notification path; notify per `NotifyStableUpdates` only
- `src/CLIHub.Core/Updates/VersionClassification.cs` — remove file (unused after coordinator simplification)
- `tests/CLIHub.Core.Tests/Configuration/PersistenceCloneRoundTripTests.cs` — remove `NotifyPrereleaseUpdates` from field list and assertions
- `tests/CLIHub.Tests/Services/UpdateStartupCoordinatorTests.cs` — remove prerelease test scenarios and `notifyPrerelease` parameter; keep stable-notification coverage
- `tests/CLIHub.Core.Tests/Updates/VersionClassificationTests.cs` — remove file (removed with its subject)
- `docs/architecture/configuration.md` — remove `notifyPrereleaseUpdates` from AppPreferences field list

**Config backward compatibility:**
System.Text.Json (CoreJson.Options) ignores unknown members on load — an old `notifyPrereleaseUpdates: true` key is skipped and never written back. No migration needed.

**No breaking changes.**
**No schema version bump.**

## Non-Goals

- Changing the release-notes guidance in `docs/releasing.md` (the existing note stays; wording is preserved)
- Introducing a beta-channel capability (this removal is not a deferral; if pre-release channels return, the capability is re-introduced with a design)
- Touching unrelated update UI or notification surfaces
