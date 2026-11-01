## 1. State the rule in the release runbook

- [x] 1.1 Add `## 0. Refresh the README screenshots` to `docs/releasing.md` between
      `## Before you start` (`:8`) and `## 1. Write the release notes` (`:13`). It must state: the
      refresh runs before the first release-preparation commit and its PNGs ship in that commit; the
      harness command to run
      (`powershell -NoProfile -ExecutionPolicy Bypass -File docs\images\tools\Capture-ReadmeImages.ps1`);
      the preconditions (CLIHub closed, because the harness refuses to run while the app is running; the
      harness seeds a mock workspace from `-MockWorkspaceRoot`, default `D:\Projects\Acme`, and
      temporarily replaces and then restores `%APPDATA%\CLIHub\config.json` and
      `%APPDATA%\CLIHub\cache\logos.json`); what the default run writes (the launch window, the
      Appearance, Project Scanning, CLI Agents, and Agent Detection pages, plus `banner.png`,
      `workflow.png`, and `social-preview.png` through headless Chrome — `-SkipWebImages` keeps the three
      HTML-rendered images); and the instruction to inspect the regenerated images before committing.
- [x] 1.2 Update the runbook's opening paragraph (`:3-4`) so the list of what a release needs names the
      screenshots alongside the notes, the development version, and the tag.
- [x] 1.3 Add one bullet to `## 5. Verify` (`:83-90`) that the README screenshots show the release's UI.
- [x] 1.4 Keep steps 1–5 and the re-run, rollback, and version-tracking sections unchanged apart from
      cross-references that the new step makes stale; do not renumber the existing steps.
- [x] 1.5 Gate: re-read the edited `docs/releasing.md` end to end and confirm the command matches
      `docs/images/README.md:25-27` verbatim, that every harness claim (app-running refusal, restore,
      mock workspace, `-SkipWebImages`, Chrome) matches
      `docs/images/tools/Capture-ReadmeImages.ps1:39`, `:80-83`, `:85-108`, `:517-526`, and that no
      existing internal link or anchor now points at a wrong section.

## 2. Point the other entry points at the rule

- [x] 2.1 Add a short "Release preparation" note to `docs/images/README.md` directly under the
      regeneration command (`:25-27`) that names runbook step 0 as the trigger and links
      `../releasing.md`; do not duplicate the procedure or the command there.
- [x] 2.2 Add one bullet to `AGENTS.md`'s "Change, Commit, and Archive Policy" (`:61-77`) stating that a
      release-preparation sequence starts with the README screenshot refresh — harness run, output
      committed before any other release-preparation commit — with `docs/releasing.md` as the
      procedure. Place it after the manual-smoke bullet (`:69-71`) and before the "commit contains only
      the change's files" bullet (`:72-73`), so the ordering rule reads next to the other gating rules.
- [x] 2.3 Add a `### Changed` bullet to `CHANGELOG.md`'s `## Unreleased` section (`:11-15`) describing
      the new first release step, matching the file's existing entry shape (`<sentence>`,
      `` (`<change-id>`) `` at the end).
- [x] 2.4 Gate: grep the repository for `Capture-ReadmeImages` and confirm every prose mention either
      gives the command or points at `docs/releasing.md`, and that no second copy of the procedure
      exists.

## 3. Verify wording and run the gates

- [x] 3.1 Re-read the final `docs/releasing.md`, `docs/images/README.md`, `AGENTS.md`, and
      `CHANGELOG.md` at this commit and correct anything that no longer matches the tree (paths, line
      references, command text, `-SkipWebImages` spelling, parameter names).
- [x] 3.2 Confirm the changed files use the repository's documentation style: English, em dashes,
      backtick-fenced paths and commands, straight quotes inside prose, one sentence per line where the
      neighbouring files already do so.
- [x] 3.3 Run `openspec validate --all` and quote the exact result; confirm the change validates with
      `skip_specs: true` and no empty `specs/` directory.
- [x] 3.4 Run `dotnet build CLIHub.sln` and confirm 0 errors (documentation-only change must not affect
      the build).
- [x] 3.5 Run `dotnet test CLIHub.sln` and confirm all tests still pass.
- [x] 3.6 Run `node .opencode/scripts/check-model-policy.mjs` and confirm it reports OK.
- [x] 3.7 Run `graphify update .` if `graphify-out/graph.json` exists.
- [x] 3.8 Report the change to the user and wait for explicit approval before `git commit`; archive only
      with separate explicit approval.
