# Plugin index signing: Design document

## Verified Current State

### .NET 10 Crypto: Ed25519 Absent, ECDSA Available

**Evidence (observed 2026-11-14):**

- **Reference assembly:** `C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\10.0.15\ref\net10.0\System.Security.Cryptography.dll` — string-table grep for "Ed25519" finds only `get_MLDsa44WithEd25519` and `get_MLDsa65WithEd25519`, both `CompositeMLDsaAlgorithm` properties. No standalone `Ed25519` type.
- **XML doc file:** `System.Security.Cryptography.xml` at lines 3187–3213 — only `CompositeMLDsaAlgorithm.MLDsa44WithEd25519` and `MLDsa65WithEd25519` documented.
- **Microsoft Learn API docs:** the `System.Security.Cryptography` namespace listing for .NET 10.0 has zero standalone Ed25519 entries. `X25519DiffieHellman` exists for key exchange (not signing). The cross-platform crypto tables list `MLDSA44-Ed25519-SHA512` and `MLDSA65-Ed25519-SHA512` as **unsupported on all platforms** (Windows, Linux, macOS, Android, Browser).
- **Conclusion:** Pure Ed25519 signing is NOT available in .NET 10 without third-party libraries.

**ECDSA P-256 availability:** `ECDsa` class (in `System.Security.Cryptography.dll` since .NET 6) supports `ImportFromPem`, `VerifyData(byte[], byte[], HashAlgorithmName)`, and named curves including `ECCurve.NamedCurves.nistP256`. Fully supported on Windows 10/11 (CNG backend), the only target platform.

### Project Dependencies (src/CLIHub.Core/CLIHub.Core.csproj)

**File:lines 1–34:**
- 5 package references: `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging`, `Serilog`, `Serilog.Sinks.File`, `Velopack`. None are crypto libraries.
- No BouncyCastle, NSec, NaCl, or other Ed25519 implementations.

### Feed Client (src/CLIHub.Core/Plugins/PluginFeedClient.cs)

**File:lines with evidence:**
- 19–21: Constants: `MaxIndexSizeBytes = 2 * 1024 * 1024`, `MaxPackageSizeBytes = 10 * 1024 * 1024`, `MaxRedirectHops = 5`.
- 107–112: `CreateDefaultHandler()` returns `SocketsHttpHandler` with `AllowAutoRedirect = false`.
- 115–144: `GetFeedAsync(CancellationToken)` — resolves feed URL, builds conditional headers, calls `FetchFeedAsync` via `ExecuteWithStructuredErrorsAsync`, handles cache fallback if allowed.
- 156–248: `FetchFeedAsync(string, IReadOnlyDictionary<string, string>?, CancellationToken)` — follows redirects, handles 304, reads bounded body, parses, saves cache and metadata.
- 224: `var parsed = this.ParseFeedIndex(body);` — **this is the verification insertion point**: after `ReadBoundedAsync` returns the body and before `ParseFeedIndex`.
- 630–698: `SendFollowingRedirectsAsync` — GET request, manual redirect with HTTPS enforcement and 5-hop cap. **Reused for the signature GET.**
- 700–729: `ReadBoundedAsync` — reads body up to `maxBytes`. A separate instance with 4 KB bound will be used for the signature file.
- 374–389: `SaveCachedIndex` — atomic write of raw index bytes. The signature is NOT cached separately; the verified index is cached, and the cached index is trusted on 304/fallback.
- 282–300: `LoadSyncMetadata` — drops metadata when cached index is missing (line 290).
- 398–412: `TryGetCachedFeedResult` — serves cached index on live failure. The cached index was verified before caching, so no re-verification needed.

**Existing error profiles (lines 24–46):**
- `FeedErrorProfile`: "Feed request timed out", "Could not load plugin feed. Check your network connection."
- `PackageErrorProfile`: "Package download timed out", "Package download failed"
- `LogoErrorProfile`: "Logo download timed out", "Logo download failed"

### Feed URL Provider (src/CLIHub.Core/Plugins/FeedUrlProvider.cs)

**File:lines 1–42:**
- 13: `public const string DefaultFeedUrl = "https://raw.githubusercontent.com/trustsoft/CLIHub-plugins-v2/master/index.json";`
- 20–41: `GetFeedUrl(IPreferencesStore)` returns: null/absent → `DefaultFeedUrl`; empty string → null (disabled); non-empty → custom URL.
- **Distinguishing default vs custom:** at `PluginFeedClient.GetFeedAsync` line 117, `feedUrl` is already resolved. Compare `feedUrl == FeedUrlProvider.DefaultFeedUrl` to decide whether to verify the signature.

