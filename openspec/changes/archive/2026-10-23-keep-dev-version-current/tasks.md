## 1. Write Version-Check Script

- [x] 1.1 Write `.github/scripts/Check-DevVersion.ps1` with XML parsing to read `<Version>` from `Directory.Build.props`, stable-tag enumeration (`^v(\d+)\.(\d+)(\.(\d+))?$`), SemVer comparison (props >= highest tag), exit 0 on pass / exit 1 on fail, and parameters `-PropsPath`, `-RepoPath`, `-MinVersion` for fixture testing.
- [x] 1.2 Add error handling for missing props file, malformed version, a non-git directory, and a shallow clone that hides tags (detect with `git rev-parse --is-shallow-repository`), with the actionable error messages specified in `design.md`. A non-shallow repository with no stable tag passes by treating the highest version as `0.0.0`; `-MinVersion` skips git enumeration entirely.
- [x] 1.3 Test locally with fixture mode: `pwsh ./.github/scripts/Check-DevVersion.ps1 -MinVersion "0.9.8"` (passes with props at 0.9.8), then `-MinVersion "0.9.9"` (fails with props at 0.9.8, passes after the props bump to 0.9.9), then a missing/ malformed props fixture (fails with the documented message).

**Gate:** Script exits 0 for pass / 1 for fail with clear messages; fixture tests pass.

## 2. Update CI and Release Workflows

- [x] 2.1 In `.github/workflows/ci.yml`, change line 13 `uses: actions/checkout@v7` to `uses: actions/checkout@v7` with `fetch-depth: 0` parameter.
- [x] 2.2 Insert a new step after checkout in `.github/workflows/ci.yml`, before "Setup .NET":
  ```yaml
  - name: Check development version
    shell: pwsh
    run: ./.github/scripts/Check-DevVersion.ps1
  ```
- [x] 2.3 In `.github/workflows/release.yml`, change the test job's checkout (line 15) to `uses: actions/checkout@v7` with `fetch-depth: 0` parameter.
- [x] 2.4 Insert the same version-check step (with the same `./`-prefixed command) in `.github/workflows/release.yml` test job after checkout, before "Setup .NET".

**Gate:** Verify workflow syntax is valid YAML; ensure the version-check step runs before build/test in both workflows.

## 3. Bump Props and Update Documentation

- [x] 3.1 Change `Directory.Build.props:15` from `<Version>0.9.8</Version>` to `<Version>0.9.9</Version>`.
- [x] 3.2 Update `docs/releasing.md`: insert the new step "2. Set the development version" after "1. Write the release notes" with the props-bump instructions and commit example, renumber old steps 2-4 as 3-5, and replace the "Not wired yet" section (lines 92-99) with the revised section documenting the development version tracking rule and moving the unchanged items under "Not wired yet".
- [x] 3.3 Update `docs/repo-structure.md:23`: insert `.github/scripts/` in the layout tree after `.github/workflows/`, with description "Workflow support scripts (version checks, utilities)".

**Gate:** Props version reads `0.9.9`, runbook includes the numbered props-bump step before tagging, repo-structure documents the scripts directory.

## 4. Validation and Verification

- [x] 4.1 Run `openspec validate --all` and verify all capabilities pass.
- [x] 4.2 Run `dotnet build CLIHub.sln -c Release` and verify the build succeeds.
- [x] 4.3 Run `dotnet test CLIHub.sln -c Release` and verify all tests pass.
- [x] 4.4 Test the version-check script with the updated props: `pwsh .github/scripts/Check-DevVersion.ps1` (should exit 0 with props at 0.9.9 and highest tag v0.9.9).
- [x] 4.5 Test the version-check script failure case with fixture: `pwsh .github/scripts/Check-DevVersion.ps1 -MinVersion "0.10.0"` (should exit 1 with actionable message).

**Gate:** Validation passes, build and tests green, version-check script passes with props at 0.9.9 and fails with higher fixture version.

## 5. Screenshot Recapture (Orchestrator-Owned)

**Note:** The implementer does NOT perform this step. The orchestrator completes it after verification gates pass.

- [x] 5.1 Launch the rebuilt dev app against a throwaway configuration and verify the version chip reports `0.9.9` in the launch window and Settings footer.
- [x] 5.2 Capture `docs/images/launch-window.png`, `settings-appearance.png`, `settings-discovery.png`, `settings-agents.png` at 150% display scaling, crop 2 px DWM border, apply 12 px rounded corner mask.
- [x] 5.3 Regenerate `docs/images/banner.png` from `src/banner.html` (Chrome headless, 1.5×, 1280×360 viewport → 1920×540 output) and `docs/images/social-preview.png` from `src/social-preview.html` (2× render downscaled to exactly 1280×640).
- [x] 5.4 Leave the updated images in the working tree for review; the orchestrator commits them only after explicit user approval (no commit is created by this task).

**Gate:** All images show version `0.9.9`; banner and social preview regenerated at 1920×540 and 1280×640; working tree contains the updated PNGs and no commit was made.

## 6. Final Verification

- [x] 6.1 Run `dotnet build CLIHub.sln -c Release` after screenshot commit.
- [x] 6.2 Run `dotnet test CLIHub.sln -c Release` after screenshot commit.
- [x] 6.3 Run `openspec validate --all` after screenshot commit.
- [x] 6.4 Run `pwsh .github/scripts/Check-DevVersion.ps1` to confirm the gate passes with the updated repository state.

**Gate:** Build, tests, validation, and version-check all pass; the change is ready for review.

## Implementation Notes

- Stages 1-4 are implemented and verified locally with PowerShell 7.4 (`pwsh`) and Windows
  PowerShell 5.1 (`powershell.exe`); the script avoids PowerShell 7-only syntax.
- `docs/releasing.md` now introduces the runbook as "the notes, the development version, and the tag"
  so the summary matches the new numbered step. This one-clause edit is the only change beyond the
  text prescribed by `design.md`; the rest of the runbook follows the design wording, re-wrapped to
  the document's line length.
- An invalid `-MinVersion` value fails with
  `Version '<value>' supplied to -MinVersion is not a valid SemVer version.` The design does not
  specify this message, so it mirrors the malformed-props wording.
- Stages 5 and 6 are deferred to the orchestrator: the screenshot recapture, then the re-runs of
  build, tests, validation, and the version check after the images are committed.
