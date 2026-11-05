# pipeline-status (OpenCode CLI plugin)

Shows engineering-pipeline progress in the OpenCode CLI sidebar and a
drill-down panel. The plugin only **reads** state; the orchestrating agent
writes it at stage transitions.

- Sidebar: one line per change with `✓` done, `●` active, `○` pending,
  `!` blocked, the current stage as its single letter (`p` = plan, `i` = impl,
  `c` = commit, `a` = archive, the same legend the header counts use), and the
  change id. Each row is a flex row with the stage column first: the status icon,
  one space, and the stage letter, a fixed column that never shrinks, closed by
  a ` · ` separator that sits between the stage column and the name column (not
  after the icon). The change id follows, left-aligned in the remaining width.
  A row therefore reads `✓ a · usage-block-bars`, and names line up in one
  column because the name text starts with one explicit space. A name that does
  not fit is truncated by the host with OpenTUI's native middle ellipsis (for
  example `very-long-pi...going-and-going`) instead of pushing the stage column,
  and the separator stays in the fixed column, so it is never swallowed; nothing
  is right-aligned. The current change's row — the first `active` or `blocked`
  change in state-file order — renders its stage group and its name in the
  change's status color; every other row is muted. Rows are ordered by
  completion — fully completed (`status` `done` with all four stages `done`) at
  the top, work in progress next, untouched pending work at the bottom — and
  equal ranks keep their state-file order. It keeps at most 10 rows: when the
  state file holds more changes, the oldest fully completed ones are the rows
  left out of the sidebar first, where "oldest" means the earliest position in
  `changes`. Unfinished work is never hidden, so the sidebar shows more than 10
  rows when too few changes are completed, and a muted
  `+N more · /pipe` row reports how many rows it left out. The `Pipeline` header
  row carries a `▶`/`▼` chevron and, when the state is readable, the number of
  changes currently at each stage in the compact form
  `N p · N i · N c · N a` (`p` = plan, `i` = impl, `c` = commit,
  `a` = archive), in stage order. Zero-count stages are omitted from that
  suffix: an all-archived state reads `Pipeline · 10 a`, a state with one plan
  and two archives reads `Pipeline · 1 p · 2 a`, and a state whose counts are
  all zero shows no suffix at all.
  A click anywhere on that row collapses or expands the block (`▶` =
  collapsed, `▼` = expanded). The block starts expanded, and a collapsed block
  hides the change rows, the `+N more` row, and the `pipeline: no state`
  fallback. The collapsed state is remembered per project when the host offers
  storage; without it the toggle still works for the session.
- Panel: `/pipe` (slash) or the "Open pipeline status" palette command opens a
  fullscreen panel with all four stages, timestamps, commit SHAs, per-stage
  notes, and the change note. The panel ignores the sidebar's window and sort: it
  renders every change in state-file order.
- Missing, unreadable, malformed, or empty state degrades to one muted
  `pipeline: no state` row when the block is expanded. Rendering never throws.

The host discovers the plugin at `<project>\.opencode\plugins\pipeline-status\tui.tsx`
(plugin id `sidebar-suite.pipeline`); nothing registers it, and no configuration
document lists it.

The shared sidebar helpers — colors, collapse persistence, polling, and the
header row — live at `<project>\.opencode\shared\sidebar-ui.tsx`; the entry imports
them as `../../shared/sidebar-ui`, and nothing under `shared` is loaded as a plugin.

Canonical sources live in the opencode-sidebar-suite repository, and
`install.ps1` copies them into the project's `.opencode` directory.

## State file location

Resolution order:

1. `OPENCODE_PIPELINE_FILE` (absolute path) when set. This override is
   authoritative: if the file is missing, the UI reports no state instead of
   silently falling back.
2. `<session project>/.opencode/pipeline.json` **if it already exists**.
3. `<repo>/.opencode/pipeline/<slug>.json`.
4. `<repo>/.opencode/pipeline/default.json` only when the mapped file does not
   exist.

`<session project>` is the current session's `location.directory` (falling back
to the plugin location, the default location, then the process cwd).

`<slug>` is the project directory basename with characters outside
`[A-Za-z0-9._-]` replaced by `_` (for example `CLIHub`).

