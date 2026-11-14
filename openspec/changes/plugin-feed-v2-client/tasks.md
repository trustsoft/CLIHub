# Tasks: Plugin registry v2 client — dual-format feed, default switch, descriptor-only install

Conventions for every edit: file-scoped namespace first, `using` directives after it, nullable
reference types, `camelCase` private fields without underscore prefix, `this.` qualification,
braces required, XML docs on new public members, English only, test names in the
`MethodOrScenario_Condition_ExpectedResult` pattern. All `file:line` references are pre-change
positions at HEAD `6c103e8`; when earlier tasks in the same file shift lines, anchor by the named
symbol. Per `AGENTS.md`, run GitNexus impact analysis
(`node .gitnexus/run.cjs impact "<symbol>" --direction upstream --repo .`) before the first edit of
`AppVersionComparator`, `PluginFeedClient`, `PluginFeedResult.cs`, `IPluginInstaller`,
`PluginInstaller`, `IPluginFeedClient`, `FeedUrlProvider`, `BrowsePluginsDialogViewModel`, and
`FeedPluginItem`, and heed HIGH/CRITICAL/UNKNOWN warnings before proceeding (a text search must
back any `UNKNOWN`). No git write commands, no `git commit`, no `openspec archive` in these tasks.

Reference decision IDs (D1–D9) and deviations (DEV-1–DEV-4) live in `design.md`.

## Stage 1: R-B — `AppCompatibilityRange` evaluator (Core only)

- [ ] 1.1 Edit `src/CLIHub.Core/Plugins/AppVersionComparator.cs`: change `ParseVersion`
       (`:33-40`) from `private static` to `internal static`, add an XML doc note that it parses
       tolerantly (missing or unparseable segments become 0) and that `AppCompatibilityRange`
       reuses it; no behavior change
- [ ] 1.2 Create `src/CLIHub.Core/Plugins/AppCompatibilityRange.cs`: `public static class
       AppCompatibilityRange` with XML docs implementing design D4 —
       `public static bool IsSatisfied(string currentVersion, string compatibility)`: returns
       false when the range does not parse (fail closed) or any constraint is violated; and
       `internal static bool TryParse(string compatibility, out IReadOnlyList<(string Op, (int
       Major, int Minor, int Patch) Version)> constraints)`: trims the input; splits constraints
       on commas and/or whitespace (DEV-3); each constraint must start with one of the operators
       `">=", "<=", ">", "<", "="` (longest match first) followed by 1–3 dot-separated
       non-negative integer segments (missing minor/patch → 0 via `int.TryParse` per segment,
       mirroring `AppVersionComparator.ParseVersion` tolerance); anything else fails the parse;
       comparison is component-wise using the same ordering `AppVersionComparator.IsSatisfied`
       uses (`AppVersionComparator.cs:20-30`)
- [ ] 1.3 Create `tests/CLIHub.Core.Tests/Plugins/AppCompatibilityRangeTests.cs` with at least:
       single `>=` satisfied/unsatisfied; upper bound `">=1.1.0 <2.0.0"` satisfied at 1.5.0 and
       unsatisfied at 2.0.0 (strict `<`); `"<="` and `">"` and `"="` operators; comma-separated
       list `">=1.1.0,<2.0.0"`; mixed separators `">=1.1.0, <2.0.0"`; two-segment version
       `">=1.1"` equivalent to `">=1.1.0"`; equivalence theory against
       `AppVersionComparator.IsSatisfied(current, min)` for `">=" + min` over a truth table
       (design D4); invalid operator (`"~1.0.0"`), non-numeric segment (`">=1.x"`), empty and
       whitespace-only input → `TryParse` false and `IsSatisfied` false
- [ ] 1.4 Gate (Stage 1): `dotnet build CLIHub.sln` (0 errors, 0 warnings) and
       `dotnet test CLIHub.sln` (0 failed) — the only delta versus the pre-change baseline is the
       new test class; no existing test may change

## Stage 2: R-D installer seam — descriptor-based Inspect/Install (Core only)

- [ ] 2.1 Create `src/CLIHub.Core/Plugins/PluginDescriptorInstallSource.cs`:
       `public sealed record PluginDescriptorInstallSource(string DescriptorJson, byte[]? LogoBytes)`
       with XML docs (verbatim descriptor JSON from a v2 feed entry; optional logo bytes; the
       feed-side source of a descriptor-only install)
