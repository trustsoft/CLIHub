## Why

The README screenshots (`docs/images/launch-window.png` and the four `settings-*.png` files) are the
product's shop window, and nothing in the release process keeps them current. `docs/releasing.md`
currently asks only for the release notes, the development version, and the tag, so a release can ship
with screenshots that no longer match the UI a user installs. The drift is real and recent: the
committed Settings screenshots were stale for the whole `1.0.4` line — they still carried the replaced
defaults-card copy ("Used when a field is empty"), and they only got refreshed incidental to a
one-attribute sidebar change (`24f4be9`).

The refresh is cheap to run (`docs/images/tools/Capture-ReadmeImages.ps1`, one command, ~1–2 minutes, no
unlocked desktop needed) and it must happen before the release is cut, not after, because the tag points
at a commit: whatever the tagged tree contains is what every installer and every GitHub visitor sees.

## What Changes

- Add a new first step, `## 0. Refresh the README screenshots`, to `docs/releasing.md`, ahead of the
  release notes and the development version. The rule it states: the screenshot regeneration runs
  **before the first release-preparation commit**, and the refreshed PNGs ship in that commit. The step
  carries the exact harness command, its preconditions (CLIHub closed; the harness temporarily swaps
  `%APPDATA%\CLIHub\config.json` and `cache\logos.json` and restores them), what it writes (the launch
  window, the Appearance / Project Scanning / CLI Agents / Agent Detection pages, and, unless
  `-SkipWebImages` is passed, the banner, workflow, and social-preview images), and the instruction to
  eyeball the output before committing.
- Update the runbook's opening sentence and its `## 5. Verify` checklist so the screenshots are named
  as part of the release's job and as a thing to check before tagging.
- Add a "Release preparation" note to `docs/images/README.md` pointing at runbook step 0, so a reader
  who lands in the image directory sees the obligation without having to find the runbook.
- Add one bullet to `AGENTS.md`'s "Change, Commit, and Archive Policy" stating the same rule for agents:
  the harness runs and its output is committed before any other release-preparation commit.
- Record the change in `CHANGELOG.md` under `## Unreleased` → `Changed`.

## Non-Goals

- No CI enforcement that the committed screenshots are fresh. The captures are not byte-stable — they
  contain the scan time ("Last scan 21:42:31") and installed agent versions — so a diff-or-fail job
  cannot distinguish a stale image from a re-render, and rendering inside Actions adds a Chrome and GPU
  dependency to CI for no guarantee. The obligation stays a reviewed human step.
- No change to `docs/images/tools/Capture-ReadmeImages.ps1`, the debug-only `CLIHUB_CAPTURE_DIR` capture
  mode, or `.github/workflows/`.
- No change to the `release-pipeline` or `ci-build` specs: those describe what the tagged build does,
  not how a human prepares the commit that gets tagged. `.openspec.yaml` sets `skip_specs: true`.
- No new release-time smoke task and no change to the tag/publish mechanics.

## Impact

- Affected: `docs/releasing.md`, `docs/images/README.md`, `AGENTS.md`, `CHANGELOG.md`. Documentation and
  agent-instruction prose only.
- No source, test, project-file, build-configuration, or workflow changes; no durable behavior-contract
  change.
- No dependency on other open changes.

## Dependencies

- None. The harness, its parameters, and its restore behavior already exist and were exercised on
  2026-11-01 (`24f4be9`).
