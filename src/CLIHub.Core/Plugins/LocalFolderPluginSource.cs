namespace CLIHub.Core.Plugins;

/// <summary>
///   Discovers plugin descriptors from local plugin folders.
/// </summary>
public sealed class LocalFolderPluginSource : IPluginDescriptorSource
{
    private readonly string pluginsPath;
    private readonly IPluginDescriptorReader descriptorReader;

    /// <summary>
    ///   Creates a local folder plugin source.
    /// </summary>
    /// <param name="pluginsPath"> The root folder containing plugin directories. </param>
    /// <param name="descriptorReader"> The descriptor reader. </param>
    public LocalFolderPluginSource(string pluginsPath, IPluginDescriptorReader descriptorReader)
    {
        this.pluginsPath = pluginsPath ?? throw new ArgumentNullException(nameof(pluginsPath));
        this.descriptorReader = descriptorReader ?? throw new ArgumentNullException(nameof(descriptorReader));
    }

    /// <inheritdoc />
    public IEnumerable<PluginDescriptorSourceEntry> EnumerateDescriptors()
    {
        if (!Directory.Exists(this.pluginsPath))
        {
            Directory.CreateDirectory(this.pluginsPath);
            yield break;
        }

        var pluginDirectories = Directory.GetDirectories(this.pluginsPath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(path => path, StringComparer.Ordinal)
            .ToArray();

        foreach (var pluginDirectory in pluginDirectories)
        {
            PluginDescriptorSourceEntry entry;
            try
            {
                var descriptor = this.descriptorReader.Read(pluginDirectory);
                entry = new PluginDescriptorSourceEntry(pluginDirectory, descriptor.Plugin, descriptor.Error);
            }
            catch (Exception ex)
            {
                entry = new PluginDescriptorSourceEntry(pluginDirectory, null, ex);
            }

            yield return entry;
        }
    }
}
