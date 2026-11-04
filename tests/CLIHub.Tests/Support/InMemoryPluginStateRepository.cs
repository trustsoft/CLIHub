namespace CLIHub.Tests.Support;

using CLIHub.Core.Plugins;

/// <summary>
///   In-memory <see cref="IPluginStateRepository" /> for view-model tests: hands out the
///   configured document and records saves, so lifecycle actions can be asserted against the
///   persisted state without touching the file system.
/// </summary>
public sealed class InMemoryPluginStateRepository : IPluginStateRepository
{
    /// <summary> The document returned by <see cref="Load" />; replaced by <see cref="Save" />. </summary>
    public PluginStateDocument Document { get; set; } = PluginStateDocument.Empty();

    /// <summary> How many times <see cref="Save" /> was called. </summary>
    public int SaveCount { get; private set; }

    /// <inheritdoc />
    public PluginStateDocument Load() => this.Document;

    /// <inheritdoc />
    public void Save(PluginStateDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        this.SaveCount++;
        this.Document = document;
    }
}
