# Plugin registry consolidation: one local registry document, hash coverage, uniform modification detection

## Why

Three loosely-coupled local sources of truth exist today — the plugins folder,
`%APPDATA%\CLIHub\plugin-state.json`, and the feed — and they drift apart (registry discussion
2026-11-13/14, recorded in ADR-0015 `docs/adr/0015-plugin-registry-v2.md:15-17` and
`docs/backlog.md:144-156`): state entries whose folder is gone, folders without state entries
(origin unknown), and `installedVersion: null` entries reading as "update available" in Browse
Plugins. Stored content hashes are only ever *consulted* by the built-in seeder's three-hash logic
(`PluginSeeder.cs:89-150`); nothing re-verifies an installed plugin's descriptor on scan, so user
modifications to installed plugins are invisible. ADR-0015 commits to "one registry document as the
single local source of truth (content hash for every installed plugin, tolerant read, adoption of
existing `plugin-state.json` contents without a migration runner, per ADR-0014)" as a roadmap slice
(`docs/adr/0015-plugin-registry-v2.md:59-62`). This is that slice (roadmap slice 2 of the registry
v2 program, after the archived `2026-11-14-plugin-feed-v2-client`).

## What Changes

Four items, S-A–S-D. None weakens a guarantee; the full guarantee inventory is in `design.md`.

- **S-A — the registry document.** `%APPDATA%\CLIHub\registry.json` becomes the only local source
  of truth for plugin lifecycle state: a `plugins` map id → entry `{ origin, contentHash,
  logoHash, installedVersion, enabled, installedAt }` (`PluginOrigin` values unchanged:
  `builtin|installed|unknown`). Tolerant read per ADR-0014 (missing fields defaulted — `enabled`
  defaults **true** — unknown fields ignored, corrupt file degrades to empty, never throws) and
  atomic temp+rename writes via the existing `AtomicFileWriter`. The inverted `disabled` flag is
  replaced by positive `enabled`. **Adoption, not migration:** when `registry.json` is absent and
  the legacy `plugin-state.json` exists, the repository reads it, maps entries (hashes, origin,
  installedVersion preserved; `disabled` inverted; `installedAt` null — not fabricated), writes
  `registry.json` once, deletes the legacy file (logged); if the write fails, the legacy file stays
  and adoption retries on the next start; once `registry.json` exists the legacy file is never read
  again. A corrupt `registry.json` with a legacy file present recovers by adopting the legacy
  content.
- **S-B — hash coverage.** `contentHash` (SHA-256 of the raw `plugin.json` bytes, hex) is recorded
  for every managed plugin: zip installs and descriptor-only installs (already recorded by
  `PluginInstaller.RecordInstalledState`), seeded built-ins (seeder three-hash logic unchanged),
  and — new — folders adopted during catalog reconciliation. One mechanism (`PluginHasher`); the
  canonical input is the raw descriptor file bytes.
- **S-C — scan verification and reconciliation.** `PluginCatalog.LoadPlugins` verifies each
  processed plugin's stored `contentHash` against the descriptor on disk: mismatch = user-modified,
  logged as a warning and exposed through a new read-only `IPluginCatalog.GetUserModifiedPlugins()`
  snapshot. Verification never rewrites the stored hash. Folders with a valid descriptor but no
  registry entry are adopted with `origin = "unknown"`, the disk hash, the descriptor version, and
  `enabled = true`. Registry entries whose folder disappeared are **kept, not pruned** (logged):
  the seeder's re-seed and disabled-preservation semantics depend on entries surviving missing
  folders. No new card UI: no modified/flag affordance exists today (the card has a version chip
  and an origin tooltip only, `AgentCardItem.cs:186-208, 377-391`), so this slice records, logs,
  and exposes the API — a badge is deliberately left to a later slice.
- **S-D — repository seam.** `IPluginStateRepository`/`PluginStateRepository`/
  `PluginStateDocument`/`PluginStateEntry` are renamed to `IPluginRegistry`/
  `PluginRegistryRepository`/`PluginRegistryDocument`/`PluginRegistryEntry` (`PluginOrigin`
  unchanged), across all ~13 production call sites (PluginCatalog, PluginInstaller, PluginSeeder,
  AgentManagementViewModel, BrowsePluginsDialogViewModel, DI) and the test fakes. All writes
  continue to go through the repository (atomic write preserved); `AppPaths` gains `RegistryFile`
  and renames `PluginStateFile` → `LegacyPluginStateFile`.

