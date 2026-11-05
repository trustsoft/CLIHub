# Add OMP and Devin as Built-in Agents — Tasks

## Stage 0: Pre-Implementation Gate

- [x] 0.1 Run `openspec list` and confirm the archive status of `move-openclaude-to-feed` and
  `move-github-copilot-to-feed`. This change's spec delta (`MODIFIED "Built-in agent
  descriptors"`, 3 agents → 5 agents) assumes **both** are archived, in that order
  (`move-openclaude-to-feed` first), before this change's own archive step.
- [x] 0.2 If either sister change has been abandoned, renamed, or had its spec delta reworked to a
  different requirement name or shape, stop and update this change's
  `specs/plugin-seeding/spec.md` delta before continuing (see design.md, Decision 5)
- [x] 0.3 Re-read `src/CLIHub.Core/SeedPlugins/` and confirm the current built-in set. If
  `move-github-copilot-to-feed` has already landed its source edits, the directory holds exactly
  three subfolders (`opencode/`, `pi/`, `qwen-code/`) before this change adds `omp/` and `devin/`.
  If it has not landed yet, the directory may still hold `github-copilot/` too — re-verify line
  numbers in every file this change touches against whatever state is actually on disk, rather
  than trusting design.md's line numbers, which describe an earlier intermediate state.

**Gate:** Both sister changes' status is known and consistent with this change's assumptions, or
this change's delta has been updated to match reality.

## Stage 1: Add Embedded OMP and Devin Resources

- [x] 1.1 Create `src/CLIHub.Core/SeedPlugins/omp/plugin.json` with the content from design.md
  Decision 1 (id `omp`, name `OMP`, full launch/resume/version/update command set, `updateCheck`
  source `npm` package `@oh-my-pi/pi-coding-agent`, detection `%USERPROFILE%\.omp` +
  `%LOCALAPPDATA%\omp` + project indicator `.omp`)
- [x] 1.2 Create `src/CLIHub.Core/SeedPlugins/devin/plugin.json` with the content from design.md
  Decision 2 (id `devin`, name `Devin`, full launch/resume/version/update command set,
  `updateCheck` source `githubReleases` repo `CognitionAI/devin-cli`, detection `%APPDATA%\devin` +
  `%LOCALAPPDATA%\devin`, project indicator `.devin`)
- [x] 1.3 Validate both new `plugin.json` files parse as valid JSON and match the schema every
  other built-in descriptor follows (compare field-by-field against `src/CLIHub.Core/SeedPlugins/
  opencode/plugin.json` and `.../github-copilot/plugin.json`)

**Gate:** `dotnet build CLIHub.sln` succeeds with the two new JSON files present (no logo yet is
fine — seeding tolerates a missing logo per `docs/architecture/plugins.md:143`, "Missing logo falls
back" — but do not leave this stage without a working descriptor; the next stage adds the logos
before any test run).

## Stage 2: Produce OMP and Devin Logos

- [x] 2.1 Measure the four existing built-in logos' pixel dimensions (`src/CLIHub.Core/SeedPlugins/
  {opencode,pi,qwen-code,github-copilot}/logo.png`) to settle the open question in design.md
  (160×160 vs 512×512) and pick the matching dimension for the two new logos
- [x] 2.2 Fetch the OMP vendor SVG from
  `https://raw.githubusercontent.com/can1357/oh-my-pi/main/assets/icon.svg` and confirm it matches
  the brief's description (795 B, 120×90, white `#fafafa` glyph + orange `#f97316`, MIT repo). If
  it does not match, stop and record the discrepancy before proceeding (deviation protocol)
- [x] 2.3 Fetch the Devin vendor SVG from `https://devin.ai/favicon.svg` and confirm it matches the
  brief's description (425×425, single black path, proprietary mark). If it does not match, stop
  and record the discrepancy before proceeding
- [x] 2.4 Using a one-time local rasterization step (e.g. `bunx` or `npx` invoking `@resvg/resvg-js`
  or `sharp`; do not add either as a project dependency — see design.md Decision 4), composite each
  SVG onto a neutral plate chosen for contrast against both the Dark and Light themes, matching the
  visual weight of the four existing logos as closely as practical
