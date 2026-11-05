# Move Cline CLI to Feed — Tasks

## Stage 1: Remove Embedded Cline Resources

- [x] 1.1 Delete `src/CLIHub.Core/SeedPlugins/cline-cli/plugin.json`
- [x] 1.2 Delete `src/CLIHub.Core/SeedPlugins/cline-cli/logo.png`
- [x] 1.3 Delete `src/CLIHub.Core/SeedPlugins/cline-cli/` directory (should be empty after 1.1 and 1.2)
- [x] 1.4 Verify `EmbeddedPluginResources.BuiltinPluginIds()` no longer includes `"cline-cli"` by writing and running a one-line test that asserts the returned list does not contain `"cline-cli"`

**Gate:** `dotnet build CLIHub.sln` succeeds.

## Stage 2: Update Tests

- [x] 2.1 Update `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs:11` — change `BuiltinIds` array from six to five elements (remove `"cline-cli"`)
- [x] 2.2 Update seed-count assertions — change `Assert.Equal(6, ...)` to `Assert.Equal(5, ...)` in:
  - [x] Line 69 (first-run seeding)
  - [x] Line 153 (missing plugins folder)
  - [x] Line 162 (existing plugins present)
  - [x] Line 195 (deleted built-in re-seeded)
  - [x] Line 292 (unmodified built-in refreshed on app update)
  - [x] Line 374 (disabled built-in not re-seeded)
  - [x] Line 405 (each seeded descriptor is loadable)
- [x] 2.3 Remove Cline-specific command-set assertions in test `SeedDescriptors_ExposeCommandSets`:
  - [x] Remove assertion checking `plugins["cline-cli"]` command set (launch, version, no resume)
  - [x] Remove assertion checking `plugins["cline-cli"].UpdateCheck` npm source
- [x] 2.4 Verify the command-set test (`SeedDescriptors_ExposeCommandSets`) still verifies the five remaining agents
- [x] 2.5 Add a test that proves seeding writes exactly five descriptors on a fresh installation
- [x] 2.6 Update `tests/CLIHub.Tests/ViewModels/Agents/BrowsePluginsDialogViewModelTests.cs` test `LoadAsync_ReservedBuiltInId_FiltersOut`:
  - [x] Change the reserved id from `"cline-cli"` to a still-reserved built-in (e.g. `"opencode"` or `"github-copilot"`)
  - [x] Update the feed entry name and assertion to match the new id
  - [x] Keep the assertion that verifies the reserved id is filtered out
- [x] 2.7 Verify other tests using `cline` as a fixture do not assert built-in membership:
  - [x] Check `AgentProcessMatcherTests.cs` — verify it uses `cline` only as a generic npm/process fixture
  - [x] Check `AgentProcessMonitorTests.cs` (if it exists) — verify it uses `cline` only as a fixture
  - [x] Check `AgentReleaseClientTests.cs` (if it exists) — verify it uses `cline` only as a fixture
- [x] 2.8 Grep `cline-cli` in `tests/` and verify no other tests assume it is reserved or built-in

**Gate:** `dotnet test CLIHub.sln` passes; all seeding tests pass.

## Stage 3: Update Documentation

- [x] 3.1 Update `README.md:18` — change to "Built-in descriptors cover OpenCode, Pi, GitHub Copilot, OpenClaude, and Qwen Code. Cline CLI is available from the curated plugin feed; additional agents can be added as JSON plugins."
- [x] 3.2 Update `docs/glossary.md:61` — keep Cline CLI in the general Agent list (Cline still exists as an agent via the feed), or mark it as "built-in or feed"
- [x] 3.3 Update `docs/glossary.md:72` — change the Built-in agent definition to list five agents (remove Cline CLI)
- [x] 3.4 Update `docs/architecture/plugins.md:44` — change "Built-in descriptors for OpenCode, Pi, Cline CLI, GitHub Copilot, OpenClaude, and Qwen Code" to list five agents (remove Cline CLI)

**Gate:** Grep for "Cline" in `README.md`, `docs/glossary.md`, `docs/architecture/plugins.md`, and `docs/project-context.md` returns no built-in references (references to Cline as an example in `agent-process-matching` or historical changelog entries are acceptable).

## Stage 4: Update Specs (Delta)

- [x] 4.1 REMOVE requirement "Built-in descriptor coverage" and ADD requirement "Built-in descriptor set" in delta `openspec/changes/move-cline-to-feed/specs/plugin-seeding/spec.md`:
  - [x] REMOVED section includes scenarios "Six agents seeded", "Complete command sets", "Commands match the agent CLI surface", and "Unsupported command kinds are omitted" (old versions referencing Cline)
  - [x] ADDED section includes scenarios "Five agents seeded", "Each seeded descriptor is loadable", "Complete command sets", "Commands match the agent CLI surface", and "Unsupported command kinds are omitted" (new versions without Cline)
- [x] 4.2 Verify no other specs reference "six agents" or "Cline" as a built-in (grep `openspec/specs/`)

**Gate:** `openspec validate --all` passes with zero errors.

## Stage 5: Build and Verify

