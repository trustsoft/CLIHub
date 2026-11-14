# Design: Plugin registry v2 client — dual-format feed, default switch, descriptor-only install

All `file:line` references verified at HEAD `6c103e8` (working tree clean) unless noted. The live
v2 feed was fetched and inspected on 2026-11-14; its observed shape is normative for R-A/R-D and
is recorded first.

## Verified Ground Truth: the live v2 index

`GET https://raw.githubusercontent.com/trustsoft/CLIHub-plugins-v2/master/index.json` → HTTP 200,
12,201 bytes, `schemaVersion: 2`, `lastUpdated: "2026-11-14T01:55:08Z"`, 7 plugins. Observed entry
keys (identical across all 7): `id, name, description, version, compatibility, channel, publisher,
homepage, descriptor, package, logoUrl, releaseNotesUrl, signature`. One entry in full:

```jsonc
{
  "id": "antigravity", "name": "Antigravity", "description": "Google Antigravity CLI (agy)",
  "version": "1.0.0", "compatibility": ">=1.1.0", "channel": "stable", "publisher": "CLIHub",
  "homepage": "https://antigravity.google/docs/cli/overview",
  "descriptor": {
    "schemaVersion": 1, "id": "antigravity", "name": "Antigravity",
    "description": "Google Antigravity CLI (agy)", "version": "1.0.0", "publisher": "CLIHub",
    "homepage": "https://antigravity.google/docs/cli/overview", "minAppVersion": "1.1.0",
    "commands": { "launch": { "executable": "agy" },
                  "resume": { "executable": "agy", "arguments": "--continue" },
                  "version": { "executable": "agy", "arguments": "--version" } },
    "detection": { "systemPaths": ["%USERPROFILE%\\.gemini\\antigravity-cli"] }
  },
  "package": { "url": "https://github.com/.../antigravity-1.0.0.clihub-plugin.zip",
               "sha256": "ff1d3a87…f158" },
  "logoUrl": "https://raw.githubusercontent.com/trustsoft/CLIHub-plugins-v2/master/plugins/antigravity/logo.png",
  "releaseNotesUrl": null, "signature": null
}
```

Facts that drive decisions:

1. **No per-entry `schemaVersion` in v2.** v1 entries carry a required per-entry `schemaVersion`
   (the *descriptor* schema version). In v2 that field lives inside `descriptor` (observed: 1).
2. **The inline descriptors contain `schemaVersion: 1` and `minAppVersion`.** The local `Plugin`
   model has no `MinAppVersion` property (`src/CLIHub.Core/Models/Plugins/Plugin.cs:6-149`) and
   System.Text.Json ignores unmapped properties, so both fields round-trip harmlessly (the
   tolerant-read direction of ADR-0014). This is a **deviation from the brief's field list** (it
   named only id/name/description/version/publisher/homepage/commands/updateCheck/detection) —
   recorded in "Deviations".
