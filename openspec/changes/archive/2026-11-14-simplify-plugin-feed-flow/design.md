# Design: Simplify the plugin feed fetch/install procedure

All `file:line` references verified at HEAD `73c9c69` unless noted. Working tree at verification
time: clean except one foreign modified file, `.opencode/agents/pipe/verifier.md` (agent-config
clarification from another session) — **out of scope, must remain unstaged and untouched**
(decision D7).

## Verified Current State

### `PluginFeedClient` (src/CLIHub.Core/Plugins/PluginFeedClient.cs)

| Element | Location | Fact |
|---|---|---|
| Class | `:15` | `public sealed class PluginFeedClient : IPluginFeedClient, IDisposable` |
| Size/hop constants | `:18-20` | `MaxIndexSizeBytes` 2 MB, `MaxPackageSizeBytes` 10 MB, `MaxRedirectHops` 5 |
| Public ctor | `:31-35` | delegates to internal ctor with `CreateDefaultHandler()` |
| Internal ctor | `:44-53` | used by tests to inject a stub `HttpMessageHandler`; sets `UserAgent` |
| Default handler | `:55-60` | `SocketsHttpHandler { AllowAutoRedirect = false }` (`:59`) — stays |
| `GetFeedAsync` | `:63-133` | feed URL resolution `:65-69`; HTTPS pre-check `:71-75`; linked timeout CTS `:77-78`; **HEAD probe** `:82`; probe-error return `:83-86`; **GET final URL** `:88` (default `HttpCompletionOption` = buffered); `!IsSuccessStatusCode` → "Could not load plugin feed" `:89-93`; fast-path ContentLength `:97-101`; `ReadBoundedAsync` `:103-108`; `ParseFeedIndex` `:110`; four catches `:112-132` |
| `DownloadPackageAsync` | `:136-218` | arg/null + HTTPS pre-check `:138-145`; temp path `:147`; linked timeout CTS **inside try** `:151-152`; **HEAD probe** `:154`; probe-error return `:155-158`; **GET** `HttpCompletionOption.ResponseHeadersRead` `:160`; `!IsSuccessStatusCode` → "Package download failed" `:161-165`; fast-path `:169-173`; `CopyBoundedAsync` + temp delete on overflow `:177-183`; SHA-256 verify + temp delete on mismatch `:186-192`; success `:194`; four catches `:196-217` |
| `FollowRedirectsAsync` | `:229-264` | **HEAD per hop** `:234-235`; terminal when `< 300 || >= 400` `:237-241`; 3xx without `Location` → warning log, treated as terminal at current URL `:243-247`; Location resolution (absolute or relative against current URL) `:249-251`; per-hop HTTPS via `IsHttpsUrl` `:253-257` → error `"Redirect to non-HTTPS URL rejected"`; loop `for hop < MaxRedirectHops` `:232` → `"Redirect limit exceeded (maximum 5 hops)"` `:263` |
| `ReadBoundedAsync` | `:271-295` | streams via `ReadAsStreamAsync`, aborts past `maxBytes` |
| `CopyBoundedAsync` | `:302-324` | streams to file, aborts past `maxBytes` |
| `ParseFeedIndex` / `TryParseEntry` | `:326-428` | schema validation, duplicate-id skip |
| `IsHttpsUrl` | `:445-449` | absolute URI + `https` scheme, case-insensitive |
| `ComputeSha256Async` | `:451-456` | `SHA256.HashDataAsync`, hex string |
| `DeleteTempFile` | `:458-476` | best-effort delete with warning log |
| `ProbeTimeout` | `:478-479` | `AgentProbePolicy.ResolveTimeout(...AgentProbeTimeoutSeconds)` |

Catch-block inventory (the duplication R2 removes):

| Shape | GetFeedAsync | DownloadPackageAsync |
|---|---|---|
| timeout (`OperationCanceledException` when `!caller.IsCancellationRequested`) | `:112-116` → "Feed request timed out", log Warning w/o ex | `:196-201` → "Package download timed out" + `DeleteTempFile`, log Warning w/o ex |
| `HttpRequestException` | `:117-121` → "Could not load plugin feed. Check your network connection.", log Warning w/ ex | `:202-207` → "Package download failed" + `DeleteTempFile`, log Warning w/ ex |
| caller cancel (`OperationCanceledException` when caller token set) | `:122-125` → `throw;` | `:208-211` → `throw;` |
| catch-all `Exception` | `:126-132` → "Could not load plugin feed. Check your network connection.", log Warning("Feed request failed unexpectedly") w/ ex | `:212-217` → "Package download failed" + `DeleteTempFile`, log **Error**("Unexpected error during package download") w/ ex |

