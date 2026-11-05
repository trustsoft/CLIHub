# Move GitHub Copilot to Feed — Tasks

## Stage 0: Pre-Implementation Gate

- [x] 0.1 Run `openspec list` and confirm the current state of `move-openclaude-to-feed`. If it has
  already been archived, re-check whether the main spec's requirement is named
  `Built-in agent descriptors` (expected) before proceeding — the delta in this change assumes
  that exact name
- [x] 0.2 If `move-openclaude-to-feed` has been abandoned, renamed, or its spec delta reworked to a
  different requirement name or shape, stop and update this change's `specs/plugin-seeding/spec.md`
  delta before continuing (see design.md, Decision 6, for why the shape depends on that change)

**Gate:** `move-openclaude-to-feed` status is known and consistent with this change's assumptions,
or this change's delta has been updated to match reality.

## Stage 1: Remove Embedded GitHub Copilot Resources

- [x] 1.1 Delete `src/CLIHub.Core/SeedPlugins/github-copilot/plugin.json`
- [x] 1.2 Delete `src/CLIHub.Core/SeedPlugins/github-copilot/logo.png`
- [x] 1.3 Delete `src/CLIHub.Core/SeedPlugins/github-copilot/` directory (should be empty after 1.1
  and 1.2)

**Gate:** `dotnet build CLIHub.sln` succeeds. (The permanent assertion that `BuiltinPluginIds()` no
longer includes `"github-copilot"` is added as test `BuiltinPluginIds_NoLongerContainsGithubCopilot`
in task 2.4 — no throwaway test is created in this stage.)

## Stage 2: Update Tests

- [x] 2.1 Update `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs:11` — change `BuiltinIds`
  array from four to three elements (remove `"github-copilot"`)
- [x] 2.2 Update seed-count assertions — change `Assert.Equal(4, ...)` to `Assert.Equal(3, ...)` in:
  - [x] Line 69 (first-run seeding)
  - [x] Line 154 (missing plugins folder / non-built-in user plugin present)
  - [x] Line 163 (idempotent first call)
  - [x] Line 196 (unmodified built-in refreshed on app update)
  - [x] Line 293 (unknown-origin directory missing, re-seeded as built-in)
  - [x] Line 375 (user-modified logo preserved on app update)
  - [x] Line 406 (each seeded descriptor is loadable / `SeededDescriptors_LoadAsValidPlugins`)
  - [x] Verify this list against the actual file at implementation time — line numbers were
    confirmed during planning (see design.md, "Tests") but re-confirm before editing, since this
    file may shift if other changes land first
- [x] 2.3 Update "other built-ins" assertions — change `Assert.Equal(3, ...)` to `Assert.Equal(2, ...)` in:
  - [x] Line 110 (`EnsureBuiltinsExist_NoStateDiskMatchesEmbedded_AdoptsAsBuiltin`)
  - [x] Line 125 (`EnsureBuiltinsExist_NoStateDiskDiffersFromEmbedded_PreservesAsUnknown`)
  - [x] Line 215 (`EnsureBuiltinsExist_BuiltinUserModified_PreservesEditAndKeepsStoredHash`)
  - [x] Line 233 (`EnsureBuiltinsExist_InstalledPlugin_NeverTouched`)
  - [x] Line 250 (`EnsureBuiltinsExist_InstalledPluginWithFiles_FilesUntouched`)
  - [x] Line 264 (`EnsureBuiltinsExist_DisabledBuiltin_NotReSeeded`)
  - [x] Line 312 (`EnsureBuiltinsExist_UnknownOriginDiskMatchesEmbedded_AdoptsAsBuiltin`)
  - [x] Line 331 (`EnsureBuiltinsExist_UnknownOriginDiskDiffers_PreservesUserContent`)
- [x] 2.4 Remove GitHub-Copilot-specific command-set assertions:
  - [x] In test `SeededDescriptors_ExposeExpectedCommandSets` (line 413), remove the assertion
    checking `plugins["github-copilot"]` (line 430)
  - [x] In test `SeededDescriptors_DeclareExpectedUpdateSources` (line 436), remove the assertion
    checking `plugins["github-copilot"].UpdateCheck` (line 452)
- [x] 2.5 Verify the command-set tests still verify the three remaining agents (OpenCode, Pi, Qwen
  Code)