### Cache Infrastructure (src/CLIHub.Core/Infrastructure/FileSystem/AppPaths.cs)

**File:lines 72–85:**
- `FeedCacheFile`: `%APPDATA%\CLIHub\cache\feed-index.json` — raw index bytes.
- `FeedSyncMetadataFile`: `%APPDATA%\CLIHub\cache\feed-sync.json` — ETag, Last-Modified, timestamp, indexHash.
- No signature cache file exists; the verified index in `FeedCacheFile` is the trust anchor for cached state.

### Sync Metadata (src/CLIHub.Core/Plugins/PluginFeedSyncMetadata.cs)

**File:lines 1–135:**
- Record with `ETag`, `LastModified`, `Timestamp`, `IndexHash`.
- `Load` is tolerant (corrupt → delete + null).
- `Save` is atomic via `AtomicFileWriter`.
- No signature-related fields; no change needed (the verified index is cached).

### Feed Result (src/CLIHub.Core/Plugins/PluginFeedResult.cs)

**File:lines 1–26:**
- `PluginFeedResult(bool Success, PluginFeedIndex? Index, IReadOnlyList<string> Errors)` with `FromCache` and `CachedAt` init-only properties.
- No signature-related properties yet.

### v2 Repo Workflow (D:\Projects\CLIHub-plugins-v2\.github\workflows\generate-index.yml)

**File:lines 1–62:**
- 3–8: Triggers on `pull_request` to master and `push` to master (paths: `plugins/**`, `tools/generate-index.ps1`).
- 16: `runs-on: ubuntu-latest`
- 22: `./tools/generate-index.ps1` — generates `index.json`.
- 26–33: PR mode: reports stale index advisory.
- 35–56: Push mode: opens `index-regen` PR with regenerated `index.json`.
- 58–62: **Reserved signing step** (currently commented out):
  ```yaml
  # - name: Sign index
  #   env:
  #     ED25519_PRIVATE_KEY: ${{ secrets.INDEX_SIGNING_KEY }}
  #   run: ./tools/sign-index.ps1   # writes index.sig
  ```

### v2 Repo Script (D:\Projects\CLIHub-plugins-v2\tools\generate-index.ps1)

**File:lines 1–187:**
- Generates `index.json` from `plugins/<id>/` folders.
- Idempotent: rewrites only when plugin set changes (line 168).
- No signing step; produces only `index.json`.

### ADR-0015 (docs/adr/0015-plugin-registry-v2.md)

**File:lines 56–57:**
> **Signing:** `index.sig` is reserved now; the key, the CI signing step, and client verification are a later slice. Clients treat a missing signature as valid until verification ships.

Line 73: "Signing from day one — rejected: key management plus client verification is its own slice; reserving the file and field costs nothing."

### Backlog Entry (docs/backlog.md)

**File:lines 200–207:**
> ### Index signing for the v2 feed
> - Evidence: ADR-0015 reserves `index.sig` and a null `signature` field; the signing slice was deferred.
> - Proposed remedy: generate an ed25519 key pair, sign `index.json` in the CI workflow (key in Actions secrets), and verify the signature on the client before parsing; treat a missing signature as valid only during a defined transition window.

---

## Decisions

### D1: Crypto Primitive — ECDSA P-256 (not Ed25519)

**Decision:** Use `System.Security.Cryptography.ECDsa` with the NIST P-256 curve (`ECCurve.NamedCurves.nistP256`) and SHA-256 hashing.

**Rationale:**
- .NET 10 has no standalone Ed25519 signing class (verified above). Adding a third-party dependency (NSec, BouncyCastle, NaCl.Net) for a single algorithm is disproportionate for a project with 5 total package references.
- ECDSA P-256 is well-supported on Windows (CNG backend), the only target platform. The `VerifyData` API with `HashAlgorithmName.SHA256` is straightforward.
- Signatures are small: DER-encoded output from `openssl dgst -sha256 -sign` is ~70–72 bytes (base64: ~96 chars).
- Verification is fast: a single call to `ECDsa.ImportFromPem(publicKey).VerifyData(body, signature, HashAlgorithmName.SHA256)`.
- The signature envelope's `algorithm` field (`ecdsa-p256-sha256`) allows future rotation to Ed25519 if .NET ever supports it — the client checks the algorithm field and can dispatch to a different verifier.

