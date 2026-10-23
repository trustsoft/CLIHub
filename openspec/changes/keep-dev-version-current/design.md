## Context

### Current State

**Version Sources (verified):**
- Release version: `.github/workflows/release.yml:44-59` derives from pushed tag; line 92 passes `-p:Version=${{ steps.version.outputs.version }}` to `dotnet publish`.
- Development version: `Directory.Build.props:15` declares `<Version>0.9.8</Version>`.
- Version reporting: `src/CLIHub.Core/Updates/UpdateService.cs:121-133` reads `AssemblyInformationalVersionAttribute` (fallback to "unknown"), fed by the props version for dev builds.
- Latest release tag: `v0.9.9` exists; `v0.9.8` preceded it.
- Last props bump: commit `7b94aa2 release: prepare 0.9.8`; no commit ever set 0.9.9 (`git log -S'<Version>0.9.9</Version>' -- Directory.Build.props` returns empty).
- Current runbook: `docs/releasing.md:98-99` notes "bump it to the next version as part of the new cycle's work" under "Not wired yet" with no enforcement.
- Screenshots: `docs/images/launch-window.png`, `settings-appearance.png`, `settings-discovery.png`, `settings-agents.png` show `0.9.8` version chips (committed in `6c65290`).

**Workflow Checkout:**
- `.github/workflows/ci.yml:12-13` uses `actions/checkout@v7` with default shallow fetch (no tag history).
- `.github/workflows/release.yml:14-15` (test job) and `:36-37` (release job) also use shallow checkout; only the release job needs tags for version derivation, but the test job runs first and could gate on props freshness.

**Repository Layout:**
- `docs/repo-structure.md:23` documents `.github/workflows/` but does not list a `.github/scripts/` directory (none currently exists).

### Problem

The development version has drifted one release behind because the manual bump step is not enforced. Users and contributors see an outdated version in dev builds, and the drift can persist across multiple releases if the step is forgotten.

## Goals / Non-Goals

**Goals:**
- Enforce that the development version in `Directory.Build.props` is never older than the highest stable release tag.
- Add an automated CI gate so drift is caught on pull requests and master pushes.
- Block release tags from commits carrying a stale development version.
- Keep the script testable locally with fixtures/parameters for verification without mutating the repository.
- Recapture README images to reflect the current release version `0.9.9`.
- Make the props-bump step explicit and required in the release runbook.

**Non-Goals:**
- Do not replace the tag-driven release version with automated version tooling (MinVer, Nerdbank.GitVersioning).
- Do not change how the release workflow derives the version — the tag remains the source of truth.
- Do not introduce prerelease version handling (stable-only decision per `docs/releasing.md:92-96`).
- Do not automate props-bump itself — the manual step becomes enforceable, not automatic.

## Decisions

### Version-Check Script

