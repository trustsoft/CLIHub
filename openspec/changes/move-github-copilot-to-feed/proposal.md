# Move GitHub Copilot to Feed

## Why

GitHub Copilot is currently embedded in CLIHub as one of the built-in agents seeded on every
installation (`src/CLIHub.Core/SeedPlugins/github-copilot/plugin.json:1-27`). Moving it to the
curated plugin feed allows:

1. **Independent versioning** — GitHub Copilot descriptor updates (command changes, new
   `updateCheck` sources, detection-path fixes) can be published to the feed without waiting for a
   CLIHub application release
2. **Consistent plugin experience** — Users discover and install GitHub Copilot through the same
   Browse Plugins flow already used for Cline CLI and OpenClaude
3. **Reduced built-in surface** — The embedded set shrinks from four agents (OpenCode, Pi, GitHub
   Copilot, Qwen Code) to three (OpenCode, Pi, Qwen Code), continuing the same migration already
   applied to Cline CLI (archived `2026-11-05-move-cline-to-feed`) and in flight for OpenClaude
   (`move-openclaude-to-feed`, committed `f1a014d`, not yet archived)

This change does not remove the GitHub Copilot plugin; it changes where new installations obtain
it. Existing users keep their already-seeded GitHub Copilot plugin untouched.

## What Changes

### Application (CLIHub)

1. **Remove embedded resources** — Delete `src/CLIHub.Core/SeedPlugins/github-copilot/plugin.json`
   and `logo.png` from the assembly's embedded resources
2. **Update built-in set references** — Three agents remain: OpenCode, Pi, Qwen Code
   - `README.md:18` user-facing built-in list — add GitHub Copilot to the curated-feed sentence
   - `docs/glossary.md:61,72` Agent and Built-in agent definitions (GitHub Copilot remains in the
     general Agent list at :61; removed from the Built-in agent list at :72)
   - `docs/architecture/plugins.md:44` built-in descriptor reference
   - `docs/project-context.md` — verified: no change needed (see Design, "Verified Current State")
3. **Update tests** — Adjust assertions that expect four built-in agents to expect three
   - `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs:11` `BuiltinIds` array
   - Seed-count assertions (currently `Assert.Equal(4, ...)` → `Assert.Equal(3, ...)`, and the
     derived "other built-ins" assertions currently `Assert.Equal(3, ...)` → `Assert.Equal(2, ...)`)
   - Command-set and update-source tests drop the `github-copilot` assertions, continuing to verify
     the three remaining agents
   - `tests/CLIHub.Core.Tests/Plugins/EmbeddedPluginResourcesTests.cs:7` `ExpectedBuiltinIds` array,
     test name at line 10, and a new `BuiltinPluginIds_NoLongerContainsGithubCopilot` test
   - `tests/CLIHub.Core.Tests/Plugins/PluginInstallerTests.cs:301,306` and
     `tests/CLIHub.Tests/ViewModels/Agents/BrowsePluginsDialogViewModelTests.cs:101,116` already use
     `"opencode"` as their reserved-id fixture (not `github-copilot`) — verified, no change needed
4. **Update specs** — see Design for the exact delta shape and its archive-ordering precondition.
   In short: `MODIFIED "Built-in agent descriptors"` in
   `openspec/specs/plugin-seeding/spec.md`, keeping the sister change's scenario names
   ("Built-in agents seeded", "Each seeded descriptor is loadable", "Complete command sets",
   "Commands match the agent CLI surface", "Unsupported command kinds are omitted") and changing
   only the THEN bodies to drop GitHub Copilot

### Feed Repository (CLIHub-plugins)