**Alternative rejected — RSA-2048 PKCS#1 PSS:** Larger signatures (256 bytes vs ~70), slower verification, and overkill for this use case. ECDSA is the modern default for new asymmetric signing when Ed25519 isn't available.

**Alternative rejected — add NSec or BouncyCastle:** Adds a package dependency, a supply-chain surface, and version management churn for a single algorithm. The project policy is conservative about dependencies.

### D2: Signature Format — JSON Envelope

**Decision:** `index.sig` is a JSON file with three fields:
```json
{
  "algorithm": "ecdsa-p256-sha256",
  "signature": "<base64 of DER-encoded ECDSA signature>",
  "signedAt": "2026-11-14T01:55:08Z"
}
```

**Rationale:**
- A JSON envelope enables algorithm rotation without renaming files — a future `algorithm: "ed25519"` entry changes only the verification dispatch, not the file naming convention.
- The `algorithm` field makes the format self-describing: the verifier reads it and selects the appropriate verification path.
- The `signedAt` timestamp is informational (logged, not enforcement).
- Base64 encoding makes the signature text-safe for JSON. The raw DER output from openssl is ~70–72 bytes; base64 is ~96 characters.
- A `PluginFeedSignature` record (`algorithm`, `signature`, `signedAt`) with `System.Text.Json` serialization keeps the parsing in one place.

**Alternative rejected — raw binary file (64-byte Ed25519 signature):** Couples the file format to one algorithm. A JSON envelope costs ~50 bytes of overhead for extensibility.

