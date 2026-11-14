# Tasks: Plugin registry consolidation — one local registry document

Conventions for every edit: file-scoped namespace first, `using` directives after it, nullable
reference types, `camelCase` private fields without underscore prefix, `this.` qualification,
braces required, XML docs on new public members, English only, test names in the
`MethodOrScenario_Condition_ExpectedResult` pattern. All `file:line` references are pre-change
positions at HEAD `cff442f`; when earlier tasks in the same file shift lines, anchor by the named
symbol. Per `AGENTS.md`, run GitNexus impact analysis
(`node .gitnexus/run.cjs impact "<symbol>" --direction upstream --repo .`) before the first edit of
`PluginStateRepository`, `PluginStateDocument`, `PluginStateEntry`, `PluginCatalog`,
`PluginInstaller`, `PluginSeeder`, `AgentManagementViewModel`, and
`BrowsePluginsDialogViewModel`, and heed HIGH/CRITICAL/UNKNOWN warnings before proceeding (a text
search must back any `UNKNOWN`). No git write commands, no `git commit`, no `openspec archive` in
these tasks. Reference decision IDs (D1–D10) and deviations (DEV-1–DEV-4) live in `design.md`.

Before Stage 1, capture the baseline: `dotnet build CLIHub.sln` (expect 0 errors/0 warnings),
`dotnet test CLIHub.sln` (record passed count), and note the current `openspec validate --all`
baseline (`Totals: 34 passed, 0 failed (34 items)` — 33 specs plus this change — with only the
pre-existing long-requirement and placeholder-purpose warnings).

## Stage 1: S-D — seam rename (behavior-identical; document, file name, and flag unchanged)

- [x] 1.1 Rename `src/CLIHub.Core/Plugins/PluginStateDocument.cs` → `PluginRegistryDocument.cs`
       and the record `PluginStateDocument` → `PluginRegistryDocument` (keep `SchemaVersion`,
       `Plugins`, `Empty()` exactly; update XML docs to say "registry document" but keep the
       `plugin-state.json` path reference for now)
- [x] 1.2 Rename `src/CLIHub.Core/Plugins/PluginStateEntry.cs` → `PluginRegistryEntry.cs`, the
       record `PluginStateEntry` → `PluginRegistryEntry` (ctor, `Origin`, `ContentHash`,
       `LogoHash`, `InstalledVersion`, `Disabled` unchanged — the `enabled` flip is Stage 2), and
       keep `PluginOrigin` in the same file untouched
- [x] 1.3 Rename `src/CLIHub.Core/Plugins/IPluginStateRepository.cs` → `IPluginRegistry.cs` and
       the interface `IPluginStateRepository` → `IPluginRegistry` (method signatures unchanged)
- [x] 1.4 Rename `src/CLIHub.Core/Plugins/PluginStateRepository.cs` → `PluginRegistryRepository.cs`
       and the class likewise (constructor, `Load`, `Save`, tolerant-read normalization, and the
       `AppPaths.PluginStateFile` default `:33` byte-for-byte in behavior)
- [x] 1.5 Sweep production consumers to the new type names only (no behavior edits):
       `PluginCatalog.cs` (`:21,:48,:79,:100`), `PluginInstaller.cs` (`:30,:50,:172,:485,:501-511`),
       `PluginSeeder.cs` (`:23,:33,:57,:93-94`), `AgentManagementViewModel.cs`
       (`:45,:93,:1253-1259`), `BrowsePluginsDialogViewModel.cs` (`:22,:36,:44,:52,:168`), and the
       DI registration `ServiceCollectionExtensions.cs:109`
       (`AddSingleton<IPluginRegistry, PluginRegistryRepository>`)
- [x] 1.6 Rename `tests/CLIHub.Tests/Support/InMemoryPluginStateRepository.cs` →
       `InMemoryPluginRegistry.cs` and the fake class likewise (members unchanged)
- [x] 1.7 Sweep test files for the renamed types **without changing any assertion or fixture
       value**: `PluginStateRepositoryTests.cs` (also rename file+class to
       `PluginRegistryRepositoryTests`), `PluginCatalogTests.cs` (`:32-44` + local variable names),
       `PluginSeederTests.cs` (`:28-54` + fixtures), `PluginInstallerTests.cs` (`:33,:84-86,:375,
       :402,:605,:624`), `BrowsePluginsDialogViewModelTests.cs` (`:22,:601-611` + fake type),
       `AgentManagementViewModelTests.Plugins.cs` (`:130-255`), and
       `AgentManagementViewModelBuilder.cs` (`:37,:167,:238`)
