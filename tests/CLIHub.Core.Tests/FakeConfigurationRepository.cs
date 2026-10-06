namespace CLIHub.Tests.Services;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;

/// <summary>
///   In-memory configuration repository for tests; no file system access.
/// </summary>
public sealed class FakeConfigurationRepository : IConfigurationRepository
{
    private readonly object _gate = new();
    private ConfigurationSnapshot _snapshot;

    public FakeConfigurationRepository(AppConfig? initial = null)
    {
        _snapshot = ConfigurationSnapshot.From(initial ?? new AppConfig());
    }

    public int UpdateCount { get; private set; }

    public ConfigurationSnapshot Read()
    {
        lock (_gate)
        {
            return ConfigurationSnapshot.From(_snapshot.ToAppConfig());
        }
    }

    public void Update(Action<ConfigurationSnapshot> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        lock (_gate)
        {
            var updated = ConfigurationSnapshot.From(_snapshot.ToAppConfig());
            update(updated);
            _snapshot = ConfigurationSnapshot.From(updated.ToAppConfig());
            UpdateCount++;
        }
    }

    public void Flush()
    {
    }

    public void Dispose()
    {
    }
}