- [ ] 2.2 Edit `src/CLIHub.Core/Plugins/IPluginInstaller.cs`: add
       `PluginInstallPreview InspectDescriptor(PluginDescriptorInstallSource source)` and
       `PluginInstallResult InstallDescriptor(PluginDescriptorInstallSource source, bool
       replaceExisting)` with XML docs mirroring `Inspect`/`Install` (`:13-25`) and noting the
       descriptor-only path writes only `plugin.json` (+ optional `logo.png`) with no zip and no
       checksum step (the feed index is the integrity anchor per ADR-0015)
- [ ] 2.3 Edit `src/CLIHub.Core/Plugins/PluginInstaller.cs`: extract the commit tail of `Install`
       (`:95-200` — replace-consent check, staging creation, staged-descriptor re-read +
       validation, backup, `BeforeStagingMoveForTests`, atomic move, backup delete,
       `RecordInstalledState`, catalog reload, catch → `Rollback`) into a private method shared by
       `Install` and `InstallDescriptor`, preserving the current order invariants exactly
       (consent before staging; validation before backup; backup before move; reload failure never
       rolls back; error shapes via `PluginInstallException` and the
       `"Installation failed: {message}"` fallback). **Constraint:** this is a behavior-identical
       refactor for the zip path — no existing `PluginInstallerTests` file content may change in
       this stage
- [ ] 2.4 Edit `src/CLIHub.Core/Plugins/PluginInstaller.cs`: implement `InspectDescriptor` per
       design D5 — guard null/whitespace `DescriptorJson` with
       `PluginInstallPreview.Invalid(["Descriptor JSON is required"])` (new precondition string,
       new API); `descriptorReader.ReadContent`; read error → invalid preview with the read error
       message (same shape as `InspectCore:322-331`); `descriptorValidator.Validate`; reserved
       built-in id rejection using the same set and message shape as `InspectCore:342-346`;
       `ReplacesExistingPlugin`/`ExistingPluginVersion` from state like `:363-367`; success
       preview via `BuildDeclaredCommands`. No files written. The zip-only id==folder-name check
       does not apply (the target folder is derived from the descriptor id)
- [ ] 2.5 Edit `src/CLIHub.Core/Plugins/PluginInstaller.cs`: implement `InstallDescriptor` per
       design D5 — `InspectDescriptor`-equivalent validation first (defense in depth), then
       create the `.staging-<guid>` directory, write `plugin.json` with the **verbatim**
       `DescriptorJson` and `logo.png` from `LogoBytes` when non-null (only these two files, ever),
       re-read + re-validate the staged descriptor from disk through
       `descriptorReader.Read(stagingPath)` + validator (identical to `Install:141-157`), then run
       the shared commit core from 2.3
- [ ] 2.6 Edit `tests/CLIHub.Core.Tests/Plugins/PluginInstallerTests.cs`: add descriptor-seam
       tests using the existing `CreateInstaller`/`WriteExistingPlugin`/`ValidDescriptor` helpers —
       (a) `InspectDescriptor_ValidDescriptor_ReturnsPreviewWithAllDeclaredCommands` (mirrors the
       zip `Inspect` assertions `:97-115`; asserts no files were created); (b)
       `InspectDescriptor_MissingLaunchCommand_Rejected` and
       `InspectDescriptor_BuiltInId_Rejected`; (c)
       `InstallDescriptor_ValidSource_WritesDescriptorAndLogoAndRecordsState` — asserts
       `plugins/<id>/plugin.json` content equals the source JSON **byte-for-byte** (verbatim pin,
       including a `minAppVersion` field inside the descriptor that the local model ignores),
       `logo.png` equals the logo bytes, state entry has origin `installed` + contentHash +
       logoHash + installedVersion from the descriptor, catalog `ReloadPlugins` called once; (d)
       `InstallDescriptor_DescriptorOnlyNoLogo_RecordsNullLogoHash`; (e)
       `InstallDescriptor_ExistingPluginNoConsent_RejectedWithoutWrites`; (f)
       `InstallDescriptor_MoveFailure_RollsBackAndRestoresBackup` (throw from
       `BeforeStagingMoveForTests` against a pre-written existing plugin, mirroring the existing
       zip rollback test); (g) `InstallDescriptor_EmptyDescriptorJson_ReturnsStructuredError`
- [ ] 2.7 Gate (Stage 2): `dotnet build CLIHub.sln` (0 errors, 0 warnings) and
       `dotnet test CLIHub.sln` (0 failed) — every pre-existing `PluginInstallerTests` test passes
       **unedited** (proves the 2.3 refactor is behavior-identical); the suite grows by exactly
       the seven new tests from 2.6; any other count delta is a stop-and-report signal

