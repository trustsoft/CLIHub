# Design: concurrent-session-archive-safety

## Context

This change addresses two documented incidents of concurrent-session state loss in the CLIHub repository. Both occurred when multiple orchestrator sessions operated in the same working tree, resulting in silent file reversions and data loss that required manual recovery from git history or session snapshots.

The solution is a layered process and tooling guardrail, not a new subsystem. The goal is to make these losses materially less likely through explicit rules, post-operation checks, and reinforcement of existing write discipline.

## Current state with file:line evidence

### The concurrent-session loss incidents

**Incident 1: OpenSpec archive/spec loss (2026-11-01)**

Documented in `docs/backlog.md:156-173` (as of commit `1ca798f`):

```markdown
### Two sessions in one working tree can revert each other's OpenSpec files

- Severity: Medium (silent loss of archived state; no error surfaces)
- Evidence: on 2026-11-01 a second orchestrator session worked `openspec/changes/agent-detection-defaults-detail-copy/`
  in the same checkout. At 20:12:52, while the first session was between the archive commit of
  `watched-directory-duplicate-feedback` (20:03) and the commit of `remove-startup-window-marker`
  (20:19), that change's archived files disappeared, an older pre-tick copy of the change reappeared
  under `openspec/changes/`, and `openspec list` started reporting the already-archived change as
  active again; the same operation also removed the change's already-applied scenario from
  `openspec/specs/preferences-ui/spec.md`. Nothing in the first session's git history explains it (no
  stash, no HEAD move), so the most likely cause is the other session checking out or restoring paths.
  The damage was repaired from HEAD and verified (`git status` clean, `openspec list` correct,
  `validate` clean, 225 archived changes intact).
- Proposed remedy: give each concurrent session its own worktree (`opencode.worktree_create`) instead
  of sharing one checkout, and after every `openspec archive` re-check `git status` plus
  `openspec list` rather than trusting the command's own output.
- Trigger: any second session in the same checkout, or an archived change reappearing in
  `openspec list`.
```

**Incident 2: Pipeline-state loss (2026-11-02)**

Recorded in the current `.opencode/pipeline.json` entry `concurrent-session-archive-safety`:

```json
"note": "Backlog 156 (formerly 511): shared-checkout state loss. Evidence: .opencode/pipeline.json was emptied at 2026-11-02T17:02:41Z by a direct node -e write (changes=[]), before docs commits 01e9a06/1ca798f at 17:18:53Z/17:19:03Z; the change directory was created then deleted at user request at 17:01:12Z. No plan artifacts remain; awaiting re-plan."
```

Ten completed entries were lost and later restored from a session snapshot. The loss was caused by a direct `node -e` write that bypassed `.opencode/scripts/pipeline-state.mjs`, violating the repository rule that `pipeline-state.mjs` is the sole writer.

### Existing write discipline rule

`AGENTS.md:78-81` (as of commit `1ca798f`):

```markdown
- Pipeline state transitions MUST go through `node .opencode/scripts/pipeline-state.mjs`
  (`add`/`update`/`prune`), so the 10-entry cap and the atomic compare-and-swap write are enforced in one
  place; stage transitions, stage notes, and commit shas are recorded with `update`. The sidebar plugin
  (`plugins/pipeline-status`) stays read-only and never writes the state file.
```

The rule exists but does not explicitly forbid `node -e` or direct JSON writes, and there is no post-operation check that verifies the rule was followed.

### Existing archive commit rule

`AGENTS.md:63-64` (as of commit `1ca798f`):

```markdown
- You MUST get explicit user approval before `git commit`, `git push`, `git reset`/`git revert` that rewrites
  history, `openspec archive`, or moving a change into `openspec/changes/archive/`.
```

The rule mandates user approval but does not include a post-archive integrity check.

### Existing `pipeline-state.mjs check` command

The `check` command is implemented at `pipeline-state.mjs:986-1011` and documented at `:11`:

```javascript
// node .opencode/scripts/pipeline-state.mjs check [--file <path>] [--repo <path>] [--strict]
```

Evidence sources (`pipeline-state.mjs:707-744`):
- `archiveIds()` — enumerates `openspec/changes/archive/` directory names
- `specDirExists(id)` — checks for `openspec/changes/<id>/`
- `commitResolves(sha)` — runs `git cat-file -e <sha>^{commit}`

Verdict types (`pipeline-state.mjs:662-700`):
- `ok` — entry has evidence (commit sha, spec dir, archive dir, or `tooling:` note)
- `warning: <id> no evidence` — completed entry (`status: done` or `stage: commit|archive`) with no evidence
- `warning: <id> commit sha <sha> not found in <repo>` — recorded sha that git cannot resolve