- [x] 2.5 Rasterize both composites to PNG at the dimension chosen in 2.1
- [x] 2.6 Write the two PNGs to `src/CLIHub.Core/SeedPlugins/omp/logo.png` and
  `src/CLIHub.Core/SeedPlugins/devin/logo.png`. Do not commit any SVG source file or add a new
  `assets/logos/agents/` directory
- [x] 2.7 Visually compare both new logos against the four existing ones, side by side, in both the
  Dark and Light theme (launch CLIHub or inspect the PNGs directly) and confirm legibility and
  rough visual consistency (size, padding, plate treatment) before proceeding

**Gate:** Both logo files exist, are valid non-empty PNGs, and pass a human visual legibility check
on both themes (this gate has no automated test — see design.md Risk 4).

## Stage 3: Update Tests

- [x] 3.1 Re-read `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs` in full and confirm the
  current `BuiltinIds` array and every seed-count / "other built-ins" assertion value (do not trust
  design.md's line numbers, which describe an earlier intermediate state — see Stage 0.3)
- [x] 3.2 Update `BuiltinIds` to the five-element array: `["opencode", "pi", "omp", "qwen-code",
  "devin"]`
- [x] 3.3 Update every seed-count assertion (first-run seeding, missing plugins folder, idempotent
  first call, unmodified built-in refreshed on app update, unknown-origin directory missing,
  user-modified logo preserved, each seeded descriptor is loadable) from whatever the sister chain
  left them at (3) to 5
- [x] 3.4 Update every "other built-ins" assertion (one built-in already present/adopted, so only
  the others are written) from whatever the sister chain left them at (2) to 4
- [x] 3.5 Add OMP and Devin command-set assertions to `SeededDescriptors_ExposeExpectedCommandSets`
  — both use the same `full` four-kind array already used for `opencode`, `pi`, `qwen-code`
- [x] 3.6 Add OMP and Devin update-source assertions to
  `SeededDescriptors_DeclareExpectedUpdateSources`:
  - [x] `Assert.Equal(new PluginUpdateCheck("npm", Package: "@oh-my-pi/pi-coding-agent"),
    plugins["omp"].UpdateCheck)`
  - [x] `Assert.Equal(new PluginUpdateCheck("githubReleases", Repo: "CognitionAI/devin-cli"),
    plugins["devin"].UpdateCheck)`
- [x] 3.7 Rename `EnsureBuiltinsExist_EmptyFolder_WritesFourDescriptors` to the count-neutral
  `EnsureBuiltinsExist_EmptyFolder_WritesAllBuiltinDescriptors` (see design.md Decision 6 — this
  name is untouched by either sister change, so this change renames it directly)
- [x] 3.8 Re-read `tests/CLIHub.Core.Tests/Plugins/EmbeddedPluginResourcesTests.cs` in full and
  update `ExpectedBuiltinIds` to the five-element array
- [x] 3.9 Rename the enumeration test (whatever count-specific name the sister chain left it as,
  expected `BuiltinPluginIds_EnumeratesTheThreeBuiltInAgents` per `move-github-copilot-to-feed`'s
  own plan) to the count-neutral `BuiltinPluginIds_EnumeratesTheBuiltInAgents`
- [x] 3.10 Verify `BuiltinPluginIds_NoLongerContainsClineCli`, `BuiltinPluginIds_
  NoLongerContainsOpenClaude`, and (if sister change 2 has landed) `BuiltinPluginIds_
  NoLongerContainsGithubCopilot` remain unaffected — no change needed
- [x] 3.11 Do **not** add a `BuiltinPluginIds_NoLongerContains...` test for `omp` or `devin` (that
  shape pins a removal; neither agent is being removed)
- [x] 3.12 Add two theory rows to `tests/CLIHub.Core.Tests/Agents/AgentVersionServiceTests.cs`'s
  `GetVersion_ExtractsVersionNumber` theory: `["omp/18.12.6", "18.12.6"]` and `["devin 3000.11.92
  (7f4e64980e22)", "3000.11.92"]`
- [x] 3.13 Add two theory rows to `tests/CLIHub.Core.Tests/Agents/AgentUpdateVersionTests.cs`'s
  `TryExtract_VersionTextWithSurroundingContent_ExtractsFirstNumericTriple` theory: `["omp/18.12.6",
  18, 12, 6]` and `["devin 3000.11.92 (7f4e64980e22)", 3000, 11, 92]`
- [x] 3.14 Verify `tests/CLIHub.Core.Tests/Plugins/PluginInstallerTests.cs` and
  `tests/CLIHub.Tests/ViewModels/Agents/BrowsePluginsDialogViewModelTests.cs` still use `"opencode"`
  as their reserved-id fixture (no change expected — confirmed during planning)
- [x] 3.15 Grep `omp` and `devin` (case-insensitive, scoped sensibly to avoid matching unrelated
  words) across `tests/` and verify no other test needs updating

**Gate:** `dotnet test CLIHub.sln` passes; all seeding, version-parsing, and update-version tests
pass with the new theory rows and the five-agent counts.

## Stage 4: Update Documentation

- [x] 4.1 Re-read `README.md:18` (wording depends on what the sister chain has already changed it
  to) and update it to: "Built-in descriptors cover OpenCode, Pi, OMP, Qwen Code, and Devin. Cline
  CLI, GitHub Copilot, and OpenClaude are available from the curated plugin feed; additional agents
  can be added as JSON plugins." (adjust the feed-agent list if the sister chain's own wording
  differs from this assumption by the time this task runs)