3. **All 7 entries currently carry `package`.** Descriptor-only entries (no `package`) do not exist
   in the seed set yet, but the schema permits them (ADR-0015: "descriptor-only plugins install
   without a zip package") and R-D must handle them; tests synthesize them.
4. `compatibility` values observed: `">=1.1.0"`, `">=1.1.1"` (single floor constraints only so
   far). `channel` is `"stable"` on all entries. `releaseNotesUrl`/`signature` are JSON `null`.

## Verified Current State

### `PluginFeedClient` (src/CLIHub.Core/Plugins/PluginFeedClient.cs)

| Element | Location | Fact |
|---|---|---|
| Class | `:15` | `public sealed class PluginFeedClient : IPluginFeedClient, IDisposable` |
| Constants | `:18-20` | `MaxIndexSizeBytes` 2 MB, `MaxPackageSizeBytes` 10 MB, `MaxRedirectHops` 5 |
| Error profiles | `:22-36` | `StructuredErrorProfile` feed/package strings (timeout, request, log messages, levels) |
| Public ctor | `:47-51` | `(IPreferencesStore, ILogger)` → internal ctor with `CreateDefaultHandler()` |
| Internal ctor | `:60-69` | test seam over a stub `HttpMessageHandler`; sets `UserAgent` |
| Handler | `:71-76` | `SocketsHttpHandler { AllowAutoRedirect = false }` (manual redirects) |
| `GetFeedAsync` | `:79-130` | feed URL resolution `:81-84` (disabled → structured error), HTTPS pre-check `:87-91`, one structured-error wrapper `:93-129` around `SendFollowingRedirectsAsync` `:96`, status check `:103-107`, fast-path size `:111-115`, `ReadBoundedAsync` `:117-122`, `ParseFeedIndex` `:124` |
| `DownloadPackageAsync` | `:133-195` | arg + HTTPS pre-check `:135-142`, temp path `:144`, redirects `:149`, bounded copy `:172-178`, SHA-256 verify + delete on mismatch `:181-187` |
| `ExecuteWithStructuredErrorsAsync<T>` | `:211-247` | linked CTS + `CancelAfter(ProbeTimeout)` `:220-221`; timeout / `HttpRequestException` / caller-cancel-rethrow / catch-all in that order |
| `StructuredErrorProfile` | `:260-266` | private record of the per-operation strings |
| `SendFollowingRedirectsAsync` | `:282-326` | GET per hop; terminal non-3xx `:293-297`; 3xx without `Location` terminal (D2 of the prior change) `:299-307`; absolute/relative Location resolution `:309-311`; per-hop HTTPS `:313-318` → "Redirect to non-HTTPS URL rejected"; hop cap `:288,324-325` → "Redirect limit exceeded (maximum 5 hops)" |
| `ReadBoundedAsync` | `:333-357` | streams to string, aborts past `maxBytes` |
| `CopyBoundedAsync` | `:364-386` | streams to file, aborts past `maxBytes` |
| `ParseFeedIndex` | `:388-443` | missing/invalid `schemaVersion` → "Feed index missing or invalid schemaVersion" `:396-402`; **`schemaVersion > 1` → "Unsupported feed schema version {v}"** `:404-408`; optional `lastUpdated` `:411-414`; `plugins` array required `:417-421`; per-entry loop with skip-on-invalid `:426-433`; duplicate-id skip inside `TryParseEntry` `:473-477`; `JsonException` → "Invalid JSON in feed index" `:438-442` |
| `TryParseEntry` (v1) | `:445-490` | required non-empty strings: id, name, description, version, **minAppVersion**, publisher, homepage, **packageUrl, sha256** `:450-458`; required per-entry `schemaVersion` `:464-470`; optional releaseNotesUrl/signature `:480-481`; builds the 12-field record `:483`; any exception → warn + skip `:485-489` |
| `TryGetString` | `:492-505` | non-empty string or failure (whitespace counts as missing) |
| `IsHttpsUrl` | `:507-511` | absolute URI + `https` scheme, case-insensitive |
| `ProbeTimeout` | `:540-541` | `AgentProbePolicy.ResolveTimeout(...AgentProbeTimeoutSeconds)` |

### Feed result model (src/CLIHub.Core/Plugins/PluginFeedResult.cs)

`PluginFeedResult` `:9-12`; `PluginFeedIndex(int SchemaVersion, string? LastUpdated, IReadOnlyList<PluginFeedEntry>)`
`:20-23`; **`PluginFeedEntry`** `:40-52` — 12 positional parameters, all reference types non-nullable
except `ReleaseNotesUrl`/`Signature`; `PluginDownloadResult` `:60-63`. `PluginFeedEntry` is
constructed only by `PluginFeedClient.TryParseEntry:483` and by tests (13 sites in
`BrowsePluginsDialogViewModelTests`, positional).

### `PluginInstaller` (src/CLIHub.Core/Plugins/PluginInstaller.cs)

| Element | Location | Fact |
|---|---|---|
| Constants | `:22-27` | 10 MB archive, 10 MB per entry, 100 files, `plugin.json`, `logo.png`, `.staging-` prefix |
| Ctor | `:49-64` | optional `IPluginDescriptorReader`/`IPluginDescriptorValidator` (default `new`), optional `pluginsPath` — the pattern R-A's feed-client ctor change follows |
| `Inspect` | `:67-80` | try `InspectCore`, catch → `PluginInstallPreview.Invalid(["Package could not be read: {msg}"])` |
| `Install` | `:83-201` | re-validates via `InspectCore` `:88-93` (TOCTOU note `:86-88`); id/target/replace-consent `:95-103`; staging `:105`; **extraction allow-list: only `plugin.json` and `logo.png` are ever written** `:113-132`; missing descriptor check `:134-137`; re-read + re-validate from staged disk `:141-152`; id==folder-name `:154-157`; backup `:161-165`; `BeforeStagingMoveForTests` hook `:167`; atomic move `:169`; delete backup `:171-174`; `RecordInstalledState` `:176`; catalog reload tolerated to fail `:181-188`; catch → `Rollback` + structured errors `:192-200` |
| `InspectCore` | `:203-380` | size/count/symlink/zip-slip/hidden/allow-list checks `:212-289`; single top dir `:292-303`; in-memory descriptor parse via `descriptorReader.ReadContent` `:322`; validator `:334-335`; id==topDirectory `:337-340`; reserved built-in ids `:342-346`; `ReplacesExistingPlugin`/`ExistingPluginVersion` from state `:363-367`; preview via `BuildDeclaredCommands` `:369-379` |
| `RecordInstalledState` | `:382-393` | `PluginOrigin.Installed`, `ContentHash`/`LogoHash` via `PluginHasher.ComputeFileHash` (logo hash null when absent), `InstalledVersion = plugin.Version`, `Disabled = false` |
| `Rollback` | `:395-428` | delete staging; restore backup if it exists |
| `BuildDeclaredCommands` | `:430-455` | launch/resume/version/update → `CommandPreview` |

### Feed URL and comparators

- `FeedUrlProvider.DefaultFeedUrl` `FeedUrlProvider.cs:13` — v1 URL
  `https://raw.githubusercontent.com/trustsoft/CLIHub-plugins/main/index.json`; `GetFeedUrl`
  null → default, whitespace → null (disabled), else custom `:20-41`.
- `AppVersionComparator` `AppVersionComparator.cs:7` — `IsSatisfied(current, minAppVersion)` `:15-31`
  (component-wise `>=`); **`ParseVersion` private** `:33-40`, tolerant: missing segments → 0,
  unparseable segments → 0 (`"1.1"` → (1,1,0)). Tests: `AppVersionComparatorTests.cs` (3 tests).
- `PluginFeedVersionComparator` `PluginFeedVersionComparator.cs` — `Compare` `:41-73`
  (numeric then string-equality fallback), `IsDowngrade` `:83-89`, strict internal
  `TryParseVersion` `:91-107` (requires ≥ 3 parseable segments), `CompareVersions` `:109-122`.

### ViewModel and item (src/CLIHub/)

- `BrowsePluginsDialogViewModel.LoadFeedAsync` `BrowsePluginsDialogViewModel.cs:143-231` —
  built-in filter `:174-178`; status model `:183-196`; **`canInstall = AppVersionComparator.IsSatisfied(currentVersion, entry.MinAppVersion)` `:198`**; `isDowngrade` `:199`; entry→item mapping `:201-215`.
- `InstallPluginAsync` `:233-313` — download `:239`, `Inspect` `:253`, confirmation `:262`,
  `replaceExisting` from status `:269-271`, `Install` `:273`, `MarkInstalled(preview.Version)`
  `:280`, temp cleanup `finally` `:297-311`.
- `FeedPluginItem` `FeedPluginItem.cs` — ctor takes `packageUrl`/`sha256`/`minAppVersion` non-null
  `:33-63`; `BlockedReason` `:246-248` → `"Requires CLIHub {MinAppVersion} or newer. Update CLIHub to install this plugin."`; `MarkInstalled` `:181-188`.
- XAML `src/CLIHub/Views/BrowsePluginsDialog.xaml` binds only display properties
  (`Name` `:110`, `VersionText` `:114`, `ActionText` `:122`, `CanInstall` `:123`,
  `ShowActionButton` `:124`, `Publisher` `:134`, `Homepage` `:143`, `Description` `:152`,
  `WarningMessage` `:161`, `BlockedReason` `:171`, `StatusText` `:187`) — `PackageUrl`, `Sha256`,
  `MinAppVersion` are never bound, so the item-model change is XAML-invisible.

### Logo pipeline (verified: no HTTP logo fetch exists today)

- `PluginLogoResolver.Resolve` `PluginLogoResolver.cs:35-37` resolves through the persistent logo
  cache (`LogoCacheKeys.PluginPrefix + pluginId`) backed by a **filesystem scan only** `:39-49`.
- `ProjectLogoResolver` is also filesystem-only `ProjectLogoResolver.cs:37-52`.
- The only `HttpClient`s in the solution: `PluginFeedClient`'s internal one and the DI singleton
  for `AgentReleaseClient` (`ServiceCollectionExtensions.cs:131-138`). Nothing downloads images
  over HTTP today — the brief's fallback clause ("a plain HTTPS download … is acceptable") applies;
  see D7.
- After a catalog reload, `AgentPaneController.OnPluginsChanged` `AgentPaneController.cs:130-139`
  calls `InvalidateCaches()` `:123-128` → `InvalidateAll(LogoCacheKeys.PluginPrefix)` `:125`, so a
  newly installed `logo.png` is picked up by the existing pipeline without new cache machinery.

### Composition (src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs)

`IPluginDescriptorReader`/`IPluginDescriptorValidator` are registered singletons `:101-102`;
`IPluginInstaller` `:110`; `IPluginFeedClient` `:111`. Adding optional reader/validator parameters
to `PluginFeedClient`'s public ctor needs no registration change (DI resolves them; the
`PluginInstaller` ctor at `PluginInstaller.cs:49-64` is the precedent).