### `BrowsePluginsDialogViewModel` (src/CLIHub/ViewModels/Agents/BrowsePluginsDialogViewModel.cs)

- `InstallCommand` `:64` — canExecute `item is not null && item.CanInstall && !this.IsLoading`
  (does **not** reference `Status`).
- `LoadFeedAsync` `:143-237` — clears `Plugins` `:147`; fetch `:151`; failure surface `:152-159`;
  state/currentVersion/built-in ids `:167-169`; built-in filter `:174-178`; **status model**
  `:183-196`: no state entry → `Available`; entry with `InstalledVersion == null` →
  `UpdateAvailable`; else `PluginFeedVersionComparator.Compare(installedVersion, entry.Version)`;
  `canInstall` `:198` (`AppVersionComparator.IsSatisfied`, `AppVersionComparator.cs:15`);
  **isDowngrade** `:199-205` (status guard + null guard + two `TryParseVersion` pairs + one
  `CompareVersions`); item construction `:207-221`.
- `InstallPluginAsync` `:239-319` — download `:245-254`; `Inspect` `:259-265`; confirmation
  `:268-272`; `replaceExisting` `:275-277`; `Install` `:279`; success: outcome dialog + log
  `:282-283`, **feed reload** `:286`; failure dialog `:290-291`; catches `:294-302`; temp cleanup
  `finally` `:306-317`.
- `RunAsync` `:321-322` delegates to `IApplicationOperationLifetime`.

### `FeedPluginItem` (src/CLIHub/ViewModels/Agents/FeedPluginItem.cs)

`ObservableObject` (`:8`) but **every property is get-only** (`:62-127`) — the class never calls
`SetProperty`. Computed display properties: `StatusText` `:132-139`, `VersionText` `:144-149`
(depends on `Status` + `InstalledVersion`), `ActionText` `:155-160` (depends on `Status`),
`ShowActionButton` `:165` (depends on `Status`), `WarningMessage` `:170` (depends on
`IsDowngrade`), `HasWarning` `:180`, `BlockedReason` `:185-187` (depends on `CanInstall` /
`MinAppVersion` only). XAML bindings that must re-render on an in-place update
(`src/CLIHub/Views/BrowsePluginsDialog.xaml`): `VersionText` `:114`, `ActionText` `:122`,
`CanInstall` `:123` (immutable — unaffected), `ShowActionButton` `:124`, `WarningMessage` `:161`,
`HasWarning` `:163`, `StatusText` `:187`.

### `PluginFeedVersionComparator` (src/CLIHub.Core/Plugins/PluginFeedVersionComparator.cs)

`Compare(string?, string)` public `:41-73` — numeric branch `:49-67` (`>0` →
`DifferentVersionAvailable`, `==0` → `Current`, `<0` → `UpdateAvailable`), string-equality
fallback `:70-72`. `TryParseVersion` **internal** `:75-91`; `CompareVersions` **internal**
`:93-106`. `CLIHub.Core.csproj:28-31` grants `InternalsVisibleTo` to `CLIHub`, `CLIHub.Tests`,
`CLIHub.Core.Tests`, `CLIHub.Core.Windows.Tests` — so the ViewModel's internal calls at
`BrowsePluginsDialogViewModel.cs:201-205` compile **today without any surface opening**
(deviation 2 from the brief). Existing comparator tests exercise only public `Compare`
(`tests/CLIHub.Core.Tests/Plugins/PluginFeedVersionComparatorTests.cs:8-54`).

### Install-time security surface (unchanged by this change; cited for the guarantees table)

