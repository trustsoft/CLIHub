namespace CLIHub.Core.Tests.Plugins;

using System.Net;
using System.Security.Cryptography;
using System.Text;

using CLIHub.Core.Configuration;
using CLIHub.Core.Plugins;
using CLIHub.Core.Tests.Fakes;

using Microsoft.Extensions.Logging.Abstractions;

public class PluginFeedClientTests
{
    private const string FeedUrl = "https://example.com/feed.json";
    private const int MaxIndexSizeBytes = 2 * 1024 * 1024; // 2 MB
    private const int MaxPackageSizeBytes = 10 * 1024 * 1024; // 10 MB

    private readonly StubHandler handler = new();

    private PluginFeedClient CreateClient(string feedUrl = FeedUrl, int? timeoutSeconds = null)
    {
        var preferences = new PreferencesStore(new FakeConfigurationRepository());
        preferences.Update(p => p.PluginFeedUrl = feedUrl);
        if (timeoutSeconds.HasValue)
        {
            preferences.Update(p => p.AgentProbeTimeoutSeconds = timeoutSeconds.Value);
        }

        return new PluginFeedClient(this.handler, preferences, NullLogger<PluginFeedClient>.Instance);
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
        this.handler.Enqueue(RedirectResponse("https://example.com/redirected"));
        this.handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK));
        this.handler.Enqueue(JsonResponse(ValidIndexJson));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.True(result.Success);
        Assert.Single(result.Index!.Plugins);
        Assert.Equal(3, this.handler.Requests.Count);
        Assert.Equal("https://example.com/redirected", this.handler.Requests[1].RequestUri!.AbsoluteUri);
        Assert.Equal("https://example.com/redirected", this.handler.Requests[2].RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetFeedAsync_RedirectToHttp_IsRejected()
    {
        this.handler.Enqueue(RedirectResponse("http://evil.example.com/feed"));
        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.False(result.Success);
        Assert.Contains("Redirect to non-HTTPS URL rejected", result.Errors);
        Assert.Single(this.handler.Requests);
    }

    [Fact]
    public async Task GetFeedAsync_TooManyRedirects_IsRejected()
    {
        for (var i = 0; i < 5; i++)
        {
            this.handler.Enqueue(RedirectResponse($"https://example.com/redirect-{i}"));
        }

        var client = this.CreateClient();

        var result = await client.GetFeedAsync();

        Assert.False(result.Success);
        Assert.Contains("Redirect limit exceeded (maximum 5 hops)", result.Errors);
        Assert.Equal(5, this.handler.Requests.Count);
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

    private void EnqueueFeed(HttpResponseMessage response)
    {
        this.handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)); // HEAD
        this.handler.Enqueue(response); // GET
    }

    private void EnqueuePackage(HttpResponseMessage response)
    {
        this.handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)); // HEAD
        this.handler.Enqueue(response); // GET
    }

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
