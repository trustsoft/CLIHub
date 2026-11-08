namespace CLIHub.Core.Plugins;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;

/// <summary>
///   Fetches the plugin feed index and downloads plugin packages over HTTPS with checksum verification.
/// </summary>
public sealed class PluginFeedClient : IPluginFeedClient, IDisposable
{
    private const string UserAgent = "CLIHub";
    private const int MaxIndexSizeBytes = 2 * 1024 * 1024; // 2 MB
    private const int MaxPackageSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const int MaxRedirectHops = 5;

    private readonly HttpClient httpClient;
    private readonly IPreferencesStore preferencesStore;
    private readonly ILogger<PluginFeedClient> logger;

    /// <summary>
    ///   Creates the feed client with a dedicated HTTP handler that does not auto-redirect.
    /// </summary>
    /// <param name="preferencesStore"> Preferences store for timeout and feed URL. </param>
    /// <param name="logger"> Logger. </param>
    public PluginFeedClient(IPreferencesStore preferencesStore,
                            ILogger<PluginFeedClient> logger)
        : this(CreateDefaultHandler(), preferencesStore, logger)
    {
    }

    /// <summary>
    ///   Creates the feed client over a supplied handler, used by tests to inject a stub HTTP
    ///   transport instead of hitting the network.
    /// </summary>
    /// <param name="handler"> HTTP handler for the feed client's requests. </param>
    /// <param name="preferencesStore"> Preferences store for timeout and feed URL. </param>
    /// <param name="logger"> Logger. </param>
    internal PluginFeedClient(HttpMessageHandler handler,
                              IPreferencesStore preferencesStore,
                              ILogger<PluginFeedClient> logger)
    {
        this.preferencesStore = preferencesStore ?? throw new ArgumentNullException(nameof(preferencesStore));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

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

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(this.ProbeTimeout);

        try
        {
            var (finalUrl, redirectError) = await this.FollowRedirectsAsync(feedUrl, timeoutCts.Token).ConfigureAwait(false);
            if (finalUrl is null)
            {
                return new PluginFeedResult(false, null, [redirectError ?? "Redirect rejected"]);
            }

            using var response = await this.httpClient.GetAsync(finalUrl, timeoutCts.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogWarning("Feed request failed with status {StatusCode}", response.StatusCode);
                return new PluginFeedResult(false, null, ["Could not load plugin feed"]);
            }

            // Fast-path rejection when the server declares the size up front. This is an
            // optimization only; the bounded read below enforces the limit during streaming.
            if (response.Content.Headers.ContentLength is > MaxIndexSizeBytes)
            {
                this.logger.LogError("Feed index too large: {Size} bytes", response.Content.Headers.ContentLength);
                return new PluginFeedResult(false, null, ["Feed index too large"]);
            }

            var body = await ReadBoundedAsync(response.Content, MaxIndexSizeBytes, timeoutCts.Token).ConfigureAwait(false);
            if (body is null)
            {
                this.logger.LogError("Feed index exceeds the {Limit} byte limit", MaxIndexSizeBytes);
                return new PluginFeedResult(false, null, ["Feed index too large"]);
            }

            return this.ParseFeedIndex(body);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            this.logger.LogWarning("Feed request timed out");
            return new PluginFeedResult(false, null, ["Feed request timed out"]);
        }
        catch (HttpRequestException ex)
        {
            this.logger.LogWarning(ex, "Feed request failed");
            return new PluginFeedResult(false, null, ["Could not load plugin feed. Check your network connection."]);
        }
        catch (Exception ex)
        {
            // A malformed redirect Location (UriFormatException) or any other unexpected failure
            // must degrade to a structured error rather than escape to the caller.
            this.logger.LogWarning(ex, "Feed request failed unexpectedly");
            return new PluginFeedResult(false, null, ["Could not load plugin feed. Check your network connection."]);
        }
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

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(this.ProbeTimeout);

            var (finalUrl, redirectError) = await this.FollowRedirectsAsync(packageUrl, timeoutCts.Token).ConfigureAwait(false);
            if (finalUrl is null)
            {
                return new PluginDownloadResult(false, null, [redirectError ?? "Redirect rejected"]);
            }

            using var response = await this.httpClient.GetAsync(finalUrl, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogWarning("Package download failed with status {StatusCode}", response.StatusCode);
                return new PluginDownloadResult(false, null, ["Package download failed"]);
            }

            // Fast-path rejection when the server declares the size up front. This is an
            // optimization only; the bounded copy below enforces the limit during streaming.
            if (response.Content.Headers.ContentLength is > MaxPackageSizeBytes)
            {
                this.logger.LogError("Package too large: {Size} bytes", response.Content.Headers.ContentLength);
                return new PluginDownloadResult(false, null, ["Package too large"]);
            }

            // Download to the temp file, aborting mid-stream once the package exceeds the limit so
            // a missing/lying Content-Length or chunked response cannot stream unbounded data.
            var tooLarge = await CopyBoundedAsync(response.Content, tempFilePath, MaxPackageSizeBytes, timeoutCts.Token).ConfigureAwait(false);
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
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            this.logger.LogWarning("Package download timed out");
            this.DeleteTempFile(tempFilePath);
            return new PluginDownloadResult(false, null, ["Package download timed out"]);
        }
        catch (HttpRequestException ex)
        {
            this.logger.LogWarning(ex, "Package download failed");
            this.DeleteTempFile(tempFilePath);
            return new PluginDownloadResult(false, null, ["Package download failed"]);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexpected error during package download");
            this.DeleteTempFile(tempFilePath);
            return new PluginDownloadResult(false, null, ["Package download failed"]);
        }
    }

    /// <summary>
    ///   Manually follows HTTP redirects up to the maximum hop count, rejecting any redirect to
    ///   non-HTTPS and rejecting chains longer than the maximum.
    /// </summary>
    /// <param name="url"> Initial URL. </param>
    /// <param name="cancellationToken"> Cancellation token. </param>
    /// <returns>
    ///   The final HTTPS URL and a null error, or a null URL with the rejection reason.
    /// </returns>
    private async Task<(string? FinalUrl, string? Error)> FollowRedirectsAsync(string url, CancellationToken cancellationToken)
    {
        var currentUrl = url;
        for (var hop = 0; hop < MaxRedirectHops; hop++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, currentUrl);
            using var response = await this.httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if ((int)response.StatusCode < 300 || (int)response.StatusCode >= 400)
            {
                // Not a redirect
                return (currentUrl, null);
            }

            if (response.Headers.Location is null)
            {
                this.logger.LogWarning("Redirect response without Location header");
                return (currentUrl, null);
            }

            var nextUrl = response.Headers.Location.IsAbsoluteUri
                ? response.Headers.Location.AbsoluteUri
                : new Uri(new Uri(currentUrl), response.Headers.Location).AbsoluteUri;

            if (!IsHttpsUrl(nextUrl))
            {
                this.logger.LogError("Redirect to non-HTTPS URL rejected: {NextUrl}", nextUrl);
                return (null, "Redirect to non-HTTPS URL rejected");
            }

            currentUrl = nextUrl;
        }

        this.logger.LogWarning("Maximum redirect hops ({MaxHops}) exceeded", MaxRedirectHops);
        return (null, $"Too many redirects (more than {MaxRedirectHops} hops)");
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

            if (schemaVersion > 1)
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
                var entry = this.TryParseEntry(entryElem, seenIds);
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

            return new PluginFeedEntry(id, name, description, version, schemaVersion, minAppVersion, publisher, homepage, packageUrl, sha256, releaseNotesUrl, signature);
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
