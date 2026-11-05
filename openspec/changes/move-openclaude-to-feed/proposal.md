# Move OpenClaude to Feed

## Why

The OpenClaude plugin is currently embedded in CLIHub as one of five built-in agents seeded on every installation. Moving it to the curated plugin feed allows:

1. **Independent versioning** — OpenClaude descriptor updates can be published to the feed without waiting for a CLIHub application release
2. **Consistent plugin experience** — Users discover and install OpenClaude through the same Browse Plugins flow as other feed plugins
3. **Reduced built-in surface** — The embedded set shrinks from five to four agents, making the built-in commitment clearer and the seeding logic simpler

This change does not remove the OpenClaude plugin; it changes where new installations obtain it. Existing users keep their already-seeded OpenClaude plugin untouched.

## What Changes

### Application (CLIHub)

1. **Remove embedded resources** — Delete `src/CLIHub.Core/SeedPlugins/openclaude/plugin.json` and `logo.png` from the assembly's embedded resources
2. **Update built-in set references** — Four agents remain: OpenCode, Pi, GitHub Copilot, Qwen Code
   - `README.md:18` user-facing built-in list — state that OpenClaude is available from the curated plugin feed
   - `docs/glossary.md:61,72` Agent and Built-in agent definitions (OpenClaude remains in the general Agent list at :61)
   - `docs/architecture/plugins.md:44` built-in descriptor reference
   - `docs/project-context.md` — verified: no change needed. The built-in set is not enumerated there; `:53` only states "Built-in plugins are refreshed on app update using three-hash logic that preserves user modifications," with no agent list or count
3. **Update tests** — Adjust assertions that expect five built-in agents to expect four
   - `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs:11` `BuiltinIds` array
   - Seed-count assertions (currently `Assert.Equal(5, ...)` → `Assert.Equal(4, ...)`)
   - Command-set test removes OpenClaude assertions and continues verifying the four remaining agents
   - `tests/CLIHub.Core.Tests/Plugins/EmbeddedPluginResourcesTests.cs:7` `ExpectedBuiltinIds` array and test name at line 10
   - `tests/CLIHub.Tests/ViewModels/Agents/BrowsePluginsDialogViewModelTests.cs` test `LoadAsync_ReservedBuiltInId_FiltersOut` already uses `opencode` as a reserved id fixture (no change needed)
   - `tests/CLIHub.Core.Tests/Plugins/PluginInstallerTests.cs:301` `Inspect_BuiltinId_RejectedAgainstEmbeddedResources` already uses `opencode` (no change needed)
   - Verify other tests using `openclaude` as a fixture do not assert built-in membership
4. **Update specs** — REMOVE requirement "Built-in descriptor set" (current five-agent scenarios, reproduced verbatim), ADD requirement "Built-in agent descriptors" (count-neutral name; scenarios reference four agents — OpenCode, Pi, GitHub Copilot, Qwen Code — without OpenClaude) in `openspec/specs/plugin-seeding/spec.md`
   - MODIFIED is not used for this requirement: OpenSpec's archive compares scenario names in a MODIFIED block against the current spec and errors if any current scenario name is missing from the incoming block (a MODIFIED block replaces the whole requirement, so a scenario rename such as "Five agents seeded" → "Four agents seeded" is rejected as a scenario loss, not accepted as a rename)
   - The new requirement name is deliberately count-neutral ("Built-in agent descriptors" instead of naming a number) so a future built-in removal does not need another REMOVED/ADDED pair just to change a count
   - No changes to `openspec/specs/plugin-installation/spec.md` (reserved-id scenario does not list specific ids)

### Feed Repository (CLIHub-plugins)

1. **Publish OpenClaude as a feed plugin** — Copy the removed embedded descriptor and logo to `D:\Projects\CLIHub-plugins\plugins\openclaude\`
   - Set `"publisher": "CLIHub"` (curator attribution, consistent with the feed's curation role)
   - Set `minAppVersion` to `1.1.1` (the CLIHub version that ships this removal)
   - Retain the existing command set (`launch`, `resume`, `version`, `update`)
2. **Update `reserved-ids.json`** — Remove `"openclaude"` from the array, as it is no longer a built-in id
3. **Package and publish** — Follow the feed's [CONTRIBUTING.md](https://github.com/trustsoft/CLIHub-plugins/blob/main/CONTRIBUTING.md) workflow:
   - Run `scripts/package-plugin.ps1 openclaude` locally
   - Validate with `node scripts/build-index.mjs --check`
   - Open a pull request; CI validates, packages, publishes a GitHub Release, and updates `index.json`

## Impact

### Migration Semantics

- **Existing users** — The already-seeded OpenClaude descriptor and `origin=builtin` state remain untouched. Seeding no longer manages it (three-hash logic will not refresh it). If the user removes the OpenClaude plugin directory, they can reinstall it from the feed.
- **New installations** — CLIHub 1.1.1 and later do not seed OpenClaude. Users discover and install it from the Browse Plugins page, where it appears as a feed plugin with `origin=installed`.
- **No automatic migration** — Existing `origin=builtin` OpenClaude entries do not auto-convert to `origin=installed`. The feed publishes OpenClaude at version `1.0.0` (matching the embedded version), so users with the built-in OpenClaude see "Current" on the feed entry's version indicator.

### Reserved ID Lifecycle

1. **Before the CLIHub release** — `openclaude` is reserved (embedded resource in the current build + `reserved-ids.json` in the feed). Feed submissions with `id=openclaude` are rejected by feed CI.
2. **CLIHub 1.1.1 ships** — The embedded resource is removed. `PluginInstaller` derives reserved ids from `EmbeddedPluginResources.BuiltinPluginIds()`, so `openclaude` is no longer reserved in that build or later.
3. **Feed PR merged after the release** — `reserved-ids.json` is updated to remove `openclaude`. The feed's OpenClaude entry becomes visible in Browse Plugins.

The feed's `reserved-ids.json` must not be updated before the CLIHub 1.1.1 release. If the feed publishes OpenClaude before 1.1.1 ships, users on 1.1.0 will see OpenClaude in Browse Plugins (the list shows entries with `minAppVersion` above the running version but disables Install), and local/manual package installs will fail with "Plugin id 'openclaude' is reserved for the built-in agent 'openclaude' and cannot be installed."

### Release Dependency

The feed plugin can be published **after** CLIHub 1.1.1 is released, not before. The `minAppVersion=1.1.1` field ensures the Install button is disabled on 1.1.0 (Browse Plugins shows entries but disables Install when `minAppVersion` exceeds the running version). Publishing after the release avoids the reserved-id rejection on local/manual package installs for users on 1.1.0.

## Non-Goals

- **No automatic removal of existing OpenClaude plugins** — The change does not delete user content. Existing `origin=builtin` OpenClaude entries remain.
- **No origin migration** — The change does not convert `origin=builtin` entries to `origin=installed`. Users who remove and reinstall OpenClaude will have `origin=installed` on the new copy.
- **No feed entry before the release** — The feed PR is opened after CLIHub 1.1.1 is released, not before. The `reserved-ids.json` update and the OpenClaude feed entry land in the same PR.
- **No version bump in this change** — `Directory.Build.props` remains at `1.1.0`; the version bump to `1.1.1` is release preparation work, outside the scope of this change. The release dependency is documented in this proposal and in tasks.md.

## Dependencies

None. This change is self-contained.
