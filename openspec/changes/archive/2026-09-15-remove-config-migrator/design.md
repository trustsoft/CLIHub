## Context

`ConfigMigrator` runs once in the App composition root (`src/CLIHub.App/App.xaml.cs`)
before the stores are constructed. When a legacy `config.json` exists and
`settings.json` does not, it splits the legacy file into `settings.json`,
`projects.json`, and `agents.json`, then renames the original to
`config.json.migrated`. See `proposal.md` — Why for motivation.

## Goals / Non-Goals

**Goals:**
- Remove the migration path: class, its composition-root call, its tests, the
  `app-config` requirement, and the documentation that describes it.
- Keep the owner-file persistence contract (`app-config`) unchanged.

**Non-Goals:**
- No replacement or on-demand import tool for legacy files.
- No change to `JsonDocumentStore`, `SettingsStore`, or the document formats.

## Decisions

- **Delete rather than relocate.** Owner-file loading already falls back to
  built-in defaults, so the application is correct without migration; the only
  loss is automatic conversion of a legacy file, which no current installation
  has. Alternative — inline the migration into `SettingsStore` — rejected: it
  keeps the compatibility branch and the spec requirement for no beneficiary.
- **Composition root, not a hidden startup hook.** The call disappears with the
  class; `App.xaml.cs` continues to construct the three stores directly.

## Risks / Trade-offs

- A hypothetical pre-release installation still carrying `config.json` would not
  be migrated and would start from defaults, losing settings/projects/agents →
  accepted, and recorded in the removed requirement's Migration note.