- [x] 1.8 Gate (Stage 1): `dotnet build CLIHub.sln` (0 errors, 0 warnings) and
       `dotnet test CLIHub.sln` (0 failed, same passed count as baseline — the rename is
       behavior-identical; any assertion-level diff is a stop-and-report signal)

## Stage 2: S-A — registry document shape + adoption + `enabled` polarity

- [x] 2.1 Edit `src/CLIHub.Core/Infrastructure/FileSystem/AppPaths.cs`: add
       `public static string RegistryFile { get; } = Path.Combine(Root, "registry.json");` with XML
       docs; rename `PluginStateFile` → `LegacyPluginStateFile` (same value) with XML docs marking
       it the legacy pre-registry state file adopted into `registry.json` (D1; sole consumer is the
       repository default)
- [x] 2.2 Edit `src/CLIHub.Core/Plugins/PluginRegistryDocument.cs`: delete the `SchemaVersion`
       property (D8 — no schema-version field; tolerant reads per ADR-0014); keep `Plugins`
       (ordinal) and `Empty()`
- [x] 2.3 Edit `src/CLIHub.Core/Plugins/PluginRegistryEntry.cs`: replace `Disabled` with
       `public bool Enabled { get; set; } = true;` and add
       `public DateTimeOffset? InstalledAt { get; set; }` (D2, D8); ctor becomes
       `PluginRegistryEntry(string origin = PluginOrigin.Unknown, bool enabled = true)` so a
       missing JSON `enabled` stays **true** (fail-safe default; mirror of the tolerant
       `origin` default); update XML docs for the new polarity and `installedAt` semantics
       (install-time only, null for built-ins and adopted entries)
- [x] 2.4 Edit `src/CLIHub.Core/Plugins/PluginRegistryRepository.cs`: default path becomes
       `AppPaths.RegistryFile`; ctor gains `string? legacyStateFilePath = null` defaulting to
       `AppPaths.LegacyPluginStateFile` (test seam for both files); implement `Load` per design D3 —
       (a) registry exists and parses → return (tolerant normalization: `Plugins ??=` ordinal,
       blank `origin` → `unknown`, as today `PluginStateRepository.cs:55-64`); (b) registry absent
       + legacy exists → map legacy entries (origin/contentHash/logoHash/installedVersion kept,
       `disabled` inverted to `enabled`, `installedAt` null), try one atomic save via a private
       `bool TrySave(PluginRegistryDocument)` (wraps `AtomicFileWriter.Write`; returns false on
       throw), on success delete the legacy file best-effort + log Information, on failure keep the
       legacy file + log Warning (adoption retries next start); (c) registry corrupt + legacy
       exists → case (b) with a Warning that the registry was unreadable; corrupt + no legacy →
       empty + today's warning; (d) neither file → `Empty()`; (e) both files → registry wins, log
       Information once per repository instance (private bool flag). `Save` keeps today's
       swallow-and-log `void` contract. Deserialization uses the legacy
       `PluginStateDocument`-shaped reader only inside the adoption path — deserialize the legacy
       JSON with a private `LegacyStateShape` DTO (schemaVersion + plugins map with `disabled`),
       unknown fields ignored
- [x] 2.5 Polarity sweep (compile-driven; design D2): `PluginCatalog.cs:101`
       `stateEntry.Disabled` → `!stateEntry.Enabled`; `PluginSeeder.cs:84-87`
       `entry is { Disabled: true }` → `entry is { Enabled: false }`;
       `AgentManagementViewModel.cs:1178` `entry.Disabled = true` → `entry.Enabled = false` and
       `:1197` `entry.Disabled = false` → `entry.Enabled = true`. Grep the solution for remaining
       `\.Disabled` on registry entries (property name is gone; any survivor is a compile error)
