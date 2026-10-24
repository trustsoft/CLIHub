# Design

## Verified current state

### Item 1 — "main window" wording in `app-lifecycle` and `preferences-ui`

Case-insensitive grep of `openspec/` and `docs/` for "main window" (2026-10-24) returns **exactly five live
occurrences** — the five the backlog cites, with no additions the backlog missed:

| # | Location | Structural context | Current text |
|---|---|---|---|
| 1 | `openspec/specs/app-lifecycle/spec.md:23` | scenario THEN — "First instance receives activation signal" (`:21`), under requirement "Single instance enforcement" (`:9`, 3 scenarios) | `the running instance shows and activates its main window` |
| 2 | `openspec/specs/app-lifecycle/spec.md:99` | requirement prose — "Startup window visibility" (`:97`, 2 scenarios) | `The application SHALL show or hide its main window at startup …` |
| 3 | `openspec/specs/app-lifecycle/spec.md:103` | scenario THEN — "Show window on startup" (`:101`) | `the main window is shown` |
| 4 | `openspec/specs/app-lifecycle/spec.md:107` | scenario THEN — "Start in the system tray" (`:105`) | `the main window is not shown and the application runs in the system tray, still reachable from the tray, the hotkey, and a subsequent launch` |
| 5 | `openspec/specs/preferences-ui/spec.md:96` | scenario THEN — "Startup window visibility applied" (`:93`), under requirement "Save applies preferences" (`:62`, 8 scenarios) | `the next application start shows the main window when the toggle is off and starts in the system tray when it is on` |

- **No Purpose involvement.** The Purposes are `app-lifecycle/spec.md:5` and `preferences-ui/spec.md:5`; a
  broader `-i "main"` grep over both files returns only the five lines above plus unrelated words
  ("remains", "primary-monitor"). The Purpose-mechanics question in the brief is therefore moot; for the
  record, a delta `## Purpose` **cannot** edit an existing capability's Purpose — the apply step warns
  `delta Purpose ignored; <spec> already has one. Edit <target> directly to change it.`
  (`dist/core/specs-apply.js:252`, seeding path `:237-287`). Purpose seeding via ADDED was the
  `launch-window-layout-rename` mechanism because that change *created* a capability.
