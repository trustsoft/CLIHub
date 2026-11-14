# Design: Plugin registry consolidation — one local registry document

All `file:line` references verified at HEAD `cff442f` (working tree clean) on 2026-11-14.
Baseline `openspec validate --all`: `Totals: 33 passed, 0 failed (33 items)` (pre-existing
long-requirement warnings only).

## Verified Current State

### Persistence seam (src/CLIHub.Core/Plugins/)

| Element | Location | Fact |
|---|---|---|
| `IPluginStateRepository` | `IPluginStateRepository.cs:7-21` | `PluginStateDocument Load()` (missing/corrupt → empty, never throws) + `void Save(document)` (atomic) |
| `PluginStateRepository` | `PluginStateRepository.cs:16-35` | File-backed; ctor defaults to `AppPaths.PluginStateFile` (`:33`), overridable for tests |
| `Load` | `:38-73` | Missing file → `Empty()` `:42-45`; null deserialize → empty + warning `:47-53`; `Plugins ??=` `:55`; tolerant blank-origin → `unknown` `:57-64`; catch-all → empty + warning, never throws `:68-72` |
| `Save` | `:76-90` | `ArgumentNullException` `:78`; `AtomicFileWriter.Write` `:84` (temp + rename); failure logged, not thrown `:86-89` |
| `PluginStateDocument` | `PluginStateDocument.cs:7-24` | `SchemaVersion` (always 1) `:12`; `Plugins` dictionary keyed by id, ordinal `:17`; `Empty()` `:23` |
| `PluginOrigin` | `PluginStateEntry.cs:6-23` | `Builtin = "builtin"` `:11`, `Installed = "installed"` `:16`, `Unknown = "unknown"` `:22`. **No `discovered` constant** — "discovered" is a *project* provenance value, not a plugin origin |
| `PluginStateEntry` | `:31-72` | Ctor `(string origin = Unknown)` `:37-40`; `Origin` `:47`; `ContentHash` `:53` (hex SHA-256 of `plugin.json`, nullable); `LogoHash` `:59`; `InstalledVersion` `:65` (nullable — this is the null that reads as "update available"); `Disabled` `:71` (bool, default false) |
| `AtomicFileWriter` | `AtomicFileWriter.cs:16-29` | Ensures parent dir `:21-25`, writes `target + ".tmp"` `:27`, `File.Move(overwrite: true)` `:28` |
| `CoreJson.Options` | `CoreJson.cs:14-18` | camelCase + indented — the registry document is camelCase automatically |
| `AppPaths` | `AppPaths.cs:40,55` | `PluginsDirectory` `:40`; `PluginStateFile` `:55` — sole consumer is the repository default (`PluginStateRepository.cs:33`; grep confirmed) |

### Consumers

