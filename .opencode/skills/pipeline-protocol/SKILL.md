---
name: pipeline-protocol
description: Lightweight OpenSpec change pipeline — plan, implement, verify, commit, archive — with approval gates, independent verification, background dispatch templates, and pipeline state updates. Use when orchestrating or implementing changes in a repository that uses OpenSpec.
---

# Pipeline protocol

A lightweight, OpenSpec-centric way to run changes with an orchestrator plus three subagents. It
optimizes for evidence over claims, small reversible steps, and a human gate on every irreversible
action.

## Invariants

1. **One change = one OpenSpec change directory.** Durable behavior changes get a proposal before
   implementation. Pure internal refactors may use `skip_specs: true`.
2. **Approval gates.** `git commit`, `git push`, and `openspec archive` happen only with explicit
   user approval for that specific action. Approval never carries over to a later commit.
3. **Independent verification.** The orchestrator re-runs the gates itself. The implementer's report
   is a hypothesis; the tree and command output are the truth.
4. **No hidden mutations.** Enrichment and side effects are explicit; read methods return detached
   copies. Subagents never run git write commands.
5. **Scope discipline.** A commit contains only the files belonging to the approved change.
6. **Truthfulness.** A subagent reports only outputs it actually observed. A failing gate is
   reported as failing, with diagnosis.
7. **Durable state.** Pipeline state lives in a file, not in chat history. Deferred findings live in
   `docs/backlog.md`.
8. **Close-out.** Every pipeline-owned entry finishes at `archive`/`done` in the session it was
   opened. A stage that does not apply is recorded `done` with an `n/a: <reason>` note; a pending
   row is never left behind a pointer that moved past it.

## Stage playbook

### 1. Plan

Dispatch `pipe/planner` in the background with a self-contained prompt:

```text
Prepare (do NOT implement) an OpenSpec change in <repo> for: <goal>.
Working tree is clean at commit <sha>.

Read AGENTS.md, docs/project-context.md, the relevant sources, and OpenSpec conventions.
Produce openspec/changes/<id>/ with proposal.md (why/what/impact + non-goals + dependencies),
design.md (verified current state with file:line, decisions, alternatives, open questions),
tasks.md (stages sized for a fresh session, gates after each stage; every task written as a
Markdown checkbox line `- [ ] 1.1 ...` so `openspec archive` can count progress — a prose task
list makes the archive report "Task status: No tasks"), and spec deltas
(MODIFIED keeps original scenario names; skip_specs for pure refactors).

Constraints: no source edits, no git write commands, no commits. Run `openspec validate --all` and
quote the result. Report: change id, files, validation result, decisions beyond the brief, open questions.
```

