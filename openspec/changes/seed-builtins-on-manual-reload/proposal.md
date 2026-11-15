# Seed built-ins on manual reload

## Why

The manual "Reload Catalog" command rescans the plugins directory but does not re-seed missing
built-in descriptors. Deleting a built-in descriptor (e.g., `plugins\opencode\plugin.json`) and
clicking "Reload Catalog" leaves the built-in absent until the next application restart, although
startup always heals it. This is the gap documented at `docs/backlog.md:94-106` ("Built-in repair
never runs after startup").

The fix is one call: run the plugin seeder's `EnsureBuiltinsExist()` before the catalog reload,
matching the startup sequence (`PluginInitializationService.Initialize` at
`src/CLIHub/Startup/PluginInitializationService.cs:27-28`). The seeder already uses three-hash
logic that preserves disabled built-ins and user-modified descriptors, so the manual reload
inherits the same safety net.

## What Changes

- **AgentManagementViewModel** gains an `IPluginSeeder` constructor dependency.
- **ReloadCatalog** (`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:1422`) calls
  `EnsureBuiltinsExist()` before `catalog.ReloadPlugins()`. Seeding failure is non-fatal (the
  seeder catches all exceptions internally) — the catalog still reloads.
- **Spec amendments** in `plugin-seeding` and `plugin-installation`: the manual reload requirement
  now says it re-seeds missing built-in descriptors before rescanning. The "Removed plugin is
  absent after reload" scenario in `plugin-seeding` is scoped to non-built-in plugins; a new
  scenario covers built-in restoration by manual reload.
- **Plugin catalog** (`PluginCatalog.ReloadPlugins`) is never changed — automatic reloads
  (installer, feed event, disable/enable) do NOT seed.
- **Documentation** (`docs/architecture/plugins.md` "Manual Catalog Reload" section) updated to
  reflect the seed-first behavior.
- **Backlog** entry "Built-in repair never runs after startup" (`docs/backlog.md:94-106`) is
  resolved.

## Impact

- Modified code: `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` (constructor +
  `ReloadCatalog`).
- Modified tests: `tests/CLIHub.Tests/Builders/AgentManagementViewModelBuilder.cs` (new constructor
  argument) and `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Plugins.cs`
  (one new test proving the seed-then-reload ordering; existing reload tests kept intact).
- Modified specs: `plugin-seeding` (reload requirement, one scenario scoped to non-built-in
  plugins (name unchanged), two new scenarios), `plugin-installation` (manual reload requirement
  and its scenario).
- Modified docs: `docs/architecture/plugins.md`, `docs/backlog.md`.
- No DI change: `IPluginSeeder` is already registered in Core
  (`src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:108`), and the view model is
  constructor-injected through the transient `IAgentManagementSection` registration
  (`src/CLIHub/Composition/ServiceRegistration.cs:167`).
- Verification: `dotnet build CLIHub.sln` + `dotnet test CLIHub.sln` after each stage;
  `openspec validate --all`; `graphify update .` at the end.

## Non-Goals

- No change to `PluginCatalog.ReloadPlugins` — automatic reload paths (install, feed event,
  disable/enable) stay seed-free.
- No change to the seeder's three-hash logic, disabled-built-in skip, or non-fatal failure policy.
- No new abstractions — the view model composes the seeder directly, consistent with how it
  already composes `IPluginCatalog`, `IPluginRegistry`, and `IPluginInstaller`.
- No change to the startup path — `PluginInitializationService.Initialize` remains unchanged.

## Dependencies

- Base: d0e78e5 (working tree clean). No other active changes.
- Consumes, does not modify: `IPluginSeeder.EnsureBuiltinsExist()`, `IPluginCatalog.ReloadPlugins()`.
- No file overlap with concurrent work.