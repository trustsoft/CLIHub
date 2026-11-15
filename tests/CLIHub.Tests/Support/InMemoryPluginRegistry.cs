namespace CLIHub.Tests.Support;

using CLIHub.Core.Plugins;

/// <summary>
///   In-memory <see cref="IPluginRegistry" /> for view-model tests: hands out the
///   configured document and records save attempts, so lifecycle actions can be asserted against
///   the persisted state without touching the file system.
/// </summary>
public sealed class InMemoryPluginRegistry : IPluginRegistry
{
    /// <summary>
    ///   The document returned by <see cref="Load" />; replaced by a successful
    ///   <see cref="TrySave" />.
    /// </summary>
    public PluginRegistryDocument Document { get; set; } = PluginRegistryDocument.Empty();

    /// <summary> How many times <see cref="TrySave" /> was called, successful or not. </summary>
    public int SaveCount { get; private set; }

    /// <summary>
    ///   Whether <see cref="TrySave" /> reports success. Set to false to simulate an unwritable
    ///   registry.
    /// </summary>
    public bool TrySaveSucceeds { get; set; } = true;

    /// <inheritdoc />
    public PluginRegistryDocument Load() => this.Document;

    /// <inheritdoc />
    public bool TrySave(PluginRegistryDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        this.SaveCount++;

        if (!this.TrySaveSucceeds)
        {
            return false;
        }

        this.Document = document;
        return true;
    }
}
