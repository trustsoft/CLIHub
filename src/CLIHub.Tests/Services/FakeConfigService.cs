using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;

namespace CLIHub.Tests.Services;

/// <summary>
/// In-memory IConfigService for tests; no file system access.
/// </summary>
public class FakeConfigService : IConfigService
{
    private AppConfig _config;

    public FakeConfigService(AppConfig? initial = null)
    {
        _config = initial ?? new AppConfig();
    }

    public string ConfigFilePath => "(in-memory)";

    public int SaveCount { get; private set; }

    public AppConfig Load() => _config;

    public void Save(AppConfig config)
    {
        _config = config;
        SaveCount++;
    }

    public Project? GetCurrentProject()
    {
        if (_config.CurrentProjectId == null)
            return null;
        return _config.Projects.FirstOrDefault(p => p.Id == _config.CurrentProjectId);
    }

    public void SetCurrentProject(string? projectId)
    {
        _config.CurrentProjectId = projectId;
    }
}
