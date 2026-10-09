# Changelog and Release Notes

This document records the current authoring rules. Historical rationale and implementation details are in
Git history and the archived OpenSpec changes.

## Canonical Files

| File | Audience | Format |
|---|---|---|
| `CHANGELOG.md` | Developers | `Added`, `Changed`, `Fixed`, `Removed`; include breaking-change markers and relevant spec names. |
| `RELEASE-NOTES.md` | End users and the application | `New`, `Improved`, `Fixed`; plain-text list items only. |

Both files use matching `## <version> — <date>` headings. Add the same release version and date to both
files when preparing a release.

## Application Contract

`RELEASE-NOTES.md` is embedded into `CLIHub.Core` and parsed by `ReleaseNotesService`. The parser recognizes
version headings followed by `### New`, `### Improved`, and `### Fixed` list groups. Malformed sections are
ignored without preventing the application from starting. The What's New window displays the parsed history;
the application can open it once after an upgrade.

## Release Workflow

Write both files, run the normal checks, and follow [`docs/releasing.md`](releasing.md). Do not duplicate
release mechanics here; the runbook and the release-pipeline spec are canonical for packaging behavior.