Tests: the repository test file is rewritten for the new document and the adoption matrix
(adopt+delete, failed-write retry, both-present, corrupt-recovery, tolerant defaults); catalog
tests gain verification/reconciliation coverage; installer tests gain `installedAt`/`enabled`
assertions; seeder, ViewModel, and builder tests are swept for the rename and the `enabled`
polarity with behavior assertions otherwise unchanged.

## Capabilities

### Added Capability: `plugin-registry`

Delta in `specs/plugin-registry/spec.md` (this change):

- **ADDED "Single local registry document"** — document location and entry shape, tolerant read,
  atomic write.
- **ADDED "Adopt legacy plugin-state.json"** — one-time adoption, inversion, legacy disposition
  (delete on success, keep-and-retry on failure, ignore once registry exists, corrupt-registry
  recovery).
- **ADDED "Uniform content hash coverage"** — canonical hash input, every install kind + seeding +
  adoption, stored hash never rewritten by detection.
- **ADDED "Verify content and reconcile on catalog load"** — mismatch = user-modified (log + API),
  uniform across origins, folder adoption with origin unknown, missing-folder entries retained,
  clean scan writes nothing.

### Modified Capability: `plugin-installation`

Delta in `specs/plugin-installation/spec.md` (this change):

- **MODIFIED "Plugin lifecycle state tracking"** — state lives in `registry.json`; "State file
  updated on install" now records `enabled: true` and `installedAt`; "State persists across
  restarts" reads the registry.
- **MODIFIED "Disable and enable plugins"** — `disabled: true/false` becomes `enabled: false/true`
  in the scenario bodies; scenario names preserved.
- **MODIFIED "SHA-256 content integrity"** — "Hash comparison detects modification" now pins the
  catalog-load verification; other scenarios unchanged in substance.

### Modified Capability: `plugin-seeding`

Delta in `specs/plugin-seeding/spec.md` (this change):

- **MODIFIED "Seed built-in descriptors on first run"** — all 13 scenario names preserved; "plugin
  state file"/"state file" wording moves to the registry; "Disabled built-in not re-seeded" uses
  `enabled: false`.
- **MODIFIED "Never overwrite user content"** — "User-modified descriptor preserved" reads the
  stored hash from the registry.
- **MODIFIED "Skip disabled plugins during load"** — `disabled: true` → `enabled: false` wording;
  "Enabled plugin loads normally" reworded for the positive flag.

### Modified Capability: `plugin-feed`

Delta in `specs/plugin-feed/spec.md` (this change):

- **MODIFIED "Display available plugins with status"** — all 7 scenario names preserved; the six
  bodies referencing `plugin-state.json` reference `registry.json` instead (semantics, including
  null-`installedVersion` → "Update Available", unchanged).
- **MODIFIED "Update plugin from feed"** — all 3 scenario names preserved; the two bodies
  referencing `installedVersion` in `plugin-state.json` reference the registry.

## Impact

**Modified code:**

- `src/CLIHub.Core/Infrastructure/FileSystem/AppPaths.cs` — `RegistryFile` added;
  `PluginStateFile` → `LegacyPluginStateFile`.
- `src/CLIHub.Core/Plugins/IPluginStateRepository.cs`, `PluginStateRepository.cs`,
  `PluginStateDocument.cs`, `PluginStateEntry.cs` — renamed to `IPluginRegistry`,
  `PluginRegistryRepository`, `PluginRegistryDocument`, `PluginRegistryEntry`; new document shape;
  adoption logic in `Load`.
- `src/CLIHub.Core/Plugins/PluginCatalog.cs` — registry types, `enabled` check, hash verification,
  folder adoption, `GetUserModifiedPlugins`.
- `src/CLIHub.Core/Plugins/IPluginCatalog.cs` — `GetUserModifiedPlugins` (sole implementer is
  `PluginCatalog`; no test fake implements the interface).
- `src/CLIHub.Core/Plugins/UserModifiedPluginEntry.cs` — new record (parallel to
  `DisabledPluginEntry`).
- `src/CLIHub.Core/Plugins/PluginInstaller.cs` — registry types; `RecordInstalledState` records
  `enabled: true` and `installedAt`.
- `src/CLIHub.Core/Plugins/PluginSeeder.cs` — registry types; `entry is { Disabled: true }` →
  `entry is { Enabled: false }`; three-hash logic otherwise untouched.
- `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs` — `IPluginRegistry` registration.
- `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` — registry types; enable/disable
  mutate `Enabled`.
- `src/CLIHub/ViewModels/Agents/BrowsePluginsDialogViewModel.cs` — registry types (behavior
  unchanged).
