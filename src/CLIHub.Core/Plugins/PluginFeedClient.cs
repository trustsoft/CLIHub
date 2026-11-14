namespace CLIHub.Core.Plugins;

using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.FileSystem;

using Microsoft.Extensions.Logging;

/// <summary>
///   Fetches the plugin feed index and downloads plugin packages over HTTPS with checksum verification.
/// </summary>
public sealed class PluginFeedClient : IPluginFeedClient, IDisposable
{
    private const string UserAgent = "CLIHub";
    private const int MaxIndexSizeBytes = 2 * 1024 * 1024; // 2 MB
    private const int MaxPackageSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const int MaxSignatureSizeBytes = 4 * 1024; // 4 KB
    private const int MaxRedirectHops = 5;

    private const string SignatureInvalidError = "Plugin feed signature is invalid. The feed may have been tampered with.";
    private const string SignatureMalformedError = "Plugin feed signature envelope is malformed.";
    private const string SignatureRequiredError = "Plugin feed signature is required but missing.";
    private const string SignatureTooLargeError = "Plugin feed signature file too large";
    private const string SignatureMissingWarning = "Plugin feed signature is missing; accepted during transition window.";

    private static readonly StructuredErrorProfile FeedErrorProfile = new(
        TimeoutError: "Feed request timed out",
        RequestError: "Could not load plugin feed. Check your network connection.",
        TimeoutLogMessage: "Feed request timed out",
        RequestLogMessage: "Feed request failed",
        UnexpectedLogMessage: "Feed request failed unexpectedly",
        UnexpectedLogLevel: LogLevel.Warning);

    private static readonly StructuredErrorProfile PackageErrorProfile = new(
        TimeoutError: "Package download timed out",
        RequestError: "Package download failed",
        TimeoutLogMessage: "Package download timed out",
        RequestLogMessage: "Package download failed",
        UnexpectedLogMessage: "Unexpected error during package download",
        UnexpectedLogLevel: LogLevel.Error);

    private static readonly StructuredErrorProfile LogoErrorProfile = new(
        TimeoutError: "Logo download timed out",
        RequestError: "Logo download failed",
        TimeoutLogMessage: "Logo download timed out",
        RequestLogMessage: "Logo download failed",
        UnexpectedLogMessage: "Unexpected error during logo download",
        UnexpectedLogLevel: LogLevel.Warning);

    /// <summary>
    ///   Signature transport failures never surface as user-facing errors: a signature that cannot be
    ///   fetched is treated exactly like a missing one, and the transition-window rule decides whether
    ///   the feed may still be used (design OQ-3). The strings below are diagnostic log messages.
    /// </summary>
    private static readonly StructuredErrorProfile SignatureErrorProfile = new(
        TimeoutError: "Signature request timed out",
        RequestError: "Signature request failed",
        TimeoutLogMessage: "Feed signature request timed out",
        RequestLogMessage: "Feed signature request failed",
        UnexpectedLogMessage: "Feed signature request failed unexpectedly",
        UnexpectedLogLevel: LogLevel.Warning);

    private readonly HttpClient httpClient;
    private readonly IPreferencesStore preferencesStore;
    private readonly IPluginDescriptorReader descriptorReader;
    private readonly IPluginDescriptorValidator descriptorValidator;
    private readonly ILogger<PluginFeedClient> logger;
    private readonly string cacheFilePath;
    private readonly string syncMetadataFilePath;
    private readonly TimeProvider time;
    private readonly string feedPublicKeyPem;

    /// <summary>
    ///   Creates the feed client with a dedicated HTTP handler that does not auto-redirect.
    /// </summary>
    /// <param name="preferencesStore"> Preferences store for timeout and feed URL. </param>
    /// <param name="logger"> Logger. </param>
    /// <param name="descriptorReader"> Reader validating v2 inline descriptors at parse time. </param>
    /// <param name="descriptorValidator"> Validator checking v2 inline descriptors at parse time. </param>
    public PluginFeedClient(IPreferencesStore preferencesStore,
                            ILogger<PluginFeedClient> logger,
                            IPluginDescriptorReader? descriptorReader = null,
                            IPluginDescriptorValidator? descriptorValidator = null)
        : this(CreateDefaultHandler(), preferencesStore, logger, descriptorReader, descriptorValidator)
    {
    }

    /// <summary>
    ///   Creates the feed client over a supplied handler, used by tests to inject a stub HTTP
    ///   transport instead of hitting the network.
    /// </summary>
    /// <param name="handler"> HTTP handler for the feed client's requests. </param>
    /// <param name="preferencesStore"> Preferences store for timeout and feed URL. </param>
    /// <param name="logger"> Logger. </param>
    /// <param name="descriptorReader"> Reader validating v2 inline descriptors at parse time. </param>
    /// <param name="descriptorValidator"> Validator checking v2 inline descriptors at parse time. </param>
    /// <param name="cacheFilePath">
    ///   Overrides the cached index location; defaults to <c> AppPaths.FeedCacheFile </c>.
    /// </param>
    /// <param name="syncMetadataFilePath">
    ///   Overrides the sync metadata location; defaults to <c> AppPaths.FeedSyncMetadataFile </c>.
    /// </param>
    /// <param name="timeProvider">
    ///   Time source for the signature transition window; system time when null. Tests set it to keep
    ///   the missing-signature behavior deterministic on both sides of the cutoff.
    /// </param>
    /// <param name="feedPublicKeyPem">
    ///   Verification key override, used only by tests; null uses the embedded
    ///   <see cref="PluginFeedSigner.PublicKeyPem" /> trust anchor. Production composition never sets
    ///   it, so the trust anchor stays a compile-time constant.
    /// </param>
    internal PluginFeedClient(HttpMessageHandler handler,
                              IPreferencesStore preferencesStore,
                              ILogger<PluginFeedClient> logger,
                              IPluginDescriptorReader? descriptorReader = null,
                              IPluginDescriptorValidator? descriptorValidator = null,
                              string? cacheFilePath = null,
                              string? syncMetadataFilePath = null,
                              TimeProvider? timeProvider = null,
                              string? feedPublicKeyPem = null)
    {
        this.preferencesStore = preferencesStore ?? throw new ArgumentNullException(nameof(preferencesStore));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.descriptorReader = descriptorReader ?? new PluginDescriptorReader();
        this.descriptorValidator = descriptorValidator ?? new PluginDescriptorValidator();
        this.cacheFilePath = string.IsNullOrWhiteSpace(cacheFilePath) ? AppPaths.FeedCacheFile : cacheFilePath;
        this.syncMetadataFilePath = string.IsNullOrWhiteSpace(syncMetadataFilePath)
            ? AppPaths.FeedSyncMetadataFile
            : syncMetadataFilePath;
        this.time = timeProvider ?? TimeProvider.System;
        this.feedPublicKeyPem = string.IsNullOrWhiteSpace(feedPublicKeyPem)
            ? PluginFeedSigner.PublicKeyPem
            : feedPublicKeyPem;

        this.httpClient = new HttpClient(handler ?? throw new ArgumentNullException(nameof(handler)));
        this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
    }

