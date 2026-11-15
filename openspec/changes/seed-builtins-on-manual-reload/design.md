# Design: Seed built-ins on manual reload

## Verified current state

### The only production seeder caller is startup

- `PluginInitializationService.Initialize` (`src/CLIHub/Startup/PluginInitializationService.cs:25-29`)
  is the sole production caller of `IPluginSeeder.EnsureBuiltinsExist()`; it seeds, then calls
  `IPluginCatalog.LoadPlugins()`:
  ```csharp
  public void Initialize()
  {
      this.pluginSeeder.EnsureBuiltinsExist();   // :27
      this.pluginCatalog.LoadPlugins();          // :28
  }
  ```
- `StartupStateLoader.Load` runs it once at startup (`src/CLIHub/Startup/StartupStateLoader.cs:32`).

### The manual reload path only rescans

- `AgentManagementViewModel.ReloadCatalog`
  (`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:1422`) is exactly:
  ```csharp
  private void ReloadCatalog() => this.catalog.ReloadPlugins();
  ```
  It is bound to `ReloadCatalogCommand` (`:160`) and exposed as the "Reload Catalog" menu action
  (`:166`).
- `PluginCatalog.ReloadPlugins` (`src/CLIHub.Core/Plugins/PluginCatalog.cs:71-76`) rescans via
  `LoadPluginsCore()` and raises `PluginsChanged`; it does not seed. `LoadPluginsCore`
  (`:78-213`) verifies content hashes, skips disabled plugins, adopts unknown folders, and keeps
  (never prunes) registry entries whose folder is gone (`:190-200`), which is what lets a
  later re-seed recover a deleted built-in.

### Seeding is non-throwing and non-fatal already

- `PluginSeeder.EnsureBuiltinsExist` (`src/CLIHub.Core/Plugins/PluginSeeder.cs:42-217`) wraps its
  body in `try { … } catch (Exception ex) { LogWarning; return 0; }` (`:212-216`). A write failure
  therefore never propagates to a caller — the "Seeding failure is non-fatal" requirement
  (`openspec/specs/plugin-seeding/spec.md:119-125`) is enforced inside the seeder, not by the
  startup caller. This means the manual reload can call the seeder without any new try/catch.
- The seeder preserves disabled built-ins (`PluginSeeder.cs:84-87`) and user-modified descriptors
  (`:130-135`, `:185-188`); installed-origin plugins are never touched (`:78-81`). Re-running it
  mid-session is safe and idempotent (its own test `EnsureBuiltinsExist_IsIdempotent` at
  `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs:202-213`).

### DI wiring

- Core registers `IPluginSeeder` → `PluginSeeder` (`src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:108`).
- CLIHub registers `IPluginInitializationService` → `PluginInitializationService`
  (`src/CLIHub/Composition/ServiceRegistration.cs:92`) and
  `IAgentManagementSection` → `AgentManagementViewModel` as transient (`:167`). Both projects are
  visible to the CLIHub composition root, so `IPluginSeeder` is resolvable for the view model
  without new registration code.

## Decision: re-seed missing built-ins on manual reload (Option A)

**Chosen.** The manual reload re-seeds missing built-in descriptors before rescanning, so the
"Reload Catalog" command matches startup behavior and a deleted built-in is restored in place of
requiring a restart.

**Option B (rejected)** — keep behavior and document that built-in repair only happens on
restart. This would leave a user-visible inconsistency: the same "Reload Catalog" action behaves
differently for built-in versus non-built-in descriptors, and the user has no in-app way to
recover a built-in they (or a cleanup tool) deleted. The implementation cost of Option A is one
dependency and one extra call, with the non-fatal policy already enforced by the seeder, so
documenting the limitation buys nothing.

## Chosen seam: inject `IPluginSeeder` into the view model (Seam 1)

`AgentManagementViewModel` takes `IPluginSeeder` and `ReloadCatalog()` becomes:

