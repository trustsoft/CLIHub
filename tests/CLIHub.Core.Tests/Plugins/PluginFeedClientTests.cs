namespace CLIHub.Core.Tests.Plugins;

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using CLIHub.Core.Configuration;
using CLIHub.Core.Plugins;
using CLIHub.Core.Tests.Fakes;

using Microsoft.Extensions.Logging.Abstractions;

public class PluginFeedClientTests : IDisposable
{
    private const string FeedUrl = "https://example.com/feed.json";
    private const int MaxIndexSizeBytes = 2 * 1024 * 1024; // 2 MB
    private const int MaxPackageSizeBytes = 10 * 1024 * 1024; // 10 MB

    private static readonly JsonSerializerOptions MetadataOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly StubHandler handler = new();
    private readonly string cacheDirectory = Path.Combine(
        Path.GetTempPath(),
        $"clihub-feed-cache-{Guid.NewGuid():N}");

    private string CacheFilePath => Path.Combine(this.cacheDirectory, "feed-index.json");

    private string SyncMetadataFilePath => Path.Combine(this.cacheDirectory, "feed-sync.json");

    public void Dispose()
    {
        if (Directory.Exists(this.cacheDirectory))
        {
            Directory.Delete(this.cacheDirectory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private PluginFeedClient CreateClient(string feedUrl = FeedUrl, int? timeoutSeconds = null)
    {
        var preferences = new PreferencesStore(new FakeConfigurationRepository());
        preferences.Update(p => p.PluginFeedUrl = feedUrl);
        if (timeoutSeconds.HasValue)
        {
            preferences.Update(p => p.AgentProbeTimeoutSeconds = timeoutSeconds.Value);
        }

        return new PluginFeedClient(
            this.handler,
            preferences,
            NullLogger<PluginFeedClient>.Instance,
            cacheFilePath: this.CacheFilePath,
            syncMetadataFilePath: this.SyncMetadataFilePath);
    }

    [Fact]
    public async Task GetFeedAsync_FeedDisabled_ReturnsStructuredErrorAndSendsNoRequest()
    {
        var client = this.CreateClient(feedUrl: string.Empty);

        var result = await client.GetFeedAsync();

        Assert.False(result.Success);
        Assert.Null(result.Index);
        Assert.Contains("Feed is disabled in configuration", result.Errors);
        Assert.Empty(this.handler.Requests);
    }

    [Fact]
    public async Task GetFeedAsync_HttpFeedUrl_ReturnsErrorBeforeAnyRequest()
    {
        var client = this.CreateClient(feedUrl: "http://example.com/feed.json");

        var result = await client.GetFeedAsync();

        Assert.False(result.Success);
        Assert.Contains("Feed URL must use HTTPS", result.Errors);
        Assert.Empty(this.handler.Requests);
    }

    [Fact]
    public async Task DownloadPackageAsync_HttpPackageUrl_ReturnsErrorBeforeAnyRequest()
    {
        var client = this.CreateClient();

        var result = await client.DownloadPackageAsync("http://example.com/pkg.zip", new string('0', 64));

        Assert.False(result.Success);
        Assert.Contains("Package URL must use HTTPS", result.Errors);
        Assert.Empty(this.handler.Requests);
    }

    [Fact]
    public async Task GetFeedAsync_ValidHttpsIndex_ParsesEntries()
    {
        this.EnqueueFeed(JsonResponse(ValidIndexJson));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.True(result.Success);
        Assert.NotNull(result.Index);
        Assert.Equal(1, result.Index!.SchemaVersion);
        Assert.Equal("2026-11-04T12:00:00Z", result.Index.LastUpdated);

        var entry = Assert.Single(result.Index.Plugins);
        Assert.Equal("plugin-a", entry.Id);
        Assert.Equal("Plugin A", entry.Name);
        Assert.Equal("1.0.0", entry.Version);
        Assert.Equal("Pub", entry.Publisher);
        Assert.Equal("https://example.com/a", entry.Homepage);
        Assert.Equal("https://example.com/a.zip", entry.PackageUrl);
        Assert.Equal("abc123", entry.Sha256);
        Assert.Equal("https://example.com/a/releases", entry.ReleaseNotesUrl);
        Assert.Null(entry.Signature);
    }

    [Fact]
    public async Task GetFeedAsync_UnknownFields_AreTolerated()
    {
        var json = """
            {"schemaVersion":1,"unknownTop":true,"plugins":[
              {"id":"a","name":"A","description":"D","version":"1.0.0","schemaVersion":1,
               "minAppVersion":"1.0.0","publisher":"P","homepage":"https://e.com",
               "packageUrl":"https://e.com/a.zip","sha256":"abc","extraField":{"nested":[1,2,3]}}
            ]}
            """;
        this.EnqueueFeed(JsonResponse(json));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.True(result.Success);
        Assert.Single(result.Index!.Plugins);
    }

    [Fact]
    public async Task GetFeedAsync_DuplicateIds_KeepsFirst()
    {
        var json = """
            {"schemaVersion":1,"plugins":[
              {"id":"a","name":"First","description":"D","version":"1.0.0","schemaVersion":1,
               "minAppVersion":"1.0.0","publisher":"P","homepage":"https://e.com",
               "packageUrl":"https://e.com/a.zip","sha256":"abc"},
              {"id":"a","name":"Second","description":"D","version":"2.0.0","schemaVersion":1,
               "minAppVersion":"1.0.0","publisher":"P","homepage":"https://e.com",
               "packageUrl":"https://e.com/a2.zip","sha256":"def"}
            ]}
            """;
        this.EnqueueFeed(JsonResponse(json));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.True(result.Success);
        var entry = Assert.Single(result.Index!.Plugins);
        Assert.Equal("First", entry.Name);
        Assert.Equal("1.0.0", entry.Version);
    }

    [Fact]
    public async Task GetFeedAsync_InvalidEntry_IsSkipped()
    {
        var json = """
            {"schemaVersion":1,"plugins":[
              {"id":"valid","name":"Valid","description":"D","version":"1.0.0","schemaVersion":1,
               "minAppVersion":"1.0.0","publisher":"P","homepage":"https://e.com",
               "packageUrl":"https://e.com/v.zip","sha256":"abc"},
              {"id":"broken","description":"missing required fields"}
            ]}
            """;
        this.EnqueueFeed(JsonResponse(json));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.True(result.Success);
        var entry = Assert.Single(result.Index!.Plugins);
        Assert.Equal("valid", entry.Id);
    }

    [Fact]
    public async Task GetFeedAsync_IndexOverLimit_WithContentLength_ReturnsError()
    {
        var bytes = Encoding.UTF8.GetBytes(OversizedIndexJson(MaxIndexSizeBytes + 1));
        this.EnqueueFeed(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) });
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.False(result.Success);
        Assert.Contains("Feed index too large", result.Errors);
    }

    [Fact]
    public async Task GetFeedAsync_IndexOverLimit_ChunkedNoContentLength_ReturnsError()
    {
        var bytes = Encoding.UTF8.GetBytes(OversizedIndexJson(MaxIndexSizeBytes + 1));
        this.EnqueueFeed(ChunkedResponse(bytes));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.False(result.Success);
        Assert.Contains("Feed index too large", result.Errors);
    }

    [Fact]
    public async Task GetFeedAsync_IndexOverLimit_LyingContentLength_ReturnsError()
    {
        var bytes = Encoding.UTF8.GetBytes(OversizedIndexJson(MaxIndexSizeBytes + 1));
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentLength = 100; // server lies about the size
        this.EnqueueFeed(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.False(result.Success);
        Assert.Contains("Feed index too large", result.Errors);
    }

    [Fact]
    public async Task GetFeedAsync_RedirectToHttps_IsFollowed()
    {
        this.EnqueueFeed(RedirectResponse("https://example.com/redirected"));
        this.EnqueueFeed(JsonResponse(ValidIndexJson));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.True(result.Success);
        Assert.Single(result.Index!.Plugins);
        Assert.Equal(2, this.handler.Requests.Count);
        Assert.Equal("https://example.com/feed.json", this.handler.Requests[0].RequestUri!.AbsoluteUri);
        Assert.Equal("https://example.com/redirected", this.handler.Requests[1].RequestUri!.AbsoluteUri);
        Assert.All(this.handler.Requests, request => Assert.Equal(HttpMethod.Get, request.Method));
    }

    [Fact]
    public async Task GetFeedAsync_NoRedirects_SendsSingleGetRequest()
    {
        this.EnqueueFeed(JsonResponse(ValidIndexJson));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.True(result.Success);
        var request = Assert.Single(this.handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://example.com/feed.json", request.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetFeedAsync_RedirectWithoutLocation_ReturnsStructuredError()
    {
        // Decision D2: a 3xx without a Location header is terminal; the caller's status check
        // turns it into the regular structured request error (not a redirect-specific string).
        this.EnqueueFeed(new HttpResponseMessage(HttpStatusCode.Redirect));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.False(result.Success);
        Assert.Contains("Could not load plugin feed", result.Errors);
    }

    [Fact]
    public async Task GetFeedAsync_RelativeRedirectLocation_IsResolvedAgainstCurrentUrl()
    {
        var redirect = new HttpResponseMessage(HttpStatusCode.Redirect);
        redirect.Headers.Location = new Uri("redirected", UriKind.Relative);
        this.EnqueueFeed(redirect);
        this.EnqueueFeed(JsonResponse(ValidIndexJson));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.True(result.Success);
        Assert.Single(result.Index!.Plugins);
        Assert.Equal("https://example.com/redirected", this.handler.Requests[1].RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetFeedAsync_RedirectToHttp_IsRejected()
    {
        this.EnqueueFeed(RedirectResponse("http://evil.example.com/feed"));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.False(result.Success);
        Assert.Contains("Redirect to non-HTTPS URL rejected", result.Errors);
        Assert.Single(this.handler.Requests);
        Assert.Equal(HttpMethod.Get, this.handler.Requests[0].Method);
    }

    [Fact]
    public async Task GetFeedAsync_TooManyRedirects_IsRejected()
    {
        for (var i = 0; i < 5; i++)
        {
            this.EnqueueFeed(RedirectResponse($"https://example.com/redirect-{i}"));
        }

        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.False(result.Success);
        Assert.Contains("Redirect limit exceeded (maximum 5 hops)", result.Errors);
        Assert.Equal(5, this.handler.Requests.Count);
        Assert.All(this.handler.Requests, request => Assert.Equal(HttpMethod.Get, request.Method));
    }

    [Fact]
    public async Task GetFeedAsync_NetworkFailure_ReturnsStructuredError()
    {
        this.handler.Throw = new HttpRequestException("connection refused");
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.False(result.Success);
        Assert.Contains("Could not load plugin feed. Check your network connection.", result.Errors);
    }

    [Fact]
    public async Task GetFeedAsync_MalformedRedirectLocation_ReturnsStructuredError()
    {
        // A redirect whose Location cannot be resolved to a valid URI surfaces a UriFormatException
        // during redirect resolution; it must degrade to a structured error, not escape the caller.
        this.handler.Throw = new UriFormatException("Invalid URI: the redirect Location was malformed");
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task GetFeedAsync_Timeout_ReturnsStructuredError()
    {
        this.handler.Delay = TimeSpan.FromSeconds(30);
        var client = this.CreateClient(timeoutSeconds: 1);

        var result = await client.GetFeedAsync();

        Assert.False(result.Success);
        Assert.Contains("Feed request timed out", result.Errors);
    }

    [Fact]
    public async Task DownloadPackageAsync_ChecksumMatch_ReturnsTempFile()
    {
        var package = new byte[] { 1, 2, 3, 4, 5 };
        var expectedSha256 = Convert.ToHexString(SHA256.HashData(package));
        this.EnqueuePackage(ByteResponse(package));
        var client = this.CreateClient();

        string? tempFile = null;
        try
        {
            var result = await client.DownloadPackageAsync("https://example.com/pkg.zip", expectedSha256);

            Assert.True(result.Success);
            Assert.NotNull(result.TempFilePath);
            Assert.True(File.Exists(result.TempFilePath));
            var request = Assert.Single(this.handler.Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://example.com/pkg.zip", request.RequestUri!.AbsoluteUri);
            tempFile = result.TempFilePath;
        }
        finally
        {
            if (tempFile is not null && File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task DownloadPackageAsync_RedirectToHttps_IsFollowed()
    {
        var package = new byte[] { 1, 2, 3, 4 };
        var expectedSha256 = Convert.ToHexString(SHA256.HashData(package));
        this.EnqueuePackage(RedirectResponse("https://example.com/pkg-redirected.zip"));
        this.EnqueuePackage(ByteResponse(package));
        var client = this.CreateClient();

        string? tempFile = null;
        try
        {
            var result = await client.DownloadPackageAsync("https://example.com/pkg.zip", expectedSha256);

            Assert.True(result.Success);
            Assert.Equal(2, this.handler.Requests.Count);
            Assert.Equal("https://example.com/pkg-redirected.zip", this.handler.Requests[1].RequestUri!.AbsoluteUri);
            Assert.All(this.handler.Requests, request => Assert.Equal(HttpMethod.Get, request.Method));
            tempFile = result.TempFilePath;
        }
        finally
        {
            if (tempFile is not null && File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task DownloadPackageAsync_RedirectToHttp_IsRejected()
    {
        var expectedSha256 = Convert.ToHexString(SHA256.HashData([1, 2, 3]));
        this.EnqueuePackage(RedirectResponse("http://evil.example.com/pkg.zip"));
        var client = this.CreateClient();

        var tempFilesBefore = ListTempFiles();
        var result = await client.DownloadPackageAsync("https://example.com/pkg.zip", expectedSha256);

        Assert.False(result.Success);
        Assert.Null(result.TempFilePath);
        Assert.Contains("Redirect to non-HTTPS URL rejected", result.Errors);
        var request = Assert.Single(this.handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(tempFilesBefore.Count, ListTempFiles().Count);
    }

    [Fact]
    public async Task DownloadPackageAsync_ChecksumMismatch_DeletesTempFileAndReturnsError()
    {
        var package = new byte[] { 1, 2, 3, 4, 5 };
        var wrongSha256 = new string('0', 64);
        this.EnqueuePackage(ByteResponse(package));
        var client = this.CreateClient();

        var tempFilesBefore = ListTempFiles();
        var result = await client.DownloadPackageAsync("https://example.com/pkg.zip", wrongSha256);

        Assert.False(result.Success);
        Assert.Null(result.TempFilePath);
        Assert.Contains("Package checksum mismatch", Assert.Single(result.Errors));
        Assert.Equal(tempFilesBefore.Count, ListTempFiles().Count);
    }

    [Fact]
    public async Task DownloadPackageAsync_PackageOverLimit_AbortsAndDeletesTempFile()
    {
        var oversized = new byte[MaxPackageSizeBytes + 1];
        this.EnqueuePackage(ChunkedResponse(oversized));
        var client = this.CreateClient();

        var tempFilesBefore = ListTempFiles();
        var result = await client.DownloadPackageAsync("https://example.com/pkg.zip", new string('0', 64));

        Assert.False(result.Success);
        Assert.Null(result.TempFilePath);
        Assert.Contains("Package too large", result.Errors);
        Assert.Equal(tempFilesBefore.Count, ListTempFiles().Count);
    }

    [Fact]
    public async Task GetFeedAsync_ValidV2Index_ParsesEntries()
    {
        this.EnqueueFeed(JsonResponse(ValidV2IndexJson));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.True(result.Success);
        Assert.NotNull(result.Index);
        Assert.Equal(2, result.Index!.SchemaVersion);
        Assert.Equal(2, result.Index.Plugins.Count);

        var packageEntry = result.Index.Plugins.Single(p => p.Id == "antigravity");
        Assert.Equal("https://example.com/antigravity-1.0.0.clihub-plugin.zip", packageEntry.PackageUrl);
        Assert.Equal("ff1d3a87", packageEntry.Sha256);
        Assert.NotNull(packageEntry.DescriptorJson);
        Assert.Contains("minAppVersion", packageEntry.DescriptorJson, StringComparison.Ordinal);
        Assert.Contains("detection", packageEntry.DescriptorJson, StringComparison.Ordinal);
        Assert.Equal(">=1.1.0", packageEntry.Compatibility);
        Assert.Equal("stable", packageEntry.Channel);
        Assert.Equal("https://example.com/antigravity/logo.png", packageEntry.LogoUrl);
        Assert.Null(packageEntry.MinAppVersion);
        Assert.Equal(1, packageEntry.SchemaVersion);

        var descriptorEntry = result.Index.Plugins.Single(p => p.Id == "descriptor-only");
        Assert.Null(descriptorEntry.PackageUrl);
        Assert.Null(descriptorEntry.Sha256);
        Assert.NotNull(descriptorEntry.DescriptorJson);
    }

    [Fact]
    public async Task GetFeedAsync_V2DescriptorOnlyEntry_HasNullPackageFields()
    {
        this.EnqueueFeed(JsonResponse(ValidV2IndexJson));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.True(result.Success);
        var descriptorEntry = result.Index!.Plugins.Single(p => p.Id == "descriptor-only");
        Assert.Null(descriptorEntry.PackageUrl);
        Assert.Null(descriptorEntry.Sha256);
        Assert.Equal(">=1.0.0", descriptorEntry.Compatibility);
        Assert.Equal("stable", descriptorEntry.Channel);
        Assert.NotNull(descriptorEntry.DescriptorJson);
    }

    [Fact]
    public async Task GetFeedAsync_V2NonStableChannel_EntrySkipped()
    {
        var json = """
            {"schemaVersion":2,"plugins":[
              {"id":"preview-plugin","name":"Preview","description":"D","version":"1.0.0",
               "compatibility":">=1.0.0","channel":"beta","publisher":"P","homepage":"https://e.com",
               "descriptor":{"id":"preview-plugin","name":"Preview","commands":{"launch":{"executable":"p"}}}}
            ]}
            """;
        this.EnqueueFeed(JsonResponse(json));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.True(result.Success);
        Assert.Empty(result.Index!.Plugins);
    }

    [Fact]
    public async Task GetFeedAsync_V2MalformedCompatibility_EntrySkipped()
    {
        var json = """
            {"schemaVersion":2,"plugins":[
              {"id":"bad-range","name":"Bad Range","description":"D","version":"1.0.0",
               "compatibility":"~1.0.0","channel":"stable","publisher":"P","homepage":"https://e.com",
               "descriptor":{"id":"bad-range","name":"Bad Range","commands":{"launch":{"executable":"b"}}}}
            ]}
            """;
        this.EnqueueFeed(JsonResponse(json));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.True(result.Success);
        Assert.Empty(result.Index!.Plugins);
    }

    [Fact]
    public async Task GetFeedAsync_V2InvalidDescriptor_EntrySkipped()
    {
        var json = """
            {"schemaVersion":2,"plugins":[
              {"id":"no-launch","name":"No Launch","description":"D","version":"1.0.0",
               "compatibility":">=1.0.0","channel":"stable","publisher":"P","homepage":"https://e.com",
               "descriptor":{"id":"no-launch","name":"No Launch","commands":{"version":{"executable":"n"}}}}
            ]}
            """;
        this.EnqueueFeed(JsonResponse(json));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.True(result.Success);
        Assert.Empty(result.Index!.Plugins);
    }

    [Fact]
    public async Task GetFeedAsync_V2DescriptorIdMismatch_EntrySkipped()
    {
        var json = """
            {"schemaVersion":2,"plugins":[
              {"id":"entry-id","name":"Mismatch","description":"D","version":"1.0.0",
               "compatibility":">=1.0.0","channel":"stable","publisher":"P","homepage":"https://e.com",
               "descriptor":{"id":"other-id","name":"Mismatch","commands":{"launch":{"executable":"m"}}}}
            ]}
            """;
        this.EnqueueFeed(JsonResponse(json));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.True(result.Success);
        Assert.Empty(result.Index!.Plugins);
    }

    [Fact]
    public async Task GetFeedAsync_V2DuplicateIdAfterInvalidEntry_KeepsValidEntry()
    {
        // The first entry shares its id with the second but fails descriptor validation; the id
        // is claimed only after validation, so the valid entry must not be skipped as a duplicate.
        var json = """
            {"schemaVersion":2,"plugins":[
              {"id":"twice","name":"First","description":"D","version":"1.0.0",
               "compatibility":">=1.0.0","channel":"stable","publisher":"P","homepage":"https://e.com",
               "descriptor":{"id":"twice","name":"First","commands":{"version":{"executable":"f"}}}},
              {"id":"twice","name":"Second","description":"D","version":"2.0.0",
               "compatibility":">=1.0.0","channel":"stable","publisher":"P","homepage":"https://e.com",
               "descriptor":{"id":"twice","name":"Second","commands":{"launch":{"executable":"s"}}}}
            ]}
            """;
        this.EnqueueFeed(JsonResponse(json));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.True(result.Success);
        var entry = Assert.Single(result.Index!.Plugins);
        Assert.Equal("Second", entry.Name);
        Assert.Equal("2.0.0", entry.Version);
        Assert.NotNull(entry.DescriptorJson);
    }

    [Fact]
    public async Task GetFeedAsync_V2PackageMissingSha256_EntrySkipped()
    {
        var json = """
            {"schemaVersion":2,"plugins":[
              {"id":"incomplete-package","name":"Incomplete","description":"D","version":"1.0.0",
               "compatibility":">=1.0.0","channel":"stable","publisher":"P","homepage":"https://e.com",
               "descriptor":{"id":"incomplete-package","name":"Incomplete","commands":{"launch":{"executable":"i"}}},
               "package":{"url":"https://example.com/pkg.zip"}}
            ]}
            """;
        this.EnqueueFeed(JsonResponse(json));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.True(result.Success);
        Assert.Empty(result.Index!.Plugins);
    }

    [Fact]
    public async Task GetFeedAsync_SchemaVersion3_ReturnsUnsupportedError()
    {
        var json = "{\"schemaVersion\":3,\"plugins\":[]}";
        this.EnqueueFeed(JsonResponse(json));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.False(result.Success);
        Assert.Null(result.Index);
        Assert.Contains("Unsupported feed schema version 3", result.Errors);
    }

    [Fact]
    public async Task DownloadLogoAsync_HttpsLogo_ReturnsBytes()
    {
        var logoBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A };
        this.EnqueueFeed(ByteResponse(logoBytes));
        var client = this.CreateClient();

        var result = await client.DownloadLogoAsync("https://example.com/logo.png");

        Assert.True(result.Success);
        Assert.Equal(logoBytes, result.LogoBytes);
        Assert.Empty(result.Errors);
        var request = Assert.Single(this.handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://example.com/logo.png", request.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task DownloadLogoAsync_HttpLogoUrl_ReturnsErrorBeforeAnyRequest()
    {
        var client = this.CreateClient();

        var result = await client.DownloadLogoAsync("http://example.com/logo.png");

        Assert.False(result.Success);
        Assert.Null(result.LogoBytes);
        Assert.Contains("Logo URL must use HTTPS", result.Errors);
        Assert.Empty(this.handler.Requests);
    }

    [Fact]
    public async Task DownloadLogoAsync_RedirectToHttp_IsRejected()
    {
        this.EnqueueFeed(RedirectResponse("http://evil.example.com/logo.png"));
        var client = this.CreateClient();

        var result = await client.DownloadLogoAsync("https://example.com/logo.png");

        Assert.False(result.Success);
        Assert.Null(result.LogoBytes);
        Assert.Contains("Redirect to non-HTTPS URL rejected", result.Errors);
        var request = Assert.Single(this.handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
    }

    [Fact]
    public async Task DownloadLogoAsync_RedirectToHttps_IsFollowed()
    {
        var logoBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        this.EnqueueFeed(RedirectResponse("https://example.com/logo-redirected.png"));
        this.EnqueueFeed(ByteResponse(logoBytes));
        var client = this.CreateClient();

        var result = await client.DownloadLogoAsync("https://example.com/logo.png");

        Assert.True(result.Success);
        Assert.Equal(logoBytes, result.LogoBytes);
        Assert.Equal(2, this.handler.Requests.Count);
        Assert.Equal("https://example.com/logo-redirected.png", this.handler.Requests[1].RequestUri!.AbsoluteUri);
        Assert.All(this.handler.Requests, request => Assert.Equal(HttpMethod.Get, request.Method));
    }

    [Fact]
    public async Task DownloadLogoAsync_OverLimit_ReturnsError()
    {
        var oversized = new byte[MaxPackageSizeBytes + 1];
        this.EnqueueFeed(ChunkedResponse(oversized));
        var client = this.CreateClient();

        var result = await client.DownloadLogoAsync("https://example.com/logo.png");

        Assert.False(result.Success);
        Assert.Null(result.LogoBytes);
        Assert.Contains("Logo download failed", result.Errors);
    }

    [Fact]
    public async Task DownloadLogoAsync_NetworkFailure_ReturnsStructuredError()
    {
        this.handler.Throw = new HttpRequestException("connection refused");
        var client = this.CreateClient();

        var result = await client.DownloadLogoAsync("https://example.com/logo.png");

        Assert.False(result.Success);
        Assert.Null(result.LogoBytes);
        Assert.Contains("Logo download failed", result.Errors);
    }

    [Fact]
    public async Task DownloadLogoAsync_Timeout_ReturnsStructuredError()
    {
        this.handler.Delay = TimeSpan.FromSeconds(30);
        var client = this.CreateClient(timeoutSeconds: 1);

        var result = await client.DownloadLogoAsync("https://example.com/logo.png");

        Assert.False(result.Success);
        Assert.Null(result.LogoBytes);
        Assert.Contains("Logo download timed out", result.Errors);
    }

    [Fact]
    public async Task GetFeedAsync_FirstFetch_SendsNoConditionalHeaders()
    {
        this.EnqueueFeed(JsonResponse(ValidIndexJson));
        var client = this.CreateClient();

        await client.GetFeedAsync();

        var request = Assert.Single(this.handler.Requests);
        Assert.False(request.Headers.Contains("If-None-Match"));
        Assert.False(request.Headers.Contains("If-Modified-Since"));
    }

    [Fact]
    public async Task GetFeedAsync_SuccessfulFetch_StoresCachedIndexAndSyncMetadata()
    {
        this.EnqueueFeed(WithEtag(JsonResponse(ValidIndexJson), "\"v1\""));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.True(result.Success);
        Assert.False(result.FromCache);
        Assert.Null(result.CachedAt);
        Assert.Equal(ValidIndexJson, File.ReadAllText(this.CacheFilePath));

        var metadata = this.ReadMetadata();
        Assert.NotNull(metadata);
        Assert.Equal("\"v1\"", metadata.ETag);
        Assert.NotNull(metadata.Timestamp);
        Assert.False(string.IsNullOrWhiteSpace(metadata.IndexHash));
    }

    [Fact]
    public async Task GetFeedAsync_SuccessfulFetch_WritesTheComparisonHashOfTheParsedIndex()
    {
        this.EnqueueFeed(JsonResponse(ValidIndexJson));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        var metadata = this.ReadMetadata();
        Assert.NotNull(metadata);
        Assert.NotNull(metadata.IndexHash);
        Assert.Equal(
            PluginFeedSyncMetadata.ComputeIndexHash(result.Index!.Plugins),
            metadata.IndexHash);
    }

    [Fact]
    public async Task GetFeedAsync_SuccessfulFetch_ReplacesAStaleStoredHash()
    {
        // A cache written before the sync service ever ran (or by an older build) must not leave a
        // stale hash behind: the next 304 would otherwise read a mismatch and publish a spurious
        // change event (verifier finding a).
        this.WriteCachedIndex(ValidIndexJson);
        this.WriteMetadata(new PluginFeedSyncMetadata { IndexHash = "stale-hash" });
        this.EnqueueFeed(JsonResponse(ValidIndexJson));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.Equal(
            PluginFeedSyncMetadata.ComputeIndexHash(result.Index!.Plugins),
            this.ReadMetadata()!.IndexHash);
    }

    [Fact]
    public async Task GetFeedAsync_WithStoredEtag_SendsIfNoneMatchAndUsesCachedIndexOn304()
    {
        var fetchedAt = new DateTimeOffset(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);
        this.WriteCachedIndex(ValidIndexJson);
        this.WriteMetadata(new PluginFeedSyncMetadata { ETag = "\"v1\"", Timestamp = fetchedAt });
        this.EnqueueFeed(new HttpResponseMessage(HttpStatusCode.NotModified));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        // A 304 is a fresh confirmation, not stale data: success without the FromCache flag, and
        // the cached index is served without re-fetching or rewriting it.
        Assert.True(result.Success);
        Assert.False(result.FromCache);
        Assert.Null(result.CachedAt);
        Assert.Equal("plugin-a", Assert.Single(result.Index!.Plugins).Id);
        Assert.Equal(ValidIndexJson, File.ReadAllText(this.CacheFilePath));
        Assert.Equal(fetchedAt, this.ReadMetadata()!.Timestamp);

        var request = Assert.Single(this.handler.Requests);
        Assert.Equal("\"v1\"", Assert.Single(request.Headers.GetValues("If-None-Match")));
    }

    [Fact]
    public async Task GetFeedAsync_WithStoredLastModified_SendsIfModifiedSinceAndUsesCachedIndexOn304()
    {
        var lastModified = new DateTimeOffset(2026, 11, 13, 8, 15, 0, TimeSpan.Zero);
        this.WriteCachedIndex(ValidIndexJson);
        this.WriteMetadata(new PluginFeedSyncMetadata { LastModified = lastModified.ToString("r") });
        this.EnqueueFeed(new HttpResponseMessage(HttpStatusCode.NotModified));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.True(result.Success);
        Assert.False(result.FromCache);
        var request = Assert.Single(this.handler.Requests);
        Assert.Equal(lastModified.ToString("r"), Assert.Single(request.Headers.GetValues("If-Modified-Since")));
        Assert.False(request.Headers.Contains("If-None-Match"));
    }

    [Fact]
    public async Task GetFeedAsync_WithEtagAndLastModified_PrefersIfNoneMatch()
    {
        this.WriteCachedIndex(ValidIndexJson);
        this.WriteMetadata(new PluginFeedSyncMetadata
        {
            ETag = "\"v1\"",
            LastModified = new DateTimeOffset(2026, 11, 13, 8, 15, 0, TimeSpan.Zero).ToString("r")
        });
        this.EnqueueFeed(JsonResponse(ValidIndexJson));
        var client = this.CreateClient();

        await client.GetFeedAsync();

        var request = Assert.Single(this.handler.Requests);
        Assert.Equal("\"v1\"", Assert.Single(request.Headers.GetValues("If-None-Match")));
        Assert.False(request.Headers.Contains("If-Modified-Since"));
    }

    [Fact]
    public async Task GetFeedAsync_RedirectedConditionalFetch_SendsTheValidatorOnEveryHop()
    {
        this.WriteCachedIndex(ValidIndexJson);
        this.WriteMetadata(new PluginFeedSyncMetadata { ETag = "\"v1\"" });
        this.EnqueueFeed(RedirectResponse("https://example.com/redirected"));
        this.EnqueueFeed(JsonResponse(ValidIndexJson));
        var client = this.CreateClient();

        await client.GetFeedAsync();

        Assert.Equal(2, this.handler.Requests.Count);
        Assert.All(this.handler.Requests,
            request => Assert.Equal("\"v1\"", Assert.Single(request.Headers.GetValues("If-None-Match"))));
    }

    [Fact]
    public async Task GetFeedAsync_UnconditionalResponseWithLastModified_StoresItForTheNextFetch()
    {
        var lastModified = new DateTimeOffset(2026, 11, 12, 7, 0, 0, TimeSpan.Zero);
        var response = JsonResponse(ValidIndexJson);
        response.Content.Headers.LastModified = lastModified;
        this.EnqueueFeed(response);
        var client = this.CreateClient();

        await client.GetFeedAsync();

        var metadata = this.ReadMetadata();
        Assert.NotNull(metadata);
        Assert.Equal(lastModified.ToString("r"), metadata.LastModified);
        Assert.Null(metadata.ETag);
    }

    [Fact]
    public async Task GetFeedAsync_NetworkFailureWithCachedIndex_ServesCacheWithFromCacheAndTimestamp()
    {
        var cachedAt = new DateTimeOffset(2026, 11, 14, 10, 30, 0, TimeSpan.Zero);
        this.WriteCachedIndex(ValidIndexJson);
        this.WriteMetadata(new PluginFeedSyncMetadata { ETag = "\"v1\"", Timestamp = cachedAt });
        this.handler.Throw = new HttpRequestException("connection refused");
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.True(result.Success);
        Assert.True(result.FromCache);
        Assert.Equal(cachedAt, result.CachedAt);
        Assert.Equal("plugin-a", Assert.Single(result.Index!.Plugins).Id);
    }

    [Fact]
    public async Task GetFeedAsync_ServerErrorWithCachedIndex_ServesCache()
    {
        this.WriteCachedIndex(ValidIndexJson);
        this.WriteMetadata(new PluginFeedSyncMetadata { Timestamp = DateTimeOffset.UtcNow });
        this.EnqueueFeed(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.True(result.Success);
        Assert.True(result.FromCache);
    }

    [Fact]
    public async Task GetFeedAsync_ParseErrorWithCachedIndex_ServesCache()
    {
        this.WriteCachedIndex(ValidIndexJson);
        this.WriteMetadata(new PluginFeedSyncMetadata { Timestamp = DateTimeOffset.UtcNow });
        this.EnqueueFeed(JsonResponse("{\"schemaVersion\":9,\"plugins\":[]}"));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.True(result.Success);
        Assert.True(result.FromCache);
    }

    [Fact]
    public async Task GetFeedAsync_NetworkFailureWithoutCache_ReturnsStructuredError()
    {
        this.handler.Throw = new HttpRequestException("connection refused");
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.False(result.Success);
        Assert.Null(result.Index);
        Assert.False(result.FromCache);
        Assert.Contains("Could not load plugin feed. Check your network connection.", result.Errors);
    }

    [Fact]
    public async Task GetFeedAsync_RedirectPolicyRejection_DoesNotFallBackToCache()
    {
        this.WriteCachedIndex(ValidIndexJson);
        this.WriteMetadata(new PluginFeedSyncMetadata { Timestamp = DateTimeOffset.UtcNow });
        this.EnqueueFeed(RedirectResponse("http://evil.example.com/feed"));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        // A redirect-policy rejection is a security failure, not a transient network failure: the
        // rejection stays visible even with a usable cache.
        Assert.False(result.Success);
        Assert.False(result.FromCache);
        Assert.Contains("Redirect to non-HTTPS URL rejected", result.Errors);
    }

    [Fact]
    public async Task GetFeedAsync_LiveIndexOverLimitWithCachedIndex_DoesNotFallBackToCache()
    {
        this.WriteCachedIndex(ValidIndexJson);
        this.WriteMetadata(new PluginFeedSyncMetadata { Timestamp = DateTimeOffset.UtcNow });
        var oversized = Encoding.UTF8.GetBytes(OversizedIndexJson(MaxIndexSizeBytes + 1));
        this.EnqueueFeed(ChunkedResponse(oversized));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.False(result.Success);
        Assert.False(result.FromCache);
        Assert.Contains("Feed index too large", result.Errors);
    }

    [Fact]
    public async Task GetFeedAsync_304WithoutCachedIndex_ReturnsStructuredError()
    {
        // Metadata without an index is orphaned and dropped, so the request goes out
        // unconditionally; a 304 still cannot be served without cached content.
        this.WriteMetadata(new PluginFeedSyncMetadata { ETag = "\"v1\"" });
        this.EnqueueFeed(new HttpResponseMessage(HttpStatusCode.NotModified));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.False(result.Success);
        Assert.Contains("Could not load plugin feed", result.Errors);
        Assert.False(File.Exists(this.SyncMetadataFilePath));
    }

    [Fact]
    public async Task GetFeedAsync_CorruptCachedIndex_IsDeletedWithMetadataAndNotUsed()
    {
        Directory.CreateDirectory(this.cacheDirectory);
        File.WriteAllText(this.CacheFilePath, "{ this is not the feed index");
        this.WriteMetadata(new PluginFeedSyncMetadata { ETag = "\"v1\"", Timestamp = DateTimeOffset.UtcNow });
        this.handler.Throw = new HttpRequestException("offline");
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.False(result.Success);
        Assert.False(result.FromCache);
        Assert.False(File.Exists(this.CacheFilePath));
        Assert.False(File.Exists(this.SyncMetadataFilePath));
    }

    [Fact]
    public async Task GetFeedAsync_CachedIndexOverLimit_IsDeletedAndNotUsed()
    {
        Directory.CreateDirectory(this.cacheDirectory);
        File.WriteAllBytes(this.CacheFilePath, new byte[MaxIndexSizeBytes + 1]);
        this.WriteMetadata(new PluginFeedSyncMetadata { ETag = "\"v1\"", Timestamp = DateTimeOffset.UtcNow });
        this.handler.Throw = new HttpRequestException("offline");
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.False(result.Success);
        Assert.False(result.FromCache);
        Assert.False(File.Exists(this.CacheFilePath));
    }

    private void WriteCachedIndex(string json)
    {
        Directory.CreateDirectory(this.cacheDirectory);
        File.WriteAllText(this.CacheFilePath, json);
    }

    private void WriteMetadata(PluginFeedSyncMetadata metadata)
    {
        Directory.CreateDirectory(this.cacheDirectory);
        File.WriteAllText(this.SyncMetadataFilePath, JsonSerializer.Serialize(metadata, MetadataOptions));
    }

    private PluginFeedSyncMetadata? ReadMetadata() =>
        File.Exists(this.SyncMetadataFilePath)
            ? JsonSerializer.Deserialize<PluginFeedSyncMetadata>(File.ReadAllText(this.SyncMetadataFilePath), MetadataOptions)
            : null;

    private static HttpResponseMessage WithEtag(HttpResponseMessage response, string etag)
    {
        response.Headers.ETag = new EntityTagHeaderValue(etag);
        return response;
    }

    private void EnqueueFeed(HttpResponseMessage response) => this.handler.Enqueue(response);

    private void EnqueuePackage(HttpResponseMessage response) => this.handler.Enqueue(response);

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static HttpResponseMessage ByteResponse(byte[] bytes) =>
        new(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };

    private static HttpResponseMessage ChunkedResponse(byte[] bytes) =>
        new(HttpStatusCode.OK) { Content = new StreamContent(new NonSeekableReadStream(bytes)) };

    private static HttpResponseMessage RedirectResponse(string location) =>
        new(HttpStatusCode.Redirect) { Headers = { Location = new Uri(location) } };

    private static string OversizedIndexJson(int descriptionLength)
    {
        var padding = new string('a', descriptionLength);
        return "{\"schemaVersion\":1,\"plugins\":[{\"id\":\"x\",\"name\":\"X\",\"description\":\"" +
               padding +
               "\",\"version\":\"1.0.0\",\"schemaVersion\":1,\"minAppVersion\":\"1.0.0\",\"publisher\":\"P\"," +
               "\"homepage\":\"https://e.com\",\"packageUrl\":\"https://e.com/x.zip\",\"sha256\":\"abc\"}]}";
    }

    private static HashSet<string> ListTempFiles() =>
        Directory.GetFiles(Path.GetTempPath(), "clihub-plugin-*.zip").ToHashSet(StringComparer.OrdinalIgnoreCase);

    private const string ValidIndexJson = """
        {
          "schemaVersion": 1,
          "lastUpdated": "2026-11-04T12:00:00Z",
          "plugins": [
            {
              "id": "plugin-a",
              "name": "Plugin A",
              "description": "AI coding assistant",
              "version": "1.0.0",
              "schemaVersion": 1,
              "minAppVersion": "1.0.0",
              "publisher": "Pub",
              "homepage": "https://example.com/a",
              "packageUrl": "https://example.com/a.zip",
              "sha256": "abc123",
              "releaseNotesUrl": "https://example.com/a/releases",
              "signature": null
            }
          ]
        }
        """;

    // Mirrors the live v2 index shape (design "Verified Ground Truth"): the package-backed entry's
    // inline descriptor carries schemaVersion and minAppVersion; the descriptor-only entry omits
    // the package key entirely.
    private const string ValidV2IndexJson = """
        {
          "schemaVersion": 2,
          "lastUpdated": "2026-11-14T01:55:08Z",
          "plugins": [
            {
              "id": "antigravity",
              "name": "Antigravity",
              "description": "Google Antigravity CLI (agy)",
              "version": "1.0.0",
              "compatibility": ">=1.1.0",
              "channel": "stable",
              "publisher": "CLIHub",
              "homepage": "https://antigravity.google/docs/cli/overview",
              "descriptor": {
                "schemaVersion": 1,
                "id": "antigravity",
                "name": "Antigravity",
                "description": "Google Antigravity CLI (agy)",
                "version": "1.0.0",
                "publisher": "CLIHub",
                "homepage": "https://antigravity.google/docs/cli/overview",
                "minAppVersion": "1.1.0",
                "commands": {
                  "launch": { "executable": "agy" },
                  "resume": { "executable": "agy", "arguments": "--continue" },
                  "version": { "executable": "agy", "arguments": "--version" }
                },
                "detection": { "systemPaths": ["%USERPROFILE%\\.gemini\\antigravity-cli"] }
              },
              "package": {
                "url": "https://example.com/antigravity-1.0.0.clihub-plugin.zip",
                "sha256": "ff1d3a87"
              },
              "logoUrl": "https://example.com/antigravity/logo.png",
              "releaseNotesUrl": null,
              "signature": null
            },
            {
              "id": "descriptor-only",
              "name": "Descriptor Only",
              "description": "Descriptor-only plugin",
              "version": "0.9.0",
              "compatibility": ">=1.0.0",
              "channel": "stable",
              "publisher": "CLIHub",
              "homepage": "https://example.com/descriptor-only",
              "descriptor": {
                "id": "descriptor-only",
                "name": "Descriptor Only",
                "version": "0.9.0",
                "publisher": "CLIHub",
                "homepage": "https://example.com/descriptor-only",
                "commands": {
                  "launch": { "executable": "donly" }
                }
              }
            }
          ]
        }
        """;

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> responses = new();

        public List<HttpRequestMessage> Requests { get; } = [];

        public Exception? Throw { get; set; }

        public TimeSpan? Delay { get; set; }

        public void Enqueue(HttpResponseMessage response) => this.responses.Enqueue(_ => response);

        public void Enqueue(Func<HttpRequestMessage, HttpResponseMessage> factory) => this.responses.Enqueue(factory);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.Requests.Add(request);
            if (this.Throw is not null)
            {
                throw this.Throw;
            }

            if (this.Delay.HasValue)
            {
                await Task.Delay(this.Delay.Value, cancellationToken);
            }

            if (this.responses.Count == 0)
            {
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
            }

            return this.responses.Dequeue()(request);
        }
    }

    private sealed class NonSeekableReadStream : Stream
    {
        private readonly MemoryStream inner;

        public NonSeekableReadStream(byte[] data) => this.inner = new MemoryStream(data);

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => this.inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
