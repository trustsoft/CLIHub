## Why

Several repository documents describe an earlier architecture and release state: they omit the Core test project, use the old configuration and plugin service names, and contain stale version and release guidance. This makes the repository harder to build, navigate, and maintain from its own documentation.

## What Changes

- Synchronize README, architecture, repository-structure, release, and agent guidance with the current solution and project files.
- Update development version references to the current `0.9.0` baseline while preserving historical release note versions.
- Document the two test projects and their actual project dependencies.
- Document `PluginCatalog`/`IPluginCatalog`, the `PluginManager` compatibility adapter, and the current configuration repository boundary.
- Correct active versus legacy window descriptions and the list of registered services.
- Verify all documentation links and the documented build/test commands against the repository.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- None. This is a documentation-only synchronization; `.openspec.yaml` explicitly sets `skip_specs: true`.

## Impact

- `README.md`, `AGENTS.md`, `docs/architecture.md`, `docs/repo-structure.md`, and `docs/releasing.md`.
- No application code, project files, CI workflow behavior, or durable capability requirements.
