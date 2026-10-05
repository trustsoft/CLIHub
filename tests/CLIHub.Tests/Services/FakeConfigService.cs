namespace CLIHub.Tests.Services;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;

/// <summary>
///   In-memory IConfigService for tests; no file system access.
/// </summary>
public class FakeConfigService : IConfigService
{
    private AppConfig _config;

    public FakeConfigService(AppConfig? initial = null)
    {
        _config = initial ?? new AppConfig();
    }

    public int SaveCount { get; private set; }

    public AppConfig Load() => _config;

    public void Save(AppConfig config)
    {
        _config = config;
        SaveCount++;
    }

    public void Flush()
    {
    }

    public void Dispose()
    {
    }
}