```csharp
private void ReloadCatalog()
{
    this.pluginSeeder.EnsureBuiltinsExist();
    this.catalog.ReloadPlugins();
}
```

Rationale:

- **Minimal and localized.** One new constructor parameter, one method grows by one line, and the
  sequence lives in a single place (the manual reload path). No other call sites change.
- **Consistent with the existing coordinator pattern.** The view model already composes
  `IPluginCatalog`, `IPluginRegistry`, `IPluginInstaller`, and `IPluginFeedClient` directly in its
  action methods (`DisablePlugin`/`EnablePlugin` at `:1327-1381`, `OnPluginFeedUpdated` at
  `:650`). Adding `IPluginSeeder` follows the same shape.
- **Non-throwing seam.** `EnsureBuiltinsExist()` swallows its own failures, so "still reload when
  seeding fails" is satisfied structurally, with no additional error handling to test or get wrong.
- **Keeps `IPluginInitializationService` startup-only.** That service's contract
  (`src/CLIHub/Startup/IPluginInitializationService.cs:5-11`) says "during application startup";
  stretching it to also drive a manual reload would dilute its single responsibility and require a
  new `Reload()` method on the interface plus an implementation and mock updates.

**Seam 2 (rejected)** — add `Reload()` to `IPluginInitializationService` and have the view model
depend on that service. Rejected because: (a) it broadens a startup coordinator into a general
reload coordinator; (b) a new interface method ripples into `StartupStateLoaderTests` and any other
mock of the interface; (c) the ordered seed-then-reload proof would move out of the view-model test
into a new `PluginInitializationServiceTests` case, splitting the behavior's test surface.

## Behavior details pinned

- **Non-fatal seeding.** `EnsureBuiltinsExist()` is non-throwing (`PluginSeeder.cs:212-216`); the
  manual reload always proceeds to `ReloadPlugins()` even when seeding returns 0 after a logged
  failure. No new error handling.
- **Disabled built-ins preserved.** The seeder skips `entry.Enabled == false` (`:84-87`), so a
  disabled built-in whose folder is missing is NOT re-seeded by a manual reload; its registry entry
  survives and the Disabled section still shows it.
- **User-modified descriptors preserved.** The three-hash branch `diskHash != storedHash` logs and
  preserves (`:130-135`); re-seeding on reload cannot clobber a hand edit.
- **Automatic reloads do NOT seed.** `DisablePlugin`/`EnablePlugin` (`:1332`, `:1363`),
  `OnPluginFeedUpdated` (`:650`), and the installer's internal catalog reload continue to call only
  `ReloadPlugins()`. `PluginCatalog` is untouched.
- **Mid-session app-update branch is inert.** The seeder's "refresh after application update"
  branch requires `embeddedHash != storedHash`, which cannot happen without a new build, so a
  manual reload only re-seeds missing descriptors and adopts matching folders — it never
  spuriously overwrites an unmodified current built-in.

## Spec amendment rationale

- `plugin-seeding` "Reload the plugin catalog on demand" (`openspec/specs/plugin-seeding/spec.md:167-177`)
  currently promises "same … rules as initial loading" and asserts, in "Removed plugin is absent
  after reload", that *any* removed descriptor is absent. Re-seeding contradicts the latter for
  built-ins. The requirement text is amended to state seed-first, the removed-plugin scenario is
  scoped to non-built-in plugins (scenario name preserved, per OpenSpec rules), and two scenarios
  are added: built-in restoration by manual reload, and seeding failure not blocking the reload.
- `plugin-installation` "Manual catalog reload" (`openspec/specs/plugin-installation/spec.md:230-238`)
  gains the seed-first step in both the requirement and its scenario.

## Open questions

- None blocking. Possible follow-ups (out of scope): whether a future explicit plugin-delete flow
  should also seed or prune; whether a reload-time "restored N built-ins" toast is warranted (the
  seeder already logs at Information, but the UI shows the restored card without a notice).
