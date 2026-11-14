# Embed the real signing key — complete the plugin-index-signing release gate

## Why

The archived `plugin-index-signing` change (2026-11-14) shipped the full signature verification
pipeline with one deliberate gap: `PluginFeedSigner.PublicKeyPem` is a placeholder key, so no signed
feed index can verify successfully against it. The change's design.md §Implementation Notes recorded
this as the release gate:

> `PublicKeyPem` is a placeholder (release gate) … It must be replaced with the real `public.pem`
> content before the release that ships verification: with a mismatched key every signed index fails
> verification as a hard error.

The real key pair was generated, the private half is stored as `INDEX_SIGNING_KEY` in the
`trustsoft/CLIHub-plugins-v2` Actions secrets, `public.pem` is committed and published there, and
the CI workflow now signs every index regeneration (`index.sig` alongside `index.json`). Signing was
verified locally against the published public key (`tools/sign-index.ps1 -Verify` → verified).

The placeholder must now be replaced so the verification pipeline actually verifies the real feed.

## What Changes

- **S-A** — Replace the placeholder `PublicKeyPem` constant in `PluginFeedSigner.cs` with the real
  ECDSA P-256 public key from `trustsoft/CLIHub-plugins-v2/public.pem`. Remove the "Placeholder
  until…" remarks block; replace it with a dated statement that this is the production anchor and
  rotation is manual per `docs/architecture/plugin-index-signing-repo.md`.
- **S-B** — Add a guard test that `PublicKeyPem` imports as a valid EC key (a cheap `ECDsa.ImportFromPem`
  assertion — catches a broken paste that the existing `Contains("BEGIN PUBLIC KEY")` check misses).
  No existing test asserts the old placeholder value.
- **S-C** — Update the release-gate wording in `docs/architecture/plugin-index-signing-repo.md`:
  the transition timeline step 4 is now done, the placeholder reference is removed, and the release
  gate is marked satisfied.
- **S-D** — Manual smoke: with the real key embedded, Browse Plugins must load with no signature
  error against the live signed feed.

## Capabilities

None. This is a value swap within an existing capability. The behavior contract in
`openspec/specs/plugin-index-signing/spec.md` specifies verification, transition-window, and trust-
anchor rules — none of which name a specific PEM value. No spec delta is needed (see `skip_specs`
in `.openspec.yaml`).

## Impact

**Modified code:**
- `src/CLIHub.Core/Plugins/PluginFeedSigner.cs` — `PublicKeyPem` constant value and its XML doc remarks.
- `tests/CLIHub.Core.Tests/Plugins/PluginFeedSignerTests.cs` — one new guard test.

**Modified docs:**
- `docs/architecture/plugin-index-signing-repo.md` — remove placeholder mention, mark step 4 done,
  note release gate satisfied.

**Unchanged:** verification logic, transition window, signature format, feed fetch pipeline, cache
interplay, trust anchor scope, `RequireFeedSignature` flag, all other tests.

## Non-Goals

- No changes to the verification logic, `RequireFeedSignature`, transition window, or spec deltas.
- No edits to archived change files.
- No changes to the feed repository or `index.sig` pipeline.
- No key rotation (that's a future slice per ADR-0015).

## Dependencies

- Base: HEAD `eefea0dd2` (clean working tree, `openspec list` reports no active changes).
- Follows: archived `2026-11-14-plugin-index-signing`.
- Depends on: the real key pair existing and `public.pem` being committed in `trustsoft/CLIHub-plugins-v2`
  (confirmed 2026-11-14).