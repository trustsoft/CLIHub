# Design

## Verified current state

### The capability and its drift

- `openspec/specs/main-window-layout/spec.md` (311 lines) holds **12 requirements** and **64 scenarios**
  (counted with `rg -c '^### Requirement: '` = 12 and `rg -c '^#### Scenario: '` = 64 on 2026-10-24; the task
  brief said "11 requirements, ~40 scenarios" — see Deviations).
- "main window" appears 9 times: the Purpose (line 5) and eight scenario WHEN lines — 14, 34, 42, 46, 50, 58,
  170, 178. Lines 118, 218, 242, 246, 250, 266 already say "the launch window" (the "Window chrome"
  requirement is fully correct), so the spec is internally inconsistent — exactly the backlog item's evidence.
- No requirement name and no scenario name contains "main window", so names can stay byte-identical while only
  text changes.

### The window the spec actually describes

- `LaunchWindow` is the only window class registered in DI: `services.AddSingleton<LaunchWindow>();` at
  `src/CLIHub/Composition/ServiceRegistration.cs:113` (inside `AddLaunchWindowServices`, line 100).
- The launch window is assigned as the WPF application main window: `context.SetMainWindow(startupUi.LaunchWindow);`
  at `src/CLIHub/Startup/ApplicationBootstrapper.cs:68` — the WPF-role fact that leaked into the spec prose.
  `docs/architecture/startup.md:33` documents the same step ("Assigns the session's launch window to
  `Application.MainWindow`").
- `MainWindow` (`src/CLIHub/Views/MainWindow.xaml.cs:25`) is an unregistered legacy reference: codegraph shows
  only self-references within `MainWindow.xaml.cs`, no DI registration, no instantiation elsewhere
  (blast radius: 2 callers, both in its own file). `docs/project-context.md:51` and `docs/architecture.md:38`
  already document it as retained and unregistered. Untouched by this change.

### The backlog item

- `docs/backlog.md:227-233`: "`main-window-layout` names the wrong window", Severity Medium (documentation
  debt), remedy "rename or annotate the capability in the next spec-touching change", trigger "any
  `main-window-layout` edit". The user chose option B (rename + wording fix); option C (delete the legacy
  `MainWindow` class) stays parked.

### Tooling (openspec 1.15.0, npm global install)

- Delta sections recognized: `## ADDED|MODIFIED|REMOVED|RENAMED Requirements`
  (`dist/core/parsers/requirement-blocks.js:106-110`, `dist/core/archive.js:1235`).
- REMOVED entries may be header-only `### Requirement: <name>` blocks or bullet form
  (`dist/core/parsers/change-parser.js:7-10, 119-129`).
- `## RENAMED Requirements` renames a **requirement within a capability** (`parseRenamedPairs`,
  `requirement-blocks.js:128`); there is no capability-level rename operation — the capability id *is* the
  `specs/<id>/` directory name.
- Retirement: when a change's REMOVED entries take a capability's last requirement, `openspec archive` deletes
  the emptied main spec **only if** the change's `.openspec.yaml` declares `retire_capabilities: true`
  (`dist/utils/change-metadata.js:246-256`, `METADATA_FILENAME = '.openspec.yaml'` at line 8; the gate and its
  diagnostics in `dist/core/archive.js` around lines 1495-1560). Retirement is refused only if the old spec
  holds "unaccounted" content the merge cannot map; our spec is the standard
  `# … Specification` / `## Purpose` / `## Requirements` shape, all accounted.
- A delta's `## Purpose` seeds the Purpose when the capability is created: `dist/core/specs-apply.js:237-287`
  (`extractPurposeSection(changeContent)` → `buildSpecSkeleton(specName, changeName, deltaPurpose)`). Without
  it the new spec gets the placeholder Purpose that `validate` flags (seen live on
  `agent-availability-display` in this repo).
- Change metadata keys allowed in `.openspec.yaml`: `schema, created, goal, affected_areas, initiative,
  skip_specs, retire_capabilities` (`dist/core/change-metadata/schema.js:24,51`); `schema: spec-driven` matches
  `openspec/config.yaml:1`. All 150+ archived changes in this repo carry `.openspec.yaml` with
  `schema: spec-driven` + `created:`; none has used `retire_capabilities` yet (this is the first).

## Decision 1 — Rename mechanics

Express the rename as **create + retire**, not RENAMED:

1. `specs/launch-window-layout/spec.md` (in this change): `## Purpose` (seeds the new capability's Purpose at
   creation) + `## ADDED Requirements` with all 12 requirements, wording fixed.
2. `specs/main-window-layout/spec.md` (in this change): `## REMOVED Requirements` with header-only entries for
   the same 12 requirement names.
3. `.openspec.yaml` with `retire_capabilities: true` so `openspec archive` deletes the emptied
   `openspec/specs/main-window-layout/` instead of aborting with "must have at least one requirement".

On archive this creates `openspec/specs/launch-window-layout/spec.md` (Purpose + 12 requirements) and removes
the old spec directory; `openspec validate --all` stays green before and after.

