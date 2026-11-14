# Plugin index signing: Implementation Tasks

## Prerequisites

- Working tree clean at commit 0b2328e.
- `openspec validate --all` passes before starting.
- All existing tests pass: `dotnet test CLIHub.sln`.

## Stage 1: Signature Primitives and Verification Core

### Objective
Create the `PluginFeedSigner` static class, the `PluginFeedSignature` record, and the transition-window constants. Add unit tests with generated test keys. No integration with `PluginFeedClient` yet.

### Tasks

- [ ] 1.1 — Create `PluginFeedSignature.cs` record (in `CLIHub.Core/Plugins`)
  - Record: `PluginFeedSignature { Algorithm, Signature, SignedAt }` with `[JsonPropertyName]` attributes.
  - `Algorithm`: string (e.g. `"ecdsa-p256-sha256"`).
  - `Signature`: string (base64 of the DER-encoded ECDSA signature).
  - `SignedAt`: string? (ISO 8601 UTC timestamp from the envelope).
  - Static `Parse(string json)` method that deserializes from the JSON envelope.
  - Verify: `dotnet build CLIHub.sln`

- [ ] 1.2 — Create `PluginFeedSigner.cs` static class (in `CLIHub.Core/Plugins`)
  - `PublicKeyPem`: const string with the official feed's public key in PEM format. Placeholder value initially; the real key is pasted after the v2 repo key generation.
  - `SignatureRequiredAfter`: `public static readonly DateTimeOffset` constant set to a date ~2 weeks after expected ship (e.g., `2026-12-15`). Document as the transition-window cutoff.
  - `RequireFeedSignature`: `public const bool` = `false` initially. Document as the future flag to flip to `true` in a follow-up slice.
  - `IsSignatureRequired(DateTimeOffset now)`: returns `RequireFeedSignature || now >= SignatureRequiredAfter`.
  - `Verify(byte[] body, byte[] signatureDer, string? publicKeyPem = null)`: imports the PEM key (or the const), creates `ECDsa`, calls `VerifyData(body, signatureDer, HashAlgorithmName.SHA256)`. Returns `bool`.
  - `TryDecodeSignature(string signatureBase64, out byte[] signatureDer)`: converts base64 to bytes; returns false on invalid base64.
  - XML documentation on all public members.
  - Verify: `dotnet build CLIHub.sln`

- [ ] 1.3 — Generate test keys for unit tests
  - Run `openssl ecparam -name prime256v1 -genkey -noout` to create a test private key.
  - Run `openssl ec -in <private.pem> -pubout` to extract the public key.
  - Embed the test public key PEM as a test constant (not the production key).
  - Create a second "wrong" key pair for negative tests.
  - Create a small test body (`"test payload"`) and sign it with the test private key.
  - Save the test constants (keys, sample body, valid signature base64, wrong-key signature, tampered-body) in the test file.
  - Verify: `dotnet build CLIHub.sln`

- [ ] 1.4 — Add `PluginFeedSignerTests.cs` (in `tests/CLIHub.Core.Tests/Plugins/`)
  - Test: `Verify_ValidSignature_ReturnsTrue` — valid body + valid key → true.
  - Test: `Verify_TamperedBody_ReturnsFalse` — modified body + valid key → false.
  - Test: `Verify_WrongKey_ReturnsFalse` — valid body + wrong key → false.
  - Test: `Verify_AlgorithmMismatch_ReturnsFalse` — valid body signed with a different curve → false.
  - Test: `TryDecodeSignature_ValidBase64_ReturnsBytes` — valid base64 decodes.
  - Test: `TryDecodeSignature_InvalidBase64_ReturnsFalse` — garbage base64 → false.
  - Test: `IsSignatureRequired_BeforeCutoffFlagFalse_ReturnsFalse` — transition window open.
  - Test: `IsSignatureRequired_AfterCutoff_ReturnsTrue` — past the cutoff.
  - Test: `IsSignatureRequired_FlagTrueBeforeCutoff_ReturnsTrue` — flag overrides.
  - Test: `Parse_ValidEnvelope_ReturnsSignature` — deserializes JSON envelope.
  - Test: `Parse_MissingAlgorithm_ThrowsOrReturnsNull` — invalid envelope.
  - Run: `dotnet test CLIHub.sln`

- [ ] Gate: All tests pass (`dotnet test CLIHub.sln`); `openspec validate --all`

---

## Stage 2: Signature Fetch and Verify in PluginFeedClient

