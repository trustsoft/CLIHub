# Plugin index signing — cryptographic signature verification of the v2 feed index

## Why

The plugin feed index (`index.json`) is the integrity anchor for the entire plugin ecosystem: descriptor-only installs trust the inline descriptor in the index, and package-backed installs trust the package URL + SHA-256 checksum recorded there. Today the index is unsigned — any compromise of the GitHub repository or the `raw.githubusercontent.com` CDN edge could inject malicious entries. ADR-0015 reserved `index.sig` and a null `signature` field for this slice; the backlog entry "Index signing for the v2 feed" (§docs/backlog.md:200-207) reiterates: "generate an ed25519 key pair, sign index.json in the CI workflow, and verify the signature on the client before parsing; treat a missing signature as valid only during a defined transition window."

**Deviation from ADR-0015:** .NET 10 has no built-in pure Ed25519 signing class (`System.Security.Cryptography.Ed25519` does not exist — verified against the 10.0.15 reference assembly and API docs). Ed25519 appears only in composite post-quantum algorithms (`CompositeMLDsaAlgorithm.MLDsa44WithEd25519`, `MLDsa65WithEd25519`), both unsupported on all platforms. The project is conservative about dependencies (5 packages, none crypto), and adding one for a single algorithm is disproportionate. The design substitutes **ECDSA with NIST P-256** (built-in `System.Security.Cryptography.ECDsa`), which is well-supported, produces small signatures (~70 bytes DER), and verifies fast. The `algorithm` field in the signature envelope allows future rotation to Ed25519 if .NET ever ships it.

## What Changes

Five items, S-A–S-E. None weakens a security guarantee; the full guarantee inventory is in `design.md`.

- **S-A — Signature format + verification primitive.** A new `PluginFeedSigner` static class in Core encapsulating ECDSA P-256 verification via `ECDsa.ImportFromPem` + `VerifyData`. The on-disk signature format is a JSON envelope (`index.sig`): `{ "algorithm": "ecdsa-p256-sha256", "signature": "<base64-DER>", "signedAt": "ISO8601" }`. The algorithm field enables future rotation without renaming files. The signature covers the exact `index.json` body bytes (UTF-8).

- **S-B — Client fetch + verify pipeline in `PluginFeedClient`.** After fetching the index body and before `ParseFeedIndex`, the client fetches `index.sig` from the same URL directory (a second GET using the same `SendFollowingRedirectsAsync` machinery, bounded to 4 KB), verifies the signature against the body bytes, and only then proceeds to parse. Invalid signature = hard error (new user-facing string: "Plugin feed signature is invalid. The feed may have been tampered with."). Missing signature = accepted with a warning during the transition window, hard error after. 304 responses trust the already-verified cached index (no re-fetch of the signature file). Cache-fallback from a failed live fetch also trusts the already-verified cached state — the fallback does not bypass verification.

- **S-C — Trust anchor.** The official feed's public key is an embedded PEM constant in Core (`PluginFeedSigner.PublicKeyPem`). Verification applies only to the default feed URL (`FeedUrlProvider.DefaultFeedUrl`); custom `pluginFeedUrl` feeds skip verification — the user chose the URL, so they trust the operator. `FeedUrlProvider` already distinguishes these cases at `src/CLIHub.Core/Plugins/FeedUrlProvider.cs:20-41`.

- **S-D — Repo-side artifacts (specified only; not edited).** In `trustsoft/CLIHub-plugins-v2`: key generation via `openssl ecparam -name prime256v1`, signing via `tools/sign-index.ps1` using `openssl dgst -sha256 -sign`, workflow integration in `.github/workflows/generate-index.yml` after the index generation step (both push/auto-PR and PR-check modes), and public key publication as `public.pem`. The transition plan: publish signatures before the CLIHub release that ships verification.

- **S-E — Tests.** Verification tests with generated test keys (valid, tampered body, wrong key, missing signature in transition mode, missing in strict mode, algorithm mismatch). Fetch plumbing tests (signature GET uses same transport rules, 4 KB bound enforced, redirect policy matches). 304/cache interplay tests (304 trusts cached verified state, cache fallback does not bypass verification). All existing feed and sync tests pass with fixture updates only.

## Capabilities

### Added Capability: `plugin-index-signing`

Delta in `specs/plugin-index-signing/spec.md` (this change):

- **ADDED "Signature format and verification primitive"** — ECDSA P-256 verification of the index body against an `index.sig` JSON envelope; algorithm field for rotation.
- **ADDED "Signature fetch and verify pipeline"** — second GET for `index.sig` over same transport, verification before parse, hard error on invalid, transition-window tolerance for missing.
- **ADDED "Trust anchor for official feed only"** — embedded public key constant, default-feed-only verification, custom feeds skipped.
- **ADDED "Transition window for missing signatures"** — config flag or constant with time cutoff so "missing = accepted" cannot persist silently forever.

### Modified Capability: `plugin-feed`

Delta in `specs/plugin-feed/spec.md` (this change):

- **MODIFIED "Fetch and parse feed index"** — new scenarios: signature verification before parse, valid signature succeeds, invalid signature hard error, missing signature accepted during transition, missing signature rejected after cutoff. Original 16 scenario names preserved; new scenarios alongside them.
- **ADDED scenarios for transport of `index.sig`:** same HTTPS/redirect/hop/4 KB bound as index fetch.