- [x] 2.6 Update `tests/CLIHub.Core.Tests/Plugins/EmbeddedPluginResourcesTests.cs:7` —
  `ExpectedBuiltinIds` array from four to three elements (remove `"github-copilot"`)
- [x] 2.7 Update `tests/CLIHub.Core.Tests/Plugins/EmbeddedPluginResourcesTests.cs:10` — rename test
  from `BuiltinPluginIds_EnumeratesTheFourBuiltInAgents` to
  `BuiltinPluginIds_EnumeratesTheThreeBuiltInAgents`
- [x] 2.8 Add test `BuiltinPluginIds_NoLongerContainsGithubCopilot` in
  `EmbeddedPluginResourcesTests`, parallel to the existing `BuiltinPluginIds_NoLongerContainsClineCli`
  (line 22) and `BuiltinPluginIds_NoLongerContainsOpenClaude` (line 32), asserting
  `"github-copilot"` is not in the returned ids
- [x] 2.9 Verify `tests/CLIHub.Tests/ViewModels/Agents/BrowsePluginsDialogViewModelTests.cs:101,116`
  still uses `"opencode"` as its reserved id fixture (no change needed — confirmed during planning)
- [x] 2.10 Verify `tests/CLIHub.Core.Tests/Plugins/PluginInstallerTests.cs:303,306` still uses
  `"opencode"` as its reserved id fixture (no change needed — confirmed during planning)
- [x] 2.11 Grep `copilot` (case-insensitive) in `tests/` and verify no other test asserts
  `github-copilot` is reserved or built-in (distinguish from generic process-matching/version-string
  fixtures in `AgentProcessMatcherTests.cs`, `AgentVersionServiceTests.cs`,
  `AgentUpdateAvailabilityServiceTests.cs`, `AgentReleaseClientTests.cs`, which use "copilot" as
  sample data unrelated to built-in membership and need no change)

**Gate:** `dotnet test CLIHub.sln` passes; all seeding tests pass.

## Stage 3: Update Documentation

- [x] 3.1 Update `README.md:18` — change to "Built-in descriptors cover OpenCode, Pi, and Qwen
  Code. Cline CLI, GitHub Copilot, and OpenClaude are available from the curated plugin feed;
  additional agents can be added as JSON plugins." (adjust wording if `move-openclaude-to-feed` has
  already changed this line by the time this task runs — re-read the file first)
- [x] 3.2 Update `docs/glossary.md:61` — keep GitHub Copilot in the general Agent list (it remains
  an agent via the feed)
- [x] 3.3 Update `docs/glossary.md:72` — change the Built-in agent definition to list three agents
  (remove GitHub Copilot): "OpenCode, Pi, Qwen Code."
- [x] 3.4 Update `docs/architecture/plugins.md:44` — change "Built-in descriptors for OpenCode, Pi,
  GitHub Copilot, and Qwen Code" to list three agents (remove GitHub Copilot)

**Gate:** Grep for "GitHub Copilot" in `README.md`, `docs/glossary.md`, and
`docs/architecture/plugins.md` finds no built-in-set references (the general Agent list entry in
`docs/glossary.md:61` and the curated-feed sentence in `README.md:18` are expected and acceptable).

## Stage 4: Update Specs (Delta)

**Note:** This change uses `MODIFIED "Built-in agent descriptors"`, reusing the exact scenario
names the sister change `move-openclaude-to-feed` introduces via its own `ADDED` block. This is
**only valid once `move-openclaude-to-feed` is archived** (see design.md, Decision 6, and Stage 0
above). The delta file already exists from planning; this stage re-verifies it, it does not author
it from scratch.

- [x] 4.1 Re-read `openspec/changes/move-github-copilot-to-feed/specs/plugin-seeding/spec.md` and
  confirm it still matches the main spec's eventual post-sister-archive shape: requirement name
  `Built-in agent descriptors`, scenarios `Built-in agents seeded`, `Each seeded descriptor is
  loadable`, `Complete command sets`, `Commands match the agent CLI surface`,
  `Unsupported command kinds are omitted` — same five names, THEN bodies for the first two naming
  only OpenCode, Pi, Qwen Code
- [x] 4.2 Verify no other specs reference "GitHub Copilot" as a built-in (grep `openspec/specs/`
  excluding the generic CLI-surface example `copilot --continue` in `agent-version` and
  `agent-update-detection`, which illustrate unrelated requirements and are not built-in-membership
  claims)