- [x] 5.1 Run `dotnet build CLIHub.sln --configuration Release`
- [x] 5.2 Run `dotnet test CLIHub.sln --configuration Release`
- [x] 5.3 Run `openspec validate --all` and verify zero errors
- [x] 5.4 Inspect the Release build's embedded resources (use ILSpy or `ildasm`) and confirm `SeedPlugins.cline_cli.plugin.json` and `SeedPlugins.cline_cli.logo.png` are absent
- [x] 5.5 Verify `EmbeddedPluginResources.BuiltinPluginIds()` returns five ids (OpenCode, Pi, GitHub Copilot, OpenClaude, Qwen Code) by running a minimal test harness or inspecting the release assembly
- [x] 5.6 Verify a manually crafted `.clihub-plugin.zip` with `id=cline-cli` passes the reserved-id check in `PluginInstaller` (or write a test that asserts this)

**Gate:** All checks pass. The build produces a valid CLIHub assembly with five built-in agents.

## Stage 6: Feed Deployment (Separate Repository)

**Note:** These tasks operate on `D:\Projects\CLIHub-plugins`, not the CLIHub application repository. They are performed **after** CLIHub 1.1.0 is released.

- [ ] 6.1 Create `D:\Projects\CLIHub-plugins\plugins\cline-cli\` directory
- [ ] 6.2 Copy `plugin.json` from the removed `src/CLIHub.Core/SeedPlugins/cline-cli/plugin.json` (backup from git history if needed) to `D:\Projects\CLIHub-plugins\plugins\cline-cli\plugin.json`
- [ ] 6.3 Update the copied `plugin.json`:
  - [ ] Set `"publisher": "CLIHub"` (unchanged from built-in, curator attribution)
  - [ ] Add `"minAppVersion": "1.1.0"` (new field)
  - [ ] Verify `"version": "1.0.0"` is unchanged
  - [ ] Verify commands (`launch`, `version`, `update`), `updateCheck`, and `detection` are unchanged
- [ ] 6.4 Copy `logo.png` from the removed `src/CLIHub.Core/SeedPlugins/cline-cli/logo.png` (backup from git history if needed) to `D:\Projects\CLIHub-plugins\plugins\cline-cli\logo.png`
- [ ] 6.5 Update `D:\Projects\CLIHub-plugins\reserved-ids.json` — remove `"cline-cli"` from the array (five ids remain)
- [ ] 6.6 Package the plugin:
  ```powershell
  cd D:\Projects\CLIHub-plugins
  pwsh scripts\package-plugin.ps1 cline-cli
  ```
- [ ] 6.7 Validate the feed:
  ```powershell
  cd D:\Projects\CLIHub-plugins
  node scripts\build-index.mjs --check
  ```
- [ ] 6.8 Open a pull request to `trustsoft/CLIHub-plugins` with:
  - Title: "Add Cline CLI plugin (migrated from built-in)"
  - Description: Link to CLIHub 1.1.0 release; note that Cline is no longer built-in as of that version
  - Changes: `plugins/cline-cli/`, updated `reserved-ids.json`
- [ ] 6.9 Wait for PR review and CI validation
- [ ] 6.10 Merge PR after CLIHub 1.1.0 is released (not before)

**Gate:** Feed CI passes; `index.json` is updated with the Cline entry; the GitHub Release for `cline-cli-1.0.0` exists.

## Stage 7: Smoke Test (Manual)

**Prerequisites:** CLIHub 1.1.0 is built and installed; feed PR is merged.

- [ ] 7.1 **Fresh installation smoke:**
  - [ ] Install CLIHub 1.1.0 on a clean machine (or with `%APPDATA%\CLIHub\` deleted)
  - [ ] Verify the CLI Agents page shows five built-in agents (no Cline)
  - [ ] Open Browse Plugins and verify Cline CLI appears in the feed
  - [ ] Install Cline from the feed; verify it appears in CLI Agents with `origin=installed`
  - [ ] Launch Cline; verify it executes correctly

- [ ] 7.2 **Existing installation smoke:**
  - [ ] Upgrade an existing CLIHub 1.0.x installation (with Cline already seeded) to 1.1.0
  - [ ] Verify the CLI Agents page still shows Cline with `origin=builtin` badge
  - [ ] Open Browse Plugins; verify Cline appears in the feed with version `1.0.0` and status "Current" (not "Update available")
  - [ ] Remove the Cline plugin directory manually (delete `%APPDATA%\CLIHub\plugins\cline-cli\`)
  - [ ] Reload the catalog; verify Cline is absent from CLI Agents
  - [ ] Install Cline from the feed; verify it reappears with `origin=installed`

- [ ] 7.3 **Reserved ID smoke:**
  - [ ] On CLIHub 1.1.0, attempt to install a manually crafted `.clihub-plugin.zip` with `id=cline-cli`
  - [ ] Verify the installation succeeds (the id is no longer reserved)

**Gate:** All smoke tests pass; no regressions observed.

## Stage 8: Update Knowledge Graph

- [x] 8.1 Run `graphify update .` from the repository root to refresh the project knowledge graph with the new built-in set

**Gate:** `graphify-out/graph.json` reflects the current codebase.

## Final Checklist

Before reporting completion:

- [ ] All tests pass (`dotnet test CLIHub.sln`)
- [ ] All specs validate (`openspec validate --all` reports zero errors)
- [ ] Documentation references are updated (README, glossary, architecture docs)
- [ ] Feed PR is merged and Cline is available in the feed
- [ ] Smoke tests confirm expected behavior on fresh and existing installations
- [ ] Knowledge graph is updated

## Notes

- **No git commits or archive** — This planning artifact does not include git operations. The implementation session will commit the changes.
- **Feed deployment is post-release** — Stage 6 cannot start until CLIHub 1.1.0 is released.
- **Smoke test is gated** — Stage 7 cannot start until Stage 6 is complete (feed PR merged).
