## Why

The release version comes from the git tag by design (`.github/workflows/release.yml:92` derives it at workflow execution), but the development version in `Directory.Build.props:15` is manually bumped. The latest release tag is `v0.9.9`, yet the props file still declares `<Version>0.9.8</Version>` (last bumped in commit `7b94aa2 release: prepare 0.9.8`). The development build therefore reports an outdated version (seen in the launch window and Settings version chips through `UpdateService.GetCurrentVersion()`, which reads the assembly informational version at `src/CLIHub.Core/Updates/UpdateService.cs:121-133`), and the README screenshots committed in `6c65290` show `0.9.8` instead of the actual latest release `0.9.9`.

`docs/releasing.md:98-99` documents a manual bump step with "Not wired yet" — there is no automated check, so drift can persist unnoticed across releases.

## What Changes

- Bump `Directory.Build.props` to `<Version>0.9.9</Version>` (equal to the latest release tag).
- Add a PowerShell version-check script under `.github/scripts/` that reads the props `<Version>`, determines the highest stable release tag, and fails when props < highest tag. The script must handle shallow clones by failing with a clear message when tags are unavailable, and must support parameters or fixtures to exercise the comparison logic without mutating the repository (for verification).
- Add the version check to `.github/workflows/ci.yml` with `fetch-depth: 0` checkout so pull requests and master pushes fail when the development version is behind the highest release tag.
- Add the version check to `.github/workflows/release.yml` test job with full tag history so a tag cannot be released from a commit carrying a stale development version. The release job continues deriving the version from the tag.
- Update `docs/releasing.md` to make "set `Directory.Build.props` to the version being released" an explicit numbered step before tagging, explain the gate, and correct the "Not wired yet" note.
- Update `docs/repo-structure.md` to document `.github/scripts/` in the layout.
- Recapture README images (`docs/images/launch-window.png`, `settings-appearance.png`, `settings-discovery.png`, `settings-agents.png`, plus regenerated `banner.png` and `social-preview.png`) using the documented procedure (150% scaling, crop 2 px DWM ring, 12 px rounded mask, Chrome headless for banner/social) so the version chip shows `0.9.9` instead of `0.9.8`. This is a GUI task performed by the orchestrator; the implementer does not fabricate or hand-edit PNGs.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `ci-build`: Add a requirement that the CI gate fails when the development version in `Directory.Build.props` is behind the highest stable release tag.
- `release-pipeline`: Add a requirement that the release pipeline test job refuses a tag whose commit carries a stale development version (preserving existing scenario names where possible).

## Impact

- `.github/workflows/ci.yml` gains `fetch-depth: 0` checkout and runs the version gate.
- `.github/workflows/release.yml` test job gains `fetch-depth: 0` checkout and runs the same gate.
- A new PowerShell script under `.github/scripts/` performs the comparison and is runnable locally for verification.
- `Directory.Build.props` version bumped from `0.9.8` to `0.9.9`.
- `docs/releasing.md` adds the props-bump step and explains the gate.
- `docs/repo-structure.md` adds `.github/scripts/` to the layout.
- README images recaptured to reflect the current release version `0.9.9`.
- No source code, test suite, or dependency changes.

## Dependencies

None.

## Non-Goals

- Do not switch to MinVer, Nerdbank.GitVersioning, or any other automated version derivation system — the decision to derive the release version from the tag (and keep the development default separate) is deliberate and user-approved.
- Do not change the release workflow's version derivation — it continues to use the tag as the source of truth.
- Do not introduce prerelease version handling — stable releases only, as documented in `docs/releasing.md:92-96`.