## Stage 3: R-D logo fetch — `DownloadLogoAsync` on the feed client (Core only)

- [ ] 3.1 Edit `src/CLIHub.Core/Plugins/PluginFeedResult.cs`: add
       `public sealed record PluginLogoDownloadResult(bool Success, byte[]? LogoBytes,
       IReadOnlyList<string> Errors)` with XML docs (display-only asset; no checksum — the index
       is the integrity anchor per ADR-0015)
- [ ] 3.2 Edit `src/CLIHub.Core/Plugins/IPluginFeedClient.cs`: add
       `Task<PluginLogoDownloadResult> DownloadLogoAsync(string logoUrl, CancellationToken
       cancellationToken = default)` with XML docs noting HTTPS-only, manual redirects with the
       5-hop cap and per-hop HTTPS, the 10 MB bound, and the probe timeout
- [ ] 3.3 Edit `src/CLIHub.Core/Plugins/PluginFeedClient.cs`: add a `LogoErrorProfile`
       (`TimeoutError: "Logo download timed out"`, `RequestError: "Logo download failed"`, log
       messages and Warning/Request levels mirroring the feed profile — new strings, new failure
       mode, design D7) next to `:22-36`; add `private static async Task<byte[]?>
       ReadBoundedBytesAsync(HttpContent content, int maxBytes, CancellationToken
       cancellationToken)` as the byte-array mirror of `ReadBoundedAsync:333-357` (null past the
       bound); implement `DownloadLogoAsync` — null/HTTPS pre-check with `IsHttpsUrl` (error
       "Logo URL must use HTTPS", new string), `ExecuteWithStructuredErrorsAsync` wrapper with
       `SendFollowingRedirectsAsync` (`HttpCompletionOption.ResponseHeadersRead`), status check,
       fast-path ContentLength rejection, and the bounded byte read against
       `MaxPackageSizeBytes` (10 MB, design D7)
- [ ] 3.4 Edit `tests/CLIHub.Core.Tests/Plugins/PluginFeedClientTests.cs`: add logo tests using
       the existing `StubHandler`/`EnqueueFeed` helpers — (a)
       `DownloadLogoAsync_HttpsLogo_ReturnsBytes` (single GET asserted); (b)
       `DownloadLogoAsync_HttpLogoUrl_ReturnsErrorBeforeAnyRequest`; (c)
       `DownloadLogoAsync_RedirectToHttp_IsRejected` ("Redirect to non-HTTPS URL rejected"); (d)
       `DownloadLogoAsync_RedirectToHttps_IsFollowed` (2 requests, second on the redirected URL);
       (e) `DownloadLogoAsync_OverLimit_ReturnsError` (chunked oversize body via
       `NonSeekableReadStream`); (f) `DownloadLogoAsync_NetworkFailure_ReturnsStructuredError`
       ("Logo download failed"); (g)
       `DownloadLogoAsync_Timeout_ReturnsStructuredError` ("Logo download timed out")
- [ ] 3.5 Gate (Stage 3): `dotnet build CLIHub.sln` (0 errors, 0 warnings) and
       `dotnet test CLIHub.sln` (0 failed); suite grows by exactly the seven new tests from 3.4

## Stage 4: R-A + R-C + R-E — dual-format parsing, default URL, ViewModel wiring

- [ ] 4.1 Edit `src/CLIHub.Core/Plugins/PluginFeedResult.cs`: reshape `PluginFeedEntry`
       (`:40-52`) to the D1 form — `MinAppVersion`, `PackageUrl`, `Sha256` become `string?`; add
       `string Compatibility` (non-null: v1 synthesized `">=" + minAppVersion`, v2 raw range),
       `string? Channel`, `string? DescriptorJson`, `string? LogoUrl`; update the record's XML
       docs, including that `SchemaVersion` is the *descriptor* schema version (v2:
       `descriptor.schemaVersion ?? 1`)
- [ ] 4.2 Edit `src/CLIHub.Core/Plugins/PluginFeedClient.cs`: add optional
       `IPluginDescriptorReader? descriptorReader = null, IPluginDescriptorValidator?
       descriptorValidator = null` parameters to the public ctor (`:47-51`, defaults
       `new PluginDescriptorReader()` / `new PluginDescriptorValidator()`, the
       `PluginInstaller.cs:49-60` pattern) storing them in readonly fields for parse-time v2
       validation; extend the internal test ctor (`:60-69`) the same way; DI registration in
       `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs` is **not** touched (reader and
       validator are registered `:101-102`)