### Modified Capability: `plugin-feed-sync`

Delta in `specs/plugin-feed-sync/spec.md` (this change):

- **MODIFIED "Background feed refresh"** — the background sync uses the same verification pipeline; a signature verification failure publishes no event and logs at Error.
- **MODIFIED "Last-good index cache"** — a cached index was already verified before caching; a 304 trusts the verified state without re-fetching the signature.

## Impact

**Modified code:**

- `src/CLIHub.Core/Plugins/PluginFeedSigner.cs` — new static class with `Verify(byte[] body, byte[] signature)` and public key constant.
- `src/CLIHub.Core/Plugins/PluginFeedSignature.cs` — new record for the JSON envelope (`algorithm`, `signature`, `signedAt`).
- `src/CLIHub.Core/Plugins/PluginFeedClient.cs` — signature fetch step in `FetchFeedAsync` (after `ReadBoundedAsync`, before `ParseFeedIndex`), signature GET transport, 304/cache interplay.
- `src/CLIHub.Core/Plugins/PluginFeedResult.cs` — new `SignatureValid` and `SignatureWarning` properties on result.
- `src/CLIHub.Core/Plugins/IPluginFeedClient.cs` — updated XML doc for signature verification.
- `src/CLIHub.Core/Plugins/FeedUrlProvider.cs` — add `IsDefaultFeedUrl(string)` helper.
- Tests: `tests/CLIHub.Core.Tests/Plugins/PluginFeedSignerTests.cs` (new), modified `PluginFeedClientTests.cs` (signature scenarios), modified `PluginFeedSyncServiceTests.cs` (verification in sync path).
- Docs: `docs/architecture/plugins.md` (signing section), `docs/project-context.md` (runtime facts), `docs/adr/0015-plugin-registry-v2.md` (note the deviation from Ed25519).

**Unchanged:** feed index schema, registry document schema, plugin descriptor format, install/seeding flows, all existing user-facing error strings except the new signature-error strings, HTTPS/redirect/size/checksum rules, `CLIHub.Core` WPF-free constraint, all preferences/config fields.

**Verification:** `dotnet build CLIHub.sln` + `dotnet test CLIHub.sln` after every stage; `openspec validate --all`; `graphify update .` at the end; manual smoke test of signature verification with a signed test index served from a local HTTPS endpoint.

## Non-Goals

- No changes to the feed index schema (`signature` field stays null; index-level signing covers it).
- No key rotation automation (document the manual rotation procedure).
- No signing of packages (index only, per ADR-0015's rejected alternative).
- No changes to install flows.
- No edits to the v2 repo — this change specifies the repo-side artifacts; the orchestrator will deliver them separately.

## Dependencies

- Base: commit 0b2328e (clean working tree). No other active changes (`openspec list` reports none). No file overlap with concurrent work.
- Follows: archived `2026-11-14-plugin-feed-background-sync` and `2026-11-14-plugin-registry-consolidation`.
- Consumes, does not modify: `PluginFeedClient.FetchFeedAsync` (inserts verify step), `FeedUrlProvider.GetFeedUrl` (reads to decide verify vs skip), `SendFollowingRedirectsAsync` (reused for signature GET).
- Blocks: no later changes are blocked. Future work: key rotation automation, Ed25519 migration if .NET adds it.

## Risk

- **Deviation from ADR-0015 (Ed25519 → ECDSA P-256):** the ADR calls for ed25519 but .NET 10 lacks it. ECDSA P-256 is a sound alternative with zero new dependencies. The algorithm field in the envelope allows future rotation.
- **Missing signature accepted during transition window:** a motivated attacker who can block the signature file (but not the index) could serve an unsigned index during the transition window. Mitigated by the time-bounded cutoff and the logged warning.
- **Custom feeds skip verification:** a user who sets a custom feed URL is responsible for its trustworthiness. This is consistent with the existing design where custom feeds are user-controlled.
- **4 KB bound on signature file:** a legitimate future algorithm could produce a larger signature envelope. Mitigated by a generous 4 KB bound (current ECDSA P-256 DER + base64 + JSON overhead is ~150 bytes) and the separate bound constant.

## Non-negotiable Guarantees

1. **HTTPS/redirect/hop-cap/bounds on every fetch:** the signature GET uses the same `SendFollowingRedirectsAsync` with manual HTTPS enforcement, 5-hop cap, and a 4 KB bound as the index GET.
2. **2 MB index bound preserved:** the index body is fetched and bounded before signature verification, unchanged from today.
3. **Verification-before-parse:** the signature is verified against the raw body bytes before `ParseFeedIndex` is called.
4. **Every existing user-facing error string preserved:** the new "signature invalid" and "signature missing" strings are the only additions.
5. **Tolerant cache behavior does not bypass trust decisions:** a 304 trusts the already-verified cached index; a cache fallback from a failed live fetch uses the already-verified cached index; unverified data never enters the cache.
6. **Transition window is explicit and time-bounded:** a constant `SignatureRequiredAfter` with a UTC date, plus a boolean `RequireFeedSignature` flag, so "missing = accepted" cannot persist silently forever.
7. **CLIHub.Core stays WPF-free:** all signing logic is in Core.