`configRoot()` is the directory that holds `plugins/`: the project's `.opencode`
after a project install (`...\.opencode\plugins\pipeline-status\tui.tsx` →
`...\.opencode`), or `~/.config/opencode` when the plugin is loaded from a
bundled/cache location.

A project install keeps the fallback inside that project's own `.opencode\`, so
two checkouts do not share a state file. The canonical file is
`<repo>/.opencode/pipeline.json`, written by `pipeline-state.mjs`.

The sidebar re-reads the file every 2 seconds. No OpenCode restart is needed
for state edits; a restart is needed only to load/remove the plugin itself.

## Schema

```json
{
  "schemaVersion": 1,
  "project": "CLIHub",
  "updatedAt": "2026-10-20T02:19:13+03:00",
  "changes": [
    {
      "id": "kebab-name",
      "stage": "plan|impl|commit|archive",
      "status": "pending|active|done|blocked",
      "updatedAt": "ISO-8601",
      "commits": {
        "plan": "sha",
        "impl": "sha",
        "commit": "sha",
        "archive": "sha"
      },
      "stages": {
        "plan": { "status": "done", "at": "ISO-8601", "commit": "sha", "note": "proposal" },
        "impl": { "status": "active", "at": "ISO-8601", "commit": "sha", "note": "tests, reviews" },
        "commit": { "status": "pending" },
        "archive": { "status": "pending" }
      },
      "note": "free text shown under the stage rows"
    }
  ]
}
```

Rules:

- Only four stages exist: `plan`, `impl`, `commit`, `archive`. Validation,
  review, and smoke results are `note` values inside a stage or the change,
  never separate stages.
- Required per change: a non-empty `id` and a valid `stage`. `status` defaults
  to `pending`; an unknown status is treated as `pending`.
- `commits` and `stages` are optional. Missing stage rows are derived from the
  current stage: earlier stages are `done`, the current stage uses the change
  `status`, later stages are `pending`.
- `at` / `timestamp` / `updatedAt` inside a stage are accepted as the stage
  timestamp. `commit` inside a stage overrides `commits[stage]`.
- Changes with an unknown `stage`, an empty `id`, or a duplicate `id` are
  skipped. If no valid change remains, the UI shows `pipeline: no state`.
- Order in `changes` is the state-file order, and it is the order the `/pipe`
  panel renders. The sidebar reorders its own rows instead: highest completion
  score first. The score is the number of stages whose resolved status is `done`
  (0–4) plus `0.5` when the change is `active` or `blocked`, so `archive`/`done`
  (4) ranks above `commit`/`done` (3), `impl`/`active` (1.5), `impl`/`pending`
  (1), and `plan`/`pending` (0); equal scores keep state-file order. A score of
  `4` therefore means all four stage rows are `done`.
- "Fully completed" — the only kind of row the window may drop — is stricter
  than a score of `4`: the change's own `status` must be `done` **and** all four
  stages must resolve to `done`. The top-level `stage` pointer is deliberately
  not trusted on its own, because legacy records sit at `stage` `impl` (or
  `commit`) with every stage row `done` and commit/archive notes of `n/a`; those
  are finished work and are trimmed. A record whose pointer says `archive` while
  a stage row is still unfinished is inconsistent, and it keeps its row.
- The sidebar keeps at most 10 rows. The window is decided on state-file
  positions, not on the sorted order: it drops the earliest fully completed
  entries (as defined above) until the rows fit, and it keeps every unfinished
  entry (`pending`, `active`, `blocked`, or `done` with any stage row not
  `done`). Because unfinished work is never hidden, the sidebar can exceed 10
  rows when too few changes are completed. Timestamps are never used to decide
  age.
- The plugin never deletes, prunes, or rewrites the state file; the trim and the
  sort are display-only over the parsed snapshot.
- The first change with status `active` or `blocked` in state-file order is
  highlighted in the sidebar, wherever the sort places that row: its change id
  and its ` · ` plus stage letter group render in the change's status color — the
  same color as its status icon, blue for `active` and red for `blocked` — while
  every other row is muted. The icon colors are `✓` green for `done`, `●` blue
  for `active`, `!` red for `blocked`, and `○` muted for `pending`.

## How the orchestrator updates this

