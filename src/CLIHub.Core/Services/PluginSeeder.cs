using CLIHub.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CLIHub.Core.Services;

/// <summary>
/// Seeds the plugins folder from descriptors embedded in this assembly.
/// </summary>
public class PluginSeeder : IPluginSeeder
{
    private const string ResourceMarker = ".SeedPlugins.";
    private const string ResourceSuffix = ".plugin.json";

    private readonly ILogger<PluginSeeder> _logger;
    private readonly string _pluginsPath;

    public PluginSeeder(ILogger<PluginSeeder> logger, string? pluginsPath = null)
    {
        _logger = logger;
        _pluginsPath = pluginsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CLIHub",
            "plugins");
    }

    public int SeedIfEmpty()
    {
        try
        {
            if (HasAnyPlugin(_pluginsPath))
                return 0;

            Directory.CreateDirectory(_pluginsPath);

            var assembly = typeof(PluginSeeder).Assembly;
            var written = 0;

            foreach (var resourceName in assembly.GetManifestResourceNames()
                         .Where(n => n.Contains(ResourceMarker) && n.EndsWith(ResourceSuffix, StringComparison.Ordinal)))
            {
                using var stream = assembly.GetManifestResourceStream(resourceName)
                                   ?? throw new InvalidOperationException($"Missing resource {resourceName}");
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();

                // Embedded-resource names sanitize characters (for example '-' becomes '_'),
                // so the plugin id is taken from the descriptor content instead.
                var id = ReadPluginId(json);
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("Skipped seed resource {Resource}: missing id", resourceName);
                    continue;
                }

                var directory = Path.Combine(_pluginsPath, id);
                var target = Path.Combine(directory, "plugin.json");

                if (File.Exists(target))
                    continue;

                Directory.CreateDirectory(directory);
                File.WriteAllText(target, json);

                written++;
                _logger.LogInformation("Seeded plugin descriptor {PluginId}", id);
            }

            if (written > 0)
                _logger.LogInformation("Seeded {Count} plugin descriptor(s)", written);

            return written;
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

    private static string? ReadPluginId(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("id", out var id))
                return id.GetString();
        }
        catch
        {
            // malformed seed resource
        }

        return null;
    }
}
