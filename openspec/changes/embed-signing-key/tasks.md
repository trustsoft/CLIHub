# Embed the real signing key: Tasks

- [ ] 1.0 Read the key source files one more time before editing:
  `src/CLIHub.Core/Plugins/PluginFeedSigner.cs`,
  `docs/architecture/plugin-index-signing-repo.md` (lines 120-140),
  `tests/CLIHub.Core.Tests/Plugins/PluginFeedSignerTests.cs` (the guard-test insertion point).

- [ ] 1.1 Replace `PublicKeyPem` in `PluginFeedSigner.cs`:
  - Swap the PEM string literal body (lines 54-57) with the real key:
    ```
    MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEOHK1Rt/rH6LgLrUD/IkvGj+dDUb8
    RSm8qjVVhybAhOZpy277i5Lk4ycFCJal6gXhQFJaHpPb2DLy71I4qyOUxg==
    ```
    Keep the `-----BEGIN PUBLIC KEY-----` / `-----END PUBLIC KEY-----` guards and the raw string
    literal (`"""`).
  - Replace the `<remarks>` block (lines 47-52) with a dated production-anchor statement:
    > This is the production trust anchor from `trustsoft/CLIHub-plugins-v2/public.pem`
    > (published 2026-11-14). Key rotation is manual and documented in
    > `docs/architecture/plugin-index-signing-repo.md` §Key Rotation.
  - Keep the `<summary>` XML doc (lines 41-46) unchanged.
  - **Gate:** `dotnet build CLIHub.sln` — must succeed (the constant is a `const string` with no
    runtime import until `Verify` is called; a syntax error here fails the build).

- [ ] 1.2 Add the guard test in `PluginFeedSignerTests.cs`:
  - Add `using System.Security.Cryptography;` if not already present (it is — `ECDsa` is used in
    the test data file but not in the signer tests).
  - Insert a new `[Fact]` after the existing `Verify_NoKeyOverride_VerifiesAgainstTheEmbeddedTrustAnchor`
    test:
    ```csharp
    [Fact]
    public void PublicKeyPem_IsValidEcKey_ImportsSuccessfully()
    {
        using var ecdsa = ECDsa.Create();
        var exception = Record.Exception(() => ecdsa.ImportFromPem(PluginFeedSigner.PublicKeyPem));
        Assert.Null(exception);
    }
    ```
  - **Gate:** `dotnet test CLIHub.sln --filter "FullyQualifiedName~PluginFeedSignerTests"` —
    all existing signer tests pass, and the new test passes. A broken paste (malformed PEM, wrong
    curve) fails `ImportFromPem` with `ArgumentException` → `Record.Exception` returns non-null →
    `Assert.Null` fails.

- [ ] 1.3 Update `docs/architecture/plugin-index-signing-repo.md`:
  - **Line 123** (~"currently holds a placeholder"): change to:
    > `PluginFeedSigner.PublicKeyPem` is the embedded production anchor (set 2026-11-14).
    > A CI check that compares it with `public.pem` is a reasonable later addition.
  - **Transition timeline step 4** (lines 132-134): add a dated completion line:
    > **Done (2026-11-14).** The placeholder was replaced with the real `public.pem` content.
    > The transition window is open: a missing signature is logged as a warning and accepted.
  - **Line 139** (~"Release gate: step 4 must not ship with the placeholder key in place"):
    change to:
    > **Release gate: satisfied (2026-11-14).** The real key is embedded; the client release
    > that enables verification and the key replacement ship together.
  - **Gate:** Read the section back — the timeline flows logically and the release-gate wording
    is past tense.

- [ ] 1.4 Run the full build and test suite:
  ```powershell
  dotnet build CLIHub.sln
  dotnet test CLIHub.sln
  ```
  All 36 spec suites pass; the new guard test is green; no unrelated failures.

- [ ] 1.5 Run `openspec validate --all` — must report 0 failures (36 passed, pre-existing warnings
  only, same as before this change).

- [ ] 1.6 Run `graphify update .` to refresh the knowledge graph with the changed source file.

- [ ] 1.7 **Manual smoke (blocks commit/archive):**
  - Delete `%APPDATA%\CLIHub\cache\feed-index.json` (and `feed-sync.json`) to force a fresh
    signature fetch.
  - Run CLIHub: `dotnet run --project src/CLIHub/CLIHub.csproj`.
  - Open Browse Plugins from the CLI Agents page. The feed must load with no signature error,
    no "Plugin feed signature is invalid" error, and no warning banner.
  - Confirm plugins are listed and installable.
  - Check the log file at `%APPDATA%\CLIHub\logs\` — no signature-related Error entries,
    `SignatureValid: true` in the feed result (if logged).
  - **Only after this smoke passes:** report the result and ask for commit/archive approval.