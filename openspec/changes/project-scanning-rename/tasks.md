# Tasks: project-scanning-rename

Rename only — no behavior, layout, binding, icon, key, or identifier change at any stage. Every edit
is a text substitution on the exact lines listed in design (tables U/T/C/D). The working tree carries
unrelated uncommitted icon edits (`SettingsWindow.xaml:252`, `SettingsViewModel.cs:349`,
`CHANGELOG.md:24`) — never touch those hunks. Every gate must pass before the next stage.

## 1. Stage 1 — verify the prepared change

- [x] 1.1 Confirm the working tree is still HEAD `efd77f0` plus only the icon edits and untracked
      scratch HTML (`git status --short`), and that no `src/`, `tests/`, or `openspec/specs/` file is
      modified yet.
- [x] 1.2 Re-run the occurrence census: `rg -n -i "project discovery" README.md RELEASE-NOTES.md
      CHANGELOG.md docs/ src/ tests/ openspec/specs/ openspec/plans/ ui/` — the live hits must match
      design tables U/C/S/D plus the kept-verbatim lines (glossary `:38,:42`, `README.md:49`,
      `project-context.md:46`, `CHANGELOG.md:98,108,110`, `RELEASE-NOTES.md:59,72`). Any new live
      occurrence: stop, record it, and extend the design table before editing.
- [x] 1.3 Faithfulness check of the three delta files against the main specs: each MODIFIED
      requirement must differ from `openspec/specs/<capability>/spec.md` only in the page-name
      phrases (design table S); every requirement name and scenario name byte-identical; no
      requirement omitted from a carried block.
- [x] 1.4 Gate: `openspec validate --all` passes (pre-existing warnings elsewhere unchanged).

## 2. Stage 2 — user-visible strings and the navigation test

- [x] 2.1 `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:307-308` — change the page title to
      `"Project Scanning"` and the description to `"Watched directories, scanning rules, and the
      background watcher."`. Touch nothing else in the catalog entry (key `"projects"` stays).
- [x] 2.2 `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:54` — "managed by Project Discovery" →
      "managed by Project Scanning".
- [x] 2.3 `src/CLIHub/Views/SettingsWindow.xaml:225` — `Detail="Managed by Project Discovery"` →
      `Detail="Managed by Project Scanning"`. Do not touch the icon lines (`:200`, `:226`, `:252`).
- [x] 2.4 `tests/CLIHub.Tests/ViewModels/Settings/SettingsNavigationTests.cs:51,55` — rename the
      test to `Catalog_ProjectsPageIsNamedProjectScanning` and assert `"Project Scanning"`.
- [x] 2.5 Gate: `dotnet build CLIHub.sln` succeeds with zero errors; `dotnet test
      CLIHub.sln` passes (at minimum `dotnet test --filter SettingsNavigationTests`, then the full
      suite); `git diff` shows only the four hunks above plus the untouched icon hunks.

## 3. Stage 3 — internal comments and XML docs

- [x] 3.1 `src/CLIHub/Views/SettingsWindow.xaml:152,182` — comments "Project Discovery actions…" →
      "Project Scanning actions…" and "Project Discovery summary cards…" → "Project Scanning summary
      cards…".
- [x] 3.2 `src/CLIHub/Settings/SettingsApplicationService.cs:133,315` — "…on the Project Discovery
      page…" → "…on the Project Scanning page…" (two XML-doc lines).
- [x] 3.3 `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:479,929,979` — "…Project Discovery
      page…" → "…Project Scanning page…" (three XML-doc lines).
- [x] 3.4 `src/CLIHub.Core/Scanning/IProjectScanner.cs:50` — "…on the Project Discovery page." →
      "…on the Project Scanning page.".
- [x] 3.5 `src/CLIHub/ViewModels/Projects/ProjectModel.cs:110` — "managed by Project Discovery." →
      "managed by Project Scanning.".
- [x] 3.6 Gate: `dotnet build CLIHub.sln` succeeds; `rg -n "Project Discovery" src/ tests/` returns
      nothing.

## 4. Stage 4 — docs, changelog, release notes

