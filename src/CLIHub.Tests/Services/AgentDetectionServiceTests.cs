using CLIHub.Core.Models;
using CLIHub.Core.Services;

namespace CLIHub.Tests.Services;

public class AgentDetectionServiceTests : IDisposable
{
    private readonly string _projectDir;
    private readonly AgentDetectionService _service = new();

    public AgentDetectionServiceTests()
    {
        _projectDir = Path.Combine(Path.GetTempPath(), "clihub-detect-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_projectDir);
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
}
