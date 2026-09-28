namespace CLIHub.Tests.Services;

using CLIHub.Core.Models;
using CLIHub.Core.Services;

public class AgentDetectionServiceTests : IDisposable
{
    private readonly string _projectDir;
    private readonly FakeConfigService _config = new();
    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
    private readonly AgentDetectionService _service;

    public AgentDetectionServiceTests()
    {
        _projectDir = Path.Combine(Path.GetTempPath(), "clihub-detect-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_projectDir);
        _service = new AgentDetectionService(_config, _time);
    }

    public void Dispose()
    {
        try { Directory.Delete(_projectDir, recursive: true); } catch { }
    }

    [Fact]
    public void IsInstalledInSystem_MarkerPresent_ReturnsTrue()
    {
        var marker = Path.Combine(_projectDir, "installed-marker");
        Directory.CreateDirectory(marker);
        var plugin = new Plugin
        {
            Id = "a", Name = "A",
            Detection = new AgentDetection { SystemPaths = { marker } }
        };

        Assert.True(_service.IsInstalledInSystem(plugin));
    }

    [Fact]
    public void IsInstalledInSystem_NoMarker_ReturnsFalse()
    {
        var plugin = new Plugin
        {
            Id = "a", Name = "A",
            Detection = new AgentDetection { SystemPaths = { Path.Combine(_projectDir, "missing") } }
        };

        Assert.False(_service.IsInstalledInSystem(plugin));
    }

    [Fact]
    public void IsInstalledInSystem_ExpandsEnvironmentVariables()
    {
        var plugin = new Plugin
        {
            Id = "a", Name = "A",
            Detection = new AgentDetection { SystemPaths = { "%TEMP%" } }
        };

        Assert.True(_service.IsInstalledInSystem(plugin));
    }

    [Fact]
    public void IsAvailableInProject_IndicatorPresent_ReturnsTrue()
    {
        Directory.CreateDirectory(Path.Combine(_projectDir, ".opencode"));
        var plugin = new Plugin
        {
            Id = "a", Name = "A",
            Detection = new AgentDetection { ProjectIndicators = { ".opencode" } }
        };

        Assert.True(_service.IsAvailableInProject(plugin, _projectDir));
    }

    [Fact]
    public void IsAvailableInProject_IndicatorMissing_ReturnsFalse()
    {
        var plugin = new Plugin
        {
            Id = "a", Name = "A",
            Detection = new AgentDetection { ProjectIndicators = { ".opencode" } }
        };

        Assert.False(_service.IsAvailableInProject(plugin, _projectDir));
    }

    [Fact]
    public void IsAvailableInProject_MissingFolder_ReturnsFalse()
    {
        var plugin = new Plugin
        {
            Id = "a", Name = "A",
            Detection = new AgentDetection { ProjectIndicators = { ".opencode" } }
        };

        Assert.False(_service.IsAvailableInProject(plugin, Path.Combine(_projectDir, "nope")));
    }

    [Fact]
    public void IsInstalledInSystem_WithinTtl_ReturnsCachedValue()
    {
        var marker = Path.Combine(_projectDir, "cached-marker");
        File.WriteAllText(marker, string.Empty);
        var plugin = new Plugin
        {
            Id = "cache", Name = "C",
            Detection = new AgentDetection { SystemPaths = { marker } }
        };

        Assert.True(_service.IsInstalledInSystem(plugin));

        File.Delete(marker);
        Assert.True(_service.IsInstalledInSystem(plugin)); // served from cache

        _time.Advance(AgentDetectionService.DefaultTtl + TimeSpan.FromMinutes(1));
        Assert.False(_service.IsInstalledInSystem(plugin)); // TTL expired -> re-checked
    }

    [Fact]
    public void IsInstalledInSystem_RespectsConfiguredTtl()
    {
        _config.Load().Preferences.AgentProbeTtlMinutes = 1;
        var marker = Path.Combine(_projectDir, "short-ttl-marker");
        File.WriteAllText(marker, string.Empty);
        var plugin = new Plugin
        {
            Id = "short", Name = "S",
            Detection = new AgentDetection { SystemPaths = { marker } }
        };

        Assert.True(_service.IsInstalledInSystem(plugin));
        File.Delete(marker);

        _time.Advance(TimeSpan.FromMinutes(2));
        Assert.False(_service.IsInstalledInSystem(plugin));
    }
}
