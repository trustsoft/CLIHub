# Design: slim-core-api-surface

## Context

The P5 review (improvements.md) plus an interface-member usage audit found: duplicated project-domain members on `IConfigService`, six interface members with zero production callers, an internal mutable list handed out by `PluginManager`, placeholder files written into user plugin folders, and tripled serializer options. See proposal.md.

## Goals / Non-Goals

**Goals:**

- Interfaces read as the intent of the system: only members with real consumers.
- Core stops mutating user content (no placeholder files inside plugin folders).
- One shared serializer configuration.
- No test coverage lost.

**Non-Goals:**

- Touching `GetRecentProjects` (live in the tray menu and spec-backed).
- Reworking `MainWindow`-era code or the UI layer beyond the agent logo fallback.
- Adding new capabilities (portable mode, favorites listing, plugin lookup by ID) — removals are not deferrals; if a need reappears, the member returns with a consumer.

## Decisions

### 1. Removals, not deprecations

All dropped members have zero production callers (verified by audit); the project is the only consumer of Core. `[Obsolete]` shims would keep the surface wide for no one.

*Alternative considered*: deprecate-and-remove-later — rejected: no external API consumers exist to warn.

### 2. Placeholder provisioning replaced by a UI fallback, not by seeder work

The seeder only runs against an empty plugins folder, so it cannot provision placeholders for user-created plugin folders — moving `CreatePlaceholderLogo` there would leave the gap open. Instead: `PluginManager` resolves `logo.png` or null (negative-cached by `LogoCacheService`, exactly like projects), and the launch view model substitutes the shipped `default-project.png` when the path is null.

*Why this is spec-aligned*: `plugin-seeding` already requires "missing logo falls back to the default logo"; the transparent placeholder PNG effectively rendered nothing, so agents without a logo gain a visible default icon — a small, user-visible improvement the user confirmed. Leftover `assets\placeholder-logo.png` files in existing plugin folders become inert and are not cleaned up (never touch user content).

### 3. `IReadOnlyList<Plugin>` from `GetAllPlugins`

The list is loaded once at startup; consumers iterate or LINQ over it. `IReadOnlyList` prevents casts to `List<Plugin>` while avoiding per-call copies. `AgentListComposer.Compose` takes `IEnumerable<Plugin>` — unchanged.

### 4. One shared options type, tests updated

`internal static class CoreJson` (working name) exposes the single camelCase/indented `JsonSerializerOptions`. `AppConfigSerializationTests` switch from `ConfigService.JsonOptions` to the shared symbol; the internal seam exists anyway (`InternalsVisibleTo`).

### 5. `ResolveLogo` demoted to `internal` on `ProjectService`

The method's semantics (scan candidates, fallback to default) remain worth testing, but the path-only signature lost its role to the ID-keyed cache. `InternalsVisibleTo("CLIHub.Tests")` keeps the three tests meaningful without holding a place on the public interface.

## Risks / Trade-offs

- [A removed member is needed later] → The audit showed zero callers; re-adding a member with a real consumer is a small, honest change.
- [Logo-less agents change appearance] → From invisible (transparent PNG) to the default icon — the documented, spec-required behavior; confirmed with the user.
- [`GetAllPlugins` signature change ripples] → All consumers bind through `IEnumerable`/`foreach`/LINQ; `IReadOnlyList<Plugin>` satisfies them without edits.

## Migration Plan

Pure internal refactor; no data, config, or on-disk format changes (leftover placeholder files stay untouched). Rollback is a plain revert.

## Open Questions

None.