Exit code (`pipeline-state.mjs:1009-1011`):
- 0 — all checks passed
- 1 — `--strict` and warnings exist

Tests covering the command are in `pipeline-state.test.mjs:1102-1226` (the `check CLI` suite).

### Worktree creation path

Agents have access to the OpenCode `worktree_create` tool, which returns `{ directory: string }`. The orchestrator instructions in the parent prompt or the `orchestration` skill document the API, but `AGENTS.md` does not currently mandate worktree isolation for concurrent sessions.

## Goals / Non-Goals

**Goals:**
- Establish a process rule that materially reduces the risk of concurrent-session loss.
- Add a post-archive integrity check that detects silent file reversions before continuing.
- Reinforce the existing write discipline for `pipeline-state.mjs` and explicitly forbid bypassing it.
- Document the recovery procedure that was successful in both incidents.

**Non-Goals:**
- No new daemon, file watcher, or lock-file subsystem.
- No automated enforcement of the worktree rule (no pre-commit hook that blocks commits when worktrees are absent).
- No redesign of the orchestrator's session management.
- No change to the pipeline-state file format or the `pipeline-state.mjs` script beyond documentation clarifications.
- No change to product behavior; this is agent-tooling and documentation work only.

## Decisions

### Decision 1: Worktree isolation rule

**Chosen:** Add to `AGENTS.md`, in the "Change, Commit, and Archive Policy" section, immediately after the existing commit approval rule:

> **Concurrent sessions:** Every concurrent orchestrator session MUST use its own worktree (the OpenCode `worktree_create` tool) instead of sharing one checkout. A single session at a time may use the primary working tree. Running two sessions in the same working tree causes silent file reversions (archived changes reappearing, specs reverting, pipeline state loss) that require manual recovery from git history or session snapshots.

**Rationale:**
- Both documented incidents occurred because two sessions shared one checkout.
- Worktrees are a built-in git feature with no repository-side setup or ongoing cost.
- The OpenCode `worktree_create` tool is already available to agents; no new API is needed.
- Stating the rule explicitly in `AGENTS.md` surfaces it in the agent context every session reads.

**Alternatives considered:**
- Add a pre-commit hook that blocks commits when `opencode/worktrees/` is absent: rejected as brittle (forks or contributors may use different workflows, and the hook would need logic to distinguish "second session" from "only session").
- Add a lock file that agents check before starting: rejected as over-engineering; worktree isolation is simpler and has no lock-file contention failure mode.

### Decision 2: Post-archive integrity check

**Chosen:** Add to `AGENTS.md`, in the "Change, Commit, and Archive Policy" section after the implement/verify rule, a checklist that runs after every `openspec archive` and its archive commit:

> **Post-archive integrity check:** After `openspec archive` and its archive commit, verify the following before continuing:
> - `git status --porcelain` is empty (no unexpected staged or unstaged changes)
> - `openspec list` does not include the just-archived change
> - `openspec validate --all` reports 0 errors
> - `node .opencode/scripts/pipeline-state.mjs check` reports 0 warnings (or expected warnings only)
>
> If any check fails, stop and report the discrepancy to the user instead of continuing to the next task.

**Rationale:**
- Incident 1 would have been caught immediately by `openspec list` (the archived change reappeared) or `git status` (the spec revert would show as an unstaged change).
- The checks are fast (sub-second) and already part of the normal verification workflow; consolidating them into a mandatory post-archive gate costs almost nothing.
- Placing the rule next to the implement/verify rule in `AGENTS.md` keeps it in the agent's context when the archive happens.

**Alternatives considered:**
- Run the checks before the archive commit instead of after: rejected because the commit itself could fail or be interrupted, leaving the repository in an unexpected state; running the checks after the commit verifies the final state.
- Run the checks only when concurrent sessions are detected: rejected because detecting "concurrent sessions" reliably from one session is hard, and the checks are cheap enough to always run.

### Decision 3: Reinforce pipeline-state write discipline

**Chosen:** Add a new bullet to `AGENTS.md` after the existing pipeline-state bullet:

> - Never write `.opencode/pipeline.json` directly with `node -e`, shell redirection, or manual JSON edits; every mutation MUST go through `node .opencode/scripts/pipeline-state.mjs`.

**Rationale:**
- Incident 2 was caused by a `node -e` write that bypassed the script.
- The existing rule mentions the script as the entry point but does not explicitly forbid alternatives.
- Adding a separate bullet makes the prohibition unambiguous without breaking the existing sentence.

