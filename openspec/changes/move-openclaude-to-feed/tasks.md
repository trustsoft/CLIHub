# Move OpenClaude to Feed — Tasks

## Stage 1: Remove Embedded OpenClaude Resources

- [x] 1.1 Delete `src/CLIHub.Core/SeedPlugins/openclaude/plugin.json`
- [x] 1.2 Delete `src/CLIHub.Core/SeedPlugins/openclaude/logo.png`
- [x] 1.3 Delete `src/CLIHub.Core/SeedPlugins/openclaude/` directory (should be empty after 1.1 and 1.2)

**Gate:** `dotnet build CLIHub.sln` succeeds. (The permanent assertion that `BuiltinPluginIds()` no longer includes `"openclaude"` is added as test `BuiltinPluginIds_NoLongerContainsOpenClaude` in task 2.7 — no throwaway test is created in this stage.)

## Stage 2: Update Tests

- [x] 2.1 Update `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs:11` — change `BuiltinIds` array from five to four elements (remove `"openclaude"`)
- [x] 2.2 Update seed-count assertions — change `Assert.Equal(5, ...)` to `Assert.Equal(4, ...)` in:
  - [x] Line 69 (first-run seeding)
  - [x] Line 154 (missing plugins folder)
  - [x] Line 163 (existing plugins present)
  - [x] Line 196 (deleted built-in re-seeded)
  - [x] Line 293 (unmodified built-in refreshed on app update)
  - [x] Line 375 (disabled built-in not re-seeded)
  - [x] Line 406 (each seeded descriptor is loadable)
- [x] 2.3 Remove OpenClaude-specific command-set assertions:
  - [x] In test `SeededDescriptors_ExposeExpectedCommandSets` (line 413), remove the assertion checking `plugins["openclaude"]` (line 430)
  - [x] In test `SeededDescriptors_DeclareExpectedUpdateSources` (line 436), remove the assertion checking `plugins["openclaude"].UpdateCheck` (line 452)
- [x] 2.4 Verify the command-set tests still verify the four remaining agents (OpenCode, Pi, GitHub Copilot, Qwen Code)
- [x] 2.5 Update `tests/CLIHub.Core.Tests/Plugins/EmbeddedPluginResourcesTests.cs:7` — change `ExpectedBuiltinIds` array from five to four elements (remove `"openclaude"`)
- [x] 2.6 Update `tests/CLIHub.Core.Tests/Plugins/EmbeddedPluginResourcesTests.cs:10` — rename test from `BuiltinPluginIds_EnumeratesTheFiveBuiltInAgents` to `BuiltinPluginIds_EnumeratesTheFourBuiltInAgents`
- [x] 2.7 Add test `BuiltinPluginIds_NoLongerContainsOpenClaude` in `EmbeddedPluginResourcesTests` that asserts `"openclaude"` is not in the returned ids
- [x] 2.8 Verify `tests/CLIHub.Tests/ViewModels/Agents/BrowsePluginsDialogViewModelTests.cs` test `LoadAsync_ReservedBuiltInId_FiltersOut` still uses `"opencode"` as its reserved id (no change needed)
- [x] 2.9 Verify `tests/CLIHub.Core.Tests/Plugins/PluginInstallerTests.cs:301` test `Inspect_BuiltinId_RejectedAgainstEmbeddedResources` still uses `"opencode"` as its reserved id (no change needed)
- [x] 2.10 Grep `openclaude` in `tests/` and verify no other tests assume it is reserved or built-in

**Gate:** `dotnet test CLIHub.sln` passes; all seeding tests pass.

## Stage 3: Update Documentation

- [x] 3.1 Update `README.md:18` — change to "Built-in descriptors cover OpenCode, Pi, GitHub Copilot, and Qwen Code. Cline CLI and OpenClaude are available from the curated plugin feed; additional agents can be added as JSON plugins."
- [x] 3.2 Update `docs/glossary.md:61` — keep OpenClaude in the general Agent list (OpenClaude still exists as an agent via the feed)
- [x] 3.3 Update `docs/glossary.md:72` — change the Built-in agent definition to list four agents (remove OpenClaude): "OpenCode, Pi, GitHub Copilot, Qwen Code."
- [x] 3.4 Update `docs/architecture/plugins.md:44` — change "Built-in descriptors for OpenCode, Pi, GitHub Copilot, OpenClaude, and Qwen Code" to list four agents (remove OpenClaude)