| Consumer | Location | Fact |
|---|---|---|
| `PluginCatalog` | `PluginCatalog.cs:21,48` | Optional `IPluginStateRepository?` ctor param; field nullable |
| — state use | `:79,100-110` | `PluginStateDocument? state = null` `:79`; **lazy single `Load()`** `:100`; only use is the disabled check `stateEntry.Disabled` `:101` → `DisabledPluginEntry(id, dir, origin, version)` `:103-107` + skip `:108-109`. **No hash verification exists anywhere in the catalog** |
| — load flow | `:74-152` | Per enumerated directory: read error → invalid `:86-92`; disabled check `:98-110`; validation `:112-121`; duplicate first-wins `:123-132`; adopt into list `:134-138`. `invalidPlugins` `:24`, `disabledPlugins` `:25` snapshots |
| `PluginInstaller` | `PluginInstaller.cs:30,49-64` | Required `IPluginStateRepository` ctor param |
| — state reads | `:172,485` | `InstalledVersion` read for `ExistingPluginVersion` (zip + descriptor previews) |
| — `RecordInstalledState` | `:501-512` | Called from `CommitInstall` `:295`. **Already records** `ContentHash = PluginHasher.ComputeFileHash(plugin.json)` `:506`, `LogoHash` `:507` (null when absent), `InstalledVersion` `:508`, `Disabled = false` `:509` |
| — shared commit core | `:228-320` | Used by zip `Install` `:96` and `InstallDescriptor` `:201`; descriptor written verbatim `:206`; staging/backup/move/rollback untouched by this change |
| `PluginSeeder` | `PluginSeeder.cs:23,32-39` | Required `IPluginStateRepository` ctor param |
| — flow | `:42-217` | `Load()` `:57`; skip installed `:78-81`; skip disabled `entry is { Disabled: true }` `:84-87`; `embeddedHash`/`diskHash`/`storedHash` `:89-91`; missing disk → seed + record `:96-107`; disk==embedded → adopt builtin `:108-118`; app-update refresh `:119-129`; user-modified → preserve, **keep storedHash** `:130-135`; first observation of unrecognized content → `origin=unknown` + `diskHash` `:136-150`; logo three-hash `:152-197`; conditional `Save` `:200-203` |
| `AgentManagementViewModel` | `AgentManagementViewModel.cs:45,93` | Field + ctor param `IPluginStateRepository pluginState` |
| — card build | `:513-545` | `pluginState.Load()` `:515`; `Origin`/`InstalledVersion` onto card `:540-541` |
| — enable/disable | `:1174-1204` | `DisablePlugin` sets `entry.Disabled = true` `:1178` + reload `:1179`; `EnablePlugin` sets `false` `:1197` |
| — `UpdatePluginStateEntry` | `:1253-1260` | Load → get-or-create entry (`new PluginStateEntry(Unknown)`) → mutate → Save |
| `BrowsePluginsDialogViewModel` | `BrowsePluginsDialogViewModel.cs:22,41-66,168` | Ctor param `:44`; `Load()` in `LoadFeedAsync` `:168`; status classification `:181-197` — no entry → Available `:186-189`, entry with `InstalledVersion == null` → UpdateAvailable `:190-193` |
| `PluginInitializationService` | `PluginInitializationService.cs:18-29` | Startup order: `EnsureBuiltinsExist()` `:27` **then** `LoadPlugins()` `:28` |
| DI | `ServiceCollectionExtensions.cs:99-113` | `AddSingleton<IPluginStateRepository, PluginStateRepository>` `:109` |
| `IPluginCatalog` | `IPluginCatalog.cs:19-53` | Sole implementer is `PluginCatalog` (grep: no other `: IPluginCatalog` and no interface-holding fake; tests construct the real catalog) |

### Hashing facts

- `PluginHasher.ComputeHash(byte[])` `PluginHasher.cs:18-22`, `ComputeHash(string)` (UTF-8 bytes)
  `:29-33`, `ComputeFileHash(path)` (null when missing) `:40-46`. Lowercase hex.
- **One mechanism, two input forms:** the seeder hashes the embedded descriptor *string*
  (`PluginSeeder.cs:89`) and the disk file (`:90`); the installer hashes installed files
  (`PluginInstaller.cs:506-507`). They agree because `File.WriteAllText` writes UTF-8 without BOM,
  so the bytes written equal `Encoding.UTF8.GetBytes(json)` — the same bytes both overloads hash.
  This equivalence is already load-bearing: the seeder's adopt path (`:108-118`) requires
  `diskHash == embeddedHash` after a fresh seed (`PluginSeederTests.cs:91-101` records hashes,
  `:160-180` idempotency).