**Alternatives considered:**
- Edit the existing bullet to insert the prohibition mid-sentence: rejected as it would produce ungrammatical prose (inserting text between "...enforced in one place;" and "stage transitions, stage notes...").
- Add a file watcher or lock file that detects direct writes: rejected as over-engineering; the rule is process-level, not a technical enforcement mechanism.
- Rename the file to a non-obvious location: rejected as pointless (agents can still find it, and it breaks the sidebar plugin and other readers).

### Decision 4: Document the recovery procedure

**Chosen:** Add a new subsection to `docs/backlog.md` under "Agent tooling", after the existing "Two sessions in one working tree" entry:

> ### Recovery from concurrent-session loss
>
> When concurrent-session loss is detected (archived change reappears, spec reverts, pipeline state emptied):
>
> **Tracked OpenSpec/spec files:**
> 1. Run `git reflog` to find the last known good commit before the loss.
> 2. Run `git restore --source=<sha> -- <paths>` to restore specific files (preferred), or
> 3. Run `git reset --hard <sha>` to restore the entire working tree (requires explicit user approval per AGENTS.md, and only when unrelated work is not at risk).
>
> **`.opencode/pipeline.json` (gitignored):**
> 1. Restore from the OpenCode snapshot store (for example `~/.local/share/opencode/snapshot/<project-hash>` on this machine), or
> 2. Reconstruct entries with `node .opencode/scripts/pipeline-state.mjs add/update`.
>
> **Verify restoration:** Run `git status`, `openspec list`, `openspec validate --all`, and `node .opencode/scripts/pipeline-state.mjs check`.
>
> The 2026-11-01 OpenSpec files were restored from HEAD, and the 2026-11-02 pipeline state from a session snapshot.

**Rationale:**
- The two incidents used different recovery methods: tracked files from git, pipeline state from a snapshot.
- Separating the two categories makes the procedure clearer and corrects the claim that `git reset --hard` can restore `.opencode/pipeline.json` (it cannot; the file is gitignored).
- Stating that `git reset --hard` requires user approval matches AGENTS.md's existing rule for history-rewriting commands.
- Placing it in `docs/backlog.md` makes it discoverable when searching for "concurrent session" or "recovery".
- The procedure is manual (no automation added), matching the non-goals.

**Alternatives considered:**
- Add the recovery procedure to `AGENTS.md` instead of `docs/backlog.md`: rejected because `AGENTS.md` is for operational rules agents follow during normal work, not for human-driven recovery procedures.
- Add an automated recovery script: rejected as out of scope; the manual procedure is rare-path and has been successful.

### Decision 5: Update the backlog finding

**Chosen:** Update the "Two sessions in one working tree" entry in `docs/backlog.md:156-173` by adding a mitigation status line:

> - Status: mitigated by `concurrent-session-archive-safety` (2026-11-02): worktree isolation, post-archive checks, and sole-writer discipline are now rules; reopen if a shared-checkout loss recurs.

**Rationale:**
- This change addresses the finding's proposed remedy but does not eliminate the trigger (a second session in the same checkout can still cause loss); marking it "mitigated" rather than "resolved" is more accurate.
- The trigger statement is kept because it is still true (a second session in the same checkout would still cause loss), but the rule now forbids it.
- Following the repository convention of marking parked findings with their status and a reference to the addressing change (see `docs/backlog.md:273-276` for a similar pattern).

**Alternatives considered:**
- Mark the finding as `[resolved]`: rejected because the trigger remains (the technical vulnerability is unchanged; only the process rule changed).
- Remove the finding entirely: rejected because the evidence and trigger remain valuable historical context.
- Move the finding to an "Archive" section: rejected because `docs/backlog.md` does not have an archive section; mitigated items are marked in place.

## Risks / Trade-offs

**[Risk] The worktree-isolation rule is not automatically enforced.** → Mitigation: stating it explicitly in `AGENTS.md` surfaces it in every session's context. A pre-commit hook would be brittle and is not justified for a rare-path failure.

**[Trade-off] The post-archive integrity check adds ~1 second per archive.** → Accepted: the checks are already part of the normal workflow (build, test, validate); consolidating them into a mandatory gate catches silent losses immediately with negligible cost.

**[Trade-off] No automated lock file or watcher prevents concurrent writes to `.opencode/pipeline.json`.** → Accepted: the atomic compare-and-swap write in `pipeline-state.mjs` already handles concurrent writes from compliant callers; reinforcing the rule that the script is the sole writer is sufficient. A lock file would add complexity for a failure mode (direct `node -e` write) that is already explicitly forbidden.

## Open Questions

None. The worktree-isolation rule, post-archive checks, write discipline reinforcement, and recovery procedure are all mechanical additions to existing documentation. No new code is written; the `pipeline-state.mjs check` command already exists and is tested.
