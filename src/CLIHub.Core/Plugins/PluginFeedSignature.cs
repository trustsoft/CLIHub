namespace CLIHub.Core.Plugins;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
///   The JSON envelope of the official feed's <c> index.sig </c> file: the declared signing
///   algorithm, the base64-encoded DER signature over the raw index body bytes, and an optional
///   signing timestamp. The algorithm field lets a later slice rotate the primitive without renaming
///   the file.
/// </summary>
/// <param name="Algorithm">
///   Declared signing algorithm; only <see cref="PluginFeedSigner.SupportedAlgorithm" /> is accepted
///   today.
/// </param>
/// <param name="Signature"> Base64 of the DER-encoded ECDSA signature over the raw index body bytes. </param>
/// <param name="SignedAt"> Optional ISO 8601 UTC timestamp of the signing run; informational only. </param>
public sealed record PluginFeedSignature(
    [property: JsonPropertyName("algorithm")] string Algorithm,
    [property: JsonPropertyName("signature")] string Signature,
    [property: JsonPropertyName("signedAt")] string? SignedAt)
{
    /// <summary>
    ///   Parses an <c> index.sig </c> envelope. Unknown fields are ignored, so a newer signer can add
    ///   envelope metadata without breaking older clients.
    /// </summary>
    /// <param name="json"> The envelope document. </param>
    /// <returns>
    ///   The envelope, or null when the document is empty, is not valid JSON, or carries no
    ///   <c> algorithm </c> or no <c> signature </c> value.
    /// </returns>
    public static PluginFeedSignature? Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<PluginFeedSignature>(json);
            if (envelope is null ||
                string.IsNullOrWhiteSpace(envelope.Algorithm) ||
                string.IsNullOrWhiteSpace(envelope.Signature))
            {
                return null;
            }

            return envelope;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
