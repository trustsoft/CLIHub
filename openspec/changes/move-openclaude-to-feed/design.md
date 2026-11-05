# Move OpenClaude to Feed — Design

## Verified Current State

### Embedded OpenClaude Descriptor

**File:** `src/CLIHub.Core/SeedPlugins/openclaude/plugin.json`

```json
{
  "id": "openclaude",
  "name": "OpenClaude",
  "description": "OpenClaude CLI",
  "version": "1.0.0",
  "publisher": "CLIHub",
  "homepage": "https://www.npmjs.com/package/@twigpine/openclaude",
  "commands": {
    "launch": { "executable": "openclaude" },
    "resume": { "executable": "openclaude", "arguments": "--continue" },
    "version": { "executable": "openclaude", "arguments": "--version" },
    "update": { "executable": "openclaude", "arguments": "update" }
  },
  "updateCheck": {
    "source": "npm",
    "package": "@twigpine/openclaude"
  },
  "detection": {
    "systemPaths": [
      "%USERPROFILE%\\.openclaude"
    ],
    "projectIndicators": [
      ".claude",
      "CLAUDE.md"
    ]
  }
}
```

**Logo:** `src/CLIHub.Core/SeedPlugins/openclaude/logo.png` exists.

### Seeding and Reserved IDs

**File:** `src/CLIHub.Core/Plugins/PluginSeeder.cs:42-218`

- Line 48-198: `EnsureBuiltinsExist()` enumerates descriptors via `EmbeddedPluginResources.EnumerateDescriptors()`, applies three-hash refresh logic, and writes descriptors for each embedded plugin.

**File:** `src/CLIHub.Core/Plugins/EmbeddedPluginResources.cs:10-127`

- Lines 33-54: `EnumerateDescriptors(Assembly)` reads all manifest resources matching `.SeedPlugins.` and ending with `.plugin.json`
- Lines 97-102: `BuiltinPluginIds(Assembly)` returns the distinct set of plugin ids from embedded descriptors
- The method is used by `PluginInstaller` to determine reserved ids

**File:** `src/CLIHub.Core/Plugins/PluginInstaller.cs:342-346`

```csharp
if (EmbeddedPluginResources.BuiltinPluginIds(typeof(PluginSeeder).Assembly)
    .Contains(plugin.Id, StringComparer.Ordinal))
{
    errors.Add($"Plugin id '{plugin.Id}' is reserved for the built-in agent '{plugin.Id}' and cannot be installed");
}
```

Reserved IDs are derived from the embedded resources at runtime. Removing the `openclaude` embedded descriptor automatically unreserves the id.

### Tests

**File:** `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs:11`

```csharp
private static readonly string[] BuiltinIds = ["opencode", "pi", "github-copilot", "openclaude", "qwen-code"];
```

**Assertions expecting five built-in agents (verified against the current file):**

- Line 69: `Assert.Equal(5, written);` — first-run seeding
- Line 154: `Assert.Equal(5, written);` — missing plugins folder
- Line 163: `Assert.Equal(5, seeder.EnsureBuiltinsExist());` — existing plugins present
- Line 196: `Assert.Equal(5, written);` — deleted built-in re-seeded
- Line 293: `Assert.Equal(5, written);` — unmodified built-in refreshed on app update
- Line 375: `Assert.Equal(5, written);` — disabled built-in not re-seeded
- Line 406: `Assert.Equal(5, plugins.Count);` — each seeded descriptor is loadable

**Command-set assertions:**

- Test `SeededDescriptors_ExposeExpectedCommandSets` (line 413): Verifies `openclaude` has `launch`, `resume`, `version`, and `update` (line 430)
- Test `SeededDescriptors_DeclareExpectedUpdateSources` (line 436): Verifies `openclaude` update check is npm source with package `"@twigpine/openclaude"` (line 452)

**File:** `tests/CLIHub.Core.Tests/Plugins/EmbeddedPluginResourcesTests.cs:7,10`

