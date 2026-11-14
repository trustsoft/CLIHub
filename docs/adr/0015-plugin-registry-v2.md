# ADR-0015: Plugin Registry v2 — Separate Feed Repository

**Date:** 2026-11-14  
**Status:** Accepted  
**Amended:** 2026-11-14 — see [the amendment](#amendment-2026-11-14--ecdsa-p-256-replaces-the-reserved-ed25519-plan):
the signing slice uses ECDSA P-256, not Ed25519.
**Related:** ADR-0004 (plugin catalog and precedence), ADR-0014 (no migrations; tolerant reads), `docs/architecture/plugins.md`, `docs/architecture/plugin-index-signing-repo.md`, `docs/backlog.md` (registry v2 roadmap)

## Context

The plugin feed is a static `index.json` (schema version 1) in the `trustsoft/CLIHub-plugins`
repository, fetched over `raw.githubusercontent.com`. Clients reject `schemaVersion` values above 1,
so the index format cannot evolve in place without breaking deployed versions.

Four structural limitations motivated a redesign rather than another schema-1 field:

- The registry is three loosely-coupled sources of truth — the plugins folder, `plugin-state.json`,
  and the feed — which drift apart (state entries without folders, folders without state entries,
  `installedVersion: null` reading as "update available").
- The feed is fetched only when the Browse Plugins dialog opens; update availability is invisible
  until the user looks.
- Content hashes exist only for built-in plugins (three-hash seeder logic); installed plugins are
  never re-verified on scan.
- The reserved `signature` field has never carried a value, and the trust anchor is implicitly
  "whoever controls the repository".

## Decision

**A new feed repository, `trustsoft/CLIHub-plugins-v2` (branch `master`), is the registry v2 home.
The existing `trustsoft/CLIHub-plugins` repository freezes at its current content** — it stays
reachable for already-shipped clients, receives no new plugins, and the client default switches to
v2 in an upcoming release. Custom `pluginFeedUrl` values are untouched, and the schema-1 parsing
path remains for custom feeds.

**Repository layout — per-plugin folders are the source of truth, the index is generated:**

```text
index.json              # CI-generated, schemaVersion 2
index.sig               # reserved for the signature slice; absent until then
plugins/<id>/plugin.json
plugins/<id>/logo.png   # optional
```

A GitHub Actions workflow on merge to `master` validates every `plugins/<id>/plugin.json` against the
descriptor schema and generates `index.json` from them; branch protection on `master` blocks a broken
descriptor from publishing. Hand-editing `index.json` is not allowed.

**Index schema v2 entries add:** the plugin's descriptor inline (descriptor-only plugins install
without a zip package — the index in a signed git repository is the integrity anchor), a
`compatibility` range instead of `minAppVersion` only, a `channel` field (only `stable` for now),
and a `signature` field (null until the signing slice).

**Client behavior:** the feed client parses both schema versions (1 for custom feeds, 2 for the
default); descriptor-only installs write the descriptor files directly (logo through the existing
logo cache) with no zip, no per-package checksum step; the zip path with SHA-256 verification stays
for heavier packages. `FeedUrlProvider.DefaultFeedUrl` points at the v2 index after the switch.

**Signing:** `index.sig` is reserved now; the key, the CI signing step, and client verification are
a later slice. Clients treat a missing signature as valid until verification ships.

**Local registry consolidation** — one registry document as the single local source of truth
(content hash for every installed plugin, tolerant read, adoption of existing `plugin-state.json`
contents without a migration runner, per ADR-0014) — is committed to as a roadmap slice with its
own change; this ADR fixes the direction, not the document schema.

Rejected alternatives:

- *Evolve `index.json` in the existing repository* — rejected: schema-1 clients reject the new
  format; an in-place change either splits the feed or breaks old versions.
- *Handwritten `index.json`* — rejected: drift between the index and per-plugin descriptors is
  inevitable; generation makes the folders the single source of truth and blocks broken
  descriptors at PR time.
- *Signing from day one* — rejected: key management plus client verification is its own slice;
  reserving the file and field costs nothing.
- *Per-package signatures* — rejected: one trusted feed; the signed index already anchors package
  URLs and checksums.

## Consequences

**Easier:** the index schema evolves without touching deployed clients (parallel feeds); adding a
plugin is one PR with one folder; broken descriptors are stopped before publication; rollback of
the default switch is a one-line patch (point `DefaultFeedUrl` back at v1).

**Harder:** two feed repositories exist during the transition — the frozen v1 keeps old entries
available but takes no new content; every plugin addition requires a PR in the v2 repository; the
validation/generation workflow is new CI infrastructure to own.

**Committed to:** static JSON feeds with no server component; descriptor-only fast path as the
primary install shape; tolerant dual-schema parsing for as long as custom v1 feeds exist; CI
generation as the only index authoring path.

## Amendment (2026-11-14) — ECDSA P-256 replaces the reserved Ed25519 plan

The signing slice is implemented, and it does **not** use Ed25519. The ADR's "Signing" section and the
backlog entry both named ed25519; that part of the direction is superseded.

**Why.** .NET 10 ships no standalone Ed25519 signing class. Verified on 2026-11-14 against the
`Microsoft.NETCore.App.Ref` 10.0.15 reference assembly and its XML documentation: the only Ed25519
entries in `System.Security.Cryptography` are the composite post-quantum properties
`CompositeMLDsaAlgorithm.MLDsa44WithEd25519` and `MLDsa65WithEd25519`, both unsupported on every
platform .NET reports. The Microsoft Learn namespace listing for .NET 10.0 has no Ed25519 signing type
(`X25519DiffieHellman` is key exchange, not signing). Using Ed25519 would therefore mean adding a
third-party dependency (NSec, BouncyCastle, NaCl) to a project that carries five package references,
none of them cryptographic — a supply-chain and maintenance cost that is disproportionate for one
algorithm.

**Decision.** The signature primitive is `System.Security.Cryptography.ECDsa` over the NIST P-256 curve
with SHA-256, emitted as an RFC 3279 DER signature and carried base64-encoded in a JSON envelope
(`index.sig`): `{ "algorithm": "ecdsa-p256-sha256", "signature": "<base64 DER>", "signedAt": "..." }`.
The `algorithm` field keeps the rotation path open: if .NET ever ships Ed25519, a new envelope value
changes only the verification dispatch, not the file name, the repository layout, or the client trust
model.

**Consequences.** Signatures are ~70–72 bytes DER (~96 base64 characters), verification is a single
`VerifyData` call, and the only platform requirement is the Windows CNG backend CLIHub already targets.
The trust anchor is the compile-time constant `PluginFeedSigner.PublicKeyPem`; only the default official
feed is verified, and custom `pluginFeedUrl` feeds keep skipping verification (the user chose the
operator). Client verification ships behind a time-bounded transition window
(`SignatureRequiredAfter`, 2026-12-15 UTC) plus a compile-time `RequireFeedSignature` flag, so a missing
signature cannot remain silently acceptable.

**Unchanged by this amendment.** `index.sig` stays the reserved file name, the per-entry `signature`
field stays null and unverified (the index-level signature covers every entry, package URL, and
checksum), per-package signing stays rejected, and "signing from day one" stays rejected as a
sequencing choice — it is now the realized slice rather than a reserved intention.

