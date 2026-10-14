namespace CLIHub.Core.Windows.Tests;

using CLIHub.Core.Windows;

/// <summary>
///   In-memory <see cref="IStartupRegistry"/> for tests.
/// </summary>
public sealed class FakeStartupRegistry : IStartupRegistry
{
    public Dictionary<string, string> Values { get; } = new(StringComparer.Ordinal);

    public bool ThrowOnAccess { get; set; }

    public string? GetValue(string name)
    {
        this.ThrowIfRequested();
        return this.Values.TryGetValue(name, out var value) ? value : null;
    }

    public void SetValue(string name, string value)
    {
        this.ThrowIfRequested();
        this.Values[name] = value;
    }

    public void DeleteValue(string name)
    {
        this.ThrowIfRequested();
        this.Values.Remove(name);
    }

    private void ThrowIfRequested()
    {
        if (this.ThrowOnAccess)
        {
            throw new InvalidOperationException("registry unavailable");
        }
    }
}
