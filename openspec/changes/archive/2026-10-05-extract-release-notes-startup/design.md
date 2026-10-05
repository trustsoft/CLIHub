## Context

`App.OnStartup` currently gets the running version from `IUpdateService`, checks `IReleaseNotesService`, reads `LastSeenReleaseNotesVersion` through `IPreferencesStore`, applies `ReleaseNotesPrompt.Decide`, and calls `IReleaseNotesLauncher`. The launcher already owns the window and persistence side effects, while `ReleaseNotesPrompt` owns the pure decision logic.

## Goals / Non-Goals

**Goals:**

- Move release-notes startup orchestration out of `App.xaml.cs`.
- Pass the already loaded `AppPreferences` snapshot into the coordinator.
- Preserve the existing decision and action mapping.
- Preserve warning-only exception handling.
- Make the orchestration testable without constructing WPF `Application` or windows.

**Non-Goals:**

- Change `ReleaseNotesPrompt`, release-notes parsing, or launcher persistence.
- Change the behavior of manual What's New opening from the tray.
- Add a new release-notes domain abstraction.
- Change update service version discovery.

## Decisions

- Add `IReleaseNotesStartupCoordinator` with a synchronous `Evaluate(AppPreferences preferences)` operation, matching the current startup flow.
- Inject `IUpdateService`, `IReleaseNotesService`, `IReleaseNotesLauncher`, and `ILogger` into the implementation.
- Obtain the recorded version from the passed preferences snapshot and do not call `IPreferencesStore` inside the coordinator.
- Keep the existing `try/catch` boundary in the coordinator and log the same warning message when evaluation or presentation fails.
- Register the coordinator as a singleton in WPF composition.

## Risks / Trade-offs

- **A second preferences load could reappear** -> Make the coordinator accept `AppPreferences` explicitly and test that the passed recorded version drives the decision.
- **Window/persistence behavior could change** -> Delegate `ShowReleaseNotes` and `MarkReleaseNotesSeen` unchanged to `IReleaseNotesLauncher`.
- **Startup could become blocked by release notes** -> Keep the operation synchronous as today but retain the warning-only catch; the underlying release-notes work is local and bounded.

## Migration Plan

1. Add the coordinator interface and implementation.
2. Register it in WPF composition.
3. Replace `ShowReleaseNotesOnce` in `App.OnStartup` with coordinator invocation and remove the old private method.
4. Run focused tests, full build, and full test suite.
5. Rollback consists of restoring the inline method and removing the coordinator registration/types.

## Open Questions

None.
