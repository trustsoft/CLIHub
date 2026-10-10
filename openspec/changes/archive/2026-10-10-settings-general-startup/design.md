## Context

Startup visibility is stored as `AppPreferences.ShowWindowOnStartup` (default `true`) and consumed in `ApplicationBootstrapper.Start` (`if (startupState.Preferences.ShowWindowOnStartup) startupUi.ShowLaunchWindow()`). Persistence goes through `AppConfigDocument.PreferencesDocument` and the schema machinery: `AppConfigDocument.CurrentSchemaVersion` is `1`, and `ConfigurationRepository` reads the document, builds a `ConfigurationSnapshot`, and runs `ConfigMigrationRunner` when the document version is older. `LegacyTerminalPreferenceMigration` (0→1) is the only registered migration and keeps a legacy model field (`TerminalExecutable`) so the migration can read it from the snapshot.

See `proposal.md` for motivation and the specs deltas for required behavior.

## Goals / Non-Goals

**Goals:**
- Replace the active preference with `StartMinimized` end to end (model, persistence, startup, Settings).
- Advance the schema to version 2 with a 1→2 migration that preserves the user's effective startup intent.

**Non-Goals:**
- System notifications, autostart, or any other preference.
- Removing the legacy `showWindowOnStartup` JSON property (retained for migration and backward-compatible readers).

## Decisions

### Migrate at the snapshot level, keeping a legacy model field
`ConfigMigrationRunner` operates on `ConfigurationSnapshot`, and a migration can only see fields present on the model. Mirroring `LegacyTerminalPreferenceMigration`/`TerminalExecutable`, keep `AppPreferences.ShowWindowOnStartup` as a documented legacy field and add `StartMinimized`; the new migration reads the legacy value and sets `StartMinimized = !ShowWindowOnStartup`. Alternative considered: inverting during DTO→model conversion — rejected because the repository does not transform values and migrations are the established mechanism.

### Schema version 2 with literal migration targets
Set `AppConfigDocument.CurrentSchemaVersion = 2`. `LegacyTerminalPreferenceMigration.TargetVersion` currently returns `CurrentSchemaVersion`, which would become `2` and violate the runner's "advance exactly one version" rule (0→1). Change it to a literal `1`, and give the new migration `SourceVersion = 1`, `TargetVersion = CurrentSchemaVersion` (2). Register the new migration in the Core DI composition alongside the existing one.

### Persist both fields, keep the legacy value non-stale
`PreferencesDocument.From` writes `StartMinimized` and also `ShowWindowOnStartup = !StartMinimized`, so older readers and the migration both stay consistent; `ToPreferences` reads `StartMinimized`. This keeps the single-document contract and the existing JSON shape while the new field carries the truth.

### Show the window unless start-minimized
`ApplicationBootstrapper` shows the launch window when `!Preferences.StartMinimized`, replacing the current `ShowWindowOnStartup` check. The tray/hotkey/second-launch reachability is unchanged.

### Rename end to end in the UI path
`SettingsDraft`/`SettingsInput` (`SettingsApplicationService`), `SettingsViewModel.ShowWindowOnStartup` → `StartMinimized`, and the `SettingsGeneralPage` toggle label/binding become "Start minimized to tray". Save/apply/rollback logic is unchanged.

## Risks / Trade-offs

- [Migration chain breakage] → change the legacy migration's target to a literal `1`; add Core tests asserting a schema-1 document with `showWindowOnStartup` true and false migrates to the correct `startMinimized`.
- [JSON shape drift] → keep every other property name; only add `startMinimized`; update round-trip tests.
- [Behavior change for existing users] → the inversion migration preserves intent; a manual test with a schema-1 `config.json` confirms the tray vs window outcome.

## Migration Plan

Existing `config.json` with `schemaVersion: 1` is migrated to `2` on load (0→1→2 chain), converting `showWindowOnStartup` into `startMinimized`. Documents already at `2` are a no-op. A version above `2` is treated as unknown and left untouched by the existing safe path. Rollback is reverting the code; the persisted `startMinimized` plus the derived legacy field keep both readers working.

## Open Questions

- None affecting the specs, approach, or task breakdown.
