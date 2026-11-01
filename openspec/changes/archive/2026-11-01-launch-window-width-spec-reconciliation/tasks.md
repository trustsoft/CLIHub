# Tasks: launch-window-width-spec-reconciliation

Implementation happens in a fresh session; `design.md` carries the full rationale, file:line
evidence, and the exact corrected scenario text. This change is spec/documentation only — no
source, test, or build artifact changes are expected.

## 1. Spec delta

- [x] 1.1 Read `openspec/specs/launch-window-layout/spec.md:236-258` to re-confirm the current
      "Window chrome" requirement text and its five scenarios before validating the delta.
      Re-confirmed at HEAD `739e920`: the SHALL sentence is at `:238`, the five scenarios span
      `:240-258`, and the "Fixed width and content height" THEN still reads "the window height
      follows its content and its width does not change".
- [x] 1.2 Treat
      `openspec/changes/launch-window-width-spec-reconciliation/specs/launch-window-layout/spec.md`
      as the deliverable and verify its `MODIFIED Requirements` → "Window chrome" block: the
      requirement's SHALL sentence and the four scenarios this change does not modify ("No title
      bar or frame controls", "Window outline", "Rounded corners", "Window cannot be moved or
      resized by dragging") must be byte-identical to
      `openspec/specs/launch-window-layout/spec.md`, in the same order. Only "Fixed width and
      content height" may differ, and only in its THEN clause — OpenSpec's `MODIFIED` delta
      mechanism rejects dropping or renaming a scenario name within a requirement, so the name
      stays and only the body changes (see `design.md`, "Tooling constraint", for the validator
      error that confirmed this):
      ```
      #### Scenario: Fixed width and content height
      - **WHEN** the panes' content changes
      - **THEN** the window height follows its content, and the window's size is never settable by
        the user — there is no OS resize border and `ResizeMode` is `NoResize`
      ```
      If any of the four had drifted, the correction belongs in the delta, never in the durable
      spec. Verified: the SHALL sentence and all four scenarios are identical after line-ending
      normalisation (the delta uses the repository's LF change-delta convention; the durable spec
      is CRLF), the scenario order matches, and the changed scenario keeps its name with only its
      THEN rewritten to the text above.
- [x] 1.3 Confirm the delta carries all five scenario names in the current spec's order and that
      the "Agents pane and row sizing" requirement — including its "Window fits its content"
      scenario (`openspec/specs/launch-window-layout/spec.md:232-234`), the authoritative
      statement that the width follows the panes — is not part of the delta, i.e. the delta does
      not touch width-follows-content. Verified: the delta's only `### Requirement:` is "Window
      chrome"; neither "Agents pane and row sizing" nor "Window fits its content" appears in it,
      and its five scenario headings are "No title bar or frame controls", "Window outline",
      "Rounded corners", "Fixed width and content height", "Window cannot be moved or resized by
      dragging" — the same five, in the same order, as the current spec.
- [x] 1.4 Recorded: the durable spec `openspec/specs/launch-window-layout/spec.md` is intentionally
      left untouched by this change. `openspec archive` applies the delta to it, in the commit that
      archives this change. Precedent: commit `d67d95c` (`chore(openspec): archive
      launch-window-project-provenance`) is the commit that wrote
      `openspec/specs/project-management/spec.md` (+13 lines) while archiving that change, with no
      earlier commit touching the durable spec. Editing the durable spec here would put the same
      text in two commits and blur which step owns it.
- [x] 1.5 Run `openspec validate --all` — must pass while the change is still active, and the
      output must still list `launch-window-width-spec-reconciliation`. Green:
      `✓ change/launch-window-width-spec-reconciliation`, `Totals: 30 passed, 0 failed (30 items)`.

## 2. Documentation bookkeeping

