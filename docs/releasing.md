# Release Runbook

How to cut a CLIHub release. The pipeline (the `ci-build` and `release-pipeline` specs) does the
building, packaging, and publishing; your job is the README screenshots, the notes, the development
version, and the tag. The durable pipeline contract is
[`openspec/specs/release-pipeline/spec.md`](../openspec/specs/release-pipeline/spec.md).

## Before you start

- All work is merged to `master` with green CI.
- The version is decided and follows SemVer (`MAJOR.MINOR.PATCH`).

## 0. Refresh the README screenshots

Do this before the first release-preparation commit. The tag points at a commit, so the screenshots in
the tree when you tag are the ones every installer and every GitHub visitor sees — refreshing them
afterwards leaves the release showing the previous UI.

Close CLIHub first, then regenerate the image set:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File docs\images\tools\Capture-ReadmeImages.ps1
```

The harness builds the Debug app, seeds a mock workspace (`-MockWorkspaceRoot`, default
`D:\Projects\Acme`), temporarily replaces and then restores `%APPDATA%\CLIHub\config.json` and
`cache\logos.json`, renders the launch window plus the Theme & Accent, Project Scanning, CLI Agents, and
Agent Detection pages at 1.5×, and applies the 12 px rounded-corner mask. Unless you pass
`-SkipWebImages`, it also re-renders `banner.png`, `workflow.png`, and `social-preview.png` with headless
Chrome — a missing Chrome fails the run. The harness refuses to run while CLIHub is running, and a
failed capture stops with the contents of `capture-error.txt` instead of writing new images.

Open the regenerated PNGs in `docs\images\` and look at them before committing: they must show the UI of
the version being released, with the seeded demo project names rather than your own. Commit the refreshed
set as the first release-preparation commit, for example
`docs(images): refresh the README screenshots for <version>`.

## 1. Write the release notes

Add a section for the new version to both documents at the repository root, with the same version
and date:

**`RELEASE-NOTES.md`** (user-facing — what the updater and the GitHub release will show):

```markdown
## 0.9.0 — YYYY-MM-DD

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

**`CHANGELOG.md`** (technical, for developers): `## 0.9.0 — YYYY-MM-DD` with `Added` / `Changed` /
`Fixed` / `Removed` groups, `**BREAKING**` markers, and spec references where useful. If an
`## Unreleased` section exists, move its groups under the new dated heading and remove it; do not leave it
in place.

Commit the notes to `master` (for example `docs: release notes for 0.9.0`) and wait for CI to go
green.

## 2. Set the development version

Update `Directory.Build.props` to match the version being released:

```xml
<Version>0.9.0</Version>
```

Commit to `master` (for example `release: set version 0.9.0`) and wait for CI to go green. The
automated version check ensures this step is not skipped — a stale version in the props file will
block the release pipeline's test job.

After the release is published, you may bump the props version to the next planned version (for
example `0.10.0`) as part of the new cycle's work, or leave it at the released version until the next
release preparation begins. Both satisfy the constraint (props >= highest release tag).

## 3. Tag and push

```powershell
git tag v0.9.0
git push origin v0.9.0
```

The tag without the leading `v` is the release version — it overrides the development default in
`Directory.Build.props` for this build. Tag only commits that are on `master`; the pipeline re-runs
the tests as a gate anyway, but a release should come from green master.

## 4. Watch the pipeline

The `Release` workflow (Actions tab) runs two jobs, about 4–6 minutes total:

1. `test` — build + full test suite (a failure here stops everything before any packaging);
2. `release` — derives the version from the tag, extracts the notes section, publishes
   framework-dependent win-x64, packages with `vpk` (setup, portable, delta; the installer offers
   the .NET 10 Desktop Runtime when missing), and publishes everything to GitHub Releases on the
   pushed tag, with the notes section as the release body.

## 5. Verify

- The workflow run is green.
- The README screenshots in `docs\images\` show the released UI (refreshed in step 0).
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

## Development version tracking

The `Directory.Build.props` version is the development default (what a dev-run build reports). It MUST
be equal to or ahead of the highest stable release tag — this is enforced by the CI and release
pipelines through `.github/scripts/Check-DevVersion.ps1`.

## Not wired yet

- **Prerelease channels** — a tag like `v0.9.0-beta.1` would pass the version check but publish as a
  stable release; do not use prerelease tags until a beta channel change exists (stable-only
  decision, 2026-10-03).
- **Code signing** — packages are unsigned, so SmartScreen may warn on first install.
