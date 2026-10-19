namespace CLIHub.Core.Tests.Infrastructure.Persistence;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Infrastructure.Persistence;

public class LogoCacheServiceTests : IDisposable
{
    private readonly string tempDir;

    public LogoCacheServiceTests()
    {
        this.tempDir = Path.Combine(Path.GetTempPath(), "clihub-logocache-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this.tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(this.tempDir, recursive: true); } catch { }
    }

    private string StatePath => Path.Combine(this.tempDir, "logos.json");

    private LogoCacheService CreateService(string? statePath = null) =>
        new(NullLogger<LogoCacheService>.Instance, statePath ?? this.StatePath);

    [Fact]
    public void Constructor_ValidStateFile_LoadsEntriesWithoutResolving()
    {
        File.WriteAllText(this.StatePath, """{ "logos": { "project:abc": "C:\\logos\\abc.png" } }""");

        var service = this.CreateService();

        Assert.Equal("C:\\logos\\abc.png", service.GetOrResolve("project:abc", () => throw new InvalidOperationException("must not resolve")));
    }

    [Fact]
    public void Constructor_CorruptStateFile_StartsEmptyAndContinues()
    {
        File.WriteAllText(this.StatePath, "{ not valid json ");

        var service = this.CreateService();

        Assert.Equal("resolved.png", service.GetOrResolve("plugin:x", () => "resolved.png"));
    }

    [Fact]
    public void GetOrResolve_Miss_ResolvesAndStores_SecondCallIsCacheHit()
    {
        var service = this.CreateService();
        var calls = 0;

        string? Resolver()
        {
            calls++;
            return "logo.png";
        }

        var first = service.GetOrResolve("project:a", Resolver);
        var second = service.GetOrResolve("project:a", Resolver);

        Assert.Equal("logo.png", first);
        Assert.Equal("logo.png", second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void GetOrResolve_NoLogoFound_NegativeResultIsCached()
    {
        var service = this.CreateService();
        var calls = 0;

        var first = service.GetOrResolve("project:b", () => { calls++; return null; });
        var second = service.GetOrResolve("project:b", () => { calls++; return null; });

        Assert.Null(first);
        Assert.Null(second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Dispose_AfterResolutions_WritesStateFileWithoutTempRemains()
    {
        var service = this.CreateService();
        service.GetOrResolve("project:c", () => "C:\\c\\logo.png");

        service.Dispose();

        Assert.True(File.Exists(this.StatePath), "state file must be written on dispose");
        Assert.False(File.Exists(this.StatePath + ".tmp"), "temp file must not remain after save");
        Assert.Contains("project:c", File.ReadAllText(this.StatePath));
    }

    [Fact]
    public void Dispose_NothingResolved_DoesNotCreateStateFile()
    {
        using var service = this.CreateService();

        Assert.False(File.Exists(this.StatePath), "state file must not be written when nothing changed");
    }

    [Fact]
    public void Dispose_SecondCall_DoesNotThrowOrRewrite()
    {
        var service = this.CreateService();
        service.GetOrResolve("project:d", () => "d.png");
        service.Dispose();
        var lastWrite = File.GetLastWriteTimeUtc(this.StatePath);

        service.Dispose();

        Assert.Equal(lastWrite, File.GetLastWriteTimeUtc(this.StatePath));
    }

    [Fact]
    public void Restart_NewInstanceOnSameStateFile_ReturnsCachedValueWithoutResolving()
    {
        var first = this.CreateService();
        first.GetOrResolve("plugin:opencode", () => "C:\\plugins\\opencode\\logo.png");
        first.Dispose();

        var calls = 0;
        var second = this.CreateService();
        var logo = second.GetOrResolve("plugin:opencode", () => { calls++; return "other.png"; });

        Assert.Equal("C:\\plugins\\opencode\\logo.png", logo);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void InvalidateAll_NextLookup_RerunsResolverAndUpdatesCache()
    {
        var service = this.CreateService();
        var logoFile = Path.Combine(this.tempDir, "logo.png");
        File.WriteAllText(logoFile, "x");

        var before = service.GetOrResolve("project:e", () => logoFile);
        service.InvalidateAll();
        File.WriteAllText(logoFile, "renamed");
        var renamedFile = Path.Combine(this.tempDir, "icon.png");
        File.Move(logoFile, renamedFile);

        var after = service.GetOrResolve("project:e", () => renamedFile);

        Assert.Equal(logoFile, before);
        Assert.Equal(renamedFile, after);
        Assert.Equal(renamedFile, service.GetOrResolve("project:e", () => throw new InvalidOperationException("must stay cached")));
    }

    [Fact]
    public void InvalidateAll_WithPrefix_RemovesOnlyPrefixedEntries()
    {
        var service = this.CreateService();
        service.GetOrResolve("plugin:opencode", () => "opencode.png");
        service.GetOrResolve("plugin:pi", () => null);
        service.GetOrResolve("project:abc", () => "abc.png");

        service.InvalidateAll(LogoCacheKeys.PluginPrefix);

        var pluginCalls = 0;
        Assert.Equal("fresh.png", service.GetOrResolve("plugin:opencode", () => { pluginCalls++; return "fresh.png"; }));
        Assert.Equal(1, pluginCalls);
        var negativeCalls = 0;
        Assert.Null(service.GetOrResolve("plugin:pi", () => { negativeCalls++; return null; }));
        Assert.Equal(1, negativeCalls);
        Assert.Equal("abc.png", service.GetOrResolve("project:abc", () => throw new InvalidOperationException("project entry must be preserved")));
    }

    [Fact]
    public void InvalidateAll_WithPrefixMatchingNothing_DoesNotDirtyState()
    {
        File.WriteAllText(this.StatePath, """{ "logos": { "project:abc": "C:\\logos\\abc.png" } }""");
        Thread.Sleep(50);
        var lastWrite = File.GetLastWriteTimeUtc(this.StatePath);
        var service = this.CreateService();

        service.InvalidateAll(LogoCacheKeys.PluginPrefix);

        service.Dispose();
        Assert.Equal(lastWrite, File.GetLastWriteTimeUtc(this.StatePath));
    }

    [Fact]
    public void InvalidateAll_WithPrefixMatchingEntries_MarksStateDirty()
    {
        File.WriteAllText(
            this.StatePath,
            """{ "logos": { "plugin:opencode": "C:\\logos\\open.png", "project:abc": "C:\\logos\\abc.png" } }""");
        var service = this.CreateService();

        service.InvalidateAll(LogoCacheKeys.PluginPrefix);

        service.Dispose();
        var state = File.ReadAllText(this.StatePath);
        Assert.DoesNotContain("plugin:opencode", state);
        Assert.Contains("project:abc", state);
    }

    [Fact]
    public void InvalidateAll_EmptyCache_DoesNotDirtyState()
    {
        var service = this.CreateService();

        service.InvalidateAll();
        service.InvalidateAll(LogoCacheKeys.PluginPrefix);

        service.Dispose();
        Assert.False(File.Exists(this.StatePath), "invalidating an empty cache must not create a state file");
    }

    [Fact]
    public void Remove_ExistingKey_EntryDeletedAndNextLookupReruns()
    {
        var service = this.CreateService();
        var calls = 0;

        service.GetOrResolve("project:r", () => { calls++; return "old.png"; });
        service.Remove("project:r");
        var after = service.GetOrResolve("project:r", () => { calls++; return "new.png"; });

        Assert.Equal("new.png", after);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void Remove_UnknownKey_HasNoEffectAndDoesNotDirtyState()
    {
        var service = this.CreateService();

        service.Remove("project:never-cached");

        Assert.False(File.Exists(this.StatePath), "removing an unknown key must not create a state file");
    }

    [Fact]
    public void Dispose_AfterRemoval_SavedStateFileOmitsRemovedKey()
    {
        var service = this.CreateService();
        service.GetOrResolve("project:gone", () => "gone.png");
        service.GetOrResolve("project:kept", () => "kept.png");
        service.Remove("project:gone");

        service.Dispose();
        var state = File.ReadAllText(this.StatePath);

        Assert.DoesNotContain("project:gone", state);
        Assert.Contains("project:kept", state);
    }

    [Fact]
    public void GetOrResolve_NullKeyOrResolver_Throws()
    {
        var service = this.CreateService();

        Assert.Throws<ArgumentNullException>(() => service.GetOrResolve(null!, () => null));
        Assert.Throws<ArgumentNullException>(() => service.GetOrResolve("key", null!));
    }
}