- [x] 2.1 In `docs/backlog.md`, section "UI and theming", mark the entry "`launch-window-layout`
      spec contradicts itself about the window width" (`:607-622`) done in the file's existing
      style (see nearby `[done ...]`/`[closed ...]` entries, e.g. `:215-227`, `:586-605`): append a
      `- Status: fixed by `launch-window-width-spec-reconciliation` (2026-11-01), which corrected the
      "Fixed width and content height" scenario's body (the scenario's name is unchanged — OpenSpec's
      MODIFIED delta mechanism does not support a scenario-level rename) to state the window's
      height-follows-content and non-user-resizable facts without claiming the width is constant.`
      line (or the file's closest equivalent phrasing), and add a `- Residual evidence: none.` line
      if that is the file's convention for closed entries. Keep the existing evidence paragraph; do
      not delete the history. Done: the heading carries
      `- **[done 2026-11-01: launch-window-width-spec-reconciliation]**` like its neighbour at
      `:586`, and `Status` + `Residual evidence` lines follow the existing `Trigger` line with the
      evidence paragraph, severity, remedy, and trigger kept verbatim.
- [x] 2.2 In `CHANGELOG.md`, under `## Unreleased` (`:11-16`), insert a `### Changed` group before
      the existing `### Fixed` group (matching the `Added`/`Changed`/`Fixed`/`Removed` order used
      under `## 1.0.4 — 2026-11-01`, `:18-42`) with one bullet:
      `- The \`launch-window-layout\` spec no longer contradicts itself about the launch window's
      width; the "Window chrome" requirement now states that the window's height follows its
      content and its size is never user-adjustable, without claiming the width stays constant
      (\`launch-window-width-spec-reconciliation\`, \`launch-window-layout\`).`
      Do not touch `RELEASE-NOTES.md` and do not add a new version heading. Done: `### Changed` sits
      between `## Unreleased` and `### Fixed`, and no other changelog group or version heading
      changed.
- [x] 2.3 Re-read `docs/architecture/ui.md` in full to confirm it still states no claim about the
      launch window's sizing behavior (per `design.md`, no claim was found). If a claim is found on
      re-read, correct it minimally to match `SizeToContent="WidthAndHeight"` +
      `ResizeMode="NoResize"`; otherwise make no edit to this file. Done: read all 154 lines again;
      the only row/element sizing text is the project row's fixed-width marker rail (`:109-116`),
      which says nothing about the window's or a pane's overall width, so no edit was made.
- [x] 2.4 Run `openspec validate --all` again after the documentation edits — must pass. Quote the
      output in the final report. Green: `✓ change/launch-window-width-spec-reconciliation`,
      `Totals: 30 passed, 0 failed (30 items)`.

## 3. Final validation

- [x] 3.1 Build: `dotnet build CLIHub.sln` — green. (No source file is touched by this change; this
      gate exists to catch an accidental edit outside `openspec/` and `docs/`/`CHANGELOG.md`.)
      Green: `Build succeeded. 0 Warning(s) 0 Error(s)`, 1.53 s.
- [x] 3.2 Test: `dotnet test CLIHub.sln` — green. (No test asserts spec text; this gate exists for
      the same reason as 3.1.) Green: 568 + 63 + 703 = 1334 passed, 0 failed, 0 skipped across the
      three test assemblies.
- [x] 3.3 Validate: `openspec validate --all` — green. Quote the output in the final report. Green:
      `✓ change/launch-window-width-spec-reconciliation`, `Totals: 30 passed, 0 failed (30 items)`;
      the active change is still listed.
- [x] 3.4 Update graph: `graphify update .`. Done: `[graphify watch] Rebuilt: 17039 nodes, 30885
      edges, 1006 communities`; `Code graph updated.` (exit code 0).
- [ ] 3.5 Report the result. No manual smoke task is needed: nothing observable in the running
      application changes, so there is no user confirmation step before requesting archive/commit
      approval — follow `AGENTS.md`'s change/commit/archive policy for the approval ask itself.