- [x] 4.2 Update `docs/glossary.md:61` (general Agent list) — add OMP and Devin: "Agent — an AI
  coding agent CLI (OpenCode, Pi, Cline CLI, GitHub Copilot, OMP, OpenClaude, Qwen Code, Devin, or
  any user-added plugin)." This list is cumulative across every agent CLIHub has ever shipped a
  descriptor for (built-in or feed), not scoped to "currently built-in" — see design.md, Verified
  Current State, for why GitHub Copilot and OpenClaude stay in this list even after their built-in
  removal
- [x] 4.3 Update `docs/glossary.md:72` (Built-in agent definition) — rewrite to the final five-agent
  form: "OpenCode, Pi, OMP, Qwen Code, Devin."
- [x] 4.4 Update `docs/architecture/plugins.md:44` — rewrite "Built-in descriptors for OpenCode,
  Pi, [...], and Qwen Code" to "Built-in descriptors for OpenCode, Pi, OMP, Qwen Code, and Devin"
- [x] 4.5 Confirm `docs/project-context.md` still does not enumerate the built-in set (re-verify;
  no edit expected per design.md)
- [x] 4.6 Leave `docs/images/src/workflow.html:151` untouched (example list, not a built-in claim —
  per design.md and the brief)

**Gate:** Grep for "OMP" and "Devin" in `README.md`, `docs/glossary.md`, and
`docs/architecture/plugins.md` finds the new built-in-set references; grep for the old agent names
confirms no stale built-in-count wording remains (the general Agent list entries are expected and
acceptable).

## Stage 5: Update Specs (Delta)

**Note:** This change uses `MODIFIED "Built-in agent descriptors"`, reusing the exact scenario
names introduced by `move-openclaude-to-feed`'s `ADDED` block and carried through by
`move-github-copilot-to-feed`'s own `MODIFIED` block. This is **only valid once both sister changes
are archived, in that order** (see design.md, Decision 5, and Stage 0 above). The delta file
already exists from planning; this stage re-verifies it, it does not author it from scratch.

- [x] 5.1 Re-read `openspec/changes/add-omp-devin-builtins/specs/plugin-seeding/spec.md` and
  confirm it still matches the main spec's eventual post-both-sister-archives shape: requirement
  name `Built-in agent descriptors`, scenarios `Built-in agents seeded`, `Each seeded descriptor is
  loadable`, `Complete command sets`, `Commands match the agent CLI surface`, `Unsupported command
  kinds are omitted` — same five names, THEN bodies for the first two naming OpenCode, Pi, OMP,
  Qwen Code, Devin