### Descriptor reader/validator

- `PluginDescriptorReader.ReadContent` `PluginDescriptorReader.cs:37-77` deserializes with
  `CoreJson.Options` (camelCase); unmapped JSON properties are ignored (default
  `JsonUnmappedMemberHandling.Skip`), which is what makes the inline `minAppVersion` harmless.
- `PluginDescriptorValidator.Validate` `PluginDescriptorValidator.cs:11-49` — schemaVersion > 1
  rejected `:27-30`, id `:33-36`, name `:38-42`, launch command `:43-46`.

### Tests (counts verified)

- `tests/CLIHub.Core.Tests/Plugins/PluginFeedClientTests.cs` — 24 `[Fact]`s; v1 fixture
  `ValidIndexJson` `:469-490`; `StubHandler` records requests `:492-526`; oversize trio uses
  `NonSeekableReadStream` `:528-569`.
- `tests/CLIHub.Core.Tests/Plugins/PluginInstallerTests.cs` — temp-root + `pluginsPath` override
  `:19-36`, `CreatePackage` helper `:38-51`, `ValidDescriptor` `:57-70`, existing-plugin writer
  `:77-89` (zip-path tests stay untouched by this change).
- `tests/CLIHub.Tests/ViewModels/Agents/BrowsePluginsDialogViewModelTests.cs` — 13 `[Fact]`s; 13
  positional `PluginFeedEntry` constructions (e.g. `:29-32`); `LoadAsync_MinAppVersionUnsatisfied_CanInstallFalse`
  pins the verbatim blocked message `:146`; install tests verify `DownloadPackageAsync`/`Inspect`/`Install`
  call counts and in-place card updates `:214-341`.
- `InternalsVisibleTo` for `CLIHub`, `CLIHub.Tests`, `CLIHub.Core.Tests`, `CLIHub.Core.Windows.Tests`
  (`src/CLIHub.Core/CLIHub.Core.csproj:28-31`) — internal members are test-reachable without
  surface opening.