**Gate:** `openspec validate --all` passes with zero failed items (an INFO-level "Archive would
refuse this delta... not found" line is expected and correct until `move-openclaude-to-feed` is
archived — see design.md for why this is not an error). Also run
`openspec validate move-github-copilot-to-feed --type change --strict` and confirm it reports the
change as valid.

## Stage 5: Build and Verify

- [x] 5.1 Run `dotnet build CLIHub.sln --configuration Release`
- [x] 5.2 Run `dotnet test CLIHub.sln --configuration Release`
- [x] 5.3 Run `openspec validate --all` and verify zero failed items
- [x] 5.4 Inspect the Release build's embedded resources (ILSpy or `ildasm`) and confirm
  `SeedPlugins.github_copilot.plugin.json` and `SeedPlugins.github_copilot.logo.png` are absent
- [x] 5.5 Verify `EmbeddedPluginResources.BuiltinPluginIds()` returns three ids (OpenCode, Pi, Qwen
  Code) by running a minimal test harness or inspecting the release assembly
- [x] 5.6 Verify a manually crafted `.clihub-plugin.zip` with `id=github-copilot` passes the
  reserved-id check in `PluginInstaller` (or write a test that asserts this, mirroring
  `Inspect_FormerlyBuiltinClineCliId_NotRejectedAsReserved`)

**Gate:** All checks pass. The build produces a valid CLIHub assembly with three built-in agents.

**Note:** `Directory.Build.props` is NOT bumped in this stage or anywhere in this change. It stays
at `1.1.0`. The version bump to `1.1.1` is release-preparation work, shared with
`move-openclaude-to-feed`, and happens exactly once, outside both changes.

## Stage 6: Feed Deployment (Separate Repository — Post-Release, Gated on v1.1.1)

**Note:** These tasks operate on `D:\Projects\CLIHub-plugins`, not the CLIHub application
repository. They are performed **after** CLIHub 1.1.1 is released. **This stage, and Stage 7 below,
stay unticked until that release has happened — do not check these off during the implementation
session that completes Stages 1–5.**

**Prerequisites:**

- [x] 6.0 Verify CLIHub 1.1.1 is released (tag `v1.1.1` exists; `Directory.Build.props` has
  `<Version>1.1.1</Version>` on the release branch/tag)

**Coordination check (do this before touching any file):**

- [x] 6.1 Inspect `D:\Projects\CLIHub-plugins\plugins\` and `reserved-ids.json`. Recorded outcome:
  `move-openclaude-to-feed`'s feed entry had not started, so both went out in a single PR
  (`trustsoft/CLIHub-plugins#2`) adding `plugins/openclaude/` and `plugins/github-copilot/` and
  removing both ids from `reserved-ids.json` in one commit (the design's "not started" branch,
  implemented as a combined PR).

**Feed tasks:**

