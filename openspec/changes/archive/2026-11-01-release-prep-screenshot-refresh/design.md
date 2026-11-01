## Verified current state (2026-11-01, at `24f4be9`)

- `docs/releasing.md` is the release runbook and opens with "your job is the notes, the development
  version, and the tag" (`:1-6`). Its steps are `1. Write the release notes` (`:13`), `2. Set the
  development version` (`:46`), `3. Tag and push` (`:62`), `4. Watch the pipeline` (`:73`), and
  `5. Verify` (`:83`). Screenshots are not mentioned anywhere in the file.
- `docs/images/README.md:17-37` documents "Regenerate all README images" with the exact command
  `powershell -NoProfile -ExecutionPolicy Bypass -File docs\images\tools\Capture-ReadmeImages.ps1`
  (`:25-27`) and its parameters (`:29-37`). `:73-85` documents the app-capture path. Neither file says
  *when* the refresh is required.
- The harness (`docs/images/tools/Capture-ReadmeImages.ps1`) refuses to run while CLIHub is running
  (`:80-83`), backs up and restores `%APPDATA%\CLIHub\config.json` and `%APPDATA%\CLIHub\cache\logos.json`
  (`:85-108`, `Restore-File`), seeds a mock workspace that defaults to `D:\Projects\Acme` (`:39`, `:110-111`)
  and deletes only a root it created itself, requires `OutputDirectory` to be inside the repository
  (`:65-69`), applies the 12 px rounded-corner mask (`:508`), and renders `banner.png`, `workflow.png`,
  and `social-preview.png` with headless Chrome unless `-SkipWebImages` is passed — throwing
  "Chrome was not found" otherwise (`:517-526`).
- `AGENTS.md:61-77` holds the commit/archive policy and contains no release-preparation rule.
- `CHANGELOG.md:11` has an `## Unreleased` section with `### Changed` (`:13`) and `### Fixed` (`:17`).
- Evidence that the drift is not hypothetical: the committed Settings screenshots were refreshed on
  2026-11-01 only because a sidebar change happened to require it (`24f4be9`), and before that refresh
  they still carried the "Used when a field is empty" defaults-card copy that `a3c2ebf` had replaced,
  along with their original 220 DIP sidebar width.

## Decisions

### Decision 1 — The rule lives in the runbook, as a numbered step before the notes

`docs/releasing.md` is the canonical "how a release is formed" document (`docs/project-context.md:83`
routes "Release procedure" there, and `docs/changelog-and-release-notes.md:28-31` defers release
mechanics to it). The rule therefore goes there, as `## 0. Refresh the README screenshots`, numbered
before `1. Write the release notes` so the reading order matches the required order of operations.

### Decision 2 — "Before the first release-preparation commit", not "before tagging"

Tagging is step 3; a stale image committed in step 1 or 2 would still be the image the tag points at.
Putting the refresh first makes the requirement checkable at the moment it matters — the first commit
of the release-preparation sequence — and lets the refreshed PNGs ride in that commit instead of
producing a separate late commit that is easy to forget when the notes or version need a follow-up fix.

### Decision 3 — No CI gate

Alternative considered: a workflow job that re-renders and fails on a diff. Rejected because the
captures embed the scan time and installed agent versions, so two runs never produce identical bytes;
a diff job would fail on every run and be disabled within a week. Alternative considered: a
`CHANGELOG`-style checklist assertion. Rejected because it verifies prose, not pixels. The cost of the
manual step is one command and one visual review, which is why the runbook now states it.

### Decision 4 — Mirror the rule in `AGENTS.md` and `docs/images/README.md`

Agents cut releases in this repository, so the rule has to be visible where they read policy
(`AGENTS.md`) and where they read about the images (`docs/images/README.md`). Both are one-line
statements that point back to `docs/releasing.md` for the procedure, so there is one canonical
description and no duplicated commands.

### Decision 5 — No spec delta

`release-pipeline` and `ci-build` describe what the automated pipeline does with a pushed `v*` tag.
Refreshing README screenshots is a human preparation step on `master` before the tag exists; encoding
it as a pipeline requirement would describe behavior the pipeline does not and should not have.
`.openspec.yaml` therefore sets `skip_specs: true`, matching the `testing-conventions-doc` precedent.

## Alternatives not taken

- **Refresh inside the release workflow.** Rejected: CI cannot commit to `master`, and the images must
  be reviewed by a human before they represent the product.
- **A pre-commit or pre-push git hook.** Rejected: hooks are local, unhooked clones bypass them, and a
  ~1–2 minute capture on every commit is unacceptable; the trigger is release preparation, not pushing.
- **Refresh only when the UI changed.** Rejected: it requires a judgement call about whether a diff is
  visually relevant, and the "did it change?" check costs the same as the refresh itself.

## Open questions

- None blocking. If a future change automates capture in CI with a deterministic seed (fixed scan time,
  pinned agent versions), the "no CI gate" decision should be revisited.
