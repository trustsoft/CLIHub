namespace CLIHub.Core.Tests.Plugins;

using System.Security.Cryptography;
using System.Text;

/// <summary>
///   Test-only ECDSA key material for the feed signature tests. The keys were generated for the test
///   suite only (<c> openssl ecparam -name prime256v1 </c> and <c> secp384r1 </c>) and have no
///   relationship to the production trust anchor embedded in
///   <see cref="CLIHub.Core.Plugins.PluginFeedSigner.PublicKeyPem" />.
/// </summary>
internal static class PluginFeedSignatureTestData
{
    /// <summary> The payload the fixed signatures below were produced over. </summary>
    public const string Payload = "test payload";

    /// <summary> A one-byte-extension of <see cref="Payload" />, used as the tampered body. </summary>
    public const string TamperedPayload = "test payload!";

    /// <summary> Signature over <see cref="Payload" /> by <see cref="TestPrivateKeyPem" /> (openssl, DER). </summary>
    public const string ValidSignatureBase64 =
        "MEYCIQDjI30I77K7O1pudFJHUv+Ob9ghqYWKTo5wuA3xQb3bCAIhAKftep2lo+Fpp6oJ1CdjORPI5sCJ3yghP/plQ2pJI9rP";

    /// <summary> Signature over <see cref="Payload" /> by the wrong key pair (openssl, DER). </summary>
    public const string WrongKeySignatureBase64 =
        "MEQCIA8hm5IcAnibWcNaw8AhS3YbvqY2wd7iYEv4PxdrReTgAiAg07lPxT//9J1ic9/m7Ipnt0wpyt7rOR+erp2LxpYcnw==";

    /// <summary> Signature over <see cref="Payload" /> by the P-384 key pair (different curve, DER). </summary>
    public const string P384SignatureBase64 =
        "MGYCMQDmrFyt7FPoQGrGPHNQmXlA2HTAGSQZe/fuwb7TLnrzYLU1kpXeDD0tm6FsJzxmuFICMQC7Dt1a/FzcP7UYOts2Sw6/qh8005mIIOxAEwdQCyUH8RJbdmXZ52cIL7yjwPXK984=";

    /// <summary> Public key of the test pair the valid signature belongs to (P-256). </summary>
    public const string TestPublicKeyPem = """
        -----BEGIN PUBLIC KEY-----
        MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEQ3BG66EC1ivHXZHzE1qtvTTatkh8
        VJ7KYvUoNsgloZKQOSOvPcLM1DH4j3OkUCF8ty2fgnOySGrSL7nGhp8OCw==
        -----END PUBLIC KEY-----
        """;

    /// <summary> Public key of a second, unrelated P-256 pair (negative tests). </summary>
    public const string WrongPublicKeyPem = """
        -----BEGIN PUBLIC KEY-----
        MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE9vB3yp8/BQrndPP4DMzWXD9eJNCc
        VJqad4T3nT/JpBHYjJiWF6BRW7McivCnfy+UZvuS5wPTuQgeuJ+JmlJuBQ==
        -----END PUBLIC KEY-----
        """;

    /// <summary> Public key on a different curve (P-384), used for the algorithm/curve mismatch test. </summary>
    public const string P384PublicKeyPem = """
        -----BEGIN PUBLIC KEY-----
        MHYwEAYHKoZIzj0CAQYFK4EEACIDYgAEVMqfo2tEVqrIzwCQe+yJ2U+VRuooPZe/
        0qQKKPj7IX9jv5ISSy9WawUuegpUoBGvaIJSkqhoFRO+0tdwN5JgRf21RFDnDl1L
        PoWE/33Mlfwt9mgAejEHWs95NDu4gQtV
        -----END PUBLIC KEY-----
        """;

    /// <summary> Private key of the test pair; lets tests sign arbitrary feed bodies. </summary>
    public const string TestPrivateKeyPem = """
        -----BEGIN EC PRIVATE KEY-----
        MHcCAQEEINGaHkkXRGhqs+oFwsY3gvYr08Es9NH/YC63DWHiMlaRoAoGCCqGSM49
        AwEHoUQDQgAEQ3BG66EC1ivHXZHzE1qtvTTatkh8VJ7KYvUoNsgloZKQOSOvPcLM
        1DH4j3OkUCF8ty2fgnOySGrSL7nGhp8OCw==
        -----END EC PRIVATE KEY-----
        """;

    /// <summary> The UTF-8 bytes of <see cref="Payload" />. </summary>
    public static byte[] PayloadBytes => Encoding.UTF8.GetBytes(Payload);

    /// <summary> The UTF-8 bytes of <see cref="TamperedPayload" />. </summary>
    public static byte[] TamperedPayloadBytes => Encoding.UTF8.GetBytes(TamperedPayload);

    /// <summary>
    ///   Signs a body with the test private key in the same DER format openssl produces
    ///   (<see cref="DSASignatureFormat.Rfc3279DerSequence" />).
    /// </summary>
    /// <param name="body"> The exact body bytes to sign. </param>
    /// <param name="privateKeyPem"> Key override; the test key when null. </param>
    /// <returns> The DER signature bytes. </returns>
    public static byte[] Sign(byte[] body, string? privateKeyPem = null)
    {
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(privateKeyPem ?? TestPrivateKeyPem);
        return ecdsa.SignData(body, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
    }

    /// <summary> Builds an <c> index.sig </c> envelope around a base64 signature value. </summary>
    /// <param name="signatureBase64"> The base64 signature value. </param>
    /// <param name="algorithm"> The declared algorithm. </param>
    /// <returns> The envelope JSON. </returns>
    public static string Envelope(string signatureBase64, string algorithm = "ecdsa-p256-sha256") =>
        $$"""{"algorithm":"{{algorithm}}","signature":"{{signatureBase64}}","signedAt":"2026-11-14T01:55:08Z"}""";
}
