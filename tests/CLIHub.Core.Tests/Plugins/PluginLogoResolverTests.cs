namespace CLIHub.Core.Tests.Plugins;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Plugins;

public class PluginLogoResolverTests : IDisposable
{
    private readonly string tempDir;

    public PluginLogoResolverTests()
    {
        this.tempDir = Path.Combine(Path.GetTempPath(), "clihub-pluginlogo-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this.tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(this.tempDir, recursive: true); } catch { }
    }

    private PluginLogoResolver CreateResolver(LogoOptions? options = null, string? statePath = null)
    {
        var cache = new LogoCacheService(
            NullLogger<LogoCacheService>.Instance,
            statePath ?? Path.Combine(this.tempDir, "logos.json"));

        return new PluginLogoResolver(cache, options ?? new LogoOptions(null));
    }

    [Fact]
    public void Resolve_LogoPngExists_ReturnsLogoPath()
    {
        var pluginDir = Directory.CreateDirectory(Path.Combine(this.tempDir, "opencode")).FullName;
        File.WriteAllText(Path.Combine(pluginDir, "logo.png"), "png");
        var resolver = this.CreateResolver();

        var logo = resolver.Resolve("opencode", pluginDir);

        Assert.Equal(Path.Combine(pluginDir, "logo.png"), logo);
    }

    [Fact]
    public void Resolve_NoLogo_NegativeOutcomeIsCached()
    {
        var pluginDir = Directory.CreateDirectory(Path.Combine(this.tempDir, "bare")).FullName;
        var resolver = this.CreateResolver();

        Assert.Null(resolver.Resolve("bare", pluginDir));
        Assert.Null(resolver.Resolve("bare", pluginDir));

        // A logo added after the negative outcome stays hidden until the cache entry is invalidated.
        File.WriteAllText(Path.Combine(pluginDir, "logo.png"), "png");

        Assert.Null(resolver.Resolve("bare", pluginDir));
    }

    [Fact]
    public void Resolve_NoLogo_ReturnsDefaultLogoPath()
    {
        var pluginDir = Directory.CreateDirectory(Path.Combine(this.tempDir, "bare")).FullName;
        var resolver = this.CreateResolver(new LogoOptions("default-logo.png"));

        var logo = resolver.Resolve("bare", pluginDir);

        Assert.Equal("default-logo.png", logo);
    }

    [Fact]
    public void Resolve_SecondCall_ReusesCacheWithoutScanning()
    {
        var pluginDir = Directory.CreateDirectory(Path.Combine(this.tempDir, "opencode")).FullName;
        File.WriteAllText(Path.Combine(pluginDir, "logo.png"), "png");
        var statePath = Path.Combine(this.tempDir, "logos.json");

        string? first;
        using (var cache = new LogoCacheService(NullLogger<LogoCacheService>.Instance, statePath))
        {
            first = new PluginLogoResolver(cache, new LogoOptions(null)).Resolve("opencode", pluginDir);
        }

        File.Delete(Path.Combine(pluginDir, "logo.png"));
        using (var cache = new LogoCacheService(NullLogger<LogoCacheService>.Instance, statePath))
        {
            var second = new PluginLogoResolver(cache, new LogoOptions(null)).Resolve("opencode", pluginDir);

            Assert.Equal(first, second);
            Assert.Equal(Path.Combine(pluginDir, "logo.png"), second);
        }
    }
}
