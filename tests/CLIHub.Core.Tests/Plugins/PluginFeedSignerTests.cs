namespace CLIHub.Core.Tests.Plugins;

using CLIHub.Core.Plugins;

/// <summary>
///   Covers the feed signature primitive: the DER verification path over raw body bytes, the
///   base64 decoder, the transition-window rule, and the <c> index.sig </c> envelope parser.
/// </summary>
public class PluginFeedSignerTests
{
    [Fact]
    public void Verify_ValidSignature_ReturnsTrue()
    {
        var signature = Convert.FromBase64String(PluginFeedSignatureTestData.ValidSignatureBase64);

        var valid = PluginFeedSigner.Verify(
            PluginFeedSignatureTestData.PayloadBytes,
            signature,
            PluginFeedSignatureTestData.TestPublicKeyPem);

        Assert.True(valid);
    }

    [Fact]
    public void Verify_TamperedBody_ReturnsFalse()
    {
        var signature = Convert.FromBase64String(PluginFeedSignatureTestData.ValidSignatureBase64);

        var valid = PluginFeedSigner.Verify(
            PluginFeedSignatureTestData.TamperedPayloadBytes,
            signature,
            PluginFeedSignatureTestData.TestPublicKeyPem);

        Assert.False(valid);
    }

    [Fact]
    public void Verify_WrongKey_ReturnsFalse()
    {
        var valid = PluginFeedSigner.Verify(
            PluginFeedSignatureTestData.PayloadBytes,
            Convert.FromBase64String(PluginFeedSignatureTestData.WrongKeySignatureBase64),
            PluginFeedSignatureTestData.TestPublicKeyPem);

        Assert.False(valid);
    }

    [Fact]
    public void Verify_SignatureFromADifferentCurve_ReturnsFalse()
    {
        // A P-384 signature over the same body never verifies under a P-256 key.
        var valid = PluginFeedSigner.Verify(
            PluginFeedSignatureTestData.PayloadBytes,
            Convert.FromBase64String(PluginFeedSignatureTestData.P384SignatureBase64),
            PluginFeedSignatureTestData.TestPublicKeyPem);

        Assert.False(valid);
        Assert.False(PluginFeedSigner.Verify(
            PluginFeedSignatureTestData.PayloadBytes,
            Convert.FromBase64String(PluginFeedSignatureTestData.ValidSignatureBase64),
            PluginFeedSignatureTestData.P384PublicKeyPem));
    }

