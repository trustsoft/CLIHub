## Why

CLIHub is pre-release and there are no installations carrying the legacy unified
`config.json` that the migration path exists to convert. Keeping `ConfigMigrator`
means carrying a compatibility branch, its tests, and a spec requirement for a
population that does not exist — and it runs on every startup before any store is
constructed.

## What Changes

- **BREAKING (compatibility)**: remove the legacy single-file configuration
  migration. A present `config.json` is no longer split into `settings.json`,
  `projects.json`, and `agents.json`; the app reads only the owner files and
  falls back to built-in defaults when they are absent.
- Remove `ConfigMigrator` from Core and its call in the App composition root.
- Remove the `ConfigMigrator` unit tests.
- Drop the migration requirement from the `app-config` capability and retire the
  migration note in the documentation.

## Capabilities

### New Capabilities
<!-- None - this change only removes behavior. -->

### Modified Capabilities
- `app-config`: remove the "Migrate legacy single-file configuration" requirement
  and its scenarios. The remaining document persistence, atomic replacement, and
  backup requirements are unchanged.

## Impact

- `src/CLIHub.Core/Services/ConfigMigrator.cs` (deleted)
- `tests/CLIHub.Core.Tests/ConfigMigratorTests.cs` (deleted)
- `src/CLIHub.App/App.xaml.cs` (composition root drops the migrator call)
- `openspec/specs/app-config/spec.md` (requirement removed, purpose updated)
- `docs/vision.md`, `AGENTS.md`, `docs/architecture-map.md` (documentation)
- Scryer model: `ConfigMigrator` symbol node removed, `Configuration`
  responsibility reworded
- No API or dependency changes.
