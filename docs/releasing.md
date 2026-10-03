# Release Runbook

How to cut a CLIHub release. The pipeline (the `ci-build` and `release-pipeline` specs) does the
building, packaging, and publishing; your job is the notes and the tag. Pipeline mechanics:
[architecture.md → Packaging & CI/CD](architecture.md#packaging-cicd).

## Before you start

- All work is merged to `master` with green CI.
- The version is decided and follows SemVer (`MAJOR.MINOR.PATCH`).

## 1. Write the release notes

Add a section for the new version to both documents at the repository root, with the same version
and date:

**`RELEASE-NOTES.md`** (user-facing — what the updater and the GitHub release will show):

```markdown
## 0.7.0 — 2026-10-10

### New
- ...

### Improved
- ...

### Fixed
- ...
```

Rules: the heading is exactly `## <version> — <date>` (an em dash or a spaced hyphen); groups are
only `New`, `Improved`, and `Fixed`; entries are plain text without Markdown emphasis, one entry per
list item. **The release fails if there is no section for the tagged version** — this is deliberate,
so a release can never ship without its notes.

**`CHANGELOG.md`** (technical, for developers): `## [0.7.0] — 2026-10-10` with `Added` / `Changed` /
`Fixed` / `Removed` groups, `**BREAKING**` markers, and spec references where useful.

Commit the notes to `master` (for example `docs: release notes for 0.7.0`) and wait for CI to go
green.

## 2. Tag and push

```powershell
git tag v0.7.0
git push origin v0.7.0
```

The tag without the leading `v` is the release version — it overrides the development default in
`Directory.Build.props` for this build. Tag only commits that are on `master`; the pipeline re-runs
the tests as a gate anyway, but a release should come from green master.

## 3. Watch the pipeline

The `Release` workflow (Actions tab) runs two jobs, about 4–6 minutes total:

1. `test` — build + full test suite (a failure here stops everything before any packaging);
2. `release` — derives the version from the tag, extracts the notes section, publishes
   framework-dependent win-x64, packages with `vpk` (setup, portable, delta; the installer offers
   the .NET 8 Desktop Runtime when missing), and publishes everything to GitHub Releases on the
   pushed tag, with the notes section as the release body.

## 4. Verify

- The workflow run is green.
- `https://github.com/trustsoft/CLIHub/releases/tag/vX.Y.Z` exists, is not a draft, and has
  `CLIHub-win-Setup.exe`, `CLIHub-win-Portable.zip`, `CLIHub-X.Y.Z-full.nupkg`, `releases.win.json`,
  and the body matching the notes section.
- Install the setup on a machine: the app reports the new version and "Check for updates" finds
  nothing newer.

Installed clients pick the release up on their own: startup check, tray notification, one-click
"Download and restart", and the What's New window opens once with the new version's notes.

## Re-running a failed release

Fix the cause, then either re-run the failed jobs from the Actions UI (safe — the upload merges into
the existing release) or move the tag:

```powershell
git tag -f vX.Y.Z
git push -f origin vX.Y.Z
```

## Rollback

Delete the GitHub release and the tag. Installed clients then never see the update; a client that
already installed it stays on it (Velopack has no downgrade path).

## Not wired yet

- **Prerelease channels** — a tag like `v0.7.0-beta.1` would pass the version check but publish as a
  stable release; do not use prerelease tags until a beta channel change exists (stable-only
  decision, 2026-10-03).
- **Code signing** — packages are unsigned, so SmartScreen may warn on first install.
- **`Directory.Build.props`** — its `Version` is only the development default (what a dev-run build
  reports); bump it to the next version as part of the new cycle's work, not as a release step.
