namespace CLIHub.Core.Tests.Fakes;

using CLIHub.Core.Models;
using CLIHub.Core.Infrastructure.Processes;

/// <summary>
///   Fake runtime executable locator with a configurable path per kind (null by default).
/// </summary>
public sealed class FakeRuntimeExecutableLocator : IRuntimeExecutableLocator
{
    private readonly Dictionary<RuntimeKind, string> paths = new();

    /// <summary>
    ///   The kinds asked for, in call order.
    /// </summary>
    public List<RuntimeKind> RequestedKinds { get; } = new();

    /// <summary>
    ///   Configures the path returned for a kind.
    /// </summary>
    /// <param name="kind"> The runtime kind. </param>
    /// <param name="path"> The absolute executable path. </param>
    public void SetPath(RuntimeKind kind, string path) => this.paths[kind] = path;

    /// <inheritdoc />
    public string? Locate(RuntimeKind kind)
    {
        this.RequestedKinds.Add(kind);
        return this.paths.TryGetValue(kind, out var path) ? path : null;
    }
}
