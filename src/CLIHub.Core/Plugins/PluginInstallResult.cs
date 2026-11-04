namespace CLIHub.Core.Plugins;

/// <summary>
///   The outcome of installing a plugin package.
/// </summary>
/// <param name="Success"> Whether the plugin was installed and activated. </param>
/// <param name="InstalledPluginId"> The installed plugin id on success, or null. </param>
/// <param name="Errors"> The errors that made the installation fail; empty on success. </param>
public sealed record PluginInstallResult(
    bool Success,
    string? InstalledPluginId,
    IReadOnlyList<string> Errors)
{
    /// <summary>
    ///   Creates a successful result for the installed plugin id.
    /// </summary>
    /// <param name="pluginId"> The installed plugin id. </param>
    /// <returns> The successful result. </returns>
    public static PluginInstallResult Succeeded(string pluginId) =>
        new(true, pluginId, []);

    /// <summary>
    ///   Creates a failed result carrying the errors.
    /// </summary>
    /// <param name="errors"> The failure errors. </param>
    /// <returns> The failed result. </returns>
    public static PluginInstallResult Failed(IReadOnlyList<string> errors) =>
        new(false, null, errors);
}
