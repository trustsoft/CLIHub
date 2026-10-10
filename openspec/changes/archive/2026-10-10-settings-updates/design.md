## Context

Update checks run through `UpdateService` (Velopack) behind `IUpdateChecker`/`IUpdateStateSource`, coordinated by the application `UpdateWorkflow`; the launch window, tray, What's New, and Settings consume the shared state. `UpdateService.CreateDefaultManager` builds a `GithubSource(url, null, prerelease: false)`, so only stable releases are seen. The only update preference today is `CheckForUpdatesOnStartup`.

See `proposal.md` for motivation and the specs deltas for required behavior.

## Goals / Non-Goals

**Goals:**
- Add three additive update preferences with defaults and no migration.
- Auto-download an available update in the background when enabled, keeping per-entry-point restart policy.
- Gate stable/pre-release notifications and include pre-releases only when enabled.
- Simplify the Settings version status.

**Non-Goals:**
- Update channel selection; download progress percentages; changing install/restart mechanics.

## Decisions

### Additive preferences, no migration
`AppPreferences` gains `AutoDownloadUpdates = true`, `NotifyStableUpdates = true`, `NotifyPrereleaseUpdates = false`; `PreferencesDocument` mirrors them and `ConfigurationSnapshot.ClonePreferences` copies them. Absent fields take the defaults, so no schema bump or migration is needed.

### Pre-release checks via a per-check flag
`IUpdateChecker.CheckForUpdatesAsync` (and the underlying `UpdateService`) gains an `includePrerelease` parameter; `UpdateService` rebuilds the `GithubSource` with the matching `prerelease` value. The application `UpdateWorkflow` reads `NotifyPrereleaseUpdates` from `IPreferencesStore` and passes it in. Alternative considered: giving `UpdateService` a direct preferences dependency — rejected to keep Core's update boundary independent of the settings store.

### Auto-download uses the download-only step
When a check finds an update and `AutoDownloadUpdates` is enabled, `UpdateWorkflow` starts the **download-only** operation (not the download-and-apply operation). Entry points keep their existing apply policy: the launch window still offers its explicit "Restart to update" action, while the tray / What's New auto-apply behavior is unchanged once a download exists. The existing `_downloadGate` prevents a duplicate download when a user also triggers one.

### Notification gating by version class
The available version string is classified as stable or pre-release (a semver pre-release identifier such as `-beta.1` marks pre-release). The availability notification is shown only when the matching toggle is enabled; when both are disabled no availability notification is shown. Pre-release versions are never surfaced unless `NotifyPrereleaseUpdates` is enabled (they are not fetched otherwise).

### Simplified version status
The Settings Updates page shows the current version and the check status line (up to date, or update available with the version). The technical metadata (target, commit, agent protocol) is not shown.

## Risks / Trade-offs

- [Auto-download surprises users on metered links] → default-on is the agreed behavior; the toggle in Settings disables it.
- [Interface change to `IUpdateChecker`/`UpdateService`] → update the single Core implementation and its tests; the flag defaults to `false` for existing call sites.
- [Pre-release classification] → a small, testable helper; document the semver rule.

## Migration Plan

No schema change. New preferences default when absent; the next write persists them.

## Open Questions

- Exact semver pre-release detection rule and where the preference is read (workflow vs startup coordinator) — settle during implementation; does not change the specs or task breakdown.