**Gate:** Grep for "OpenClaude" in `README.md`, `docs/glossary.md`, and `docs/architecture/plugins.md` returns no built-in references (references to OpenClaude as an example agent or in the general Agent list are acceptable).

## Stage 4: Update Specs (Delta)

**Note:** MODIFIED cannot be used for this requirement. OpenSpec's archive validator (`findScenarioLossIssues` in the installed CLI's `validator.js`) compares the MODIFIED block's scenario names against the current spec's scenario names and errors if any current name is missing — there is no scenario-rename mechanism (`diffScenarioNames` is a pure name-multiset diff). A MODIFIED block also replaces the whole requirement verbatim, so keeping both "Five agents seeded" and "Four agents seeded" scenario names side by side (to dodge the validator error) would archive two contradictory scenarios into the durable spec. The correct form is REMOVED + ADDED, with the new requirement given a count-neutral name so future agent-count changes do not need another rename.

- [x] 4.1 In delta `openspec/changes/move-openclaude-to-feed/specs/plugin-seeding/spec.md`, REMOVE requirement "Built-in descriptor set":
  - [x] REMOVED section reproduces the current five-agent requirement verbatim: scenarios "Five agents seeded", "Each seeded descriptor is loadable", "Complete command sets", "Commands match the agent CLI surface", "Unsupported command kinds are omitted"
- [x] 4.2 In the same delta file, ADD requirement "Built-in agent descriptors" (new, count-neutral name — does not state a number so a future built-in removal can MODIFY it without renaming it again):
  - [x] ADDED section includes scenario "Built-in agents seeded" stating descriptors for OpenCode, Pi, GitHub Copilot, and Qwen Code are available
  - [x] ADDED section includes scenario "Each seeded descriptor is loadable" (unchanged body)
  - [x] ADDED section includes scenario "Complete command sets" stating OpenCode, Pi, GitHub Copilot, and Qwen Code each define `launch`, `resume`, `version`, and `update`
  - [x] ADDED section includes scenarios "Commands match the agent CLI surface" and "Unsupported command kinds are omitted" (generic examples, unchanged bodies)
- [x] 4.3 Verify no other specs reference "five agents" or "OpenClaude" as a built-in (grep `openspec/specs/`)

**Gate:** `openspec validate --all` passes with zero errors. Also run `openspec validate move-openclaude-to-feed --type change` and confirm it reports the change as valid.

## Stage 5: Build and Verify

- [x] 5.1 Run `dotnet build CLIHub.sln --configuration Release`
- [x] 5.2 Run `dotnet test CLIHub.sln --configuration Release`
- [x] 5.3 Run `openspec validate --all` and verify zero errors
- [x] 5.4 Inspect the Release build's embedded resources (use ILSpy or `ildasm`) and confirm `SeedPlugins.openclaude.plugin.json` and `SeedPlugins.openclaude.logo.png` are absent
- [x] 5.5 Verify `EmbeddedPluginResources.BuiltinPluginIds()` returns four ids (OpenCode, Pi, GitHub Copilot, Qwen Code) by running a minimal test harness or inspecting the release assembly
- [x] 5.6 Verify a manually crafted `.clihub-plugin.zip` with `id=openclaude` passes the reserved-id check in `PluginInstaller` (or write a test that asserts this)

**Gate:** All checks pass. The build produces a valid CLIHub assembly with four built-in agents.

## Stage 6: Feed Deployment (Separate Repository)

**Note:** These tasks operate on `D:\Projects\CLIHub-plugins`, not the CLIHub application repository. They are performed **after** CLIHub 1.1.1 is released.

**Prerequisites:**

- [ ] 6.0 Verify CLIHub 1.1.1 is released (tag `v1.1.1` exists; `Directory.Build.props` has `<Version>1.1.1</Version>` on the release branch/tag)

**Feed tasks:**

