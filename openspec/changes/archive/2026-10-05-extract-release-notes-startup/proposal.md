## Why

`App.OnStartup` currently owns the release-notes version lookup, preference lookup, prompt decision, and window/recording actions. Extracting this sequence gives startup orchestration a focused boundary while preserving the existing first-run and upgrade behavior.

## What Changes

- Add a startup coordinator for the one-time release-notes decision.
- Preserve `Show`, `RecordOnly`, and `Skip` behavior from `ReleaseNotesPrompt`.
- Use the already loaded startup preferences snapshot rather than reading preferences again.
- Preserve the existing warning-only failure boundary so release notes cannot block application startup.
- Replace the inline release-notes workflow in `App.OnStartup` and add focused tests.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is an internal refactor; release notes and application lifecycle requirements remain unchanged.

## Impact

- Affected runtime code: `App.xaml.cs`, WPF application composition, and the new startup coordinator.
- Affected tests: focused coordinator and DI tests.
- No change to release-notes parsing, version recording, What's New window behavior, or user-visible startup behavior.
- OpenSpec specs are intentionally skipped because no observable requirement changes.