- **DEV-1 (discrepancy, see Deviations):** the brief and ADR-0015 say content hashes "exist only
  for built-in plugins". In fact zip installs have recorded `contentHash` + `logoHash` since the
  installer existed (`PluginInstaller.cs:506-507`; pinned by `plugin-installation` spec "Hash
  computed on install", `openspec/specs/plugin-installation/spec.md:268-272`). The actual gaps this
  slice closes: (a) nothing ever *re-verifies* stored hashes on scan, and (b) folders with no entry
  (hand-created, or state lost) never get a hash. The plan targets those gaps; no new hash
  recording is needed on the install paths beyond `installedAt`.

### UI affordance facts (surfacing decision input)

- Cards carry: a version/state chip replaced by an update chip while an update is available
  (`AgentCardItem.cs:186-208`), and identity in the name tooltip — origin as a labelled line
  ("Origin: Built-in/Installed/Unknown"), then version/publisher/homepage
  (`AgentCardItem.cs:353-391`; spec `agent-ui/spec.md:312-314` pins "no separate origin badge").
- **No modified/flag affordance exists** (grep for `modified|Modified|flag` in
  `src/CLIHub/ViewModels/Agents/`: only a code comment at `AgentManagementViewModel.cs:1243`). So
  S-C surfacing is: log + registry/catalog API property, no UI — per the brief's own instruction.

### Tests inventory

| File | Facts |
|---|---|
| `tests/CLIHub.Core.Tests/Plugins/PluginStateRepositoryTests.cs` | 6 tests `:27-146`: missing file → empty `:27-34`; corrupt → empty + "Failed to load plugin state" log `:36-47`; atomic temp+rename `:49-65`; parent-dir creation `:67-86`; round-trip `:88-125` (asserts `SchemaVersion` 1 `:32,:109`); blank/absent origin → unknown `:127-146` |
| `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs` | 23 tests `:65-436`; helpers build `PluginStateRepository` over a temp file `:28-29`, `WriteState`/`ReadState` `:46-54`; fixtures set `Disabled = true` (`:257` area) |
| `tests/CLIHub.Core.Tests/Plugins/PluginInstallerTests.cs` | Repository into installer `:33`; state fixtures `:84-86`; state assertions `:375,402,605,624` |
| `tests/CLIHub.Core.Tests/Plugins/PluginCatalogTests.cs` | `CreateManager(IPluginStateRepository?)` `:32-34`; `CreateStateRepository` over `plugin-state.json` name `:35-37`; `WriteState` `:38-44`; disabled behavior tests `:404-482` |
| `tests/CLIHub.Tests/Support/InMemoryPluginStateRepository.cs` | Fake: `Document` + `SaveCount`, implements `IPluginStateRepository` `:10-27` |
| `tests/CLIHub.Tests/ViewModels/Agents/BrowsePluginsDialogViewModelTests.cs` | Fake wiring `:22,601-611`; `InstalledVersion` fixtures `:39-41,77,281,322` |
| `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Plugins.cs` | Fake + `PluginStateEntry` fixtures `:130,150-151,226-228,254-255` |
| `tests/CLIHub.Tests/Builders/AgentManagementViewModelBuilder.cs` | `WithPluginState` `:37,167,238` |

### Specs pinning the current behavior (what pins what)

| Spec | Requirement | Pins |
|---|---|---|
| `plugin-installation/spec.md:147-164` | "Plugin lifecycle state tracking" | "State file updated on install" — origin/contentHash/logoHash/installedVersion + **`disabled is set to false`** `:159`; "State persists across restarts" `:161-164` |
| `plugin-installation/spec.md:166-184` | "Disable and enable plugins" | `disabled: true` `:173`, `disabled: false` `:181` |
| `plugin-installation/spec.md:264-286` | "SHA-256 content integrity" | hash-on-install `:268-272`; "Hash comparison detects modification" hedges with "the catalog **or seeding policy** computes its hash" `:274-279`; semantics `:281-286` |
| `plugin-seeding/spec.md:9-102` | "Seed built-in descriptors on first run" | 13 scenarios; "plugin state file"/"state file" wording throughout (`:17,:31,:38,:45,:53,:61,:68,:76,:80`); `disabled: true` `:80` |
| `plugin-seeding/spec.md:104-117` | "Never overwrite user content" | "stored hash … in the state file" `:110` |
| `plugin-seeding/spec.md:279-297` | "Skip disabled plugins during load" | `disabled: true` `:285,:291`; `disabled: false` `:296` |
| `plugin-feed/spec.md:150-202` | "Display available plugins with status" | `plugin-state.json` in 6 scenario bodies `:156,:162,:168,:177,:186,:195`; null-version → "Update Available" `:193-197` |
| `plugin-feed/spec.md:264-288` | "Update plugin from feed" | `installedVersion` in `plugin-state.json` `:275,:288` |
| `agent-ui/spec.md:310-314` | card identity tooltip | origin as tooltip line, no badge — unchanged by this slice |

## Decisions

### D1 — File name: `registry.json` (new name, not `plugin-state.json`)

`AppPaths.RegistryFile` = `%APPDATA%\CLIHub\registry.json`; `PluginStateFile` renamed
`LegacyPluginStateFile` (same value). Rationale: ADR-0015 names the concept ("one registry
document"); a distinct file name makes adoption **trigger on file presence** (registry absent +
legacy present) rather than on content sniffing, and leaves an unambiguous rollback story — an old
build reading `plugin-state.json` simply re-creates it, and the next start re-adopts. Rejected:
keeping `plugin-state.json` (would need a content marker to distinguish "already registry-shaped"
from "legacy", reintroducing a version gate ADR-0014 removed).

### D2 — `enabled` (default **true**) replaces `disabled`

The flag flips polarity in the new document. Rationale: (1) the slice already renames the document,
the model, and the seam — the flag rides the same one-time churn wave instead of needing a second
conversion later; (2) adoption is the single conversion point (`disabled: true` → `enabled:
false`), so no long-lived dual-polarity code exists; (3) positive polarity matches how the rest of
the entry reads (`origin`, `installedAt` describe what is true). The missing-field hazard (an
`enabled` bool defaulting to `false` would disable everything when a field is dropped) is handled
by the constructor default `enabled = true` — System.Text.Json constructor binding keeps the
default when the JSON property is absent, exactly how the tolerant `origin = Unknown` default works
today (`PluginStateEntry.cs:37-47`, pinned by `PluginStateRepositoryTests.cs:127-146`). Sites
flipping: catalog `PluginCatalog.cs:101` (`stateEntry.Disabled` → `!stateEntry.Enabled`), seeder
`:84-87` (`{ Disabled: true }` → `{ Enabled: false }`), ViewModel `:1178/:1197`. Rejected: keep
`disabled` (lower churn, but the v2 document would carry a negative-polarity flag indefinitely and
the rename cost is already sunk in this slice).

### D3 — Adoption lives in the repository's `Load`; legacy deleted after a successful write

`Load()` behavior:

1. `registry.json` exists and parses → return it (today's path, minus schemaVersion).
2. `registry.json` absent, `plugin-state.json` exists → read legacy (tolerant, same catch shape),
   map entries (origin/contentHash/logoHash/installedVersion preserved; `disabled` inverted;
   `installedAt` = null — timestamps are not fabricated for pre-existing content), **attempt one
   atomic save of `registry.json`**; on success, delete the legacy file (best effort, log
   Information) and return the mapped document; on failure, keep the legacy file, return the
   mapped document, and let the next start retry (registry is still absent).
3. `registry.json` corrupt/unparseable **and** legacy exists → adopt legacy (case 2) with a
   Warning that the registry was unreadable; corrupt and no legacy → empty + warning (today's
   behavior, `PluginStateRepository.cs:68-72`).
4. Neither file exists → `Empty()` (first run, `:42-45`).
5. Both exist → registry wins; the legacy file is ignored, logged once per repository instance at
   Information (the app is single-instance, so a lingering legacy file means a downgraded build ran
   meanwhile; warning on every `Load` would spam — `Load` runs per catalog load, seeder run, page
   build, and Browse open).

The save-success signal comes from a private `bool TrySave(document)` used only by adoption
(`AtomicFileWriter.Write` throws → false); the public `void Save` keeps its swallow-and-log
contract (`:76-90`), so the interface stays `Load`/`Save`. Rationale for adopting inside `Load`
rather than a startup step: the repository is the single read/write boundary, so **no consumer
ordering can skip adoption** (the seeder, catalog, ViewModels, and Browse all `Load` first); the
write happens once ever per machine, not per load, which respects the spirit of ADR-0014's "never
rewrites the file at load" (that principle is about not rewriting the *same* document each load;
adoption converts a *different, legacy* file once). Rejected: adoption in
`PluginInitializationService` (every other entry point — tests, Browse, a future headless path —
would need to remember to adopt first); keeping the legacy file forever (a stale second truth is
exactly the drift this slice removes); deleting legacy before the registry write succeeds (would
lose the only copy).

### D4 — Canonical hash input: the raw descriptor file bytes (one mechanism)

`contentHash` = `PluginHasher.ComputeFileHash(plugin.json)` — lowercase-hex SHA-256 over raw bytes —
recorded by the installer (`PluginInstaller.cs:506`), the seeder (string form of the same bytes,
`PluginSeeder.cs:89,103,115,143`), and catalog adoption (new). There are not "two mechanisms": the
seeder's string overload hashes the identical UTF-8-no-BOM bytes it writes with
`File.WriteAllText` (see Hashing facts), so embedded/disk/stored hashes remain comparable across
all writers. `logoHash` keeps its secondary role (seeder logo three-hash only) and stays in the
entry. Rejected: hashing a canonicalized/parsed form (breaks byte-for-byte "what CLIHub last wrote"
semantics and the seeder's preserve-user-modification logic); hashing the whole folder (spec pins
the descriptor+logo-only package format; folder-level hashing adds cost with no consumer).

### D5 — Reconciliation: adopt unknown folders; keep gone-folder entries

On catalog load (`LoadPluginsCore`):

- **Folder with a valid descriptor, no registry entry** → adopt: `origin = "unknown"`,
  `contentHash = diskHash`, `installedVersion = descriptor.version`, `enabled = true`,
  `installedAt = null`; one `Save` per scan when anything was adopted. This generalizes the
  seeder's "first observation of unrecognized content" rule (`PluginSeeder.cs:138-145`) from
  built-in ids to every folder, making "origin unknown" an explicit recorded state instead of an
  absent entry. Duplicate-id losers are not adopted (the winner's hash is recorded; the loser is
  reported through the existing duplicate diagnostic). Unreadable descriptors cannot be keyed (the
  id lives in the descriptor) and stay unadopted, surfacing via `GetInvalidPlugins()` as today.
- **Registry entry whose folder is gone** → **kept, logged (Information), never pruned**.
  Rationale: the seeder's decision table depends on entries surviving missing folders —
  `origin=builtin` + missing → re-seed (`PluginSeeder.cs:96-107`), and `origin=builtin` +
  `enabled=false` + missing → do **not** re-seed (`:84-87`); pruning would resurrect plugins the
  user disabled and deleted, and would irreversibly destroy disabled/origin state on a transient
  folder absence (cloud-sync hiccup, antivirus quarantine). The folder is a derived content store;
  its absence does not delete registry knowledge. Browse's "Installed (current)" status for a
  gone-folder plugin is unchanged *today* and is recorded as OQ-3 for the sync slice. Rejected:
  prune on scan (breaks seeder semantics above; irreversible); mark entries with a `missing` field
  (derivable at scan time; a stored flag would itself drift).

### D6 — Verification surfacing: log + API only; no UI this slice

For every plugin the catalog processes (the disabled branch and the loaded path after
first-wins dedupe), compute the disk descriptor hash and compare with the stored `contentHash`:

- mismatch → `LogWarning` ("user-modified", wording mirrors the seeder's
  `PluginSeeder.cs:134`) + a `UserModifiedPluginEntry(PluginId, PluginDirectory, Origin,
  StoredContentHash, DiskContentHash)` in a new `IPluginCatalog.GetUserModifiedPlugins()` snapshot
  (parallel to `GetDisabledPlugins()`; sole implementer is `PluginCatalog`, so the interface
  addition breaks nothing).
- match, or stored hash null (nothing CLIHub wrote is recorded — e.g. a legacy entry predating
  hashes) → nothing flagged, no write. A null stored hash is "cannot verify", not "modified".
- **the stored hash is never rewritten by verification** — the modification must stay detectable
  on the next scan (the seeder's invariant, `PluginSeeder.cs:130-135`).

No card badge/tooltip text is added: no affordance exists (`AgentCardItem.cs:353-391`), the
`agent-ui` spec pins the tooltip shape, and the brief forbids new card UI. The API + log keep the
door open for the sync/badges slice. Rejected: a `UserModified` property on the `Plugin` domain
model (the flag is lifecycle state, not descriptor content, and the catalog's detached-copy
contract would leak registry truth into the descriptor model); updating the stored hash on
detection (destroys the marker).

### D7 — Seam naming: `IPluginRegistry` / `PluginRegistryRepository` / `PluginRegistryDocument` / `PluginRegistryEntry`

`PluginOrigin` and its three values are unchanged. Production call sites to sweep (verified):
`PluginCatalog.cs:21,48,79,100`, `PluginInstaller.cs:30,50,172,485,501-511`, `PluginSeeder.cs:23,33,57`,
`AgentManagementViewModel.cs:45,93,1253-1259`, `BrowsePluginsDialogViewModel.cs:22,44,168`,
`ServiceCollectionExtensions.cs:109`. Test sites: `InMemoryPluginStateRepository.cs:10-27` (→
`InMemoryPluginRegistry`), `PluginStateRepositoryTests.cs` (→ `PluginRegistryRepositoryTests`),
`PluginCatalogTests.cs:32-44`, `PluginSeederTests.cs:28-54` + fixtures, `PluginInstallerTests.cs`
(6 sites), `BrowsePluginsDialogViewModelTests.cs` (11 sites + factory `:601-611`),
`AgentManagementViewModelTests.Plugins.cs` (4 sites), `AgentManagementViewModelBuilder.cs:37,167,238`.
Rejected: keeping `IPluginStateRepository` names (the document, file, and semantics all change —
keeping the name would make "state" vs "registry" ambiguous across the codebase and specs);
`IPluginRegistryStore`/`FilePluginRegistry` (invented naming without a repo precedent;
`PluginRegistryRepository` follows the established `*Repository` family — `ConfigurationRepository`,
ADR-0013's "third JSON-persisted atomic-write consumer" note at `docs/adr/0013:71`).

### D8 — Document shape details

```jsonc
// %APPDATA%\CLIHub\registry.json (camelCase, indented, via CoreJson)
{
  "plugins": {
    "opencode": {
      "origin": "builtin",            // builtin | installed | unknown (PluginOrigin)
      "contentHash": "sha256hex",     // raw plugin.json bytes; null = never recorded
      "logoHash": "sha256hex",        // nullable, seeder logo three-hash
      "installedVersion": "1.0.0",    // nullable — unchanged name and null semantics
      "enabled": true,                // replaces disabled; absent = true
      "installedAt": "2026-11-14T..." // UTC DateTimeOffset, install-time only; null otherwise
    }
  }
}
```

- **No `schemaVersion` field.** ADR-0014 removed it from `config.json` as functionless; tolerant
  reads (unknown fields ignored) make it pure noise. The legacy file's `schemaVersion: 1` is
  dropped during adoption.
- **`installedVersion` keeps its name** (deviation from the brief's `version` sketch — DEV-3): the
  name is pinned across `plugin-feed` scenarios and encodes the nullable "what the installed
  descriptor declared" semantics the drift discussion leans on; renaming buys nothing.
- **`installedAt`** is recorded only by the installer (`RecordInstalledState`,
  `DateTimeOffset.UtcNow`) for both install paths; the seeder leaves it null (built-ins ship with
  the app; "installed" is not meaningful) and adoption leaves it null (unknowable).
- **`feedUrlAtInstall` is deferred** (deviation — DEV-4): no consumer exists until the
  background-sync slice, and recording it would require threading the resolved feed URL through
  `BrowsePluginsDialogViewModel` → `PluginDescriptorInstallSource` → `RecordInstalledState` (the
  ViewModel does not hold the URL today; it is resolved inside `PluginFeedClient.GetFeedAsync`).
  Tolerant reads make adding the field later free for both directions. OQ-1 offers to pull it into
  this slice if the user prefers the schema frozen complete now.

### D9 — Catalog write policy

The catalog becomes a registry writer for exactly one case: adoption of entry-less folders (D5).
It performs **at most one `Save` per scan** (dirty flag, the seeder's pattern
`PluginSeeder.cs:58,200-203`) using a document loaded once at scan start (`:100`'s lazy load
becomes unconditional-but-once). Verification itself never saves (D6). Load-then-save racing
another writer is not a concern: the app is single-instance and startup is sequential (seeder →
catalog, `PluginInitializationService.cs:27-28`); the existing
`AgentManagementViewModel.UpdatePluginStateEntry` already relies on the same load-modify-save
pattern (`:1253-1260`).

### D10 — Guarantee inventory (unchanged by design)

Atomic writes (temp+rename, `AtomicFileWriter.cs:16-29`); tolerant reads never throw
(`PluginStateRepository.cs:68-72` shape preserved); seeder three-hash preserve-user-modifications
semantics untouched (`PluginSeeder.cs:89-197`); installer staging/backup/rollback untouched
(`PluginInstaller.cs:228-320`); zip + descriptor-only installs keep recording state
(`:501-512`); catalog precedence/deterministic order/first-wins untouched (ADR-0004;
`LocalFolderPluginSource.cs:31-34`); `CLIHub.Core` stays WPF-free (all new code in
`CLIHub.Core/Plugins`); user-facing strings unchanged (the flag and file names are not user-facing;
the only new strings are log messages and API surface).

## Alternatives Rejected

- **Keep `plugin-state.json` as the file name** — D1 (adoption would need content sniffing or a
  version marker; rollback story muddier).
- **Keep `disabled`** — D2 (polarity churn is one-time here, permanent clarity win for the v2
  document).
- **Adoption as a startup service step** — D3 (order-dependent; every entry point must remember).
- **Keep the legacy file after adoption** — D3 (stale second truth; the drift class survives).
- **Prune gone-folder entries on scan** — D5 (breaks disabled+deleted builtin non-resurrection and
  re-seed semantics; irreversible).
- **Mark gone-folder entries with a persisted flag** — D5 (derived state; the flag itself drifts).
- **`UserModified` on the `Plugin` model** — D6 (lifecycle state leaking into the descriptor model).
- **Rewrite the stored hash when a modification is detected** — D6 (destroys the marker; the seeder
  deliberately keeps it, `PluginSeeder.cs:130-135`).
- **Two hash mechanisms (descriptor bytes for installs, something else for built-ins)** — D4 (they
  are the same mechanism today; splitting them would break cross-writer comparability).
- **`version` instead of `installedVersion`** — D8/DEV-3 (pinned name; no benefit).
- **`IPluginRegistryStore` naming** — D7 (no repo precedent).
- **New capability `plugin-registry` vs patching only existing specs** — chose the new capability
  because the registry document is a cross-cutting durable contract (installer writes it, seeder
  writes it, catalog reads/adopts it, feed status reads it) that would otherwise be smeared across
  three unrelated specs.

## Deviations

- **DEV-1 — "hashes exist only for built-ins" is not literally true.** The brief and ADR-0015
  (:20-21) say content hashes exist only for built-in plugins; code reality: zip installs have
  recorded `contentHash`/`logoHash` all along (`PluginInstaller.cs:506-507`, spec-pinned at
  `plugin-installation/spec.md:268-272`), and descriptor-only installs inherit that path. Code
  wins: S-B's work is re-verification-on-scan (S-C), hash coverage for adopted folders, and
  `installedAt` — not new install-path hashing. The proposal states it this way.
- **DEV-2 — "discovered" is not a plugin origin.** The brief's shape listed
  `builtin|installed|discovered|unknown`; the existing `PluginOrigin` constants are
  `builtin|installed|unknown` (`PluginStateEntry.cs:11-22`) — "discovered" is a project-provenance
  value (project scanning). Aligning with the existing constants (the brief's own instruction)
  means the registry keeps three origin values.
- **DEV-3 — field named `installedVersion`, not `version`** — D8.
- **DEV-4 — `feedUrlAtInstall` deferred to the sync slice** — D8/OQ-1.

## Open Questions

- **OQ-1 — `feedUrlAtInstall` now or later?** Default plan: defer (DEV-4). If the user wants the
  schema frozen complete in this slice, add `string? FeedUrl` to `PluginDescriptorInstallSource`,
  an `IPreferencesStore` (or resolved-URL) dependency to `BrowsePluginsDialogViewModel`, and record
  it in `RecordInstalledState` — roughly one extra stage of ViewModel/test churn.
- **OQ-2 — eventual prune semantics.** Entries whose folder disappeared are kept and logged (D5).
  When a user-driven plugin-delete flow lands (none exists today — disable is the only "removal"),
  should delete prune the entry explicitly? Recorded for that slice, not this one.
- **OQ-3 — should Browse's "Installed" statuses consult folder presence?** Today a gone-folder
  entry still shows "Installed (current)" in Browse Plugins. With the registry now the recorded
  truth this is visible and deliberate (D5); the sync slice may want presence-aware statuses.
  Out of scope here (would change pinned feed-status behavior).

## Implementation Notes (filled during implementation)

- None yet.
