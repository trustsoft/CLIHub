namespace CLIHub.Core.Plugins;

/// <summary>
///   Loads and persists the plugin registry document at
///   <c> %APPDATA%\CLIHub\registry.json </c>.
/// </summary>
public interface IPluginRegistry
{
    /// <summary>
    ///   Loads the plugin registry document. A missing or corrupt registry yields an empty
    ///   document (all plugin origins unknown) and never throws; when the registry is absent,
    ///   a legacy <c> plugin-state.json </c> is adopted into it once.
    /// </summary>
    /// <returns> The loaded registry document. </returns>
    PluginRegistryDocument Load();

    /// <summary>
    ///   Attempts to persist the plugin registry document atomically through a
    ///   temp-file-then-rename write. A failed write is logged as a warning and reported through
    ///   the return value instead of throwing, so callers decide how to present the degradation.
    /// </summary>
    /// <param name="document"> The registry document to persist. </param>
    /// <returns> <see langword="true" /> when the document was written; otherwise <see langword="false" />. </returns>
    bool TrySave(PluginRegistryDocument document);
}
