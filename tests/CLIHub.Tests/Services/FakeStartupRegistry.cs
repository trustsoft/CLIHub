namespace CLIHub.Tests.Services;

using CLIHub.Core.Services;

/// <summary>
/// In-memory <see cref="IStartupRegistry"/> for tests.
/// </summary>
public sealed class FakeStartupRegistry : IStartupRegistry
{
    public Dictionary<string, string> Values { get; } = new(StringComparer.Ordinal);

    public bool ThrowOnAccess { get; set; }

    public string? GetValue(string name)
    {
        ThrowIfRequested();
        return Values.TryGetValue(name, out var value) ? value : null;
    }

    public void SetValue(string name, string value)
    {
        ThrowIfRequested();
        Values[name] = value;
    }

    public void DeleteValue(string name)
    {
        ThrowIfRequested();
        Values.Remove(name);
    }

    private void ThrowIfRequested()
    {
        if (ThrowOnAccess)
        {
            throw new InvalidOperationException("registry unavailable");
        }
    }
}
