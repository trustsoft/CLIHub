# Move Cline CLI to Feed — Design

## Verified Current State

### Embedded Cline Descriptor

**File:** `src/CLIHub.Core/SeedPlugins/cline-cli/plugin.json`

```json
{
  "id": "cline-cli",
  "name": "Cline CLI",
  "description": "Cline AI coding assistant CLI",
  "version": "1.0.0",
  "publisher": "CLIHub",
  "homepage": "https://www.npmjs.com/package/cline",
  "commands": {
    "launch": { "executable": "cline" },
    "version": { "executable": "cline", "arguments": "--version" },
    "update": { "executable": "cline", "arguments": "update" }
  },
  "updateCheck": {
    "source": "npm",
    "package": "cline"
  },
  "detection": {
    "systemPaths": [
      "%USERPROFILE%\\.cline",
      "%LOCALAPPDATA%\\cline"
    ],
    "projectIndicators": [
      ".clinerules"
    ]
  }
}
```

**Logo:** `src/CLIHub.Core/SeedPlugins/cline-cli/logo.png` exists.

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

Reserved IDs are derived from the embedded resources at runtime. Removing the `cline-cli` embedded descriptor automatically unreserves the id.

### Tests

**File:** `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs:11`

```csharp
private static readonly string[] BuiltinIds = ["opencode", "pi", "cline-cli", "github-copilot", "openclaude", "qwen-code"];
```

**Assertions expecting six built-in agents:**

- Line 69: `Assert.Equal(6, written);` — first-run seeding
- Line 153: `Assert.Equal(6, written);` — missing plugins folder
- Line 162: `Assert.Equal(6, seeder.EnsureBuiltinsExist());` — existing plugins present
- Line 195: `Assert.Equal(6, written);` — deleted built-in re-seeded
- Line 292: `Assert.Equal(6, written);` — unmodified built-in refreshed on app update
- Line 374: `Assert.Equal(6, written);` — disabled built-in not re-seeded
- Line 405: `Assert.Equal(6, plugins.Count);` — each seeded descriptor is loadable

**Command-set assertions:**

- Test `SeedDescriptors_ExposeCommandSets`: Verifies `cline-cli` has `launch` and `version`, and that `resume` is null
- Test `SeedDescriptors_ExposeCommandSets`: Verifies `cline-cli` update check is npm source with package `"cline"`

**File:** `tests/CLIHub.Tests/ViewModels/Agents/BrowsePluginsDialogViewModelTests.cs:93-117`

Test `LoadAsync_ReservedBuiltInId_FiltersOut`:

- Line 101: Creates feed entry with `id="cline-cli"` (a reserved built-in id)
- Line 116: Asserts that `cline-cli` is filtered out of `vm.Plugins`

After `cline-cli` is removed from the embedded set, it is no longer reserved and this test will fail. The test must change its reserved id to a still-reserved built-in (e.g. `"opencode"` or `"github-copilot"`).

**File:** `tests/CLIHub.Core.Tests/Infrastructure/Processes/AgentProcessMatcherTests.cs:117`

Test `IsMatch_UnscopedNpmPackage_ReturnsTrue`:

- Uses `cline-cli` and `cline` as a generic npm/process fixture
- Does not assert built-in membership or reserved status
- No change needed

### Specs

**File:** `openspec/specs/plugin-seeding/spec.md:108-135`

#### Requirement: Built-in descriptor coverage

- **Scenario: Six agents seeded** (line 108-111) — "WHEN seeding runs THEN descriptors for OpenCode, Pi, Cline CLI, GitHub Copilot, OpenClaude, and Qwen Code are available"
- **Scenario: Complete command sets** (line 119-125) — Lists the command set for each agent; Cline CLI defines `launch` and `version` (no `resume`, no `update` in the scenario, though the descriptor has `update`)
- **Scenario: Commands match the agent CLI surface** (line 127-130) — Example `cline update` checks for and installs updates

**Note:** The scenario "Complete command sets" line 124 states "Cline CLI defines `launch` and `version`" but the descriptor (and test line 455) includes `update`. This is a spec/implementation discrepancy that predates this change; the design preserves the existing `update` command in the feed descriptor.

