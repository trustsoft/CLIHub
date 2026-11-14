# Feed Redirect Hop Limit Off-By-One: Design Document

## Verified Current State

### The off-by-one loop

**`src/CLIHub.Core/Plugins/PluginFeedClient.cs:23`:**
```csharp
private const int MaxRedirectHops = 5;
```

**`src/CLIHub.Core/Plugins/PluginFeedClient.cs:886`:**
```csharp
for (var hop = 0; hop < MaxRedirectHops; hop++)
```

`MaxRedirectHops = 5`, so the loop iterates for `hop = 0, 1, 2, 3, 4` — five iterations, five HTTP requests. Each request is either terminal (non-3xx, 3xx-without-Location) or a redirect. If all five are redirects, the loop falls through to the error at line 930-931:

**`src/CLIHub.Core/Plugins/PluginFeedClient.cs:930-931`:**
```csharp
this.logger.LogWarning("Maximum redirect hops ({MaxHops}) exceeded", MaxRedirectHops);
return (null, $"Redirect limit exceeded (maximum {MaxRedirectHops} hops)");
```

The actual behavior: initial request + at most **4 followed redirects** = 5 requests maximum. A chain of 5 redirects is rejected as exceeding the limit.

### The four call sites

`SendFollowingRedirectsAsync` is shared by:

| Call site | File:line | Test coverage of redirect limit |
|---|---|---|
| Feed index fetch (`FetchFeedAsync`) | `:198` | `GetFeedAsync_TooManyRedirects_IsRejected` at tests `:310-326` |
| Signature fetch (`FetchAndVerifySignatureAsync`) | `:360` | `GetFeedAsync_SignatureRedirectChainOverTheHopLimit_IsTreatedAsMissing` at tests `:1334-1350` (6 redirects, 7 total requests, pins over-limit → missing-signature) |
| Package download (`DownloadPackageAsync`) | `:655` | No redirect-limit-specific test (size/checksum tested) |
| Logo download (`DownloadLogoAsync`) | `:717` | No redirect-limit-specific test |

All four call sites share the identical helper. Fixing the loop in the helper fixes all four. The feed-index pair (`GetFeedAsync_TooManyRedirects_IsRejected` and `GetFeedAsync_FiveRedirects_AreFollowedAndSucceed`) pins the boundary, and the signature-chain test (`GetFeedAsync_SignatureRedirectChainOverTheHopLimit_IsTreatedAsMissing`) separately pins that an over-limit signature redirect chain is treated as a missing signature. Both are updated rather than duplicated. Per-call-site tests for package and logo would test the same shared behavior redundantly.

### The pinned test

**`tests/CLIHub.Core.Tests/Plugins/PluginFeedClientTests.cs:310-326`:**
```csharp
[Fact]
public async Task GetFeedAsync_TooManyRedirects_IsRejected()
{
    for (var i = 0; i < 5; i++)
    {
        this.EnqueueFeed(RedirectResponse($"https://example.com/redirect-{i}"));
    }

    var client = this.CreateClient();

    var result = await client.GetFeedAsync();

    Assert.False(result.Success);
    Assert.Contains("Redirect limit exceeded (maximum 5 hops)", result.Errors);
    Assert.Equal(5, this.handler.Requests.Count);
    Assert.All(this.handler.Requests, request => Assert.Equal(HttpMethod.Get, request.Method));
}
```

This test enqueues exactly 5 redirects (the buggy boundary: 4 followed + 5th request gets the 5th redirect and fails). With the fix, 5 redirects must succeed (6 requests), so this test must be updated. Additionally, a new test for the 5-redirect success case is needed.

### The three spec promises

1. **`openspec/specs/plugin-feed/spec.md:583`** — Logo requirement body:
   > "redirects followed manually with per-hop HTTPS and a **maximum of 5 hops**"

2. **`openspec/specs/plugin-feed-sync/spec.md:179`** — Requirement body:
   > "the same redirect, hop-count, and size-bound guarantees"
   > Scenario "Redirect hop limit enforced" at `:193-196`:
   > "**no more than 5 hops** are followed"

3. **`openspec/specs/plugin-index-signing/spec.md:53`** — Requirement body:
   > "manual redirect enforcement and a **5-hop cap**"

All three specs promise 5 redirect hops. The code delivers at most 4. The fix aligns the code with the specs.

**Note:** The `plugin-feed` spec's "Fetch and parse feed index" requirement (line 8) has no redirect-hop-limit scenario at all. The 5-hop limit is stated only under the logo requirement (`:581-583`), which says feed and package fetches use "the same transport rules." This change adds an explicit scenario to the primary transport-owning requirement.

---

## Decisions

### D1: Fix the loop condition to `hop <= MaxRedirectHops`

**Decision:** Change line 886 from `hop < MaxRedirectHops` to `hop <= MaxRedirectHops`.

**Rationale:** The constant `MaxRedirectHops = 5` is semantically correct (the limit IS 5 redirect hops). The off-by-one is in the loop condition, not in the constant. With `<=`:

- hop = 0: 1st request (initial). If redirect, follow #1.
- hop = 1: 2nd request. If redirect, follow #2.
- hop = 2: 3rd request. If redirect, follow #3.
- hop = 3: 4th request. If redirect, follow #4.
- hop = 4: 5th request. If redirect, follow #5.
- hop = 5: 6th request. If redirect → loop exits (hop becomes 6 > 5) → error.

**Result:** 6 requests maximum = initial + 5 followed redirects. A 6th redirect is rejected. This matches the constant name, the error message text, and all three spec promises.