**Alternative rejected — signature embedded in index.json (e.g., a `_signature` field):** The signature must cover the exact index body bytes; embedding it in the body creates a circular dependency (you can't sign the signature). A separate file avoids this.

### D3: Signature Coverage — Exact Index Body Bytes

**Decision:** The signature covers the exact `index.json` body bytes as received from the HTTP response (UTF-8 bytes), before any JSON parsing.

**Rationale:**
- This is the simplest and most robust approach: sign the raw bytes, verify the raw bytes.
- No canonicalization needed; no normalization of whitespace or field order.
- The signer (`openssl dgst -sha256 -sign`) and verifier (`ECDsa.VerifyData`) both operate on raw byte arrays.
- The body bytes are already available in `FetchFeedAsync` as `body` (string) and need only `Encoding.UTF8.GetBytes(body)` for verification.

### D4: Verification Point — After Body Read, Before Parse

**Decision:** In `PluginFeedClient.FetchFeedAsync`, after `ReadBoundedAsync` returns the body (line 215) and before `ParseFeedIndex` (line 224), insert a signature verification step:
1. If the feed URL is the default (`FeedUrlProvider.DefaultFeedUrl`), fetch `index.sig` from the same URL path (replace `/index.json` with `/index.sig`).
2. Deserialize the JSON envelope to `PluginFeedSignature`.
3. Verify `ECDsa.VerifyData(bodyBytes, signatureBytes, HashAlgorithmName.SHA256)`.
4. On valid: proceed to `ParseFeedIndex`.
5. On invalid: return hard error (no cache fallback).
6. On missing signature file (404): accept with warning during transition window, hard error after cutoff.

**Rationale:**
- Verification-before-parse is the core security guarantee: an unverified body never reaches `ParseFeedIndex`.
- The signature file is fetched AFTER the index body so the body is available for verification immediately.
- The signature fetch uses the same `SendFollowingRedirectsAsync` machinery as the index fetch, ensuring HTTPS enforcement, 5-hop cap, and probe timeout on every hop.
- A failed signature is a hard error that never degrades to cache — tampering must not be hidden.

### D5: Signature File Transport — Same Rules, 4 KB Bound

**Decision:** The `index.sig` GET uses `SendFollowingRedirectsAsync` with the same HTTPS-only, 5-hop, and timeout rules as the index fetch. The response body is bounded to 4 KB (a new constant `MaxSignatureSizeBytes`).

**Rationale:**
- Consistent transport guarantees: if the index is fetched over HTTPS with redirect enforcement, the signature must be too.
- A standalone 4 KB bound prevents a compromised server from streaming unbounded data through the signature endpoint.
- 4 KB is generous: the current envelope is ~150 bytes; even a future multi-algorithm envelope with RSA-4096 would fit comfortably (~1 KB).
- Conditional request headers (ETag/If-None-Match) are NOT sent for the signature GET — the signature changes with every index update, so a 304 on the signature is unlikely; simplicity wins.

### D6: 304 and Cache Interplay — Trust the Verified Cached State

**Decision:**
1. **304 on index GET:** The cached index was verified before it was cached (D4). A 304 response means the index is unchanged, so the cached verified state is trusted. No signature re-fetch needed.
2. **Failed live fetch → cache fallback:** The cached index was verified when it was persisted. The fallback path (`TryGetCachedFeedResult`, line 398) returns the cached verified index. No re-verification needed.
3. **Index body changes (200) → signature re-verify:** Every fresh 200 response triggers a new signature fetch and verification before the index is parsed and cached.

**Rationale:**
- The guarantee is: data entering the cache is verified. Once cached, it stays trusted.
- Re-fetching the signature on a 304 adds an unnecessary request without security benefit — the index didn't change.
- The signature file is NOT cached separately (the verified index in the feed cache IS the cached state). This avoids synchronizing two cache files.

### D7: Transition Window — Time-Bounded Constant

**Decision:** A `DateTimeOffset` constant `SignatureRequiredAfter` in `PluginFeedSigner` defines the cutoff. Before that date, a missing signature is accepted with a logged warning. After that date, a missing signature is a hard error.

```csharp
// In PluginFeedSigner:
public static readonly DateTimeOffset SignatureRequiredAfter = new(2026, 12, 1, 0, 0, 0, TimeSpan.Zero);
```

Plus a boolean `RequireFeedSignature` constant (default `false` initially) that can be flipped to `true` in a follow-up slice to make missing signatures a hard error regardless of the date.

**Rationale:**
- The cutoff is embedded in the source, not in configuration — it cannot be silently disabled by a config edit or stale preference.
- The combination of a date constant AND a boolean flag gives two independent ways to enforce: the date is the "natural" transition, the flag is the "override" for the follow-up slice that flips to always-required.
- The date is set generously (e.g., 2+ weeks after the expected CLIHub release that ships verification) so the v2 repo has time to start producing signatures before clients demand them.
- A logged warning during the transition window makes the missing-signature state observable.

**Alternative rejected — config preference:** A user-editable config field could be toggled to skip verification. This defeats the purpose — the user should not be able to silence a missing-signature warning indefinitely.

**Alternative rejected — flag only, no date:** A flag without a date creates no natural deadline; the transition can stretch indefinitely. The date creates an explicit commitment.

### D8: Trust Anchor — Embedded Constant, Default Feed Only

**Decision:** The public key is embedded as a `const string` PEM in `PluginFeedSigner`:

```csharp
public const string PublicKeyPem = """
-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE...
-----END PUBLIC KEY-----
""";
```

Verification applies ONLY when the resolved feed URL equals `FeedUrlProvider.DefaultFeedUrl`. Custom feed URLs skip verification entirely.

**Rationale:**
- An embedded constant cannot be modified at runtime (no file I/O, no registry read, no config field). This is the strongest trust-anchor model.
- The constant is updated in source when the key is rotated (manual rotation procedure, documented).
- Custom feeds are user-controlled: the user chose the URL, so they trust the operator. Requiring signature verification for custom feeds would force every custom-feed operator to set up signing infrastructure.
- `FeedUrlProvider.GetFeedUrl` already resolves the effective URL at `PluginFeedClient.cs:117`; comparing it with `DefaultFeedUrl` is a cheap string comparison.

**Alternative rejected — configurable public key path:** Adds filesystem I/O and an attack surface (swap the key file). An embedded constant has zero runtime dependencies.

### D9: Repo-Side Signing — openssl on ubuntu-latest

**Decision:** Signing in the v2 repo CI uses `openssl` (preinstalled on `ubuntu-latest`):

```bash
# tools/sign-index.ps1 (new script, specified here; implementation in v2 repo):
# 1. Read the private key from CI secret (base64-encoded PEM, avoids quoting issues)
# 2. Decode and sign:
#    $privateKey = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($env:INDEX_SIGNING_KEY))
#    $privateKey | openssl dgst -sha256 -sign /dev/stdin index.json | base64 -w0 > sig.b64
# 3. Build the JSON envelope:
#    $sig = [ordered]@{ algorithm = 'ecdsa-p256-sha256'; signature = (Get-Content sig.b64 -Raw).Trim(); signedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ') }
#    ConvertTo-Json $sig -Compress | Set-Content index.sig -Encoding utf8
```

**Key generation procedure (one-time, manual):**
```bash
openssl ecparam -name prime256v1 -genkey -noout -out private.pem
openssl ec -in private.pem -pubout -out public.pem
# Encode the private key for the CI secret:
base64 -w0 private.pem
```

**Workflow integration in `.github/workflows/generate-index.yml`:**
- The signing step runs AFTER the index generation step, in BOTH modes:
  - **Push mode (auto-PR):** After `generate-index.ps1` produces `index.json`, `sign-index.ps1` produces `index.sig`. Both files are committed in the `index-regen` branch.
  - **PR-check mode:** The signing step verifies that a submitted PR produces a valid signature (the secret is available in PR workflows from the same repo). The advisory "index.json differs" message is extended to mention `index.sig`.

**Public key publication:** `public.pem` is committed to the v2 repo root for transparency. Its content is embedded as `PluginFeedSigner.PublicKeyPem` in the CLIHub source.

**Transition plan:**
1. Generate key pair; add private key to `INDEX_SIGNING_KEY` secret in the v2 repo.
2. Deploy the signing workflow + `sign-index.ps1` to the v2 repo. Signatures start appearing in `index.sig`.
3. Wait until signatures have been publishing for at least one index regeneration cycle.
4. Ship the CLIHub release with verification code and the transition window open (missing = warning).
5. After the `SignatureRequiredAfter` cutoff date passes, a follow-up slice flips `RequireFeedSignature` to `true`.

---

## Alternatives Rejected

### A1: Ed25519 via NSec or BouncyCastle (rejected in favor of ECDSA P-256)

**Why:** Adds a package dependency (supply-chain surface, version management) for a single algorithm. The project has 5 dependencies total; adding one for Ed25519 when ECDSA P-256 is built-in and sound is disproportionate. The `algorithm` envelope field preserves the Ed25519 path for the future.

### A2: Raw binary signature file (no JSON envelope) (rejected in favor of JSON envelope)

**Why:** Couples the file format to one algorithm. A JSON envelope costs ~50 bytes of overhead and gives us algorithm rotation, a timestamp, and forward compatibility.

### A3: Sign the index as a header in the HTTP response (rejected in favor of separate file)

**Why:** `raw.githubusercontent.com` does not support custom response headers. A separate `index.sig` file is under our control in the repository.

### A4: Embed the signature in `index.json` (rejected in favor of separate file)

**Why:** The signature must cover the exact body bytes. Embedding the signature in the body creates a circular dependency: to sign, you need the final body; with the signature embedded, the body changes. You'd need a canonicalization scheme to strip the signature before hashing, which is fragile.

### A5: Verify signature for all feeds including custom (rejected in favor of default-only)

**Why:** Custom feed operators are chosen by the user. Requiring them to set up signing infrastructure would break existing custom feeds. The user's decision to use a custom URL is an explicit trust decision.

### A6: Cache the signature file separately (rejected in favor of trusting the verified cached index)

**Why:** The security guarantee is "data entering the cache is verified." A separate signature cache adds synchronization complexity (signature file stale while index is fresh?) without security benefit. The verified index in the cache is the cached trust state.

---

## Open Questions for Implementation

### OQ-1: Signature file URL derivation

The signature URL is derived by replacing `/index.json` with `/index.sig` in the feed URL. What if the feed URL does not end in `/index.json`? Currently, `FeedUrlProvider.DefaultFeedUrl` always ends in `/index.json`. For robustness, the derivation should be: take the directory of the feed URL and append `/index.sig`. Confirm this in implementation.
**Proposed answer:** Use `new Uri(new Uri(feedUrl), "index.sig").AbsoluteUri` — this resolves relative to the feed URL's directory (standard URI resolution). Tested with `DefaultFeedUrl`.

### OQ-2: Should the signature error string be localizable?

Currently, no CLIHub strings are localized; all are hardcoded English. The new signature-error strings follow this pattern.
**Proposed answer:** Not in this slice. Same pattern as all other error strings.

### OQ-3: What happens when the signature GET returns a non-200/non-404 status?

A 404 is "missing signature" (transition window). Any other non-2xx status is treated as a transport error: fall through to the transition-window logic (if within window, warn and proceed; if past cutoff, hard error).
**Proposed answer:** Treat only 200 as "signature available." 404 → missing. Everything else → transport error, same behavior as missing (the transition-window logic applies). This avoids a separate error path for signature transport failures.

### OQ-4: Does the signature verification happen before or after the size-bound check?

The size-bound check (`MaxIndexSizeBytes`, line 205) is a fast-path rejection that runs before the body is read. The signature verification runs after the body is read. If the body exceeds 2 MB, we never reach verification — this is correct, since an oversized index is rejected regardless of signature.
**Proposed answer:** Current order is correct. No change needed.

### OQ-5: Should `PluginFeedSigner` be a static class or an injectable service?

A static class with a const public key is the simplest approach. An injectable service would allow testing with different keys, but the static class already supports that: the `Verify` method takes the public key as a parameter (the const is a default).
**Proposed answer:** Static class with `public static bool Verify(byte[] body, byte[] signature, string? publicKeyPem = null)`. The const is the default; tests pass different keys.

### OQ-6: What's the exact openssl command on ubuntu-latest?

`openssl dgst -sha256 -sign <(echo "$PRIVATE_KEY") index.json | base64 -w0` — but passing the key via process substitution may expose it in the process list. Safer: decode the base64 secret to a temp file, sign, then shred the temp file.
**Proposed answer:** The `sign-index.ps1` script writes the decoded key to a temp file with restricted permissions, signs, and deletes the temp file immediately. Exact implementation deferred to the v2 repo delivery.

---

## Spec Coverage and Deltas

### plugin-index-signing (new capability)

Full scenarios in `specs/plugin-index-signing/spec.md`:
- **Signature format and verification primitive** — valid ECDSA P-256 signature verified, tampered body rejected, wrong key rejected, algorithm mismatch rejected.
- **Signature fetch and verify pipeline** — signature fetched before parse, same transport, 4 KB bound, invalid → hard error, missing → transition-window behavior.
- **Trust anchor for official feed only** — embedded public key constant, default feed verified, custom feed skipped.
- **Transition window for missing signatures** — missing accepted with warning before cutoff, hard error after, flag flips to always-required.

### plugin-feed (modified)

**Existing scenarios preserved (names and substance):**
- "Valid index fetched successfully" — extended: index body is verified before parse.
- All 16 original scenarios unchanged in substance.

**New scenarios:**
- "Valid signature, index parsed successfully" — verification passes before parse.
- "Invalid signature, hard error" — no cache fallback, user-facing error string.
- "Missing signature during transition window" — warning logged, index parsed.
- "Missing signature after cutoff" — hard error.
- "Signature fetch uses HTTPS and redirect rules" — same `SendFollowingRedirectsAsync`.
- "Signature file exceeds 4 KB bound" — rejected.

### plugin-feed-sync (modified)

**Existing scenarios preserved.**

**New scenarios:**
- "Background sync verifies signature" — the sync service's refresh path includes verification.
- "304 does not re-fetch signature" — cached verified state is trusted.
- "Cache fallback trusts verified state" — no re-verification needed.
- "Sync failure on invalid signature" — error logged at Error, no event published.

---

## Implementation Notes (2026-11-14)

Recorded during implementation; each item is a place where the code reality differs from, or sharpens,
the decisions above.

### DER signature format must be named explicitly (corrects D1/D3)

`ECDsa.VerifyData(data, signature, HashAlgorithmName)` defaults to `DSASignatureFormat.IeeeP1363FixedFieldConcatenation`
(raw `r||s`, 64 bytes for P-256). `openssl dgst -sha256 -sign` emits an RFC 3279 **DER SEQUENCE** (~70–72
bytes), so the parameterless call rejects every signature the repository-side signer produces. Measured
against the 10.0.15 runtime: the default overload returns `false` for an openssl DER signature, while
`DSASignatureFormat.Rfc3279DerSequence` returns `true` for the same bytes. `PluginFeedSigner.Verify`
therefore names the DER format. Decoding failures never throw (`CryptographicException` maps to `false`);
a malformed embedded key PEM still throws, which is a build-time error surfaced as a structured feed
failure rather than a silent acceptance.

### Exact body bytes (sharpens D3)

`FetchFeedAsync` now reads the index with the existing bounded **byte** reader and decodes the string from
those bytes, instead of reading a string and re-encoding it. Verification therefore covers exactly the
bytes the server sent; the 2 MB bound and `body`-string consumption by `ParseFeedIndex`/`SaveCachedIndex`
are unchanged.

### Transition window: two-argument rule for the flag (sharpens D7)

`RequireFeedSignature` is a `const`, so no test can flip it. The rule is exposed twice:
`IsSignatureRequired(DateTimeOffset now)` (the production call, using the constant) and
`IsSignatureRequired(DateTimeOffset now, bool requireSignature)` (the pure rule, so the flag-true case and
the follow-up slice share one implementation). The cutoff is final at **2026-12-15T00:00:00Z**.

### `SignatureErrorProfile` maps failures to "missing", not to user-facing errors (clarifies 2.2)

The signature GET runs inside its own `ExecuteWithStructuredErrorsAsync` scope with
`SignatureErrorProfile`, whose strings are diagnostic only: any timeout, network error, or unexpected
exception there is routed to the transition-window rule (missing signature) instead of surfacing a second
user-facing error style. Caller cancellation still rethrows, and an overall probe-timeout that fires while
the signature GET is in flight resolves to the existing "Feed request timed out" path.

### Malformed envelope reports both strings (resolves the spec tension)

`specs/plugin-index-signing` requires "an error describing the malformation" for a malformed envelope,
while `specs/plugin-feed` groups a malformed envelope under the "Plugin feed signature is invalid. The feed
may have been tampered with." scenario. A malformed envelope (invalid JSON, missing `algorithm` or
`signature`, non-base64 value) therefore returns both errors, in that order; the tampered string is used
alone for a signature that decodes but does not verify.

