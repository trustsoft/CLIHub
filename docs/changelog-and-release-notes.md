# Changelog & Release Notes — Plan (draft, decision pending)

**Status:** Discussion draft. Nothing here is implemented; this captures the options for a later decision, which will then become an OpenSpec change.

## Goal

Track what changes in CLIHub for two different audiences, and surface the user-facing side in the app:

1. **Developers** — a technical record of changes (what/why, modules, breaking changes, spec references).
2. **End users** — short, friendly **Release Notes** (like [fork.dev/releasenoteswin](https://fork.dev/releasenoteswin)), which will likely be shown in a "What's New" window in the app.

## Two documents, one voice each

| File | Audience | Voice | Groupings |
|------|----------|-------|-----------|
| `CHANGELOG.md` (repo root) | developers | technical, precise | Added / Changed / Fixed / Removed (+ `BREAKING`, spec refs) |
| `RELEASE-NOTES.md` (repo root, *name TBD*) | end users | laconic, person-oriented | New / Improved / Fixed |

Both use the same version headings (`## <version> — <date>`) so a release lines up across the two files.

### Example — `CHANGELOG.md` (technical)

```markdown
## [1.0.0] - 2026-09-27
### Added
- Agent command set (launch / resume / version / update / init) with output capture (`agent-commands`)
- Availability detection and dim/hide filtering (`agent-detection`, `agent-availability-display`)
### Changed
- **BREAKING**: plugin schema is now a `commands` object plus a `detection` block (`agent-commands`)
```

### Example — `RELEASE-NOTES.md` (user-facing)

```markdown
## 1.0.0 — 27 Sep 2026
### New
- Six built-in agents, ready out of the box: OpenCode, Pi, Cline CLI, GitHub Copilot, OpenClaude, Qwen Code
- Launch an agent in your project from the tray or with Ctrl+Shift+A
### Improved
- See every agent's version at a glance
### Fixed
- No more duplicate tray icons when launching twice
```

## Getting the user notes into the app UI

```
RELEASE-NOTES.md ──(embedded resource)──> CLIHub.Core
                                            │  IReleaseNotesService parses
                                            │  → ReleaseNote { Version, Date, New[], Improved[], Fixed[] }
                                            v
                                   "What's New" window
                                   • latest version first, scrollable history
                                   • optional: auto-open once after an update
```

- **Embedding** (not fetching) keeps notes available offline and at any time.
- The same text can later be passed to `vpk pack --releaseNotes` so the update notification and the in-app notes match.
- To parse reliably, the notes file must keep a strict shape: `## <version> — <date>` followed by `### New|Improved|Fixed` lists.

## Keeping the two files in sync — options

1. **Hand-written per release** (simplest). When cutting a version, add a section to both files. Matches this repo's hand-curated style; no new tooling.
2. **Generated from Conventional Commits** with `git-cliff` — one full config for `CHANGELOG.md`, one filtered to `feat`/`fix` for the user notes. Requires commit-message discipline.
3. **Derived from OpenSpec** — build the technical log from `openspec/changes/archive/`; the user notes still need human wording.

**Current leaning:** start with (1); revisit tooling if it becomes tedious.

## Open questions (to decide)

1. **Filename/location of the user notes** — `RELEASE-NOTES.md` at the repo root, or `docs/release-notes.md`?
2. **Auto-show "What's New"?** — open automatically once after an update, or only from the tray menu?
3. **Technical changelog source** — hand-written, or generated (Conventional Commits + git-cliff) from the start?
4. **Seed content** — should `CHANGELOG.md` / `RELEASE-NOTES.md` be seeded now with a `1.0.0` section summarizing everything delivered so far (from the archived changes)?
5. **Localization** — user notes are English now; will they ever need translation?
6. **History depth in the UI** — show only the latest version, or full scrollable history?

## Proposed next step (once decided)

An OpenSpec change, e.g. `release-notes`, that:

- adds `CHANGELOG.md` (technical) and the user-facing notes file, seeded with `1.0.0`;
- adds a capability `release-notes-display`: embed the notes, `IReleaseNotesService` (parse), and a **"What's New"** window reachable from the tray;
- defers CI / `vpk --releaseNotes` wiring to the packaging change.

## Decision log

| # | Question | Decision | Date |
|---|----------|----------|------|
| 1 | User-notes filename/location | _pending_ | |
| 2 | Auto-show What's New | _pending_ | |
| 3 | Technical changelog source | _pending_ | |
| 4 | Seed with 1.0.0 | _pending_ | |
| 5 | Localization | _pending_ | |
| 6 | UI history depth | _pending_ | |
