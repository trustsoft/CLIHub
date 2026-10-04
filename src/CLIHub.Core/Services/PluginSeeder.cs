namespace CLIHub.Core.Services;

using CLIHub.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

/// <summary>
///   Seeds the plugins folder from descriptors and logos embedded in this assembly.
/// </summary>
public class PluginSeeder : IPluginSeeder
{
    private const string ResourceMarker = ".SeedPlugins.";
    private const string DescriptorSuffix = ".plugin.json";
    private const string LogoSuffix = ".logo.png";

    private readonly ILogger<PluginSeeder> _logger;
    private readonly string _pluginsPath;

    /// <summary>
    ///   Creates the seeder for the given plugins root.
    /// </summary>
    /// <param name="logger"> The logger. </param>
    /// <param name="pluginsPath"> Overrides the plugins root; defaults to <c>%APPDATA%\CLIHub\plugins</c>. </param>
    public PluginSeeder(ILogger<PluginSeeder> logger, string? pluginsPath = null)
    {
        _logger = logger;
        _pluginsPath = pluginsPath ?? AppPaths.PluginsDirectory;
    }

    /// <inheritdoc />
    public int SeedIfEmpty()
    {
        try
        {
            if (HasAnyPlugin(_pluginsPath))
            {
                return 0;
            }

            Directory.CreateDirectory(_pluginsPath);

            var assembly = typeof(PluginSeeder).Assembly;
            var resources = assembly.GetManifestResourceNames()
                .Where(n => n.Contains(ResourceMarker))
                .ToList();

            // Embedded-resource names sanitize characters (for example '-' becomes '_'),
            // so the plugin id is taken from the descriptor content. The sanitized key
            // links a logo resource back to its descriptor.
            var idByKey = new Dictionary<string, string>(StringComparer.Ordinal);
            var descriptorResources = new List<(string Key, string ResourceName, string Json)>();

            foreach (var name in resources.Where(n => n.EndsWith(DescriptorSuffix, StringComparison.Ordinal)))
            {
                using var stream = assembly.GetManifestResourceStream(name);
                if (stream == null)
                {
                    continue;
                }

                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                var id = ReadPluginId(json);

                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("Skipped seed descriptor {Resource}: missing id", name);
                    continue;
                }

                var key = ExtractKey(name, DescriptorSuffix);
                idByKey[key] = id;
                descriptorResources.Add((key, name, json));
            }

            var descriptorsWritten = 0;
            foreach (var (_, _, json) in descriptorResources)
            {
                var id = ReadPluginId(json)!; // validated above
                var directory = Path.Combine(_pluginsPath, id);
                if (File.Exists(Path.Combine(directory, "plugin.json")))
                {
                    continue;
                }

                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "plugin.json"), json);
                descriptorsWritten++;
                _logger.LogInformation("Seeded plugin descriptor {PluginId}", id);
            }

            foreach (var name in resources.Where(n => n.EndsWith(LogoSuffix, StringComparison.Ordinal)))
            {
                var key = ExtractKey(name, LogoSuffix);
                if (!idByKey.TryGetValue(key, out var id))
                {
                    _logger.LogWarning("Skipped seed logo {Resource}: no matching descriptor", name);
                    continue;
                }

                var directory = Path.Combine(_pluginsPath, id);
                var target = Path.Combine(directory, "logo.png");
                if (File.Exists(target))
                {
                    continue;
                }

                using var source = assembly.GetManifestResourceStream(name);
                if (source == null)
                {
                    continue;
                }

                Directory.CreateDirectory(directory);
                using var destination = File.Create(target);
                source.CopyTo(destination);
                _logger.LogInformation("Seeded logo for {PluginId}", id);
            }

            if (descriptorsWritten > 0)
            {
                _logger.LogInformation("Seeded {Count} plugin descriptor(s)", descriptorsWritten);
            }

            return descriptorsWritten;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Plugin seeding failed; continuing without seeding");
            return 0;
        }
    }

    private static bool HasAnyPlugin(string pluginsPath) =>
        Directory.Exists(pluginsPath) &&
        Directory.GetDirectories(pluginsPath).Any(d => File.Exists(Path.Combine(d, "plugin.json")));

    private static string ExtractKey(string resourceName, string suffix)
    {
        var start = resourceName.IndexOf(ResourceMarker, StringComparison.Ordinal) + ResourceMarker.Length;
        return resourceName.Substring(start, resourceName.Length - start - suffix.Length);
    }

    private static string? ReadPluginId(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("id", out var id))
            {
                return id.GetString();
            }
        }
        catch
        {
            // malformed seed resource
        }

        return null;
    }
}
