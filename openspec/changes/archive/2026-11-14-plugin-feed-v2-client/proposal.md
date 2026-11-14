# Plugin registry v2 client: dual-format feed, default switch, descriptor-only install

## Why

ADR-0015 (`docs/adr/0015-plugin-registry-v2.md`, accepted 2026-11-14) moved the plugin registry to
a new repository `trustsoft/CLIHub-plugins-v2` whose CI-generated index is schema version 2. The
v1 repository is frozen; it keeps serving already-shipped clients but receives no new plugins. The
client today cannot consume any of that:

- `PluginFeedClient.ParseFeedIndex` rejects every `schemaVersion` above 1
  (`src/CLIHub.Core/Plugins/PluginFeedClient.cs:404-408`), so the v2 index is unusable.
- `FeedUrlProvider.DefaultFeedUrl` still points at the frozen v1 index
  (`src/CLIHub.Core/Plugins/FeedUrlProvider.cs:13`), so new plugins published only to v2 are
  invisible.
- Every feed install requires a zip download + SHA-256 step, although most plugins are a single
  JSON descriptor plus an optional logo; v2 carries the descriptor inline precisely so those
  plugins can install without a package (`docs/adr/0015-plugin-registry-v2.md:46-48`).
- Compatibility gating is a single `minAppVersion` floor
  (`src/CLIHub/ViewModels/Agents/BrowsePluginsDialogViewModel.cs:198`); v2 replaces it with a
  `compatibility` range (e.g. `>=1.1.0 <2.0.0`).

The v2 feed is live and verified: `https://raw.githubusercontent.com/trustsoft/CLIHub-plugins-v2/master/index.json`
serves HTTP 200 (12,201 bytes, 2026-11-14) with 7 seeded entries. This change is roadmap slice 1 of
the registry v2 backlog (`docs/backlog.md:131-142`).

## What Changes

Five items, R-A–R-E. None weakens a security guarantee; the full guarantee inventory with the code
that keeps each one is in `design.md`.

- **R-A — dual-format parsing.** `ParseFeedIndex` accepts index `schemaVersion` 1 and 2 (rejects
  > 2 with the existing "Unsupported feed schema version X" error). One normalized
  `PluginFeedEntry` covers both shapes: v1 → packageUrl/sha256/minAppVersion as today plus a
  synthesized `compatibility` of `">=" + minAppVersion`; v2 → inline `descriptor` (raw JSON),
  optional `package {url, sha256}`, `compatibility`, `channel`, `logoUrl`. Invalid v2 entries
  (missing required fields, malformed range, non-`stable` channel, invalid inline descriptor,
  descriptor/entry id mismatch, incomplete `package`) are skipped with a logged warning, exactly
  like invalid v1 entries. Entry shape follows the *index* schemaVersion; no per-entry sniffing.
- **R-B — compatibility ranges.** New `AppCompatibilityRange` in `CLIHub.Core/Plugins`: constraint
  list `>=|<=|>|<|= major[.minor[.patch]]`, constraints separated by commas and/or whitespace,
  ANDed together; reuses `AppVersionComparator`'s tolerant version parsing (private → internal).
  v1 entries gate identically to today because their synthesized range `">=min"` is evaluated with
  the same parser and comparison (equivalence argument in `design.md` D4).
- **R-C — default feed switch.** `FeedUrlProvider.DefaultFeedUrl` becomes the v2 index URL. The
  `PluginFeedUrl` semantics are untouched (null → default, `""` → disabled, non-empty → custom
  HTTPS). Custom v1 feeds keep working end-to-end through the v1 parsing branch.
- **R-D — descriptor-only install.** When a v2 entry has no `package`, the plugin installs
  directly from the inline descriptor: no download, no checksum (the index in the git repository is
  the integrity anchor per ADR-0015). The new `IPluginInstaller.InspectDescriptor`/`InstallDescriptor`
  seam stages the verbatim descriptor JSON (+ optional `logo.png` bytes) under the same
  `.staging-<guid>` directory, re-validates from disk through `PluginDescriptorReader` +
  `PluginDescriptorValidator`, and shares the existing backup / atomic-move / rollback / state /
  catalog-reload commit path with zip installs. The confirmation dialog is a normal
  `PluginInstallPreview` built from the inline descriptor. Logos are fetched by a new
  `IPluginFeedClient.DownloadLogoAsync` (HTTPS-only, manual redirects with the 5-hop cap and
  per-hop HTTPS, 10 MB bound, probe timeout); a failed logo fetch logs a warning and proceeds
  without the logo — `logo.png` is optional everywhere else too.