1. **Publish GitHub Copilot as a feed plugin** — Copy the removed embedded descriptor and logo to
   `D:\Projects\CLIHub-plugins\plugins\github-copilot\`
   - Keep `"publisher": "CLIHub"` (curator attribution, consistent with the feed's curation role)
   - Set `minAppVersion` to `1.1.1` — **this change ships in CLIHub 1.1.1**, the same release that
     removes OpenClaude (`move-openclaude-to-feed`)
   - Retain the existing command set (`launch`, `resume`, `version`, `update`) and `updateCheck`
     (`githubReleases` / `github/copilot-cli`)
2. **Update `reserved-ids.json`** — Remove `"github-copilot"` from the array
3. **Coordinate with the OpenClaude feed PR** — Both GitHub Copilot and OpenClaude are removed from
   the built-ins in the same 1.1.1 release. See Design for whether one feed PR should carry both
   entries or two separate PRs should be opened; this proposal defers the final call to the
   feed-deployment stage, which runs after the release regardless.
4. **Package and publish** — Follow the feed's
   [CONTRIBUTING.md](https://github.com/trustsoft/CLIHub-plugins/blob/main/CONTRIBUTING.md)
   workflow: `scripts/package-plugin.ps1 github-copilot`, validate with
   `node scripts/build-index.mjs --check`, open a pull request; CI validates, packages, publishes a
   GitHub Release, and updates `index.json`

## Impact

### Migration Semantics

- **Existing users** — The already-seeded GitHub Copilot descriptor and `origin=builtin` state
  remain untouched. Seeding no longer manages it (three-hash logic will not refresh it). If the
  user removes the GitHub Copilot plugin directory, they can reinstall it from the feed.
- **New installations** — CLIHub 1.1.1 and later do not seed GitHub Copilot. Users discover and
  install it from the Browse Plugins page, where it appears as a feed plugin with
  `origin=installed`.
- **No automatic migration** — Existing `origin=builtin` GitHub Copilot entries do not auto-convert
  to `origin=installed`. The feed publishes GitHub Copilot at version `1.0.0` (matching the
  embedded version), so users with the built-in GitHub Copilot see "Current" on the feed entry's
  version indicator.

### Reserved ID Lifecycle

1. **Before the CLIHub release** — `github-copilot` is reserved (embedded resource in the current
   build + `reserved-ids.json` in the feed). Feed submissions with `id=github-copilot` are rejected
   by feed CI.
2. **CLIHub 1.1.1 ships** — The embedded resource is removed. `PluginInstaller` derives reserved ids
   from `EmbeddedPluginResources.BuiltinPluginIds()`, so `github-copilot` is no longer reserved in
   that build or later.
3. **Feed PR merged after the release** — `reserved-ids.json` is updated to remove `github-copilot`.
   The feed's GitHub Copilot entry becomes visible (installable) in Browse Plugins.

The feed's `reserved-ids.json` must not be updated before the CLIHub 1.1.1 release. If the feed
publishes GitHub Copilot before 1.1.1 ships, users on 1.1.0 will see GitHub Copilot in Browse
Plugins (the list shows entries with `minAppVersion` above the running version but disables
Install), and local/manual package installs will fail with "Plugin id 'github-copilot' is reserved
for the built-in agent 'github-copilot' and cannot be installed."

### Release Dependency

**This change ships in CLIHub 1.1.1**, the same release that removes OpenClaude
(`move-openclaude-to-feed`). The feed plugin can be published **after** CLIHub 1.1.1 is released,
not before. The `minAppVersion=1.1.1` field ensures the Install button is disabled on 1.1.0.
Publishing after the release avoids the reserved-id rejection on local/manual package installs for
users on 1.1.0.

### Feed-PR Coordination with the Sister Change

`move-openclaude-to-feed` already plans a post-release feed PR that removes `openclaude` from
`reserved-ids.json` and adds its feed entry. This change plans the same for `github-copilot`. Both
land in the feed repository after the same CLIHub 1.1.1 release. See Design, Decision "Feed PR
Coordination," for the chosen sequencing (single combined PR vs. two PRs) and its rationale.

## Non-Goals

- **No automatic removal of existing GitHub Copilot plugins** — The change does not delete user
  content. Existing `origin=builtin` GitHub Copilot entries remain.
- **No origin migration** — The change does not convert `origin=builtin` entries to
  `origin=installed`. Users who remove and reinstall GitHub Copilot will have `origin=installed` on
  the new copy.
- **No feed entry before the release** — The feed PR (or its GitHub Copilot portion) is opened after
  CLIHub 1.1.1 is released, not before. The `reserved-ids.json` update and the GitHub Copilot feed
  entry land in the same post-release stage.
- **No version bump in this change** — `Directory.Build.props` remains at `1.1.0`; the version bump
  to `1.1.1` is release preparation work, outside the scope of this change.
- **OMP and Devin are out of scope** — The user-confirmed target built-in set
  (OpenCode, Pi, OMP, Qwen Code, Devin) adds OMP and Devin as built-ins. That addition is a
  **separate, later change** and is not planned, designed, or implemented here. This change only
  removes GitHub Copilot; OpenCode, Pi, and Qwen Code are untouched.
- **New feed plugins (Gemini, Kilocode, Kiro, Mistral Vibe) are out of scope** — Not part of this
  migration.
- **No changes to the OpenClaude feed entry or the `move-openclaude-to-feed` change** — This change
  does not edit that change's artifacts. Coordination is documented, not implemented, here.

## Dependencies

- **Archive ordering:** This change's spec delta uses `MODIFIED "Built-in agent descriptors"`,
  which assumes that requirement exists in the main spec by the time this change is archived. That
  requirement is introduced by `move-openclaude-to-feed`'s `ADDED` block. **This change must be
  archived after `move-openclaude-to-feed` is archived.** See Design for verification of what
  `openspec validate --all` does today while the base is absent, and for the archive-ordering
  precondition in full.
- **Follow-on change (not part of this one):** adding OMP and Devin as built-ins, to reach the
  user-confirmed target set (OpenCode, Pi, OMP, Qwen Code, Devin).
