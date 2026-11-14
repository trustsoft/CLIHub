# Simplify the plugin feed fetch/install procedure

## Why

The plugin feed pipeline (`PluginFeedClient` + `BrowsePluginsDialogViewModel`) carries four
accumulated complications that cost a round-trip, duplicate catch-block scaffolding, re-parse
versions redundantly, and re-download the whole feed index to refresh a single card:

- **Two-pass redirect probe.** `PluginFeedClient.FollowRedirectsAsync`
  (`src/CLIHub.Core/Plugins/PluginFeedClient.cs:229-264`) issues a **HEAD** request per hop, then
  `GetFeedAsync` (`:82-88`) and `DownloadPackageAsync` (`:154-160`) re-request the final URL with
  **GET**. Every fetch pays one extra round-trip even with zero redirects, and a server that
  answers GET and HEAD differently (redirecting GET but not HEAD, or vice versa) breaks the fetch
  with a misleading error.
- **Duplicated structured-error catch blocks.** `GetFeedAsync` (`:112-132`) and
  `DownloadPackageAsync` (`:196-217`) each repeat four near-identical catch shapes (timeout /
  `HttpRequestException` / caller-cancel rethrow / catch-all), differing only in strings and
  temp-file cleanup.
- **Redundant version computation.** `BrowsePluginsDialogViewModel.LoadFeedAsync`
  (`src/CLIHub/ViewModels/Agents/BrowsePluginsDialogViewModel.cs:199-205`) parses both versions
  twice and compares twice to derive `isDowngrade`, even though
  `PluginFeedVersionComparator` (`src/CLIHub.Core/Plugins/PluginFeedVersionComparator.cs`) already
  owns version semantics.
- **Full feed re-fetch after every install.** `InstallPluginAsync` (`:286`) calls `LoadFeedAsync`
  after a successful install, re-downloading the entire index to refresh one card — and on a
  transient network failure it wipes the dialog's list and shows an error right after a
  *successful* install (`LoadFeedAsync` clears `Plugins` at `:147` before fetching).

## What Changes

Four items, R1–R4. None weakens a security guarantee; the full guarantee inventory is restated in
`design.md` ("Non-negotiable guarantees") with the code that keeps each one.

- **R1 — single-pass GET with in-flight redirect following.** Replace the HEAD-preflight probe
  with one private helper that performs the actual GET (`AllowAutoRedirect` stays `false`,
  `PluginFeedClient.cs:55-60`), follows 3xx manually on the response (resolve `Location` against
  the current URL, enforce HTTPS per hop via `IsHttpsUrl`, keep `MaxRedirectHops = 5`, keep the
  existing redirect-rejection error strings), and returns the terminal response for the existing
  bounded read / bounded copy. Delete `FollowRedirectsAsync`. A 3xx without `Location` is treated
  as terminal (decision D2 in `design.md`), which reproduces today's observable outcome.
- **R2 — one structured-error wrapper.** Extract a single private generic helper that wraps an
  operation into `PluginFeedResult` / `PluginDownloadResult`, preserving every current error
  string, the current log messages/levels, and the caller-cancel-rethrow semantics exactly.
- **R3 — one-call downgrade check.** Add a public
  `PluginFeedVersionComparator.IsDowngrade(installedVersion, feedVersion)` (decision D4) and
  collapse the ViewModel computation to a single call with identical semantics (equivalence proof
  in `design.md`). `TryParseVersion`/`CompareVersions` stay `internal`.
- **R4 — in-place card update after install.** Drop the post-install `LoadFeedAsync` call;
  update the installed `FeedPluginItem` in place via a new `MarkInstalled(installedVersion)`
  method: `InstalledVersion` from the inspected package's declared version, `Status` recomputed
  through `PluginFeedVersionComparator.Compare` (not hardcoded `Current`; null declared version
  maps to `UpdateAvailable`, mirroring `LoadFeedAsync:183-196`), `IsDowngrade` recomputed. This is
  an accepted observable change: the dialog no longer reflects concurrent feed edits after an
  install; Refresh remains the way to re-sync.

Tests: the redirect tests in `tests/CLIHub.Core.Tests/Plugins/PluginFeedClientTests.cs` are
reworked to the single-request GET pattern (they enqueue HEAD preludes today, `:345-355`) and gain
new pins (zero-redirect single GET, 3xx-without-Location, relative Location); the install-flow
test in `tests/CLIHub.Tests/ViewModels/Agents/BrowsePluginsDialogViewModelTests.cs` is extended to
pin the no-refetch, in-place-update behavior. Deviation from the brief recorded in `design.md`:
that test currently asserts **no** post-install reload count, so it passes today and after R4 — it
is extended, not fixed.