**Location:** `.github/scripts/Check-DevVersion.ps1` (new file; PowerShell is the workflows' native shell and needs no new package dependencies).

**Contract:**
- Read `<Version>X.Y.Z</Version>` from `Directory.Build.props` (XML parsing or regex; handle missing file/malformed version gracefully).
- Enumerate git tags matching `v<major>.<minor>[.<patch>]` (stable releases; ignore prerelease suffixes like `-beta.1` per the stable-only decision).
- Determine the highest stable tag by SemVer comparison (treat `v1.2` as `1.2.0` for comparison).
- Compare props version >= highest tag version (equal is allowed; the dev version can match the latest release during active work).
- Exit 0 when props >= highest tag; exit 1 with an actionable message when props < highest tag.
- Exit 1 with a clear error when tags cannot be enumerated (shallow clone with no tags, no git repo).
- Support parameters for testability:
  - `-PropsPath <path>` (default: `Directory.Build.props` at repo root) — override for fixture testing.
  - `-RepoPath <path>` (default: current directory) — git repository location.
  - `-MinVersion <version>` (optional fixture) — override the git-tag-derived highest version for local verification without pushing tags.

**Stable Tag Selection:**
- Match `^v(\d+)\.(\d+)(\.(\d+))?$` (major.minor or major.minor.patch after stripping leading `v`).
- Ignore tags with prerelease suffixes (`-alpha`, `-beta.1`, etc.) — regex must not match those.
- When the repository is a normal (non-shallow) clone and no stable tags exist, treat the highest version as `0.0.0` (props version must be >= 0.0.0, effectively always passing in a fresh repo).

**Missing Tags Behavior:**
- The script distinguishes three cases so an absent tag history cannot silently pass:
  1. `git tag` cannot run (not a git repository, git missing, corrupted repository) — exit 1 with the actionable tag-history message.
  2. The repository is shallow (`git rev-parse --is-shallow-repository` reports `true`) and no stable tag is visible — exit 1, because the absence of tags is an artifact of the shallow checkout, not a fresh repository.
  3. The repository is not shallow and genuinely has no stable `v*` tag — treat the highest version as `0.0.0` and pass. This is the only case where an empty tag list is legitimate.
- The CI and release test jobs must use `fetch-depth: 0` to provide full history; the script does not silently pass when a shallow checkout hides the tags.
- When `-MinVersion` is supplied for fixture testing, skip git tag enumeration entirely and compare against that value.

**Error Messages:**
- Props < highest tag: `"Development version <props-version> in Directory.Build.props is behind the highest release tag <tag-version>. Update Directory.Build.props to <tag-version> or higher before merging."`
- Tags unavailable: `"Cannot enumerate release tags. Ensure the repository has full tag history (fetch-depth: 0) and contains at least one stable release tag."`
- Props file missing/unreadable: `"Directory.Build.props not found or version property is missing."`
- Malformed version: `"Version '<value>' in Directory.Build.props is not a valid SemVer version."`

### CI Workflow Integration

**`.github/workflows/ci.yml`:**
- Add `fetch-depth: 0` to the `actions/checkout@v7` step (line 13).
- Insert a new step after checkout, before "Setup .NET":
  ```yaml
  - name: Check development version
    shell: pwsh
    run: .github/scripts/Check-DevVersion.ps1
  ```
- The gate runs for all pull requests and pushes to master.

**`.github/workflows/release.yml`:**
- Test job: add `fetch-depth: 0` to checkout (line 15) and insert the version-check step after checkout, before "Setup .NET":
  ```yaml
  - name: Check development version
    shell: pwsh
    run: .github/scripts/Check-DevVersion.ps1
  ```
- Release job: keep existing shallow checkout (no change needed; it already fetches the tag for version derivation).
- The test job gate ensures a tagged commit cannot release if its props version is stale; the failure happens before packaging.

### Operational Rule

**Props Version Maintenance:**
- The development version in `Directory.Build.props` MUST be set to the version being released (matching the tag) before the tag is pushed.
- This makes it an explicit numbered step in `docs/releasing.md`, inserted between "Write the release notes" and "Tag and push".
- The CI gate enforces props >= highest tag, so a commit bumped to the release version (equal) passes, and a commit ahead of the release version (e.g., bumped to the next planned version) also passes.
- After a release, the props version can remain at the released version or be bumped ahead to the next planned version — both satisfy the constraint; the runbook documents the recommended timing.

### Documentation Updates

**`docs/releasing.md`:**
- Insert a new numbered step after step 1 ("Write the release notes") and before step 2 ("Tag and push"):
  ```markdown
  ## 2. Set the development version
  
  Update `Directory.Build.props` to match the version being released:
  
  ```xml
  <Version>0.9.0</Version>
  ```
  
  Commit to `master` (for example `release: set version 0.9.0`) and wait for CI to go green. The automated version check ensures this step is not skipped — a stale version in the props file will block the release pipeline's test job.
  
  After the release is published, you may bump the props version to the next planned version (for example `0.10.0`) as part of the new cycle's work, or leave it at the released version until the next release preparation begins. Both satisfy the constraint (props >= highest release tag).
  ```
- Renumber existing steps 2-4 as 3-5.
- Replace the "Not wired yet" section (lines 92-99) with:
  ```markdown
  ## Development version tracking
  
  The `Directory.Build.props` version is the development default (what a dev-run build reports). It MUST be equal to or ahead of the highest stable release tag — this is enforced by the CI and release pipelines through `.github/scripts/Check-DevVersion.ps1`.
  
  ## Not wired yet
  
  - **Prerelease channels** — a tag like `v0.9.0-beta.1` would pass the version check but publish as a stable release; do not use prerelease tags until a beta channel change exists (stable-only decision, 2026-10-03).
  - **Code signing** — packages are unsigned, so SmartScreen may warn on first install.
  ```

**`docs/repo-structure.md`:**
- Insert `.github/scripts/` into the layout tree at line 23, after `.github/workflows/`:
  ```text
  ├── .github/
  │   ├── workflows/        # CI and tag-driven release workflows
  │   └── scripts/          # Workflow support scripts (version checks, utilities)
  ```

### Screenshot Recapture

**Procedure (orchestrator-owned task):**
- Launch the dev build (after props bump to 0.9.9); screenshots must show the actual running application with the updated version chip.
- Capture at 150% Windows display scaling (standard high-DPI reference).
- Crop 2 px DWM border (outer shadow ring).
- Apply 12 px rounded corner mask to match the design system.
- `docs/images/launch-window.png`, `settings-appearance.png`, `settings-discovery.png`, `settings-agents.png` — capture from running app.
- `docs/images/banner.png` — regenerate from `docs/images/src/banner.html` with Chrome headless at `--force-device-scale-factor=1.5 --window-size=1280,360` (produces the current 1920×540 asset).
- `docs/images/social-preview.png` — regenerate from `docs/images/src/social-preview.html` at 2× (`--force-device-scale-factor=2 --window-size=1280,640`) and downscale to exactly 1280×640.
- The implementer does NOT fabricate, hand-edit, or Photoshop these images; the orchestrator performs the GUI capture after the build is verified.
- Images are updated in the working tree only. They are committed by the orchestrator after explicit user approval; no task may create a commit on its own.

## Risks / Trade-offs

- **Manual props bump is still required** → The gate enforces it but does not automate it; this preserves the approved tag-driven version model and avoids introducing MinVer/Nerdbank tooling the user explicitly rejected.
- **CI jobs fetch full history** → Adds ~1-2 seconds to checkout; acceptable for the enforcement benefit.
- **Equal versions are allowed** → The props version can match the latest release (common during development of the release commit itself); the check is `>=`, not `>`.
- **Script must handle edge cases** → Missing tags (shallow clone), malformed versions, missing props file — script fails clearly rather than passing silently.
- **Screenshots are blocking** → The implementer cannot complete this task without the orchestrator's GUI capture; the tasks file marks this dependency explicitly.

## Alternatives Considered

1. **Automated props bump (GitVersion/MinVer/Nerdbank)** → Rejected by user; the approved approach keeps the tag as the version source and makes the props bump enforceable, not automatic.
2. **Post-commit hook for props bump** → Hooks are local and not enforced in CI; the CI gate is more reliable.
3. **Block merge instead of block CI** → GitHub branch protection already uses CI status; no separate merge gate needed.
4. **Strict equality (props == highest tag)** → Rejected; props can be bumped ahead to the next version during active development, and that should pass the gate.

## Migration Plan

1. Write `.github/scripts/Check-DevVersion.ps1` with the contract above.
2. Test locally with fixtures (`-MinVersion "0.9.9"` override) to verify pass/fail logic without pushing tags.
3. Update `.github/workflows/ci.yml` (add `fetch-depth: 0` and version-check step).
4. Update `.github/workflows/release.yml` test job (add `fetch-depth: 0` and version-check step).
5. Bump `Directory.Build.props` to `<Version>0.9.9</Version>`.
6. Update `docs/releasing.md` (insert props-bump step, revise "Not wired yet" section).
7. Update `docs/repo-structure.md` (add `.github/scripts/` to layout).
8. Build and test locally; run `openspec validate --all`.
9. Orchestrator recaptures screenshots with the documented procedure and regenerates banner/social images.
10. Full build/test/validate after screenshot commit.

Rollback: remove the version-check steps from workflows, revert the script file, restore `docs/releasing.md` and `docs/repo-structure.md`; the props bump to 0.9.9 can remain (it is correct and not harmful).

## Open Questions

None.
