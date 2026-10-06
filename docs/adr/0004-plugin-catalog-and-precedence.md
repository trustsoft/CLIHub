# 0004. Plugin catalog and precedence

## Status

Accepted — 2026-10-06. Implemented by `extract-plugin-descriptor-reader`, `extract-plugin-descriptor-validator`, `make-plugin-loading-deterministic`, `introduce-plugin-catalog-boundary`, and `add-plugin-catalog-reload`.

## Context

Plugins are folders under `%APPDATA%\CLIHub\plugins\<id>\` containing a `plugin.json` descriptor. Users can create them, the seeder creates built-in ones, and nothing prevents two folders from declaring the same `id` — for example a user copy of a seeded plugin, or a hand-edited duplicate.

With plain `Directory.EnumerateDirectories`, the order is filesystem-dependent: duplicate resolution and even the agent list order could change between machines and runs. Validation of descriptors and the loading workflow were also fused inside `PluginManager`, making both hard to test and hard to evolve.

## Decision

- **Plugins are data, always:** a plugin is a descriptor (JSON) plus an optional logo. No DLL loading, no in-process execution — CLIHub only launches the declared commands in an external terminal.
- **Deterministic loading:** plugin directories are enumerated in `OrdinalIgnoreCase` order, so the loaded set and its order are stable across machines and runs.
- **First-wins duplicate policy:** when two directories declare the same `id`, the first in that deterministic order wins; the loser is skipped and reported in loading diagnostics rather than silently dropped or merged.
- **Catalog boundary with a compatibility adapter:** `PluginCatalog` (`IPluginCatalog`) owns discovery and the loaded set; the legacy `PluginManager` remains as a shared-instance adapter so older consumers keep working.
- **Separate read and validation seams:** `IPluginDescriptorReader` performs lookup/read/deserialization; `IPluginDescriptorValidator` returns structured validation results. The catalog composes them.
- **Synchronous reload:** `Reload()` rebuilds the catalog and raises `PluginsChanged`; the initial load stays implicit at startup.

Rejected alternatives:

- *Last-wins or filesystem-order resolution* — rejected: nondeterministic; a user could not predict which duplicate is active.
- *Rejecting the whole load on a duplicate* — rejected: one broken folder must not hide every other agent.
- *Merging duplicate descriptors* — rejected: unpredictable origin of each merged field makes support impossible.

## Consequences

- Agent lists and duplicate outcomes are reproducible; the deterministic order is also what makes first-wins meaningful.
- The catalog is the single place that answers "which plugins exist"; consumers can subscribe to `PluginsChanged` instead of re-reading the disk.
- The compatibility adapter is deliberately temporary: new consumers take `IPluginCatalog`, and the adapter retires when its last consumer is migrated.
- Duplicate plugins still require the user to notice the diagnostic; the policy prevents corruption but does not police hygiene.