- [ ] 4.3 Edit `src/CLIHub.Core/Plugins/PluginFeedClient.cs` `ParseFeedIndex` (`:388-443`): change
       the rejection bound from `schemaVersion > 1` to `schemaVersion > 2` (same error string and
       log, `:404-408`); keep everything else identical; pass the index `schemaVersion` into the
       per-entry loop so `TryParseEntry` dispatches on it (v1/v2 rules per design D2 — no
       per-entry sniffing)
- [ ] 4.4 Edit `src/CLIHub.Core/Plugins/PluginFeedClient.cs` `TryParseEntry` (v1 branch,
       `:445-490`): keep every current rule verbatim; additionally set
       `Compatibility = ">=" + minAppVersion`, and `Channel`/`DescriptorJson`/`LogoUrl` to null
       (synthesis per design D4)
- [ ] 4.5 Edit `src/CLIHub.Core/Plugins/PluginFeedClient.cs`: add `TryParseEntryV2` per design D2 —
       required non-empty strings `id, name, description, version, publisher, homepage,
       compatibility` (`TryGetString`); `compatibility` must pass
       `AppCompatibilityRange.TryParse`, else warn + skip; `descriptor` must be an object
       (add a small `TryGetObject` helper mirroring `TryGetString:492-505`), its `GetRawText()`
       stored verbatim, it must parse via `descriptorReader.ReadContent` and pass
       `descriptorValidator.Validate`, and its `Plugin.Id` must equal the entry `id` (ordinal),
       else warn + skip; `SchemaVersion = descriptor.SchemaVersion ?? 1`; `channel` absent/null/
       non-string → `"stable"`, any other string → warn + skip (DEV-1); `package` absent/null →
       descriptor-only (null `PackageUrl`/`Sha256`), present object → require non-empty `url` and
       `sha256` strings mapping to `PackageUrl`/`Sha256` (HTTPS stays download-time-enforced, as
       in v1); `logoUrl`/`releaseNotesUrl`/`signature` optional via existing semantics; duplicate
       ids reuse the existing `seenIds` check; wrap the body in the same warn + skip catch shape
       as the v1 branch (`:485-489`)
- [ ] 4.6 Edit `src/CLIHub.Core/Plugins/FeedUrlProvider.cs`: change `DefaultFeedUrl` (`:13`) to
       `https://raw.githubusercontent.com/trustsoft/CLIHub-plugins-v2/master/index.json`; `GetFeedUrl`
       semantics (`:20-41`) and XML docs otherwise untouched
- [ ] 4.7 Edit `src/CLIHub/ViewModels/Agents/FeedPluginItem.cs`: ctor and properties per design
       D6/D8 — `packageUrl`/`sha256`/`minAppVersion` become nullable; add `compatibility`
       (non-null), `descriptorJson`, `logoUrl` params/properties; add computed
       `public bool UsesDescriptorInstall => this.DescriptorJson is not null && this.PackageUrl is
       null`; change `BlockedReason` (`:246-248`) to the dual form — `MinAppVersion` non-null →
       the verbatim existing string; otherwise →
       `$"Requires CLIHub {this.Compatibility}. Update CLIHub to install this plugin."` (new
       string, OQ-2). No XAML change (bindings verified in design.md)
- [ ] 4.8 Edit `src/CLIHub/ViewModels/Agents/BrowsePluginsDialogViewModel.cs`
       `LoadFeedAsync` (`:143-231`): replace `:198` with
       `var canInstall = AppCompatibilityRange.IsSatisfied(currentVersion, entry.Compatibility);`
       (one call for both schemas, D4 equivalence); extend the entry→item mapping (`:201-215`)
       with the new fields (`compatibility`, `descriptorJson`, `logoUrl`); statuses (`:183-196`)
       and the built-in filter (`:174-178`) unchanged
- [ ] 4.9 Edit `src/CLIHub/ViewModels/Agents/BrowsePluginsDialogViewModel.cs`
       `InstallPluginAsync` (`:233-313`): branch at the top on `item.UsesDescriptorInstall` —
       descriptor path per design D6: `InspectDescriptor` → invalid → `ShowValidationError`;
       `ShowInstallConfirmation(preview)`; on confirm fetch the logo when `item.LogoUrl` is
       non-null via `DownloadLogoAsync`, logging a warning and passing null logo bytes on failure
       (D7 tolerated-failure policy); `InstallDescriptor(..., replaceExisting)` with the existing
       status-based `replaceExisting` derivation (`:269-271`); same
       `ShowInstallOutcome`/`MarkInstalled(preview.Version)` success handling and the same catch
       shape. The zip path (`:239-313`) stays byte-for-byte unchanged