    private static SocketsHttpHandler CreateDefaultHandler() =>
        new()
        {
            // Manual redirect handling so HTTPS can be enforced on every hop.
            AllowAutoRedirect = false
        };

    /// <inheritdoc />
    public async Task<PluginFeedResult> GetFeedAsync(CancellationToken cancellationToken = default)
    {
        var feedUrl = FeedUrlProvider.GetFeedUrl(this.preferencesStore);
        if (feedUrl is null)
        {
            return new PluginFeedResult(false, null, ["Feed is disabled in configuration"]);
        }

        if (!IsHttpsUrl(feedUrl))
        {
            this.logger.LogError("Feed URL must use HTTPS: {FeedUrl}", feedUrl);
            return new PluginFeedResult(false, null, ["Feed URL must use HTTPS"]);
        }

        var conditionalHeaders = BuildConditionalRequestHeaders(this.LoadSyncMetadata());

        var outcome = await this.ExecuteWithStructuredErrorsAsync(
            token => this.FetchFeedAsync(feedUrl, conditionalHeaders, token),
            errors => new FeedFetchOutcome(new PluginFeedResult(false, null, [errors]), AllowCacheFallback: true),
            FeedErrorProfile,
            onFailure: null,
            cancellationToken).ConfigureAwait(false);

        if (outcome.Result.Success || !outcome.AllowCacheFallback)
        {
            return outcome.Result;
        }

        return this.TryGetCachedFeedResult(outcome.Result);
    }

    /// <summary>
    ///   Performs one conditional feed fetch: follows redirects (conditional headers on every hop),
    ///   turns a 304 into success from the cached index without parsing or writing, and on a fresh
    ///   200 parses the index and refreshes the cache and sync metadata. The returned outcome tells
    ///   the caller whether a failure may degrade to the last-good cache.
    /// </summary>
    /// <param name="feedUrl"> The configured HTTPS feed URL. </param>
    /// <param name="conditionalHeaders"> Conditional request headers from the sync metadata, or null. </param>
    /// <param name="cancellationToken"> The linked timeout token from the structured-error scope. </param>
    /// <returns> The fetch outcome and whether a cache fallback is permitted on failure. </returns>
    private async Task<FeedFetchOutcome> FetchFeedAsync(
        string feedUrl,
        IReadOnlyDictionary<string, string>? conditionalHeaders,
        CancellationToken cancellationToken)
    {
        var (response, redirectError) = await this.SendFollowingRedirectsAsync(
            feedUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken,
            conditionalHeaders).ConfigureAwait(false);
        if (response is null)
        {
            // A rejected redirect is a transport-policy failure, not a transient network failure:
            // it is reported without a cache fallback so the rejection stays visible.
            return new FeedFetchOutcome(
                new PluginFeedResult(false, null, [redirectError ?? "Redirect rejected"]),
                AllowCacheFallback: false);
        }

        using var finalResponse = response;

        if (finalResponse.StatusCode == HttpStatusCode.NotModified)
        {
            // 304 Not Modified: the cached index is confirmed current. No parse, no cache write,
            // no metadata write, and no fallback (this is a fresh confirmation, not stale data).
            var cachedIndex = this.LoadCachedIndex();
            if (cachedIndex is null)
            {
                this.logger.LogWarning("Feed returned 304 Not Modified but no usable cached index exists");
                return new FeedFetchOutcome(
                    new PluginFeedResult(false, null, ["Could not load plugin feed"]),
                    AllowCacheFallback: false);
            }

            return new FeedFetchOutcome(
                new PluginFeedResult(true, cachedIndex.Value.Index, []),
                AllowCacheFallback: false);
        }

        if (!finalResponse.IsSuccessStatusCode)
        {
            this.logger.LogWarning("Feed request failed with status {StatusCode}", finalResponse.StatusCode);
            return new FeedFetchOutcome(
                new PluginFeedResult(false, null, ["Could not load plugin feed"]),
                AllowCacheFallback: true);
        }

        // Fast-path rejection when the server declares the size up front. This is an
        // optimization only; the bounded read below enforces the limit during streaming.
        if (finalResponse.Content.Headers.ContentLength is > MaxIndexSizeBytes)
        {
            this.logger.LogError("Feed index too large: {Size} bytes", finalResponse.Content.Headers.ContentLength);

            // The size bound is a security guarantee, so it never degrades to stale data.
            return new FeedFetchOutcome(
                new PluginFeedResult(false, null, ["Feed index too large"]),
                AllowCacheFallback: false);
        }

        var bodyBytes = await ReadBoundedBytesAsync(finalResponse.Content, MaxIndexSizeBytes, cancellationToken).ConfigureAwait(false);
        if (bodyBytes is null)
        {
            this.logger.LogError("Feed index exceeds the {Limit} byte limit", MaxIndexSizeBytes);
            return new FeedFetchOutcome(
                new PluginFeedResult(false, null, ["Feed index too large"]),
                AllowCacheFallback: false);
        }

        // Verification-before-parse (design D4): for the official feed the signature must cover the
        // exact body bytes before a single entry is parsed. A failed signature check never degrades
        // to the cache, so tampering stays visible.
        var signature = await this.VerifySignatureAsync(feedUrl, bodyBytes, cancellationToken).ConfigureAwait(false);
        if (!signature.Success)
        {
            return new FeedFetchOutcome(
                new PluginFeedResult(false, null, signature.Errors) { SignatureValid = false },
                AllowCacheFallback: false);
        }

        var body = Encoding.UTF8.GetString(bodyBytes);
        var parsed = this.ParseFeedIndex(body);
        if (!parsed.Success)
        {
            return new FeedFetchOutcome(parsed, AllowCacheFallback: true);
        }

        var success = signature.Warning is null
            ? parsed
            : parsed with { SignatureWarning = signature.Warning };

        // Only verified index bytes enter the cache (design guarantee 5): an index accepted without a
        // signature during the transition window is served once but never cached, so a later 304 or a
        // cache fallback can never present unverified data. A failed signature check returned above.
        if (signature.Warning is not null)
        {
            this.logger.LogWarning("The unverified plugin feed index is not cached during the transition window");
            return new FeedFetchOutcome(success, AllowCacheFallback: false);
        }

        this.SaveCachedIndex(body);

        // A fresh 200 writes the complete metadata: the validators, the cache timestamp, AND the
        // comparison hash of the parsed index. Writing the hash here keeps a cache created without
        // the sync service (for example an open Browse dialog after a failed background refresh)
        // self-consistent, so the service's next 304 compares equal and publishes no spurious
        // change event (verifier finding a). A 304 writes nothing at all.
        this.SaveSyncMetadata(new PluginFeedSyncMetadata
        {
            ETag = ReadResponseHeaderValue(finalResponse, "ETag"),
            LastModified = ReadResponseHeaderValue(finalResponse, "Last-Modified"),
            Timestamp = DateTimeOffset.UtcNow,
            IndexHash = parsed.Index is { } index
                ? PluginFeedSyncMetadata.ComputeIndexHash(index.Plugins)
                : null
        });

        return new FeedFetchOutcome(success, AllowCacheFallback: false);
    }

