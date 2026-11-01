## 1. Draft `docs/testing.md`

- [x] 1.1 Create `docs/testing.md` following the 12-section outline in `design.md` ("Decision 6"): Overview;
      Test project mapping; Naming convention; Arrange/Act/Assert and one behavior per test;
      Mocking (strict vs. loose vs. hand-written fakes); Time and temp-directory patterns; Shared test
      helpers; Skip semantics; Architecture-boundary tests; Adding a test for a new spec scenario; Adding a
      spec scenario for new behavior; Gate commands.
- [x] 1.2 For every fact cited in `design.md` ("Verified current state"), re-read the live file at the
      cited `file:line` before writing the corresponding sentence in `docs/testing.md` — do not transcribe
      `design.md`'s citations blindly, since time may have passed between planning and implementation. Per
      Decision 5, do not state a global test count, coverage percentage, or Moq strict/loose count (exact or
      approximate) anywhere in `docs/testing.md`. Per Decision 9, omit the naming-form census (578/231/0)
      entirely rather than recomputing it, and do not mention roadmap items C1-C5 by id anywhere in the doc.
- [x] 1.3 Write the mocking section using the mixed strict/loose rule from `design.md` ("Strict mocks —
      deviation from the roadmap's claim"): strict for collaborators the test verifies interactions with,
      loose (`new Mock<T>()` / `Mock.Of<T>()`) for supplied-but-unasserted collaborators, hand-written fakes
      (e.g. `FakeTimeProvider`) when the double needs stateful behavior. Do not state "all mocks are strict"
      or "zero loose mocks," and do not cite any mock-construction count.
- [x] 1.4 Write the "Adding a test for a new spec scenario" and "Adding a spec scenario for new behavior"
      sections as short numbered workflows (no new tooling, no new commands beyond what `AGENTS.md` and
      `openspec/config.yaml` already document).
- [x] 1.5 Gate: read the finished `docs/testing.md` once end to end and confirm every section from the
      outline is present and every sentence that names a file, line, command, or package version matches
      what Stage 1.2's re-read actually found — fix any mismatch before moving to Stage 2.

## 2. Wire the links, trim the overlapping doc, and relocate its speculative section

- [x] 2.1 Add one line for `docs/testing.md` under `docs/architecture.md`'s "## Focused Documents" list
      (`docs/architecture.md:78-87`), matching the existing one-line-per-doc pattern.
- [x] 2.2 Add one new row to `docs/project-context.md`'s "## Documentation Routing" table
      (`docs/project-context.md:65-83`): question "Testing conventions and gates" (or equivalent short
      phrasing consistent with the table's existing style), canonical source `docs/testing.md`. Do not add
      anything to the "## Verification" section — see `design.md` Decision 1 for why that section is not the
      target.
- [x] 2.3 Trim `tests/CLIHub.Tests/README.md` per `design.md` Decision 4: keep the `LaunchWindowViewModelBuilder`
      walkthrough and its before/after example (current `:17-94`); replace the naming convention, AAA/
      "Test One Behavior Per Test," "Only Mock What You Verify," and hand-written-fakes guidance, and the
      "Adding New Tests" section with a short pointer to `docs/testing.md`; remove the stale
      "**Total: 1,282 tests** (counts as of 2026-10-30...)" line and the per-project counts in the "Overview"
      and "Directory Structure" sections without replacing them with a new number (point to
      `dotnet test CLIHub.sln` instead, per `design.md` Decision 5).
- [x] 2.4 Delete the "## Future Improvements" section from `tests/CLIHub.Tests/README.md` (current `:252-267`)
      as part of the same trim. Do not drop its content: add one new entry to `docs/backlog.md` under
      "## Test suite" per `design.md` Decision 7 — title "Speculative test-infrastructure ideas from
      `tests/CLIHub.Tests/README.md`", severity Low, evidence citing `tests/CLIHub.Tests/README.md:252-267`
      at `1c27693`, listing the preserved ideas (builders for `ProjectPaneController`/`AgentPaneController`,
      AutoFixture integration, snapshot testing, BenchmarkDotNet performance benchmarks, cross-layer
      integration tests), explicitly excluding `SettingsViewModelBuilder` because
      `tests/CLIHub.Tests/Builders/SettingsViewModelFixture.cs` already serves that need, and trigger "a
      contributor wants a builder, UI-state verification, or performance measurement that the current suite
      does not provide." Match the file's established entry shape (title, `- Severity:`, `- Evidence:`,
      `- Proposed remedy:` or equivalent, trigger).
- [x] 2.5 Gate: confirm the new `docs/backlog.md` "## Test suite" entry exists with the content from Stage 2.4,
      and confirm "## Future Improvements" no longer appears anywhere in `tests/CLIHub.Tests/README.md`.
- [x] 2.6 Gate: grep the repository for `architecture.md#` and `project-context.md#` deep links plus any
      inbound reference to `tests/CLIHub.Tests/README.md` sections that Stages 2.3-2.4 removed, and confirm
      no link now points at a removed anchor or a since-deleted section heading.

## 3. Verify accuracy and run gates

- [x] 3.1 Re-read every `file:line` citation and every shell command that ended up in the final
      `docs/testing.md`, `tests/CLIHub.Tests/README.md`, and the new `docs/backlog.md` entry (not just
      `design.md`'s draft of them) against the tree as it stands at the end of Stage 2, and correct or delete
      anything that no longer matches — this is required even if Stage 1.2 already did a pass, because Stage
      2's edits happen after that pass.
- [x] 3.2 Check ASCII/quoting consistency between `docs/testing.md` and its neighboring docs
      (`docs/architecture.md`, `docs/project-context.md`, `tests/CLIHub.Tests/README.md`, `docs/backlog.md`):
      straight quotes vs. curly quotes, backtick-fenced code spans for file paths and commands, consistent
      heading levels. Confirm no table row or bullet in any touched file references a file, section heading,
      or anchor that does not exist in the repository at this commit.
- [x] 3.3 Run `dotnet build CLIHub.sln` and confirm it still reports 0 errors (documentation-only change must
      not affect the build).
- [x] 3.4 Run `dotnet test CLIHub.sln` and confirm all tests still pass (documentation-only change must not
      affect test results).
- [x] 3.5 Run `openspec validate --all` and confirm it reports the change valid; quote the exact output in the
      implementation session's summary.
- [x] 3.6 Run `node .opencode/scripts/check-model-policy.mjs` per `AGENTS.md`'s command list and confirm it
      reports OK.
- [x] 3.7 Add a "Delivered" marker next to the C6 entry in
      `openspec/plans/2026-10-21-test-suite-hardening-roadmap.md` §5 (around line 182-189), with this exact
      line: `- **Delivered:** \`testing-conventions-doc\` (2026-11-01) — \`docs/testing.md\`; roadmap items
      C1–C5 remain open.` This is required (see `design.md` Decision 8), not optional — the roadmap is not
      validated by `openspec validate`, but it is the canonical place a future reader checks for what has
      shipped from this sequence.
- [x] 3.8 Run `graphify update .` if `graphify-out/graph.json` exists in this repository, per `AGENTS.md`'s
      "Codebase Navigation" section.