```csharp
private static readonly string[] ExpectedBuiltinIds = ["opencode", "pi", "github-copilot", "openclaude", "qwen-code"];

[Fact]
public void BuiltinPluginIds_EnumeratesTheFiveBuiltInAgents()
```

**File:** `tests/CLIHub.Tests/ViewModels/Agents/BrowsePluginsDialogViewModelTests.cs:93-117`

Test `LoadAsync_ReservedBuiltInId_FiltersOut`:

- Line 101: Creates feed entry with `id="opencode"` (a reserved built-in id)
- Line 116: Asserts that `opencode` is filtered out of `vm.Plugins`

After `openclaude` is removed from the embedded set, this test continues using `"opencode"` as its reserved id fixture and requires no change.

**File:** `tests/CLIHub.Core.Tests/Plugins/PluginInstallerTests.cs:301,315`

- Test `Inspect_BuiltinId_RejectedAgainstEmbeddedResources` (line 301): Uses `"opencode"` as a reserved id fixture; no change needed.
- Test `Inspect_FormerlyBuiltinClineCliId_NotRejectedAsReserved` (line 315): Verifies `cline-cli` is not reserved after the Cline removal. This pattern can be replicated for `openclaude` if desired, or the existing `cline-cli` test continues to serve as the precedent for formerly-built-in ids.

### Specs

**File:** `openspec/specs/plugin-seeding/spec.md:299-326`

#### Requirement: Built-in descriptor set

- **Scenario: Five agents seeded** (line 303-306) — "WHEN seeding runs THEN descriptors for OpenCode, Pi, GitHub Copilot, OpenClaude, and Qwen Code are available"
- **Scenario: Complete command sets** (line 313-316) — "OpenCode, Pi, GitHub Copilot, OpenClaude, and Qwen Code each define `launch`, `resume`, `version`, and `update`"
- **Scenario: Commands match the agent CLI surface** (line 318-321) — Example `copilot --continue` resumes the most recent session
- **Scenario: Unsupported command kinds are omitted** (line 323-326) — Example of an agent with no resume-most-recent command

**File:** `openspec/specs/plugin-installation/spec.md:62-68`

#### Scenario: Built-in id rejected for installed plugin

- Line 62-68: States that a package with a built-in id is rejected, and notes that built-in IDs are enumerated from embedded resources (not a hardcoded list).
- The scenario does **not** explicitly list the five ids. No spec delta is required for this scenario.

### Documentation

**File:** `README.md:18-19`

"Built-in descriptors cover OpenCode, Pi, GitHub Copilot, OpenClaude, and Qwen Code. Cline CLI is available from the curated plugin feed; additional agents can be added as JSON plugins."

**File:** `docs/glossary.md:61-62,71-72`

- Line 61-62: "Agent — an AI coding agent CLI (OpenCode, Pi, Cline CLI, GitHub Copilot, OpenClaude, Qwen Code, or any user-added plugin)"
- Line 71-72: "Built-in agent — one of the agents whose descriptors (and logos) are embedded in the application binary: OpenCode, Pi, GitHub Copilot, OpenClaude, Qwen Code."

**File:** `docs/architecture/plugins.md:44`

"Built-in descriptors for OpenCode, Pi, GitHub Copilot, OpenClaude, and Qwen Code are embedded and seeded using the three-hash refresh logic described below."

**File:** `docs/project-context.md`

The built-in set is not explicitly listed in the runtime facts section of `project-context.md`.

### Feed Repository

**File:** `D:\Projects\CLIHub-plugins\reserved-ids.json:1-7`

```json
[
  "github-copilot",
  "openclaude",
  "opencode",
  "pi",
  "qwen-code"
]
```