- [x] 2.6 Rewrite `tests/CLIHub.Core.Tests/Plugins/PluginRegistryRepositoryTests.cs` for the new
       document and adoption matrix (design D2/D3/D8): (a)
       `Load_MissingRegistryAndLegacy_ReturnsEmptyRegistry`; (b)
       `Load_CorruptRegistryWithoutLegacy_ReturnsEmptyAndLogsWarning` (today's behavior); (c)
       `Save_WritesAtomicallyViaTempFileThenRename` (adapted, asserts no `schemaVersion` property
       in the written JSON and no `.tmp` left); (d) `SaveThenLoad_RoundTripPreservesData`
       (enabled true/false, installedAt DateTimeOffset round-trip, null logoHash/installedVersion);
       (e) `Load_MissingFields_DefaultOriginUnknownAndEnabledTrue` (hand-written JSON without
       `origin`/`enabled` — fail-safe default pin); (f) adoption:
       `Load_LegacyStateAdopted_WritesRegistryOnceAndDeletesLegacy` (legacy with builtin+installed
       + disabled entries → registry.json written, legacy deleted, entries mapped with
       `disabled: true` → `enabled: false`, hashes/versions preserved, installedAt null, log
       message asserted); (g) `Load_AdoptionWriteFails_KeepsLegacyAndRetriesNextLoad` (make the
       registry path unwritable — e.g. a read-only directory or a directory-blocking file named
       `registry.json` — legacy kept, mapped document still served, second repository over a
       writable path succeeds); (h) `Load_BothFilesPresent_RegistryWinsAndLegacyIgnored`
       (conflicting `enabled` values prove registry wins); (i)
       `Load_CorruptRegistryWithLegacy_AdoptsLegacyContent`
- [x] 2.7 Fixture-only polarity updates in tests that construct entries with `Disabled = true` →
       `Enabled = false` (`PluginCatalogTests.cs` disabled tests `:407,:427,:470`;
       `PluginSeederTests.cs` disabled-builtin fixture; `AgentManagementViewModelTests.Plugins.cs`
       `:150-151`); drop any `SchemaVersion` assertions (`PluginStateRepositoryTests.cs:32,:109`
       equivalents)
- [x] 2.8 Gate (Stage 2): `dotnet build CLIHub.sln` (0 errors, 0 warnings) and
       `dotnet test CLIHub.sln` (0 failed); the only behavior deltas are the document rename
       (tests run against temp paths), the `enabled` polarity, and the repository-test rewrite;
       catalog/seeder/installer/ViewModel behavior tests pass with fixture-only edits

## Stage 3: S-B — installer/seeder registry writes (hash coverage + `installedAt`)

- [x] 3.1 Edit `src/CLIHub.Core/Plugins/PluginInstaller.cs` `RecordInstalledState` (`:501-512`):
       set `Enabled = true` and `InstalledAt = DateTimeOffset.UtcNow` alongside the existing
       `Origin`/`ContentHash`/`LogoHash`/`InstalledVersion` (both install paths flow through
       `CommitInstall` `:295` — no other change; DEV-1: hashing already exists here)
- [x] 3.2 Verify (read-only) that `PluginSeeder` needs no new write: new entries it creates
       default to `Enabled = true` via the ctor default, `InstalledAt` stays null for built-ins
       (D8), and `ContentHash`/`LogoHash` recording (`:89-197`) is untouched; if any seeder site
       constructs an entry with the old `Disabled` property, fix it in this task (none known)
- [x] 3.3 Edit `tests/CLIHub.Core.Tests/Plugins/PluginInstallerTests.cs`: extend the two
       record-state assertions (`:402` zip area, `:605/:624` descriptor-only area) to assert
       `Enabled` is true and `InstalledAt` is non-null and recent (`DateTimeOffset.UtcNow`
       tolerance); keep every existing hash/version assertion unedited
- [x] 3.4 Edit `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs`: add
       `EnsureBuiltinsExist_EmptyFolder_RecordsEnabledEntryWithNullInstalledAt` (entry `Enabled`
       true, `InstalledAt` null, `ContentHash` equals the hash of the written `plugin.json`
       file — the D4 same-bytes pin); no existing seeder assertion changes
- [x] 3.5 Gate (Stage 3): `dotnet build CLIHub.sln` (0 errors, 0 warnings) and
       `dotnet test CLIHub.sln` (0 failed); suite grows by exactly the Stage 2 repository tests,
       3.3 extensions, and the 3.4 test

## Stage 4: S-C — catalog scan verification + reconciliation + exposure

- [x] 4.1 Create `src/CLIHub.Core/Plugins/UserModifiedPluginEntry.cs`:
       `public sealed record UserModifiedPluginEntry(string PluginId, string PluginDirectory,
       string? Origin, string? StoredContentHash, string? DiskContentHash)` with XML docs
       (parallel to `DisabledPluginEntry.cs:15-19`; a plugin whose on-disk descriptor no longer
       matches the hash CLIHub last wrote)
- [x] 4.2 Edit `src/CLIHub.Core/Plugins/IPluginCatalog.cs`: add
       `IReadOnlyList<UserModifiedPluginEntry> GetUserModifiedPlugins()` with XML docs mirroring
       `GetDisabledPlugins()` (`:48-52`); sole implementer is `PluginCatalog` (verified in
       design.md)
- [x] 4.3 Edit `src/CLIHub.Core/Plugins/PluginCatalog.cs` `LoadPluginsCore` (`:74-152`), design
       D5/D6/D9: load the registry document once unconditionally when a stateRepository is present
       (replacing the lazy `:100` load); add `userModifiedPlugins` list + snapshot field; for each
       processed plugin — in the disabled branch (`:98-110`) and in the loaded path after the
       duplicate check (`:123-138`) — compute
       `PluginHasher.ComputeFileHash(Path.Combine(entry.PluginDirectory, "plugin.json"))` and, when
       a stored `ContentHash` is non-null and differs, log a Warning (mirror the seeder wording at
       `PluginSeeder.cs:134`: descriptor modified locally; preserving) and add a
       `UserModifiedPluginEntry`; when the stored hash is null, flag nothing (cannot verify);
       **never rewrite the stored hash**. Adoption: when a valid plugin has **no** registry entry,
       record `origin = PluginOrigin.Unknown`, `contentHash = diskHash`,
       `installedVersion = plugin.Version`, `enabled = true`, `installedAt = null`, log
       Information, set a dirty flag, and `Save` once after the enumeration loop (skip the save
       when nothing was adopted). Registry entries whose folder was not seen are left untouched
       with one Information log per id (kept, not pruned — D5)
- [x] 4.4 Edit `src/CLIHub.Core/Plugins/PluginCatalog.cs`: expose
       `GetUserModifiedPlugins()` returning the snapshot (mirror `GetDisabledPlugins()` `:161`)
- [x] 4.5 Edit `tests/CLIHub.Core.Tests/Plugins/PluginCatalogTests.cs`: keep every existing test
       unedited where possible (they assert against temp state files; the repository rename is
       already swept) and add — (a)
       `LoadPlugins_FolderWithoutEntry_AdoptedAsUnknownWithDiskHash` (registry written after scan;
       entry origin unknown, contentHash equals hash of the descriptor on disk, installedVersion
       from descriptor, enabled true, installedAt null); (b)
       `LoadPlugins_InstalledDescriptorModified_FlaggedUserModified` (entry origin installed with
       a stale contentHash → plugin still loads, `GetUserModifiedPlugins` contains it with stored
       and disk hashes, stored hash not rewritten after reload); (c)
       `LoadPlugins_BuiltinDescriptorModified_FlaggedUniformly` (same for origin builtin — the
       uniform-detection pin); (d) `LoadPlugins_MatchingHash_NotFlagged`; (e)
       `LoadPlugins_NullStoredHash_NotFlagged`; (f)
       `LoadPlugins_RegistryEntryWithoutFolder_EntryKeptNotPruned` (entry survives the scan and a
       subsequent load); (g) `LoadPlugins_CleanScan_DoesNotRewriteRegistry` (file write time or
       content unchanged when nothing is adopted); (h)
       `ReloadPlugins_AfterModification_StateReflectsNewScan` (snapshot replaced per reload,
       mirroring the invalid-entries behavior)
- [x] 4.6 Gate (Stage 4): `dotnet build CLIHub.sln` (0 errors, 0 warnings) and
       `dotnet test CLIHub.sln` (0 failed); every pre-existing `PluginCatalogTests` test passes
       unedited or with fixture-only polarity edits; suite grows by exactly the 4.5 tests

## Stage 5: ViewModel wiring, docs, final gates

- [x] 5.1 Verify `AgentManagementViewModel` behavior end-to-end in tests
       (`AgentManagementViewModelTests.Plugins.cs`): disable → registry entry `enabled: false`,
       plugin moves to `GetDisabledPlugins`; enable → `enabled: true`, returns to
       `GetAllPlugins`; cards still read `Origin`/`InstalledVersion` from the registry (`:513-545`
       path); `UpdatePluginStateEntry` (`:1253-1260`) creates missing entries with the registry
       ctor. No new card/tooltips/chips UI and no XAML edits (D6)
- [x] 5.2 Verify `BrowsePluginsDialogViewModel` status semantics unchanged through the registry
       seam (no code edit expected beyond Stage 1's rename): the existing
       `BrowsePluginsDialogViewModelTests` statuses (Available / Installed / Update Available /
       Different Version Available, including the null-installedVersion case `:77`) pass unedited
       apart from fake/type renames
- [x] 5.3 Edit `docs/architecture/plugins.md`: "Plugin Lifecycle State" section (`:89-116`) → the
       registry document at `%APPDATA%\CLIHub\registry.json` with the new JSONC shape (origin,
       contentHash, logoHash, installedVersion, enabled, installedAt), the tolerant-read and
       one-time adoption sentences (legacy `plugin-state.json` adopted then deleted), and the
       disable/enable wording (`enabled: false/true`); the three-hash section (`:118-145`) keeps
       its decision table with `plugin-state.json` references changed to the registry; add a short
       "Scan verification and reconciliation" subsection (mismatch = user-modified, logged and
       exposed via `GetUserModifiedPlugins()`; entry-less folders adopted origin unknown;
       missing-folder entries kept)
- [x] 5.4 Edit `docs/project-context.md` runtime facts (`:44-48`): data layout lists
       `registry.json` (not `plugin-state.json`); the lifecycle-state sentence describes the
       registry as the single local source of truth with hash verification on scan
- [x] 5.5 Edit `docs/backlog.md` plugin-registry-v2 section (`:144-156`): mark the consolidation
       slice delivered by this change (one line, mirroring the `slice complete` style at `:28`) and
       keep the background-sync trigger wording pointing after it
- [x] 5.6 Final build gate: `dotnet build CLIHub.sln` — 0 errors, 0 warnings
- [x] 5.7 Final test gate: `dotnet test CLIHub.sln` — 0 failed, 0 skipped; record the passed
       count and confirm the only delta versus the baseline is the rewritten repository tests plus
       the new tests from 2.6, 3.3, 3.4, and 4.5
- [x] 5.8 Run `openspec validate --all` — confirm `Totals: 34 passed, 0 failed (34 items)` with
       `✓ change/plugin-registry-consolidation` in the output and no new warnings versus the
       baseline; fix any structural finding only inside
       `openspec/changes/plugin-registry-consolidation/`, never in `openspec/specs/`
- [x] 5.9 Run `node .opencode/scripts/check-model-policy.mjs` if any subagent was dispatched
       during implementation, and `node .opencode/scripts/pipeline-state.mjs add
       plugin-registry-consolidation` to record the pipeline entry (all stage transitions through
       the script, never direct `.opencode/pipeline.json` edits)
- [x] 5.10 Run `graphify update .` so the knowledge graph reflects the registry seam, adoption,
        verification, and reconciliation
- [x] 5.11 Scope check: `git status --porcelain` lists, for this change, only
        `src/CLIHub.Core/Infrastructure/FileSystem/AppPaths.cs`, the four renamed
        `src/CLIHub.Core/Plugins/PluginRegistry*.cs` files (plus deleted
        `PluginState*.cs` counterparts), `src/CLIHub.Core/Plugins/PluginCatalog.cs`,
        `IPluginCatalog.cs`, `PluginInstaller.cs`, `PluginSeeder.cs`,
        `UserModifiedPluginEntry.cs`, `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs`,
        `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs`,
        `BrowsePluginsDialogViewModel.cs`, the test files listed in proposal.md Impact,
        `docs/architecture/plugins.md`, `docs/project-context.md`, `docs/backlog.md`, and
        `openspec/changes/plugin-registry-consolidation/` — anything else is a stop-and-report
        signal
- [ ] 5.12 Report the gate results together with a recommended manual smoke (with CLIHub closed,
        place a populated `%APPDATA%\CLIHub\plugin-state.json` including a `disabled: true` entry
        and delete any `registry.json`; start CLIHub; confirm `registry.json` appears with
        `enabled: false` for the disabled plugin, `plugin-state.json` is deleted, the agent list
        and Disabled section are unchanged; hand-edit an installed plugin's `plugin.json`, click
        Reload Catalog, confirm the warning log and that `GetUserModifiedPlugins` reports it via a
        debug probe or the log line; delete a plugin folder, reload, confirm the registry keeps the
        entry and a built-in is re-seeded while a disabled+deleted built-in is not), then wait for
        the smoke result and ask for explicit `git commit` and `openspec archive` approval
        (approval never carries over; nothing is staged before it)