- [ ] 6.1 Create `D:\Projects\CLIHub-plugins\plugins\openclaude\` directory
- [ ] 6.2 Copy `plugin.json` from the removed `src/CLIHub.Core/SeedPlugins/openclaude/plugin.json` (backup from git history if needed) to `D:\Projects\CLIHub-plugins\plugins\openclaude\plugin.json`
- [ ] 6.3 Update the copied `plugin.json`:
  - [ ] Set `"publisher": "CLIHub"` (unchanged from built-in, curator attribution)
  - [ ] Add `"minAppVersion": "1.1.1"` (new field)
  - [ ] Verify `"version": "1.0.0"` is unchanged
  - [ ] Verify commands (`launch`, `resume`, `version`, `update`), `updateCheck`, and `detection` are unchanged
- [ ] 6.4 Copy `logo.png` from the removed `src/CLIHub.Core/SeedPlugins/openclaude/logo.png` (backup from git history if needed) to `D:\Projects\CLIHub-plugins\plugins\openclaude\logo.png`
- [ ] 6.5 Update `D:\Projects\CLIHub-plugins\reserved-ids.json` — remove `"openclaude"` from the array (four ids remain: `github-copilot`, `opencode`, `pi`, `qwen-code`)
- [ ] 6.6 Package the plugin:
  ```powershell
  cd D:\Projects\CLIHub-plugins
  pwsh scripts\package-plugin.ps1 openclaude
  ```
- [ ] 6.7 Validate the feed:
  ```powershell
  cd D:\Projects\CLIHub-plugins
  node scripts\build-index.mjs --check
  ```
- [ ] 6.8 Open a pull request to `trustsoft/CLIHub-plugins` with:
  - Title: "Add OpenClaude plugin (migrated from built-in)"
  - Description: Link to CLIHub 1.1.1 release; note that OpenClaude is no longer built-in as of that version
  - Changes: `plugins/openclaude/`, updated `reserved-ids.json`
- [ ] 6.9 Wait for PR review and CI validation
- [ ] 6.10 Merge PR after CLIHub 1.1.1 is released (not before)

**Gate:** Feed CI passes; `index.json` is updated with the OpenClaude entry; the GitHub Release for `openclaude-1.0.0` exists.

## Stage 7: Smoke Test (Manual)

**Prerequisites:** CLIHub 1.1.1 is built and installed; feed PR is merged.

- [ ] 7.1 **Fresh installation smoke:**
  - [ ] Install CLIHub 1.1.1 on a clean machine (or with `%APPDATA%\CLIHub\` deleted)
  - [ ] Verify the CLI Agents page shows four built-in agents (no OpenClaude)
  - [ ] Open Browse Plugins and verify OpenClaude appears in the feed
  - [ ] Install OpenClaude from the feed; verify it appears in CLI Agents with `origin=installed`
  - [ ] Launch OpenClaude; verify it executes correctly

- [ ] 7.2 **Existing installation smoke:**
  - [ ] Upgrade an existing CLIHub 1.1.0 installation (with OpenClaude already seeded) to 1.1.1
  - [ ] Verify the CLI Agents page still shows OpenClaude with `origin=builtin` badge
  - [ ] Open Browse Plugins; verify OpenClaude appears in the feed with version `1.0.0` and status "Current" (not "Update available")
  - [ ] Remove the OpenClaude plugin directory manually (delete `%APPDATA%\CLIHub\plugins\openclaude\`)
  - [ ] Reload the catalog; verify OpenClaude is absent from CLI Agents
  - [ ] Install OpenClaude from the feed; verify it reappears with `origin=installed`

- [ ] 7.3 **Reserved ID smoke:**
  - [ ] On CLIHub 1.1.1, attempt to install a manually crafted `.clihub-plugin.zip` with `id=openclaude`
  - [ ] Verify the installation succeeds (the id is no longer reserved)

**Gate:** All smoke tests pass; no regressions observed.

## Stage 8: Update Knowledge Graph

- [x] 8.1 Run `graphify update .` from the repository root to refresh the project knowledge graph with the new built-in set

**Gate:** `graphify-out/graph.json` reflects the current codebase.

## Final Checklist

Before reporting completion:

- [x] All tests pass (`dotnet test CLIHub.sln`)
- [x] All specs validate (`openspec validate --all` reports zero errors)
- [x] Documentation references are updated (README, glossary, architecture docs)
- [ ] Feed PR is merged and OpenClaude is available in the feed
- [ ] Smoke tests confirm expected behavior on fresh and existing installations
- [x] Knowledge graph is updated

## Notes

- **No git commits or archive** — This planning artifact does not include git operations. The implementation session will commit the changes.
- **Feed deployment is post-release** — Stage 6 cannot start until CLIHub 1.1.1 is released.
- **Smoke test is gated** — Stage 7 cannot start until Stage 6 is complete (feed PR merged).
- **Version bump is NOT included** — `Directory.Build.props` remains at `1.1.0`; the version bump to `1.1.1` is release preparation work, outside the scope of this change. The feed entry's `minAppVersion=1.1.1` is a release dependency documented in proposal.md and this file, but the version bump itself happens during release preparation, not as part of this change.
