## 1. UI XAML removal

- [x] 1.1 Remove the "Channel: Stable" chip (Border element lines 101-106) from `src/CLIHub/Views/SettingsWindow.xaml` UpdatesHeaderTitleExtra DataTemplate, keeping the Version chip and the Release-notes button
- [x] 1.2 Remove the disabled "Notify about pre-releases" SettingCard (lines 35-46) from `src/CLIHub/Views/SettingsUpdatesPage.xaml`, keeping the three remaining toggles (startup check, auto-download, stable notifications)
- [x] 1.3 Build `CLIHub` project with 0 errors/warnings

## 2. Core configuration model

- [x] 2.1 Remove `AppPreferences.NotifyPrereleaseUpdates` property (lines 93-97) from `src/CLIHub.Core/Models/Configuration/AppConfig.cs`
- [x] 2.2 Remove `PreferencesDocument.NotifyPrereleaseUpdates` field (line 115) and its mappings in `From` (line 147) and `ToPreferences` (line 188) from `src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs`
- [x] 2.3 Remove `NotifyPrereleaseUpdates` clone mapping (line 75) from `ConfigurationSnapshot.ClonePreferences` in `src/CLIHub.Core/Configuration/ConfigurationSnapshot.cs`
- [x] 2.4 Build `CLIHub.Core` project with 0 errors/warnings

## 3. Core update logic

- [x] 3.1 Remove `IPreferencesStore? preferences` field (line 26), `cachedManagerPrerelease` field (line 30), the constructor's `IPreferencesStore preferencesStore` parameter (line 42), and the `prerelease` read (line 79) from `src/CLIHub.Core/Updates/UpdateService.cs`
- [x] 3.2 Replace `CreateDefaultManager(prerelease)` with `CreateDefaultManager()` (remove the parameter) and construct the stable-only source unconditionally in `CreateDefaultManager`; remove the `cachedManagerPrerelease` cache-invalidation check (line 83). Deviation: Velopack 1.2.161 offers no single-argument `GithubSource(string)`, so the implemented call is `GithubSource(RepositoryUrl, null, prerelease: false)`.
- [x] 3.3 Inspect DI registration files (search for `UpdateService` registration, likely in `src/CLIHub/Composition/` or startup) and remove the `IPreferencesStore` argument from the service registration
- [x] 3.4 Replace `UpdateStartupCoordinator` notification logic (lines 53-56) with stable-only notification: `if (appPrefs.NotifyStableUpdates) { ... }` (remove the `IsPrerelease` ternary)
- [x] 3.5 Delete `src/CLIHub.Core/Updates/VersionClassification.cs` (unused after coordinator simplification)
- [x] 3.6 Build `CLIHub.Core` and `CLIHub` projects with 0 errors/warnings

## 4. UI ViewModel and service

- [x] 4.1 Remove `NotifyPrereleaseUpdates` parameter (line 24) from `SettingsInput` record in `src/CLIHub/Settings/SettingsApplicationService.cs`
- [x] 4.2 Remove `NotifyPrereleaseUpdates` parameter (line 87) from `SettingsDraft` record
- [x] 4.3 Remove `NotifyPrereleaseUpdates` from `LoadDraft` mapping (line 204), `TryCreateDraft` draft construction (line 280), and `WritePreferences` assignment (line 564)
- [x] 4.4 Remove `notifyPrereleaseUpdates` field (line 91), `NotifyPrereleaseUpdates` property (lines 412-419), and PropertyChanged notification (line 667) from `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`
- [x] 4.5 Remove `this.NotifyPrereleaseUpdates` from `LoadDraft` (line 629) and from `SaveCommand` input construction (line 846)
- [x] 4.6 Build `CLIHub` project with 0 errors/warnings

## 5. Tests

- [x] 5.1 Remove `nameof(AppPreferences.NotifyPrereleaseUpdates)` (line 39), the `NotifyPrereleaseUpdates = true` initialization (line 194), and the assertion (line 251) from `tests/CLIHub.Core.Tests/Configuration/PersistenceCloneRoundTripTests.cs`
- [x] 5.2 Remove `notifyPrerelease` parameter and pre-release test scenarios (tests on lines 109-132: `CheckAsync_PrereleaseAvailable_WhenDisabled_DoesNotNotify` and `CheckAsync_PrereleaseAvailable_WhenEnabled_Notifies`) from `tests/CLIHub.Tests/Services/UpdateStartupCoordinatorTests.cs`; update `CreateCoordinator` to remove the `notifyPrerelease` parameter (line 146) and `NotifyPrereleaseUpdates` from the stub preferences (line 152)
- [x] 5.3 If `tests/CLIHub.Core.Tests/Updates/VersionClassificationTests.cs` exists, delete it
- [x] 5.4 Run `dotnet test CLIHub.sln` and verify all tests pass

## 6. Documentation

- [x] 6.1 Remove `notifyPrereleaseUpdates` from the AppPreferences field list in `docs/architecture/configuration.md` (line 6)
- [x] 6.2 Verify `docs/releasing.md` prerelease-tag guidance (lines 144-146) does not require rewording (it should not — the note documents the stable-only policy and remains accurate)

## 7. Build and validation

- [x] 7.1 Run `dotnet build CLIHub.sln` with 0 errors/warnings
- [x] 7.2 Run `dotnet test CLIHub.sln` and verify all tests pass
- [x] 7.3 Run `openspec validate --all` and verify 0 errors
- [x] 7.4 Run `graphify update .` to refresh the knowledge graph after code changes

## 8. Manual smoke (user, not the implementer)

- [ ] 8.1 Settings → Updates page shows Version chip, no Channel chip, and three toggles (startup check, auto-download, stable notifications)
- [ ] 8.2 Update check returns only stable releases (no pre-release versions offered)
- [ ] 8.3 Old config file with `notifyPrereleaseUpdates: true` loads without error, and the key disappears on next save

> **Note:** The implementer MUST leave stage 8 checkboxes unticked and MUST NOT claim GUI verification. The orchestrator reports these smoke tests to the user for confirmation.
