# Embed the real signing key: Design

## Verified Current State

### Placeholder key (src/CLIHub.Core/Plugins/PluginFeedSigner.cs:53-58)

```csharp
public const string PublicKeyPem = """
    -----BEGIN PUBLIC KEY-----
    MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEvfe3Dc5TjrjbJw/TWUzPS7xIl/CT
    FX66o1ieA0Zl2gLlAxtRQ9nG3nOmaOwHzigiyJsIJGxNgse6OqnM2bZsAg==
    -----END PUBLIC KEY-----
    """;
```

The `<remarks>` block (lines 47-52) documents this as a placeholder.

### Real public key (CLIHub-plugins-v2/public.pem, committed 2026-11-14)

```
-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEOHK1Rt/rH6LgLrUD/IkvGj+dDUb8
RSm8qjVVhybAhOZpy277i5Lk4ycFCJal6gXhQFJaHpPb2DLy71I4qyOUxg==
-----END PUBLIC KEY-----
```

This is the same SubjectPublicKeyInfo PEM shape — a `const string` raw string literal.

### Test that touches the embedded key (tests/CLIHub.Core.Tests/Plugins/PluginFeedSignerTests.cs:64-75)

```csharp
[Fact]
public void Verify_NoKeyOverride_VerifiesAgainstTheEmbeddedTrustAnchor()
{
    var valid = PluginFeedSigner.Verify(
        PluginFeedSignatureTestData.PayloadBytes,
        Convert.FromBase64String(PluginFeedSignatureTestData.ValidSignatureBase64));
    Assert.False(valid);
    Assert.Contains("BEGIN PUBLIC KEY", PluginFeedSigner.PublicKeyPem, StringComparison.Ordinal);
}
```

This test asserts the embedded key is PEM-shaped but does **not** assert its value. It verifies
that a test signature (from a different key pair) fails — which holds for ANY embedded key that
isn't the test key. The `Contains("BEGIN PUBLIC KEY")` check is weak: a broken paste that still
starts with that header would pass. No test verifies the key parses as a valid EC key.

### Release-gate docs (docs/architecture/plugin-index-signing-repo.md:123, 132, 139)

- **Line 123:** "`PluginFeedSigner.PublicKeyPem` currently holds a placeholder, and a CI check that
  compares it with `public.pem` is a reasonable later addition."
- **Line 132:** "Replace the placeholder in `PluginFeedSigner.PublicKeyPem` with the real
  `public.pem` content and ship the CLIHub release that verifies signatures."
- **Line 139:** "**Release gate:** step 4 must not ship with the placeholder key in place. With a
  mismatched key, every signed index fails verification — a hard error with no cache fallback — so
  the client release that enables verification and the key replacement ship together."

### Spec (openspec/specs/plugin-index-signing/spec.md)