    /// <summary>
    ///   Runs the signature step for a freshly fetched index body (design D4/D6): only the default
    ///   official feed is verified, and a custom URL — the user's own trust decision — skips the step
    ///   entirely (design D8).
    /// </summary>
    /// <param name="feedUrl"> The resolved feed URL. </param>
    /// <param name="bodyBytes"> The raw index body bytes that must be covered by the signature. </param>
    /// <param name="cancellationToken"> The linked timeout token of the enclosing fetch. </param>
    /// <returns> Whether the fetch may continue, with the warning or the hard errors it carries. </returns>
    private async Task<SignatureVerification> VerifySignatureAsync(
        string feedUrl,
        byte[] bodyBytes,
        CancellationToken cancellationToken)
    {
        if (!FeedUrlProvider.IsDefaultFeedUrl(feedUrl))
        {
            return SignatureVerification.Skipped;
        }

        // The signature lives next to the index: standard URI resolution drops the file name.
        var signatureUrl = new Uri(new Uri(feedUrl, UriKind.Absolute), "index.sig").AbsoluteUri;

        // The signature GET runs under its own structured-error scope so any transport failure
        // (timeout, network error, redirect rejection, unexpected exception) degrades to the
        // transition-window rule instead of bypassing it. Caller cancellation still rethrows.
        return await this.ExecuteWithStructuredErrorsAsync(
            token => this.FetchAndVerifySignatureAsync(signatureUrl, bodyBytes, token),
            reason => this.AcceptMissingSignature(reason),
            SignatureErrorProfile,
            onFailure: null,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///   Fetches <c> index.sig </c> with the same transport as the index (HTTPS-only, manual redirects
    ///   with a 5-hop cap), bounds it to 4 KB, and verifies it against the body bytes. Only a 200
    ///   response carries a signature; a 404 and every other status route through the
    ///   transition-window rule (design OQ-3).
    /// </summary>
    /// <param name="signatureUrl"> The absolute signature URL next to the index. </param>
    /// <param name="bodyBytes"> The raw index body bytes. </param>
    /// <param name="cancellationToken"> The signature scope's linked timeout token. </param>
    /// <returns> The verification outcome. </returns>
    private async Task<SignatureVerification> FetchAndVerifySignatureAsync(
        string signatureUrl,
        byte[] bodyBytes,
        CancellationToken cancellationToken)
    {
        var (response, redirectError) = await this.SendFollowingRedirectsAsync(
            signatureUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        if (response is null)
        {
            // A rejected redirect is not a signature; the transition-window rule decides what that
            // means for the feed, exactly as it does for a missing file.
            return this.AcceptMissingSignature(redirectError ?? "Redirect rejected");
        }

        using var finalResponse = response;

        if (!finalResponse.IsSuccessStatusCode)
        {
            return this.AcceptMissingSignature($"HTTP {(int)finalResponse.StatusCode}");
        }

        // Fast-path rejection when the server declares the size up front; the bounded read below
        // enforces the limit while streaming. An oversized envelope is present-but-unacceptable, so
        // it is a hard error rather than a missing signature.
        if (finalResponse.Content.Headers.ContentLength is > MaxSignatureSizeBytes)
        {
            this.logger.LogError("Feed signature file too large: {Size} bytes", finalResponse.Content.Headers.ContentLength);
            return SignatureVerification.Failed(SignatureTooLargeError);
        }

        var envelopeJson = await ReadBoundedAsync(finalResponse.Content, MaxSignatureSizeBytes, cancellationToken).ConfigureAwait(false);
        if (envelopeJson is null)
        {
            this.logger.LogError("Feed signature file exceeds the {Limit} byte limit", MaxSignatureSizeBytes);
            return SignatureVerification.Failed(SignatureTooLargeError);
        }

        return this.VerifySignatureEnvelope(envelopeJson, bodyBytes);
    }

    /// <summary>
    ///   Verifies a parsed envelope against the body bytes: the declared algorithm must be the
    ///   supported one, the base64 value must decode, and the ECDSA signature must cover the exact
    ///   body bytes under the trust anchor.
    /// </summary>
    /// <param name="envelopeJson"> The <c> index.sig </c> document. </param>
    /// <param name="bodyBytes"> The raw index body bytes. </param>
    /// <returns> The verification outcome. </returns>
    private SignatureVerification VerifySignatureEnvelope(string envelopeJson, byte[] bodyBytes)
    {
        var envelope = PluginFeedSignature.Parse(envelopeJson);
        if (envelope is null)
        {
            this.logger.LogError("Feed signature envelope is malformed or carries no algorithm or signature value");
            return SignatureVerification.Failed(SignatureMalformedError, SignatureInvalidError);
        }

        if (!string.Equals(envelope.Algorithm, PluginFeedSigner.SupportedAlgorithm, StringComparison.Ordinal))
        {
            this.logger.LogError("Unsupported feed signature algorithm {Algorithm}", envelope.Algorithm);
            return SignatureVerification.Failed($"Unsupported signature algorithm: {envelope.Algorithm}");
        }

        if (!PluginFeedSigner.TryDecodeSignature(envelope.Signature, out var signatureDer))
        {
            this.logger.LogError("Feed signature value is not valid base64");
            return SignatureVerification.Failed(SignatureMalformedError, SignatureInvalidError);
        }

        if (!PluginFeedSigner.Verify(bodyBytes, signatureDer, this.feedPublicKeyPem))
        {
            this.logger.LogError("Feed signature verification failed; the index may have been tampered with");
            return SignatureVerification.Failed(SignatureInvalidError);
        }

        this.logger.LogInformation("Feed signature verified (signed at {SignedAt})", envelope.SignedAt);
        return SignatureVerification.Verified;
    }

    /// <summary>
    ///   Applies the transition-window rule to a signature that could not be obtained: before the
    ///   cutoff (with the flag unset) the index is accepted with a logged warning; on or after it a
    ///   hard error is returned.
    /// </summary>
    /// <param name="reason"> Why no signature was available, for the log. </param>
    /// <returns> The accepted-with-warning or the hard-error outcome. </returns>
    private SignatureVerification AcceptMissingSignature(string reason)
    {
        if (PluginFeedSigner.IsSignatureRequired(this.time.GetUtcNow()))
        {
            this.logger.LogError("Plugin feed signature is required but missing ({Reason}); rejecting the index", reason);
            return SignatureVerification.Failed(SignatureRequiredError);
        }

        this.logger.LogWarning("{Warning} ({Reason})", SignatureMissingWarning, reason);
        return SignatureVerification.AcceptedWithoutSignature;
    }

    /// <summary>
    ///   Builds the conditional request headers: <c> If-None-Match </c> from a stored ETag, else
    ///   <c> If-Modified-Since </c> from a stored Last-Modified, else none (an unconditional fetch).
    /// </summary>
    /// <param name="metadata"> The stored sync metadata, or null. </param>
    /// <returns> The headers to send, or null when no validator is stored. </returns>
    private static IReadOnlyDictionary<string, string>? BuildConditionalRequestHeaders(PluginFeedSyncMetadata? metadata)
    {
        if (metadata is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(metadata.ETag))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal) { ["If-None-Match"] = metadata.ETag };
        }

        if (!string.IsNullOrWhiteSpace(metadata.LastModified))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal) { ["If-Modified-Since"] = metadata.LastModified };
        }

