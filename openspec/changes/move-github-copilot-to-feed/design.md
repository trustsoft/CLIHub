# Move GitHub Copilot to Feed — Design

## Verified Current State

All of the following was read directly from the working tree at commit `f1a014d` (tree clean,
`git status --porcelain` empty) during design, not inferred from the brief.

### Embedded GitHub Copilot Descriptor

**File:** `src/CLIHub.Core/SeedPlugins/github-copilot/plugin.json:1-27`

```json
{
  "id": "github-copilot",
  "name": "GitHub Copilot",
  "description": "GitHub Copilot CLI",
  "version": "1.0.0",
  "publisher": "CLIHub",
  "homepage": "https://github.com/github/copilot-cli",
  "commands": {
    "launch": { "executable": "copilot" },
    "resume": { "executable": "copilot", "arguments": "--continue" },
    "version": { "executable": "copilot", "arguments": "--version" },
    "update": { "executable": "copilot", "arguments": "update" }
  },
  "updateCheck": {
    "source": "githubReleases",
    "repo": "github/copilot-cli"
  },
  "detection": {
    "systemPaths": [
      "%USERPROFILE%\\.copilot",
      "%LOCALAPPDATA%\\GitHubCopilotCLI"
    ],
    "projectIndicators": [
      ".github/copilot-instructions.md"
    ]
  }
}
```

**Logo:** `src/CLIHub.Core/SeedPlugins/github-copilot/logo.png` exists (not read; binary).

### Seeding and Reserved IDs

**File:** `src/CLIHub.Core/Plugins/PluginSeeder.cs` — `EnsureBuiltinsExist()` enumerates descriptors
via `EmbeddedPluginResources.EnumerateDescriptors()`, applies three-hash refresh logic, and writes
descriptors for each embedded plugin. Unchanged by the Cline and OpenClaude migrations; this change
relies on the same mechanism without modifying it.

**File:** `src/CLIHub.Core/Plugins/EmbeddedPluginResources.cs` — `BuiltinPluginIds(Assembly)` returns
the distinct set of plugin ids from embedded descriptors. `PluginInstaller` uses this to determine
reserved ids. Removing the `github-copilot` embedded descriptor automatically unreserves the id —
confirmed by the identical mechanism already exercised for `cline-cli` (archived
`2026-11-05-move-cline-to-feed`) and `openclaude` (`move-openclaude-to-feed`, in flight).

### Current Built-in Set (already at 4, not 5)

**This is the first material deviation from a literal reading of the task brief.** The brief's
"Current state" section describes the built-ins as four (`opencode`, `pi`, `github-copilot`,
`qwen-code`) and that is exactly what was verified on disk — `openclaude` is **already gone** from
`src/CLIHub.Core/SeedPlugins/`, and both test files already assert the 4-agent state:

- `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs:11`:
  `private static readonly string[] BuiltinIds = ["opencode", "pi", "github-copilot", "qwen-code"];`
- `tests/CLIHub.Core.Tests/Plugins/EmbeddedPluginResourcesTests.cs:7,10,32`: `ExpectedBuiltinIds` is
  the same 4-element array; the test is already named
  `BuiltinPluginIds_EnumeratesTheFourBuiltInAgents`; a `BuiltinPluginIds_NoLongerContainsOpenClaude`
  test already exists and passes.

In other words, `move-openclaude-to-feed`'s source and test changes (its tasks.md stages 1–2) are
**already implemented and committed** at `f1a014d`, even though the main spec
(`openspec/specs/plugin-seeding/spec.md`) still shows the old 5-agent requirement and the change has
not been archived (`openspec list` reports it at 45/78 tasks, stages 6/7 — feed deployment and
smoke test — still open, which matches its tasks.md having those stages unchecked pending the
1.1.1 release).

**Consequence for this change:** the source and test edits below are described against the current
file content (4 agents, no `openclaude`) exactly as read — not against the brief's text, which
restates the same end state but was written before this verification. No discrepancy in outcome,
only in which baseline is "current."