Then verify independently: `openspec validate --all` and inspect the files on disk (a change with
empty `specs/` directories fails validation — check, don't trust).

Commit the plan only after approval: `docs(openspec): propose <change-id>`.

### 2. Implement

Dispatch `pipe/implementer` with the change path, stage gates, and these clauses:

```text
Implement the change exactly as tasks.md prescribes. After each stage: build + tests green.
No git write commands; leave the tree dirty. Never weaken a test; deviation protocol:
stop in the area, implement the closest correct alternative, record the deviation.
Final gates: build, tests, openspec validate --all, graphify update .
Report only outputs you actually ran: stage status, files, open-question resolutions, deviations,
exact gate results, manual smoke, notes for archival.
```

### 3. Verify

Dispatch `pipe/verifier` (read-only) and, in parallel, re-run the gates yourself. Compare results.
Alongside build, tests, and `openspec validate --all`, re-run
`node .opencode/scripts/pipeline-state.mjs check` and resolve any `no-evidence`,
`inconsistent-stages`, or `no-archive-path` warning. Treat any
disagreement as a finding until resolved.

### 4. Commit

After explicit approval: stage only the change's files, commit with a conventional message
(`feat|fix|refactor|perf|docs|chore(scope): summary`), then run `openspec archive <id> --yes` and
commit the archive separately (`chore(openspec): archive <change-id>`).

The archive step MUST close the state entry in the same command sequence:
`node .opencode/scripts/pipeline-state.mjs update <id> --status done --stage-status archive=done --commit archive=<sha>`.
If it prints the no-ground-truth warning for out-of-repo tooling work, re-run the update with
`--note "tooling: <location>"` so `check` stays clean. When no OpenSpec change exists to
archive (docs-only work, out-of-repo tooling), record the archive stage as `done` with a
note beginning `n/a: <reason>` instead of running `openspec archive`; the entry still ends at
archive/done. The update also closes every earlier stage row. If `check` prints
`inconsistent-stages` or `no-archive-path`, treat it as a finding and resolve it before the
closing commit.

### 5. Smoke and close

Manual host-side checks are the user's. Record them honestly: tick the archived `tasks.md`
checkboxes only after the user confirms, in a small docs commit. Close the state entry at
`archive`/`done` before ending the session (see the close-out rule).

## OpenSpec conventions that bite

- A change without spec deltas must declare `skip_specs: true` in `.openspec.yaml`; otherwise
  `openspec validate --all` fails.
- A MODIFIED requirement must preserve the original scenario names; put new behavior into existing
  scenarios' THEN clauses or add new scenarios.
- Archive applies deltas to `openspec/specs/` and moves the change under
  `openspec/changes/archive/<date>-<id>/`; commit that move separately.
- Keep spec deltas and implementation in the same change; do not split behavior across changes.

## Pipeline state file

The sidebar plugin reads a JSON state file maintained by the orchestrator:

- `$OPENCODE_PIPELINE_FILE` if set, otherwise
- `<project>/.opencode/pipeline.json` if it already exists, otherwise
- `<project>/.opencode/pipeline/<slug>.json` (with a project install, `configRoot()` is
  `<Repo>\.opencode`, so the fallback stays inside the project; slug = sanitized project
  directory basename), otherwise
- `<project>/.opencode/pipeline/default.json` when the mapped file does not exist.

Schema (four stages; notes carry validation/review/smoke results):

```json
{
  "schemaVersion": 1,
  "project": "CLIHub",
  "updatedAt": "ISO-8601",
  "changes": [
    {
      "id": "kebab-name",
      "stage": "plan|impl|commit|archive",
      "status": "pending|active|done|blocked",
      "updatedAt": "ISO-8601",
      "commits": { "plan": "sha", "impl": "sha", "archive": "sha" },
      "stages": {
        "plan":    { "status": "done", "at": "ISO-8601", "commit": "sha", "note": "..." },
        "impl":    { "status": "done", "at": "ISO-8601", "commit": "sha", "note": "tests/review" },
        "commit":  { "status": "done", "at": "ISO-8601", "commit": "sha" },
        "archive": { "status": "done", "at": "ISO-8601", "commit": "sha", "note": "..." }
      },
      "note": "free text"
    }
  ]
}
```

Update it at every transition. A new change enters as `stage: "plan", status: "active"`; a blocked
change becomes `status: "blocked"` with the reason in `note`. Close the entry at
`archive`/`done` before the session ends (see the close-out rule). Only pipeline-owned changes
belong in the file; foreign active changes stay out.


### Write discipline (required)

- Write UTF-8 without a BOM (a leading BOM makes JSON.parse throw and the sidebar renders 'pipeline: no state' indefinitely). Do not use PowerShell Set-Content -Encoding UTF8 (it emits a BOM); use [System.IO.File]::WriteAllText (UTF-8 no BOM) or WriteAllBytes with explicitly encoded content.
- Write atomically: temp file in the same directory, then rename over the target, so a reader can never observe a truncated file. The sidebar polls every 2 s with no retry.

## Close-out rule (required)

Every pipeline-owned entry MUST finish at `stage: "archive", status: "done"` in the same
session it was opened. Docs-only work and out-of-repo tooling are not exceptions; record the
stage that does not apply instead of leaving the entry open.

- **A stage that does not apply** — docs-only work with no OpenSpec change, out-of-repo
  tooling — is recorded `status: "done"` with a `note` beginning `n/a: <reason>`. When the
  archive stage is `n/a`, `openspec archive` is not required; the entry still closes at
  archive/done.
- **Advancing the stage closes every earlier stage row** in the same update. Never leave a
  `pending` row behind a pointer that moved past it: the sidebar, `check`, and pruning assume
  every row before the pointer is done. A hand edit must backfill explicitly; the
  single-writer script does it for `update`.
- **`check` is the gate.** It warns `inconsistent-stages` (the pointer is past a non-done
  row) and `no-archive-path` (the entry can never reach archive/done). Either warning is a
  finding to resolve before the commit that closes the work, not a cosmetic note.

## Backlog policy

Deferred findings go to `docs/backlog.md` with: title, domain, severity, evidence (`file:line`),
proposed remedy, and the trigger that should bring it back. Review the backlog during `/pipe-resume`
instead of re-deriving it from conversation history.

## Session resume checklist

1. Read the state file, `docs/backlog.md`, `openspec list`, and `git log` since the last `updatedAt`;
   run `node .opencode/scripts/pipeline-state.mjs check` and resolve any `no-evidence`,
   `inconsistent-stages`, or `no-archive-path` warning (annotate the entry with
   `tooling: <location>`, close the stage, or prune it).
2. For each active change, determine its real stage from the tree and specs, not from memory.
3. Propose the next single action and wait for confirmation.
4. Never re-open archived changes silently; a regression gets a new change.