- **No additional occurrences.** All other grep hits live in `openspec/changes/archive/**` (historical,
  untouched) and `docs/backlog.md:256,262-269` (the item itself plus the resolved rename entry's record).
- **Consistency guard.** The sibling capability specs say "the launch window" throughout:
  `openspec/specs/launch-window-layout/spec.md:13,41,45,49,57,117,169,177,217,241,245,249,265` (e.g.
  `**WHEN** the launch window is shown`) and `openspec/specs/launch-window-theme/spec.md:10,16,20,24,53,56`.
  Both edited specs already use the noun themselves — `app-lifecycle` in the "Launch window popup shell"
  requirement (`:109-147`) and at `:146,180,186,204`; `preferences-ui` at `:163,179,301,513,525`.
  `docs/glossary.md:23,120` also uses "launch window". The fix adopts exactly this phrasing.
- **Backlog item:** `docs/backlog.md:262-271` — Severity Cosmetic, remedy "normalize the window noun
  opportunistically when each file is next edited", trigger "any edit to either spec". This change is that
  edit; the trigger has fired. The archived `launch-window-layout-rename` design enumerated these five lines
  as deliberately out of its scope (its Decision 3) and proposed "a separate small change" — this one.

### Item 2 — `agent-ui` requirement naming style drift

All 13 requirements in `openspec/specs/agent-ui/spec.md`, with exact header style:

| Line | Header | Style |
|---|---|---|
| `:9` | `REQ-AGENT-CAP-002: Update Command Support Detection` | ID-style |
| `:29` | `REQ-AGENT-CAP-003: Version Command Support Detection` | ID-style |
| `:49` | `REQ-AGENT-CAP-005: Resume Command Capability Check` | ID-style |
| `:69` | `REQ-AGENT-CAP-006: Update Command Capability Check` | ID-style |
| `:89` | `REQ-AGENT-CAP-007: Version Command Capability Check` | ID-style |
| `:109` | `REQ-AGENT-CAP-008: Dynamic Command State Updates` | ID-style |
| `:132` | `REQ-AGENT-CAP-009: Launch Command Always Available` | ID-style |
| `:146` | `REQ-AGENT-CAP-010: Visual Distinction of Disabled Commands` | ID-style |
| `:160` | `REQ-AGENT-CAP-011: No Breaking Changes to Plugin Schema` | ID-style |
| `:174` | `REQ-AGENT-CAP-012: Graceful Handling of Malformed Plugins` | ID-style |
| `:194` | `Menu command enablement based on capabilities` | sentence-style |
| `:219` | `Agent card action availability` | sentence-style |
| `:289` | `Agent card displayed content` | sentence-style |

- **The brief's range is slightly imprecise:** `REQ-AGENT-CAP-001` and `-004` do not exist today. They were
  removed historically (present in `openspec/changes/archive/2026-10-09-agent-command-capability-aware-ui/specs/agent-ui/spec.md:5,72`,
  absent after `2026-10-11-remove-init-agent-command`). The ID-style set is the **ten** headers listed above
  (002, 003, 005–012), and the delta covers exactly those.
- The ID-style requirements use code-fenced GIVEN/WHEN/THEN scenario blocks (e.g. `:14-18`); the sentence-style
  ones use `- **WHEN**`/`- **THEN**` bullets (e.g. `:225-226`). Both styles validate today; neither is touched.
- **No live external references to the old names.** Repo grep for `REQ-AGENT-CAP` outside
  `openspec/changes/archive/` matches only `openspec/specs/agent-ui/spec.md` itself and
  `docs/backlog.md:276` (the backlog item, rewritten at commit/archive time). Grep for the descriptive
  suffix fragments ("Support Detection", "Capability Check", "Dynamic Command State", …) outside the spec
  returns nothing. Archived changes keep historical names untouched.
- **Backlog item:** `docs/backlog.md:273-277` — Severity Cosmetic, remedy "normalize opportunistically when
  the file is edited". This change is that edit.

### Tooling (openspec 1.15.0, `@fission-ai/openspec`, npm global)

Verified in the installed CLI (`C:\Users\Maxim\AppData\Roaming\npm\node_modules\@fission-ai\openspec`):

- Delta sections recognized: `## ADDED|MODIFIED|REMOVED|RENAMED Requirements`
  (`dist/core/parsers/requirement-blocks.js:107-110`, title set at `:156`).
- **RENAMED pair format**: a `FROM:` bullet followed immediately by its `TO:` bullet, each wrapping
  `### Requirement: <name>` (`parseRenamedPairs`, `requirement-blocks.js:403`; bullet marker optional,
  backticks optional). Unpaired/interleaved lines are refused, not guessed.
- **RENAMED semantics** (`dist/core/specs-apply.js:279-345`): applied first (order RENAMED → REMOVED →
  MODIFIED → ADDED, `:279`); the FROM name must exist in the main spec (`:321` "source not found"); the TO
  name must not exist (`:325`) and must not differ from an existing name only in case/spacing (`:328`);
  application rewrites **only the header line** of the block (`:340` `rawLines[0] = newHeader`) — bodies are
  byte-identical by construction. MODIFIED/RENAMED require an existing spec (`:305`).
- **Validate mirrors archive**: the change validator runs the archive merge builder
  (`dist/core/validation/validator.js:388` `findArchiveBlockers`), so a mistyped FROM surfaces at
  `openspec validate` time, not only at archive. Duplicate FROM/TO pairs are errors (`validator.js:336-346`),
  as is a MODIFIED block referencing a RENAMED old name (`:367`) or a RENAMED TO colliding with ADDED
  (`:370`).
- **MODIFIED semantics**: a MODIFIED requirement replaces the whole block
  (`requirement-blocks.js:483`); validate reports every current scenario name the incoming block drops as an
  ERROR (`findMissingCurrentScenarios`, `validator.js:313-318`), so each MODIFIED block below carries **all**
  scenarios of its requirement with names byte-identical, changing only the enumerated wording.
- Baseline `openspec validate --all` before this change: **29 passed, 0 failed** (exit 0; pre-existing
  long-requirement/placeholder-Purpose warnings elsewhere, unchanged by this change).

### Precedents

- `openspec/changes/archive/2026-10-24-low-sweep-2/` and `.../2026-10-24-commandline-agent-blocking/` both
  ship deltas with **no** `.openspec.yaml` (verified by directory listing with hidden files); low-sweep-2's
  proposal.md:77-79 records the rationale ("requires a delta … does not set `skip_specs`").
  `2026-10-24-launch-window-layout-rename/.openspec.yaml` exists only because it needed
  `retire_capabilities: true`.
- low-sweep-2's tasks.md stage 3 is the orchestrator-stage backlog-rewrite pattern this change follows.

## Decision 1 — Item 1 via MODIFIED blocks (wording table)

Three MODIFIED requirement blocks in two delta files; each carries the requirement's complete current text
with **every scenario name byte-identical** and only these changes:

| Location | Before | After |
|---|---|---|
| `app-lifecycle:23` | `…shows and activates its main window` | `…shows and activates its launch window` |
| `app-lifecycle:99` | `The application SHALL show or hide its main window at startup…` | `The application SHALL show or hide its launch window at startup…` |
| `app-lifecycle:103` | `the main window is shown` | `the launch window is shown` |
| `app-lifecycle:107` | `the main window is not shown and…` | `the launch window is not shown and…` |
| `preferences-ui:96` | `the next application start shows the main window when the toggle is off…` | `the next application start shows the launch window when the toggle is off…` |

The new phrasing mirrors the siblings' exact pattern ("the launch window is shown",
`launch-window-layout/spec.md:13`). Scenario counts carried: "Single instance enforcement" 3
(`:13,17,21`), "Startup window visibility" 2 (`:101,105`), "Save applies preferences" 8
(`:66,73,78,83,88,93,98,106`).

