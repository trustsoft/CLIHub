# Tasks: agent-detection-defaults-detail-copy

Copy-only change: one VM string + doc comment, one XAML tooltip literal, one test
assertion, two spec deltas. If a task seems to need a binding, command, layout,
control-type, or other-card edit, stop and report instead.

## 1. Update the DEFAULTS card detail string

- [x] 1.1 In `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`, change the
      `ProbePolicyDefaultsDetail` property (around line 377) from
      `"Used when a field is empty"` to `"Cache TTL · Checking timeout"` (U+00B7
      middle dot, one space on each side — copy the character from
      `WatchedDirectoryEditor.cs:667`/`:317` rather than retyping it, to avoid an
      ASCII hyphen or a different dot character slipping in).
- [x] 1.2 Update the XML doc comment directly above it (lines ~374-376) to describe the
      new content, e.g. "The DEFAULTS summary-card detail: the two fields the built-in
      defaults apply to." Do not change `ProbePolicyDefaults` (line 372) or its own doc
      comment (lines 368-371).
- [x] 1.3 Gate: `dotnet build CLIHub.sln` — zero errors.

## 2. Update the DEFAULTS card tooltip

- [x] 2.1 In `src/CLIHub/Views/SettingsWindow.xaml`, on the DEFAULTS
      `controls:SummaryCard` (around line 268), change
      `DetailToolTip="Applied when a detection field is empty"` to
      `DetailToolTip="Built-in defaults for cache TTL and version check timeout"`. Do
      not touch `Detail`, `Icon`, `Label`, or `Value` on this card, and do not touch the
      CACHE TTL or TIMEOUT cards' own `DetailToolTip` values (lines 256, 262).
- [x] 2.2 Gate: `dotnet build CLIHub.sln` — zero errors.

## 3. Update the pinned test assertion

- [x] 3.1 In
      `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs`,
      change line 904 from
      `Assert.Equal("Used when a field is empty", fixture.ViewModel.ProbePolicyDefaultsDetail);`
      to
      `Assert.Equal("Cache TTL · Checking timeout", fixture.ViewModel.ProbePolicyDefaultsDetail);`.
      Do not change line 903 (`ProbePolicyDefaults` stays `"15 min · 10 sec"`) or any
      other assertion in this test.
- [x] 3.2 Grep the rest of `tests/` for `"Used when a field is empty"` to confirm no
      other test pins the old literal; if one turns up, update it the same way and note
      it in the PR description.
- [x] 3.3 Gate: `dotnet test CLIHub.sln` — zero failures.

## 4. Spec deltas (already authored in this change; verify only)

- [x] 4.1 Verify `specs/settings-theme/spec.md` in this change contains exactly one
      `## MODIFIED Requirements` block for requirement `Agent Detection page summary
      cards`, with the scenario named exactly `Defaults card detail` and its THEN
      updated to the new literal; every other scenario under that requirement
      (`Cards shown`, `Cache TTL card shows the effective value`, `Timeout card shows
      the effective value`, `Built-in default detail`, `Custom value detail`, `Cards
      follow live changes`, `Defaults card shown`, `Defaults card value`) present and
      byte-identical to `openspec/specs/settings-theme/spec.md:453-502`.
- [x] 4.1a Verify the requirement sentence itself (line 5 of this delta file) is
      **intentionally** edited, not byte-identical to the main spec: its trailing
      clause must read "...and SHALL name the two detection parameters those default
      values apply to." (not "...and SHALL state that these values are used when a
      field is empty." from `openspec/specs/settings-theme/spec.md:455`). Every other
      clause of that sentence (cache TTL card behavior, timeout card behavior, the
      "N min · M sec" format example, the Diagnostic icon clause, the live-refresh
      clause) must stay byte-identical to the main spec. This is a deliberate fix
      (design.md D4a) for a requirement/scenario contradiction found after the first
      authoring pass — do NOT "correct" it back to the main spec's wording; that would
      reintroduce the contradiction with the `Defaults card detail` scenario.
- [x] 4.2 Verify `specs/preferences-ui/spec.md` in this change contains exactly one
      `## MODIFIED Requirements` block for requirement `Agent Detection page content`,
      with the scenario named exactly `DEFAULTS card detail` and its THEN updated to
      the new literal; every other scenario under that requirement present and
      byte-identical to `openspec/specs/preferences-ui/spec.md:695-786`. Unlike the
      settings-theme delta, the requirement sentence here (line 5) IS byte-identical
      to `openspec/specs/preferences-ui/spec.md:697` — it carries no "when a field is
      empty" clause, so no prose fix was needed (design.md, "Requirement-prose
      contradiction found and fixed"). If a future edit changes this delta's
      requirement sentence without updating design.md, treat that as a defect to flag,
      not to silently accept.
- [x] 4.3 Confirm `.openspec.yaml` in this change has `schema: spec-driven`,
      `created: 2026-11-01`, and no `skip_specs` line.

## 5. Final gates

- [x] 5.1 Gate: `git diff -- src/ tests/` shows only the three literal edits from
      stages 1-3 (plus the one doc-comment line) — no binding, command, layout, or
      other-card attribute touched.
- [x] 5.2 Gate: `dotnet build CLIHub.sln` — zero errors.
- [x] 5.3 Gate: `dotnet test CLIHub.sln` — zero failures.
- [x] 5.4 Gate: `openspec validate --all` — zero failures, with both spec deltas
      present.
- [x] 5.5 Run `graphify update .` so the knowledge graph reflects the edited files.

## 6. Reporting and approval

- [ ] 6.1 Report the result and wait for explicit user approval before any
      `git commit` or `openspec archive agent-detection-defaults-detail-copy` — this
      change has no manual-smoke requirement beyond visually confirming the two new
      strings in a running Settings window if the user wants that; approval for build
      verification never implies approval to commit or archive.
