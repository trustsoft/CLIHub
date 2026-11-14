# 0016. Plugin lifecycle manager rejected as a load/validate facade

## Status

Accepted — 2026-11-14. Records the rejection of Proposal 5 ("Plugin lifecycle management") from the
2026-10-29 architecture analysis. The real gaps the audit surfaced are parked independently in
`docs/backlog.md` under "Plugin lifecycle".

## Context

Proposal 5 assumed plugin discovery, validation, loading, and seeding had no owner, and promised
explicit lifecycle phases, hot-reload, a marketplace, and centralized error reporting. A read-only
audit of the proposal against the current tree found those responsibilities already assigned:

- `IPluginCatalog` owns discovery and the loaded set: `LoadPlugins`/`ReloadPlugins`
  (`src/CLIHub.Core/Plugins/IPluginCatalog.cs:29,34`), raised-change notification (`:24`), and the
  three diagnostics partitions — invalid, disabled, and user-modified (`:46,52,60`). The
  implementation records them in `PluginCatalog.cs:65-76` and exposes them at `:249-258`.
- `IPluginSeeder.EnsureBuiltinsExist` owns built-in repair, sequenced with the initial load by
  `PluginInitializationService.Initialize` (`src/CLIHub/Startup/PluginInitializationService.cs:25-29`).
- `IPluginRegistry` owns the persisted lifecycle state, held in one document
  (`docs/architecture/plugins.md:91`): origin, content and logo hashes, installed version, enabled
  flag, install timestamps.
- `IPluginInstaller` owns the install transaction — staging directory, backup, atomic move, state
  recording — and reloads the catalog after commit (`PluginInstaller.cs:295-309`).
- `PluginValidationResult` already expresses validation as structured data
  (`src/CLIHub.Core/Plugins/PluginValidationResult.cs`).
- ADR-0004:15 pins the invariant the proposal collided with: plugins are data — no DLL loading, no
  in-process execution, only declared commands launched in an external terminal.

## Decision

Do not build `IPluginLifecycleManager`.

Four of the five proposed members already exist as types or methods — `ValidateAsync`, for one,
would return `PluginValidationResult`, a type the codebase already defines and uses
(`src/CLIHub.Core/Plugins/PluginValidationResult.cs`); introducing the interface would rename
existing seams rather than add capability.
The proposed aggregate `PluginCatalogResult` is less expressive than the current partition, which
keeps invalid, disabled, and user-modified entries in separate lists of distinct entry types with
per-entry diagnostics. "Activation" has no meaning for a descriptor-only plugin: nothing starts,
nothing stops, and the real transitions are install, enable/disable, and reload — each already owned
and spec-pinned. State would gain a fourth owner writing the same three things, and the three
existing owners are pinned by their own specs and tests, so a facade would have to delegate
straight through to all of them.

The promised capabilities also shipped without it: manual reload (`ReloadPlugins`), the v2 feed
marketplace in Browse Plugins, background feed sync, and the signed index (ADR-0015).

Rejected alternatives:

- *Introduce `IPluginLifecycleManager` as a thin coordinator over the existing owners* — rejected:
  it adds a hop with no policy of its own, and the delegation contracts would be re-tested twice.
- *Introduce `PluginCatalogResult` and migrate the three getters to it* — rejected: it collapses
  three differently-shaped partitions into one type and loses the entry-specific diagnostics.

## Consequences

**Easier:** the implementation seams stay where a reader finds them; each plugin behavior change
edits the owning type and adds a delta to that type's spec instead of threading through a facade;
the existing catalog, registry, installer, and seeder tests keep pinning exactly what they name.

**Harder:** there is no single place to hang cross-cutting lifecycle policy. The three real gaps the
audit found — built-in repair not running on manual reload, locally modified plugins never consulted
by the install flow, and silent registry save failures — stay visible as separate backlog items, so
each must be weighed and fixed on its own terms rather than absorbed into a new abstraction.

**Committed to:** plugins remain data (ADR-0004); lifecycle behavior changes arrive as spec deltas
to `plugin-registry`, `plugin-installation`, or `plugin-catalog`, not as a manager layer.

## Reopen triggers

1. A second plugin source appears that bypasses the signed feed index, so loading and state must
   arbitrate between origins.
2. A real activation concept arrives — a plugin declares a helper process CLIHub must start and
   stop, giving "activate"/"deactivate" non-vacuous meaning.
3. A third state store must be written transactionally with the registry, requiring a transaction or
   state-machine seam across owners.

Each of those would justify a transaction or state-machine seam between the existing owners. None of
them would justify a load/validate facade.
