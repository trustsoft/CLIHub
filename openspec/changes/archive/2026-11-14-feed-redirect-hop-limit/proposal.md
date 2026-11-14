## Why

The redirect loop in `PluginFeedClient.SendFollowingRedirectsAsync` has an off-by-one: `for (var hop = 0; hop < MaxRedirectHops; hop++)` performs at most 5 requests (initial + at most 4 followed redirects), rejecting a 5-redirect chain as exceeding the limit. Three specs (`plugin-feed`, `plugin-feed-sync`, `plugin-index-signing`) and the `MaxRedirectHops = 5` constant all promise that up to 5 redirects are acceptable. The error message itself says "maximum 5 hops." The code is one hop short of its own documentation.

This is a real behavior change: one extra legitimate redirect becomes acceptable, making the implementation match its declared contract.

## What Changes

- **Source:** `src/CLIHub.Core/Plugins/PluginFeedClient.cs:886` — change the loop condition from `hop < MaxRedirectHops` to `hop <= MaxRedirectHops` so the loop allows 6 requests (initial + up to 5 followed redirects). No other changes to `SendFollowingRedirectsAsync`: the HTTPS-only per-hop check, 3xx-without-Location terminal behavior, header replay, intermediate disposal, and the error message string all remain identical.
- **Tests:** `tests/CLIHub.Core.Tests/Plugins/PluginFeedClientTests.cs:310-326` — `GetFeedAsync_TooManyRedirects_IsRejected` currently pins the buggy behavior (5 redirects enqueued, 5 requests asserted, failure expected). Update to enqueue 6 redirects, assert 6 requests, and continue to assert failure with "Redirect limit exceeded (maximum 5 hops)." Add one new test `GetFeedAsync_FiveRedirects_AreFollowedAndSucceed` that enqueues 5 redirects then one successful response, asserts 6 total requests (initial + 5 follows), and asserts success.
- **Spec:** `plugin-feed` — add two ADDED scenarios under "Fetch and parse feed index" pinning the boundary explicitly: "Exactly five redirects are followed" (5-redirect chain succeeds, terminal response decides) and "Redirect beyond the hop limit is rejected" (a 6th redirect triggers the limit error). The logo requirement (`:583`) already says "a maximum of 5 hops" and needs no change.
- **Backlog:** `docs/backlog.md` — mark the "Plugin feed cancellation is reported as a download failure" entry fully resolved by this change, since its second sub-item (the redirect off-by-one) was the remaining open work.

## Capabilities

### Modified Capabilities

- `plugin-feed`: Two ADDED scenarios under "Fetch and parse feed index"

### New Capabilities

None.

## Impact

**Behavior change:** One additional redirect is now accepted (5 followed redirects instead of 4). A 5-redirect chain that was previously rejected now succeeds. The boundary was always documented as 5; the code was wrong.

**Code changes:**
- `src/CLIHub.Core/Plugins/PluginFeedClient.cs`: loop condition only (1 character changed from `<` to `<=`)
- `tests/CLIHub.Core.Tests/Plugins/PluginFeedClientTests.cs`: one existing test updated, one new test added

**Spec deltas:**
- `plugin-feed`: MODIFIED "Fetch and parse feed index" — two ADDED scenarios

**Documentation:**
- `docs/backlog.md`: One entry marked resolved

## Explicit Non-Goals

- Do not change the `MaxRedirectHops` constant value or name (5 is correct; the loop was wrong).
- Do not change non-HTTPS rejection, size bounds, timeouts, cancellation handling, the 3xx-without-Location terminal behavior, or conditional-header replay.
- Do not refactor `PluginFeedClient` beyond the loop and its XML doc comment.
- Do not change the plugin feed format, the signature scheme, or the UI.
- Do not add new redirect-limit tests for the package-download or logo-download call sites — the existing feed-index pair and the updated signature-chain test (`GetFeedAsync_SignatureRedirectChainOverTheHopLimit_IsTreatedAsMissing`) cover the shared helper.
- Do not modify `plugin-feed-sync` or `plugin-index-signing` specs — they already inherit the correct transport rules.

## Dependencies

None. This change is self-contained and does not block or depend on other changes.