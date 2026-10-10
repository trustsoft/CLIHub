## 1. Preferences

- [x] 1.1 Add `AutoDownloadUpdates` (default true), `NotifyStableUpdates` (default true), and `NotifyPrereleaseUpdates` (default false) to `AppPreferences`, mirror them in `PreferencesDocument`, and copy them in `ConfigurationSnapshot.ClonePreferences`; verify the serialization round-trip tests pass.

## 2. Pre-release checks

- [x] 2.1 Add an `includePrerelease` parameter to `IUpdateChecker.CheckForUpdatesAsync` and `UpdateService` (rebuild the `GithubSource` with the matching `prerelease` value); verify the existing update-service tests still pass with the default `false`.
- [x] 2.2 Read `NotifyPrereleaseUpdates` from `IPreferencesStore` in the update workflow and pass it into the check; verify a persistence-backed test covers both values.

## 3. Background auto-download

- [x] 3.1 After a check finds an available update and `AutoDownloadUpdates` is enabled, start the download-only operation (not download-and-apply); verify the workflow test covers auto-download enabled and disabled.
- [x] 3.2 Keep each entry point's apply policy intact (launch window explicit restart; tray / What's New unchanged); verify the existing shared-workflow and launch-control tests pass.

## 4. Notification gating

- [x] 4.1 Classify the available version as stable or pre-release and gate the availability notification by `NotifyStableUpdates` / `NotifyPrereleaseUpdates`; verify tests cover stable-only, pre-release, and both-disabled.

## 5. Settings UI

- [x] 5.1 Add the three toggles to the Updates page and bind them to the new preferences; verify the settings view-model tests pass.
- [x] 5.2 Simplify the version status to the current version plus the check status; verify the page builds and shows no target/commit/protocol detail.

## 6. Verification

- [x] 6.1 Run `dotnet build CLIHub.sln` and verify it succeeds.
- [x] 6.2 Run `dotnet test CLIHub.sln` and verify all tests pass.
- [x] 6.3 Run `openspec validate settings-updates --strict` and verify the change is valid.
- [x] 6.4 Smoke-test: with auto-download on, finding an update downloads it in the background and offers restart; the toggles persist; disabling pre-release excludes pre-release versions.
