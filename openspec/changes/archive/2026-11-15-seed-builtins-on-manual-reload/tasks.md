# Tasks

## Stage 1 — View model and DI

- [x] 1.1 Add `IPluginSeeder` constructor parameter to `AgentManagementViewModel`
  (`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs`)
  - Add `private readonly IPluginSeeder pluginSeeder;` field after the `pluginState` field (`:54`)
  - Add the parameter after `pluginState` (`IPluginRegistry`) in the constructor signature
  - Assign `this.pluginSeeder = pluginSeeder ?? throw new ArgumentNullException(nameof(pluginSeeder));`
  - Gate: `dotnet build CLIHub.sln` (fails on the test builder — see 1.3 — not on DI;
    `IPluginSeeder` is already registered in Core at
    `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:108` and the VM is
    constructor-injected via the transient `IAgentManagementSection` registration at
    `src/CLIHub/Composition/ServiceRegistration.cs:167`; no DI change needed)

- [x] 1.2 Update `ReloadCatalog` to seed before reloading
  - Replace the body `this.catalog.ReloadPlugins();` at `:1422` with:
    ```csharp
    this.pluginSeeder.EnsureBuiltinsExist();
    this.catalog.ReloadPlugins();
    ```
  - Gate: `dotnet build CLIHub.sln` (still fails on the builder — 1.3)

- [x] 1.3 Update `AgentManagementViewModelBuilder` in
  `tests/CLIHub.Tests/Builders/AgentManagementViewModelBuilder.cs`
  - Add `private Mock<IPluginSeeder>? pluginSeeder;` field
  - Add `public AgentManagementViewModelBuilder WithSeeder(Mock<IPluginSeeder> pluginSeeder)` fluent method
  - In `Compose()`: resolve with `var seeder = this.pluginSeeder ?? new Mock<IPluginSeeder>();`
  - Pass `seeder.Object` to the `AgentManagementViewModel` constructor (after `pluginState`)
  - Add `public required Mock<IPluginSeeder> PluginSeeder { get; init; }` to `AgentMocks`
  - Gate: `dotnet build CLIHub.sln`

## Stage 2 — Test

- [x] 2.1 Add view-model tests in `AgentManagementViewModelTests.Plugins`
  - `ReloadCatalogCommand_SeedsBeforeReloadingCatalog` — use `MockSequence` with strict mocks for
    `IPluginSeeder` and `IPluginCatalog`; `seeder.EnsureBuiltinsExist()` set up before
    `catalog.ReloadPlugins()`; assert the seed call precedes the reload.
  - `ReloadCatalogCommand_ReloadsRegardlessOfSeedingResult` — configure the seeder to return 0
    (`EnsureBuiltinsExist()` returns the number of descriptors written; 0 means nothing to
    restore, not failure). Assert `catalog.ReloadPlugins()` is still called. Optionally add a
    second case where the seeder returns a positive count (descriptors restored) to prove the
    reload also runs after a real restore. Do NOT simulate failure with a throwing seeder mock —
    the real seeder swallows exceptions internally (`PluginSeeder.cs:212-216`) and the view model
    adds no try/catch (matching `PluginInitializationService.Initialize`), so a VM-level throw
    would test invented behavior. The "Seeding failure does not block manual reload" spec
    scenario is enforced by the seeder's internal catch; cite the existing
    `EnsureBuiltinsExist_StateSaveFails_LogsContextAndContinues` test
    (`tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs`) as the point of proof.
  - Keep the existing `ReloadCatalogCommand_ReloadsTheCatalog` test unchanged (the builder's
    default seeder mock returns 0 and it still verifies `ReloadPlugins()`).
  - Gate: `dotnet test CLIHub.sln`

- [x] 2.2 No new `PluginInitializationServiceTests` case needed (seam 1 keeps seeding+reload in
  the view model). Existing `Initialize_SeedsBeforeLoadingPlugins` is unchanged.
  - Gate: `dotnet test CLIHub.sln` (all existing tests pass)

## Stage 3 — Spec delta and validate

- [x] 3.1 Verify the existing delta at `specs/plugin-seeding/spec.md` against the original
  (`openspec/specs/plugin-seeding/spec.md:167-177`) — MODIFIED "Reload the plugin catalog on
  demand" keeps the scenario name "Removed plugin is absent after reload" (scoped to
  non-built-ins) and adds the "Missing built-in descriptor is restored by manual reload" and
  "Seeding failure does not block manual reload" scenarios — then tick.
- [x] 3.2 Verify the existing delta at `specs/plugin-installation/spec.md` against the original
  (`openspec/specs/plugin-installation/spec.md:230-238`) — MODIFIED "Manual catalog reload" adds
  the seed-first clause to the requirement and its scenario — then tick.
- [x] 3.3 Run `openspec validate --all` — MUST report 0 errors
  - Gate: `openspec validate --all`

## Stage 4 — Docs and backlog

- [x] 4.1 Update `docs/architecture/plugins.md` "Manual Catalog Reload" section (`:185-187`) to
  state the command first re-seeds missing built-in descriptors through the plugin seeder, then
  triggers `IPluginCatalog.ReloadPlugins()`
- [x] 4.2 Resolve `docs/backlog.md` "Built-in repair never runs after startup" entry (`:94-106`):
  add `**Status: Resolved by seed-builtins-on-manual-reload.**` and a one-line summary of the
  resolution
  - Gate: read the files to confirm correctness

## Stage 5 — Final verification

- [x] 5.1 `dotnet build CLIHub.sln`
- [x] 5.2 `dotnet test CLIHub.sln`
- [x] 5.3 `graphify update .`
- [x] 5.4 `node .gitnexus/run.cjs detect-changes --scope all --repo .` — confirm no regression
  (partial/truncated is not clean)