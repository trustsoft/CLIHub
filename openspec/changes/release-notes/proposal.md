# Proposal

## Why

CLIHub ships user-visible changes (new agents, window behavior, settings) with no in-app way to see them, and the repository has no changelog at all — a new version arrives silently. `docs/changelog-and-release-notes.md` recorded the plan and its open questions; the questions are now decided, so the plan can become an implementable change.

## What Changes

- Add two hand-maintained documents at the repository root, sharing the same version headings:
  - `CHANGELOG.md` — technical record for developers (Added / Changed / Fixed / Removed, spec references, `**BREAKING**` markers).
  - `RELEASE-NOTES.md` — short user-facing notes grouped as New / Improved / Fixed.
- Seed both with a single `0.5.0` section summarizing the capabilities delivered to date (taken from `openspec/changes/archive/`), aligning with the current product version in `Directory.Build.props`.
- Embed `RELEASE-NOTES.md` into `CLIHub.Core` as a resource so notes are available offline and independent of the update mechanism.
- Add `IReleaseNotesService` (`CLIHub.Core`) that parses the embedded notes into `ReleaseNote` records (version, date, New/Improved/Fixed lists), ordered newest first, and exposes the latest note.
- Add a **What's New** window listing release notes from newest to oldest in a scrollable view, opened from the tray menu.
- Show the window once after the application is upgraded: when the running version differs from the last version recorded in preferences, the window opens at startup and the recorded version is updated.
- Record the decisions from `docs/changelog-and-release-notes.md` and describe the notes format, the new window, and the new preference in project documentation.

## Capabilities

### New Capabilities

- `release-notes`: the release-notes documents (technical changelog and user-facing notes), their strict section format, and the service that parses the embedded user-facing notes into ordered, queryable records.
- `release-notes-display`: surfacing notes in the application — the **What's New** window and its reachability from the tray, and the one-time automatic display after an upgrade.

### Modified Capabilities

<!-- None: the notes documents, parser, window, and one-time display are all new behavior.
     Existing specs (update-checking, preferences-ui, app-lifecycle) keep their current
     requirements; the What's New window is a separate window from Settings and does not
     change update checking. -->

## Impact

- **New files:** `CHANGELOG.md`, `RELEASE-NOTES.md` (repository root); `CLIHub.Core/Models/ReleaseNote.cs`, `CLIHub.Core/Interfaces/IReleaseNotesService.cs`, `CLIHub.Core/Services/ReleaseNotesService.cs`; `CLIHub.Core/ReleaseNotes/RELEASE-NOTES.md` (embedded resource); `CLIHub/Windows/WhatsNewWindow.xaml(.cs)`, `CLIHub/ViewModels/WhatsNewViewModel.cs`, `CLIHub/IReleaseNotesLauncher.cs` + `CLIHub/ReleaseNotesLauncher.cs`.
- **Modified:** `CLIHub.Core/CLIHub.Core.csproj` (embedded resource), `CLIHub.Core/Models/AppConfig.cs` (`AppPreferences.LastSeenReleaseNotesVersion`), `CLIHub.Core/ServiceCollectionExtensions.cs` (register the service), `CLIHub/ServiceRegistration.cs` (register window/launcher/view model), `CLIHub/TrayIconController.cs` ("What's New" menu item), `CLIHub/App.xaml.cs` (startup one-time display), `docs/architecture.md`, `docs/repo-structure.md`, `docs/changelog-and-release-notes.md`, `README.md`.
- **Tests:** new `tests/CLIHub.Tests/ReleaseNotes/ReleaseNotesServiceTests.cs` (parsing, ordering, malformed input), plus coverage for the one-time-display decision.
- **Dependencies:** none added.
- **Not included:** CI wiring and `vpk pack --releaseNotes` (packaging change); fetching notes from the network; localization infrastructure (English only).
