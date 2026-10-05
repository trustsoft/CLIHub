namespace CLIHub.Tests.Services;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;

/// <summary>
///   In-memory IConfigService for tests; no file system access.
/// </summary>
public class FakeConfigService : IConfigService
{
    private ConfigurationSnapshot _snapshot;

    public FakeConfigService(AppConfig? initial = null)
    {
        _snapshot = ConfigurationSnapshot.From(initial ?? new AppConfig());
    }

    public int SaveCount { get; private set; }

    public ConfigurationSnapshot Load() => ConfigurationSnapshot.From(_snapshot.ToAppConfig());

    public void Save(ConfigurationSnapshot snapshot)
    {
        _snapshot = ConfigurationSnapshot.From(snapshot.ToAppConfig());
        SaveCount++;
    }

    public void Flush()
    {
    }

    public void Dispose()
    {
    }
}

