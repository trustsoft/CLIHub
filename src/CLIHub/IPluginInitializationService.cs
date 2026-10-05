namespace CLIHub;

/// <summary>
///   Initializes the plugin catalog during application startup.
/// </summary>
public interface IPluginInitializationService
{
    /// <summary>
    ///   Seeds built-in plugins when needed, then loads the plugin catalog.
    /// </summary>
    void Initialize();
}