    [Fact]
    public void Verify_NoKeyOverride_VerifiesAgainstTheEmbeddedTrustAnchor()
    {
        // The embedded constant must import as a usable P-256 key; a signature from another key pair
        // is rejected, never accepted by default.
        var valid = PluginFeedSigner.Verify(
            PluginFeedSignatureTestData.PayloadBytes,
            Convert.FromBase64String(PluginFeedSignatureTestData.ValidSignatureBase64));

        Assert.False(valid);
        Assert.Contains("BEGIN PUBLIC KEY", PluginFeedSigner.PublicKeyPem, StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_UndecodableSignature_ReturnsFalse()
    {
        var valid = PluginFeedSigner.Verify(
            PluginFeedSignatureTestData.PayloadBytes,
            [1, 2, 3, 4],
            PluginFeedSignatureTestData.TestPublicKeyPem);

        Assert.False(valid);
    }

    [Fact]
    public void TryDecodeSignature_ValidBase64_ReturnsBytes()
    {
        var decoded = PluginFeedSigner.TryDecodeSignature(
            PluginFeedSignatureTestData.ValidSignatureBase64,
            out var signatureDer);

        Assert.True(decoded);
        Assert.Equal(Convert.FromBase64String(PluginFeedSignatureTestData.ValidSignatureBase64), signatureDer);
    }

    [Fact]
    public void TryDecodeSignature_InvalidBase64_ReturnsFalse()
    {
        var decoded = PluginFeedSigner.TryDecodeSignature("not base64 at all !!!", out var signatureDer);

        Assert.False(decoded);
        Assert.Empty(signatureDer);
    }

    [Fact]
    public void TryDecodeSignature_EmptyValue_ReturnsFalse()
    {
        Assert.False(PluginFeedSigner.TryDecodeSignature(string.Empty, out _));
        Assert.False(PluginFeedSigner.TryDecodeSignature("   ", out _));
    }

    [Fact]
    public void IsSignatureRequired_BeforeCutoffWithFlagFalse_ReturnsFalse()
    {
        var now = PluginFeedSigner.SignatureRequiredAfter.AddDays(-1);

        Assert.False(PluginFeedSigner.IsSignatureRequired(now, requireSignature: false));
    }

    [Fact]
    public void IsSignatureRequired_OnTheCutoff_ReturnsTrue()
    {
        Assert.True(PluginFeedSigner.IsSignatureRequired(PluginFeedSigner.SignatureRequiredAfter));
    }

    [Fact]
    public void IsSignatureRequired_AfterCutoff_ReturnsTrue()
    {
        var now = PluginFeedSigner.SignatureRequiredAfter.AddYears(1);

        Assert.True(PluginFeedSigner.IsSignatureRequired(now, requireSignature: false));
    }

    [Fact]
    public void IsSignatureRequired_FlagTrueBeforeCutoff_ReturnsTrue()
    {
        var now = PluginFeedSigner.SignatureRequiredAfter.AddDays(-30);

        Assert.True(PluginFeedSigner.IsSignatureRequired(now, requireSignature: true));
    }

    [Fact]
    public void IsSignatureRequired_ProductionFlagIsFalseAndCutoffIsThePublishedDate()
    {
        Assert.False(PluginFeedSigner.RequireFeedSignature);
        Assert.Equal(new DateTimeOffset(2026, 12, 15, 0, 0, 0, TimeSpan.Zero), PluginFeedSigner.SignatureRequiredAfter);
        Assert.False(PluginFeedSigner.IsSignatureRequired(PluginFeedSigner.SignatureRequiredAfter.AddTicks(-1)));
    }

    [Fact]
    public void Parse_ValidEnvelope_ReturnsSignature()
    {
        var signature = PluginFeedSignature.Parse(
            PluginFeedSignatureTestData.Envelope(PluginFeedSignatureTestData.ValidSignatureBase64));

        Assert.NotNull(signature);
        Assert.Equal("ecdsa-p256-sha256", signature.Algorithm);
        Assert.Equal(PluginFeedSignatureTestData.ValidSignatureBase64, signature.Signature);
        Assert.Equal("2026-11-14T01:55:08Z", signature.SignedAt);
    }

    [Fact]
    public void Parse_EnvelopeWithUnknownFields_IgnoresThem()
    {
        var json = """
            {"algorithm":"ecdsa-p256-sha256","signature":"AAAA","signedAt":null,
             "keyId":"rotation-2","countersignature":"ignored"}
            """;

        var signature = PluginFeedSignature.Parse(json);

        Assert.NotNull(signature);
        Assert.Equal("AAAA", signature.Signature);
        Assert.Null(signature.SignedAt);
    }

    [Fact]
    public void Parse_MissingAlgorithm_ReturnsNull()
    {
        var signature = PluginFeedSignature.Parse("""{"signature":"AAAA"}""");

        Assert.Null(signature);
    }

    [Fact]
    public void Parse_MissingSignature_ReturnsNull()
    {
        var signature = PluginFeedSignature.Parse("""{"algorithm":"ecdsa-p256-sha256"}""");

        Assert.Null(signature);
    }

    [Fact]
    public void Parse_InvalidJson_ReturnsNull()
    {
        Assert.Null(PluginFeedSignature.Parse("{ not json"));
        Assert.Null(PluginFeedSignature.Parse(string.Empty));
    }

    [Fact]
    public void Parse_EnvelopeAndVerification_RoundTripToTrue()
    {
        var body = PluginFeedSignatureTestData.PayloadBytes;
        var signatureDer = PluginFeedSignatureTestData.Sign(body);
        var envelope = PluginFeedSignature.Parse(
            PluginFeedSignatureTestData.Envelope(Convert.ToBase64String(signatureDer)));

        Assert.NotNull(envelope);
        Assert.True(PluginFeedSigner.TryDecodeSignature(envelope.Signature, out var decoded));
        Assert.True(PluginFeedSigner.Verify(body, decoded, PluginFeedSignatureTestData.TestPublicKeyPem));
    }
}