- Tests: `tests/CLIHub.Core.Tests/Plugins/PluginStateRepositoryTests.cs` →
  `PluginRegistryRepositoryTests.cs` (rewritten + adoption matrix), `PluginCatalogTests.cs`,
  `PluginSeederTests.cs`, `PluginInstallerTests.cs`,
  `tests/CLIHub.Tests/Support/InMemoryPluginStateRepository.cs` → `InMemoryPluginRegistry.cs`,
  `tests/CLIHub.Tests/ViewModels/Agents/BrowsePluginsDialogViewModelTests.cs`,
  `AgentManagementViewModelTests.Plugins.cs`, `tests/CLIHub.Tests/Builders/
  AgentManagementViewModelBuilder.cs`.
- Docs: `docs/architecture/plugins.md` (Plugin Lifecycle State → registry), `docs/project-context.md`
  (runtime facts), `docs/backlog.md` (slice delivered).

**Unchanged:** installer staging/backup/rollback ordering and behavior; the seeder's three-hash
decision table; `LaunchWindow`/catalog precedence (ADR-0004); deterministic loading and duplicate
policy; feed parsing/install flows and all their user-facing strings; `config.json` and the
`PluginFeedUrl` semantics; the Browse Plugins status model; card/tooltips/chips UI;
`CLIHub.Core` stays WPF-free.

**Verification:** `dotnet build CLIHub.sln` + `dotnet test CLIHub.sln` after every stage;
`openspec validate --all`; `graphify update .` at the end; a manual adoption smoke (run once over an
existing `%APPDATA%\CLIHub\plugin-state.json`, confirm `registry.json` appears and the legacy file
is deleted with plugins, disabled state included, unchanged) is recommended before commit/archive.

## Non-Goals

- No background feed sync, no ETag/If-None-Match, no update badges outside Browse (next slice).
- No index signing (`index.sig`/`signature` stay reserved; ADR-0015 signing slice).
- No new UI surfaces: no modified/flag card badge or tooltip line (none exists today; the door is
  left open — see design D6 and OQ-3).
- No pruning or deletion of registry entries whose folder disappeared (kept and logged; semantics
  recorded in design D5, revisit recorded as OQ-2).
- No `feedUrlAtInstall` recording in this slice — the field is deferred to the background-sync
  slice, which is its first consumer (tolerant reads make adding it later free; design D8, OQ-1).
- No feed-format changes, no `config.json` changes, no deletion of working v2-feed code.
- No migration runner, schema-version gate, or document rewrite-on-load beyond the one-time
  legacy adoption (ADR-0014).

## Dependencies

- Base: HEAD `cff442f` (working tree clean). No other active changes (`openspec list` reports
  none). No file overlap with concurrent work.
- Follows the archived `2026-11-14-plugin-feed-v2-client` (registry v2 roadmap slice 1); consumes
  its descriptor-only install path unchanged.
- Consumes, does not modify: `PluginHasher`, `AtomicFileWriter`, `CoreJson`,
  `PluginDescriptorReader`/`Validator`, `LocalFolderPluginSource`, `EmbeddedPluginResources`.
- Blocks: the background-sync slice (`docs/backlog.md:158-167`), which wants the registry's sync
  metadata.

## Risk

- **Polarity inversion regressions (S-A):** flipping `disabled` → `enabled` touches the catalog's
  skip check, the seeder's disabled guard, and the enable/disable commands; a missed site would
  invert user-visible enable/disable behavior. Mitigated by a compile-driven sweep (the model change
  breaks every consumer), existing behavior tests
  (`LoadPlugins_DisabledPlugin_NotLoadedByGetAllPlugins`, `EnsureBuiltinsExist_DisabledBuiltin_
  NotReSeeded`, ViewModel enable/disable tests) passing with fixture-only edits, and the tolerant
  default `enabled: true` failing safe (missing field = plugin loads).
- **Adoption losing user state (S-A):** a bug in the mapping or the delete step could drop origins,
  hashes, or disabled state. Mitigated by the adoption test matrix (including failed-write retry
  and corrupt-registry recovery) and the manual smoke over a real legacy file before archive.
- **Seeder interference (S-C):** catalog adoption runs after seeding at startup; adopting a folder
  the seeder chose not to touch (installed/disabled) must not overwrite its entry. Mitigated by
  adopting only folders with **no** entry (read-dominant reconciliation) and catalog tests covering
  the installed-plugin and disabled-plugin folders.
- **Rename churn (S-D):** ~13 production sites and 8 test files; mitigated by a dedicated
  behavior-identical stage 1 whose gate requires the full suite to pass with assertion-level edits
  only.