**File:** `openspec/specs/plugin-installation/spec.md:62-68`

#### Scenario: Built-in id rejected for installed plugin

- Line 62-68: States that a package with a built-in id is rejected, and notes that built-in IDs are enumerated from embedded resources (not a hardcoded list).
- The scenario does **not** explicitly list the six ids. No spec delta is required for this scenario.

**File:** `openspec/specs/agent-process-matching/spec.md:48-51`

#### Scenario: Unscoped package matches

- Line 48-51: Uses `cline` as an example of an unscoped npm package. The scenario is generic; no change is needed. The Cline plugin descriptor will still declare `"package": "cline"` in the feed, so the example remains accurate.

### Documentation

**File:** `README.md:18`

"Built-in descriptors cover OpenCode, Pi, Cline CLI, GitHub Copilot, OpenClaude, and Qwen Code; additional agents can be added as JSON plugins."

**File:** `docs/glossary.md:61,72`

- Line 61: "Agent — an AI coding agent CLI (OpenCode, Pi, Cline CLI, GitHub Copilot, OpenClaude, Qwen Code)"
- Line 71-72: "Built-in agent — one of the agents whose descriptors (and logos) are embedded in the application binary: OpenCode, Pi, Cline CLI, GitHub Copilot, OpenClaude, Qwen Code."

**File:** `docs/architecture/plugins.md:44-45`

"Built-in descriptors for OpenCode, Pi, Cline CLI, GitHub Copilot, OpenClaude, and Qwen Code are embedded and seeded using the three-hash refresh logic described below."

**File:** `docs/project-context.md:18`