## Capabilities

### Modified Capability: `plugin-feed`

One requirement delta: "Install plugin from feed" gains a scenario pinning the in-place card
update without a feed re-fetch (`specs/plugin-feed/spec.md` in this change). All original scenario
names and bodies are preserved verbatim. R1–R3 change no pinned scenario text — the affected
scenarios ("Redirect to HTTP rejected", "Network timeout", "Network unavailable",
"Index exceeds size limit", "Package URL is HTTP", "Package exceeds size limit",
"Checksum mismatch", "Temporary file cleanup", the status-classification scenarios) all hold
before and after; verdict per item with evidence in `design.md` D6.

## Impact

**Modified code:**

- `src/CLIHub.Core/Plugins/PluginFeedClient.cs` — R1 + R2: new redirect-following GET helper, new
  structured-error wrapper, `GetFeedAsync`/`DownloadPackageAsync` bodies rewritten around them,
  `FollowRedirectsAsync` deleted. No public-signature change (`IPluginFeedClient` untouched).
- `src/CLIHub.Core/Plugins/PluginFeedVersionComparator.cs` — R3: one new public static method.
- `src/CLIHub/ViewModels/Agents/BrowsePluginsDialogViewModel.cs` — R3 collapse + R4 success branch.
- `src/CLIHub/ViewModels/Agents/FeedPluginItem.cs` — R4: `Status`/`InstalledVersion`/`IsDowngrade`
  become settable with change notifications for the computed display properties; new
  `MarkInstalled` method.
- Tests: `tests/CLIHub.Core.Tests/Plugins/PluginFeedClientTests.cs`,
  `tests/CLIHub.Core.Tests/Plugins/PluginFeedVersionComparatorTests.cs`,
  `tests/CLIHub.Tests/ViewModels/Agents/BrowsePluginsDialogViewModelTests.cs`.

**Unchanged:** `PluginInstaller` (all install-time security), `IPluginFeedClient` and the result
records, `FeedUrlProvider`, the dialog XAML (existing bindings already cover every notified
property — inventory in `design.md`), DI registration, configuration, `docs/architecture/plugins.md`
(its statements remain true; `design.md` D6 documents why).

**Out of scope / must not be touched:** `.opencode/agents/pipe/verifier.md` carries a foreign
uncommitted edit (agent-config clarification, not part of this change). It stays unstaged and
unmodified — see `design.md` D7.

**Verification:** `dotnet build CLIHub.sln` + `dotnet test CLIHub.sln` after every stage;
`openspec validate --all`; `graphify update .` at the end.

## Non-Goals

- No weakening of any security guarantee: HTTPS-only per hop including redirects, `MaxRedirectHops`
  = 5, 2 MB index / 10 MB package limits (fast-path Content-Length + bounded streaming), SHA-256
  verification before install, the re-validation pass inside `PluginInstaller.Install`, staging +
  backup + rollback, explicit confirmation of every declared command, and descriptor+logo-only
  extraction (no code execution) are all preserved verbatim.
- No user-facing error-string changes (every string kept verbatim; `design.md` D3 inventories
  them). The only observable behavior change is R4's dropped re-fetch, covered by the spec delta.
- No signature/`signature` field work (stays null), no feed schema changes, no new redirect
  features (e.g., no cross-method redirect memory), no caching of the feed index.
- No changes to `PluginInstaller`, `IPluginInstaller`, `PluginInstallResult` (no new version field
  on the result — `design.md` D5 rejected that), no UI layout or XAML edits.
- No commits, no `openspec archive`, no pipeline-state writes inside the tasks beyond the
  repository-mandated `pipeline-state.mjs add` recording step.

## Dependencies

- Base: HEAD `73c9c69` (archive commit of `add-chrome-window-base`). No other active changes
  (`openspec/changes/` holds only `archive/`). No file overlap with concurrent work.
- Consumes, does not modify: `PluginInstaller`, `FeedUrlProvider`, `AppVersionComparator`,
  `EmbeddedPluginResources`.
- Blocked-by: nothing. Nothing else depends on this change.

## Risk

- **Redirect-semantics regression (R1):** highest-risk item; mitigated by reworked + new
  redirect tests (hop budget, HTTPS rejection, relative Location, missing Location, method/count
  pins) and by the unchanged error strings the existing tests already assert.
- **Missed change notification in the in-place card update (R4):** a binding that does not
  re-render would leave a stale card; mitigated by the XAML binding inventory (`design.md`)
  and ViewModel tests asserting `Status`/`StatusText`-feeding state transitions.
- **Accepted UX change (R4):** no automatic reflection of concurrent feed edits after install;
  approved in scope, pinned by the new spec scenario, Refresh unchanged.
