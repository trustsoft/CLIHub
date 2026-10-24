# Tasks

Spec+docs only. No source or test edits at any stage. Every gate must pass before the next stage.

## 1. Verify the prepared change

- [ ] 1.1 Confirm the working tree still only contains this change directory, and that `openspec/specs/main-window-layout/spec.md` is unmodified (12 requirements, 64 scenarios: `rg -c '^### Requirement: '` → 12, `rg -c '^#### Scenario: '` → 64)
- [ ] 1.2 Re-run the faithfulness diff of `openspec/changes/launch-window-layout-rename/specs/launch-window-layout/spec.md` (requirements body after `## ADDED Requirements`) against `openspec/specs/main-window-layout/spec.md` lines 9-311: the only differences must be the 8 WHEN lines ("main window" → "launch window"); the Purpose is new by design
- [ ] 1.3 Verify `openspec/changes/launch-window-layout-rename/.openspec.yaml` contains `schema: spec-driven`, `created: 2026-10-24`, `retire_capabilities: true`
- [ ] 1.4 Gate: `openspec validate launch-window-layout-rename` and `openspec validate --all` both pass

## 2. Follow the rename in docs

- [ ] 2.1 `ui/mockups/README.md:71` — change the capability id `main-window-layout` to `launch-window-layout`, keeping the "(delivered, redesigned)" annotation and the rest of the line
- [ ] 2.2 `openspec/plans/2026-10-21-test-suite-hardening-roadmap.md:81` — change the capability id in the scenario-to-test table row to `launch-window-layout` (scenario names "Uniform outer margins", "Rounded corners", "Hairline divider", "Pointer feedback" are unchanged by this rename)
- [ ] 2.3 `docs/backlog.md:227-233` — convert "`main-window-layout` names the wrong window" to the resolved pattern used at `docs/backlog.md:212-223`: heading suffix " (resolved)", Status: resolved by `launch-window-layout-rename` (2026-10-24), and keep the original evidence lines as the record of what was fixed
- [ ] 2.4 Do NOT touch `CHANGELOG.md` historical entries, `openspec/plans/2026-10-10-settings-window-roadmap.md`, `openspec/changes/archive/**`, or any `src/`/`tests/` file (see proposal Non-goals)
- [ ] 2.5 Gate: repo grep shows no stale live references — `rg -n 'main-window-layout' -g '!openspec/changes/archive/**' -g '!CHANGELOG.md' -g '!openspec/plans/2026-10-10-settings-window-roadmap.md'` returns nothing outside this change directory; then `openspec validate --all` passes

## 3. Regression and graph refresh

- [ ] 3.1 Gate: `dotnet build CLIHub.sln` succeeds with zero errors (no code changed; this pins the no-regression baseline)
- [ ] 3.2 Gate: `dotnet test CLIHub.sln` passes (same expectation as HEAD)
- [ ] 3.3 Run `graphify update .` so the knowledge graph reflects the new change directory and docs edits
- [ ] 3.4 Gate: `openspec validate --all` passes (29 specs + this change; pre-existing long-requirement warnings elsewhere are unchanged)

## 4. Archive (requires explicit user approval first — repo policy)

- [ ] 4.1 Report results of stages 1-3 and ask the user for explicit approval to archive; do not run `openspec archive` without it
- [ ] 4.2 On approval: `openspec archive launch-window-layout-rename` — expect it to create `openspec/specs/launch-window-layout/spec.md` (Purpose + 12 requirements from the ADDED delta) and delete `openspec/specs/main-window-layout/` (authorized by `retire_capabilities: true`; if the retirement is refused, capture the exact diagnostic and stop — do not delete the spec by hand without reporting)
- [ ] 4.3 Verify the archive landed: `openspec/specs/launch-window-layout/spec.md` exists with the intended Purpose; `openspec/specs/main-window-layout/` no longer exists; the change moved to `openspec/changes/archive/2026-10-24-launch-window-layout-rename/` (or the date actually shown by the tool)
- [ ] 4.4 Gate: final `openspec validate --all` passes with `spec/launch-window-layout` present and `spec/main-window-layout` absent, and the Purpose carries over (no placeholder-Purpose warning for the new capability)
- [ ] 4.5 Confirm with the user before any `git commit`; the commit, if approved, contains only this change's files and the stage-2 doc edits
