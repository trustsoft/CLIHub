namespace CLIHub.Core.Tests.Fakes;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;

/// <summary>
///   In-memory configuration repository for tests; no file system access.
/// </summary>
public sealed class FakeConfigurationRepository : IConfigurationRepository
{
    private readonly object gate = new();
    private ConfigurationSnapshot snapshot;

    public FakeConfigurationRepository(AppConfig? initial = null)
    {
        this.snapshot = ConfigurationSnapshot.From(initial ?? new AppConfig());
    }

    public int UpdateCount { get; private set; }

    public ConfigurationSnapshot Read()
    {
        lock (this.gate)
        {
            return ConfigurationSnapshot.From(this.snapshot.ToAppConfig());
        }
    }

    public void Update(Action<ConfigurationSnapshot> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        lock (this.gate)
        {
            var updated = ConfigurationSnapshot.From(this.snapshot.ToAppConfig());
            update(updated);
            this.snapshot = ConfigurationSnapshot.From(updated.ToAppConfig());
            this.UpdateCount++;
        }
    }

    public void Flush()
    {
    }

    public void Dispose()
    {
    }
}
