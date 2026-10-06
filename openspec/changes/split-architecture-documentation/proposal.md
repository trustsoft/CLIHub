## Why

`docs/architecture.md` has grown to ~400 lines covering distinct topics (startup, configuration, plugins, process execution, UI) in one file. Topical documents are easier to navigate, link, and keep in sync as each area evolves.

## What Changes

- Split topic-specific sections out of `docs/architecture.md` into dedicated documents under `docs/architecture/`: startup (startup/shutdown lifecycle and failure policy), configuration (persistence, schema, migrations), plugins (descriptor format, catalog, seeding), processes (process execution and runtime spawning), and UI (windows, MVVM, themes).
- Keep `docs/architecture.md` as the entry point: solution structure, project responsibilities, dependency flow, technology stack, capabilities, testing strategy, conventions, build/run, security overview — with links to the new topical documents.
- Update cross-references in `docs/repo-structure.md`, `AGENTS.md`, and other docs that point to architecture sections.
- Content moves without behavioral rewrites; wording may be adjusted only where a section references its former location.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- None. This is a documentation-structure change with no runtime behavior; `.openspec.yaml` sets `skip_specs: true`.

## Impact

- `docs/architecture.md` is reduced and gains links to new `docs/architecture/*.md` documents.
- New files: `docs/architecture/startup.md`, `configuration.md`, `plugins.md`, `processes.md`, `ui.md` (exact names finalized in design).
- Cross-references updated in `docs/repo-structure.md`, `AGENTS.md`, and within `docs/`.
- No source code, tests, or specs are affected.