Runtime facts section lists the six built-in agents. (Not at line 18; that's the CLI Agents UI reference. The built-in list is not explicitly stated in `project-context.md` as of the verified read.)

### Feed Repository

**File:** `D:\Projects\CLIHub-plugins\reserved-ids.json:1-8`

```json
[
  "cline-cli",
  "github-copilot",
  "openclaude",
  "opencode",
  "pi",
  "qwen-code"
]
```

**File:** `D:\Projects\CLIHub-plugins\plugins\`

Contains only `README.md`. No published plugins yet.

**File:** `D:\Projects\CLIHub-plugins\README.md:37-59`

Documents the feed workflow: copy `templates/plugin.json`, fill in metadata and commands, add `logo.png`, package with `scripts/package-plugin.ps1`, validate with `node scripts/build-index.mjs --check`, open a PR. CI validates, packages, publishes a GitHub Release, and updates `index.json`.

## Decisions

### 1. Migration Semantics

**Decision:** Existing users keep their `origin=builtin` Cline plugin untouched. New installations obtain Cline from the feed as `origin=installed`. No automatic migration or removal.

**Rationale:** Deleting or migrating existing Cline plugins would violate the "Never overwrite user content" requirement (`openspec/specs/plugin-seeding/spec.md:136-150`). The three-hash seeding logic is designed to preserve user modifications; removing the embedded resource means seeding no longer manages Cline, so existing installations remain as-is.

**Alternatives rejected:**

- **Auto-migrate existing `origin=builtin` to `origin=installed`** — Would require a one-time state-file rewrite that fabricates an installed origin for content the user did not explicitly install. This violates the principle that `origin=installed` means "user installed from a package."
- **Auto-remove existing Cline directories** — Would delete user content without consent, violating the "Never overwrite user content" requirement.

### 2. Feed Plugin Version

**Decision:** Publish the feed entry at `version=1.0.0`, matching the embedded version.

**Rationale:** Existing users with `origin=builtin` Cline (version `1.0.0` from the embedded descriptor) will see the feed entry as version `1.0.0`. The UI's version comparison will show "Current" instead of "Update available," which is the correct signal: their already-seeded Cline is the same as the feed's.

If we published the feed entry at `1.0.1`, existing users would see an "Update available" prompt. Clicking Update would fail (cannot replace a built-in) or succeed (if the install flow allows replacing a built-in with an installed plugin, which is not explicitly covered in the current spec). The `1.0.0` version avoids this ambiguity.

**Alternatives rejected:**

- **Publish at `1.0.1` and auto-migrate** — Would require the migration logic rejected in Decision 1.
- **Publish at `2.0.0` or higher** — Would signal a breaking change when there is none. The descriptor content is identical.

### 3. Reserved ID Removal Timing

**Decision:** The feed's `reserved-ids.json` is updated **after** CLIHub 1.1.0 is released, in the same PR that publishes the Cline feed entry.

**Rationale:** Browse Plugins shows all feed entries regardless of `minAppVersion`, disabling the Install button when `minAppVersion` exceeds the running version. If `reserved-ids.json` is updated before the CLIHub release, users on 1.0.x will see Cline in Browse Plugins. The Install button will be disabled (due to `minAppVersion=1.1.0`), but local/manual package installs via `.clihub-plugin.zip` will fail with "Plugin id 'cline-cli' is reserved" because the embedded resource still exists in 1.0.x.

By updating `reserved-ids.json` after the release, we avoid this reserved-id rejection for manual installs on 1.0.x (though the feed entry will still appear in Browse Plugins, grayed out).

**Alternatives rejected:**

- **Update `reserved-ids.json` before the release** — Would cause the reserved-id rejection on manual installs for 1.0.x users.
- **Gate feed visibility on `minAppVersion` alone** — Browse Plugins does not filter by `minAppVersion`; it shows all entries and disables Install when the version requirement is not met.

### 4. Built-in Set After Removal

**Decision:** Five agents remain: OpenCode, Pi, GitHub Copilot, OpenClaude, Qwen Code. Update all references in code, tests, docs, and specs.

**Rationale:** Removing Cline from the embedded set is the core of the change. All derived assertions and documentation must reflect the new built-in count.

**No alternatives.**

### 5. Feed Package Metadata

**Decision:** The feed descriptor includes:

- `"id": "cline-cli"`
- `"version": "1.0.0"`
- `"publisher": "CLIHub"` (curator attribution)
- `"minAppVersion": "1.1.0"`
- Commands: `launch`, `version`, `update` (retain the existing set, including `update` even though the spec scenario omits it)
- `updateCheck`, `detection` unchanged

**Rationale:** The feed descriptor must be valid according to the feed's validation rules and CLIHub's `PluginDescriptorValidator`. The `minAppVersion` field ensures the Install button is disabled on CLIHub versions below 1.1.0 (Browse Plugins shows all entries but disables Install when `minAppVersion` exceeds the running version). The `publisher` field uses "CLIHub" to signal curator responsibility — the CLIHub team curates the feed and chose to include Cline.

**Alternatives rejected:**

- **Publisher "Community"** — Too vague; does not attribute curation responsibility.
- **Publisher "Cline Bot Inc."** — The built-in used "CLIHub"; keeping it maintains consistency.

### 6. Spec Delta Strategy

**Decision:**

- **REMOVE** requirement "Built-in descriptor coverage" with its four scenarios referencing six agents and Cline
- **ADD** requirement "Built-in descriptor set" with five scenarios referencing five agents without Cline
- The requirement name change ("coverage" → "set") allows OpenSpec to accept the REMOVED/ADDED pattern
- **NO CHANGE** to `openspec/specs/plugin-installation/spec.md` — The reserved-id scenario does not list specific ids

**Rationale:** OpenSpec does not allow renaming scenarios within a MODIFIED requirement, and does not allow the same requirement name in both REMOVED and ADDED sections. Changing the requirement name from "Built-in descriptor coverage" to "Built-in descriptor set" allows the REMOVED/ADDED approach while keeping the requirement semantically identical. The spec delta removes all Cline references and updates the agent count from six to five.

**Alternatives rejected:**

- **MODIFIED with "Six agents seeded" preserved** — Would keep a false scenario name ("Six" when five are shipped).
- **MODIFIED with scenario rename** — OpenSpec rejects scenario renames in MODIFIED requirements.

### 7. Test Strategy

**Decision:** 

1. Update the `BuiltinIds` array and all seed-count assertions from `6` to `5` in `PluginSeederTests`
2. Remove Cline-specific assertions from the command-set test (`SeedDescriptors_ExposeCommandSets`)
3. Change the reserved id in `BrowsePluginsDialogViewModelTests.LoadAsync_ReservedBuiltInId_FiltersOut` from `"cline-cli"` to a still-reserved built-in (e.g. `"opencode"` or `"github-copilot"`)
4. Verify other tests using `cline` as a fixture do not assert built-in membership

**Rationale:** 

- The seeding tests verify the number of seeded agents and their command sets. Removing Cline means the count drops to five, and the command-set test no longer includes Cline.
- The `BrowsePluginsDialogViewModelTests` test verifies that feed entries with reserved built-in ids are filtered out. After `cline-cli` is removed from the embedded set, it is no longer reserved, so the test will fail. Changing the test's reserved id to a still-reserved built-in (e.g. `"opencode"`) keeps the test's assertion valid.
- Tests using `cline` as a generic npm/process fixture (e.g. `AgentProcessMatcherTests`) do not assert built-in membership and remain unchanged.

**No alternatives.**

### 8. Feed Deployment Sequence

**Decision:** The feed PR is opened and merged **after** CLIHub 1.1.0 is released. The PR includes:

1. Create `plugins/cline-cli/plugin.json` (version `1.0.0`, publisher `CLIHub`, `minAppVersion` `1.1.0`)
2. Copy `logo.png` from the removed embedded resource
3. Remove `"cline-cli"` from `reserved-ids.json`

The PR description links to the CLIHub 1.1.0 release and notes that the removal is shipped.

**Rationale:** Browse Plugins shows all feed entries regardless of `minAppVersion`, disabling the Install button when `minAppVersion` exceeds the running version. The feed PR cannot be merged before the CLIHub release because users on 1.0.x would see Cline in Browse Plugins (Install disabled), but local/manual `.clihub-plugin.zip` installs would fail with the reserved-id error. The cleanest sequencing is to merge the feed PR after the release.

**Alternatives rejected:**

- **Open the feed PR before the release** — Would require feed maintainers to delay merge until the release, adding coordination overhead.
- **Publish Cline without updating `reserved-ids.json`** — Would leave the feed's reserved list stale.

## Deviations from Brief

None. The design follows the brief and the verified code.

## Open Questions

### 1. Release Version Assumption

**Question:** The brief assumes CLIHub version `1.1.0` for `minAppVersion`. Is this the correct next version, or should the plan use a placeholder and resolve the version at release time?

**Recommendation:** Use `1.1.0` in the plan; the release-preparation step confirms and updates it if needed.

### 2. Feed PR Merge Timing

**Question:** Should the feed PR be opened as a draft before the CLIHub release, or opened after the release?

**Current decision:** Opened after the release (avoids reserved-id rejection on manual installs for 1.0.x users).

**Alternative:** Opened as a draft before the release, merged after. This would allow feed maintainers to review the descriptor ahead of time.

**Recommendation:** Defer to feed maintainer preference; the plan defaults to "opened after."

## Risks and Follow-Ups

### Risks

1. **User confusion on existing Cline entries** — Users with `origin=builtin` Cline will see the feed entry at version `1.0.0` with status "Current." If they remove their built-in Cline and install from the feed, the new entry will have `origin=installed`. The UI does not explain the origin change. This is a cosmetic issue, not a functional one.

2. **Feed deployment sequencing** — Browse Plugins shows all feed entries regardless of `minAppVersion` (Install is disabled when `minAppVersion` exceeds the running version, but the entry is visible). If the feed PR merges before CLIHub 1.1.0 is widely adopted, users on 1.0.x see Cline in Browse Plugins (Install disabled), and local/manual package installs fail with the reserved-id error.

### Follow-Ups for `docs/backlog.md`

1. **Document feed deployment sequencing** — Add a release procedure note that feed PRs removing a built-in must be merged after the release, not before, to avoid reserved-id rejection on manual installs. (Revisit trigger: next built-in-to-feed migration.)