        return null;
    }

    /// <summary>
    ///   Reads the sync metadata tolerantly and drops it when the cached index it validates is
    ///   missing: conditional requests against content the client cannot serve are meaningless
    ///   (design D2).
    /// </summary>
    /// <returns> The sync metadata, or null when absent, corrupt, or orphaned. </returns>
    private PluginFeedSyncMetadata? LoadSyncMetadata()
    {
        var metadata = PluginFeedSyncMetadata.Load(this.syncMetadataFilePath, this.logger);
        if (metadata is null)
        {
            return null;
        }

        if (!File.Exists(this.cacheFilePath))
        {
            this.logger.LogWarning(
                "Plugin feed sync metadata exists without a cached index; deleting the metadata at {Path}",
                this.syncMetadataFilePath);
            PluginFeedSyncMetadata.TryDelete(this.syncMetadataFilePath, this.logger);
            return null;
        }

        return metadata;
    }

    /// <summary>
    ///   Writes the sync metadata atomically. A write failure only costs the next conditional fetch,
    ///   so it is logged as a warning rather than failing the successful feed load.
    /// </summary>
    /// <param name="metadata"> The metadata to persist. </param>
    private void SaveSyncMetadata(PluginFeedSyncMetadata metadata)
    {
        try
        {
            metadata.Save(this.syncMetadataFilePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            this.logger.LogWarning(ex, "Could not write the plugin feed sync metadata to {Path}", this.syncMetadataFilePath);
        }
    }

    /// <summary>
    ///   Loads the cached index when it is usable: an index larger than the 2 MB bound is deleted
    ///   and never served, a corrupt file is deleted with a warning, and a missing file is absent.
    ///   The cache timestamp prefers the sync metadata, falling back to the file's write time.
    /// </summary>
    /// <returns> The parsed cached index and when it was stored, or null when no usable cache exists. </returns>
    private (PluginFeedIndex Index, DateTimeOffset CachedAt)? LoadCachedIndex()
    {
        if (!File.Exists(this.cacheFilePath))
        {
            return null;
        }

        try
        {
            var file = new FileInfo(this.cacheFilePath);
            if (file.Length > MaxIndexSizeBytes)
            {
                this.logger.LogWarning(
                    "Cached feed index at {Path} exceeds the {Limit} byte limit; deleting it",
                    this.cacheFilePath,
                    MaxIndexSizeBytes);
                this.DeleteCachedIndexAndMetadata();
                return null;
            }

            var parsed = this.ParseFeedIndex(File.ReadAllText(this.cacheFilePath));
            if (!parsed.Success || parsed.Index is null)
            {
                this.logger.LogWarning("Cached feed index at {Path} is corrupt; deleting it", this.cacheFilePath);
                this.DeleteCachedIndexAndMetadata();
                return null;
            }

            var cachedAt = this.LoadSyncMetadata()?.Timestamp
                           ?? new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero);
            return (parsed.Index, cachedAt);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            this.logger.LogWarning(ex, "Could not read the cached feed index at {Path}", this.cacheFilePath);
            return null;
        }
    }

    /// <summary>
    ///   Deletes the unusable cached index together with its sync metadata: the two files describe
    ///   one state, so metadata left behind would point at content the client cannot serve.
    /// </summary>
    private void DeleteCachedIndexAndMetadata()
    {
        PluginFeedSyncMetadata.TryDelete(this.cacheFilePath, this.logger);
        PluginFeedSyncMetadata.TryDelete(this.syncMetadataFilePath, this.logger);
    }

    /// <summary>
    ///   Writes the raw index bytes to the last-good cache atomically. A write failure only costs
    ///   the fallback path, so it is logged as a warning rather than failing the fetch.
    /// </summary>
    /// <param name="body"> The raw index JSON of a successful fetch. </param>
    private void SaveCachedIndex(string body)
    {
        try
        {
            AtomicFileWriter.Write(this.cacheFilePath, body);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            this.logger.LogWarning(ex, "Could not write the cached plugin feed index to {Path}", this.cacheFilePath);
        }
    }

    /// <summary>
    ///   Serves the last-good cache in place of a failed live fetch: success with
    ///   <c> FromCache </c> and the cache timestamp when a usable cache exists, otherwise the
    ///   original live failure (which keeps every existing user-facing error string).
    /// </summary>
    /// <param name="liveFailure"> The structured failure of the live fetch. </param>
    /// <returns> The cached success, or the live failure when no usable cache exists. </returns>
    private PluginFeedResult TryGetCachedFeedResult(PluginFeedResult liveFailure)
    {
        var cachedIndex = this.LoadCachedIndex();
        if (cachedIndex is null)
        {
            return liveFailure;
        }

        this.logger.LogWarning("Feed request failed; serving the cached plugin index instead");
        return new PluginFeedResult(true, cachedIndex.Value.Index, [])
        {
            FromCache = true,
            CachedAt = cachedIndex.Value.CachedAt
        };
    }

    /// <summary>
    ///   Reads a response header value, checking the response headers first and the content headers
    ///   second (servers send <c> Last-Modified </c> as a content header).
    /// </summary>
    /// <param name="response"> The terminal response. </param>
    /// <param name="headerName"> The header name. </param>
    /// <returns> The first header value, or null when the header is absent. </returns>
    private static string? ReadResponseHeaderValue(HttpResponseMessage response, string headerName)
    {
        if (response.Headers.TryGetValues(headerName, out var values))
        {
            return values.FirstOrDefault();
        }

        return response.Content.Headers.TryGetValues(headerName, out var contentValues)
            ? contentValues.FirstOrDefault()
            : null;
    }

    /// <inheritdoc />
    public async Task<PluginDownloadResult> DownloadPackageAsync(string packageUrl, string expectedSha256, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(packageUrl);
        ArgumentNullException.ThrowIfNull(expectedSha256);

        if (!IsHttpsUrl(packageUrl))
        {
            this.logger.LogError("Package URL must use HTTPS: {PackageUrl}", packageUrl);
            return new PluginDownloadResult(false, null, ["Package URL must use HTTPS"]);
        }

        var tempFilePath = Path.Combine(Path.GetTempPath(), $"clihub-plugin-{Guid.NewGuid():N}.zip");

        return await this.ExecuteWithStructuredErrorsAsync(
            async token =>
            {
                var (response, redirectError) = await this.SendFollowingRedirectsAsync(packageUrl, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                if (response is null)
                {
                    return new PluginDownloadResult(false, null, [redirectError ?? "Redirect rejected"]);
                }

                using var finalResponse = response;
                if (!finalResponse.IsSuccessStatusCode)
                {
                    this.logger.LogWarning("Package download failed with status {StatusCode}", finalResponse.StatusCode);
                    return new PluginDownloadResult(false, null, ["Package download failed"]);
                }

                // Fast-path rejection when the server declares the size up front. This is an
                // optimization only; the bounded copy below enforces the limit during streaming.
                if (finalResponse.Content.Headers.ContentLength is > MaxPackageSizeBytes)
                {
                    this.logger.LogError("Package too large: {Size} bytes", finalResponse.Content.Headers.ContentLength);
                    return new PluginDownloadResult(false, null, ["Package too large"]);
                }

                // Download to the temp file, aborting mid-stream once the package exceeds the limit so
                // a missing/lying Content-Length or chunked response cannot stream unbounded data.
                var tooLarge = await CopyBoundedAsync(finalResponse.Content, tempFilePath, MaxPackageSizeBytes, token).ConfigureAwait(false);
                if (tooLarge)
                {
                    this.logger.LogError("Downloaded package exceeds the {Limit} byte limit", MaxPackageSizeBytes);
                    this.DeleteTempFile(tempFilePath);
                    return new PluginDownloadResult(false, null, ["Package too large"]);
                }

                // Verify checksum
                var actualSha256 = await ComputeSha256Async(tempFilePath).ConfigureAwait(false);
                if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
                {
                    this.logger.LogError("Package checksum mismatch. Expected {Expected}, got {Actual}", expectedSha256, actualSha256);
                    File.Delete(tempFilePath);
                    return new PluginDownloadResult(false, null, ["Package checksum mismatch. The downloaded file may be corrupted or tampered with."]);
                }

                return new PluginDownloadResult(true, tempFilePath, []);
            },
            errors => new PluginDownloadResult(false, null, [errors]),
            PackageErrorProfile,
            () => this.DeleteTempFile(tempFilePath),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PluginLogoDownloadResult> DownloadLogoAsync(string logoUrl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(logoUrl);

        if (!IsHttpsUrl(logoUrl))
        {
            this.logger.LogError("Logo URL must use HTTPS: {LogoUrl}", logoUrl);
            return new PluginLogoDownloadResult(false, null, ["Logo URL must use HTTPS"]);
        }

        return await this.ExecuteWithStructuredErrorsAsync(
            async token =>
            {
                var (response, redirectError) = await this.SendFollowingRedirectsAsync(logoUrl, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                if (response is null)
                {
                    return new PluginLogoDownloadResult(false, null, [redirectError ?? "Redirect rejected"]);
                }

                using var finalResponse = response;
                if (!finalResponse.IsSuccessStatusCode)
                {
                    this.logger.LogWarning("Logo download failed with status {StatusCode}", finalResponse.StatusCode);
                    return new PluginLogoDownloadResult(false, null, ["Logo download failed"]);
                }

                // Fast-path rejection when the server declares the size up front. This is an
                // optimization only; the bounded read below enforces the limit during streaming.
                // A standalone logo is held to the same 10 MB bound a packaged logo may reach.
                if (finalResponse.Content.Headers.ContentLength is > MaxPackageSizeBytes)
                {
                    this.logger.LogError("Logo too large: {Size} bytes", finalResponse.Content.Headers.ContentLength);
                    return new PluginLogoDownloadResult(false, null, ["Logo download failed"]);
                }

                var logoBytes = await ReadBoundedBytesAsync(finalResponse.Content, MaxPackageSizeBytes, token).ConfigureAwait(false);
                if (logoBytes is null)
                {
                    this.logger.LogError("Downloaded logo exceeds the {Limit} byte limit", MaxPackageSizeBytes);
                    return new PluginLogoDownloadResult(false, null, ["Logo download failed"]);
                }

                return new PluginLogoDownloadResult(true, logoBytes, []);
            },
            errors => new PluginLogoDownloadResult(false, null, [errors]),
            LogoErrorProfile,
            onFailure: null,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///   Runs a feed transport operation under the probe timeout, converting cancellations, network
    ///   failures, and unexpected exceptions into structured errors. Caller cancellation rethrows;
    ///   a timeout logs without an exception object; the other two catches log with the exception.
    ///   A malformed redirect Location (UriFormatException) or any other unexpected failure degrades
    ///   to a structured error rather than escaping to the caller.
    /// </summary>
    /// <typeparam name="T"> The result type produced by <paramref name="operation" />. </typeparam>
    /// <param name="operation"> Transport operation to run under the linked timeout token. </param>
    /// <param name="failure"> Builds the failure result from a structured error message. </param>
    /// <param name="profile"> Per-operation error and log-message strings and the unexpected-catch log level. </param>
    /// <param name="onFailure"> Optional cleanup run before every structured-error return. </param>
    /// <param name="cancellationToken"> Caller cancellation token. </param>
    /// <returns> The operation result, or a structured failure result. </returns>
    private async Task<T> ExecuteWithStructuredErrorsAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<string, T> failure,
        StructuredErrorProfile profile,
        Action? onFailure,
        CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(this.ProbeTimeout);

            return await operation(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            this.logger.LogWarning(profile.TimeoutLogMessage);
            onFailure?.Invoke();
            return failure(profile.TimeoutError);
        }
        catch (HttpRequestException ex)
        {
            this.logger.LogWarning(ex, profile.RequestLogMessage);
            onFailure?.Invoke();
            return failure(profile.RequestError);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            this.logger.Log(profile.UnexpectedLogLevel, ex, profile.UnexpectedLogMessage);
            onFailure?.Invoke();
            return failure(profile.RequestError);
        }
    }

    /// <summary>
    ///   Per-operation strings for <see cref="ExecuteWithStructuredErrorsAsync{T}" />: the user-facing
    ///   timeout and request errors plus the log messages, with the log level of the unexpected
    ///   catch (Warning for the feed, Error for package downloads).
    /// </summary>
    /// <param name="TimeoutError"> Structured error returned on a timeout. </param>
    /// <param name="RequestError"> Structured error returned on a request or unexpected failure. </param>
    /// <param name="TimeoutLogMessage"> Log message written on a timeout (no exception object). </param>
    /// <param name="RequestLogMessage"> Log message written on an HttpRequestException. </param>
    /// <param name="UnexpectedLogMessage"> Log message written on an unexpected exception. </param>
    /// <param name="UnexpectedLogLevel"> Log level of the unexpected catch. </param>
    private sealed record StructuredErrorProfile(
        string TimeoutError,
        string RequestError,
        string TimeoutLogMessage,
        string RequestLogMessage,
        string UnexpectedLogMessage,
        LogLevel UnexpectedLogLevel);

    /// <summary>
    ///   The outcome of one feed fetch: the result plus whether a failure may degrade to the
    ///   last-good cache. Transport and parse failures allow the fallback; redirect-policy and
    ///   size-bound rejections do not, so the security guarantees stay visible.
    /// </summary>
    /// <param name="Result"> The feed result produced by the fetch. </param>
    /// <param name="AllowCacheFallback"> Whether a failure may be replaced by the cached index. </param>
    private sealed record FeedFetchOutcome(PluginFeedResult Result, bool AllowCacheFallback);

    /// <summary>
    ///   The outcome of the signature step: whether the fetch may continue, the transition-window
    ///   warning to surface on an accepted unverified index, and the hard errors to report when the
    ///   fetch must not continue.
    /// </summary>
    /// <param name="Success"> Whether the index may proceed to parsing and caching. </param>
    /// <param name="Warning"> The transition-window notice, or null. </param>
    /// <param name="Errors"> The user-facing errors when <paramref name="Success" /> is false. </param>
    private sealed record SignatureVerification(
        bool Success,
        string? Warning,
        IReadOnlyList<string> Errors)
    {
        /// <summary> A custom feed URL: no signature is fetched and none is required. </summary>
        public static SignatureVerification Skipped { get; } = new(true, null, []);

        /// <summary> The signature verified against the body bytes. </summary>
        public static SignatureVerification Verified { get; } = new(true, null, []);

        /// <summary> No signature exists, but the transition window accepts the index with a warning. </summary>
        public static SignatureVerification AcceptedWithoutSignature { get; } =
            new(true, SignatureMissingWarning, []);

        /// <summary> A present, malformed, unsupported, oversized, or non-matching signature. </summary>
        /// <param name="errors"> The user-facing errors. </param>
        /// <returns> The failed outcome. </returns>
        public static SignatureVerification Failed(params string[] errors) => new(false, null, errors);
    }

    /// <summary>
    ///   Sends the GET request and manually follows up to the maximum hop count of HTTP redirects,
    ///   rejecting any redirect to non-HTTPS and chains longer than the maximum. Any supplied
    ///   conditional headers are attached to every hop's request, so a redirected fetch keeps its
    ///   validator. A 3xx response without a Location header is treated as terminal. Ownership of
    ///   the returned terminal response transfers to the caller, which must dispose it;
    ///   intermediate responses are disposed here.
    /// </summary>
    /// <param name="url"> Initial URL. </param>
    /// <param name="completionOption"> HTTP completion option for reading the response. </param>
    /// <param name="cancellationToken"> Cancellation token. </param>
    /// <param name="requestHeaders"> Optional request headers applied to every hop (conditional fetch). </param>
    /// <returns>
    ///   The terminal response and a null error — owned and disposed by the caller — or a null
    ///   response with the rejection reason.
    /// </returns>
    private async Task<(HttpResponseMessage? Response, string? Error)> SendFollowingRedirectsAsync(
        string url,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? requestHeaders = null)
    {
        var currentUrl = url;
        for (var hop = 0; hop <= MaxRedirectHops; hop++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, currentUrl);
            if (requestHeaders is not null)
            {
                foreach (var (name, value) in requestHeaders)
                {
                    request.Headers.TryAddWithoutValidation(name, value);
                }
            }

            var response = await this.httpClient.SendAsync(request, completionOption, cancellationToken).ConfigureAwait(false);

            if ((int)response.StatusCode < 300 || (int)response.StatusCode >= 400)
            {
                // Not a redirect; the caller owns and disposes the terminal response.
                return (response, null);
            }

            if (response.Headers.Location is null)
            {
                this.logger.LogWarning("Redirect response without Location header");

                // Terminal per decision D2: the caller's status check turns this 3xx into the
                // regular request error, reproducing the observable outcome of the previous
                // HEAD-probe flow. The caller owns and disposes the response.
                return (response, null);
            }

            var nextUrl = response.Headers.Location.IsAbsoluteUri
                ? response.Headers.Location.AbsoluteUri
                : new Uri(new Uri(currentUrl), response.Headers.Location).AbsoluteUri;

            if (!IsHttpsUrl(nextUrl))
            {
                this.logger.LogError("Redirect to non-HTTPS URL rejected: {NextUrl}", nextUrl);
                response.Dispose();
                return (null, "Redirect to non-HTTPS URL rejected");
            }

            response.Dispose();
            currentUrl = nextUrl;
        }

        this.logger.LogWarning("Maximum redirect hops ({MaxHops}) exceeded", MaxRedirectHops);
        return (null, $"Redirect limit exceeded (maximum {MaxRedirectHops} hops)");
    }

    /// <summary>
    ///   Reads the response body as a UTF-8 string, returning null once the stream exceeds
    ///   <paramref name="maxBytes" />. This enforces the size limit during streaming for responses
    ///   with a missing or lying <c> Content-Length </c> (or chunked transfer encoding).
    /// </summary>
    private static async Task<string?> ReadBoundedAsync(HttpContent content, int maxBytes, CancellationToken cancellationToken)
    {
        using var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        var total = 0;
        while (true)
        {
            var read = await stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            total += read;
            if (total > maxBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
    }

    /// <summary>
    ///   Reads the response body as a bounded byte array, returning null once the stream exceeds
    ///   <paramref name="maxBytes" />. This enforces the size limit during streaming for responses
    ///   with a missing or lying <c> Content-Length </c> (or chunked transfer encoding).
    /// </summary>
    private static async Task<byte[]?> ReadBoundedBytesAsync(HttpContent content, int maxBytes, CancellationToken cancellationToken)
    {
        using var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        var total = 0;
        while (true)
        {
            var read = await stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            total += read;
            if (total > maxBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    /// <summary>
    ///   Copies the response body to the destination file in chunks, returning true once the stream
    ///   exceeds <paramref name="maxBytes" />. On an oversized result the partial file is left for
    ///   the caller to delete.
    /// </summary>
    private static async Task<bool> CopyBoundedAsync(HttpContent content, string destinationPath, int maxBytes, CancellationToken cancellationToken)
    {
        using var source = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var destination = File.Create(destinationPath);
        var buffer = new byte[81920];
        var total = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return false;
            }

            total += read;
            if (total > maxBytes)
            {
                return true;
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }
    }

    private PluginFeedResult ParseFeedIndex(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            // Validate schemaVersion
            if (!root.TryGetProperty("schemaVersion", out var schemaVersionElem) ||
                schemaVersionElem.ValueKind != JsonValueKind.Number ||
                !schemaVersionElem.TryGetInt32(out var schemaVersion))
            {
                this.logger.LogError("Feed index missing or invalid schemaVersion");
                return new PluginFeedResult(false, null, ["Feed index missing or invalid schemaVersion"]);
            }

            if (schemaVersion > 2)
            {
                this.logger.LogError("Unsupported feed schema version {Version}", schemaVersion);
                return new PluginFeedResult(false, null, [$"Unsupported feed schema version {schemaVersion}"]);
            }

            // Parse lastUpdated (optional)
            var lastUpdated = root.TryGetProperty("lastUpdated", out var lastUpdatedElem) &&
                              lastUpdatedElem.ValueKind == JsonValueKind.String
                ? lastUpdatedElem.GetString()
                : null;

            // Parse plugins array
            if (!root.TryGetProperty("plugins", out var pluginsElem) || pluginsElem.ValueKind != JsonValueKind.Array)
            {
                this.logger.LogError("Feed index missing or invalid plugins array");
                return new PluginFeedResult(false, null, ["Feed index missing or invalid plugins array"]);
            }

            var plugins = new List<PluginFeedEntry>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var entryElem in pluginsElem.EnumerateArray())
            {
                // Entry shape follows the index schemaVersion; no per-entry sniffing. Values
                // below 2 parse under the v1 rules exactly as before.
                var entry = schemaVersion < 2
                    ? this.TryParseEntry(entryElem, seenIds)
                    : this.TryParseEntryV2(entryElem, seenIds);
                if (entry is not null)
                {
                    plugins.Add(entry);
                }
            }

            var index = new PluginFeedIndex(schemaVersion, lastUpdated, plugins);
            return new PluginFeedResult(true, index, []);
        }
        catch (JsonException ex)
        {
            this.logger.LogError(ex, "Failed to parse feed index JSON");
            return new PluginFeedResult(false, null, ["Invalid JSON in feed index"]);
        }
    }

    private PluginFeedEntry? TryParseEntry(JsonElement entryElem, HashSet<string> seenIds)
    {
        try
        {
            // Required fields
            if (!TryGetString(entryElem, "id", out var id) ||
                !TryGetString(entryElem, "name", out var name) ||
                !TryGetString(entryElem, "description", out var description) ||
                !TryGetString(entryElem, "version", out var version) ||
                !TryGetString(entryElem, "minAppVersion", out var minAppVersion) ||
                !TryGetString(entryElem, "publisher", out var publisher) ||
                !TryGetString(entryElem, "homepage", out var homepage) ||
                !TryGetString(entryElem, "packageUrl", out var packageUrl) ||
                !TryGetString(entryElem, "sha256", out var sha256))
            {
                this.logger.LogWarning("Feed entry missing required fields, skipping");
                return null;
            }

            if (!entryElem.TryGetProperty("schemaVersion", out var schemaVersionElem) ||
                schemaVersionElem.ValueKind != JsonValueKind.Number ||
                !schemaVersionElem.TryGetInt32(out var schemaVersion))
            {
                this.logger.LogWarning("Feed entry missing or invalid schemaVersion for id {Id}, skipping", id);
                return null;
            }

            // Check for duplicate IDs
            if (!seenIds.Add(id))
            {
                this.logger.LogWarning("Feed entry with duplicate id {Id} skipped", id);
                return null;
            }

            // Optional fields
            var releaseNotesUrl = TryGetString(entryElem, "releaseNotesUrl", out var rnu) ? rnu : null;
            var signature = TryGetString(entryElem, "signature", out var sig) ? sig : null;

            // The v1 minAppVersion floor is carried as a synthesized compatibility range so both
            // schemas gate through one evaluator (AppCompatibilityRange); equivalence is by
            // construction (design D4).
            return new PluginFeedEntry(id, name, description, version, schemaVersion, minAppVersion, publisher, homepage, packageUrl, sha256, releaseNotesUrl, signature, ">=" + minAppVersion, null, null, null);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to parse feed entry, skipping");
            return null;
        }
    }

    private PluginFeedEntry? TryParseEntryV2(JsonElement entryElem, HashSet<string> seenIds)
    {
        try
        {
            // Required fields
            if (!TryGetString(entryElem, "id", out var id) ||
                !TryGetString(entryElem, "name", out var name) ||
                !TryGetString(entryElem, "description", out var description) ||
                !TryGetString(entryElem, "version", out var version) ||
                !TryGetString(entryElem, "publisher", out var publisher) ||
                !TryGetString(entryElem, "homepage", out var homepage) ||
                !TryGetString(entryElem, "compatibility", out var compatibility))
            {
                this.logger.LogWarning("Feed entry missing required fields, skipping");
                return null;
            }

            if (!AppCompatibilityRange.TryParse(compatibility, out _))
            {
                this.logger.LogWarning("Feed entry has malformed compatibility for id {Id}, skipping", id);
                return null;
            }

            // The inline descriptor is required and must be a JSON object; its raw text is kept
            // verbatim as the descriptor-only install source (the index is the integrity anchor
            // per ADR-0015). An entry whose descriptor fails validation is not installable, so
            // it is skipped here rather than at install time.
            if (!TryGetObject(entryElem, "descriptor", out var descriptorElem))
            {
                this.logger.LogWarning("Feed entry missing or invalid descriptor for id {Id}, skipping", id);
                return null;
            }

            var descriptorJson = descriptorElem.GetRawText();
            var readResult = this.descriptorReader.ReadContent(descriptorJson);
            if (readResult.Error is not null || readResult.Plugin is null)
            {
                this.logger.LogWarning("Feed entry has invalid descriptor for id {Id}, skipping", id);
                return null;
            }

            var plugin = readResult.Plugin;
            if (!this.descriptorValidator.Validate(plugin).IsValid)
            {
                this.logger.LogWarning("Feed entry has invalid descriptor for id {Id}, skipping", id);
                return null;
            }

            if (!string.Equals(plugin.Id, id, StringComparison.Ordinal))
            {
                this.logger.LogWarning("Feed entry descriptor id {DescriptorId} does not match entry id {Id}, skipping", plugin.Id, id);
                return null;
            }

            // Channel: absent, null, or non-string is tolerated as stable; any other string
            // value skips the entry (design DEV-1).
            var channel = "stable";
            if (entryElem.TryGetProperty("channel", out var channelElem) &&
                channelElem.ValueKind == JsonValueKind.String &&
                channelElem.GetString() is { } channelValue)
            {
                if (!string.Equals(channelValue, "stable", StringComparison.Ordinal))
                {
                    this.logger.LogWarning("Feed entry has unsupported channel {Channel} for id {Id}, skipping", channelValue, id);
                    return null;
                }

                channel = channelValue;
            }

            // Package: absent or null makes the entry descriptor-only; otherwise both url and
            // sha256 are required. HTTPS for the package URL stays download-time-enforced,
            // exactly as for v1 entries.
            string? packageUrl = null;
            string? sha256 = null;
            if (entryElem.TryGetProperty("package", out var packageElem) &&
                packageElem.ValueKind == JsonValueKind.Object)
            {
                if (!TryGetString(packageElem, "url", out packageUrl) ||
                    !TryGetString(packageElem, "sha256", out sha256))
                {
                    this.logger.LogWarning("Feed entry has incomplete package for id {Id}, skipping", id);
                    return null;
                }
            }

            // Optional fields
            var logoUrl = TryGetString(entryElem, "logoUrl", out var lu) ? lu : null;
            var releaseNotesUrl = TryGetString(entryElem, "releaseNotesUrl", out var rnu) ? rnu : null;
            var signature = TryGetString(entryElem, "signature", out var sig) ? sig : null;

            // Check for duplicate IDs — the id is claimed only after all entry-level validation
            // passes, so an invalid entry does not block a valid entry with the same id.
            if (!seenIds.Add(id))
            {
                this.logger.LogWarning("Feed entry with duplicate id {Id} skipped", id);
                return null;
            }

            return new PluginFeedEntry(id, name, description, version, plugin.SchemaVersion ?? 1, null, publisher, homepage, packageUrl, sha256, releaseNotesUrl, signature, compatibility, channel, descriptorJson, logoUrl);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to parse feed entry, skipping");
            return null;
        }
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        if (element.TryGetProperty(propertyName, out var prop) &&
            prop.ValueKind == JsonValueKind.String &&
            prop.GetString() is { } str &&
            !string.IsNullOrWhiteSpace(str))
        {
            value = str;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryGetObject(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.TryGetProperty(propertyName, out var prop) &&
            prop.ValueKind == JsonValueKind.Object)
        {
            value = prop;
            return true;
        }

        value = default;
        return false;
    }

    private static bool IsHttpsUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> ComputeSha256Async(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream).ConfigureAwait(false);
        return Convert.ToHexString(hashBytes);
    }

    private void DeleteTempFile(string? tempFilePath)
    {
        if (tempFilePath is null)
        {
            return;
        }

        try
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to delete temporary file {FilePath}", tempFilePath);
        }
    }

    private TimeSpan ProbeTimeout =>
        AgentProbePolicy.ResolveTimeout(this.preferencesStore.Load().AgentProbeTimeoutSeconds);

    /// <inheritdoc />
    public void Dispose()
    {
        this.httpClient?.Dispose();
    }
}