### Tests (verified against actual current file content)

**File:** `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs`

- Line 11: `BuiltinIds = ["opencode", "pi", "github-copilot", "qwen-code"]`
- `Assert.Equal(4, ...)` seed-count assertions (seeding writes all four from empty): lines 69, 154,
  163, 196, 293, 375, 406
- `Assert.Equal(3, ...)` "other built-ins" assertions (one of the four already present/adopted, so
  only the other three are written): lines 110, 125, 215, 233, 250, 264, 312, 331
- Line 430 inside `SeededDescriptors_ExposeExpectedCommandSets` (declared at line 413):
  `() => AssertCommandSet(plugins["github-copilot"], full)`
- Line 452 inside `SeededDescriptors_DeclareExpectedUpdateSources` (declared at line 436):
  `() => Assert.Equal(new PluginUpdateCheck("githubReleases", Repo: "github/copilot-cli"), plugins["github-copilot"].UpdateCheck)`

All line numbers above were confirmed by reading the file directly; they match the brief exactly.

**File:** `tests/CLIHub.Core.Tests/Plugins/EmbeddedPluginResourcesTests.cs`

- Line 7: `ExpectedBuiltinIds = ["opencode", "pi", "github-copilot", "qwen-code"]`
- Line 10: `BuiltinPluginIds_EnumeratesTheFourBuiltInAgents` (already renamed from "Five" by the
  OpenClaude change; this change renames it again, to "Three")
- Line 22: `BuiltinPluginIds_NoLongerContainsClineCli` (precedent, unaffected)
- Line 32: `BuiltinPluginIds_NoLongerContainsOpenClaude` (precedent, unaffected — OpenClaude's own
  "no longer contains" test, added by the sister change, not this one)

**File:** `tests/CLIHub.Core.Tests/Plugins/PluginInstallerTests.cs:303,306`

```csharp
Assert.Contains("opencode", EmbeddedPluginResources.BuiltinPluginIds(typeof(PluginSeeder).Assembly));
...
("opencode/plugin.json", Text(ValidDescriptor.Replace("community", "opencode", StringComparison.Ordinal))));
```

Uses `"opencode"` as its reserved-id fixture — grep-verified, no `github-copilot` reference in this
file. No change needed, confirming the brief's claim.

**File:** `tests/CLIHub.Tests/ViewModels/Agents/BrowsePluginsDialogViewModelTests.cs:101,116`

```csharp
new("opencode", "Built-in OpenCode", ...)
...
Assert.DoesNotContain(vm.Plugins, p => p.Id == "opencode");
```

Uses `"opencode"` as its reserved-id fixture — grep-verified, no `github-copilot` reference. No
change needed.

**Repo-wide grep for `copilot` (case-insensitive)** against `tests/`, `src/`, `docs/`,
`openspec/specs/` found no other test asserting `github-copilot` built-in membership beyond the
`PluginSeederTests.cs` lines above. The remaining hits are either unrelated fixtures (e.g.
`AgentProcessMatcherTests.cs` using a plugin named `"copilot"` as a generic process-matching
fixture, not a built-in-membership assertion; `AgentVersionServiceTests.cs`,
`AgentUpdateAvailabilityServiceTests.cs`, `AgentReleaseClientTests.cs` all testing generic
`githubReleases`/version-string parsing behavior with GitHub Copilot's real repo string as sample
data, not built-in-count assertions) or archived change history (not touched by this change).

### Documentation (verified against actual current file content)

**File:** `README.md:18-19`

> "Built-in descriptors cover OpenCode, Pi, GitHub Copilot, and Qwen Code. Cline CLI and OpenClaude
> are available from the curated plugin feed; additional agents can be added as JSON plugins."

**File:** `docs/glossary.md:61-62,71-72`

- Line 61-62: "Agent — an AI coding agent CLI (OpenCode, Pi, Cline CLI, GitHub Copilot, OpenClaude,
  Qwen Code, or any user-added plugin)" — GitHub Copilot stays here; it remains a supported agent
  via the feed.
