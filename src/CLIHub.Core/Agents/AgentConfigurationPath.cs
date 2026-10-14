namespace CLIHub.Core.Agents;

using CLIHub.Core.Models;

/// <summary>
///   Resolves the on-disk configuration file of an agent plugin.
/// </summary>
public static class AgentConfigurationPath
{
    /// <summary>
    ///   The plugin descriptor file name.
    /// </summary>
    public const string DescriptorFileName = "plugin.json";

    /// <summary>
    ///   Returns the path to the agent's <c> plugin.json </c>, or null when the plugin directory is unknown.
    /// </summary>
    /// <param name="plugin"> The agent plugin. </param>
    /// <returns> The descriptor path, or null. </returns>
    public static string? For(Plugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        return string.IsNullOrWhiteSpace(plugin.PluginDirectory)
            ? null
            : Path.Combine(plugin.PluginDirectory, DescriptorFileName);
    }
}