### Durable contract (openspec/specs/plugin-feed/spec.md)

Requirements: fetch/parse `:8-80`; download+checksum `:82-117`; display `:119-166`; install from
feed `:168-201` (incl. "Browse card updates in place after install" `:196-201`, added by
`2026-11-14-simplify-plugin-feed-flow`); update from feed `:203-221`; min-app gating `:223-245`;
refresh `:247-268`; feed URL config `:270-289`; offline `:291-313`; built-in exclusion `:315-336`;
confirmation dialog `:338-349`; temp cleanup `:351-371`; dialog chrome `:373-405`.

### ADR / roadmap

ADR-0015 `docs/adr/0015-plugin-registry-v2.md`: v2 repo + frozen v1 `:27-31`; v2 entry fields
`:46-49`; client behavior (dual parsing, descriptor-only writes, logo through the existing logo
cache, zip+SHA-256 unchanged, `DefaultFeedUrl` switch) `:51-54`; missing signature valid `:56-57`.
Backlog slice `docs/backlog.md:131-142`.

## Decisions

### D1 — Entry model: extend `PluginFeedEntry` (chosen) over a v2 record pair

`PluginFeedEntry` becomes the single normalized record for both schemas:

```csharp
public sealed record PluginFeedEntry(
    string Id,
    string Name,
    string Description,
    string Version,
    int SchemaVersion,        // descriptor schema version (unchanged meaning; v2: descriptor.schemaVersion ?? 1)
    string? MinAppVersion,    // v1 only; null for v2 — drives the preserved v1 blocked message
    string Publisher,
    string Homepage,
    string? PackageUrl,       // v1, or v2 with package; null for descriptor-only
    string? Sha256,           // v1, or v2 with package; null for descriptor-only
    string? ReleaseNotesUrl,
    string? Signature,
    string Compatibility,     // v1: ">=" + minAppVersion (synthesized); v2: the raw range string
    string? Channel,          // v2 only; null for v1
    string? DescriptorJson,   // v2 only; verbatim raw JSON text of the inline descriptor
    string? LogoUrl);         // v2 only; null for v1
```

**Rationale.** Every consumer treats entries uniformly: status classification reads
`entry.Version`, gating reads one range string, the install branch reads one nullable pair, the
reserved-id filter reads `entry.Id`. A two-record design (`PluginFeedEntryV1`/`V2`, or a record +
discriminated union emulation) pushes `is v2?` branching into the ViewModel, the item model, and
both test suites, and doubles the parse-test matrix, for no consumer that actually wants the
distinction — the only behavioral fork is "has package vs descriptor-only", which the nullable
`PackageUrl`/`Sha256` pair already expresses. The codebase style is single normalized records
(`PluginFeedEntry`, `PluginInstallPreview`, `PluginDownloadResult`).

`Compatibility` is non-nullable and always populated by the parser (v1 through synthesis), so the
ViewModel gates with one call and no defensive null handling. The 13 test constructions are
updated mechanically in the same stage that changes the record (they are positional; each gains
`">=1.0.0"` and three `null`s).

**Alternatives rejected.**
- *Two records + discriminated handling* — branching everywhere; no consumer wants it (above).
- *Keep v1 fields non-nullable and model v2 as a wrapper object on the entry* — the wrapper is
  nullable, so every v1 consumer still needs null handling, and the v2-only fields are one
  indirection further from the ViewModel mapping. Strictly worse than flattening.

### D2 — Entry shape is dictated by the index `schemaVersion`; no mixed-schema indexes

Index `schemaVersion` 0/1 → every entry parses with today's v1 rules (unchanged, plus synthesis);
`schemaVersion` 2 → every entry parses with v2 rules; `> 2` → `"Unsupported feed schema version {v}"`
(same string, new bound — the only index-level change). Missing/invalid `schemaVersion`, missing
`plugins` array, and `JsonException` keep their current strings and outcomes.

**Rationale.** The v2 index is CI-generated from one schema; per-entry sniffing ("has
`descriptor` → v2") would accept malformed hybrid feeds unpredictably and make the skip rules
untestable. Driving by the index version keeps each parse path's required-field set crisp. Values
≤ 0 continue to parse under v1 rules exactly as today (`:404` only rejected `> 1`; the new code
only rejects `> 2`) — no behavior change outside the intended one.

**v2 entry rules** (all skip-with-logged-warning, like invalid v1 entries):
- Required non-empty strings: `id`, `name`, `description`, `version`, `publisher`, `homepage`,
  `compatibility` (and it must parse via `AppCompatibilityRange.TryParse`).
- `descriptor`: required, must be a JSON object. `GetRawText()` is stored verbatim in
  `DescriptorJson`. It must parse via `PluginDescriptorReader.ReadContent` and pass
  `PluginDescriptorValidator.Validate`, and its `id` must equal the entry `id` (ordinal). The
  entry's `SchemaVersion` field = `descriptor.schemaVersion ?? 1` (absent → 1, matching
  `docs/architecture/plugins.md:33-34`; > 1 is rejected by the validator, so such entries skip).
- `channel`: absent, JSON `null`, or non-string → accepted, normalized to `stable`; any other
  string value → skip. (Deviation DEV-1: the brief said only "'stable' accepted, others rejected";
  the tolerant default matches ADR-0014 and the live feed, where the field is always present — the
  tolerant branch simply never fires there.)