- [ ] 4.10 Edit `tests/CLIHub.Core.Tests/Plugins/PluginFeedClientTests.cs`: add a
       `ValidV2IndexJson` fixture mirroring the live payload (design "Verified Ground Truth") —
       two entries: one package-backed (descriptor containing `schemaVersion: 1` **and**
       `minAppVersion`, `commands` with launch/resume/version, `package {url, sha256}`,
       `logoUrl`, null `releaseNotesUrl`/`signature`, `compatibility ">=1.1.0"`, `channel
       "stable"`) and one descriptor-only (no `package` key). New tests: (a)
       `GetFeedAsync_ValidV2Index_ParsesEntries` — index `SchemaVersion` 2; package entry exposes
       `PackageUrl`/`Sha256` from `package`, raw `DescriptorJson`, `Compatibility`, `Channel`,
       `LogoUrl`, `MinAppVersion` null, entry `SchemaVersion` 1 (from the descriptor); (b)
       `GetFeedAsync_V2DescriptorOnlyEntry_HasNullPackageFields`; (c)
       `GetFeedAsync_V2NonStableChannel_EntrySkipped`; (d)
       `GetFeedAsync_V2MalformedCompatibility_EntrySkipped`; (e)
       `GetFeedAsync_V2InvalidDescriptor_EntrySkipped` (descriptor without `launch`); (f)
       `GetFeedAsync_V2DescriptorIdMismatch_EntrySkipped`; (g)
       `GetFeedAsync_V2PackageMissingSha256_EntrySkipped`; (h)
       `GetFeedAsync_SchemaVersion3_ReturnsUnsupportedError` asserting
       `"Unsupported feed schema version 3"`. All 24 existing tests must pass unchanged (v1
       fixture and assertions untouched)
- [ ] 4.11 Edit `tests/CLIHub.Tests/ViewModels/Agents/BrowsePluginsDialogViewModelTests.cs`:
       (a) mechanically extend the 13 positional `PluginFeedEntry` constructions (e.g. `:29-32`)
       with `">=1.0.0", null, null, null` (compatibility/channel/descriptorJson/logoUrl) — **no
       behavior assertion changes**; (b) add
       `LoadAsync_V2DescriptorOnlyEntry_CanInstallFromRangeAndShowsBlockedRange` — feed index
       `SchemaVersion: 2` with a descriptor-only entry `compatibility ">=2.0.0"`, current version
       `"1.0.0"` → `CanInstall` false and `BlockedReason`
       `"Requires CLIHub >=2.0.0. Update CLIHub to install this plugin."`; (c) add
       `InstallCommand_DescriptorOnly_InspectsConfirmsInstallsWithoutPackageDownload` — descriptor
       entry with `logoUrl`; verify `DownloadPackageAsync` `Times.Never`,
       `InspectDescriptor`/`InstallDescriptor` `Times.Once`, `DownloadLogoAsync` `Times.Once`,
       card flips to `Current` with the preview version, `GetFeedAsync` `Times.Once`; (d) add
       `InstallCommand_DescriptorOnly_LogoDownloadFailure_InstallsWithoutLogo` —
       `DownloadLogoAsync` returns failure; `InstallDescriptor` still called once with null logo
       bytes; (e) add `InstallCommand_DescriptorOnly_UserCancels_NoInstall` —
       `ConfirmInstallation = false`; `InstallDescriptor` `Times.Never`,
       `DownloadLogoAsync` `Times.Never` (logo fetched only after confirmation, D6)
- [ ] 4.12 Gate (Stage 4): `dotnet build CLIHub.sln` (0 errors, 0 warnings) and
       `dotnet test CLIHub.sln` (0 failed) — the pre-existing feed-client tests (24) and the
       pre-existing ViewModel behavior assertions pass with only the 4.11a fixture-argument
       change; the suite grows by exactly the eight tests from 4.10 and five from 4.11b-e; any
       other count delta is a stop-and-report signal

## Stage 5: Docs, final gates, and handoff

