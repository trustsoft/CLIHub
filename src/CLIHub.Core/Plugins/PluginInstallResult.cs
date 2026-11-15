namespace CLIHub.Core.Plugins;

/// <summary>
///   The outcome of installing a plugin package.
/// </summary>
/// <param name="Success"> Whether the plugin was installed. </param>
/// <param name="InstalledPluginId"> The installed plugin id on success, or null. </param>
/// <param name="Errors"> The errors that made the installation fail; empty on success. </param>
public sealed record PluginInstallResult(
    bool Success,
    string? InstalledPluginId,
    IReadOnlyList<string> Errors)
{
    /// <summary>
    ///   The non-fatal degradations of a successful install, such as an unsaved plugin state
    ///   document; empty when the install completed cleanly or failed.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>
    ///   Creates a successful result for the installed plugin id.
    /// </summary>
    /// <param name="pluginId"> The installed plugin id. </param>
    /// <param name="warnings">
    ///   The non-fatal degradations of the install, or null when there were none.
    /// </param>
    /// <returns> The successful result. </returns>
    public static PluginInstallResult Succeeded(string pluginId, IReadOnlyList<string>? warnings = null) =>
        new(true, pluginId, []) { Warnings = warnings ?? [] };

    /// <summary>
    ///   Creates a failed result carrying the errors.
    /// </summary>
    /// <param name="errors"> The failure errors. </param>
    /// <returns> The failed result. </returns>
    public static PluginInstallResult Failed(IReadOnlyList<string> errors) =>
        new(false, null, errors) { Warnings = [] };
}