Alternatives rejected:

- **Hand-editing the main specs directly**: bypasses the archive merge and leaves no delta history; repo
  policy routes durable spec changes through OpenSpec.
- **ADDED + REMOVED pairs for the same names**: the validator errors on "Requirement present in both ADDED
  and REMOVED" (`validator.js:377`); MODIFIED exists for exactly this case.
- **RENAMED**: wrong tool — no requirement or scenario name changes here.

## Decision 2 — Item 2 via RENAMED pairs (names table)

One delta file with ten `FROM:`/`TO:` pairs. The FROM side is the full current header text after
`### Requirement:` (including the `REQ-AGENT-CAP-NNN: ` prefix — that prefix is part of the requirement
name). Bodies are not carried, so they cannot drift.

Proposed names, derived from each requirement's body (first sentence quoted) and scenario names, styled to
match the three existing sentence-style headers ("Menu command enablement based on capabilities",
"Agent card action availability", "Agent card displayed content" — sentence case, noun phrases):

| FROM (current, `spec.md:line`) | Body ("The system SHALL …") | TO (proposed) |
|---|---|---|
| `REQ-AGENT-CAP-002: Update Command Support Detection` (`:9`) | detect whether an agent supports the update command | `Update command support detection` |
| `REQ-AGENT-CAP-003: Version Command Support Detection` (`:29`) | detect whether an agent supports the version command | `Version command support detection` |
| `REQ-AGENT-CAP-005: Resume Command Capability Check` (`:49`) | enable the Resume command only when the selected agent supports session resumption | `Resume command enablement for session-capable agents` |
| `REQ-AGENT-CAP-006: Update Command Capability Check` (`:69`) | enable the Update command only when the selected agent supports updates | `Update command enablement for updatable agents` |
| `REQ-AGENT-CAP-007: Version Command Capability Check` (`:89`) | enable the Show Version command only when the selected agent supports version display | `Version command enablement for version-capable agents` |
| `REQ-AGENT-CAP-008: Dynamic Command State Updates` (`:109`) | update command enabled/disabled states immediately when agent selection changes | `Dynamic command state updates` |
| `REQ-AGENT-CAP-009: Launch Command Always Available` (`:132`) | always enable the Launch command when an agent is selected, regardless of plugin.json contents | `Launch command always available` † |
| `REQ-AGENT-CAP-010: Visual Distinction of Disabled Commands` (`:146`) | visually distinguish disabled menu commands from enabled ones | `Visual distinction of disabled commands` |
| `REQ-AGENT-CAP-011: No Breaking Changes to Plugin Schema` (`:160`) | work with existing plugin.json files without requiring schema changes | `Plugin schema compatibility` † |
| `REQ-AGENT-CAP-012: Graceful Handling of Malformed Plugins` (`:174`) | handle malformed or incomplete plugin.json files without crashing | `Graceful handling of malformed plugins` |

