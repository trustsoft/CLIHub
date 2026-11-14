# Feed Index Signing — Repository-Side Artifacts

This document specifies the artifacts the feed repository `trustsoft/CLIHub-plugins-v2` needs so its
`index.json` can be signed and verified by CLIHub clients. It is a specification only: the CLIHub
repository does not contain or edit those artifacts. The client side lives in
`src/CLIHub.Core/Plugins/PluginFeedSigner.cs` (primitive and trust anchor) and
`src/CLIHub.Core/Plugins/PluginFeedClient.cs` (fetch-and-verify pipeline); the capability contract is
`openspec/specs/plugin-index-signing/spec.md`, and ADR-0015 (with its 2026-11-14 amendment) records why
the primitive is ECDSA P-256 rather than the originally reserved Ed25519.

## Signature Format

`index.sig` sits next to `index.json` and is a JSON envelope:

```jsonc
{
  "algorithm": "ecdsa-p256-sha256",            // the only accepted value today
  "signature": "MEYCIQDjI30I77K7O1pudFJHUv...", // base64 of the DER (RFC 3279) signature
  "signedAt": "2026-11-14T01:55:08Z"           // ISO 8601 UTC, informational
}
```

The signature covers the **exact bytes of `index.json` as stored in the repository** — the same bytes
`raw.githubusercontent.com` serves. There is no canonicalization: no re-serialization, no whitespace
normalization, no BOM insertion, no line-ending conversion. Any of those would change the body and
break verification. The client verifies over the raw response bytes before parsing.

## Key Generation (one-time, manual)

Run on a maintainer machine with `openssl` available; keep the private key out of the repository and
out of shell history where possible.

```bash
openssl ecparam -name prime256v1 -genkey -noout -out private.pem
openssl ec -in private.pem -pubout -out public.pem

# Store this value as the INDEX_SIGNING_KEY Actions secret (base64 of the PEM, so quoting and
# newlines survive the secret round-trip):
base64 -w0 private.pem
```

- `private.pem` is never committed and never leaves the maintainers' control.
- `public.pem` **is** committed at the repository root (transparency). Its content is embedded in the
  CLIHub source as `PluginFeedSigner.PublicKeyPem`, the compile-time trust anchor.
- The secret is named `INDEX_SIGNING_KEY` in the repository's Actions secrets.

## `tools/sign-index.ps1` Specification

A new PowerShell script in the feed repository that produces `index.sig` from the committed
`index.json`. Runs on `ubuntu-latest` with `pwsh` and the preinstalled `openssl`.

**Inputs**

| Source | Value |
|---|---|
| `-IndexPath` | `index.json` (default) |
| `-OutputPath` | `index.sig` (default) |
| `INDEX_SIGNING_KEY` env | base64 of the PEM private key (CI secret) |
| `-Verify` | switch: verify an existing `index.sig` instead of writing one |

**Steps**

1. Refuse to run without `INDEX_SIGNING_KEY` when signing (fail loudly: a silently unsigned index
   defeats the whole mechanism).
