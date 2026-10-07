# Changelog & Release Notes

**Status:** Implemented by the `release-notes` change (`openspec/changes/archive/2026-09-30-release-notes/`): the two documents,
the embedded notes and their parser, the **What's New** window, and the one-time display after an upgrade.
The formerly deferred CI / `vpk --releaseNotes` wiring was delivered by the `ci-cd-release` change
(`openspec/changes/archive/2026-10-03-ci-cd-release/`); the release procedure itself lives in
[releasing.md](releasing.md).
This document remains the rationale behind the decisions below; the durable behavior lives in the
`release-notes` and `release-notes-display` specs, and the file format is described in
[architecture.md](architecture.md#release-notes).

## Goal

Track what changes in CLIHub for two different audiences, and surface the user-facing side in the app:

1. **Developers** — a technical record of changes (what/why, modules, breaking changes, spec references).
2. **End users** — short, friendly **Release Notes** (like [fork.dev/releasenoteswin](https://fork.dev/releasenoteswin)), which will likely be shown in a "What's New" window in the app.

## Two documents, one voice each

| File | Audience | Voice | Groupings |
|------|----------|-------|-----------|
| `CHANGELOG.md` (repo root) | developers | technical, precise | Added / Changed / Fixed / Removed (+ `BREAKING`, spec refs) |
| `RELEASE-NOTES.md` (repo root) | end users | laconic, person-oriented | New / Improved / Fixed |

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

## Open questions (all decided 2026-09-30)

The list below is kept as the agenda the decisions answered; the answers are in the decision log.

1. **Filename/location of the user notes** — `RELEASE-NOTES.md` at the repo root, or `docs/release-notes.md`?
2. **Auto-show "What's New"?** — open automatically once after an update, or only from the tray menu?
3. **Technical changelog source** — hand-written, or generated (Conventional Commits + git-cliff) from the start?
4. **Seed content** — should `CHANGELOG.md` / `RELEASE-NOTES.md` be seeded now with a first section summarizing everything delivered so far (from the archived changes)?
5. **Localization** — user notes are English now; will they ever need translation?
6. **History depth in the UI** — show only the latest version, or full scrollable history?

## Delivered as (the decision log above)

An OpenSpec change, `release-notes`, that:

- adds `CHANGELOG.md` (technical) and `RELEASE-NOTES.md` (user-facing), seeded with the current version;
- adds a capability `release-notes-display`: embed the notes, `IReleaseNotesService` (parse), and a **"What's New"** window reachable from the tray;
- records that CI / `vpk --releaseNotes` wiring was delivered by the packaging change (`ci-cd-release`, 2026-10-03).

## Decision log

| # | Question | Decision | Date |
|---|----------|----------|------|
| 1 | User-notes filename/location | `RELEASE-NOTES.md` at the repository root, next to `CHANGELOG.md`; embedded into `CLIHub.Core` by link (no copy under `src/`) | 2026-09-30 |
| 2 | Auto-show What's New | Yes, once: the window opens when the running version differs from `lastSeenReleaseNotesVersion` in `config.json`. The tray item reopens it any time, and opening it manually records the version too | 2026-09-30 |
| 3 | Technical changelog source | Hand-written (option 1). Tooling (`git-cliff`) is deferred until it becomes tedious | 2026-09-30 |
| 4 | Seed with a first section | Yes: one section seeded from the archived changes | 2026-09-30 |
| 5 | Localization | English only, like the rest of the UI; no localization infrastructure | 2026-09-30 |
| 6 | UI history depth | Full history, scrollable, newest version first | 2026-09-30 |

Decisions taken while implementing (recorded here because they were open in this document):

| # | Question | Decision | Date |
|---|----------|----------|------|
| 7 | Version for the seeded section | `0.5.0` — the development version at the time the notes feature was implemented, not the illustrative `1.0.0` in the examples above | 2026-09-30 |
| 8 | First run vs. upgrade | A missing recorded version means "first run": the window is not opened automatically, and the version is recorded | 2026-09-30 |
| 9 | What's New window chrome | Follows the Settings window's lifecycle (single instance, owned by the launch window) but draws its own dark chrome like the launch window; it stays an ordinary resizable window, not a popup | 2026-09-30 |