**File:** `D:\Projects\CLIHub-plugins\plugins\`

Contains `cline-cli/` (from the previous migration) and `README.md`. No `openclaude/` directory exists yet.

**File:** `D:\Projects\CLIHub-plugins\index.json:1-21`

Contains only one plugin: `cline-cli` (version `1.0.0`, minAppVersion `1.1.0`, publisher `CLIHub`).

**File:** `D:\Projects\CLIHub-plugins\README.md:37-59`

Documents the feed workflow: copy `templates/plugin.json`, fill in metadata and commands, add `logo.png`, package with `scripts/package-plugin.ps1`, validate with `node scripts/build-index.mjs --check`, open a PR. CI validates, packages, publishes a GitHub Release, and updates `index.json`.

### Release State

**File:** `Directory.Build.props:15`

```xml
<Version>1.1.0</Version>
```

CLIHub 1.1.0 is the current released version. The next version will be 1.1.1, which will ship this change.

**File:** `CHANGELOG.md:11-26`

Section `## 1.1.0 - 2026-11-05` documents the Cline removal: "Cline CLI moved from the embedded built-in set to the curated feed."

## Decisions

### 1. Migration Semantics

**Decision:** Existing users keep their `origin=builtin` OpenClaude plugin untouched. New installations obtain OpenClaude from the feed as `origin=installed`. No automatic migration or removal.

**Rationale:** Deleting or migrating existing OpenClaude plugins would violate the "Never overwrite user content" requirement (`openspec/specs/plugin-seeding/spec.md:136-150`). The three-hash seeding logic is designed to preserve user modifications; removing the embedded resource means seeding no longer manages OpenClaude, so existing installations remain as-is.

**Alternatives rejected:**

- **Auto-migrate existing `origin=builtin` to `origin=installed`** — Would require a one-time state-file rewrite that fabricates an installed origin for content the user did not explicitly install. This violates the principle that `origin=installed` means "user installed from a package."
- **Auto-remove existing OpenClaude directories** — Would delete user content without consent, violating the "Never overwrite user content" requirement.

### 2. Feed Plugin Version

**Decision:** Publish the feed entry at `version=1.0.0`, matching the embedded version.

**Rationale:** Existing users with `origin=builtin` OpenClaude (version `1.0.0` from the embedded descriptor) will see the feed entry as version `1.0.0`. The UI's version comparison will show "Current" instead of "Update available," which is the correct signal: their already-seeded OpenClaude is the same as the feed's.

If we published the feed entry at `1.0.1`, existing users would see an "Update available" prompt. Clicking Update would fail (cannot replace a built-in) or succeed (if the install flow allows replacing a built-in with an installed plugin, which is not explicitly covered in the current spec). The `1.0.0` version avoids this ambiguity.

**Alternatives rejected:**

- **Publish at `1.0.1` and auto-migrate** — Would require the migration logic rejected in Decision 1.
- **Publish at `2.0.0` or higher** — Would signal a breaking change when there is none. The descriptor content is identical.

### 3. Reserved ID Removal Timing

**Decision:** The feed's `reserved-ids.json` is updated **after** CLIHub 1.1.1 is released, in the same PR that publishes the OpenClaude feed entry.

**Rationale:** Browse Plugins shows all feed entries regardless of `minAppVersion`, disabling the Install button when `minAppVersion` exceeds the running version. If `reserved-ids.json` is updated before the CLIHub release, users on 1.1.0 will see OpenClaude in Browse Plugins. The Install button will be disabled (due to `minAppVersion=1.1.1`), but local/manual package installs via `.clihub-plugin.zip` will fail with "Plugin id 'openclaude' is reserved" because the embedded resource still exists in 1.1.0.

By updating `reserved-ids.json` after the release, we avoid this reserved-id rejection for manual installs on 1.1.0 (though the feed entry will still appear in Browse Plugins, grayed out).

**Alternatives rejected:**

- **Update `reserved-ids.json` before the release** — Would cause the reserved-id rejection on manual installs for 1.1.0 users.
- **Gate feed visibility on `minAppVersion` alone** — Browse Plugins does not filter by `minAppVersion`; it shows all entries and disables Install when the version requirement is not met.

### 4. Built-in Set After Removal

**Decision:** Four agents remain: OpenCode, Pi, GitHub Copilot, Qwen Code. Update all references in code, tests, docs, and specs.

**Rationale:** Removing OpenClaude from the embedded set is the core of the change. All derived assertions and documentation must reflect the new built-in count.

**No alternatives.**

### 5. Feed Package Metadata

**Decision:** The feed descriptor includes:

- `"id": "openclaude"`
- `"version": "1.0.0"`
- `"publisher": "CLIHub"` (curator attribution)
- `"minAppVersion": "1.1.1"`
- Commands: `launch`, `resume`, `version`, `update` (retain the existing set)
- `updateCheck`, `detection` unchanged

**Rationale:** The feed descriptor must be valid according to the feed's validation rules and CLIHub's `PluginDescriptorValidator`. The `minAppVersion` field ensures the Install button is disabled on CLIHub versions below 1.1.1 (Browse Plugins shows all entries but disables Install when `minAppVersion` exceeds the running version). The `publisher` field uses "CLIHub" to signal curator responsibility — the CLIHub team curates the feed and chose to include OpenClaude.

**Alternatives rejected:**

- **Publisher "Community"** — Too vague; does not attribute curation responsibility.
- **Publisher "Twigpine"** — The built-in used "CLIHub"; keeping it maintains consistency and signals curator responsibility.

### 6. Spec Delta Strategy

**Decision:**

- **REMOVED** requirement "Built-in descriptor set" (current five-agent scenarios, reproduced verbatim: "Five agents seeded", "Each seeded descriptor is loadable", "Complete command sets", "Commands match the agent CLI surface", "Unsupported command kinds are omitted")
- **ADDED** requirement "Built-in agent descriptors" — a new, count-neutral name — with scenarios "Built-in agents seeded" (OpenCode, Pi, GitHub Copilot, Qwen Code), "Each seeded descriptor is loadable", "Complete command sets", "Commands match the agent CLI surface", "Unsupported command kinds are omitted"
- **NO CHANGE** to `openspec/specs/plugin-installation/spec.md` — The reserved-id scenario does not list specific ids

**Rationale:** A MODIFIED requirement is invalid for a count-changing rename. Verified against the installed OpenSpec CLI (`C:\Users\Maxim\AppData\Roaming\npm\node_modules\@fission-ai\openspec`):

- `dist/core/validation/validator.js:607-684` (`findScenarioLossIssues`) builds a map of the current spec's requirement blocks, then for every MODIFIED block in the delta it calls `diffScenarioNames(current, incoming)` and raises an **ERROR** for any scenario name present in the current block but absent from the incoming block: `MODIFIED "<name>" omits scenario(s) the current spec still has: ... archive refuses to drop them`. The first draft's MODIFIED block did **not** trigger this error, because it listed both "Five agents seeded" and "Four agents seeded" and therefore omitted no current name; `openspec validate --all` passed. The defect was still real: archive would have replaced the requirement with a block carrying two contradictory scenarios (one claiming five agents, one claiming four), since a MODIFIED block replaces the whole requirement wholesale rather than merging scenario-by-scenario. Because the guard is strictly name-based, REMOVED + ADDED is the only shape that both validates and archives a truthful requirement.
- `dist/core/parsers/requirement-blocks.js:453-480` (`diffScenarioNames`) confirms there is no rename primitive: it is a pure multiplicity-aware **name-multiset diff** between the current block's scenario names and the incoming block's scenario names. A scenario is either present by name in both (kept) or it is not (reported as missing/added). There is no concept of "this scenario was renamed" — renaming a scenario inside a MODIFIED block is indistinguishable from deleting the old-named scenario and adding a new one, and archive's safety check (line 680 above) exists specifically to refuse that silent drop.

  Earlier design text in this file claimed "OpenSpec allows renaming scenarios within a MODIFIED requirement when the scenario name includes a count that changes." That claim is factually wrong per the two source locations above and is retracted here.

  Because the fix must satisfy the validator without producing a contradictory archived requirement, the correct form is REMOVED + ADDED, exactly as the Cline change used for its six→five transition. The new requirement is named **count-neutral** ("Built-in agent descriptors" rather than naming "four") specifically so a future built-in removal does not have to repeat a REMOVED/ADDED pair purely to change a number in the name — a future change can MODIFY "Built-in agent descriptors" and only touch the scenario body text, not the scenario name, as long as the scenario names themselves stay stable.

