# Move Cline CLI to Feed

## Why

The Cline CLI plugin is currently embedded in CLIHub as one of six built-in agents seeded on every installation. Moving it to the curated plugin feed allows:

1. **Independent versioning** — Cline descriptor updates can be published to the feed without waiting for a CLIHub application release
2. **Consistent plugin experience** — Users discover and install Cline through the same Browse Plugins flow as other feed plugins
3. **Reduced built-in surface** — The embedded set shrinks from six to five agents, making the built-in commitment clearer and the seeding logic simpler

This change does not remove the Cline plugin; it changes where new installations obtain it. Existing users keep their already-seeded Cline plugin untouched.

## What Changes

### Application (CLIHub)

1. **Remove embedded resources** — Delete `src/CLIHub.Core/SeedPlugins/cline-cli/plugin.json` and `logo.png` from the assembly's embedded resources
2. **Update built-in set references** — Five agents remain: OpenCode, Pi, GitHub Copilot, OpenClaude, Qwen Code
   - `README.md:18` user-facing built-in list — state that Cline CLI is available from the curated plugin feed
   - `docs/glossary.md:72` Built-in agent definition (Cline remains in the general Agent list at :61)
   - `docs/architecture/plugins.md:44` built-in descriptor reference
   - `docs/project-context.md` runtime facts
3. **Update tests** — Adjust assertions that expect six built-in agents to expect five
   - `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs:11` `BuiltinIds` array
   - Seed-count assertions (currently `Assert.Equal(6, ...)` → `Assert.Equal(5, ...)`)
   - Command-set test removes Cline assertions and continues verifying the five remaining agents
   - `tests/CLIHub.Tests/ViewModels/Agents/BrowsePluginsDialogViewModelTests.cs` test `LoadAsync_ReservedBuiltInId_FiltersOut` changes its reserved id from `cline-cli` to a still-reserved built-in (e.g. `opencode`)
   - Verify other tests using `cline` as a fixture (`AgentProcessMatcherTests`, etc.) do not assert built-in membership
4. **Update specs** — REMOVE old requirement "Built-in descriptor coverage", ADD new requirement "Built-in descriptor set" in `openspec/specs/plugin-seeding/spec.md`
   - Old scenarios referencing six agents and Cline are removed
   - New scenarios reference five agents (OpenCode, Pi, GitHub Copilot, OpenClaude, Qwen Code) without Cline
   - No changes to `openspec/specs/plugin-installation/spec.md` (reserved-id scenario does not list specific ids)

### Feed Repository (CLIHub-plugins)

1. **Publish Cline as a feed plugin** — Copy the removed embedded descriptor and logo to `D:\Projects\CLIHub-plugins\plugins\cline-cli\`
   - Set `"publisher": "CLIHub"` (curator attribution, consistent with the feed's curation role)
   - Set `minAppVersion` to `1.1.0` (the CLIHub version that ships this removal)
   - Retain the existing command set (`launch`, `version`, `update`; no `resume`)
2. **Update `reserved-ids.json`** — Remove `"cline-cli"` from the array, as it is no longer a built-in id
3. **Package and publish** — Follow the feed's [CONTRIBUTING.md](https://github.com/trustsoft/CLIHub-plugins/blob/main/CONTRIBUTING.md) workflow:
   - Run `scripts/package-plugin.ps1 cline-cli` locally
   - Validate with `node scripts/build-index.mjs --check`
   - Open a pull request; CI validates, packages, publishes a GitHub Release, and updates `index.json`

## Impact

### Migration Semantics

- **Existing users** — The already-seeded Cline descriptor and `origin=builtin` state remain untouched. Seeding no longer manages it (three-hash logic will not refresh it). If the user removes the Cline plugin directory, they can reinstall it from the feed.
- **New installations** — CLIHub 1.1.0 and later do not seed Cline. Users discover and install it from the Browse Plugins page, where it appears as a feed plugin with `origin=installed`.
- **No automatic migration** — Existing `origin=builtin` Cline entries do not auto-convert to `origin=installed`. The feed publishes Cline at version `1.0.0` (matching the embedded version), so users with the built-in Cline see "Current" on the feed entry's version indicator.

### Reserved ID Lifecycle

1. **Before the CLIHub release** — `cline-cli` is reserved (embedded resource in the current build + `reserved-ids.json` in the feed). Feed submissions with `id=cline-cli` are rejected by feed CI.
2. **CLIHub 1.1.0 ships** — The embedded resource is removed. `PluginInstaller` derives reserved ids from `EmbeddedPluginResources.BuiltinPluginIds()`, so `cline-cli` is no longer reserved in that build or later.
3. **Feed PR merged after the release** — `reserved-ids.json` is updated to remove `cline-cli`. The feed's Cline entry becomes visible in Browse Plugins.

The feed's `reserved-ids.json` must not be updated before the CLIHub 1.1.0 release. If the feed publishes Cline before 1.1.0 ships, users on 1.0.x will see Cline in Browse Plugins (the list shows entries with `minAppVersion` above the running version but disables Install), and local/manual package installs will fail with "Plugin id 'cline-cli' is reserved for the built-in agent 'cline-cli' and cannot be installed."

### Release Dependency

The feed plugin can be published **after** CLIHub 1.1.0 is released, not before. The `minAppVersion=1.1.0` field ensures the Install button is disabled on 1.0.x (Browse Plugins shows entries but disables Install when `minAppVersion` exceeds the running version). Publishing after the release avoids the reserved-id rejection on local/manual package installs for users on 1.0.x.

## Non-Goals

- **No automatic removal of existing Cline plugins** — The change does not delete user content. Existing `origin=builtin` Cline entries remain.
- **No origin migration** — The change does not convert `origin=builtin` entries to `origin=installed`. Users who remove and reinstall Cline will have `origin=installed` on the new copy.
- **No feed entry before the release** — The feed PR is opened after CLIHub 1.1.0 is released, not before. The `reserved-ids.json` update and the Cline feed entry land in the same PR.

## Dependencies

None. This change is self-contained.
