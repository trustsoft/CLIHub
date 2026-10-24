# Tasks: spec-wording-cosmetics

Spec-only change. No source, test, or configuration edits at any stage — if a task seems to need one, stop
and report instead. Smoke is empty by design (no runtime behavior changes). Every gate must pass before the
next stage. Open questions Q1–Q3 (design "Open questions") default as written; apply a contrary decision
before its stage instead of mid-stage.

## 1. Stage 1 — verify the item-1 deltas (launch-window wording)

- [x] 1.1 Re-run the occurrence census: `rg -n -i "main window" openspec/ docs/` — the only live hits must be
      `openspec/specs/app-lifecycle/spec.md:23,99,103,107` and `openspec/specs/preferences-ui/spec.md:96`
      plus `docs/backlog.md` (the two items); everything else must be under `openspec/changes/archive/`.
      Any new live occurrence: stop, record it, and extend the delta before continuing.
- [x] 1.2 Faithfulness diff of `openspec/changes/spec-wording-cosmetics/specs/app-lifecycle/spec.md` against
      `openspec/specs/app-lifecycle/spec.md`: the two MODIFIED blocks ("Single instance enforcement",
      "Startup window visibility") must differ from the main spec only in the four wording edits
      (`:23` its main window → its launch window; `:99` show or hide its main window → launch window;
      `:103` and `:107` the main window → the launch window); requirement prose otherwise identical; the
      five scenario names (3 + 2) byte-identical; no other requirement of the spec carried.
- [x] 1.3 Faithfulness diff of `openspec/changes/spec-wording-cosmetics/specs/preferences-ui/spec.md`
      against `openspec/specs/preferences-ui/spec.md`: the MODIFIED block ("Save applies preferences") must
      differ only in the `:96` THEN ("shows the main window" → "shows the launch window"); all eight
      scenario names byte-identical; requirement prose identical.
- [x] 1.4 Gate: `openspec validate --all` passes (the validator runs the archive merge, so it also proves
      the MODIFIED names resolve and no scenario name is dropped).

## 2. Stage 2 — verify the item-2 delta (agent-ui requirement renames)

Apply open question Q1 (defaults: keep the proposed names) before this stage.

- [x] 2.1 Confirm the ten `FROM:` names in
      `openspec/changes/spec-wording-cosmetics/specs/agent-ui/spec.md` are byte-identical to the current
      headers at `openspec/specs/agent-ui/spec.md:9,29,49,69,89,109,132,146,160,174` (including the
      `REQ-AGENT-CAP-NNN: ` prefix — it is part of the requirement name), and that the ten `TO:` names do
      not equal or case/spacing-collide with any of the 13 current names (`:9-289`) or each other.
- [x] 2.2 Confirm the delta carries RENAMED pairs only — no requirement bodies, no MODIFIED/ADDED/REMOVED
      sections — so requirement bodies and scenario blocks cannot change. If Q1 changed a TO name, edit the
      pair and re-run 2.1.
- [x] 2.3 Re-run the external-reference census: `rg -n "REQ-AGENT-CAP" -g "!openspec/changes/archive/**" .`
      must return only `openspec/specs/agent-ui/spec.md` and `docs/backlog.md:276` (rewritten in stage 4).
- [x] 2.4 Gate: `openspec validate --all` passes (proves every FROM resolves in the main spec and no TO
      collides).

## 3. Stage 3 — whole-change verification

- [x] 3.1 Gate: `dotnet build CLIHub.sln` — zero errors (no code changed; pins the no-regression baseline).
- [x] 3.2 Gate: `dotnet test CLIHub.sln` — zero failures (same expectation as HEAD).
- [x] 3.3 Gate: `openspec validate --all` passes; the pre-existing warnings elsewhere
      (long-requirement/placeholder-Purpose on other specs) are unchanged.
- [x] 3.4 Review `git status --porcelain`: only `openspec/changes/spec-wording-cosmetics/` is new; no
      `src/`, `tests/`, `docs/`, or main-spec file is modified. Smoke: empty (spec-only change) — record
      that explicitly in the report.

## 4. Stage 4 — orchestrator stage at commit/archive time: backlog rewrites, graph refresh, approval

Runs when the change is being committed/archived, per current policy (not before stage 3 is green).
Follows the low-sweep-2 stage-3 pattern: resolved format, one hunk per entry, nothing else in
`docs/backlog.md` touched.

- [x] 4.1 Rewrite "`app-lifecycle`/`preferences-ui` say \"main window\" for the launch window"
      (`docs/backlog.md:262-271`) to the resolved format: heading suffix " (resolved)"; `Status:` resolved by
      the spec-wording-cosmetics change (2026-10-24); `Resolved model:` the five occurrences
      (`app-lifecycle:23,99,103,107`, `preferences-ui:96`) now say "the launch window" via three MODIFIED
      requirement blocks with all scenario names preserved — matching `launch-window-layout` and
      `launch-window-theme`; keep the original evidence lines as the record; `Trigger:` any future edit to
      either spec's window wording.
- [x] 4.2 Rewrite "`agent-ui` requirement naming style drift" (`docs/backlog.md:273-277`) to the resolved
      format: `Status:` resolved by the spec-wording-cosmetics change (2026-10-24); `Resolved model:` the
      ten ID-style headers renamed to sentence-style names via RENAMED pairs (bodies untouched), so all 13
      requirement names in the capability are sentence-style; keep the evidence line as the record;
      `Trigger:` any future requirement added to `agent-ui`.
- [x] 4.3 Gate: `git diff docs/backlog.md` shows exactly the two entry hunks and nothing else;
      `openspec validate --all` still passes.
- [x] 4.4 Run `graphify update .` so the knowledge graph reflects the new change directory and the backlog
      edits.
- [x] 4.5 Report stage results and wait for the user's explicit approval before any `git commit` or
      `openspec archive spec-wording-cosmetics` (repo policy; approval for one never carries to the other).
      On approved archive, verify `openspec/specs/agent-ui/spec.md` shows the ten renamed headers with
      bodies unchanged, the two wording-fixed specs validate, and run a final `openspec validate --all`.