- [ ] 5.1 Edit `docs/architecture/plugins.md` (Remote Plugin Feed section): default URL
       (`:177`, `:231`) → the v2 index; Index Schema → both schema versions (v1 unchanged shape;
       v2 entry shape from the verified ground truth, `package` optional, `descriptor` inline,
       `compatibility` range, `channel`, `logoUrl`); "minAppVersion gating" (`:223`) →
       compatibility gating (v1 floor synthesized as `>=minAppVersion`, same message);
       Installation Flow → descriptor-only path (no download/checksum, same confirmation and
       installer guarantees, logo fetch with tolerated failure); Publishing Contract (`:239-248`)
       → note the v1 repo is frozen and publishing happens via PR to
       `trustsoft/CLIHub-plugins-v2` per-plugin folders (ADR-0015), keeping the v1 text for
       historical custom-feed users
- [ ] 5.2 Edit `docs/project-context.md` runtime facts (`:51-55`): default feed is the official
       `trustsoft/CLIHub-plugins-v2` v2 index; installs may be package-based (SHA-256 verified) or
       descriptor-only (inline descriptor + optional logo, no package); keep the feed-URL
       preference semantics sentence as is
- [ ] 5.3 Final build gate: `dotnet build CLIHub.sln` — 0 errors, 0 warnings
- [ ] 5.4 Final test gate: `dotnet test CLIHub.sln` — 0 failed, 0 skipped; record the passed
       count and confirm the only delta versus the pre-change baseline is the new tests from
       1.3, 2.6, 3.4, 4.10, and 4.11b-e
- [ ] 5.5 Run `openspec validate --all` — confirm 0 errors with `✓ change/plugin-feed-v2-client`
       in the output; fix any structural finding only inside
       `openspec/changes/plugin-feed-v2-client/`, never in `openspec/specs/`
- [ ] 5.6 Run `node .opencode/scripts/check-model-policy.mjs` if any subagent was dispatched
       during implementation, and `node .opencode/scripts/pipeline-state.mjs add
       plugin-feed-v2-client` to record the pipeline entry (all stage transitions through the
       script, never direct `.opencode/pipeline.json` edits)
- [ ] 5.7 Run `graphify update .` so the knowledge graph reflects the dual-format feed client,
       range evaluator, and installer seam
- [ ] 5.8 Scope check: `git status --porcelain` lists, for this change, only
       `src/CLIHub.Core/Plugins/AppVersionComparator.cs`,
       `src/CLIHub.Core/Plugins/AppCompatibilityRange.cs`,
       `src/CLIHub.Core/Plugins/PluginDescriptorInstallSource.cs`,
       `src/CLIHub.Core/Plugins/PluginFeedClient.cs`,
       `src/CLIHub.Core/Plugins/PluginFeedResult.cs`,
       `src/CLIHub.Core/Plugins/IPluginFeedClient.cs`,
       `src/CLIHub.Core/Plugins/IPluginInstaller.cs`,
       `src/CLIHub.Core/Plugins/PluginInstaller.cs`,
       `src/CLIHub.Core/Plugins/FeedUrlProvider.cs`,
       `src/CLIHub/ViewModels/Agents/BrowsePluginsDialogViewModel.cs`,
       `src/CLIHub/ViewModels/Agents/FeedPluginItem.cs`,
       `tests/CLIHub.Core.Tests/Plugins/AppCompatibilityRangeTests.cs`,
       `tests/CLIHub.Core.Tests/Plugins/PluginFeedClientTests.cs`,
       `tests/CLIHub.Core.Tests/Plugins/PluginInstallerTests.cs`,
       `tests/CLIHub.Tests/ViewModels/Agents/BrowsePluginsDialogViewModelTests.cs`,
       `docs/architecture/plugins.md`, `docs/project-context.md`, and
       `openspec/changes/plugin-feed-v2-client/` — anything else is a stop-and-report signal
- [ ] 5.9 Report the gate results together with a recommended manual smoke (close CLIHub, reopen,
       open Browse Plugins against the live v2 default feed: cards render with statuses; install
       one package-backed plugin end-to-end with confirmation and checksum; install one
       descriptor-only plugin — confirmation lists the descriptor's commands, logo appears,
       card flips to Installed; point `PluginFeedUrl` at a v1-style feed and repeat a browse +
       install to confirm v1 custom feeds still work; confirm the blocked-range message by
       lowering/raising a test feed's compatibility), then wait for the smoke result and ask for
       explicit `git commit` and `openspec archive` approval (approval never carries over;
       nothing is staged before it)