### Objective
Integrate the signature verification step into `PluginFeedClient.FetchFeedAsync`. Add the signature GET transport, the 4 KB bound, the transition-window logic, and the hard error on invalid signature. Modify `PluginFeedResult` to carry signature status. Add transport-level and integration tests.

### Tasks

- [ ] 2.1 — Modify `PluginFeedResult.cs`
  - Add `SignatureValid: bool` init-only property (default `true` — backwards-compatible for non-default feeds).
  - Add `SignatureWarning: string?` init-only property (non-null when a missing signature was accepted during transition).
  - Document both.
  - Verify: `dotnet build CLIHub.sln`

- [ ] 2.2 — Add signature fetch to `PluginFeedClient.FetchFeedAsync`
  - After `ReadBoundedAsync` returns the body and before `ParseFeedIndex`:
    1. If the feed URL is NOT the default (`feedUrl != FeedUrlProvider.DefaultFeedUrl`), skip verification.
    2. Derive the signature URL: `new Uri(new Uri(feedUrl), "index.sig").AbsoluteUri`.
    3. Fetch `index.sig` via `SendFollowingRedirectsAsync` with a new `MaxSignatureSizeBytes = 4096` constant and a new `SignatureErrorProfile`.
    4. Read the bounded response body (UTF-8 string, ≤ 4 KB).
    5. Parse the JSON envelope via `PluginFeedSignature.Parse`.
    6. Validate `algorithm`: must be `"ecdsa-p256-sha256"` (the only supported value today; anything else is a hard error: "Unsupported signature algorithm: {algorithm}").
    7. Decode the base64 signature via `PluginFeedSigner.TryDecodeSignature`.
    8. Verify via `PluginFeedSigner.Verify(bodyBytes, signatureDer)`.
    9. On valid: proceed to `ParseFeedIndex` with `SignatureValid: true`.
    10. On invalid: return `FeedFetchOutcome` with hard error ("Plugin feed signature is invalid. The feed may have been tampered with.") and `AllowCacheFallback: false`.
  - Missing signature (404 or transport error on signature GET):
    1. If `PluginFeedSigner.IsSignatureRequired(DateTimeOffset.UtcNow)` → hard error: "Plugin feed signature is required but missing." with `AllowCacheFallback: false`.
    2. Else → log warning ("Plugin feed signature is missing; accepted during transition window."), proceed to `ParseFeedIndex` with `SignatureValid: true` and `SignatureWarning` set.
  - Signature file exceeds 4 KB → hard error ("Plugin feed signature file too large").
  - Verify: `dotnet build CLIHub.sln`

- [ ] 2.3 — Add `IsDefaultFeedUrl` helper to `FeedUrlProvider`
  - `public static bool IsDefaultFeedUrl(string url)` — compares with `DefaultFeedUrl` using ordinal comparison.
  - Used by `PluginFeedClient` to decide whether to verify.
  - Verify: `dotnet build CLIHub.sln`

- [ ] 2.4 — Add transport-level tests to `PluginFeedClientTests.cs`
  - Test: `GetFeedAsync_ValidSignature_ParsesIndex` — mock: 200 index + 200 signature (valid test key) → success, `SignatureValid: true`.
  - Test: `GetFeedAsync_InvalidSignature_HardError` — mock: 200 index + 200 signature (tampered) → hard error, no cache fallback.
  - Test: `GetFeedAsync_MissingSignatureDuringTransition_Warning` — mock: 200 index + 404 signature; `IsSignatureRequired` returns false → success, `SignatureWarning` set.
  - Test: `GetFeedAsync_MissingSignatureAfterCutoff_HardError` — mock: 200 index + 404 signature; `IsSignatureRequired` returns true → hard error.
  - Test: `GetFeedAsync_SignatureFileExceeds4KB_Rejected` — mock: 200 index + 200 signature (5000 bytes) → hard error.
  - Test: `GetFeedAsync_SignatureFetchUsesHttps` — mock: non-HTTPS signature URL → rejected.
  - Test: `GetFeedAsync_CustomFeedUrl_SkipsVerification` — mock: custom feed URL → no signature fetch, success.
  - Test: `GetFeedAsync_304_DoesNotFetchSignature` — mock: 304 on index → no signature GET, uses cached verified index.
  - Test: `GetFeedAsync_UnsupportedAlgorithm_HardError` — mock: signature envelope with `algorithm: "ed25519"` → hard error.
  - Test: `GetFeedAsync_SignatureTransportErrorDuringTransition_Warning` — mock: 200 index + 500 signature GET; transition window open → warning.
  - Ensure all existing `PluginFeedClientTests` pass with fixture updates only.
  - Run: `dotnet test CLIHub.sln`

