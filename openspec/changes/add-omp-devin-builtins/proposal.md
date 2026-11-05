# Add OMP and Devin as Built-in Agents

## Why

CLIHub 1.1.1 ships with a user-confirmed built-in set of five agents: OpenCode, Pi, OMP, Qwen
Code, and Devin. Two sister changes already shrink the embedded set from five (the pre-1.1.0
state) down to three (OpenCode, Pi, Qwen Code) by moving OpenClaude and GitHub Copilot to the
curated feed:

- `move-openclaude-to-feed` (committed `f1a014d`, not archived) — removes OpenClaude's embedded
  descriptor, replaces the main spec's `Built-in descriptor set` requirement with a count-neutral
  `Built-in agent descriptors` requirement (four agents at that point: OpenCode, Pi, GitHub
  Copilot, Qwen Code).
- `move-github-copilot-to-feed` (planned, not archived) — removes GitHub Copilot's embedded
  descriptor, `MODIFIED`s the same `Built-in agent descriptors` requirement down to three agents
  (OpenCode, Pi, Qwen Code).

This change is the third and final step: it adds **OMP** ("Oh My Pi", a Pi fork by Stencil Labs,
`can1357/oh-my-pi`) and **Devin** (Cognition's `CognitionAI/devin-cli`) as built-in descriptors,
bringing the embedded set to the target five: OpenCode, Pi, OMP, Qwen Code, Devin.

Both new agents are embedded (descriptor JSON + logo PNG), not feed plugins, matching the
treatment OpenCode, Pi, and Qwen Code already receive. OMP and Devin were chosen by the user as
part of the 1.1.1 built-in set; this change only implements the mechanical addition (descriptors,
tests, docs, spec) — it does not revisit that choice.

## What Changes

### Application (CLIHub)

1. **Add embedded resources** — Create `src/CLIHub.Core/SeedPlugins/omp/plugin.json` and
   `logo.png`, and `src/CLIHub.Core/SeedPlugins/devin/plugin.json` and `logo.png`. Both ship the
   full command set (`launch`, `resume`, `version`, `update`), matching every other current
   built-in.
   - OMP: `updateCheck` source `npm`, package `@oh-my-pi/pi-coding-agent`. Detection via
     `%USERPROFILE%\.omp` (user) and project `.omp` (not `AGENTS.md`, which is shared by many
     agents).
   - Devin: `updateCheck` source `githubReleases`, repo `CognitionAI/devin-cli`. Detection via
     `%APPDATA%\devin` and `%LOCALAPPDATA%\devin` (user; **not** `%USERPROFILE%\.devin`, which
     belongs to Devin Desktop, a different product) and project indicator `.devin` (the official
     CLI configuration reference documents project-local `.devin/config.json` and
     `.devin/mcp_config.json`; `AGENTS.md` alone is too weak a signal).
2. **Update built-in set references** — Five agents: OpenCode, Pi, OMP, Qwen Code, Devin
   - `README.md:18` user-facing built-in list
   - `docs/glossary.md:61,72` Agent and Built-in agent definitions (OMP and Devin join both lists)
   - `docs/architecture/plugins.md:44` built-in descriptor reference
   - `docs/project-context.md` — verified: no change needed, the built-in set is not enumerated
     there (see Design)
3. **Update tests** — Grow the built-in set from three (the state after `move-github-copilot-to-feed`)
   to five
   - `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs:11` `BuiltinIds` array (3 → 5 elements)
   - Seed-count assertions and the derived "other built-ins" assertions, renumbered for 5 total
   - Command-set and update-source tests gain OMP and Devin assertions (both declare the full
     four-command set)
   - `tests/CLIHub.Core.Tests/Plugins/EmbeddedPluginResourcesTests.cs:7` `ExpectedBuiltinIds` array
     (3 → 5 elements); rename the enumeration test to a count-neutral form
     (`BuiltinPluginIds_EnumeratesTheBuiltInAgents`) since this is the final step of the 1.1.1
     built-in-set alignment
   - `EnsureBuiltinsExist_EmptyFolder_WritesFourDescriptors` renamed to a count-neutral form
     (`EnsureBuiltinsExist_EmptyFolder_WritesAllBuiltinDescriptors`) for the same reason
   - Add theory rows to `AgentVersionServiceTests.cs` (`omp/18.12.6` → `18.12.6`; `devin 3000.11.92
     (7f4e64980e22)` → `3000.11.92`) and `AgentUpdateVersionTests.cs` (same two formats extracting
     a major.minor.patch triple) pinning the unusual `omp/` slash prefix and the Devin commit-hash
     suffix — no production code change is expected; see Design for the verified regex behavior
4. **Update specs** — `MODIFIED "Built-in agent descriptors"` in
   `openspec/specs/plugin-seeding/spec.md`, reusing the exact scenario names introduced by
   `move-openclaude-to-feed`'s `ADDED` block and carried through `move-github-copilot-to-feed`'s
   own `MODIFIED` block: `Built-in agents seeded`, `Each seeded descriptor is loadable`, `Complete
   command sets`, `Commands match the agent CLI surface`, `Unsupported command kinds are omitted`.
   Only the THEN bodies of the first two scenarios change (list five agents instead of three); the
   other three scenarios are reproduced with unchanged, generic bodies as `MODIFIED` requires
   reproducing the whole requirement.
   - This is valid only once both `move-openclaude-to-feed` and `move-github-copilot-to-feed` are
     archived, in that order. See Design for the exact archive-ordering precondition and the
     `openspec validate` output that shows this is advisory (INFO), not blocking, before then.
   - No changes to `openspec/specs/plugin-installation/spec.md` (the reserved-id scenario does not
     list specific ids).

### No feed-repository work

Unlike the two sister changes, this change does not touch `D:\Projects\CLIHub-plugins`. OMP and
Devin are built-in from the start; there is no removal-then-feed-republish step.

## Impact

- **New installations (CLIHub 1.1.1+)** — OMP and Devin descriptors are seeded alongside OpenCode,
  Pi, and Qwen Code on first run, with full command sets and the three-hash refresh logic already
  used by every built-in.
- **Existing installations** — Users upgrading to 1.1.1 gain two new built-in agent cards (OMP,
  Devin) the next time seeding runs (first launch after the upgrade), exactly as any other
  built-in addition behaves under the existing seeding logic. No existing plugin file is touched.
- **Reserved IDs** — `omp` and `devin` become reserved built-in ids the moment this change's
  embedded resources ship (derived automatically from `EmbeddedPluginResources.BuiltinPluginIds()`,
  per `src/CLIHub.Core/Plugins/PluginInstaller.cs:342-346`); manual `.clihub-plugin.zip` installs
  with either id are rejected from that build onward.
- **No version bump** — `Directory.Build.props` stays at `1.1.0` in this change; the bump to
  `1.1.1` is release-preparation work shared with both sister changes and happens exactly once,
  outside all three.

## Non-Goals

- **No feed-repository work** — OMP and Devin are built-in, not published to
  `trustsoft/CLIHub-plugins`. No feed PR, no `reserved-ids.json` edit.
- **No `CHANGELOG.md` / `RELEASE-NOTES.md` edits** — Release-note authorship is release-preparation
  work, out of scope here.
- **No version bump** — `Directory.Build.props` remains at `1.1.0`.
- **No changes to the removal side of the migration** — This change does not touch GitHub
  Copilot's or OpenClaude's embedded resources, tests, or spec deltas; those belong exclusively to
  the two sister changes.
- **No `devin auth login` or any authentication/update action** — Detection and version probing
  work unauthenticated; this change does not script or require Devin CLI login.
- **No change to OMP's or Devin's actual install path** — This change documents that `npm i -g
  @oh-my-pi/pi-coding-agent` is not a supported install method for OMP (Bun-only `engines` field
  plus a Bun shebang) rather than attempting to work around it. The embedded `updateCheck` still
  points at that npm package because it is the correct update-detection channel (version lookup
  only, not an install mechanism) — see Design for why these are different concerns.

## Dependencies

- **Archive ordering (hard precondition):** This change's spec delta uses `MODIFIED "Built-in
  agent descriptors"`, which requires that requirement to exist in the main spec with the
  three-agent body the second sister change leaves behind. **This change must be archived after
  both `move-openclaude-to-feed` and `move-github-copilot-to-feed` are archived, in that order.**
  See Design for the verified `openspec validate` behavior while the base is in an intermediate
  state, and for what happens if the archive order is violated.
- **Shared release:** This change, together with both sister changes, ships in CLIHub 1.1.1. None
  of the three bumps `Directory.Build.props`; that happens once, during release preparation.