- `package`: absent or JSON `null` → descriptor-only entry (`PackageUrl`/`Sha256` null). Present
  as an object → both `url` and `sha256` must be non-empty strings (else skip); they map to
  `PackageUrl`/`Sha256`. HTTPS for `package.url` is enforced at download time, not parse time —
  exactly the v1 split (`:138-142`).
- `logoUrl`, `releaseNotesUrl`, `signature`: optional via existing `TryGetString` semantics.
- Unknown fields ignored in both directions (existing scenario "Index contains unknown fields").

**Why validate the inline descriptor at parse time:** an entry whose descriptor fails validation
is not installable; listing it as installable would move the failure to the confirmation click
with a worse message. The brief's default for invalid entries is "log + skip entry like other
invalid entries"; validating at parse follows it and keeps the Browse list truthful. Cost:
`PluginFeedClient` takes optional `IPluginDescriptorReader`/`IPluginDescriptorValidator`
constructor parameters (defaults `new PluginDescriptorReader()` / `new PluginDescriptorValidator()`,
the `PluginInstaller` ctor pattern at `PluginInstaller.cs:49-60`); DI registration is unchanged.
Metadata drift between entry and descriptor (name/version differing) is tolerated — only `id`
equality is enforced — because the descriptor is what gets installed and confirmed; entry fields
drive display/status (D8 note).

### D3 — Default URL switch

`FeedUrlProvider.DefaultFeedUrl` (`FeedUrlProvider.cs:13`) becomes
`https://raw.githubusercontent.com/trustsoft/CLIHub-plugins-v2/master/index.json` (branch `master`
per ADR-0015 `:27`). `GetFeedUrl` semantics untouched (`:20-41`). Docs updated
(`docs/architecture/plugins.md:177,229-233`, `docs/project-context.md:52-54`). Rollback is the
one-line revert (ADR-0015 `:80`). The frozen v1 repository keeps serving old clients; nothing in
this change touches it.

### D4 — Compatibility ranges: new `AppCompatibilityRange` in `CLIHub.Core/Plugins`

New public static class `AppCompatibilityRange` (file
`src/CLIHub.Core/Plugins/AppCompatibilityRange.cs`):

- `public static bool IsSatisfied(string currentVersion, string compatibility)` — true iff every
  constraint holds.
- `internal static bool TryParse(string compatibility, out IReadOnlyList<(string Op, (int Major, int Minor, int Patch) Version)> constraints)` — used by the feed parser for parse-time range validation and by tests via `InternalsVisibleTo`.