**When the project provides a single-writer script** — this repository versions
`.opencode/scripts/pipeline-state.mjs` at `project/.opencode/scripts/` and deploys
it per project via `install.ps1` — every mutation goes through that script
(`update`, `check`, and the stage transitions), and direct edits to the JSON file are
forbidden. The script is the only writer; it also backfills earlier stage rows when the
pointer advances and reports `inconsistent-stages` / `no-archive-path`.

**Hosts without such a script** edit the JSON file directly at each transition. Write
atomically (write a temporary file in the same directory, then rename) so readers never
see a partial document.

An entry is not finished until `stage`/`status` is `archive`/`done`. When a stage does not
apply (docs-only work, out-of-repo tooling), record it done with a stage note beginning
`n/a: <reason>`; `openspec archive` is then not required. The global `pipeline-protocol`
skill carries the full close-out rule.

Example: move `harden-probe-caching-and-process-matching` from active
implementation to a committed implementation:

```json
{
  "id": "harden-probe-caching-and-process-matching",
  "stage": "commit",
  "status": "done",
  "updatedAt": "2026-10-20T03:10:00+03:00",
  "commits": {
    "plan": "926d933",
    "impl": "abc1234",
    "commit": "abc1234"
  },
  "stages": {
    "plan": { "status": "done", "at": "2026-10-20T02:10:25+03:00", "commit": "926d933", "note": "OpenSpec proposal committed" },
    "impl": { "status": "done", "at": "2026-10-20T03:05:00+03:00", "commit": "abc1234", "note": "tests: dotnet test CLIHub.sln passed" },
    "commit": { "status": "done", "at": "2026-10-20T03:10:00+03:00", "commit": "abc1234", "note": "implementation committed" },
    "archive": { "status": "pending" }
  },
  "note": "Implementation and verification complete; archive pending."
}
```

Then after `openspec archive`, set `stage`/`status` to `archive`/`done`, add
`commits.archive`, and fill `stages.archive` with its timestamp, commit, and
note (for example the archive commit and any final smoke result). Update the
top-level `updatedAt` every time.

## Verification

- `bun build tui.tsx --target=bun --external '*' --outfile <temp>` parses and
  transpiles the TypeScript/TSX entry (syntax and JSX check). Last run after the
  sidebar-plugins-shared-ui change: `Bundled 1 module in 5ms`,
  `ps-build-shared-a.js  15.49 KB  (entry point)`, exit code 0.
- `bun build ../../shared/sidebar-ui.tsx --target=bun --external '*' --outfile <temp>`
  transpiles the shared module the same way. Last run after the
  sidebar-plugins-shared-ui change: `Bundled 1 module in 4ms`,
  `sidebar-ui-build.js  3.80 KB  (entry point)`, exit code 0. Bun's reported
  milliseconds and byte counts vary by a hair between runs.
- `bun run verify.ts` exercises `state.ts` headlessly: path resolution, schema
  normalization, malformed/empty/invalid/override/local-precedence fixtures, the
  live state file when it exists, the sidebar window plus order
  (`sidebarChanges`, `completionScore`: drop order by state-file position,
  protection of `active`/`blocked`/`pending`, the over-limit case with too few
  completed changes, completion rank order, score tie stability, and that the
  input array is not mutated), the two-part `isCompleted` rule (legacy
  `impl`/`commit` pointers with four done stage rows are completed; a missing or
  pending stage row, or an `active`/`blocked`/`pending` status, is not), and the
  header `stageCounts` helper (a six-change fixture with stages
  `plan 1 · impl 2 · commit 1 · archive 2`, an empty array yielding all zeros,
  and the four counts summing to the input length) plus the sidebar header
  `headerCounts` segments (that fixture as `1 p · 2 i · 1 c · 2 a`, an empty
  array as no segments, ten archived changes as `10 a`, and one plan plus two
  archives as `1 p · 2 a`), and the shared `STAGE_LETTERS` map (all four stages
  to `p`/`i`/`c`/`a`, one character each, with every `headerCounts` segment
  using the letter of the stage at that position). Last run after the
  sidebar-plugins-shared-ui change printed
  `live state skipped: C:\Users\Maxim\.config\opencode\pipeline\CLIHub.json not found`
  followed by `verify: all assertions passed` (exit code 0). The live-state
  block only runs when that file exists.