- Line 71-72: "Built-in agent — one of the agents whose descriptors (and logos) are embedded in the
  application binary: OpenCode, Pi, GitHub Copilot, Qwen Code." — GitHub Copilot is removed here.

**File:** `docs/architecture/plugins.md:44-45`

> "Built-in descriptors for OpenCode, Pi, GitHub Copilot, and Qwen Code are embedded and seeded
> using the three-hash refresh logic described below."

**File:** `docs/project-context.md` — read in full; the built-in set is not enumerated anywhere in
it (confirmed, same finding the sister change's design.md recorded). No change needed.

### The Sister Change — `move-openclaude-to-feed` (in flight, not archived)

**State verified:** `openspec list` reports `move-openclaude-to-feed  45/78 tasks  2h ago`.
Its `tasks.md` stages 1–5 (source removal, tests, docs, spec delta, build/verify) are checked
`[x]`; stages 6–8 (feed deployment, manual smoke, knowledge graph) are unchecked `[ ]`, consistent
with "feed deployment is post-release" and the release not having happened yet.

**Its spec delta** (`openspec/changes/move-openclaude-to-feed/specs/plugin-seeding/spec.md:1-61`):

- `## REMOVED Requirements` → `### Requirement: Built-in descriptor set` — the current main-spec
  five-agent block, reproduced verbatim (scenarios: "Five agents seeded", "Each seeded descriptor is
  loadable", "Complete command sets", "Commands match the agent CLI surface", "Unsupported command
  kinds are omitted")
- `## ADDED Requirements` → `### Requirement: Built-in agent descriptors` — a new, count-neutral
  name, with scenarios "Built-in agents seeded" (OpenCode, Pi, GitHub Copilot, Qwen Code — **GitHub
  Copilot still present** here, because this sister change only removes OpenClaude), "Each seeded
  descriptor is loadable", "Complete command sets" (same four agents), "Commands match the agent CLI
  surface", "Unsupported command kinds are omitted"

**The main spec is still on the old requirement:** `openspec/specs/plugin-seeding/spec.md:299-326`
still contains `### Requirement: Built-in descriptor set` with the five-agent scenarios
("Five agents seeded" at line 303-306, "Complete command sets" at line 313-316, both naming
OpenClaude). It will be replaced by `Built-in agent descriptors` (4 agents) only when
`move-openclaude-to-feed` is archived.

**Consequence this change must handle:** this change's own spec delta needs a base to act on. Three
options were evaluated (see Decision 6 below). The chosen shape —
`MODIFIED "Built-in agent descriptors"` — targets a requirement name that **does not exist yet** in
the main spec. That is the crux of the ordering dependency, verified in detail below.

### OpenSpec Validator Behavior on a MODIFIED Block Whose Base Is Absent

**Installed CLI:** `@fission-ai/openspec@1.15.0` at
`C:\Users\Maxim\AppData\Roaming\npm\node_modules\@fission-ai\openspec` (confirmed via
`openspec --version`).

**Source read directly:** `dist/core/validation/validator.js:600-684`
(`findScenarioLossIssues`). The function's own doc comment (lines 600-606) states:

> "Silent when the main spec or the requirement header is absent: applying a MODIFIED against a
> base that is not there yet is a different failure (a sister change still in flight is the
> legitimate case), and archive is the gate for it."

The implementation matches the comment exactly:

```js
const current = currentBlockFor(key);
if (!current)
    continue;                      // <- line 669: no base found, no issue raised
const diff = diffScenarioNames(current, block);
if (diff.missing.length === 0)
    continue;
issues.push({ level: 'ERROR', ... });   // <- only reached when a base WAS found and scenarios are missing
```

`currentBlockFor` (lines 646-657) walks the main spec's requirement blocks (keyed by
`normalizeRequirementName`) and the delta's own `RENAMED` pairs; if no block is found under that
name (or any name it was renamed from), it returns `undefined` and the loop at line 668-669 skips
straight to the next MODIFIED block, raising no issue at all.

**This is a materially different (and newer) behavior than the one the sister change's design.md
documents.** That design.md (written against an earlier state of the same package, or a different
install) describes `findScenarioLossIssues` as unconditionally comparing a MODIFIED block against
the current spec and says nothing about a missing-base case — which is why it chose REMOVED+ADDED
instead of MODIFIED for OpenClaude's own delta. The installed `1.15.0` validator, read directly for
this change, has since grown an explicit, documented exception for exactly the "sister change still
in flight" case this task describes. I am reporting what I verified in the installed CLI's source,
not reconciling it with the older design note — the code wins.

**Live confirmation (both commands run against this change's actual delta file, after it was
written):**

```
$ openspec validate move-github-copilot-to-feed --type change --strict
Change 'move-github-copilot-to-feed' is valid
ℹ [INFO] plugin-seeding/spec.md: Archive would refuse this delta: plugin-seeding MODIFIED failed for header "### Requirement: Built-in agent descriptors" - not found
```

```
$ openspec validate --all
...
✓ change/move-github-copilot-to-feed
✓ change/move-openclaude-to-feed
...
  ℹ [INFO] plugin-seeding/spec.md: Archive would refuse this delta: plugin-seeding MODIFIED failed for header "### Requirement: Built-in agent descriptors" - not found
...
Totals: 33 passed, 0 failed (33 items)
```

This is a third severity level, distinct from both the ERROR `findScenarioLossIssues` would raise
if a base *did* exist with missing scenarios, and the ordinary WARNING level used for style
nits ("Requirement text is very long"). The change is reported `✓` / "valid" either way — this
INFO line is advisory, not a validation failure, and the baseline run (before this change existed)
was `Totals: 32 passed, 0 failed (32 items)` with no such line, confirming the INFO is newly
introduced by this change's delta and not a pre-existing condition.

**What this means for archive ordering:** `openspec validate` (both `--all` and the single-change
form) does not fail on a MODIFIED block whose base is absent — it reports the condition at INFO
level and still returns `✓`/valid, exactly as quoted above. The validator's own naming for this
("Archive would refuse this delta") says explicitly that the real gate is `openspec archive`, not
`openspec validate`. **This change must be archived strictly after `move-openclaude-to-feed` is
archived.** If archived first (or if `move-openclaude-to-feed` is abandoned instead of archived),
`openspec archive move-github-copilot-to-feed` is expected to fail on the same header-not-found
condition the INFO line already names — the main spec would still have `Built-in descriptor set`,
not `Built-in agent descriptors`, so there is nothing for this change's MODIFIED block to apply to.
This was not executed (archiving is outside this task's scope — no `openspec archive` command was
run), but the INFO message's own wording ("Archive would refuse this delta... not found") is the
tool telling us this directly ahead of time, which is exactly the signal task brief asked this
design to surface rather than paper over.

## Decisions

### 1. Migration Semantics

**Decision:** Existing users keep their `origin=builtin` GitHub Copilot plugin untouched. New
installations obtain GitHub Copilot from the feed as `origin=installed`. No automatic migration or
removal.

**Rationale:** Identical to the precedent set by Cline CLI and OpenClaude. Deleting or migrating
existing GitHub Copilot plugins would violate "Never overwrite user content"
(`openspec/specs/plugin-seeding/spec.md:104-117`).

**Alternatives rejected:** Same as the sister change's Decision 1 — auto-migrating origin or
auto-removing directories both violate the never-overwrite-user-content requirement.

### 2. Feed Plugin Version

**Decision:** Publish the feed entry at `version=1.0.0`, matching the embedded version.

**Rationale:** Identical reasoning to the sister change's Decision 2. Existing `origin=builtin`
GitHub Copilot users will see "Current" rather than a spurious "Update available."

### 3. Reserved ID Removal Timing

**Decision:** The feed's `reserved-ids.json` removes `"github-copilot"` **after** CLIHub 1.1.1 is
released, in the same post-release stage that publishes the GitHub Copilot feed entry.

**Rationale:** Identical to the sister change's Decision 3 — Browse Plugins shows all feed entries
regardless of `minAppVersion` and only disables Install; removing the reserved id early would let
manual `.clihub-plugin.zip` installs with `id=github-copilot` succeed on 1.1.0 builds that still
embed the resource, confusingly contradicting the "reserved" rejection message that 1.1.0 would
otherwise still show for a mismatched manual install — actually the opposite failure mode applies
here: removing the id early does not cause a false rejection, it removes a rejection that should
still apply on 1.1.0. Either way, sequencing after the release avoids both failure directions.

### 4. Built-in Set After Removal

**Decision:** Three agents remain: OpenCode, Pi, Qwen Code. Update all references in code, tests,
docs, and specs.

**Rationale:** Removing GitHub Copilot from the embedded set is the core of the change. This is an
intermediate state on the way to the user-confirmed target (OpenCode, Pi, OMP, Qwen Code, Devin) —
OMP and Devin are added by a separate, later change, not this one.

**No alternatives.**

### 5. Feed Package Metadata

**Decision:** The feed descriptor includes:

- `"id": "github-copilot"`
- `"version": "1.0.0"`
- `"publisher": "CLIHub"` (curator attribution, unchanged from the built-in)
- `"minAppVersion": "1.1.1"`
- Commands: `launch`, `resume`, `version`, `update` (retain the existing set — note GitHub Copilot
  is the one built-in with no `init` command already, per the archived
  `2026-10-11-remove-init-agent-command` change; nothing to drop here)
- `updateCheck` (`githubReleases` / `github/copilot-cli`), `detection` unchanged

**Rationale:** Same as the sister change's Decision 5, applied to GitHub Copilot's own metadata.
`minAppVersion=1.1.1` is required because this change ships in the same release as
`move-openclaude-to-feed`.

### 6. Spec Delta Strategy — MODIFIED, Conditioned on Archive Order

**Decision:** `MODIFIED "Built-in agent descriptors"` in
`openspec/changes/move-github-copilot-to-feed/specs/plugin-seeding/spec.md`, reusing the **exact
scenario names** the sister change's `ADDED "Built-in agent descriptors"` block introduces:
"Built-in agents seeded", "Each seeded descriptor is loadable", "Complete command sets", "Commands
match the agent CLI surface", "Unsupported command kinds are omitted". Only the THEN bodies of
"Built-in agents seeded" and "Complete command sets" change (drop GitHub Copilot, list OpenCode,
Pi, Qwen Code); the other three scenarios are reproduced with unchanged bodies, as `MODIFIED`
requires reproducing the whole requirement.

**This is valid only if `move-github-copilot-to-feed` is archived after `move-openclaude-to-feed`.**
See "Verified Current State" above for the precise mechanism: the validator installed in this
repo (`@fission-ai/openspec@1.15.0`) silently accepts a MODIFIED block whose named base does not
yet exist in the main spec (that is a documented, intentional allowance for exactly this
in-flight-sister-change situation), but `openspec archive` is the actual gate — it needs the base
to exist by the time this change is archived, or there is nothing for it to modify.

**Why this is the preferred shape and not REMOVED+ADDED again:**

- `Built-in agent descriptors` already exists as a *target* requirement name (introduced by the
  sister change specifically so a future built-in-count change would not need another
  rename). Using REMOVED/ADDED again here would immediately contradict the stated purpose of that
  count-neutral name — the sister change's own design.md says: "a future change can MODIFY
  'Built-in agent descriptors' and only touch the scenario body text, not the scenario name, as
  long as the scenario names themselves stay stable." This change is exactly that future change.
- Reusing the same scenario names (not renaming them) keeps `diffScenarioNames` happy once the base
  does exist: a `MODIFIED` block's scenario-name set must be a superset of the current block's (no
  omissions), and since both use the identical five names, there is no loss to flag.

**Alternatives rejected:**

- **REMOVED "Built-in agent descriptors" + ADDED "Built-in agent descriptors" (same name)** —
  OpenSpec does not allow the same requirement name in both REMOVED and ADDED sections of one delta
  (established precedent in the Cline change's design notes, re-confirmed by the OpenClaude
  change). Would also require inventing yet another name, defeating the count-neutral design.
- **REMOVED "Built-in agent descriptors" + ADDED "Built-in agent descriptors (v2)" or similar** —
  Avoids the same-name restriction but reintroduces a naming churn the sister change explicitly
  designed against. Rejected as unnecessary.
- **MODIFIED "Built-in descriptor set"** (the main spec's *current*, pre-sister-archive name) —
  Rejected: this requirement is being removed by the sister change, not kept. Modifying a
  requirement that is about to be deleted under a different change would conflict with that
  change's own REMOVED block once both are archived in the planned order (archiving this change's
  MODIFIED against "Built-in descriptor set" would succeed today, since that name does exist in the
  main spec right now, but it would collide with `move-openclaude-to-feed`'s REMOVED block
  targeting the same name — only one change can remove/modify a given requirement name in a given
  archive pass, and the two changes' operations on the same name are not reconcilable). The correct
  anchor is the *name that will exist after the sister change is archived*, which is
  `Built-in agent descriptors`.
- **Wait and only write this change after `move-openclaude-to-feed` is archived** — Rejected per the
  task brief: the task is to prepare this change now, in parallel with the unarchived sister change,
  and document the ordering dependency rather than block on it.

### 7. Test Strategy

**Decision:**

1. Update `BuiltinIds` (`PluginSeederTests.cs:11`) and all seed-count assertions from `4`→`3`
   (lines 69, 154, 163, 196, 293, 375, 406) and the derived "other built-ins" assertions from
   `3`→`2` (lines 110, 125, 215, 233, 250, 264, 312, 331)
2. Remove the `github-copilot` assertion from `SeededDescriptors_ExposeExpectedCommandSets` (line
   430) and from `SeededDescriptors_DeclareExpectedUpdateSources` (line 452)
3. Update `EmbeddedPluginResourcesTests.ExpectedBuiltinIds` (line 7) to 3 elements; rename the test
   at line 10 from `BuiltinPluginIds_EnumeratesTheFourBuiltInAgents` to
   `BuiltinPluginIds_EnumeratesTheThreeBuiltInAgents`
4. Add `BuiltinPluginIds_NoLongerContainsGithubCopilot`, parallel to the existing
   `BuiltinPluginIds_NoLongerContainsClineCli` (line 22) and
   `BuiltinPluginIds_NoLongerContainsOpenClaude` (line 32)
5. No change to `PluginInstallerTests.cs` or `BrowsePluginsDialogViewModelTests.cs` — both already
   use `opencode`, verified above
6. No other test in the repo asserts `github-copilot` built-in membership (grep-verified above)

**Rationale:** Mirrors the sister change's Test Strategy decision exactly, scoped to GitHub
Copilot's removal instead of OpenClaude's.

**No alternatives.**

### 8. Feed PR Coordination

**Decision:** The feed-deployment stage (tasks.md Stage 6) targets a **single combined PR** against
`D:\Projects\CLIHub-plugins` that adds both `plugins/github-copilot/` and `plugins/openclaude/` (if
not already merged by that point) and removes both ids from `reserved-ids.json` in one commit.

**Rationale:**

- Both removals ship in the same CLIHub 1.1.1 release, so they share the same `minAppVersion` and
  the same release-dependency gate. Splitting them into two PRs doubles CI runs and review
  overhead for no benefit, since both are blocked on the identical precondition ("CLIHub 1.1.1 is
  released").
- `move-openclaude-to-feed`'s own tasks.md Stage 6 already plans a PR that adds `plugins/openclaude/`
  and edits `reserved-ids.json`. If that PR merges first (its implementation session runs before
  this change's), this change's feed task should **extend** that same PR (or open a follow-up PR
  against the same `reserved-ids.json` state) rather than assume a fresh file. If this change's
  feed task runs first instead, it should add `plugins/github-copilot/` and leave `"openclaude"` in
  `reserved-ids.json` untouched, so the OpenClaude sister change's later PR only needs to remove its
  own id.
- Either ordering is safe because both edits are to the same JSON array (removing one id at a time
  is commutative) and to disjoint plugin directories (no file conflict).

**Alternatives rejected:**

- **Two independent PRs, no coordination note** — Risks a merge conflict on `reserved-ids.json` if
  both are prepared from the same base commit without rebasing, and risks duplicate/contradictory
  PR descriptions both claiming to be "the" post-1.1.1 feed migration PR.
- **Block this change's feed PR entirely on the OpenClaude PR merging first** — Over-constrains
  sequencing for no safety benefit; the JSON-array edit is commutative either way.

### 9. Version Bump Scope

**Decision:** This change does **not** bump `Directory.Build.props` `<Version>` from `1.1.0` to
`1.1.1`. Verified current value: `Directory.Build.props:15` → `<Version>1.1.0</Version>`.

**Rationale:** Identical to the sister change's Decision 9. The version bump is release-preparation
work shared by both the OpenClaude and GitHub Copilot removals; it happens once, during release
prep, not per migrating change.

**Alternatives rejected:** Bumping here would mix two independent architectural changes
(OpenClaude's and GitHub Copilot's migrations) with a release-process step that belongs to neither.

## Deviations from Brief

1. **Built-in set is already at 4, not "4→3 to be verified"** — The brief frames the current state
   as something to re-verify; verification confirmed it exactly (`openclaude` already removed from
   source and tests by the sister change's implementation session, main spec not yet updated to
   match). No deviation in outcome — recorded here only because the brief asked for explicit
   re-verification and flagged ambiguity that, on inspection, resolved cleanly in the brief's
   favor.
2. **Validator behavior is newer/different from the sister change's documented understanding** —
   The installed `@fission-ai/openspec@1.15.0` validator has an explicit, commented allowance for a
   MODIFIED block whose base does not exist yet ("a sister change still in flight is the legitimate
   case"). This directly enables the `MODIFIED "Built-in agent descriptors"` shape this change
   uses, which was not available (or not documented as available) when the sister change's
   design.md was written and chose REMOVED+ADDED instead. This is reported as a verified fact about
   the current tool, not a contradiction to reconcile — the two changes legitimately use different
   delta shapes because one targets a requirement that exists today and the other targets one that
   will exist once its predecessor is archived.

## Open Questions

### 1. Archive Ordering Enforcement

**Question:** Nothing in the OpenSpec tooling *mechanically* prevents archiving
`move-github-copilot-to-feed` before `move-openclaude-to-feed` (the validator stays silent on a
missing base; only `archive` itself would fail, and only at that point). Should tasks.md include an
explicit manual check (e.g., `openspec list` confirming `move-openclaude-to-feed` is archived)
before this change's own archive step, or is documenting the dependency in proposal.md sufficient?

**Current plan:** tasks.md includes an explicit pre-archive gate task that checks
`move-openclaude-to-feed` is archived first. Flagged as open because the enforcement is
process-only, not tool-enforced.

### 2. Combined vs. Sequential Feed PR — Final Call

**Question:** Decision 8 above picks "single combined PR, or whichever lands first extends
gracefully" as the default coordination strategy, but the actual choice depends on which
change's implementation session reaches the feed-deployment stage first — information not
available at proposal time. Should this be decided now (pick an explicit order) or left to the
implementing session to check `D:\Projects\CLIHub-plugins` state at the time?

**Current plan:** Left to the implementing session (tasks.md Stage 6 includes a check-before-acting
step: inspect `reserved-ids.json` and `plugins/` for an existing `openclaude` entry before deciding
whether to extend an existing PR branch or open a fresh one).

### 3. Release Version Assumption

**Question:** Same open question the sister change recorded — is `1.1.1` confirmed as the next
version, or should the plan use a placeholder?

**Current plan:** Use `1.1.1` per the user's explicit decision recorded in the task brief; this is
no longer "open" in the sense of needing a decision, but is listed because the actual release has
not happened yet and the version could theoretically change before then.

## Risks and Follow-Ups

### Risks

1. **User confusion on existing GitHub Copilot entries** — Same cosmetic risk as the sister change:
   users with `origin=builtin` GitHub Copilot will see the feed entry at version `1.0.0` with status
   "Current," and origin does not change until they reinstall from the feed.
2. **Feed deployment sequencing** — Same risk as the sister change: if the feed PR merges before
   CLIHub 1.1.1 is adopted, 1.1.0 users see a disabled Install button (not reserved-id breakage,
   since Browse Plugins itself does not filter by `minAppVersion`), but manual package installs with
   `id=github-copilot` still correctly fail as reserved on 1.1.0 until that build is retired.
3. **Sister-change coupling** — This change's spec delta is only valid in one archive order. If
   `move-openclaude-to-feed` is abandoned, reworked to a different requirement name, or archived
   with a different shape than currently planned, this change's `MODIFIED "Built-in agent
   descriptors"` delta would need to be revised before it could be archived. This is a real
   dependency, not a cosmetic one — tracked as Dependency #1 in proposal.md.
4. **Two changes touching the same main-spec requirement in sequence** — Operationally this means
   whoever implements this change must re-check the main spec's exact requirement text immediately
   before archiving (not just before planning), in case `move-openclaude-to-feed` archived with any
   wording drift from what is reproduced in its own delta file today.

### Follow-Ups for `docs/backlog.md`

1. **Document the two-built-in-removals-same-release pattern** — Worth a backlog note: when two
   built-in-to-feed migrations ship in the same release, the second one's spec delta can target the
   first one's ADDED requirement name with MODIFIED, provided archive order is respected and the
   installed OpenSpec version's validator tolerates a missing base at validate-time (confirmed here
   for `@fission-ai/openspec@1.15.0`; re-verify if the tool is upgraded).

## Files to Change

### Application (CLIHub)

1. `src/CLIHub.Core/SeedPlugins/github-copilot/plugin.json` — **DELETE**
2. `src/CLIHub.Core/SeedPlugins/github-copilot/logo.png` — **DELETE**
3. `src/CLIHub.Core/SeedPlugins/github-copilot/` — **DELETE** (directory, empty after 1 and 2)
4. `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs` — **EDIT** (lines 11, 69, 110, 125, 154,
   163, 196, 215, 233, 250, 264, 293, 312, 331, 375, 406, 430, 452)
5. `tests/CLIHub.Core.Tests/Plugins/EmbeddedPluginResourcesTests.cs` — **EDIT** (lines 7, 10, add new
   test)
6. `tests/CLIHub.Core.Tests/Plugins/PluginInstallerTests.cs` — **EDIT** (add
   `Inspect_FormerlyBuiltinGithubCopilotId_NotRejectedAsReserved`, per tasks.md 5.6's "or write a
   test" option; same shape as the Cline/OpenClaude precedents)
7. `README.md` — **EDIT** (line 18)
8. `docs/glossary.md` — **EDIT** (line 72)
9. `docs/architecture/plugins.md` — **EDIT** (line 44)
10. `openspec/specs/plugin-seeding/spec.md` — **EDIT** (delta under
   `openspec/changes/move-github-copilot-to-feed/specs/plugin-seeding/spec.md`; applied by `archive`,
   not directly — ordering dependency on `move-openclaude-to-feed` applies)

### Feed Repository (CLIHub-plugins)

**Post-release stage only (after CLIHub 1.1.1 is released):**

10. `D:\Projects\CLIHub-plugins\plugins\github-copilot\plugin.json` — **CREATE**
11. `D:\Projects\CLIHub-plugins\plugins\github-copilot\logo.png` — **CREATE**
12. `D:\Projects\CLIHub-plugins\reserved-ids.json` — **EDIT** (remove `"github-copilot"`; coordinate
    with the OpenClaude feed PR per Decision 8)
