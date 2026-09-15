## REMOVED Requirements

### Requirement: Migrate legacy single-file configuration

**Reason**: CLIHub is pre-release and no installation carries the legacy unified
`config.json`; the migration branch, its tests, and this requirement exist only
for that population. Owner-file loading already falls back to built-in defaults,
so the application remains correct without migration.

**Migration**: None. A legacy `config.json` is ignored: the system reads
`settings.json`, `projects.json`, and `agents.json` and falls back to built-in
defaults when they are absent. Any legacy data that must be preserved is moved
into the owner files by hand.
