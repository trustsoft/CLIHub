namespace CLIHub.Core.Agents;

using System.Globalization;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;

/// <summary>
///   Reads the latest published version from the npm registry or from GitHub releases over a
///   shared <see cref="HttpClient" />, bounded by the agent probe timeout preference.
/// </summary>
public class AgentReleaseClient : IAgentReleaseClient
{
    private const string UserAgent = "CLIHub";
    private const string NpmLatestUrl = "https://registry.npmjs.org/{0}/latest";
    private const string GitHubLatestUrl = "https://api.github.com/repos/{0}/releases/latest";

    private readonly HttpClient httpClient;
    private readonly IPreferencesStore preferencesStore;
    private readonly ILogger<AgentReleaseClient> logger;

    /// <summary>
    ///   Creates the client.
    /// </summary>
    /// <param name="httpClient"> The shared HTTP client. </param>
    /// <param name="preferencesStore"> Preference store supplying the probe timeout. </param>
    /// <param name="logger"> Logger. </param>
    public AgentReleaseClient(HttpClient httpClient,
                              IPreferencesStore preferencesStore,
                              ILogger<AgentReleaseClient> logger)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.preferencesStore = preferencesStore ?? throw new ArgumentNullException(nameof(preferencesStore));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // GitHub rejects requests without a User-Agent. The DI factory sets it on the shared
        // client already; this guard keeps directly constructed instances (for example in tests)
        // well-behaved without sending the header twice.
        if (this.httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetLatestVersionAsync(PluginUpdateCheck updateCheck,
                                                     CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateCheck);

        if (!this.TryBuildRequest(updateCheck, out var request))
        {
            return null;
        }

        // Bound the HTTP call with the same probe timeout the version probe uses instead of
        // HttpClient.Timeout, through a linked token so caller cancellation still propagates.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(this.ProbeTimeout);

        try
        {
            using var response = await this.httpClient
                .SendAsync(request, timeoutCts.Token)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogWarning(
                    "Update source for {Source} responded with {StatusCode}", updateCheck.Source, response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(timeoutCts.Token).ConfigureAwait(false);
            return ParseLatestVersion(updateCheck.Source, body);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            this.logger.LogWarning("Update source request for {Source} timed out", updateCheck.Source);
            return null;
        }
        catch (HttpRequestException ex)
        {
            this.logger.LogWarning(ex, "Update source request for {Source} failed", updateCheck.Source);
            return null;
        }
    }

    private bool TryBuildRequest(PluginUpdateCheck updateCheck, out HttpRequestMessage request)
    {
        string url;
        if (string.Equals(updateCheck.Source, "npm", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(updateCheck.Package))
            {
                this.logger.LogWarning("Update check source npm has no package for {Source} block", updateCheck.Source);
                request = null!;
                return false;
            }

            url = string.Format(CultureInfo.InvariantCulture, NpmLatestUrl, Uri.EscapeDataString(updateCheck.Package));
        }
        else if (string.Equals(updateCheck.Source, "githubReleases", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(updateCheck.Repo))
            {
                this.logger.LogWarning("Update check source githubReleases has no repo");
                request = null!;
                return false;
            }

            url = string.Format(CultureInfo.InvariantCulture, GitHubLatestUrl, updateCheck.Repo);
        }
        else
        {
            this.logger.LogWarning(
                "Ignoring update check with unknown source kind {Source}", updateCheck.Source);
            request = null!;
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            this.logger.LogWarning("Update check produced an invalid source URL for {Source}", updateCheck.Source);
            request = null!;
            return false;
        }

        request = new HttpRequestMessage(HttpMethod.Get, uri);
        return true;
    }

    private static string? ParseLatestVersion(string source, string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var field = string.Equals(source, "npm", StringComparison.OrdinalIgnoreCase) ? "version" : "tag_name";
            if (!document.RootElement.TryGetProperty(field, out var value)
                || value.ValueKind != JsonValueKind.String
                || value.GetString() is not { } version
                || string.IsNullOrWhiteSpace(version))
            {
                return null;
            }

            if (field == "tag_name" && version.StartsWith('v'))
            {
                version = version[1..];
            }

            return version;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private TimeSpan ProbeTimeout
    {
        get
        {
            var seconds = this.preferencesStore.Load().AgentProbeTimeoutSeconds;
            return seconds is > 0 ? TimeSpan.FromSeconds(seconds.Value) : AgentVersionService.DefaultProbeTimeout;
        }
    }
}