**Alternative rejected — changing `MaxRedirectHops` to 6:** Makes the constant misleading (the limit is 5 hops, not 6). The error message would then say "maximum 6 hops" which contradicts the specs. The constant is correct; the loop is wrong.

**Alternative rejected — `while (true)` with manual counter:** Adds verbosity without clarity benefit. The `for` loop with `<=` is the minimal, reviewable fix.

**Alternative rejected — renaming the constant to `MaxRequests`:** Would require updating every comment and error message that references "hops." The spec language is "hops"; the code should match.

### D2: Add the redirect-limit scenario to `plugin-feed`, not `plugin-feed-sync` or `plugin-index-signing`

**Decision:** Add two ADDED scenarios to the `plugin-feed` spec under "Fetch and parse feed index": "Exactly five redirects are followed" (5-redirect chain succeeds, terminal response decides) and "Redirect beyond the hop limit is rejected" (a 6th redirect triggers the limit error). Do not modify `plugin-feed-sync` or `plugin-index-signing`.

**Rationale:**
- `plugin-feed` is the canonical owner of the transport rules. The logo requirement (`:581-583`) already delegates to it ("under the same transport rules as feed and package fetches"). The "Fetch and parse feed index" requirement is where the fetch behavior is defined; it is the natural home for the redirect boundary.
- `plugin-feed-sync` (`:179`) says it "maintain[s] the same redirect, hop-count, and size-bound guarantees as the standard feed fetch" — it inherits from `plugin-feed` by reference. Its "Redirect hop limit enforced" scenario says "no more than 5 hops are followed" which is correct and already matches the intended behavior. No wording change is needed.
- `plugin-index-signing` (`:53`) says "a 5-hop cap" as part of its requirement body. This is also correct as-is and inherits the same shared `SendFollowingRedirectsAsync` behavior. No wording change is needed.

### D3: Test strategy — update existing test, add success test, skip per-call-site redundancy

**Decision:**
1. Update `GetFeedAsync_TooManyRedirects_IsRejected` — enqueue 6 redirects (the 6th triggers the rejection), assert 6 requests, assert failure.
2. Add `GetFeedAsync_FiveRedirects_AreFollowedAndSucceed` — enqueue 5 redirects + 1 success (200), assert 6 requests, assert success.
3. Update `GetFeedAsync_SignatureRedirectChainOverTheHopLimit_IsTreatedAsMissing` — enqueue 6 signature-hop redirects (the 6th triggers the limit), assert 7 total requests (1 feed + 6 signature), keep the assertion that the over-limit chain is treated as a missing signature.
4. Do NOT add new redirect-limit tests for the package-download or logo-download call sites.

**Rationale:** The feed-index redirect pair (items 1–2) pins the boundary: 5 redirects succeed, a 6th is rejected. The existing signature-chain test (item 3) separately pins that an over-limit signature redirect chain is treated as a missing signature (transition-window logic), so it is updated rather than duplicated. The four call sites share `SendFollowingRedirectsAsync`; per-call-site redirect tests beyond these would test the same code path redundantly. Package and logo have unique tests for checksum verification and logo staging; the redirect limit is transport infrastructure shared by all.

### D4: Backlog entry — mark fully resolved, preserve cancellation evidence

**Decision:** Update `docs/backlog.md` line 447 "Plugin feed cancellation is reported as a download failure" to `**Status: Resolved by \`feed-redirect-hop-limit\` (2026-11-14)**`, noting that both sub-items are now addressed (cancellation was already resolved, redirect off-by-one is fixed by this change). Keep the cancellation evidence lines intact.

**Rationale:** The backlog entry's two sub-items are both resolved. The entry's evidence block and cancellation-resolution note are preserved for traceability; only the status line changes. Following the file's existing convention (see `:467` "plugin-seeding spec example still names a feed agent" for a resolved-by marker example).

### D5: Error message wording unchanged

**Decision:** Leave the error message as `"Redirect limit exceeded (maximum {MaxRedirectHops} hops)"` — no wording change.

**Rationale:** The message `MaxRedirectHops` (5) and the limit it describes (5 hops) are already correct. The off-by-one was in the loop, not in the message. The message string references `MaxRedirectHops` which remains `5`, and after the fix the message correctly fires when a 6th redirect is encountered.

### D6: XML doc comment update

**Decision:** Update the summary on `SendFollowingRedirectsAsync` (`:863-864`) to change "manually follows HTTP redirects up to the maximum hop count" to "manually follows up to the maximum hop count of HTTP redirects" — making the language consistent with the intended behavior without changing the documented limit.

**Rationale:** The current comment says "up to the maximum hop count" which is ambiguous with the buggy behavior. The rephrasing clarifies that the maximum hop count IS the limit of redirects followed, which matches the corrected behavior.

---

## Open Questions

1. **Should the `MaxRedirectHops` constant be moved to a shared configuration constant rather than a `private const` in `PluginFeedClient`?** Currently only `PluginFeedClient` uses it. If future code needs the same limit independently of `PluginFeedClient`, extraction could be warranted. Not in scope for this fix — the constant is private, not public API, and extraction is a refactoring decision, not a bug fix.

2. **Should the test helper `RedirectResponse` accept a status code parameter for testing 307/308 (method-preserving redirects)?** The current tests all use 301/302-like redirects. Since `SendFollowingRedirectsAsync` doesn't distinguish redirect status codes (it checks `300 <= code < 400`), testing different codes would test SocketsHttpHandler behavior, not `PluginFeedClient` behavior. Not in scope.