## Why

Updates currently require a manual download action and only track stable releases; the Updates page exposes just the startup-check toggle. The agreed settings add quiet background download, stable/pre-release notification controls, and a simplified version status.

## What Changes

- Add preferences: `AutoDownloadUpdates` (default `true`), `NotifyStableUpdates` (default `true`), `NotifyPrereleaseUpdates` (default `false`). These are additive fields with defaults; no schema migration.
- When the startup or manual check finds an available update **and** background auto-download is enabled, the shared update workflow downloads it automatically; update entry points then show the existing restart-to-update state. When disabled, the current manual download action remains.
- When pre-release notifications are enabled, update checks include pre-release versions; stable and pre-release availability are notified according to their respective toggles.
- Updates page: add the three toggles and simplify the version status to the current version plus the check status (up to date, or update available with the version), dropping commit / target / agent-protocol detail.
- No update-channel selection.

## Capabilities

### New Capabilities
<!-- Requirements land in the existing update-checking and preferences-ui capabilities. -->

### Modified Capabilities
- `update-checking`: background auto-download preference and stable/pre-release notification gating.
- `preferences-ui`: the Updates page offers the auto-download and notification toggles and a simplified version status.
- `configuration-snapshot`: the new update preferences default safely when absent.

## Impact

- Core: `UpdateService` / `UpdateChecker` (pre-release flag sourced from preferences), `AppPreferences`, `AppConfigDocument`.
- App: `UpdateWorkflow` (auto-download after a successful check), startup update coordinator, `SettingsViewModel`, `SettingsUpdatesPage`.
- Tests: update workflow/service tests, settings tests, serialization tests.

## Non-goals

- Update channel selection; download progress percentages; changes to the install/restart mechanics.