- **R-E — ViewModel gating.** `BrowsePluginsDialogViewModel.LoadFeedAsync` gates `CanInstall`
  through `AppCompatibilityRange.IsSatisfied(current, entry.Compatibility)`; statuses compute
  exactly as today (`entry.Version` drives `Compare`/`IsDowngrade`); `BlockedReason` keeps the
  verbatim v1 message when `minAppVersion` is present and uses a new range message for v2 entries.
  Zip vs descriptor-only is invisible to the user: same cards, same statuses, same dialogs, same
  in-place card update after install.

Tests: new `AppCompatibilityRange` unit tests; new v2-index and descriptor-only
`PluginFeedClientTests`; new installer descriptor-seam tests in `PluginInstallerTests` (existing
zip tests untouched, proving the refactor is behavior-identical); `BrowsePluginsDialogViewModelTests`
gain a mechanical constructor-argument update (the `PluginFeedEntry` record grows fields) plus
v2-load and descriptor-only-install tests. All 24 existing feed-client tests and 13 existing
ViewModel behavior assertions pass unchanged apart from fixture arguments.

## Capabilities

### Modified Capability: `plugin-feed`

Delta in `specs/plugin-feed/spec.md` (this change):

- **MODIFIED "Fetch and parse feed index"** — dual-schema validation; new scenarios for the v2
  index, descriptor-only entries, non-stable channel skip, malformed range skip, invalid inline
  descriptor skip. All 11 original scenario names preserved; "Index schema version unsupported"
  moves the bound from 1 to 2 with the same error string.
- **MODIFIED "Display available plugins with status"** — new scenario pinning that descriptor-only
  entries are displayed identically to package entries; originals untouched.
- **MODIFIED "Install plugin from feed"** — requirement text broadened to both install sources;
  new scenarios for descriptor-only install (no download, same guarantees, cancelled, logo-failure
  tolerated). All 4 original scenarios preserved verbatim, including "Browse card updates in place
  after install" (added by `2026-11-14-simplify-plugin-feed-flow`).
- **MODIFIED "Update plugin from feed"** — new descriptor-only update scenario; originals
  preserved.
- **MODIFIED "Minimum app version gating"** — requirement text broadened to compatibility ranges;
  new range scenarios; all 3 original scenarios (including the verbatim v1 blocked message)
  preserved.
- **MODIFIED "Feed URL configuration"** — "Feed URL configuration absent or null" now pins the v2
  default URL; the other two scenarios are unchanged.
- **MODIFIED "Confirmation dialog shows all declared commands"** — new scenario pinning that the
  descriptor-only confirmation lists the inline descriptor's commands.
- **ADDED "Logo download for descriptor-only installs"** — transport rules for the new fetch and
  the tolerated-failure policy.

No other capability changes. The installer seam is consumed only by the feed flow; its behavior is
pinned from the `plugin-feed` side (open question OQ-1 asks whether a parallel `plugin-installation`
delta is wanted).

## Impact

**Modified code:**

- `src/CLIHub.Core/Plugins/PluginFeedResult.cs` — `PluginFeedEntry` grows
  `Compatibility`/`Channel`/`DescriptorJson`/`LogoUrl`; `MinAppVersion`/`PackageUrl`/`Sha256`
  become nullable; new `PluginLogoDownloadResult`.
- `src/CLIHub.Core/Plugins/PluginFeedClient.cs` — dual-format parsing, optional descriptor
  reader/validator constructor parameters, `DownloadLogoAsync`, bounded byte read helper.
- `src/CLIHub.Core/Plugins/FeedUrlProvider.cs` — default URL value.
- `src/CLIHub.Core/Plugins/AppVersionComparator.cs` — `ParseVersion` private → internal (no
  behavior change).