- [x] 6.2 Create `D:\Projects\CLIHub-plugins\plugins\github-copilot\` directory
- [x] 6.3 Copy `plugin.json` from the removed `src/CLIHub.Core/SeedPlugins/github-copilot/plugin.json`
  (backup from git history if needed) to
  `D:\Projects\CLIHub-plugins\plugins\github-copilot\plugin.json`
- [x] 6.4 Update the copied `plugin.json`:
  - [x] Add `"schemaVersion": 1` (matching the `cline-cli` feed entry's shape)
  - [x] Verify `"publisher": "CLIHub"` is unchanged
  - [x] Add `"minAppVersion": "1.1.1"` (new field)
  - [x] Verify `"version": "1.0.0"` is unchanged
  - [x] Verify commands (`launch`, `resume`, `version`, `update`), `updateCheck`
    (`githubReleases` / `github/copilot-cli`), and `detection` are unchanged
- [x] 6.5 Copy `logo.png` from the removed `src/CLIHub.Core/SeedPlugins/github-copilot/logo.png`
  (backup from git history if needed) to
  `D:\Projects\CLIHub-plugins\plugins\github-copilot\logo.png`
- [x] 6.6 Update `D:\Projects\CLIHub-plugins\reserved-ids.json` — remove `"github-copilot"` from the
  array (per the coordination check in 6.1, `"openclaude"` may or may not still be present
  depending on sister-change timing)
- [x] 6.7 Package the plugin:
  ```powershell
  cd D:\Projects\CLIHub-plugins
  pwsh scripts\package-plugin.ps1 github-copilot
  ```
- [x] 6.8 Validate the feed:
  ```powershell
  cd D:\Projects\CLIHub-plugins
  node scripts\build-index.mjs --check
  ```
- [x] 6.9 Open a pull request to `trustsoft/CLIHub-plugins` with:
  - Title: "Add GitHub Copilot plugin (migrated from built-in)"
  - Description: Link to the CLIHub 1.1.1 release; note that GitHub Copilot is no longer built-in
    as of that version; cross-reference the OpenClaude feed PR if separate
  - Changes: `plugins/github-copilot/`, updated `reserved-ids.json`
- [x] 6.10 Wait for PR review and CI validation
- [x] 6.11 Merge PR after CLIHub 1.1.1 is released (not before)

**Gate:** Feed CI passes; `index.json` is updated with the GitHub Copilot entry; the GitHub Release
for `github-copilot-1.0.0` exists.

## Stage 7: Smoke Test (Manual, Post-Release)

**Prerequisites:** CLIHub 1.1.1 is built and installed; feed PR (Stage 6) is merged. **Stays
unticked until that is true.**

- [x] 7.1 **Fresh installation smoke:**
  - [x] Install CLIHub 1.1.1 on a clean machine (or with `%APPDATA%\CLIHub\` deleted)
  - [x] Verify the CLI Agents page shows five built-in agents (OpenCode, Pi, OMP, Qwen Code, Devin —
    no GitHub Copilot)
  - [x] Open Browse Plugins and verify GitHub Copilot appears in the feed
  - [x] Install GitHub Copilot from the feed; verify it appears in CLI Agents with
    `origin=installed`
  - [x] Launch GitHub Copilot; verify it executes correctly
- [x] 7.2 **Existing installation smoke:**
  - [x] Upgrade an existing CLIHub 1.1.0 installation (with GitHub Copilot already seeded) to
    1.1.1
  - [x] Verify the CLI Agents page still shows GitHub Copilot with `origin=builtin` badge
  - [x] Open Browse Plugins; verify GitHub Copilot appears in the feed with version `1.0.0` and
    status "Current" (not "Update available")
  - [x] Remove the GitHub Copilot plugin directory manually (delete
    `%APPDATA%\CLIHub\plugins\github-copilot\`)
  - [x] Reload the catalog; verify GitHub Copilot is absent from CLI Agents
  - [x] Install GitHub Copilot from the feed; verify it reappears with `origin=installed`
- [x] 7.3 **Reserved ID smoke:**
  - [x] On CLIHub 1.1.1, attempt to install a manually crafted `.clihub-plugin.zip` with
    `id=github-copilot`
  - [x] Verify the installation succeeds (the id is no longer reserved)

**Gate:** All smoke tests pass; no regressions observed.

## Stage 8: Update Knowledge Graph

- [x] 8.1 Run `graphify update .` from the repository root to refresh the project knowledge graph
  with the new built-in set

**Gate:** `graphify-out/graph.json` reflects the current codebase.

## Final Checklist

Before reporting completion:

- [x] All tests pass (`dotnet test CLIHub.sln`)
- [x] All specs validate (`openspec validate --all` reports zero failed items)
- [x] Documentation references are updated (README, glossary, architecture docs)
- [x] `move-openclaude-to-feed` is archived before this change is archived (Stage 0 gate)
- [x] Feed PR is merged and GitHub Copilot is available in the feed
- [x] Smoke tests confirm expected behavior on fresh and existing installations
- [x] Knowledge graph is updated

## Notes

- **No git commits or archive** — This planning artifact does not include git operations. The
  implementation session will commit the changes, and archiving requires separate explicit user
  approval per `AGENTS.md`.
- **Archive ordering is load-bearing** — This change's spec delta only applies cleanly if
  `move-openclaude-to-feed` is archived first. Stage 0 exists specifically to check this before any
  other work starts.
- **Feed deployment is post-release** — Stage 6 cannot start until CLIHub 1.1.1 is released.
- **Smoke test is gated** — Stage 7 cannot start until Stage 6 is complete (feed PR merged).
- **Version bump is NOT included** — `Directory.Build.props` remains at `1.1.0` throughout this
  change. The version bump to `1.1.1` is release-preparation work shared with
  `move-openclaude-to-feed`, performed once, outside both changes.
- **OMP and Devin are not part of this change** — Adding them as built-ins to reach the
  user-confirmed target set (OpenCode, Pi, OMP, Qwen Code, Devin) is a separate, later change.
