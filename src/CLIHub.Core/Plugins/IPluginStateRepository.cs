namespace CLIHub.Core.Plugins;

/// <summary>
///   Loads and persists the plugin lifecycle state document at
///   <c> %APPDATA%\CLIHub\plugin-state.json </c>.
/// </summary>
public interface IPluginStateRepository
{
    /// <summary>
    ///   Loads the plugin state document. A missing or corrupt state file yields an empty
    ///   document (all plugin origins unknown) and never throws.
    /// </summary>
    /// <returns> The loaded state document. </returns>
    PluginStateDocument Load();

    /// <summary>
    ///   Persists the plugin state document atomically through a temp-file-then-rename write.
    /// </summary>
    /// <param name="document"> The state document to persist. </param>
    void Save(PluginStateDocument document);
}