† judgment calls — see Open questions 1 for alternatives; defaults are as listed.

Collision check: no TO name equals (or differs only in case/spacing from) any of the 13 current names or
another TO. The enablement trio deliberately echoes the existing header's "enablement" diction and the
scenario names ("Enable Resume for Session-Capable Agent" `:53`, "Enable Update for Updatable Agent" `:73`,
"Enable Version for Version-Capable Agent" `:93`), and stays distinct from the existing
"Menu command enablement based on capabilities" (`:194`) and from each other (enablement-for-X vs
support-detection).

Alternatives rejected:

- **MODIFIED blocks with new headers**: a MODIFIED block whose header matches no current requirement fails
  ("MODIFIED failed … not found", `specs-apply.js` MODIFIED path) — header-only renames are what RENAMED is
  for; MODIFIED would also force re-carrier of every body, creating drift risk the brief forbids.
- **Keeping the descriptive suffix verbatim in Title Case** (e.g. `REQ-…-005` → `Resume Command Capability
  Check`): would fix the ID drift but not the style drift; the capability's convention is sentence case.

## Decision 3 — No `.openspec.yaml`

The change ships deltas (the deliverable), retires nothing, and needs no `skip_specs` — exactly the
low-sweep-2 / commandline-agent-blocking situation (verified: neither archived change has the file).

## Decision 4 — Backlog rewrites at commit/archive time (orchestrator stage)

Per current policy the two `docs/backlog.md` rewrites happen when the change is committed/archived, not in
the preparation or verification stages. Planned as the final orchestrator stage (tasks stage 4), following
low-sweep-2's stage-3 pattern: each entry gets the resolved format (heading " (resolved)", `Status:` /
`Resolved model:` / evidence kept as the record / `Trigger:`), one hunk per entry, nothing else in the file
touched.

## Deviations from the brief

1. **`REQ-AGENT-CAP-002…012` is non-contiguous**: `001` and `004` do not exist (removed historically;
   evidence above). The rename covers the ten ID-style headers that exist. No mechanical impact.
2. **Purpose mechanics question resolved as moot**: no "main window" occurs in either Purpose, so no
   Purpose edit is needed; the mechanics were verified anyway and are recorded (delta Purpose cannot edit an
   existing capability).
3. Everything else in the brief checked out exactly: all five backlog line numbers byte-accurate, the
   archived `launch-window-layout-rename` design.md mechanics section says what the brief claims, and
   low-sweep-2 / commandline-agent-blocking indeed shipped deltas without `.openspec.yaml`.

## Open questions

1. **Two judgment-call names — resolved by the user (2026-10-25):**
   - `REQ-AGENT-CAP-009` → **`Unconditional launch command availability`** (the noun-phrase
     alternative; the planner default `Launch command always available` was clause-shaped — the
     only proposed name with that shape — and was rejected for style drift). The delta's TO line
     is updated accordingly.
   - `REQ-AGENT-CAP-011` → default `Plugin schema compatibility` (accepted; the body's guarantee;
     the alternative described a non-goal rather than the behavior).
2. **CHANGELOG timing** — add a `Changed` line under the next version heading as part of this change, or
   leave it to the release procedure? Default assumed: leave to release time (same default the
   `launch-window-layout-rename` change recorded).
3. **Archive/commit approval** — per repo policy, `openspec archive` and `git commit` wait for explicit user
   approval; stage 4 only reports and asks.
