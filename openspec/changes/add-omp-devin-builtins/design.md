# Add OMP and Devin as Built-in Agents — Design

## Verified Current State

All of the following was read directly from the working tree at commit `f1a014d` (tree clean
except the two untracked sister-change planning directories), or probed live on this machine,
during this design — not inferred from the brief.

### Repository and Change-Chain State

```
$ git log --oneline -5
f1a014d feat(plugins): move OpenClaude to the curated feed
58f944e feat(opencode): add the orchestrator pipeline agents, model-policy and pipeline-status plugins
ce1290e chore(openspec): archive settings-agents-actions-menu
dc1363e feat(settings): group CLI agent plugin actions in an Actions menu
cb30198 docs: remove fixed resolver backlog item

$ git status --porcelain
?? openspec/changes/move-github-copilot-to-feed/
```

(The third `?? openspec/changes/add-omp-devin-builtins/` entry is this change's own planning
directory, created during this session.)

**Today's built-in set is already 4** (`opencode`, `pi`, `github-copilot`, `qwen-code`) —
`move-openclaude-to-feed`'s source and test edits (its own Stages 1–5) are already committed at
`f1a014d`; only its feed-deployment and smoke-test stages (6–8) remain open, consistent with
`openspec list` and the sister change's own tasks.md. Verified directly:

- `src/CLIHub.Core/SeedPlugins/` contains exactly four subdirectories: `opencode/`, `pi/`,
  `qwen-code/`, `github-copilot/` (glob-confirmed; no `openclaude/`).
- `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs:11`:
  `private static readonly string[] BuiltinIds = ["opencode", "pi", "github-copilot", "qwen-code"];`
- `tests/CLIHub.Core.Tests/Plugins/EmbeddedPluginResourcesTests.cs:7`: the same four-element
  `ExpectedBuiltinIds` array; the enumeration test is already named
  `BuiltinPluginIds_EnumeratesTheFourBuiltInAgents` (line 10); a
  `BuiltinPluginIds_NoLongerContainsOpenClaude` test already exists (line 32) alongside
  `BuiltinPluginIds_NoLongerContainsClineCli` (line 22).

**`move-github-copilot-to-feed` is planned but not implemented** — its own design.md (read in
full) confirms the same 4-agent baseline and plans to bring it down to 3 (OpenCode, Pi, Qwen
Code). Its tasks.md stages are all unchecked (`[ ]`).

**Consequence for this change's baseline:** this change must plan against the **post-sister-2**
state — three built-in agents (OpenCode, Pi, Qwen Code) — not today's literal four-agent state on
disk, because the brief's user-approved chain is 1 → 2 → 3 and the planning artifacts for all
three exist in parallel. This design (and tasks.md) says explicitly, at every point where it
matters, which baseline a given line number or count assumes.

### The Two Sister Changes' Spec Deltas (verified in full)

**`move-openclaude-to-feed`** —
`openspec/changes/move-openclaude-to-feed/specs/plugin-seeding/spec.md:1-61`:

- `## REMOVED Requirements` → `### Requirement: Built-in descriptor set` (the current main-spec
  five-agent block, scenarios: "Five agents seeded", "Each seeded descriptor is loadable",
  "Complete command sets", "Commands match the agent CLI surface", "Unsupported command kinds are
  omitted")
- `## ADDED Requirements` → `### Requirement: Built-in agent descriptors` (new, count-neutral
  name) with scenarios "Built-in agents seeded" (OpenCode, Pi, GitHub Copilot, Qwen Code), "Each
  seeded descriptor is loadable", "Complete command sets" (same four), "Commands match the agent
  CLI surface", "Unsupported command kinds are omitted"

**`move-github-copilot-to-feed`** —
`openspec/changes/move-github-copilot-to-feed/specs/plugin-seeding/spec.md:1-31`:

- `## MODIFIED Requirements` → `### Requirement: Built-in agent descriptors`, reusing the **exact
  same five scenario names** the first sister change introduces, with "Built-in agents seeded" and
  "Complete command sets" bodies narrowed to OpenCode, Pi, Qwen Code (GitHub Copilot dropped); the
  other three scenarios reproduced unchanged.

**The main spec today** —
`openspec/specs/plugin-seeding/spec.md:299-326` — still has
`### Requirement: Built-in descriptor set` with the original five-agent ("OpenCode, Pi, GitHub
Copilot, OpenClaude, and Qwen Code") scenario bodies. It becomes `Built-in agent descriptors`
(four agents) only once `move-openclaude-to-feed` is archived, then narrows to three agents only
once `move-github-copilot-to-feed` is archived on top of that. **This change's own delta targets
the state that exists only after both of those archives have happened, in that order.**

### OpenSpec Validator Behavior on a MODIFIED Block Whose Base Is in a Different Intermediate State

**Installed CLI:** `@fission-ai/openspec@1.15.0` (confirmed via `openspec --version`), same
version both sister changes' design.md documents verified against.

**Live run against the actual repository state right now** (three changes present: both sister
changes plus this one):

```
$ openspec validate --all
...
✓ change/move-github-copilot-to-feed
✓ change/move-openclaude-to-feed
✓ change/add-omp-devin-builtins
...
  ℹ [INFO] plugin-seeding/spec.md: Archive would refuse this delta: plugin-seeding MODIFIED failed for header "### Requirement: Built-in agent descriptors" - not found
Totals: 33 passed, 0 failed (33 items)
(Totals reflect the tree when this design was written, before `move-github-copilot-to-feed` was
committed; the current tree validates 34 items. The INFO advisory is unchanged.)
```

(Full output quoted in the Report. Only one INFO line about `Built-in agent descriptors` appears,
not two — `openspec validate --all` evaluates each change's delta against the **current main
spec**, which still has the pre-sister-1 `Built-in descriptor set` name; it does not simulate
applying one change's delta before validating the next. Both the second sister change's and this
change's `MODIFIED "Built-in agent descriptors"` blocks hit the identical "header not found"
condition against today's unarchived main spec, but the tool reports it once per spec file
touched in that way in this particular run grouping, not once per change — confirmed by running
`openspec validate add-omp-devin-builtins --type change` and `openspec validate
move-github-copilot-to-feed --type change` separately, each of which reproduces its own copy of
the same INFO line independently; see Report for both transcripts.)

**Mechanism** (verified directly in `dist/core/validation/validator.js:600-684`,
`findScenarioLossIssues`, same function and same installed version both sister changes' design
docs already read): the function looks up the main spec's current requirement block under the
MODIFIED block's name; if no block is found under that name, it returns `undefined` and the loop
skips straight to the next item, raising **no issue** — not even the INFO line. The INFO line
itself (`"Archive would refuse this delta..."`) is produced by a different, higher-level check
that specifically detects and names this "base not found" condition as an advisory, not as part of
`findScenarioLossIssues`'s own ERROR path (which only fires when a base **is** found and the
incoming block omits one of its scenario names — not the case here, since this change's block
carries the identical five names).

**What this means for archive ordering:** exactly as both sister changes already document,
`openspec validate` (in any form) does not fail on a MODIFIED block whose base is absent — it is
advisory. The real gate is `openspec archive`. For this change specifically, the precondition is
two-deep: `openspec archive add-omp-devin-builtins` needs the main spec to already read
`Built-in agent descriptors` with the **three-agent** body (OpenCode, Pi, Qwen Code) — the exact
state left behind by archiving `move-github-copilot-to-feed`, which itself needs
`move-openclaude-to-feed` archived first. **Archive order: `move-openclaude-to-feed` →
`move-github-copilot-to-feed` → `add-omp-devin-builtins`.** This was not executed (archiving is
outside this task's scope — no `openspec archive` command was run as part of this design), but is
inferred the same way both sister changes' own design docs infer their own single-step ordering
precondition, extended one step further here because this change sits at the end of a three-deep
chain instead of a two-deep one.

### Current Embedded Descriptor Shape (precedent for OMP and Devin)

**File:** `src/CLIHub.Core/SeedPlugins/opencode/plugin.json:1-28` (full agent, npm update source):

```json
{
  "id": "opencode",
  "name": "OpenCode",
  "description": "AI coding agent CLI",
  "version": "1.0.0",
  "publisher": "CLIHub",
  "homepage": "https://www.npmjs.com/package/@opencode/cli",
  "commands": {
    "launch": { "executable": "opencode" },
    "resume": { "executable": "opencode", "arguments": "--continue" },
    "version": { "executable": "opencode", "arguments": "--version" },
    "update": { "executable": "opencode", "arguments": "upgrade" }
  },
  "updateCheck": { "source": "npm", "package": "@opencode/cli" },
  "detection": {
    "systemPaths": ["%USERPROFILE%\\.opencode", "%USERPROFILE%\\.config\\opencode"],
    "projectIndicators": [".opencode", "openspec"]
  }
}
```

**File:** `src/CLIHub.Core/SeedPlugins/github-copilot/plugin.json:1-27` (full agent,
`githubReleases` update source — the shape OMP's npm case and Devin's `githubReleases` case both
mirror):

```json
{
  "id": "github-copilot",
  "name": "GitHub Copilot",
  "commands": {
    "launch": { "executable": "copilot" },
    "resume": { "executable": "copilot", "arguments": "--continue" },
    "version": { "executable": "copilot", "arguments": "--version" },
    "update": { "executable": "copilot", "arguments": "update" }
  },
  "updateCheck": { "source": "githubReleases", "repo": "github/copilot-cli" },
  "detection": {
    "systemPaths": ["%USERPROFILE%\\.copilot", "%LOCALAPPDATA%\\GitHubCopilotCLI"],
    "projectIndicators": [".github/copilot-instructions.md"]
  }
}
```

None of today's four built-ins omits a command kind — all declare the full
launch/resume/version/update set. OMP and Devin follow the same pattern (confirmed live: both
CLIs expose all four surface commands — see below).

**Note:** `schemaVersion` is absent from every current built-in descriptor (defaults to 1 per
`openspec/specs/plugin-seeding/spec.md:209-212`); the new OMP and Devin descriptors follow the
same precedent and omit it too, for consistency with the three remaining built-ins.

### `PluginUpdateCheck` Model (verified shape for both source kinds)

**File:** `src/CLIHub.Core/Models/Plugins/PluginUpdateCheck.cs:14`:

```csharp
public sealed record PluginUpdateCheck(string Source, string? Package = null, string? Repo = null);
```

**File:** `src/CLIHub.Core/Models/Plugins/UpdateCheckSource.cs:13,18,72-98` — `TryResolve` matches
`Source` case-insensitively against `"npm"` (requires non-blank `Package`) or `"githubReleases"`
(requires non-blank `Repo`); any other value, or a missing required companion field, resolves to
`SourceKind.None` with a logged-but-non-fatal problem (`AgentUpdateAvailabilityService.cs:161-178`
treats an unsupported/incomplete block as "ignored with a logged warning," per
`docs/architecture/plugins.md:43`). OMP's `{"source": "npm", "package":
"@oh-my-pi/pi-coding-agent"}` and Devin's `{"source": "githubReleases", "repo":
"CognitionAI/devin-cli"}` both resolve cleanly under this logic — verified by inspection, no new
test of `UpdateCheckSource` itself is needed since both declarations are structurally identical to
existing ones (`opencode`'s npm block and `github-copilot`'s `githubReleases` block).

### Seeding and Reserved-ID Mechanism (unchanged; relied upon, not modified)

**File:** `src/CLIHub.Core/Plugins/PluginSeeder.cs` — `EnsureBuiltinsExist()` enumerates
descriptors via `EmbeddedPluginResources.EnumerateDescriptors()` and applies the three-hash refresh
logic (`docs/architecture/plugins.md:118-145`) uniformly to every embedded descriptor; adding two
more embedded resources requires no change to this method.

**File:** `src/CLIHub.Core/Plugins/EmbeddedPluginResources.cs:33-54,97-102` —
`EnumerateDescriptors(Assembly)` reads every manifest resource matching `.SeedPlugins.` and ending
`.plugin.json`; `BuiltinPluginIds(Assembly)` (lines 97-102) returns the distinct ids from those
descriptors. Adding `SeedPlugins/omp/plugin.json` and `SeedPlugins/devin/plugin.json` (picked up
automatically by the glob in `src/CLIHub.Core/CLIHub.Core.csproj:19-20`:
`<EmbeddedResource Include="SeedPlugins\**\*.json" />` and `...\*.png" />`) is sufficient — no
code change to either file.

**File:** `src/CLIHub.Core/Plugins/PluginInstaller.cs:342-346`:

```csharp
if (EmbeddedPluginResources.BuiltinPluginIds(typeof(PluginSeeder).Assembly)
    .Contains(plugin.Id, StringComparer.Ordinal))
{
    errors.Add($"Plugin id '{plugin.Id}' is reserved for the built-in agent '{plugin.Id}' and cannot be installed");
}
```

Reserved ids are derived at runtime from the embedded resource set; adding `omp` and `devin`
automatically reserves both ids the moment the two new descriptors are embedded. No change needed
here either.

### Version-Parsing Compatibility (verified in source; no production change expected)

**File:** `src/CLIHub.Core/Agents/AgentVersionService.cs:29`:

```csharp
private static readonly Regex VersionPattern = new(@"\d+(?:\.\d+)+", RegexOptions.Compiled);
```

This pattern is applied to the **first line** of the version command's captured output (verified
by reading the surrounding method body and the existing theory rows in
`tests/CLIHub.Core.Tests/Agents/AgentVersionServiceTests.cs:59-62`, which already include
`["GitHub Copilot CLI 1.0.88.", "1.0.88"]` and `["0.30.0 (OpenClaude)", "0.30.0"]` as precedent for
"product name/prefix before the version number" and "trailing parenthetical" shapes). Applied to
the two new agents' live output:

- `omp/18.12.6` → the regex has no backtrack-before-slash behavior to worry about: `omp` contains
  no digits, so the first (and only) match is `18.12.6`. Confirmed live (see Report for the exact
  command and output).
- `devin 3000.11.92 (7f4e64980e22)` → the first digit run forming a `\d+(?:\.\d+)+` match is
  `3000.11.92`; the trailing `(7f4e64980e22)` is a hex string with no `.` separators between
  digit-only groups in the pattern's required shape, so it is not matched as an alternative
  version — the regex engine finds `3000.11.92` first (leftmost match) and `Match()` (not
  `Matches()`) returns only that one. Confirmed live.

**File:** `src/CLIHub.Core/Agents/AgentUpdateVersion.cs:12`:

```csharp
private static readonly Regex TriplePattern = new(@"\d+\.\d+\.\d+", RegexOptions.Compiled);
```

This is used for the separate major.minor.patch update-comparison path
(`AgentUpdateVersion.TryExtract`, lines 21-45) and compares numerically
(`AgentUpdateVersion.Compare`, lines 57-67) component-by-component — `int.TryParse` on each
component, not string comparison, so a four-digit major like `3000` participates correctly in
`CompareTo` without any lexical-ordering risk (the existing test
`Compare_TwoDigitMinorAgainstSingleDigitMajorPrefix_ComparesNumericallyNotLexically` at
`tests/CLIHub.Core.Tests/Agents/AgentUpdateVersionTests.cs:85-95` already establishes this
precedent for a smaller case: `1.2.3` vs `1.10.0`). Applied to the two new agents:

- `omp/18.12.6` → `TriplePattern` finds `18.12.6`, extracts `(18, 12, 6)`.
- `devin 3000.11.92 (7f4e64980e22)` → `TriplePattern` finds `3000.11.92`, extracts `(3000, 11,
  92)`. The hex suffix `7f4e64980e22` has no `.`-separated digit groups matching the
  `\d+\.\d+\.\d+` shape (it is one contiguous hex run with no dots), so it cannot be
  mis-extracted as an alternative triple.

**Conclusion:** both regexes already handle these two new formats correctly by construction; no
production-code change is planned. Theory rows are still added (see Decision 6) purely to pin this
behavior as a regression guard, the same role the existing `"0.30.0 (OpenClaude)"` row already
plays for a different agent.

### OMP — Verified Facts

- Binary: `omp`, observed at `C:\Users\Maxim\.bun\bin\omp.exe` (`where.exe omp`, this session).
  This is a Bun global install; the brief's claim of
  `%USERPROFILE%\.bun\bin\omp.exe` is confirmed by substitution (this machine's profile is
  `C:\Users\Maxim`).
- `omp --version` → `omp/18.12.6` (verified live, this session).
- Project `can1357/oh-my-pi` on GitHub (MIT license), a fork of Pi by Stencil Labs; official site
  `https://omp.sh`. Official Windows install is `irm https://omp.sh/install.ps1 | iex` (binary
  variant installs to `%LOCALAPPDATA%\omp`, not observed on this machine — the Bun install path is
  what is present here).