The spec requires an embedded public key constant (line 116-118: "no file I/O, registry read, or
configuration field can override it") but never mentions the placeholder or a specific PEM value.
The `PublicKeyPem` constant name does not appear in the spec text.

## Decisions

### D1: Direct value swap — same constant shape

**Decision:** Replace the PEM string literal in-place. Keep the `const string` declaration, the raw
string literal (`"""`), and the `public` accessibility. Update only the `<remarks>` block.

**Rationale:**
- The constant's declaration shape is the contract: `public const string PublicKeyPem`. Changing its
  type, accessibility, or literal form would be a behavioral change requiring spec delta.
- A raw string literal avoids escaping and matches the PEM source exactly.
- The `<remarks>` block conveys the compilation intent; replace the placeholder narrative with a
  dated production-anchor statement plus a rotation reference.

### D2: Guard test — `ImportFromPem` success

**Decision:** Add one test:

```csharp
[Fact]
public void PublicKeyPem_IsValidEcKey_ImportsSuccessfully()
{
    using var ecdsa = ECDsa.Create();
    var exception = Record.Exception(() => ecdsa.ImportFromPem(PluginFeedSigner.PublicKeyPem));
    Assert.Null(exception);
}
```

**Rationale:**
- `ECDsa.ImportFromPem` throws `ArgumentException` for malformed PEM, a PEM that is not an EC key
  (e.g., RSA), or a PEM with garbage content. A successful import proves the key is well-formed.
- The existing `Contains("BEGIN PUBLIC KEY")` check in `Verify_NoKeyOverride_VerifiesAgainstTheEmbeddedTrustAnchor`
  only proves the string contains that header — a typo in the base64 body, a wrong curve, or an
  RSA key would all pass that check but fail `ImportFromPem`.
- This is a cheap, non-flaky, zero-network test with zero new test data.

**Alternative rejected — pin the exact PEM bytes:** A full PEM assertion would duplicate the
production key value in two files (source + test), creating a consistency burden with no security
benefit — the source constant IS the truth.

**Alternative rejected — no new test:** The existing `Verify_NoKeyOverride_VerifiesAgainstTheEmbeddedTrustAnchor`
test already exercises the embedded key indirectly (it's the default argument to `Verify`), but a
broken paste would produce a `CryptographicException` from `ImportFromPem` inside `Verify`, which is
caught and returned as `false` — indistinguishable from a signature mismatch. The guard test makes
the paste error visible.

### D3: Docs — update release-gate section only

**Decision:** Edit only `docs/architecture/plugin-index-signing-repo.md`:
- Line 123: Change "currently holds a placeholder" to state the key is embedded as of this change.
- Transition timeline step 4: Mark done — add a dated completion note.
- Line 139: Change "must not ship" to past tense — the release gate is satisfied.

Do not edit:
- `docs/architecture/plugins.md` — no mention of the placeholder (verified by grep).
- `docs/project-context.md` — no mention of the placeholder (verified by read).
- `docs/adr/0015-plugin-registry-v2.md` — no mention of the placeholder; the amendment names the
  constant but says nothing about its current value.
- Archived change files — historical record, never edited.

### D4: skip_specs — justified

**Decision:** Set `skip_specs: true` in `.openspec.yaml`.

**Justification:**
- The spec `plugin-index-signing/spec.md` never mentions the placeholder, `PublicKeyPem`, or any
  specific PEM value. It requires an embedded constant (line 116-118), which this change preserves.
- The change is purely a value swap within an existing constant. The behavior contract — what
  verification does, how the transition window works, what scope the trust anchor has — is unchanged.
- A delta that added "the key is now the real one" would be vacuous: the spec doesn't enumerate
  which key is real, only that one exists.
- `openspec validate --all` passes cleanly before this change (36 passed, 0 failed, pre-existing
  warnings only). No delta means no validation change.

## Deviations

None. This change does exactly what the release gate promised: swap the placeholder for the real key.

## Open Questions for Implementation

### OQ-1: Does the smoke test need the live feed to be published with the matching key?

**Answer:** Yes. The real key in `public.pem` was generated from the same pair as the
`INDEX_SIGNING_KEY` secret. The v2 repo's CI already signs every index regeneration. The smoke
task confirms that Browse Plugins loads without a signature error — the real key embedded in the
build must match the signatures the v2 repo publishes.

### OQ-2: Could an old cached index (verified under a different key or cached during the transition
window before verification) interfere with the smoke test?

**Answer:** No. Unverified index bytes are never cached (design.md implementation note, line 421-426
of the archived change). The feed cache at `%APPDATA%\CLIHub\cache\feed-index.json` is either empty
(fresh install) or contains verified bytes from a previous run. If it's empty, the live fetch runs
signature verification. If it's populated, the cached bytes were verified before caching. In neither
case is a stale unverified index served. However, the smoke task should note: delete
`%APPDATA%\CLIHub\cache\feed-index.json` before smoke to guarantee a fresh signature fetch.