**Alternatives rejected:**

- **MODIFIED with scenario rename ("Five agents seeded" → "Four agents seeded")** — Rejected: not a validator error by itself (both names can coexist in one MODIFIED block), but archiving it would leave two contradictory scenarios (one claiming five agents, one claiming four) in the durable spec, because a MODIFIED block replaces the entire requirement verbatim rather than reconciling scenario-by-scenario.
- **MODIFIED keeping only "Four agents seeded"** — Rejected by the validator: `findScenarioLossIssues` reports "Five agents seeded" as a missing scenario name, since the current spec still has it and the incoming MODIFIED block does not.
- **REMOVED "Built-in descriptor set" + ADDED "Built-in descriptor set"** (same name) — OpenSpec does not allow the same requirement name in both REMOVED and ADDED sections of one delta (this was already established by the Cline change's design notes); a genuinely new name is required either way, so the new name should be chosen to avoid repeating the problem (count-neutral).

### 7. Test Strategy

**Decision:** 

1. Update the `BuiltinIds` array and all seed-count assertions from `5` to `4` in `PluginSeederTests`
2. Remove OpenClaude-specific assertions from the command-set tests (`SeededDescriptors_ExposeExpectedCommandSets`, `SeededDescriptors_DeclareExpectedUpdateSources`)
3. Update `EmbeddedPluginResourcesTests.ExpectedBuiltinIds` array and rename the test from `BuiltinPluginIds_EnumeratesTheFiveBuiltInAgents` to `BuiltinPluginIds_EnumeratesTheFourBuiltInAgents`
4. Add a test `BuiltinPluginIds_NoLongerContainsOpenClaude` parallel to the existing `NoLongerContainsClineCli` test
5. No change to `BrowsePluginsDialogViewModelTests` or `PluginInstallerTests` — they already use `opencode` as their reserved id fixture
6. Verify other tests using `openclaude` as a fixture do not assert built-in membership

**Rationale:** 

- The seeding tests verify the number of seeded agents and their command sets. Removing OpenClaude means the count drops to four, and the command-set tests no longer include OpenClaude.
- The `EmbeddedPluginResourcesTests` test name must be updated to reflect the new count ("Four" instead of "Five").
- The `BrowsePluginsDialogViewModelTests` and `PluginInstallerTests` tests already use `opencode` as their reserved id fixture (changed during the Cline removal), so they require no further change.
- Tests using `openclaude` as a generic fixture (if any) will be verified to ensure they do not assert built-in membership.

**No alternatives.**

### 8. Feed Deployment Sequence

**Decision:** The feed PR is opened and merged **after** CLIHub 1.1.1 is released. The PR includes:

1. Create `plugins/openclaude/plugin.json` (version `1.0.0`, publisher `CLIHub`, `minAppVersion` `1.1.1`)
2. Copy `logo.png` from the removed embedded resource
3. Remove `"openclaude"` from `reserved-ids.json`

The PR description links to the CLIHub 1.1.1 release and notes that the removal is shipped.

**Rationale:** Browse Plugins shows all feed entries regardless of `minAppVersion`, disabling the Install button when `minAppVersion` exceeds the running version. The feed PR cannot be merged before the CLIHub release because users on 1.1.0 would see OpenClaude in Browse Plugins (Install disabled), but local/manual `.clihub-plugin.zip` installs would fail with the reserved-id error. The cleanest sequencing is to merge the feed PR after the release.

**Alternatives rejected:**

- **Open the feed PR before the release** — Would require feed maintainers to delay merge until the release, adding coordination overhead.
- **Publish OpenClaude without updating `reserved-ids.json`** — Would leave the feed's reserved list stale.

### 9. Version Bump Scope

**Decision:** This change does **not** bump `Directory.Build.props` `<Version>` from `1.1.0` to `1.1.1`. The version bump is release preparation work, outside the scope of this change.

**Rationale:** The brief explicitly states "User decision: this change ships in CLIHub 1.1.1. So the future feed entry uses `minAppVersion: "1.1.1"`. Do NOT edit `Directory.Build.props` (version bump is release prep, out of scope) — record the release dependency instead." The release dependency is documented in proposal.md and tasks.md, and the feed entry's `minAppVersion` is set to `1.1.1`, but the application version bump happens during release preparation, not as part of this change.

**Alternatives rejected:**

- **Bump version in this change** — Would mix architectural change (removing a built-in) with release process (version bump), violating the separation of concerns stated in the brief.

## Deviations from Brief

None. The design follows the brief and the verified code.

## Open Questions

### 1. Release Version Assumption

**Question:** The brief assumes CLIHub version `1.1.1` for `minAppVersion`. Is this the correct next version, or should the plan use a placeholder and resolve the version at release time?

**Current decision:** Use `1.1.1` in the plan; the release-preparation step confirms and updates it if needed.

### 2. Feed PR Merge Timing

**Question:** Should the feed PR be opened as a draft before the CLIHub release, or opened after the release?

**Current decision:** Opened after the release (avoids reserved-id rejection on manual installs for 1.1.0 users).

**Alternative:** Opened as a draft before the release, merged after. This would allow feed maintainers to review the descriptor ahead of time.

**Recommendation:** Defer to feed maintainer preference; the plan defaults to "opened after."

## Risks and Follow-Ups

### Risks

1. **User confusion on existing OpenClaude entries** — Users with `origin=builtin` OpenClaude will see the feed entry at version `1.0.0` with status "Current." If they remove their built-in OpenClaude and install from the feed, the new entry will have `origin=installed`. The UI does not explain the origin change. This is a cosmetic issue, not a functional one.

2. **Feed deployment sequencing** — Browse Plugins shows all feed entries regardless of `minAppVersion` (Install is disabled when `minAppVersion` exceeds the running version, but the entry is visible). If the feed PR merges before CLIHub 1.1.1 is widely adopted, users on 1.1.0 see OpenClaude in Browse Plugins (Install disabled), and local/manual package installs fail with the reserved-id error.

3. **Dev builds with version 1.1.0** — The current `Directory.Build.props` has `<Version>1.1.0</Version>`, but the feed entry requires `minAppVersion=1.1.1`. Dev builds after this change (but before the version bump) will still have version 1.1.0, so the feed entry's Install button will be disabled. This is expected and acceptable: the feed entry is published after the 1.1.1 release, not before.

### Follow-Ups for `docs/backlog.md`

1. **Document feed deployment sequencing** — Add a release procedure note that feed PRs removing a built-in must be merged after the release, not before, to avoid reserved-id rejection on manual installs. (Revisit trigger: next built-in-to-feed migration.)

## Files to Change

### Application (CLIHub)

1. `src/CLIHub.Core/SeedPlugins/openclaude/plugin.json` — **DELETE**
2. `src/CLIHub.Core/SeedPlugins/openclaude/logo.png` — **DELETE**
3. `src/CLIHub.Core/SeedPlugins/openclaude/` — **DELETE** (directory, should be empty after 1 and 2)
4. `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs` — **EDIT** (lines 11, 69, 154, 163, 196, 293, 375, 406, 430, 452)
5. `tests/CLIHub.Core.Tests/Plugins/EmbeddedPluginResourcesTests.cs` — **EDIT** (lines 7, 10, add new test)
6. `README.md` — **EDIT** (line 18)
7. `docs/glossary.md` — **EDIT** (lines 61, 72)
8. `docs/architecture/plugins.md` — **EDIT** (line 44)
9. `openspec/specs/plugin-seeding/spec.md` — **EDIT** (delta under `openspec/changes/move-openclaude-to-feed/specs/plugin-seeding/spec.md`)

### Feed Repository (CLIHub-plugins)

**Post-release stage only (after CLIHub 1.1.1 is released):**

10. `D:\Projects\CLIHub-plugins\plugins\openclaude/plugin.json` — **CREATE**
11. `D:\Projects\CLIHub-plugins\plugins\openclaude/logo.png` — **CREATE**
12. `D:\Projects\CLIHub-plugins\reserved-ids.json` — **EDIT** (remove `"openclaude"`)