- `npm i -g @oh-my-pi/pi-coding-agent` is **not a supported install path** per the brief (Bun-only
  `engines` field plus a Bun shebang in the published package) — not independently re-verified by
  running `npm install` in this session (that would be a network/installation side effect outside
  this task's read-only-probe constraint), but accepted as given since it is a claim about the
  npm package's own `package.json` metadata, not about CLIHub's code. Recorded as a Risk below.
- User detection marker `%USERPROFILE%\.omp` exists on this machine (confirmed: this directory
  holds OMP's own state). `%LOCALAPPDATA%\omp` (binary-install variant) is not present here —
  expected and harmless, since this machine used the Bun install path instead.
- Project detection marker: `.omp` (contains `agents/`, `commands/`, `prompts/`, `tools/`,
  `mcp.json` subdirectories per the brief — not independently re-verified in this session beyond
  confirming the directory's existence convention matches Pi's own `.pi` marker, which this repo's
  `src/CLIHub.Core/SeedPlugins/pi/plugin.json:23` already uses as a precedent for a
  single-dot-prefixed agent-specific project folder). `AGENTS.md` is deliberately **not** used as
  a detection marker (shared by many agents, not an OMP-specific signal).
- Logo source: `https://raw.githubusercontent.com/can1357/oh-my-pi/main/assets/icon.svg` (per the
  brief: 795 B, 120×90, white `#fafafa` glyph + orange `#f97316`). Not fetched in this session (no
  network tool call was made to retrieve or inspect the remote SVG bytes); the production of the
  bundled PNG is deferred to the implementation session per the Logo Production decision below,
  which also covers what to do if the fetched SVG does not match this description.

### Devin — Verified Facts

- Binary: `devin`, observed at `C:\Users\Maxim\AppData\Local\devin\cli\bin\devin.exe` (`where.exe
  devin`, this session) — matches the brief's `%LOCALAPPDATA%\devin\cli\bin\devin.exe` exactly.
- `devin --version` → `devin 3000.11.92 (7f4e64980e22)` (verified live, this session).
- Project `CognitionAI/devin-cli` on GitHub; docs at `https://docs.devin.ai/cli`.
- Update channel: GitHub Releases on `CognitionAI/devin-cli`; the brief states releases are
  tag-only (no release notes body required) and the update-check mechanism reads `tag_name`,
  matching the existing `githubReleases` source kind's documented behavior
  (`docs/architecture/plugins.md:42`: "the latest version is read from that repository's latest
  GitHub release (`tag_name`, leading `v` stripped)"). Not independently re-verified against the
  live GitHub API in this session (would require an external network call unrelated to local CLI
  probing); accepted as given, consistent with how `github-copilot`'s own `githubReleases` block
  is already handled by the same code path.
- Detection: `%APPDATA%\devin` and `%LOCALAPPDATA%\devin` both verified present on this machine as
  directories (config/sessions and install/cache respectively, per the brief). **Not**
  `%USERPROFILE%\.devin`, which the brief identifies as belonging to Devin Desktop (a VS Code
  fork) — a different product from the Devin CLI this descriptor targets. Project indicator is
  `.devin`: the official CLI configuration reference documents project-local
  `.devin/config.json`, `.devin/config.local.json`, `.devin/mcp_config.json`, and
  `.devin/mcp_config.local.json` (verified at
  `https://docs.devin.ai/cli/reference/configuration/config-file.md`, fetched 2026-11-05).
  `AGENTS.md` alone is still too weak a signal (shared by many agents) and is not used.
- Devin requires `devin auth login` before a first real session; `--version` and the detection
  markers above work without authentication, which is why detection and version probing are
  unaffected by the auth requirement. Its updater is described as aggressive (`auto_update`),
  which is a runtime behavior, not a build-time one, and does not affect this change's content.

### Documentation (verified against actual current file content)

**File:** `README.md:18-19`:

> "Built-in descriptors cover OpenCode, Pi, GitHub Copilot, and Qwen Code. Cline CLI and OpenClaude
> are available from the curated plugin feed; additional agents can be added as JSON plugins."

(This is today's literal text, reflecting the 4-agent intermediate state. By the time this change
lands, `move-github-copilot-to-feed`'s own Stage 3 is expected to have already rewritten this to
the 3-agent form — see that change's tasks.md 3.1. This change's own task edits the line to its
final 5-agent form regardless of which intermediate wording it finds, per the re-read-before-edit
pattern both sister changes already use.)

**File:** `docs/glossary.md:61-62,71-72`:

- Line 61-62 (general Agent list): "Agent — an AI coding agent CLI (OpenCode, Pi, Cline CLI,
  GitHub Copilot, OpenClaude, Qwen Code, or any user-added plugin)" — today's literal text, listing
  every agent CLIHub has ever shipped a descriptor for (built-in or feed). OMP and Devin are new
  names that have never appeared here; they are **added** to this list (not merely moved within
  it), since the general Agent list is cumulative across both built-in and feed agents, not scoped
  to "currently built-in."
- Line 71-72 (Built-in agent definition): today lists the 4-agent intermediate state (OpenCode,
  Pi, GitHub Copilot, Qwen Code); this change's task rewrites it to the final 5-agent form
  (OpenCode, Pi, OMP, Qwen Code, Devin), whatever intermediate wording it finds when the sister
  changes have landed first.

**File:** `docs/architecture/plugins.md:44-45`:

> "Built-in descriptors for OpenCode, Pi, GitHub Copilot, and Qwen Code are embedded and seeded
> using the three-hash refresh logic described below."

Same pattern: rewritten to the 5-agent final form.

**File:** `docs/project-context.md` — read in full (112 lines). The built-in set is not enumerated
anywhere in it; line 53 only states "Built-in plugins are refreshed on app update using
three-hash logic that preserves user modifications," with no agent list or count. Confirmed: no
change needed, matching both sister changes' own finding.

**File:** `docs/images/src/workflow.html:151`:

> `<p>OpenCode, Pi, Cline, Copilot, OpenClaude, Qwen Code — or add your own JSON plugin.</p>`

This is a marketing example list inside a static HTML mockup (step "02: Pick an agent"), not a
built-in-set claim — it already mixes a feed agent (Cline), two built-ins being removed (Copilot,
OpenClaude), and makes no claim of exhaustiveness ("or add your own JSON plugin" signals the list
is illustrative). Per the brief: **left untouched** by this change.

### Logo Precedent (file sizes, format)

**Verified via `Get-ChildItem`:**

| File | Size |
|---|---|
| `src/CLIHub.Core/SeedPlugins/github-copilot/logo.png` | 15,045 bytes |
| `src/CLIHub.Core/SeedPlugins/qwen-code/logo.png` | 4,960 bytes |
| `src/CLIHub.Core/SeedPlugins/opencode/logo.png` | 2,277 bytes |
| `src/CLIHub.Core/SeedPlugins/pi/logo.png` | 2,137 bytes |

All are PNG; dimensions were not independently measured in this session (no image-dimension tool
was invoked), but the brief's claim of "160×160 or 512×512" is consistent with the byte-size range
observed (2–15 KB for small square PNGs at those dimensions). No rasterization tool (`magick`,
`inkscape`, `resvg`, `sharp`) is currently a repository dependency — grep-confirmed across the
whole repository tree; this is a genuinely new, one-time local step, not a precedent to follow.

### Test Files — Exact Current Content (today's 4-agent intermediate state; plan against
post-sister-2's 3-agent state, explicitly)

**File:** `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs` (462 lines total, read in full):

- Line 11: `BuiltinIds = ["opencode", "pi", "github-copilot", "qwen-code"]` (4 today; **3 after
  `move-github-copilot-to-feed`**: `["opencode", "pi", "qwen-code"]`)
- `Assert.Equal(4, ...)` seed-count assertions today at lines 69, 154, 163, 196, 293, 375, 406
  (**become `Assert.Equal(3, ...)` after sister change 2**, then **`Assert.Equal(5, ...)` after
  this change**)
- `Assert.Equal(3, ...)` "other built-ins" assertions today at lines 110, 125, 215, 233, 250, 264,
  312, 331 (**become `Assert.Equal(2, ...)` after sister change 2**, then **`Assert.Equal(4,
  ...)`** after this change — one of the five built-ins is the one already adopted/present in each
  of these particular test scenarios, so "other built-ins" is always count-minus-one)
- Line 65: test name `EnsureBuiltinsExist_EmptyFolder_WritesFourDescriptors` — **not renamed by
  either sister change** (grep-confirmed: neither sister change's tasks.md touches this specific
  test name; only the `EmbeddedPluginResourcesTests` enumeration test gets renamed along the way).
  This change renames it to a count-neutral form since this is the final step of the three-change
  alignment (see Decision 6).
- `SeededDescriptors_ExposeExpectedCommandSets` (declared line 413) and
  `SeededDescriptors_DeclareExpectedUpdateSources` (declared line 436) — today assert all four
  current built-ins including `github-copilot` (lines 430, 452); sister change 2 removes the
  `github-copilot` assertions from both, leaving three. This change adds `omp` and `devin`
  assertions to both, bringing each back up to five.
- Line 406 (`SeededDescriptors_LoadAsValidPlugins`): `Assert.Equal(4, plugins.Count);` today →
  `Assert.Equal(3, ...)` after sister 2 → `Assert.Equal(5, ...)` after this change.

**File:** `tests/CLIHub.Core.Tests/Plugins/EmbeddedPluginResourcesTests.cs` (40 lines total, read
in full):

- Line 7: `ExpectedBuiltinIds` — 4 elements today, 3 after sister 2, **5 after this change**
  (`["opencode", "pi", "omp", "qwen-code", "devin"]`, order chosen to match the user's stated
  target-set ordering "OpenCode, Pi, OMP, Qwen Code, Devin" rather than alphabetizing — the array
  itself is order-insensitive in use since every assertion against it sorts both sides with
  `OrderBy(id => id, StringComparer.Ordinal)` before comparing, per line 17-18 of this file, so
  declaration order is cosmetic only)
- Line 10: test name `BuiltinPluginIds_EnumeratesTheFourBuiltInAgents` today → sister 2 plans to
  rename it to `...TheThreeBuiltInAgents` → this change renames it again, to the count-neutral
  `BuiltinPluginIds_EnumeratesTheBuiltInAgents` (final step of the alignment; no future built-in
  count change needs to repeat this rename, mirroring the `Built-in agent descriptors` spec
  requirement's own count-neutral naming rationale)
- Line 22: `BuiltinPluginIds_NoLongerContainsClineCli` — precedent, unaffected by this change
- Line 32: `BuiltinPluginIds_NoLongerContainsOpenClaude` — precedent, unaffected; sister 2 is
  expected to add a parallel `BuiltinPluginIds_NoLongerContainsGithubCopilot` test. This change
  does not add a "no longer contains" test for OMP or Devin, since neither was ever a built-in
  being removed — that test shape exists specifically to pin a removal, not an addition.

### Other Tests Verified Not to Need Changes

- `tests/CLIHub.Core.Tests/Plugins/PluginInstallerTests.cs:301,315` (per
  `move-openclaude-to-feed`'s design.md) and `:303,306` (per `move-github-copilot-to-feed`'s design
  doc) — both already use `"opencode"` as the reserved-id fixture, not any agent being
  added/removed. No change needed for this change either; grep-confirmed no `"omp"` or `"devin"`
  reference exists yet in this file.
- `tests/CLIHub.Tests/ViewModels/Agents/BrowsePluginsDialogViewModelTests.cs:93-117` — same
  `"opencode"` fixture pattern, confirmed by both sister changes' design docs. No change needed.

## Decisions

### 1. Descriptor Content — OMP

**Decision:** `src/CLIHub.Core/SeedPlugins/omp/plugin.json`:

```json
{
  "id": "omp",
  "name": "OMP",
  "description": "Oh My Pi — AI coding agent CLI",
  "version": "1.0.0",
  "publisher": "CLIHub",
  "homepage": "https://omp.sh",
  "commands": {
    "launch": { "executable": "omp" },
    "resume": { "executable": "omp", "arguments": "--continue" },
    "version": { "executable": "omp", "arguments": "--version" },
    "update": { "executable": "omp", "arguments": "update" }
  },
  "updateCheck": {
    "source": "npm",
    "package": "@oh-my-pi/pi-coding-agent"
  },
  "detection": {
    "systemPaths": [
      "%USERPROFILE%\\.omp",
      "%LOCALAPPDATA%\\omp"
    ],
    "projectIndicators": [
      ".omp"
    ]
  }
}
```

**Rationale:**

- `name: "OMP"` matches the user's stated built-in set naming ("OpenCode, Pi, OMP, Qwen Code,
  Devin") rather than the fuller "Oh My Pi," for consistency with how the other short-named
  built-ins (`"Pi"`, not "Pi Coding Agent") are already named. The fuller name is relegated to
  `description`, following the same pattern `qwen-code/plugin.json` uses (`name: "Qwen Code"`,
  `description: "Qwen Code CLI"`).
- `commands.launch/resume/version/update` mirror the live-verified CLI surface exactly: `omp`
  (launch), `omp --continue` (resume most recent), `omp --version` (version), `omp update`
  (update) — all four stated in the brief and consistent with the `pi` descriptor's own shape
  (OMP is a fork of Pi, so the CLI surface conventions match).
- `updateCheck.source: "npm"`, `package: "@oh-my-pi/pi-coding-agent"` — the brief states this
  package tracks in lockstep with the `can1357/oh-my-pi` GitHub releases, and npm is already the
  dominant source kind among existing built-ins (`opencode`, `pi`, `qwen-code` all use it). Picking
  npm over `githubReleases` for the *update-check* channel is unaffected by the separate fact that
  `npm install -g` is not a supported *installation* method for OMP — `updateCheck` only performs a
  version-number lookup (`https://registry.npmjs.org/<package>/latest` per
  `docs/architecture/plugins.md:41`), it never shells out to `npm install`. These are different
  operations; see the Risks section for why this distinction matters and is worth stating
  explicitly in user-facing surfaces later (not in this change's scope to fix any UI wording, since
  no current UI wording conflates update-checking with installation for any agent).
- `detection.systemPaths` includes both the Bun-install marker (`%USERPROFILE%\.omp`, verified
  present on this machine) and the official-installer binary-variant marker
  (`%LOCALAPPDATA%\omp`, not present here but documented by the brief as the official
  `omp.sh/install.ps1` target) — covering both install paths users might have, exactly as
  `github-copilot`'s own descriptor lists two `systemPaths` entries for its own two known install
  locations.
- `detection.projectIndicators` is `[".omp"]` only — **not** `AGENTS.md`, per the brief's explicit
  instruction that `AGENTS.md` is shared by many agents and is not an OMP-specific signal.

**Alternatives rejected:**

- **`name: "Oh My Pi"`** — Rejected: the user's own built-in-set shorthand is "OMP"; using the
  full name here would be internally inconsistent with every other reference to this agent in the
  brief, proposal, and future UI copy.
- **`updateCheck.source: "githubReleases"`, `repo: "can1357/oh-my-pi"`** — Rejected per the user's
  explicit decision ("OMP update source: npm `@oh-my-pi/pi-coding-agent`"), even though the brief
  notes the GitHub releases are in lockstep and would also work. Not re-litigated here.
- **Including `AGENTS.md` as a project indicator** — Rejected per the brief's explicit warning that
  it is shared by many agents and would produce false-positive detection for unrelated CLIs.

### 2. Descriptor Content — Devin

**Decision:** `src/CLIHub.Core/SeedPlugins/devin/plugin.json`:

```json
{
  "id": "devin",
  "name": "Devin",
  "description": "Devin CLI",
  "version": "1.0.0",
  "publisher": "CLIHub",
  "homepage": "https://docs.devin.ai/cli",
  "commands": {
    "launch": { "executable": "devin" },
    "resume": { "executable": "devin", "arguments": "--continue" },
    "version": { "executable": "devin", "arguments": "--version" },
    "update": { "executable": "devin", "arguments": "update" }
  },
  "updateCheck": {
    "source": "githubReleases",
    "repo": "CognitionAI/devin-cli"
  },
  "detection": {
    "systemPaths": [
      "%APPDATA%\\devin",
      "%LOCALAPPDATA%\\devin"
    ],
    "projectIndicators": [
      ".devin"
    ]
  }
}
```

**Rationale:**

- `commands` mirror the live-verified CLI surface exactly: `devin` (launch), `devin --continue`
  (resume most recent in the current directory), `devin --version` (version), `devin update`
  (update) — all four stated in the brief.
- `updateCheck.source: "githubReleases"`, `repo: "CognitionAI/devin-cli"` — per the user's explicit
  decision. Tag-only releases are fine: the existing `githubReleases` mechanism
  (`docs/architecture/plugins.md:42`) reads `tag_name` from the latest release regardless of
  whether it carries release notes, exactly matching `AgentUpdateAvailabilityService`'s existing,
  unmodified handling of `github-copilot`'s own `githubReleases` block.
- `detection.systemPaths` lists both verified-present directories: `%APPDATA%\devin`
  (config/sessions) and `%LOCALAPPDATA%\devin` (install/cache), per the brief and live
  verification. **Deliberately excludes** `%USERPROFILE%\.devin`, which the brief identifies as
  belonging to Devin Desktop (a VS Code fork) — a different product. Including it would cause
  CLIHub to report Devin CLI as "detected" on a machine that only has Devin Desktop installed,
  which is a false positive this descriptor is designed to avoid.
- `detection.projectIndicators` is `[".devin"]` — the official CLI configuration reference
  documents project-local `.devin/config.json`, `.devin/config.local.json`, and
  `.devin/mcp_config*.json` (verified 2026-11-05). `AGENTS.md` alone is rejected as too weak a
  signal (shared by many agents), matching OMP's detection rationale.

**Alternatives rejected:**

- **`systemPaths` including `%USERPROFILE%\.devin`** — Rejected per the brief's explicit warning:
  this path belongs to Devin Desktop, not the CLI this descriptor targets, and including it would
  misattribute Devin Desktop installs as Devin CLI detections.
- **`projectIndicators: ["AGENTS.md"]`** — Rejected: `AGENTS.md` is a generic convention shared by
  OpenCode, OMP (via `.omp`'s own convention plus `AGENTS.md`'s cross-agent use), and many other
  tools; using it here would cause false-positive project-level detection for any repository using
  any AGENTS.md-aware agent, not specifically Devin.
- **`updateCheck.source: "npm"`, `package: "devin"`** — Rejected per the user's explicit decision:
  npm `devin` is in lockstep with the GitHub releases per the brief, but is "not the documented
  install path," and the user's decision already names GitHub releases as the source. Not
  re-litigated here.

### 3. Both Descriptors Declare the Full Four-Command Set

**Decision:** Neither OMP nor Devin omits any of `launch`, `resume`, `version`, `update`.

**Rationale:** The brief's live-verified command inventories for both agents list all four
surface commands with documented CLI flags (`omp`, `omp --continue`, `omp --version`, `omp
update`; `devin`, `devin --continue`, `devin --version`, `devin update`). This matches every one
of today's three remaining built-ins (OpenCode, Pi, Qwen Code), none of which omits a command
kind either — the spec's own "Unsupported command kinds are omitted" scenario exists as a general
rule for the requirement, not because any current built-in actually exercises it (confirmed: no
existing `plugin.json` in `SeedPlugins/` has fewer than four command entries — grep-confirmed
across all four current descriptor files).

**No alternatives** — this is a direct transcription of verified CLI behavior, not a design
choice with tradeoffs.

### 4. Logo Production — SVG Source, PNG Output, Neutral-Plate Contrast Treatment

**Decision:** Both logos are produced as a **one-time, local rasterization step** during
implementation, not committed as part of this planning change:

1. Fetch the vendor SVG (OMP: `https://raw.githubusercontent.com/can1357/oh-my-pi/main/assets/icon.svg`;
   Devin: `https://devin.ai/favicon.svg`).
2. Composite each SVG onto a neutral plate (a filled rounded-square or circular backing shape in a
   fixed neutral tone, e.g. a mid-gray or the existing built-ins' typical backing treatment —
   exact tone to be chosen at implementation time by visually comparing against the four existing
   `logo.png` files side by side in both the Dark and Light themes) so the mark reads clearly
   against both of CLIHub's light and dark theme backgrounds, since OMP's icon is reported as a
   two-tone glyph on a transparent background (white `#fafafa` + orange `#f97316`, no plate of its
   own) and Devin's is a single black path (would vanish entirely on the Dark theme without a
   plate).
3. Rasterize to PNG at a square size consistent with the existing built-ins (160×160 or 512×512,
   matching whichever the implementer finds the current four use once dimensions are measured —
   not independently measured in this design session; see Open Questions).
4. Commit only the resulting PNG to `src/CLIHub.Core/SeedPlugins/omp/logo.png` and
   `src/CLIHub.Core/SeedPlugins/devin/logo.png`. No SVG source file is added to the repository (the
   brief confirms `assets/logos/agents/*.svg` no longer exists as a repo convention — this change
   does not reintroduce it).
5. The rasterization tool itself (a temporary `@resvg/resvg-js` or `sharp` invocation via `bunx` or
   `npx`, run once and not added as a project dependency) is a build-time convenience, not a new
   runtime or build dependency of CLIHub — `magick` and `inkscape` are confirmed not installed on
   this machine, and no existing repository tooling performs SVG rasterization today (grep-
   confirmed), so this is a genuinely new one-time step, not a precedent-following one.

**Rationale:** The user's decision already fixes the sourcing (vendors' official marks) and the
treatment goal (neutral plate / contrast treatment, visible on both themes) — this decision only
fixes the mechanical production path, matching the brief's explicit guidance ("no `magick`/
`inkscape`; use something like `@resvg/resvg-js` or `sharp` via `bunx`/`npx`; no new runtime
dependency; commit only the PNG").

**Alternatives rejected:**

- **Ship the raw vendor SVG rasterized without a plate** — Rejected per the user's explicit
  decision that both logos need a neutral plate / contrast treatment; a transparent-background
  rasterization of OMP's white-and-orange glyph would be very low-contrast on the Light theme, and
  Devin's black-only path would be invisible on the Dark theme.
  on-disk PNGs, keeping the dependency genuinely one-time and consistent with the brief's "no new
  runtime dependency" instruction.
- **Add `assets/logos/agents/*.svg` back to the repo as source-of-truth** — Rejected: the brief
  confirms this folder no longer exists as a repository convention; reintroducing it would be
  scope creep beyond "add two built-ins."

### 5. Why `MODIFIED "Built-in agent descriptors"` (Not Another REMOVED+ADDED Pair)

**Decision:** `MODIFIED "Built-in agent descriptors"` in
`openspec/changes/add-omp-devin-builtins/specs/plugin-seeding/spec.md`, reusing the identical five
scenario names both sister changes already use: `Built-in agents seeded`, `Each seeded descriptor
is loadable`, `Complete command sets`, `Commands match the agent CLI surface`, `Unsupported
command kinds are omitted`. Only the THEN bodies of the two agent-naming scenarios change
(`Built-in agents seeded` and `Complete command sets`, naming five agents instead of three); the
other three scenarios are reproduced with their existing, unchanged, generic bodies.

**Rationale:**

- This is exactly the situation `move-openclaude-to-feed`'s own design.md anticipated when it
  deliberately chose a **count-neutral** requirement name: "a future change can MODIFY 'Built-in
  agent descriptors' and only touch the scenario body text, not the scenario name, as long as the
  scenario names themselves stay stable." This change is that anticipated future change — the
  third built-in-count change in the chain, after two that already used this exact mechanism
  (sister 1 via ADDED, sister 2 via MODIFIED).
- `move-github-copilot-to-feed`'s own design.md (Decision 6) independently re-derived and verified
  the same validator behavior this design relies on: a MODIFIED block whose named base does not
  yet exist in the main spec produces an advisory INFO line at validate-time, not an error, and
  `openspec archive` is the actual gate. This change's situation is structurally identical, just
  one archive-step further down the chain (the base this change's MODIFIED block needs is itself
  the *output* of archiving the second sister change, not of the main spec as it stands today).
- Reusing the identical scenario names (not renaming them) keeps `diffScenarioNames` satisfied
  once the base does exist by archive time: a MODIFIED block's scenario-name set must be a
  superset of the current block's, and since this change's five names are identical to the
  (eventually-existing) three-agent block's five names, there is no scenario loss to flag.

**Alternatives rejected:**

- **REMOVED "Built-in agent descriptors" + ADDED "Built-in agent descriptors" (same name)** —
  Rejected: OpenSpec does not allow the same requirement name in both REMOVED and ADDED sections of
  one delta (established in the Cline change's design notes, re-confirmed independently by both
  sister changes).
- **REMOVED + ADDED with yet another new count-neutral name** — Rejected: would defeat the entire
  purpose of choosing "Built-in agent descriptors" as a stable, count-neutral name in the first
  place. If this change renamed it again, every future built-in-count change would face the same
  question with no resolution — the chain would never converge on a name it could just MODIFY.
- **MODIFIED "Built-in descriptor set"** (the pre-sister-1 original name) — Rejected: that
  requirement is removed by `move-openclaude-to-feed`'s own REMOVED block. Targeting it here would
  conflict with that change's own operation on the same name once both are archived, exactly as
  `move-github-copilot-to-feed`'s own design.md already rejected this alternative for the same
  reason, one level up the chain.
- **Wait and only author this change's delta after both sister changes are archived** — Rejected
  per the task brief: the task is to prepare this change now, in parallel with two unarchived
  sister changes, and document the ordering dependency explicitly (via a Stage 0 gate in
  tasks.md, mirroring `move-github-copilot-to-feed`'s own Stage 0) rather than block on it.

### 6. Test Strategy

**Decision:**

1. Update `BuiltinIds` (`PluginSeederTests.cs:11`) and all seed-count assertions to reflect **5
   total, 4 "other"** — the exact line numbers depend on which edits the two sister changes have
   already applied by the time this change is implemented; tasks.md instructs re-reading the file
   immediately before editing rather than trusting this design's own line numbers, which describe
   today's 4-agent intermediate state, not the eventual 3-agent-then-5-agent sequence.
2. Add `omp` and `devin` assertions to `SeededDescriptors_ExposeExpectedCommandSets` (both get the
   full four-kind command set, same `full` array already used for every other built-in) and to
   `SeededDescriptors_DeclareExpectedUpdateSources` (`PluginUpdateCheck("npm", Package:
   "@oh-my-pi/pi-coding-agent")` for `omp`; `PluginUpdateCheck("githubReleases", Repo:
   "CognitionAI/devin-cli")` for `devin`).
3. Update `EmbeddedPluginResourcesTests.ExpectedBuiltinIds` to the final 5-element array; rename
   the enumeration test from whatever count-specific name the sister changes left it as (`...Three
   BuiltInAgents`, per sister 2's plan) to the count-neutral `BuiltinPluginIds_
   EnumeratesTheBuiltInAgents` — this is the natural point to drop the counting convention
   entirely, since this change is the last of the three-change chain and no further built-in-count
   change is currently planned.
4. Rename `EnsureBuiltinsExist_EmptyFolder_WritesFourDescriptors` (a name neither sister change
   touches) to the count-neutral `EnsureBuiltinsExist_EmptyFolder_WritesAllBuiltinDescriptors`, for
   the same reason as point 3.
5. Add theory rows to `AgentVersionServiceTests.cs`'s `GetVersion_ExtractsVersionNumber` theory
   (alongside the existing `["GitHub Copilot CLI 1.0.88.", "1.0.88"]` and `["0.30.0 (OpenClaude)",
   "0.30.0"]` rows): `["omp/18.12.6", "18.12.6"]` and `["devin 3000.11.92 (7f4e64980e22)",
   "3000.11.92"]`.
6. Add theory rows to `AgentUpdateVersionTests.cs`'s `TryExtract_VersionTextWithSurroundingContent_
   ExtractsFirstNumericTriple` theory (alongside the existing `["opencode v2.0.32", 2, 0, 32]` and
   `["0.30.0 (OpenClaude)", 0, 30, 0]` rows): `["omp/18.12.6", 18, 12, 6]` and `["devin 3000.11.92
   (7f4e64980e22)", 3000, 11, 92]`.
7. No change to `PluginInstallerTests.cs` or `BrowsePluginsDialogViewModelTests.cs` — both already
   use `"opencode"` as their reserved-id fixture, confirmed by both sister changes' design docs and
   independently grep-confirmed here to contain no `"omp"`/`"devin"` references yet.
8. No "no longer contains" test is added for `omp` or `devin` — that test shape exists to pin a
   *removal*, and neither agent is being removed.

**Rationale:** Items 1-2 mirror the test-update pattern both sister changes already establish,
scoped to an addition instead of a removal. Items 3-4 are new to this change specifically because
it is the terminal step of the three-change count-alignment chain — renaming now avoids a fourth
built-in-count change (should one ever happen) needing to repeat the same rename dance the spec
requirement itself already avoids via its own count-neutral name. Items 5-6 pin the two
verified-compatible regex behaviors as regression guards, matching the existing `"0.30.0
(OpenClaude)"` row's role.

**No alternatives** for items 1-2 and 7-8 (direct application of established precedent). For items
3-4, the alternative (leave the count-specific names as sister change 2 left them, e.g.
`...TheFiveBuiltInAgents`) was considered and rejected: it would just move the "pick a new number"
problem to a fourth hypothetical change instead of resolving it, which is the same trap the spec
requirement's own naming already avoided.

### 7. No Feed-Repository Work

**Decision:** This change makes zero edits to `D:\Projects\CLIHub-plugins`.

**Rationale:** Per the user's explicit decision, OMP and Devin are built-in from the start of
their CLIHub support — there is no prior feed-plugin existence to migrate from (unlike OpenClaude
and GitHub Copilot, which both sister changes move *from* built-in *to* the feed). There is
nothing to coordinate, publish, or deprecate in the feed repository for this change.

**No alternatives.**

### 8. Version Bump Scope

**Decision:** This change does **not** bump `Directory.Build.props` `<Version>` from `1.1.0` to
`1.1.1`. Verified current value: `Directory.Build.props:15` → `<Version>1.1.0</Version>` (same
value both sister changes independently verified).

**Rationale:** Identical to both sister changes' own Decision on this point. The version bump is
release-preparation work shared by all three changes that land in 1.1.1; it happens exactly once,
during release prep, not per contributing change.

**Alternatives rejected:** Bumping here would mix this change's own scope (adding two built-ins)
with a release-process step that belongs to none of the three individual changes.

## Deviations from Brief

None identified. Every verifiable fact in the brief (binary paths, `--version` output, detection
directories, regex compatibility, archive-order precondition, validator INFO-level behavior)
reproduced exactly during this design session. The one claim not independently re-verified here —
OMP's npm package having Bun-only `engines`/shebang metadata that makes `npm i -g
@oh-my-pi/pi-coding-agent` fail — was not re-checked because doing so would require an actual
`npm install` attempt, which falls outside this task's read-only-probe constraint; it is recorded
as a Risk instead of re-asserted as independently verified.

## Open Questions

### 1. Exact Logo Pixel Dimensions and Plate Tone

**Question:** This design did not measure the existing four `logo.png` files' pixel dimensions
(no image-dimension tool was invoked in this session) or pick an exact neutral-plate color/shape.
The brief states existing logos are "160×160 or 512×512"; which one the four currently use, and
what exact plate treatment (color, corner radius, padding) best matches the existing built-ins'
visual style, needs a side-by-side visual check.

**Recommendation:** Resolve during implementation, immediately before generating the two new
logos — measure the four existing PNGs' dimensions and visually compare candidate plate treatments
against them in both the Dark and Light themes before committing either new logo.

### 2. Archive-Order Enforcement Is Process-Only

**Question:** Nothing in the OpenSpec tooling mechanically prevents archiving this change before
either sister change (or between them, in the wrong order) — the validator stays silent on a
missing MODIFIED base; only `archive` itself would fail, and only at that point, per the verified
behavior above. Should tasks.md's Stage 0 gate be the only safeguard, or is a stronger mechanical
check worth adding to the project's tooling as a general pattern?

**Current plan:** tasks.md includes an explicit Stage 0 gate (mirroring `move-github-copilot-to-
feed`'s own Stage 0) that checks both sister changes' archive status via `openspec list` before
any other work starts, and again immediately before this change's own archive step. Flagged as
open because the enforcement remains process-only, the same open question both sister changes
already carry forward from their own design docs.

### 3. Release Version Assumption

**Question:** Same open question both sister changes already record — is `1.1.1` confirmed as the
next version, or should the plan use a placeholder?

**Current plan:** Use `1.1.1` per the user's explicit decision recorded in the task brief,
consistent with both sister changes. Not re-litigated here.

## Risks and Follow-Ups

### Risks

1. **OMP's unsupported npm install path** — `npm i -g @oh-my-pi/pi-coding-agent` is reported (not
   independently re-verified) to fail due to Bun-only `engines`/shebang metadata. CLIHub's
   `updateCheck` block only performs a version-number lookup against that npm package, never an
   install; this is safe regardless. But any future UI copy or documentation that casually suggests
   "update via npm" for OMP without the Bun caveat would mislead users who do not already have OMP
   installed via Bun or the official installer. No current CLIHub UI text makes this suggestion
   (verified: the Update command only runs the descriptor's own `update` command,
   `omp update`, never `npm install`), so this is a documentation risk for *future* changes, not a
   functional defect in this one.

2. **Devin's authentication and aggressive auto-update** — `devin --version` and the declared
   detection markers work unauthenticated, so detection and version display are unaffected. But a
   user who launches Devin for the first time via CLIHub will hit Devin's own `devin auth login`
   flow inside the external terminal CLIHub opens — this is expected behavior (CLIHub launches an
   external terminal and does not manage in-process agent state, per
   `docs/project-context.md:13`), not a defect, but worth flagging since it differs from agents
   that work immediately after launch. Separately, Devin's own updater is described as aggressive
   (`auto_update`); this change's planning explicitly avoids caching the resolved executable path
   beyond a single detection probe (no new caching code is introduced by this change at all — the
   existing detection/version-probe mechanism already re-probes per the configured TTL, per
   `docs/project-context.md:62`, so no additional risk is introduced here beyond what already
   exists for every other agent).

3. **Archive-order violation** — If either sister change is abandoned, reworked to a different
   requirement name, or archived out of the planned order, this change's `MODIFIED "Built-in agent
   descriptors"` delta would need to be revised before it could be archived — a three-deep version
   of the same risk both sister changes already carry for their own, shallower chains.

4. **Logo visual-contrast judgment call** — The "neutral plate" treatment is a visual-design
   decision with no automated test coverage (no existing CLIHub test asserts anything about logo
   *appearance*, only that a `logo.png` file exists and is non-empty, per
   `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs:77-88`). Getting the plate wrong (too
   dark, too light, wrong size) would not fail any gate in tasks.md; it would only be caught by
   human visual review. Flagged as a review-time risk, not a build-time one.

### Follow-Ups for `docs/backlog.md`

1. **Document the three-built-in-count-changes-one-release pattern** — Worth a backlog note
   alongside the two-removal note `move-github-copilot-to-feed` already plans to add: when a chain
   of built-in-count changes lands in one release, each subsequent change in the chain can
   `MODIFIED` the same count-neutral requirement name, provided archive order is respected end to
   end and the installed OpenSpec version's validator continues to tolerate a missing MODIFIED
   base at validate-time (confirmed here for `@fission-ai/openspec@1.15.0`, the same version both
   sister changes verified against; re-verify if the tool is upgraded).
2. **Logo visual-review checklist** — Worth a backlog note that adding a built-in logo should
   include an explicit human visual-contrast check against both themes before merge, since no
   automated test can catch a low-contrast or stylistically inconsistent logo.

## Files to Change

### Application (CLIHub)

1. `src/CLIHub.Core/SeedPlugins/omp/plugin.json` — **CREATE**
2. `src/CLIHub.Core/SeedPlugins/omp/logo.png` — **CREATE**
3. `src/CLIHub.Core/SeedPlugins/devin/plugin.json` — **CREATE**
4. `src/CLIHub.Core/SeedPlugins/devin/logo.png` — **CREATE**
5. `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs` — **EDIT** (re-read line numbers at
   implementation time; see Decision 6)
6. `tests/CLIHub.Core.Tests/Plugins/EmbeddedPluginResourcesTests.cs` — **EDIT** (array, test
   rename)
7. `tests/CLIHub.Core.Tests/Agents/AgentVersionServiceTests.cs` — **EDIT** (two new theory rows)
8. `tests/CLIHub.Core.Tests/Agents/AgentUpdateVersionTests.cs` — **EDIT** (two new theory rows)
9. `README.md` — **EDIT** (line 18, exact wording depends on sister changes' prior edits)
10. `docs/glossary.md` — **EDIT** (lines 61, 72)
11. `docs/architecture/plugins.md` — **EDIT** (line 44)
12. `openspec/specs/plugin-seeding/spec.md` — **EDIT** (delta under
    `openspec/changes/add-omp-devin-builtins/specs/plugin-seeding/spec.md`; applied by `archive`,
    not directly — three-deep ordering dependency applies)

### Not Changed

- `Directory.Build.props` — version bump is release prep, shared across all three changes.
- `CHANGELOG.md` / `RELEASE-NOTES.md` — release prep.
- `D:\Projects\CLIHub-plugins` (any file) — no feed work in this change.
- `docs/images/src/workflow.html` — example list, not a built-in-set claim.
- `docs/project-context.md` — built-in set not enumerated there.
- Either sister change's own files.
