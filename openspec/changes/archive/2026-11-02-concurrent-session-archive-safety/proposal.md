# Proposal: concurrent-session-archive-safety

## Why

Two related losses occurred from running two orchestrator sessions in one shared working tree:

1. **OpenSpec archive/spec loss (2026-11-01).** While one session was between an archive commit and a follow-up commit, a second session's work caused an already-archived change directory to disappear, an older pre-tick copy to reappear under `openspec/changes/`, and an already-applied scenario to be removed from `openspec/specs/preferences-ui/spec.md`. The damage was repaired from HEAD. The finding is documented in `docs/backlog.md` around line 156 (formerly line 511). Its trigger — "any second session in the same checkout" — has now fired twice.

2. **Pipeline-state loss (2026-11-02).** At 20:02:41 local, `.opencode/pipeline.json` was emptied (`changes: []`) by a direct `node -e` write that bypassed `.opencode/scripts/pipeline-state.mjs`. Ten completed entries were lost and later restored from a session snapshot. This is both a second concurrent-session incident and a violation of the repository rule that `pipeline-state.mjs` is the only writer (AGENTS.md, "Change, Commit, and Archive Policy").

These losses are silent until discovered through git history or external evidence. No error surfaces when they occur, and no existing guard detects or prevents them.

## What changes

This change establishes process guardrails and minimal tooling to make these losses materially less likely:

1. **Worktree isolation rule:** Every concurrent orchestrator session MUST use its own worktree (the OpenCode `worktree_create` tool) instead of sharing one checkout. A single session at a time may use the primary working tree. This rule is added to AGENTS.md's "Change, Commit, and Archive Policy" section.

2. **Post-archive integrity check:** After every `openspec archive` and its archive commit, verify `git status --porcelain`, `openspec list`, `openspec validate --all`, and `node .opencode/scripts/pipeline-state.mjs check` before continuing to the next task. This checklist is added to AGENTS.md's "Change, Commit, and Archive Policy" section, after the implement/verify rule, so the orchestrator runs it before considering the change done.

3. **Pipeline-state write discipline:** Reinforce that `.opencode/scripts/pipeline-state.mjs` is the sole writer and forbid direct JSON edits or `node -e` writes. Rely on the existing `check` command (`pipeline-state.mjs:986-1011`) that reconciles the entries against ground truth (commit shas, openspec directories, archive directories, or `tooling:` notes) and reports warnings for entries with no evidence or shas git cannot resolve. The rule that already forbids bypassing the script is sharpened in AGENTS.md to explicitly reject `node -e` or direct JSON writes.

4. **Document the recovery procedure:** Add a subsection to `docs/backlog.md` noting that when concurrent-session loss is detected, recovery depends on what was lost: tracked OpenSpec/spec files can be restored with `git restore --source=<sha>` (or `git reset --hard` with user approval), while `.opencode/pipeline.json` must be restored from the OpenCode snapshot store or reconstructed. This does not add automation; it records the manual steps that were successful.

## Non-goals

- No new daemon, file watcher, or lock-file subsystem for `.opencode/pipeline.json`.
- No automated enforcement of the worktree-isolation rule (no pre-commit hook that blocks commits when `opencode/worktrees/` is absent; that would be brittle on forks or contributors who use different workflows).
- No redesign of the orchestrator's session management or the pipeline-state file format.
- No change to product behavior; this is agent-tooling and documentation work only.

## Impact

**Modified docs:**
- `AGENTS.md` — "Change, Commit, and Archive Policy" section gains (a) the worktree-isolation rule with the single-session exception, (b) the post-archive integrity checklist, and (c) explicit prohibition of `node -e` writes or direct JSON edits to `.opencode/pipeline.json`.
- `docs/backlog.md` — the "Two sessions in one working tree can revert each other's OpenSpec files" item (around line 156) is updated with a mitigation status referencing this change, and a new "Recovery from concurrent-session loss" subsection is added describing the git restore/snapshot recovery procedure.

**No modified specs:** this is tooling/process work. `.openspec.yaml` has `skip_specs: true`.

**No source or test changes.** The `pipeline-state.mjs check` command already exists and is tested; no new code is written.

**No breaking changes.** The worktree-isolation rule is a process recommendation that does not block existing workflows when followed correctly.

## Dependencies

None. The `pipeline-state.mjs check` command already exists at `:986-1011` and is covered by tests in `pipeline-state.test.mjs`.