Grammar: `constraint := op version`; `op ∈ { ">=", "<=", ">", "<", "=" }`; constraints separated by
commas and/or whitespace (covers both the brief's "comma-separated" wording and its
space-separated example `">=1.1.0 <2.0.0"` — DEV-3); `version := segment('.'segment){0,2}` with
each segment a non-negative integer (missing minor/patch default 0, mirroring
`AppVersionComparator.ParseVersion`'s tolerance so `">=1.1"` means `">=1.1.0"`); all constraints
ANDed. An unparseable range (bad operator, non-numeric segment, empty) fails `TryParse` — at feed
parse time that skips the entry (D2); called directly it returns `false` (fail closed).

**Version parsing reuse:** `AppVersionComparator.ParseVersion` (`AppVersionComparator.cs:33-40`)
changes from `private` to `internal` (XML doc added; no behavior change) and is used for the
current-version side and for normalizing missing constraint segments after `TryParse`'s own
per-segment `int.TryParse` validation. Comparison semantics are the component-wise compare
`IsSatisfied` already uses (`:20-30`).

**v1 equivalence (the load-bearing property):** for a v1 entry the parser synthesizes
`Compatibility = ">=" + minAppVersion`, so gating becomes
`AppCompatibilityRange.IsSatisfied(current, ">=M")`. This equals
`AppVersionComparator.IsSatisfied(current, M)` by construction — both parse both sides tolerantly
and compare component-wise with `>=`. The equivalence is pinned by unit tests asserting both calls
agree across a truth table, and by `LoadAsync_MinAppVersionUnsatisfied_CanInstallFalse` passing
with its verbatim message assertion. Gating in `LoadFeedAsync` is therefore **one call for both
schemas** (R-E).

**Alternatives rejected.**
- *Reuse `PluginFeedVersionComparator.TryParseVersion`* (strict, 3 segments) — would silently
  change v1 gating for two-segment `minAppVersion` values (strict parse fails → different result
  than today); v1 behavior must be preserved bit-for-bit.
- *A SemVer range library (NuGet)* — new dependency for a 5-operator grammar; violates the
  repo's dependency discipline for no capability gain.
- *Keep `IsSatisfied` for v1 and range for v2 (two call sites)* — works, but leaves two gating
  paths to keep equivalent forever; the synthesized range makes equivalence a parser invariant
  instead of a ViewModel convention.

### D5 — Installer seam: descriptor-based `Inspect`/`Install` on `IPluginInstaller`

New input record (file `src/CLIHub.Core/Plugins/PluginDescriptorInstallSource.cs`):

```csharp
public sealed record PluginDescriptorInstallSource(string DescriptorJson, byte[]? LogoBytes);
```

`IPluginInstaller` gains:

```csharp
PluginInstallPreview InspectDescriptor(PluginDescriptorInstallSource source);
PluginInstallResult InstallDescriptor(PluginDescriptorInstallSource source, bool replaceExisting);
```

`PluginInstaller` implementation:

- **Shared commit core.** `Install`'s tail — existing-exists check, staging, staged-descriptor
  re-read + validation, backup, `BeforeStagingMoveForTests`, atomic move, backup delete,
  `RecordInstalledState`, catalog reload, catch/`Rollback` (`PluginInstaller.cs:95-200`) — is
  extracted into a private method both paths call. The refactor is constrained to be
  behavior-identical for the zip path: **all existing `PluginInstallerTests` must pass unedited in
  the same stage** (that is the gate). Order invariants preserved: replace-consent check before
  staging; validation before backup; backup before move; reload failure never rolls back.
- **`InspectDescriptor`** — `ReadContent(source.DescriptorJson)` (empty/whitespace JSON →
  `PluginInstallPreview.Invalid(["Descriptor JSON is required"])`, a new precondition string for a
  new API), validator, reserved built-in id check (same set and message shape as
  `InspectCore:342-346`), `ReplacesExistingPlugin`/`ExistingPluginVersion` from state
  (`:363-367`), `BuildDeclaredCommands` preview. No files written. The zip-only
  id==folder-name check (`:337-340`) does not apply — the target folder name *is* the descriptor
  id, so no mismatch is representable.
- **`InstallDescriptor`** — creates `.staging-<guid>`, writes `plugin.json` = `DescriptorJson`
  **verbatim** and, when `LogoBytes` is non-null, `logo.png` = those bytes; then re-reads and
  re-validates the staged descriptor from disk through `descriptorReader.Read(stagingPath)` + the
  validator (identical to `Install:141-157` — defense in depth against a mutated source object),
  and runs the shared commit core. Failures produce the same error shapes (`PluginInstallException`
  errors, `"Installation failed: {message}"` fallback).

**Rationale.** The zip path already reduces every package to exactly `plugin.json` + optional
`logo.png` (`:113-132`) — a descriptor-only source is that reduction as direct input. Extending
the interface keeps validation, staging, backup, rollback, state, and catalog reload in the one
place that owns them (the non-negotiable guarantee list), and the confirmation preview is a real
`PluginInstallPreview`, so the dialog code is shared untouched.

**Writing the descriptor verbatim** (not round-tripped through `Plugin` + re-serialization) keeps
byte fidelity with the index — the integrity anchor per ADR-0015 — and preserves fields the local
model does not map (`minAppVersion` inside the descriptor, ground-truth fact 2). Both extra fields
are ignored by `PluginDescriptorReader` (unmapped-member skip), so re-validation and the catalog
load behave identically; `PluginHasher` hashes whatever bytes were written, so state tracking is
exact.

**Alternatives rejected.**
- *Synthesize a temp `.clihub-plugin.zip` and call `Install(packagePath)`* — manufactures an
  artifact the temp-cleanup scenarios (`spec.md:351-371`) then require to be cleaned up, adds
  compression work per install, and still validates nothing that the direct path doesn't; pure
  ceremony.
- *Synthesize a temp folder + a folder-based variant of the existing path* — a folder-based
  variant **is** a new seam (the current one is zip-specific at `InspectCore:225` and `Install:113`),
  but with weaker typing (disk path carrying untrusted bytes, plus a new temp-dir cleanup duty)
  instead of an in-memory record; rejected for the weaker contract.
- *ViewModel writes `plugin.json` directly* — bypasses re-validation, staging, backup, rollback,
  state recording, and the reserved-id check; violates the non-negotiables outright.

### D6 — Confirmation and ViewModel flow for descriptor-only installs

`InstallPluginAsync` branches on the item's install source: `PackageUrl is null` → descriptor path;
otherwise today's zip path, byte-for-byte unchanged. Descriptor path:

1. `preview = installer.InspectDescriptor(new PluginDescriptorInstallSource(item.DescriptorJson!, null))`;
   invalid → `ShowValidationError(preview.Errors)` (same dialog as zip inspection failures).
2. `ShowInstallConfirmation(preview)` — the same dialog, driven by the inline descriptor's
   metadata and declared commands (pinned by a new scenario on the confirmation requirement).
3. On confirm: if `LogoUrl` is present → `feedClient.DownloadLogoAsync` (D7); failure → log
   warning, `LogoBytes = null` (install proceeds — D7 policy). Logo is fetched *after*
   confirmation so a cancelled install performs no network fetch beyond the index.
4. `installer.InstallDescriptor(new PluginDescriptorInstallSource(item.DescriptorJson!, logoBytes), replaceExisting)`
   with `replaceExisting` derived from status exactly as today (`:269-271`).
5. Same outcome dialogs, same `item.MarkInstalled(preview.Version)` in-place card update, same
   catch/cleanup shape. No temp file is created on this path, so the `finally` block is a no-op
   for it (the "Temporary file cleanup" scenarios still hold vacuously for descriptor installs and
   strictly for zip installs).

`FeedPluginItem` changes: `PackageUrl`/`Sha256`/`MinAppVersion` become nullable properties; new
`Compatibility` (non-null), `DescriptorJson`, `LogoUrl`; computed
`UsesDescriptorInstall => DescriptorJson is not null && PackageUrl is null` (used by the ViewModel
branch and tests); `BlockedReason` dual form (D8). XAML untouched (verified inventory above).

### D7 — Logo handling: new `DownloadLogoAsync` on the feed client; failure tolerated

**No HTTP logo fetch exists today** (verified: `PluginLogoResolver.cs:39-49` and
`ProjectLogoResolver.cs:37-52` scan the filesystem; the only HttpClients are `PluginFeedClient`'s
and the `AgentReleaseClient` singleton). The brief's fallback ("a plain HTTPS download … is
acceptable — state it") therefore applies. **Placement deviation (DEV-4):** the download lives on
`IPluginFeedClient`/`PluginFeedClient`, not the installer, because the transport rules it must
obey — HTTPS-only pre-check, `SendFollowingRedirectsAsync` with per-hop HTTPS and the 5-hop cap,
size bound, probe timeout, structured-error wrapper — are all private machinery of the feed client
(`PluginFeedClient.cs:282-326`, `:211-247`). The installer stays synchronous and HTTP-free.

API: `Task<PluginLogoDownloadResult> DownloadLogoAsync(string logoUrl, CancellationToken)` with
`PluginLogoDownloadResult(bool Success, byte[]? LogoBytes, IReadOnlyList<string> Errors)` in
`PluginFeedResult.cs`. Implementation: new `LogoErrorProfile` (new strings "Logo download timed
out" / "Logo download failed" — genuinely new failure mode), bound `byte[]` read via a new private
`ReadBoundedBytesAsync` (byte mirror of `ReadBoundedAsync:333-357`) against a 10 MB bound
(`MaxPackageSizeBytes` — the same bound a `logo.png` inside a zip package may reach, so standalone
logos are not held to a stricter limit than packaged ones). **No checksum**: the index carries no
logo hash, and per ADR-0015 the index in the git repository is the integrity anchor; the logo is
display-only and never executes.

**Failure policy: warn + proceed without the logo.** `logo.png` is optional in every existing path
(zip: optional entry; catalog: default-logo fallback per `docs/architecture/plugins.md:261`), so
blocking a validated descriptor install on a cosmetic fetch would add a user-unactionable failure
mode. The install succeeds, the plugin shows the default logo, a warning is logged. Pinned by a
spec scenario; open question OQ-3 offers the alternative (abort) if the user disagrees.

**"Logo through the existing logo cache"** (ADR-0015 `:52-53`, backlog `:140`): writing `logo.png`
into the installed plugin folder *is* that mechanism — after `InstallDescriptor` the catalog
reloads, `PluginsChanged` fires, `AgentPaneController.InvalidateCaches` clears
`LogoCacheKeys.PluginPrefix` (`AgentPaneController.cs:123-139`), and `PluginLogoResolver` re-scans
the folder (`PluginLogoResolver.cs:35-49`). No new cache type is introduced.

### D8 — Gating display and error-string policy

`BlockedReason` (`FeedPluginItem.cs:246-248`):

- v1 (`MinAppVersion` non-null): **verbatim** `"Requires CLIHub {MinAppVersion} or newer. Update CLIHub to install this plugin."` — preserved string, pinned by the unchanged scenario and test.
- v2 (`MinAppVersion` null): `"Requires CLIHub {Compatibility}. Update CLIHub to install this plugin."` — a new string for a genuinely new behavior (range display), the naming/wording is open question OQ-2.

**Preserved-verbatim inventory** (user-facing strings this change must not alter): all feed
transport strings ("Feed is disabled in configuration", "Feed URL must use HTTPS", "Could not load
plugin feed", "Feed index too large", "Feed request timed out", "Could not load plugin feed. Check
your network connection.", redirect strings, "Feed index missing or invalid schemaVersion",
"Feed index missing or invalid plugins array", "Invalid JSON in feed index"), the package strings
("Package download timed out/failed", "Package too large", "Package URL must use HTTPS",
"Package checksum mismatch. The downloaded file may be corrupted or tampered with."), the
"Unsupported feed schema version X" string (its bound moves 1 → 2 — that is R-A's point, pinned by
the modified scenario), every installer error string (shared core untouched), and the v1 blocked
message above. **New strings** are limited to the v2 blocked message (D8), the two logo-download
structured errors (D7), and the `InspectDescriptor`/`InstallDescriptor` empty-JSON precondition
"Descriptor JSON is required" (D5) — each attached to behavior that did not exist.

### D9 — Signature and channel policy (from the brief's non-goals)

`signature` is parsed as an optional string exactly as today (`TryParseEntry:481`) and **never
verified**; missing/null is valid (ADR-0015 `:56-57`). `channel` handling is exactly D2's rule:
nothing channel-aware ships in this slice beyond the parse-time filter.

## Non-Negotiable Guarantees — where each one lives after the change

| Guarantee | Keeper |
|---|---|
| HTTPS-only + per-hop HTTPS + 5-hop cap for ALL fetches (index, package, **logo**) | `IsHttpsUrl` pre-checks; `SendFollowingRedirectsAsync` reused by all three (`PluginFeedClient.cs:282-326`) |
| 2 MB index bound | unchanged `:18,111-122` |
| 10 MB package bound | unchanged `:19,164-178` |
| 10 MB logo bound (new) | `ReadBoundedBytesAsync` against `MaxPackageSizeBytes` (D7) |
| SHA-256 for zip installs | `DownloadPackageAsync:181-187` untouched |
| Installer re-validation, staging, backup, rollback | shared commit core (D5); zip behavior pinned by untouched `PluginInstallerTests` |
| Confirmation dialog shape (all declared commands) | same `PluginInstallPreview` + `ShowInstallConfirmation` for both sources (D6) |
| Existing user-facing error strings | D8 inventory |
| v1 feeds work end-to-end | v1 parse branch unchanged + synthesis equivalence (D4); default-URL switch does not touch custom URLs (D3) |

## Deviations from the Brief

- **DEV-1 channel tolerance.** Brief: "accepting `stable` and rejecting others at parse time".
  Implementation: absent/null/non-string channel is *tolerated as stable*; only a non-`stable`
  string skips. Matches the codebase's tolerant-read direction (ADR-0014); the live feed always
  carries `"stable"`, so the strict reading and the tolerant one differ only on malformed feeds.
- **DEV-2 live-payload field surprise.** Brief's v2 descriptor field list omitted `schemaVersion`
  and `minAppVersion`, both present in the live index's inline descriptors. Design consumes
  `schemaVersion` (entry `SchemaVersion`) and preserves `minAppVersion` verbatim through install
  (ignored by the local model; D5).
- **DEV-3 range separators.** Brief text says "comma-separated", its example uses a space
  (`">=1.1.0 <2.0.0"`). The evaluator accepts commas and/or whitespace as separators.
- **DEV-4 logo download placement.** Brief's fallback allowed "a plain HTTPS download in the
  installer"; placed on `IPluginFeedClient` instead so the new fetch inherits the existing
  transport-rule machinery (D7). Same guarantee set; the installer remains HTTP-free.

## Implementation Notes (recorded during execution)

- **4.11a fixture count and one non-mechanical value.** The ViewModel test file holds **15**
  positional `PluginFeedEntry` constructions, not the 13 the brief/design counted; all 15 were
  extended. The design's blanket instruction "each gains `\">=1.0.0\"`" is wrong for exactly one
  fixture: `LoadAsync_MinAppVersionUnsatisfied_CanInstallFalse` uses `minAppVersion "99.0.0"`, so
  its synthesized compatibility is `">=99.0.0"` (the v1 synthesis invariant `">=" + minAppVersion`).
  All other fixtures keep `">=1.0.0"` matching their `"1.0.0"` floor. No behavior assertion changed.
- **Logo oversize error string.** Task 3.3's fast-path/bounded-read rejections return
  `"Logo download failed"` (with a distinct log message "Logo too large: …" / "Downloaded logo
  exceeds the … byte limit") instead of introducing a third new string: design D8 limits new
  user-facing strings to the v2 blocked message, the two logo structured errors, and
  `"Descriptor JSON is required"` — `"Logo URL must use HTTPS"` is additionally sanctioned by
  task 3.3 and the spec delta's "Logo URL must use HTTPS" scenario. Test (e)
  `DownloadLogoAsync_OverLimit_ReturnsError` asserts that string.
- **Task 2.6 test count.** The "(a)–(g)" bullets name **8** test methods (bullet (b) names two);
  the Core.Tests suite grew by exactly 8 installer tests, plus 43 range tests (Stage 1), 7 logo
  tests (Stage 3), 8 v2 feed-client tests (4.10), and 4 ViewModel tests (4.11b–e).
- **Verifier finding: v2 duplicate-id claim ordering.** `TryParseEntryV2` called `seenIds.Add(id)`
  before descriptor/channel/package validation, so in a feed `[A(invalid), A(valid)]` the valid
  entry was skipped as a duplicate. Fix: the id is claimed immediately before the successful
  return (matching the v1 ordering) — pinned by
  `GetFeedAsync_V2DuplicateIdAfterInvalidEntry_KeepsValidEntry`.
- **OQ-1..4 resolutions honored:** only the `plugin-feed` delta (OQ-1); the v2 blocked message is
  the raw-range form `"Requires CLIHub {compatibility}. Update CLIHub to install this plugin."`
  (OQ-2); logo failure warns and installs without `logo.png` (OQ-3); logo bound = 10 MB via
  `MaxPackageSizeBytes` (OQ-4).

## Open Questions

- **OQ-1** Should the descriptor install seam also be pinned in the `plugin-installation`
  capability (a second delta directory), or is `plugin-feed` sufficient (current plan — the feed
  is the only descriptor-source caller, and the brief scoped deltas to `specs/plugin-feed/`)?
- **OQ-2** v2 blocked message wording: `"Requires CLIHub {compatibility}. Update CLIHub to install
  this plugin."` renders e.g. "Requires CLIHub >=1.1.0 <2.0.0. …". Acceptable, or prefer a
  humanized form (e.g. "Requires CLIHub 1.1.0–2.0.0")? Humanizing adds a formatter with its own
  test surface; the raw-range form is proposed for truthfulness.
- **OQ-3** Logo-failure policy: proceed-without-logo (chosen, D7) vs abort the install. The spec
  delta pins "proceed"; flipping it later is a one-scenario modification.
- **OQ-4** Logo size bound: 10 MB (package parity, chosen). A tighter bound (e.g. 2 MB) would
  reject pathological images earlier but introduces a limit no packaged logo is subject to today.
