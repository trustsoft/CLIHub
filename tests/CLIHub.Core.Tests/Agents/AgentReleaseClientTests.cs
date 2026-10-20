namespace CLIHub.Core.Tests.Agents;

using System.Net;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Models;
using CLIHub.Core.Tests.Fakes;

public class AgentReleaseClientTests
{
    private readonly StubHandler handler = new();
    private readonly HttpClient httpClient;

    public AgentReleaseClientTests()
    {
        this.httpClient = new HttpClient(this.handler);
    }

    private AgentReleaseClient CreateClient() =>
        new(this.httpClient, new PreferencesStore(new FakeConfigurationRepository()), NullLogger<AgentReleaseClient>.Instance);

    private void RespondWithJson(string json) =>
        this.handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

    [Fact]
    public async Task GetLatestVersion_NpmBlock_ReturnsVersionField()
    {
        this.RespondWithJson("""{"version":"2.0.32"}""");
        var client = this.CreateClient();

        var version = await client.GetLatestVersionAsync(new PluginUpdateCheck("npm", Package: "@opencode/cli"));

        Assert.Equal("2.0.32", version);
        Assert.Equal(
            "https://registry.npmjs.org/%40opencode%2Fcli/latest",
            this.handler.LastRequest!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetLatestVersion_GitHubBlock_StripsLeadingVFromTagName()
    {
        this.RespondWithJson("""{"tag_name":"v1.0.100"}""");
        var client = this.CreateClient();

        var version = await client.GetLatestVersionAsync(new PluginUpdateCheck("githubReleases", Repo: "github/copilot-cli"));

        Assert.Equal("1.0.100", version);
        Assert.Equal(
            "https://api.github.com/repos/github/copilot-cli/releases/latest",
            this.handler.LastRequest!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetLatestVersion_NonSuccessStatus_ReturnsNull()
    {
        this.handler.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var client = this.CreateClient();

        var version = await client.GetLatestVersionAsync(new PluginUpdateCheck("npm", Package: "cline"));

        Assert.Null(version);
    }

    [Fact]
    public async Task GetLatestVersion_NetworkFailure_ReturnsNullWithoutEscaping()
    {
        this.handler.Throw = new HttpRequestException("connection refused");
        var client = this.CreateClient();

        var version = await client.GetLatestVersionAsync(new PluginUpdateCheck("npm", Package: "cline"));

        Assert.Null(version);
    }

    [Fact]
    public async Task GetLatestVersion_MalformedJson_ReturnsNull()
    {
        this.RespondWithJson("{ not json ");
        var client = this.CreateClient();

        var version = await client.GetLatestVersionAsync(new PluginUpdateCheck("npm", Package: "cline"));

        Assert.Null(version);
    }

    [Fact]
    public async Task GetLatestVersion_MissingVersionField_ReturnsNull()
    {
        this.RespondWithJson("""{"name":"cline"}""");
        var client = this.CreateClient();

        var version = await client.GetLatestVersionAsync(new PluginUpdateCheck("npm", Package: "cline"));

        Assert.Null(version);
    }

    [Fact]
    public async Task GetLatestVersion_RequestCarriesUserAgent()
    {
        this.RespondWithJson("""{"version":"1.0.0"}""");
        var client = this.CreateClient();

        await client.GetLatestVersionAsync(new PluginUpdateCheck("npm", Package: "pi"));

        var userAgent = string.Join(" ", this.handler.LastRequest!.Headers.UserAgent.Select(product => product.ToString()));
        Assert.Equal("CLIHub", userAgent);
    }

    [Theory]
    [InlineData("cargo", null, null)]
    [InlineData("npm", null, null)]
    [InlineData("npm", "", null)]
    [InlineData("githubReleases", "some/package", "")]
    public async Task GetLatestVersion_UnknownOrMalformedBlock_ReturnsNullWithoutRequest(
        string source,
        string? package,
        string? repo)
    {
        var client = this.CreateClient();

        var version = await client.GetLatestVersionAsync(new PluginUpdateCheck(source, package, repo));

        Assert.Null(version);
        Assert.Null(this.handler.LastRequest);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        public HttpResponseMessage Response { get; set; } = new(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };

        public Exception? Throw { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.LastRequest = request;
            if (this.Throw is not null)
            {
                throw this.Throw;
            }

            return Task.FromResult(this.Response);
        }
    }
}
