namespace CLIHub.Tests.Support;

using CLIHub.Core.Plugins;

/// <summary>
///   In-memory <see cref="IPluginRegistry" /> for view-model tests: hands out the
///   configured document and records saves, so lifecycle actions can be asserted against the
///   persisted state without touching the file system.
/// </summary>
public sealed class InMemoryPluginRegistry : IPluginRegistry
{
    /// <summary> The document returned by <see cref="Load" />; replaced by <see cref="Save" />. </summary>
    public PluginRegistryDocument Document { get; set; } = PluginRegistryDocument.Empty();

    /// <summary> How many times <see cref="Save" /> was called. </summary>
    public int SaveCount { get; private set; }

    /// <inheritdoc />
    public PluginRegistryDocument Load() => this.Document;

    /// <inheritdoc />
    public void Save(PluginRegistryDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        this.SaveCount++;
        this.Document = document;
    }
}