Rationale: `## RENAMED Requirements` cannot express a capability rename (it renames requirements inside one
capability); hand-renaming the directory would bypass the tool and lose the change history the archive
mechanism records. The create+retire pair is the tool-supported path verified above.

Alternatives rejected:

- **`## RENAMED Requirements`**: wrong granularity — it maps an old requirement name to a new one inside the
  same capability; our requirement names do not change.
- **Manually `git mv openspec/specs/main-window-layout openspec/specs/launch-window-layout` and edit in
  place**: `openspec archive`/`sync` never sees the rename, no archived delta records the transition, and the
  repo policy routes durable spec changes through OpenSpec.
- **`skip_specs: true` + hand edit of the main specs**: not applicable — the deltas are the whole change.

## Decision 2 — Wording scope

- The 8 WHEN lines and the Purpose are the only text changes; a `Compare-Object` diff of the prepared ADDED
  block against the current requirements body (lines 9-311 of the old spec) shows exactly the 8 WHEN lines
  changed and nothing else. Assertion text, requirement text, requirement order, and all names are
  byte-identical.
- New Purpose names the launch window, its source file, and folds in the option-A annotation: "the legacy
  `MainWindow` class is an unregistered build-only reference, not the subject of this capability."
- No requirement or scenario renames (confirmed: none of the names contains "main window"). The "Window
  chrome" requirement already says "launch window" and is carried over unchanged.

## Decision 3 — Cross-references (complete grep of the repo, 2026-10-24)

References to `main-window-layout` outside `openspec/changes/archive/`:

| File:line | Kind | Decision |
|---|---|---|
| `openspec/specs/main-window-layout/spec.md:1` | the capability | replaced by this change's deltas |
| `docs/backlog.md:227,233` | the backlog item driving this change | mark resolved, following the "… (resolved)" pattern at `docs/backlog.md:212-223` |
| `ui/mockups/README.md:71` | capability id in the delivered-features list | update id to `launch-window-layout` |
| `openspec/plans/2026-10-21-test-suite-hardening-roadmap.md:81` | live draft roadmap (status: "draft (baseline measured; change sequence not yet approved)") maps four scenario names to a future test workstream | update id; scenario names are unchanged so the mapping stays valid |
| `openspec/plans/2026-10-10-settings-window-roadmap.md:105` | completed, dated snapshot (status: "выполнен") listing specs current at 2026-10-10 | leave as-is (historical snapshot) |
| `CHANGELOG.md:35,263,341,344,358` | released-version history (1.0.0/1.0.1 sections) | leave as-is; the release-procedure entry for the version that ships this change will cite `launch-window-layout` |

Verified non-references: `docs/architecture.md`, `docs/architecture/ui.md`, `docs/project-context.md`,
`README.md`, and `RELEASE-NOTES.md` contain neither the id nor "main window" prose. `docs/architecture.md:38`
and `docs/project-context.md:51` already state the `MainWindow`-is-retained fact correctly; ADR 0005 and
`docs/architecture/startup.md:33` reference the WPF `Application.MainWindow` property, which is correct usage.

Out-of-scope "main window" prose in other capabilities (enumerated, not edited — none references the renamed
capability): `openspec/specs/app-lifecycle/spec.md:23,99,103,107` and
`openspec/specs/preferences-ui/spec.md:96`. The drift is identical in kind; rewording those specs is a
behavior-text change to other capabilities and would grow this change beyond its mandate. Recorded here and in
the proposal's Non-goals; a separate small change can normalize them.

## Deviations from the brief

1. **Requirement/scenario counts**: the brief said "11 requirements, ~40 scenarios"; the file has **12
   requirements and 64 scenarios** (rg counts above, requirement list at lines 9, 25, 37, 53, 81, 137, 165,
   201, 213, 237, 261, 273). Both deltas therefore cover 12 requirements; the mechanics are unaffected.
2. **Candidate cross-references**: the brief suggested `docs/architecture.md`, `docs/architecture/ui.md`, and
   `docs/project-context.md` as likely followers; none references the capability (verified by grep). The real
   followers are `ui/mockups/README.md`, the live 2026-10-21 plan, and `docs/backlog.md` (Decision 3).
3. No other deviations: `ServiceRegistration.cs:113`, `ApplicationBootstrapper.cs:68`, and the
   `MainWindow`-is-legacy facts all check out as stated.

## Open questions

1. **CHANGELOG entry timing** — add a `Changed` line under the next version heading as part of this change, or
   leave it to the release procedure (`docs/releasing.md` cuts version sections; the 1.0.1 section was cut the
   same day its changes archived). Default assumed: leave to release time; the implementing session may add it
   if the user prefers.
2. **Backlog entry for the sibling drift** — should `app-lifecycle`/`preferences-ui` "main window" prose be
   logged as its own backlog item now (Cosmetic), or left to be noticed later? Default assumed: not logged;
   the enumeration lives in this design.
3. **Archive approval** — per repo policy `openspec archive` needs explicit user approval; the task is gated
   and must not run unasked.