### An index accepted without a signature is not cached (resolves the transition/cache tension)

`specs/plugin-feed-sync` ("only verified index bytes are stored in the cache") and design guarantee 5 are
read literally: a fresh 200 accepted during the transition window is parsed and served, but neither the
index nor the sync metadata is written. A `304` therefore cannot confirm an unverified cache and a cache
fallback cannot present unverified data; the previously cached verified index (if any) stays untouched.

### Transition-accepted refresh stores no comparison hash (verifier finding a)

The background service originally persisted the index hash even when the client refused to cache the
unverified index. The client's orphan check then deleted that hash-only metadata on the next fetch, while
`RefreshAsync` had already read the previous hash, so the stored value alternated between null and a hash
and a spurious `PluginFeedUpdatedEvent` was published on every other refresh on a fresh install (plus two
warnings per refresh). `RefreshAsync` now returns before change detection when
`PluginFeedResult.SignatureWarning` is not null: no hash is written, so the metadata cannot be orphaned and
no change event is published for an index that cannot be cached. `SaveIndexHash` remains the only service
writer of the hash and `PluginFeedClient.SaveSyncMetadata` remains the only client writer (it is
unreachable for a warning result), so no other path persists it. Pinned by
`Refresh_TransitionAcceptedIndex_DoesNotPersistHashOrPublishEvent` and by the end-to-end
`Refresh_MissingSignatureDuringTransition_DoesNotCachePersistHashOrPublishEvent`; the "a change event may be
published" wording of the `plugin-feed-sync` delta is permissive, and this fix resolves it to "not
published while the index cannot be cached".