2. Decode the base64 secret into a temporary file created with owner-only permissions
   (`chmod 600` on Linux; `New-TemporaryFile` under the runner's temp directory), and register the
   delete in a `finally` block so the key never outlives the step.
3. Sign the file's bytes directly — never a re-serialized copy:
   `openssl dgst -sha256 -sign <temp-key> -out <temp-sig> index.json`.
   The DER output is ~70–72 bytes for P-256.
4. Base64-encode the DER bytes (`[Convert]::ToBase64String`), build the envelope
   `{ algorithm = 'ecdsa-p256-sha256'; signature = <base64>; signedAt = <UTC now 'yyyy-MM-ddTHH:mm:ssZ'> }`,
   and write it UTF-8 without BOM.
5. Delete the temporary key and signature files immediately, in both the success and failure paths.
6. Write a one-line summary (`index.sig written for index.json (<n> bytes)`) and exit non-zero on any
   failure so the workflow step fails.

**`-Verify` mode** (used by the pull-request check): decode `index.sig`, verify it against
`index.json` with the committed `public.pem`
(`openssl dgst -sha256 -verify public.pem -signature <der> index.json`), and exit non-zero when the
signature is missing, stale, or malformed. This is the guard that catches a hand-edited `index.json`
or a signature left over from the previous index.

## Workflow Changes (`.github/workflows/generate-index.yml`)

The signing step runs **after** index generation and **before** the file is committed.

**Push mode (`push` to `master`)** — the regeneration PR must contain both files, so `index.sig` is
added to the branch and the commit:

1. `./tools/generate-index.ps1 ...` (unchanged).
2. `./tools/sign-index.ps1` — produces `index.sig` from the freshly generated `index.json`.
3. The existing "Open PR with regenerated index" step must stage `index.json` **and** `index.sig`
   (`git add index.json index.sig`) and base its "unchanged" short-circuit on both files, so the PR
   opens when either changed.
4. The `on.push.paths` filter should include `tools/sign-index.ps1` and `public.pem`.

**PR-check mode (`pull_request` to `master`)** — the advisory messages extend to `index.sig`:

1. `./tools/generate-index.ps1 ...` (unchanged).
2. `./tools/sign-index.ps1 -Verify` when `index.sig` already exists: reports a stale or invalid
   signature instead of silently passing. When the contributor's PR carries no `index.sig`, print an
   advisory ("index.sig is generated by CI after merge") rather than failing — contributors submit
   only `plugins/**`.
3. The stale-index advisory text mentions `index.json` and `index.sig`.

**Secret availability:** secrets are not exposed to workflows triggered from forks. Every signing step
must therefore be guarded (`if: github.event_name == 'push' || github.event.pull_request.head.repo.full_name == github.repository`)
and degrade to the advisory path instead of failing external pull requests.

**Line endings:** the repository must keep `index.json` byte-stable. Adding
`.gitattributes` with `index.json text eol=lf` (and `index.sig text eol=lf`) is recommended once the
index is regenerated from scratch; a line-ending flip changes every byte and invalidates the
signature even though the JSON is semantically identical.

Replacing the reserved block in the workflow: the commented-out `ED25519_PRIVATE_KEY` /
`./tools/sign-index.ps1` scaffolding is deleted and replaced by the ECDSA step above.

## Public Key Publication

`public.pem` (SubjectPublicKeyInfo, `-----BEGIN PUBLIC KEY-----`) is committed at the repository root.
Its exact content is pasted into `PluginFeedSigner.PublicKeyPem` in CLIHub. Keep the two in sync:
`PluginFeedSigner.PublicKeyPem` currently holds a placeholder, and a CI check that compares it with
`public.pem` is a reasonable later addition.

## Transition Timeline

1. Generate the key pair; store the private half as `INDEX_SIGNING_KEY`; commit `public.pem`.
2. Deploy `tools/sign-index.ps1` and the workflow changes. `index.sig` starts appearing with the next
   index regeneration.
3. Wait at least one regeneration cycle so `index.sig` and `index.json` are known to travel together.
4. Replace the placeholder in `PluginFeedSigner.PublicKeyPem` with the real `public.pem` content and
   ship the CLIHub release that verifies signatures. The transition window is open: a missing
   signature is logged as a warning and accepted.
5. On and after `PluginFeedSigner.SignatureRequiredAfter` (2026-12-15 UTC), a missing signature is a
   hard error. A follow-up slice flips `RequireFeedSignature` to `true` to enforce it regardless of the
   date.

**Release gate:** step 4 must not ship with the placeholder key in place. With a mismatched key, every
signed index fails verification — a hard error with no cache fallback — so the client release that
enables verification and the key replacement ship together.

## Key Rotation

1. Generate a new pair and add its public half as `public.pem.rotation` (or a dated file).
2. Ship a CLIHub release that accepts signatures from either key (a rotation slice: the envelope
   already carries `algorithm`, and a `keyId` field can be added without breaking older clients, whose
   envelope parser ignores unknown fields).
3. Wait for adoption, then make the new key the only embedded trust anchor.
4. Update the `INDEX_SIGNING_KEY` secret last.

A rotation never reuses the old private key, and never ships a build that trusts a key that has not
yet produced a signature in the feed repository.

## Maintainer Verification Checklist

- `index.sig` exists next to `index.json` in the repository root after a regeneration PR merges.
- `openssl dgst -sha256 -verify public.pem -signature <(base64 -d index.sig | jq -r .signature) index.json`
  reports `Verified OK` (or `sign-index.ps1 -Verify` passes in CI).
- The client accepts the index without a transition warning (`SignatureWarning` stays null).
- Removing `index.sig` on a test branch produces the logged warning before 2026-12-15 and the
  "Plugin feed signature is required but missing." error after it.