- `src/CLIHub.Core/Plugins/AppCompatibilityRange.cs` — new.
- `src/CLIHub.Core/Plugins/PluginDescriptorInstallSource.cs` — new.
- `src/CLIHub.Core/Plugins/IPluginInstaller.cs`, `PluginInstaller.cs` — descriptor-based
  Inspect/Install; shared commit core extracted from `Install`.
- `src/CLIHub.Core/Plugins/IPluginFeedClient.cs` — `DownloadLogoAsync`.
- `src/CLIHub/ViewModels/Agents/FeedPluginItem.cs` — nullable package fields, new descriptor
  fields, `UsesDescriptorInstall`, dual-form `BlockedReason`.
- `src/CLIHub/ViewModels/Agents/BrowsePluginsDialogViewModel.cs` — range gating, entry→item
  mapping, descriptor-only install branch.
- Tests: `tests/CLIHub.Core.Tests/Plugins/AppCompatibilityRangeTests.cs` (new),
  `PluginFeedClientTests.cs`, `PluginInstallerTests.cs`,
  `tests/CLIHub.Tests/ViewModels/Agents/BrowsePluginsDialogViewModelTests.cs`.
- Docs: `docs/architecture/plugins.md` (feed section: default URL, v2 schema, ranges,
  descriptor-only flow, logo handling), `docs/project-context.md` (runtime facts paragraph).

**Unchanged:** zip parsing and the whole zip install path's observable behavior; `IPluginDialogService`
and both dialogs; the Browse Plugins XAML (verified binding inventory in `design.md`); DI
registrations; configuration and the `PluginFeedUrl` semantics; `PluginCatalog`,
`LocalFolderPluginSource`, `PluginLogoResolver`, seeding, state schema.

**Verification:** `dotnet build CLIHub.sln` + `dotnet test CLIHub.sln` after every stage;
`openspec validate --all`; `graphify update .` at the end; a manual Browse-Plugins smoke against
the live v2 feed is recommended before commit/archive.

## Non-Goals

- No background sync, no index caching, no ETag/If-None-Match (later backlog slice).
- No local registry.json consolidation (later backlog slice).
- No signature verification: `signature` and `index.sig` stay reserved; a missing signature is
  treated as valid (ADR-0015 signing slice).
- No changes to the v1 repository, its entries, or the frozen index's availability to old clients.
- No channel support beyond accepting `stable` (absent/null tolerated as stable) and skipping
  anything else at parse time.
- No zip-package format changes, no changes to package validation, and no weakening of HTTPS-only,
  redirect, hop-cap, size-bound, SHA-256, staging, backup, rollback, or confirmation-dialog rules.
- No v1→v2 migration of custom `pluginFeedUrl` values; custom feeds continue to be fetched exactly
  as configured.

## Dependencies

- Base: HEAD `6c103e8` (ADR-0015 + roadmap docs). No other active changes (`openspec list` reports
  none). No file overlap with concurrent work.
- Consumes, does not modify: `PluginDescriptorReader`, `PluginDescriptorValidator`,
  `EmbeddedPluginResources`, `PluginHasher`, `PluginCatalog`, logo cache pipeline.
- Blocked-by: the v2 repository bootstrap (out-of-repo; complete — the feed is live and verified).
- Blocks: later registry v2 slices (registry consolidation, background sync, signing) can proceed
  after this lands.

## Risk

- **Gating regression for v1 feeds (R-B):** the synthesized-range path must reproduce
  `AppVersionComparator.IsSatisfied` exactly; mitigated by equivalence unit tests and the unchanged
  `LoadAsync_MinAppVersionUnsatisfied_CanInstallFalse` test with its verbatim message assertion.
- **Zip install path regression (R-D refactor):** extracting the shared commit core touches
  `PluginInstaller.Install`; mitigated by requiring all existing `PluginInstallerTests` to pass
  unedited in the same stage, plus new tests pinning the descriptor path against the same
  staging/backup/rollback invariants (`BeforeStagingMoveForTests` hook).
- **Parser divergence from the live payload (R-A):** the real index carried surprises (inline
  descriptors contain `schemaVersion` and `minAppVersion`); the design pins the verified payload
  shape and the fixture mirrors it; unknown fields are tolerated on both sides.
- **Default switch strands users on a broken v2 feed (R-C):** rollback is the one-line
  `DefaultFeedUrl` revert per ADR-0015; the smoke task exercises the live feed before release.
