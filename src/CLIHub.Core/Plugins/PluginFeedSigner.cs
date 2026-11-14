namespace CLIHub.Core.Plugins;

using System.Security.Cryptography;

/// <summary>
///   Verifies the official plugin feed index signature: ECDSA over NIST P-256 with SHA-256 in a JSON
///   envelope (<see cref="PluginFeedSignature" />) that covers the raw index body bytes. Also carries
///   the embedded public key trust anchor and the time-bounded transition window for a missing
///   signature. Core stays WPF-free; verification is a plain cryptographic primitive with no runtime
///   configuration surface.
/// </summary>
/// <remarks>
///   ADR-0015 reserved a feed signature but named Ed25519; .NET 10 ships no standalone Ed25519
///   signing class (only unsupported composite post-quantum algorithms), so this slice uses the
///   built-in ECDSA P-256 primitive (ADR-0015 amendment dated 2026-11-14).
/// </remarks>
public static class PluginFeedSigner
{
    /// <summary>
    ///   The only algorithm an <c> index.sig </c> envelope may declare today. The envelope field
    ///   exists so a later slice can rotate the primitive — for example to Ed25519 if .NET gains it —
    ///   without renaming the file.
    /// </summary>
    public const string SupportedAlgorithm = "ecdsa-p256-sha256";

    /// <summary>
    ///   Optional hard requirement for a signature, independent of the date: when true, a missing
    ///   signature is a hard error even before <see cref="SignatureRequiredAfter" />. False in this
    ///   slice; flipping it to true is the follow-up slice that makes signatures mandatory for good.
    ///   It is a compile-time constant on purpose, so no preference or configuration file can silence
    ///   the requirement.
    /// </summary>
    public const bool RequireFeedSignature = false;

    /// <summary>
    ///   The end of the transition window: from this instant on, a missing signature is a hard error.
    ///   A compile-time constant on purpose, so the cutoff cannot be moved by editing configuration.
    /// </summary>
    public static readonly DateTimeOffset SignatureRequiredAfter = new(2026, 12, 15, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    ///   The official feed's ECDSA P-256 public key (SubjectPublicKeyInfo PEM), the trust anchor for
    ///   every default-feed index. It is a compile-time constant: no file, registry value, or
    ///   configuration field can replace it at runtime. Rotating the key is a source change plus a
    ///   release, matching the manual rotation procedure in <c> docs/architecture/plugins.md </c>.
    /// </summary>
    /// <remarks>
    ///   Placeholder until the signing key pair is generated for <c> trustsoft/CLIHub-plugins-v2 </c>:
    ///   the public half of that pair must be pasted here, and the matching <c> public.pem </c>
    ///   published in the feed repository, before a CLIHub release ships with verification enabled.
    ///   Until then a real signed feed fails verification (a hard error, never an accepted state).
    /// </remarks>
    public const string PublicKeyPem = """
        -----BEGIN PUBLIC KEY-----
        MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEvfe3Dc5TjrjbJw/TWUzPS7xIl/CT
        FX66o1ieA0Zl2gLlAxtRQ9nG3nOmaOwHzigiyJsIJGxNgse6OqnM2bZsAg==
        -----END PUBLIC KEY-----
        """;

    /// <summary>
    ///   Whether a missing feed signature must be treated as a hard error at <paramref name="now" />:
    ///   true once <see cref="RequireFeedSignature" /> is set or the transition window has closed.
    /// </summary>
    /// <param name="now"> The current UTC time. </param>
    /// <returns> True when a missing signature is a hard error. </returns>
    public static bool IsSignatureRequired(DateTimeOffset now) =>
        IsSignatureRequired(now, RequireFeedSignature);

    /// <summary>
    ///   Evaluates the transition rule for an explicit flag value: true means signatures are required
    ///   regardless of the date. The follow-up slice that makes the requirement configurable, and the
    ///   tests covering both sides of the cutoff, share this single rule.
    /// </summary>
    /// <param name="now"> The current UTC time. </param>
    /// <param name="requireSignature"> Whether signatures are unconditionally required. </param>
    /// <returns> True when a missing signature is a hard error. </returns>
    public static bool IsSignatureRequired(DateTimeOffset now, bool requireSignature) =>
        requireSignature || now >= SignatureRequiredAfter;

    /// <summary>
    ///   Verifies a DER-encoded ECDSA signature over the exact feed index body bytes.
    /// </summary>
    /// <param name="body"> The raw index body bytes as received, before any JSON parsing. </param>
    /// <param name="signatureDer"> The DER-encoded (RFC 3279) signature bytes. </param>
    /// <param name="publicKeyPem">
    ///   Public key override for tests; null verifies against the embedded <see cref="PublicKeyPem" />.
    /// </param>
    /// <returns>
    ///   True when the signature is valid for the body and the key; false otherwise, including a
    ///   signature the platform cannot decode.
    /// </returns>
    public static bool Verify(byte[] body, byte[] signatureDer, string? publicKeyPem = null)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(signatureDer);

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(publicKeyPem ?? PublicKeyPem);

        try
        {
            // The signer (openssl dgst -sign) emits an RFC 3279 DER SEQUENCE, and the format must be
            // named explicitly: the parameterless .NET default is IEEE P1363 raw r||s, which never
            // matches an openssl signature.
            return ecdsa.VerifyData(
                body,
                signatureDer,
                HashAlgorithmName.SHA256,
                DSASignatureFormat.Rfc3279DerSequence);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    /// <summary>
    ///   Decodes the envelope's base64 signature value into DER bytes.
    /// </summary>
    /// <param name="signatureBase64"> The base64 value from the <c> index.sig </c> envelope. </param>
    /// <param name="signatureDer"> The decoded DER bytes, or an empty array when decoding fails. </param>
    /// <returns> True when the value is base64 and decodes to at least one byte. </returns>
    public static bool TryDecodeSignature(string signatureBase64, out byte[] signatureDer)
    {
        signatureDer = [];
        if (string.IsNullOrWhiteSpace(signatureBase64))
        {
            return false;
        }

        try
        {
            signatureDer = Convert.FromBase64String(signatureBase64);
            return signatureDer.Length > 0;
        }
        catch (FormatException)
        {
            signatureDer = [];
            return false;
        }
    }
}
