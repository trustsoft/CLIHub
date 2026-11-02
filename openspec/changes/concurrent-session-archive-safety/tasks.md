# Tasks: concurrent-session-archive-safety

## 1. Add worktree-isolation rule to AGENTS.md

- [ ] 1.1 In `AGENTS.md`, in the "Change, Commit, and Archive Policy" section (around `:61-82`), immediately after the existing commit approval rule (the paragraph starting "You MUST get explicit user approval before `git commit`"), add a new paragraph:

  ```markdown
  **Concurrent sessions:** Every concurrent orchestrator session MUST use its own worktree (the OpenCode `worktree_create` tool) instead of sharing one checkout. A single session at a time may use the primary working tree. Running two sessions in the same working tree causes silent file reversions (archived changes reappearing, specs reverting, pipeline state loss) that require manual recovery from git history or session snapshots.
  ```

  Preserve the existing paragraph about commit approval exactly as-is; insert the new paragraph after it and before the existing "Approval applies only to the specific action" paragraph.

- [ ] 1.2 Verify the edit: re-read the "Change, Commit, and Archive Policy" section and confirm the new paragraph is present and the surrounding paragraphs are unchanged.

- [ ] 1.3 Gate: run `git diff AGENTS.md` and verify only the new paragraph was added; no other lines changed.

## 2. Add post-archive integrity check to AGENTS.md

- [ ] 2.1 In `AGENTS.md`, in the "Change, Commit, and Archive Policy" section, immediately after the paragraph that starts "Implement and verify the work first", add a new paragraph:

  ```markdown
  **Post-archive integrity check:** After `openspec archive` and its archive commit, verify the following before continuing:
  - `git status --porcelain` is empty (no unexpected staged or unstaged changes)
  - `openspec list` does not include the just-archived change
  - `openspec validate --all` reports 0 errors
  - `node .opencode/scripts/pipeline-state.mjs check` reports 0 warnings (or expected warnings only)

  If any check fails, stop and report the discrepancy to the user instead of continuing to the next task.
  ```

  Preserve all surrounding paragraphs exactly as-is.

- [ ] 2.2 Verify the edit: re-read the "Change, Commit, and Archive Policy" section and confirm the new paragraph is present and correctly formatted (bullet list for the checks, conditional for the failure action).

- [ ] 2.3 Gate: run `git diff AGENTS.md` and verify only the new paragraph was added; no other lines changed.

## 3. Reinforce pipeline-state write discipline in AGENTS.md

- [ ] 3.1 In `AGENTS.md`, locate the bullet that starts "Pipeline state transitions MUST go through" (around `:78-81`). After that bullet, add a new bullet:

  ```markdown
  - Never write `.opencode/pipeline.json` directly with `node -e`, shell redirection, or manual JSON edits; every mutation MUST go through `node .opencode/scripts/pipeline-state.mjs`.
  ```

  Preserve the existing pipeline-state bullet exactly as-is; insert the new bullet after it.

- [ ] 3.2 Verify the edit: re-read the "Change, Commit, and Archive Policy" section and confirm the new bullet is present and the existing pipeline-state bullet is unchanged.

- [ ] 3.3 Gate: run `git diff AGENTS.md` and verify only the new bullet was added; the existing pipeline-state bullet is unchanged.

## 4. Document recovery procedure in docs/backlog.md

- [ ] 4.1 In `docs/backlog.md`, locate the "Agent tooling" section (around `:154`). After the existing "Two sessions in one working tree can revert each other's OpenSpec files" entry (ends around `:173`), add a new subsection:

  ```markdown
  ### Recovery from concurrent-session loss

  When concurrent-session loss is detected (archived change reappears, spec reverts, pipeline state emptied):

  **Tracked OpenSpec/spec files:**
  1. Run `git reflog` to find the last known good commit before the loss.
  2. Run `git restore --source=<sha> -- <paths>` to restore specific files (preferred), or
  3. Run `git reset --hard <sha>` to restore the entire working tree (requires explicit user approval per AGENTS.md, and only when unrelated work is not at risk).

  **`.opencode/pipeline.json` (gitignored):**
  1. Restore from the OpenCode snapshot store (for example `~/.local/share/opencode/snapshot/<project-hash>` on this machine), or
  2. Reconstruct entries with `node .opencode/scripts/pipeline-state.mjs add/update`.

  **Verify restoration:** Run `git status`, `openspec list`, `openspec validate --all`, and `node .opencode/scripts/pipeline-state.mjs check`.

  The 2026-11-01 OpenSpec files were restored from HEAD, and the 2026-11-02 pipeline state from a session snapshot.
  ```

  Preserve all surrounding entries exactly as-is.

- [ ] 4.2 Verify the edit: re-read the "Agent tooling" section and confirm the new subsection is present and correctly formatted (two category headings, numbered lists, verification heading, recovery summary).

- [ ] 4.3 Gate: run `git diff docs/backlog.md` and verify only the new subsection was added; no other lines changed.

## 5. Update backlog finding mitigation status

- [ ] 5.1 In `docs/backlog.md`, locate the "Two sessions in one working tree can revert each other's OpenSpec files" entry (around `:156`). At the end of the "Proposed remedy" bullet (after "openspec list` rather than trusting the command's own output."), add a new bullet:

  ```markdown
  - Status: mitigated by `concurrent-session-archive-safety` (2026-11-02): worktree isolation, post-archive checks, and sole-writer discipline are now rules; reopen if a shared-checkout loss recurs.
  ```

  Preserve all other bullets in the entry exactly as-is.

- [ ] 5.2 Verify the edit: re-read the entry and confirm the new bullet is present and the surrounding bullets are unchanged.

- [ ] 5.3 Gate: run `git diff docs/backlog.md` and verify only the new bullet was added; no other lines changed.

## 6. Final verification

- [ ] 6.1 Gate: run `git status --porcelain` and verify only `AGENTS.md` and `docs/backlog.md` are modified (no other files changed).

- [ ] 6.2 Gate: run `git diff AGENTS.md docs/backlog.md` and verify all edits match the tasks above (2 paragraphs + 1 bullet added to AGENTS.md, 1 subsection + 1 bullet added to docs/backlog.md).

- [ ] 6.3 Gate: run `node .opencode/scripts/pipeline-state.mjs check` and verify it reports the current state (10 changes, expected ok/warning counts). Quote the exact output.

- [ ] 6.4 Gate: run `openspec validate --all` and verify 0 errors (existing long-requirement warnings are acceptable). Quote the exact output.