- `bun -e` importing `./state.ts` printed a seven-change mixed fixture in sorted
  order — `4.0 done-archive`, `4.0 done-archive-2`, `3.0 done-commit`,
  `2.5 blocked-commit`, `1.5 active-impl`, `1.0 pending-impl`,
  `0.0 pending-plan` — while the input array kept its original order.
- A throwaway probe under `%TEMP%` parsed the real
  `D:\Projects\CLIHub\.opencode\pipeline.json` (11 changes) and printed
  `total=11 shown=10 hidden=1`, with the single hidden row
  `theme-registry -> eligible: status=done, all stage rows done (stage pointer=archive)`
  and `every hidden row eligible: true`. Of the 11 records, 8 are trimmable —
  including three legacy `impl` pointers (`model-policy-header-parity`,
  `model-policy-sidebar-order`, `fix-verifier-shell-permissions`) whose four
  stage rows are all `done` — while `theme-xaml-structure:active`,
  `live-theme-switch:pending`, and `pipeline-sidebar-limit:active` are kept.
- `bunx tsc --noEmit --jsx preserve --target esnext --module preserve --moduleResolution bundler state.ts tui.tsx verify.ts`
  is not a usable gate on this machine: it resolves the TypeScript compiler on
  demand but there is no `@types/node` and no `@opencode/plugin/tui` type
  declaration, so it reports `Cannot find name 'node:fs'`,
  `Cannot find name 'process'`, and `Cannot find module '@opencode/plugin/tui'`.
  Type safety therefore rests on `bun build` plus the headless assertions.
- Earlier verification run (OpenCode 2.0.32 with a throwaway HOME/config): the
  plugin loaded from `tui.tsx`; the log shows
  `stage=setup plugin=sidebar-suite.pipeline` completed with no plugin error. That
  headless instance rendered at 80 columns and did not reach the seeded session
  screen (it showed "New session"), so the sidebar/panel visuals were not
  captured in that harness.
- `opencode plugin list` lists
  `C:\Users\Maxim\.config\opencode\plugins\pipeline-status`.
- `C:\Users\Maxim\.config\opencode\pipeline\CLIHub.json` does not exist right
  now, so the live-state assertions in `verify.ts` are skipped. A session
  without a project-local `.opencode/pipeline.json` renders `pipeline: no
  state`; sessions in `D:\Projects\CLIHub` read that repo-local file instead.

## Activation and rollback

Activation: restart the OpenCode CLI (or let the host reload plugins) so the
project tree is re-read.

Rollback: run `.\uninstall.ps1 -ProjectDir <project>` (it removes exactly the
managed names) or delete `<project>\.opencode\plugins\pipeline-status\` by hand.
The state file can remain on disk; it is data and is never removed by either
script.

## Known risks

- The CLI plugin surface is version-specific (built against OpenCode 2.0.32).
  A future OpenCode release may change `@opencode/plugin/tui`, the slot names,
  or the OpenTUI JSX runtime and require an update.
- JSX is compiled by OpenCode's bundled `@opentui/solid` transform; this plugin
  has no local build step and no `node_modules`.
- The sidebar width is host-owned. Each row is a `row` flex layout with the
  stage column first — icon, a space, the stage letter, and the closing ` · `
  separator — which is fixed and never shrinks (`flexShrink` 0), and the change
  name second, taking the remaining width. The name is truncated only when it
  genuinely does not fit, using OpenTUI's native middle ellipsis, and the panel
  always shows the full id. Nothing is right-aligned, so a narrow sidebar costs
  the name its middle, never its column position, and the separator never
  disappears with the name.
- Polling is every 2 seconds. Non-atomic writes can briefly show
  `pipeline: no state`.
- The panel is not scrollable and renders every change in state-file order, so
  the sidebar's 10-row window and completion sort do not make the panel shorter
  or reorder it. With roughly eight or more changes the fullscreen panel can
  overflow vertically.
- Trimming depends on the record being consistent. Finished work parked at
  `stage` `impl`/`commit` with all four stage rows `done` is trimmed as
  completed; a record whose top-level `status` is `done` but which still has an
  unfinished stage row is kept, so the sidebar can exceed 10 rows until that
  record is corrected.
- Mapping is by project-directory basename, so two checkouts with the same
  basename share `<slug>.json`. Use `OPENCODE_PIPELINE_FILE` or a local
  `.opencode/pipeline.json` to disambiguate. Worktrees get their own slug.
