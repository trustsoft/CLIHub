namespace CLIHub.Tests.Services;

using CLIHub.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

public class LogoCacheServiceTests : IDisposable
{
    private readonly string _tempDir;

    public LogoCacheServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "clihub-logocache-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private string StatePath => Path.Combine(_tempDir, "logos.json");

    private LogoCacheService CreateService(string? statePath = null) =>
        new(NullLogger<LogoCacheService>.Instance, statePath ?? StatePath);

    [Fact]
    public void Constructor_ValidStateFile_LoadsEntriesWithoutResolving()
    {
        File.WriteAllText(StatePath, """{ "logos": { "project:abc": "C:\\logos\\abc.png" } }""");

        var service = CreateService();

        Assert.Equal("C:\\logos\\abc.png", service.GetOrResolve("project:abc", () => throw new InvalidOperationException("must not resolve")));
    }

    [Fact]
    public void Constructor_CorruptStateFile_StartsEmptyAndContinues()
    {
        File.WriteAllText(StatePath, "{ not valid json ");

        var service = CreateService();

        Assert.Equal("resolved.png", service.GetOrResolve("plugin:x", () => "resolved.png"));
    }

    [Fact]
    public void GetOrResolve_Miss_ResolvesAndStores_SecondCallIsCacheHit()
    {
        var service = CreateService();
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
        var service = CreateService();
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
        var service = CreateService();
        service.GetOrResolve("project:c", () => "C:\\c\\logo.png");

        service.Dispose();

        Assert.True(File.Exists(StatePath), "state file must be written on dispose");
        Assert.False(File.Exists(StatePath + ".tmp"), "temp file must not remain after save");
        Assert.Contains("project:c", File.ReadAllText(StatePath));
    }

    [Fact]
    public void Dispose_NothingResolved_DoesNotCreateStateFile()
    {
        using var service = CreateService();

        Assert.False(File.Exists(StatePath), "state file must not be written when nothing changed");
    }

    [Fact]
    public void Dispose_SecondCall_DoesNotThrowOrRewrite()
    {
        var service = CreateService();
        service.GetOrResolve("project:d", () => "d.png");
        service.Dispose();
        var lastWrite = File.GetLastWriteTimeUtc(StatePath);

        service.Dispose();

        Assert.Equal(lastWrite, File.GetLastWriteTimeUtc(StatePath));
    }

    [Fact]
    public void Restart_NewInstanceOnSameStateFile_ReturnsCachedValueWithoutResolving()
    {
        var first = CreateService();
        first.GetOrResolve("plugin:opencode", () => "C:\\plugins\\opencode\\logo.png");
        first.Dispose();

        var calls = 0;
        var second = CreateService();
        var logo = second.GetOrResolve("plugin:opencode", () => { calls++; return "other.png"; });

        Assert.Equal("C:\\plugins\\opencode\\logo.png", logo);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void InvalidateAll_NextLookup_RerunsResolverAndUpdatesCache()
    {
        var service = CreateService();
        var logoFile = Path.Combine(_tempDir, "logo.png");
        File.WriteAllText(logoFile, "x");

        var before = service.GetOrResolve("project:e", () => logoFile);
        service.InvalidateAll();
        File.WriteAllText(logoFile, "renamed");
        var renamedFile = Path.Combine(_tempDir, "icon.png");
        File.Move(logoFile, renamedFile);

        var after = service.GetOrResolve("project:e", () => renamedFile);

        Assert.Equal(logoFile, before);
        Assert.Equal(renamedFile, after);
        Assert.Equal(renamedFile, service.GetOrResolve("project:e", () => throw new InvalidOperationException("must stay cached")));
    }

    [Fact]
    public void Remove_ExistingKey_EntryDeletedAndNextLookupReruns()
    {
        var service = CreateService();
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
        var service = CreateService();

        service.Remove("project:never-cached");

        Assert.False(File.Exists(StatePath), "removing an unknown key must not create a state file");
    }

    [Fact]
    public void Dispose_AfterRemoval_SavedStateFileOmitsRemovedKey()
    {
        var service = CreateService();
        service.GetOrResolve("project:gone", () => "gone.png");
        service.GetOrResolve("project:kept", () => "kept.png");
        service.Remove("project:gone");

        service.Dispose();
        var state = File.ReadAllText(StatePath);

        Assert.DoesNotContain("project:gone", state);
        Assert.Contains("project:kept", state);
    }

    [Fact]
    public void GetOrResolve_NullKeyOrResolver_Throws()
    {
        var service = CreateService();

        Assert.Throws<ArgumentNullException>(() => service.GetOrResolve(null!, () => null));
        Assert.Throws<ArgumentNullException>(() => service.GetOrResolve("key", null!));
    }
}