- [x] 4.1 `README.md:40,85,88` — page name → "Project Scanning" and "discovery rules" → "scanning
      rules" in the caption (D1–D3); `README.md:38` alt text "…and discovery rules" → "…and scanning
      rules" (D17). Keep `README.md:49` ("Automatic project discovery") unchanged.
- [x] 4.2 `docs/architecture/scanning.md:48,63,82,90` — page-name references → "Project Scanning"
      (D4–D7); keep every "discovery mode"/domain phrase.
- [x] 4.3 `docs/architecture/ui.md:53` — "on Project Discovery," → "on Project Scanning," (D8).
- [x] 4.4 `docs/images/README.md:13,54` — page name → "Project Scanning" (D9–D10). Do not rename or
      recapture `settings-discovery.png`; its row keeps the old file name.
- [x] 4.5 `docs/glossary.md:40,54,127,131,141` — page references and the preference-area entry →
      "Project Scanning"/"project scanning" (D11–D15). Keep `## Project discovery` (`:38`) and the
      term definition (`:42`).
- [x] 4.6 `docs/project-context.md:47` — "from the Project Discovery page" → "from the Project
      Scanning page" (D16); keep `:46` (domain concept).
- [x] 4.7 `CHANGELOG.md` — add one `Changed` line under `## 1.0.1 — 2026-10-24` (after the existing
      uncommitted icon line at `:24`, which stays byte-identical): the Settings page "Project
      Discovery" is renamed "Project Scanning" with the "scanning rules" description; page key,
      configuration keys, and behavior unchanged (`preferences-ui`, `settings-theme`,
      `project-management`).
- [x] 4.8 `RELEASE-NOTES.md` — add one `Improved` line under `## 1.0.1` (D19). Leave the released
      0.9.7 lines `:59,72` untouched.
- [x] 4.9 Gate: `rg -n -i "project discovery" -g "!openspec/changes/archive/**" -g "!graphify-out/**"
      -g "!*.html" -g "!openspec/changes/project-scanning-rename/**" .` returns only the
      kept-verbatim lines (glossary `:38,:42`, `README.md:49`, `project-context.md:46`,
      `CHANGELOG.md:98,108,110`, `RELEASE-NOTES.md:59,72`); `openspec validate --all` passes.

## 5. Stage 5 — whole-change verification and graph refresh

- [x] 5.1 Gate: `dotnet build CLIHub.sln` — zero errors.
- [x] 5.2 Gate: `dotnet test CLIHub.sln` — zero failures (same expectation as HEAD).
- [x] 5.3 Gate: `openspec validate --all` passes; pre-existing warnings elsewhere unchanged.
- [x] 5.4 Run `node .opencode/scripts/check-model-policy.mjs` (unchanged policy baseline).
- [x] 5.5 Run `graphify update .` so the knowledge graph reflects the rename and the new change
      directory.
- [x] 5.6 Review `git status --porcelain` / `git diff`: only the files in design tables U/T/C/D are
      modified or added; the three icon hunks (`SettingsWindow.xaml:252`,
      `SettingsViewModel.cs:349`, `CHANGELOG.md:24` icon line) are untouched.

## 6. Stage 6 — manual smoke, then commit/archive with explicit approval

- [x] 6.1 Run the app (`dotnet run --project src/CLIHub/CLIHub.csproj`), open Settings, and confirm:
      the sidebar item reads "Project Scanning" under "Engines & Repos"; the pinned page header
      shows the title "Project Scanning" and the description "Watched directories, scanning rules,
      and the background watcher."; the page-header "Scan Now" action is present; the Projects page
      hint reads "Discovered projects are managed by Project Scanning and are read-only here."; the
      Projects page DISCOVERED summary card reads "Managed by Project Scanning"; and no visible
      "Project Discovery" string remains anywhere in the UI.
- [ ] 6.2 Record the smoke result in the change report. `git commit` and `openspec archive
      project-scanning-rename` MUST wait until the user confirms the smoke and then gives explicit
      approval (repo policy; approval for one never carries to the other).
- [ ] 6.3 On approved archive: verify the three main specs show the renamed page with all
      requirement/scenario names intact, and run a final `openspec validate --all`.