`PluginInstaller` (`src/CLIHub.Core/Plugins/PluginInstaller.cs`): re-validation inside `Install`
`:87-93` (TOCTOU defense — "the user may have swapped the file between confirmation and
install"); reserved built-in id rejection `:342-346`; staging dir `:105`; extraction allow-list
(`plugin.json`/`logo.png` only) `:122-131`; descriptor re-read + re-validate after extraction
`:141-157`; backup `:161-165`; single-rename atomic move `:169`; rollback `:192-200` →
`:395-428`; state record with `InstalledVersion = plugin.Version` `:389` (the descriptor's own
version, nullable — `PluginInstallPreview.Version` is `string?`, `PluginInstallPreview.cs`);
catalog reload `:181-188`. `PluginInstallResult` carries only
`Success/InstalledPluginId/Errors` — no version field.

### Tests

- `tests/CLIHub.Core.Tests/Plugins/PluginFeedClientTests.cs` — `EnqueueFeed`/`EnqueuePackage`
  enqueue a HEAD 200 prelude before the GET `:345-355`; `GetFeedAsync_RedirectToHttps_IsFollowed`
  `:200-215` asserts **3 requests** (HEAD, HEAD, GET) with the last two on the redirected URL;
  `GetFeedAsync_RedirectToHttp_IsRejected` `:217-228` (1 request, error string);
  `GetFeedAsync_TooManyRedirects_IsRejected` `:230-245` (5 requests, error string);
  `GetFeedAsync_MalformedRedirectLocation_ReturnsStructuredError` `:259-271` (handler throws
  `UriFormatException`; asserts structured error);
  `GetFeedAsync_Timeout_ReturnsStructuredError` `:273-283`; oversize trio `:159-198`
  (ContentLength / chunked / lying ContentLength); `DownloadPackageAsync_ChecksumMatch_ReturnsTempFile`
  `:285-310`; mismatch/oversize with temp-file cleanup `:312-343`. `StubHandler` `:404-438`
  records every `HttpRequestMessage` (`Requests`), supports `Throw`/`Delay`.
- `tests/CLIHub.Tests/ViewModels/Agents/BrowsePluginsDialogViewModelTests.cs` —
  `InstallCommand_Success_DownloadsInspectsConfirmsInstalls` `:214-253` verifies
  `DownloadPackageAsync`/`Inspect`/`Install` `Times.Once` and dialog counts; it does **not**
  assert any `GetFeedAsync` call count (deviation 1 from the brief);
  `InstallCommand_UpdateAvailable_InstallsWithReplaceExistingTrue` `:255-294` pins the
  `replaceExisting: true` path. `InlineOperationLifetime`
  (`tests/CLIHub.Tests/Support/TestHelpers.cs:61-70`) runs operations **synchronously**, so
  `InstallCommand.Execute` in tests covers the entire flow including today's post-install reload.

### Durable contract (openspec/specs/plugin-feed/spec.md)

Requirements: fetch/parse `:8-80` (scenarios "Redirect to HTTP rejected" `:62-67`, "Network
timeout" `:69-73`, "Network unavailable" `:75-80`, "Index exceeds size limit" `:49-53`); download
+ checksum `:82-117`; status display `:119-166` (incl. downgrade scenario `:144-151`); install
from feed `:168-194`; update from feed `:196-214`; min-app gating `:216-238`; refresh on demand
`:240-261` (Refresh button only); feed URL config `:263-282`; offline degradation `:284-306`;
built-in exclusion `:308-329`; confirmation dialog `:331-342`; temp cleanup `:344-364`; dialog
chrome `:366-398`. **No scenario pins a post-install feed re-fetch.** Other specs checked:
`plugin-installation` pins catalog reload (installer-owned, `PluginInstaller.cs:181-188`) —
unaffected; nothing else mentions the Browse install flow.

## Non-negotiable guarantees (each preserved; nothing in R1–R4 touches these code paths except where noted)

| Guarantee | Kept by (current → after change) |
|---|---|
| HTTPS-only per hop incl. redirects | `IsHttpsUrl` pre-checks `PluginFeedClient.cs:71-75,141-145` + per-hop check `:253-257` → same check moved into the new GET follower (D1) |
| `MaxRedirectHops = 5` | `:20,:232,:262-263` → same constant, same loop bound, same error string (D1) |
| 2 MB index limit (fast-path + bounded read) | `:97-108` → unchanged code; the feed GET additionally moves to `ResponseHeadersRead` so the bound applies during streaming (D1) |
| 10 MB package limit (fast-path + bounded copy) | `:169-183` → unchanged code |
| SHA-256 verification before install | `:186-192` → unchanged |
| Re-validation inside `PluginInstaller.Install` (TOCTOU) | `PluginInstaller.cs:87-93` → untouched |
| Staging + backup + rollback | `PluginInstaller.cs:105,161-169,395-428` → untouched |
| Explicit user confirmation of all declared commands | `BrowsePluginsDialogViewModel.cs:268-272` + `PluginInstaller.cs:430-455` → untouched |
| No code execution; descriptor + logo only | `PluginInstaller.cs:122-131` → untouched |
| Error strings verbatim | D3 inventory; only R4's removed reload had no string of its own |

## Decisions

### D1 (R1): single-pass GET redirect follower; delete `FollowRedirectsAsync`

New private helper on `PluginFeedClient`, shape:

```
private async Task<(HttpResponseMessage? Response, string? Error)> SendFollowingRedirectsAsync(
    string url, HttpCompletionOption completionOption, CancellationToken cancellationToken)
```

- Sends `HttpRequestMessage(HttpMethod.Get, currentUrl)` per hop through the existing
  `httpClient` (still `AllowAutoRedirect = false`, `:55-60`).
- Terminal condition identical to today's probe (`:237-241`): status `< 300 || >= 400` → return
  the response itself (caller owns and disposes it with `using`).
- Redirect handling: read `response.Headers.Location`; resolve against the current URL exactly as
  today (`:249-251` — absolute as-is, relative via `new Uri(new Uri(currentUrl), location)`);
  enforce `IsHttpsUrl(nextUrl)` → `"Redirect to non-HTTPS URL rejected"`; dispose the intermediate
  response before the next hop; loop bounded by `MaxRedirectHops` → `"Redirect limit exceeded
  (maximum 5 hops)"`. Both error strings and both log messages (`:255`, `:262`) are preserved.
- **Callers:** `GetFeedAsync` and `DownloadPackageAsync` replace the probe + second GET with one
  call; a null response returns the structured redirect error (`redirectError ?? "Redirect
  rejected"`), preserving today's `:83-86` / `:155-158` fallback. The final response then flows
  into the unchanged status/size/checksum code.
- **Feed path completion option:** `GetFeedAsync` today uses the default buffered completion
  (`:88`), which buffers the whole body in memory *before* the 2 MB checks run — the "bounded
  read" is post-hoc for the index today. The rework switches the feed to
  `HttpCompletionOption.ResponseHeadersRead` (the package path already uses it, `:160`), making
  `ReadBoundedAsync` genuinely bound during streaming. No observable difference: the fast-path
  `ContentLength` check reads headers (present in both modes), and `ReadBoundedAsync` calls
  `ReadAsStreamAsync` either way. This strengthens, never weakens, the size guarantee.
- `FollowRedirectsAsync` (`:229-264`) is deleted; its only callers are `:82` and `:154`
  (verified — no other references in `src`).

**Why not** keep the probe and just merge the GET (e.g., cache the terminal HEAD result)?
Rejected: the HEAD/GET divergence is exactly the defect — HEAD cannot be "reused" because the
terminal response must come from the GET. **Why not** `AllowAutoRedirect = true` with
`Https Environmental` checks? Rejected: automatic redirect cannot enforce HTTPS per hop (the
handler follows before the app sees the target), so manual following stays.

**Net request-count change:** zero-redirect fetch goes from 2 requests (HEAD + GET) to 1; an
N-redirect fetch goes from N+2 to N+1. Behavior fixes: a server redirecting GET but not HEAD now
works; a server 200-ing HEAD but 3xx-ing GET is now followed instead of failing with the generic
error (recorded as a strictly-improving edge in D2's rationale).

### D2 (R1): 3xx without `Location` is terminal

Today's probe treats a 3xx without `Location` as terminal-at-current-URL (`:243-247`); the
subsequent GET then receives (normally) the same 3xx, fails `IsSuccessStatusCode`, and the caller
sees the generic error — "Could not load plugin feed" (`:92`) / "Package download failed"
(`:164`). Traced end-to-end, today's observable outcome for such servers is therefore *a
structured failure with the generic request error*, not success and not a redirect-specific
error.

Decision: the single-pass GET treats a 3xx without `Location` as **terminal** — it returns that
3xx response as the final response, the existing `!IsSuccessStatusCode` branch produces the same
generic error, and the existing warning log (`:245`) is kept. Rationale: (a) it reproduces
today's observable outcome exactly; (b) RFC 9110 permits a user agent to treat a Location-less
3xx as the response to the request, and `HttpClient`'s own auto-redirect behavior returns such
responses as terminal; (c) the alternative (a new redirect-specific error string) invents a
user-facing string no test or spec pins today, violating the strings-verbatim constraint.

**Alternative rejected:** reject with `"Redirect rejected"` / a new message. Rejected because it
changes the observable error for a case that already resolves to the generic error, and adds an
unpinned string. A new test pins the chosen behavior (tasks 2.5).

### D3 (R2): one generic structured-error wrapper with per-operation profiles

One private generic method:

```
private async Task<T> ExecuteWithStructuredErrorsAsync<T>(
    Func<CancellationToken, Task<T>> operation,
    Func<string, T> failure,
    StructuredErrorProfile profile,
    Action? onFailure,
    CancellationToken cancellationToken)
```

with a small private record carrying the per-operation strings and the one differing log level:

```
private sealed record StructuredErrorProfile(
    string TimeoutError, string RequestError,
    string TimeoutLogMessage, string RequestLogMessage,
    string UnexpectedLogMessage, LogLevel UnexpectedLogLevel);
```

- `failure` builds the failure result (`errors => new PluginFeedResult(false, null, [errors])` /
  `errors => new PluginDownloadResult(false, null, [errors])`) — the two result types share no
  base type, so the factory is the minimal generic bridge.
- `onFailure` (null for feed; `() => this.DeleteTempFile(tempFilePath)` for download) runs before
  every structured-error return, reproducing the download catches' cleanup (`:199`, `:205`,
  `:215`).
- Catch semantics preserved exactly: `OperationCanceledException` when the **caller** token is
  cancelled → `throw;` (caller-cancel rethrow, `:122-125`/`:208-211`); `OperationCanceledException`
  otherwise → timeout error, log Warning without exception (`:112-116`/`:196-201`);
  `HttpRequestException` → request error, log Warning with exception (`:117-121`/`:202-207`);
  anything else → request error, log at `profile.UnexpectedLogLevel` with exception
  (`:126-132` Warning vs `:212-217` Error — the one level difference is captured by the profile).
- Timeout CTS: the wrapper creates the linked CTS + `CancelAfter(this.ProbeTimeout)` inside its
  try and passes the linked token to `operation`. Today the feed's CTS sits outside its try
  (`:77-78`) and the download's inside (`:151-152`); unifying inside the try only means a
  hypothetical throw from CTS construction (which does not occur for a live token) would now be
  caught — no observable change, noted for completeness.
- Pre-checks stay **outside** the wrapper and unchanged: feed URL resolution/disabled check
  (`:65-75`), package URL null/HTTPS checks (`:138-145`), temp path allocation (`:147`).
- Every user-facing string preserved verbatim (full inventory in the catch table above). All six
  log messages preserved verbatim.

**Alternative rejected:** a shared exception-to-error mapping table without the generic wrapper —
rejected because the two operations differ in result construction and cleanup, and a mapping
table still leaves the four catch shapes duplicated per call site. **Alternative rejected:**
unifying log levels/messages (e.g., always Warning) — rejected; log text is observable in
`%APPDATA%\CLIHub\logs` and cheap to preserve exactly.

### D4 (R3): public `IsDowngrade` on the comparator; internals stay internal

Add to `PluginFeedVersionComparator`:

```
public static bool IsDowngrade(string? installedVersion, string feedVersion)
```

returning `installedVersion is not null && TryParseVersion(installedVersion) is { } a &&
TryParseVersion(feedVersion) is { } b && CompareVersions(a, b) > 0` (with XML docs). The ViewModel
expression at `:199-205` collapses to
`var isDowngrade = PluginFeedVersionComparator.IsDowngrade(installedVersion, entry.Version);`.

**Equivalence proof** (current = `status == DifferentVersionAvailable && installedVersion is not
null && parseBoth && cmp > 0`): (⇐) if `installedVersion ≠ null`, both parse, and `cmp > 0`, then
`Compare` took the numeric branch and returned `DifferentVersionAvailable`
(`PluginFeedVersionComparator.cs:52-58`); the only other way `status` could be
`DifferentVersionAvailable`-eligible is the null-version rule (`:189-191`), which requires
`installedVersion == null` — contradiction, so `status == DifferentVersionAvailable` holds.
(⇒) if `status == DifferentVersionAvailable` and both versions parse, the status came from the
numeric branch (the string fallback `:70-72` is unreachable when both parse), where
`DifferentVersionAvailable` implies `cmp > 0`. Hence dropping the status guard is semantics-
preserving, including the only-when-both-parse guard.

**Surface choice:** public `IsDowngrade`, keeping `TryParseVersion`/`CompareVersions` `internal`.
Rationale: a named predicate states the ViewModel's actual question; exposing the tuple parser
widens the contract for one caller and leaks an implementation shape; keeping everything internal
would leave the ViewModel permanently coupled to the `InternalsVisibleTo` test seam (which exists
for test assemblies and internal wiring, not as a designed UI contract). The helper is used twice
(load + R4's in-place update), justifying a public surface. Public API ⇒ XML docs per project
style.

### D5 (R4): in-place card update; no post-install re-fetch

1. **Model mutability.** `FeedPluginItem` gains private setters (or settable properties) for
   `Status`, `InstalledVersion`, and `IsDowngrade` using `SetProperty`, each raising
   `OnPropertyChanged` for its computed dependents:
   `Status` → `StatusText`, `VersionText`, `ActionText`, `ShowActionButton`;
   `InstalledVersion` → `VersionText`;
   `IsDowngrade` → `WarningMessage`, `HasWarning`.
   `CanInstall`/`BlockedReason` depend only on immutable inputs and stay get-only. The XAML
   binding inventory (above) is fully covered by these notifications, so the card re-renders
   without rebuilding the collection. `InstallCommand`'s canExecute (`:64`) does not reference
   `Status`, so no `RaiseCanExecuteChanged` is required; the button disappears via
   `ShowActionButton`/`ActionText` when the status becomes `Current`.
2. **`MarkInstalled(string? installedVersion)`** on `FeedPluginItem`: sets
   `InstalledVersion = installedVersion`,
   `Status = installedVersion is null ? PluginUpdateStatus.UpdateAvailable
   : PluginFeedVersionComparator.Compare(installedVersion, this.Version)`, and
   `IsDowngrade = PluginFeedVersionComparator.IsDowngrade(installedVersion, this.Version)`
   (normally false after installing the feed version). Centralizing the three sets keeps the
   notification wiring next to the computed properties.
3. **Status via comparator, not hardcoded `Current`** (the brief's open question): if the
   package descriptor's version differs from the feed entry's version (feed metadata drift; the
   checksum pins the *package*, not the index metadata), `Compare` reports
   `UpdateAvailable`/`DifferentVersionAvailable` truthfully — exactly what the next
   `LoadFeedAsync` would compute, since `PluginInstaller.RecordInstalledState` records the
   descriptor's own version (`PluginInstaller.cs:389`). The `null` case maps to
   `UpdateAvailable`, mirroring the existing "state entry exists but `InstalledVersion` is null"
   rule (`BrowsePluginsDialogViewModel.cs:189-191`) — post-install the state entry always exists
   (`RecordInstalledState` always writes it), so `Available` would be the wrong mapping.
4. **ViewModel success branch:** replace `await this.LoadFeedAsync(cancellationToken)` (`:286`)
   with `item.MarkInstalled(preview.Version);` after the existing outcome dialog + log. The
   installed version source is `preview.Version` — the descriptor version parsed by `Inspect`
   from the same temp file that `Install` re-validates (`PluginInstaller.cs:89`), i.e., the value
   recorded to `plugin-state.json` in every normal flow.
5. **Accepted behavior change (in scope, pinned by the spec delta):** the dialog no longer
   re-fetches the index after an install, so concurrent feed edits surface only on Refresh;
   the post-install transient-failure list-wipe defect disappears with the re-fetch. Also
   observably: `IsLoading` no longer toggles during a post-install refresh.

**Alternatives rejected:** re-reading `stateRepository` after install for the authoritative
version — rejected: in the ViewModel tests the installer is a mock that does not write state, so
the read-back would return the pre-install state and the semantics would differ between the
mocked and real paths; `preview.Version` is identical to the recorded version in every normal
flow. **Rejected:** extending `PluginInstallResult` with the installed version — rejected: widens
a Core API for one UI consumer when `preview.Version` already carries the value. **Rejected:**
replacing just the one card by rebuilding `FeedPluginItem` — rejected: a new instance breaks
identity for any future selection/animation and still needs the mutability analysis; in-place
`SetProperty` matches the project's display-model pattern (`project-management` spec pins the
same in-place-refresh principle for project rows).

**Accepted edge (recorded, bounded):** if the temp package file were swapped for a *different
valid* package between `Inspect` and `Install`, `Install`'s re-validation would either fail
(structured error, card unchanged) or install the swapped package and record its version, while
the card would show `preview.Version` until the next Refresh. The old reload path displayed the
state file's truth here; the window requires replacing a CLIHub-owned temp file mid-dialog and is
accepted as part of dropping the re-fetch.

### D6: Spec-delta verdicts per item

- **R1 — no delta.** Every pinned scenario holds unchanged: "Redirect to HTTP rejected"
  (`plugin-feed/spec.md:62-67` — rejection still happens after detecting the redirect on the GET),
  "Network timeout" `:69-73`, "Network unavailable" `:75-80`, "Index exceeds size limit"
  `:49-53`, "Package URL is HTTP" `:106-111`, "Package exceeds size limit" `:99-104`. No scenario
  mentions HEAD, the request count, or the probe. `docs/architecture/plugins.md:206` ("Manual
  redirects (3xx) are followed up to 5 hops; every redirected URL must also be HTTPS") remains
  true.
- **R2 — no delta.** Pure internal refactor; strings, results, and exceptions preserved.
- **R3 — no delta.** Semantics identical (D4 proof); the pinned downgrade scenario
  (`spec.md:144-151`) and the non-comparable scenario `:153-160` hold.
- **R4 — delta: MODIFIED requirement "Install plugin from feed".** No existing scenario pins the
  reload (verified `spec.md:168-194`; "Refresh feed on demand" `:240-261` is Refresh-button-only;
  `plugin-installation`'s catalog-reload scenarios are installer-owned), so no existing scenario
  text changes. The change is nonetheless observable (no post-install re-fetch; concurrent feed
  edits wait for Refresh), so the delta restates the requirement with all three original
  scenarios verbatim and adds one scenario pinning the in-place card update.
  `docs/architecture/plugins.md:225` ("On success, the catalog reloads and the plugin card
  reflects the new state") remains true — the catalog reload is installer-owned and the card
  still reflects the new state, now in place; no doc edit required.

### D7: Out-of-scope foreign edit

`.opencode/agents/pipe/verifier.md` is modified in the working tree by another session (agent
config clarification). It is not part of this change: do not edit, stage, revert, or include it
in any commit built from this change. `git status` scope checks in the tasks explicitly exclude
it. The implementer and verifier must leave it untouched.

### D8: Test-rework plan (called out per the brief)

`PluginFeedClientTests` (stage 2): drop the HEAD preludes from `EnqueueFeed`/`EnqueuePackage`
(`:345-355`); rework `GetFeedAsync_RedirectToHttps_IsFollowed` to the two-request GET chain;
assert GET methods and request counts so the single-pass property is pinned; add
`GetFeedAsync_NoRedirects_SendsSingleGetRequest`, `GetFeedAsync_RedirectWithoutLocation_ReturnsStructuredError`
(pins D2 with the generic error string), and
`GetFeedAsync_RelativeRedirectLocation_IsResolvedAgainstCurrentUrl` (pins the preserved relative
resolution, previously untested); leave `MalformedRedirectLocation`, timeout, and oversize tests
as-is (they must keep passing). `BrowsePluginsDialogViewModelTests` (stage 4): extend
`InstallCommand_Success_DownloadsInspectsConfirmsInstalls` with `GetFeedAsync` `Times.Once` and
card assertions, and add one update-path test pinning the in-place `Current` transition without
re-fetch. `PluginFeedVersionComparatorTests` (stage 3): new `IsDowngrade` cases.

## Deviations from the brief

1. **The install-flow test does not assert the post-install reload.** The brief states
   `InstallCommand_Success_DownloadsInspectsConfirmsInstalls` "asserts the post-install reload";
   verified at `BrowsePluginsDialogViewModelTests.cs:214-253` it verifies only
   download/inspect/install/dialog counts. It passes today *and* after R4. The task is therefore
   "extend to pin the new behavior", not "fix a breaking assertion".
2. **The comparator surface premise.** The brief describes `TryParseVersion` as merely "currently
   internal"; in reality `InternalsVisibleTo` (`CLIHub.Core.csproj:28-31`) already exposes Core
   internals to `CLIHub` and both test assemblies, and the ViewModel already compiles against
   them. The decision is reframed in D4 (public `IsDowngrade` chosen over "open the parse/compare
   pair" *or* "keep internal and rely on the seam").
3. **Feed GET completion option.** The brief said "bounded read / bounded copy as today"; code
   reality is that the *index* read is buffered by `HttpClient` before the bound check today
   (`:88` default completion). D1 moves the feed to `ResponseHeadersRead` so the guarantee is
   real during streaming — behavior-identical, guarantee-strengthening.

## Open Questions

1. **R4 null-version mapping:** when an installed descriptor declares no `version`
   (`preview.Version == null`), `MarkInstalled` maps to `UpdateAvailable` (matches today's reload
   semantics, `BrowsePluginsDialogViewModel.cs:189-191`). Confirm this is the desired display for
   version-less installs rather than a dedicated "Installed (unknown version)" status — the
   latter would be a new status model entry and is deliberately out of scope.
2. **Redirect test coverage for the package path:** the redirect tests today cover only
   `GetFeedAsync`. Both paths share the new follower, and stage 2 adds method/count assertions to
   `DownloadPackageAsync_ChecksumMatch_ReturnsTempFile` only. Want dedicated
   `DownloadPackageAsync` redirect tests (HTTPS rejection / hop limit) as well, or is the shared
   helper + feed-path coverage sufficient?
3. **Log fidelity:** D3 preserves all log messages and the Warning-vs-Error level difference
   exactly. If the team would rather unify the unexpected-catch log level (e.g., always Error),
   say so before implementation; tests do not constrain it either way.

## Implementation notes (2026-11-14, recorded at verification)

Open-question resolutions by the user: (1) version-less installs display `UpdateAvailable`; (2)
**two dedicated `DownloadPackageAsync` redirect tests were added** — `DownloadPackageAsync_RedirectToHttps_IsFollowed`
and `DownloadPackageAsync_RedirectToHttp_IsRejected` — answering open question 2 with "add"; (3) log
fidelity preserved byte-identical.

Implementation deviations from tasks.md (code reality per design.md):

1. **Task 2.1's "3xx without Location → dispose and return" is self-contradictory** — a disposed
   response cannot be consumed by the caller's `IsSuccessStatusCode` check. Implemented per D2: the
   3xx response is returned terminal and undisposed; the caller owns disposal (`using var
   finalResponse`). A code comment cites D2.
2. **Task 2.5(d)'s asserted string was wrong for the D2 path.** The terminal-3xx flows through the
   `!IsSuccessStatusCode` branch, whose preserved string is `"Could not load plugin feed"` (the
   task's literal `"…Check your network connection."` is the `HttpRequestException` string). The
   implemented test asserts the D2-correct string, which is also what the pre-change code produced
   for this server shape (probe-era trace in D2).
3. **Test-delta arithmetic** (tasks 2.6/5.2 said "three/four"): actual +5 client tests (2.5c-e plus
   the two user-decided download-path tests), +11 comparator cases (task 3.3's truth table as
   InlineData rows), +1 ViewModel test (4.4b). Every added case maps to a task or a user decision;
   suite 1587 → 1604, 0 failed.
