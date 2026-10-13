namespace CLIHub.Core.Tests.Agents;

using CLIHub.Core.Models;
using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Tests.Fakes;

public class AgentDetectionServiceTests : IDisposable
{
    private readonly string projectDir;
    private readonly FakeConfigurationRepository config = new();
    private readonly FakeTimeProvider time = new(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
    private readonly AgentDetectionService service;

    public AgentDetectionServiceTests()
    {
        this.projectDir = Path.Combine(Path.GetTempPath(), "clihub-detect-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this.projectDir);
        this.service = new AgentDetectionService(new PreferencesStore(this.config), this.time);
    }

    public void Dispose()
    {
        try { Directory.Delete(this.projectDir, recursive: true); } catch { }
    }

    [Fact]
    public void IsInstalledInSystem_MarkerPresent_ReturnsTrue()
    {
        var marker = Path.Combine(this.projectDir, "installed-marker");
        Directory.CreateDirectory(marker);
        var plugin = new Plugin
        {
            Id = "a", Name = "A",
            Detection = new AgentDetection { SystemPaths = { marker } }
        };

        Assert.True(this.service.IsInstalledInSystem(plugin));
    }

    [Fact]
    public void IsInstalledInSystem_NoMarker_ReturnsFalse()
    {
        var plugin = new Plugin
        {
            Id = "a", Name = "A",
            Detection = new AgentDetection { SystemPaths = { Path.Combine(this.projectDir, "missing") } }
        };

        Assert.False(this.service.IsInstalledInSystem(plugin));
    }

    [Fact]
    public void IsInstalledInSystem_ExpandsEnvironmentVariables()
    {
        var plugin = new Plugin
        {
            Id = "a", Name = "A",
            Detection = new AgentDetection { SystemPaths = { "%TEMP%" } }
        };

        Assert.True(this.service.IsInstalledInSystem(plugin));
    }

    [Fact]
    public void IsAvailableInProject_IndicatorPresent_ReturnsTrue()
    {
        Directory.CreateDirectory(Path.Combine(this.projectDir, ".opencode"));
        var plugin = new Plugin
        {
            Id = "a", Name = "A",
            Detection = new AgentDetection { ProjectIndicators = { ".opencode" } }
        };

        Assert.True(this.service.IsAvailableInProject(plugin, this.projectDir));
    }

    [Fact]
    public void IsAvailableInProject_IndicatorMissing_ReturnsFalse()
    {
        var plugin = new Plugin
        {
            Id = "a", Name = "A",
            Detection = new AgentDetection { ProjectIndicators = { ".opencode" } }
        };

        Assert.False(this.service.IsAvailableInProject(plugin, this.projectDir));
    }

    [Fact]
    public void IsAvailableInProject_MissingFolder_ReturnsFalse()
    {
        var plugin = new Plugin
        {
            Id = "a", Name = "A",
            Detection = new AgentDetection { ProjectIndicators = { ".opencode" } }
        };

        Assert.False(this.service.IsAvailableInProject(plugin, Path.Combine(this.projectDir, "nope")));
    }

    [Fact]
    public void IsInstalledInSystem_WithinTtl_ReturnsCachedValue()
    {
        var marker = Path.Combine(this.projectDir, "cached-marker");
        File.WriteAllText(marker, string.Empty);
        var plugin = new Plugin
        {
            Id = "cache", Name = "C",
            Detection = new AgentDetection { SystemPaths = { marker } }
        };

        Assert.True(this.service.IsInstalledInSystem(plugin));

        File.Delete(marker);
        Assert.True(this.service.IsInstalledInSystem(plugin)); // served from cache

        this.time.Advance(AgentDetectionService.DefaultTtl + TimeSpan.FromMinutes(1));
        Assert.False(this.service.IsInstalledInSystem(plugin)); // TTL expired -> re-checked
    }

    [Fact]
    public void Invalidate_ForcesReCheck()
    {
        var marker = Path.Combine(this.projectDir, "invalidate-marker");
        File.WriteAllText(marker, string.Empty);
        var plugin = new Plugin
        {
            Id = "inval", Name = "I",
            Detection = new AgentDetection { SystemPaths = { marker } }
        };

        Assert.True(this.service.IsInstalledInSystem(plugin));

        File.Delete(marker);
        Assert.True(this.service.IsInstalledInSystem(plugin)); // served from cache

        this.service.Invalidate();
        Assert.False(this.service.IsInstalledInSystem(plugin)); // re-checked after invalidation
    }

    [Fact]
    public void IsInstalledInSystem_RespectsConfiguredTtl()
    {
        new PreferencesStore(this.config).Update(preferences => preferences.AgentProbeTtlMinutes = 1);
        var marker = Path.Combine(this.projectDir, "short-ttl-marker");
        File.WriteAllText(marker, string.Empty);
        var plugin = new Plugin
        {
            Id = "short", Name = "S",
            Detection = new AgentDetection { SystemPaths = { marker } }
        };

        Assert.True(this.service.IsInstalledInSystem(plugin));
        File.Delete(marker);

        this.time.Advance(TimeSpan.FromMinutes(2));
        Assert.False(this.service.IsInstalledInSystem(plugin));
    }
}