- [x] 5.2 Verify no other specs reference "three agents" or a stale built-in count (grep
  `openspec/specs/`)

**Gate:** `openspec validate --all` passes with zero failed items (an INFO-level "Archive would
refuse this delta... not found" line is expected and correct until both sister changes are
archived — see design.md for why this is advisory, not an error). Also run `openspec validate
add-omp-devin-builtins --type change --strict` and confirm it reports the change as valid.

## Stage 6: Build and Verify

- [x] 6.1 Run `dotnet build CLIHub.sln --configuration Release`
- [x] 6.2 Run `dotnet test CLIHub.sln --configuration Release`
- [x] 6.3 Run `openspec validate --all` and verify zero failed items
- [x] 6.4 Inspect the Release build's embedded resources (ILSpy or `ildasm`) and confirm
  `SeedPlugins.omp.plugin.json`, `SeedPlugins.omp.logo.png`, `SeedPlugins.devin.plugin.json`, and
  `SeedPlugins.devin.logo.png` are all present
- [x] 6.5 Verify `EmbeddedPluginResources.BuiltinPluginIds()` returns five ids (OpenCode, Pi, OMP,
  Qwen Code, Devin) by running a minimal test harness or inspecting the release assembly
- [x] 6.6 Verify a manually crafted `.clihub-plugin.zip` with `id=omp` and another with `id=devin`
  both fail the reserved-id check in `PluginInstaller` (or write a test that asserts this)
- [x] 6.7 Launch CLIHub against a fresh `%APPDATA%\CLIHub\plugins\` folder (or a throwaway test
  profile) and confirm both OMP and Devin cards appear with their logos rendered correctly in both
  themes

**Gate:** All checks pass. The build produces a valid CLIHub assembly with five built-in agents.

**Note:** `Directory.Build.props` is NOT bumped in this stage or anywhere in this change. It stays
at `1.1.0`. The version bump to `1.1.1` is release-preparation work, shared with both sister
changes, and happens exactly once, outside all three.

## Stage 7: Update Knowledge Graph

- [x] 7.1 Run `graphify update .` from the repository root to refresh the project knowledge graph
  with the new built-in set

**Gate:** `graphify-out/graph.json` reflects the current codebase.

## Final Checklist

Before reporting completion:

- [x] All tests pass (`dotnet test CLIHub.sln`)
- [x] All specs validate (`openspec validate --all` reports zero failed items)
- [x] Documentation references are updated (README, glossary, architecture docs)
- [x] `move-openclaude-to-feed` and `move-github-copilot-to-feed` are both archived, in that order,
  before this change is archived (Stage 0 gate)
- [x] Both new logos pass a human visual-contrast check on Dark and Light themes
- [x] Knowledge graph is updated

## Notes

- **No git commits or archive** — This planning artifact does not include git operations. The
  implementation session will commit the changes, and archiving requires separate explicit user
  approval per `AGENTS.md`.
- **Archive ordering is three-deep** — This change's spec delta only applies cleanly if
  `move-openclaude-to-feed` is archived first, then `move-github-copilot-to-feed`. Stage 0 exists
  specifically to check this before any other work starts, and should be re-checked immediately
  before this change's own archive step too, since time may pass between implementation and
  archive approval.
- **No feed-repository work** — Unlike both sister changes, this change never touches
  `D:\Projects\CLIHub-plugins`. There is no Stage 6-equivalent "feed deployment" or Stage
  7-equivalent "smoke test" gated on a release; OMP and Devin are built-in from the start and need
  no post-release feed PR.
- **Version bump is NOT included** — `Directory.Build.props` remains at `1.1.0` throughout this
  change. The version bump to `1.1.1` is release-preparation work shared with both sister changes,
  performed once, outside all three.
- **No `CHANGELOG.md` / `RELEASE-NOTES.md` edits** — Release-note authorship is release-preparation
  work, out of scope here, same as both sister changes.