### Signature URL is anchored to the configured feed URL (answers OQ-1)

`new Uri(new Uri(feedUrl, UriKind.Absolute), "index.sig")` resolves against the **configured** feed URL,
not the URL a redirect chain ended on. If the index redirects to another path or host, the signature is
still fetched from the official directory, so a redirect cannot relocate the trust anchor; a mismatch
fails verification (closed), never open.

### Test seams on `PluginFeedClient` (supports OQ-5 without runtime configurability)

The internal constructor gained optional `TimeProvider` and `feedPublicKeyPem` parameters (used only by
tests, alongside the existing cache-path overrides). Production composition never sets either, so the
trust anchor remains `PluginFeedSigner.PublicKeyPem`, reachable through no file, registry, or preference.
Test key material lives in `tests/CLIHub.Core.Tests/Plugins/PluginFeedSignatureTestData.cs`, a new test-only
helper so the signer, client, and sync tests share one fixture; it holds a test key pair (P-256), a wrong
pair, and a P-384 key, all generated with `openssl ecparam` and unrelated to the production key.

### `PublicKeyPem` is a placeholder (release gate)

Task 1.2 asks for a placeholder until the feed repository's key pair exists. It must be replaced with the
real `public.pem` content before the release that ships verification: with a mismatched key every signed
index fails verification as a hard error.