- [ ] Gate: All tests pass (`dotnet test CLIHub.sln`); `openspec validate --all`

---

## Stage 3: Background Sync Integration and Cache/304 Interplay

### Objective
Ensure the background sync service's refresh path includes signature verification (it reuses `PluginFeedClient.GetFeedAsync`, so this is mostly verifying the path). Add tests for 304 and cache-fallback interplay with signature verification.

### Tasks

- [ ] 3.1 — Verify the sync service integration path
  - `PluginFeedSyncService` calls `IPluginFeedClient.GetFeedAsync` → which calls `FetchFeedAsync` → which now includes signature verification. No code changes needed in the sync service.
  - Document in code comments that a signature verification failure in the sync path publishes no event and logs at Error (the `GetFeedAsync` error path already handles this — `PluginFeedResult.Success == false` → no event published per existing logic).
  - Verify: `dotnet build CLIHub.sln`

- [ ] 3.2 — Add sync-level tests to `PluginFeedSyncServiceTests.cs`
  - Test: `Refresh_ValidSignature_PublishesEvent` — the sync service calls `GetFeedAsync`, which verifies the signature; on success, the event is published as normal.
  - Test: `Refresh_InvalidSignature_DoesNotPublishEvent` — signature verification fails → `Success == false` → no event.
  - Test: `Refresh_304_DoesNotRefetchSignature` — 304 response trusts cached verified state.
  - Test: `Refresh_MissingSignatureDuringTransition_PublishesEvent` — missing signature accepted with warning → `Success == true` → event published (with warning logged).
  - Run: `dotnet test CLIHub.sln`

- [ ] Gate: All tests pass (`dotnet test CLIHub.sln`); `openspec validate --all`

---

## Stage 4: v2 Repo Artifact Specification and Documentation

### Objective
Document the v2 repo artifacts (key generation, `sign-index.ps1`, workflow changes, public key, transition plan) without editing the v2 repo. Update project documentation. Run final validation.

### Tasks

- [ ] 4.1 — Write the repo-side artifact specification
  - Document in a `docs/architecture/plugin-index-signing-repo.md` (or as a section in `docs/architecture/plugins.md`):
    - Key generation procedure (`openssl ecparam -name prime256v1`).
    - `tools/sign-index.ps1` specification (script logic, inputs, outputs, openssl command).
    - `.github/workflows/generate-index.yml` changes (signing step placement in both push and PR modes, secret reference, file committed in index-regen PR).
    - `public.pem` publication and its relationship to `PluginFeedSigner.PublicKeyPem`.
    - Transition plan timeline.
  - Verify: `dotnet build CLIHub.sln` (docs-only, no code change)

- [ ] 4.2 — Update `docs/architecture/plugins.md`
  - Add a "Feed Index Signing" section describing: signature format, verification point, trust anchor, custom-feed behavior, transition window.
  - Add cross-reference to ADR-0015 and the deviation note (ECDSA P-256, not Ed25519).
  - Verify: `dotnet build CLIHub.sln`

- [ ] 4.3 — Update `docs/project-context.md`
  - Add a sentence to the runtime facts: "The plugin feed index is cryptographically signed (ECDSA P-256); the client verifies the signature before parsing."
  - Verify: `dotnet build CLIHub.sln`

- [ ] 4.4 — Update `docs/adr/0015-plugin-registry-v2.md`
  - Add a note to the Signing section: "Implementation: ECDSA P-256 (not Ed25519 — unavailable in .NET 10 without third-party dependencies). The signature envelope's `algorithm` field allows future rotation."
  - Verify: `dotnet build CLIHub.sln`

- [ ] 4.5 — Final validation
  - Run `dotnet test CLIHub.sln` — all tests pass.
  - Run `openspec validate --all` — 0 errors.
  - Run `graphify update .` — refresh the knowledge graph.
  - Manual smoke: sign a test index with the test key, serve over a local HTTPS endpoint, verify the client verifies it correctly.

- [ ] Gate: All tests pass; `openspec validate --all` reports 0 errors; `graphify update .` succeeds.

---

## Spec Deltas Produced

- `specs/plugin-index-signing/spec.md` — new capability: signature format, verification pipeline, trust anchor, transition window.
- `specs/plugin-feed/spec.md` — delta: modified "Fetch and parse feed index" with signature verification scenarios.
- `specs/plugin-feed-sync/spec.md` — delta: modified "Background feed refresh" and "Last-good index cache" with signature-aware behavior.