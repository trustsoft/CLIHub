## Why

Key architectural decisions — Core/UI boundaries, the single configuration document, startup orchestration, plugin precedence, and UI dialog boundaries — exist only as prose scattered across `docs/architecture.md` and archived change artifacts. ADRs give each decision a canonical, discoverable record with its context, alternatives, and consequences.

## What Changes

- Add a `docs/adr/` directory with lightweight ADR conventions (format, status lifecycle, naming, index).
- Add five retrospective ADRs recording accepted decisions:
  - Core/UI boundaries (`CLIHub.Core` has no WPF dependency; Windows-aware infrastructure seams)
  - Single configuration document (one `config.json`, one atomic write path, narrow store adapters, schema version and migrations)
  - Startup orchestration (ordered startup coordinators with an explicit failure-policy baseline)
  - Plugin catalog and precedence (deterministic directory ordering, first-wins duplicate policy, compatibility adapter)
  - UI dialog boundaries (dialog and user-notification services instead of direct WPF dialogs in view models)
- Link the ADR index from `docs/architecture.md` and `docs/repo-structure.md`.
- ADRs document already-implemented decisions; no runtime behavior is introduced or changed.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- None. Documentation-only change; `.openspec.yaml` sets `skip_specs: true`.

## Impact

- New files: `docs/adr/README.md` plus five numbered ADR documents.
- `docs/architecture.md` and `docs/repo-structure.md` gain links to the ADR index.
- No source code, tests, or specs are affected.
