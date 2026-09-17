using System.Text.Json;
using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;

namespace CLIHub.Core.Services;

public sealed record PluginLoadResult(IReadOnlyList<AgentPlugin> Agents, IReadOnlyList<string> Warnings);

public sealed class PluginLoader
{
    public const int SupportedSchemaVersion = 1;

    private readonly IFileSystem _fileSystem;
    private readonly IPathProvider _paths;

    public PluginLoader(IFileSystem fileSystem, IPathProvider paths)
    {
        _fileSystem = fileSystem;
        _paths = paths;
    }

    public PluginLoadResult Load()
    {
        var agents = new List<AgentPlugin>();
        var warnings = new List<string>();
        var root = Path.Combine(_paths.ApplicationDirectory, "plugins", "agents");

        if (!_fileSystem.DirectoryExists(root))
        {
            return new PluginLoadResult(agents, warnings);
        }

        foreach (var directory in _fileSystem.EnumerateDirectories(root).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
        {
            var folder = Path.GetFileName(directory);
            var manifestPath = Path.Combine(directory, "agent.json");

            if (!_fileSystem.FileExists(manifestPath))
            {
                warnings.Add($"Plugin '{folder}': agent.json is missing — skipped.");
                continue;
            }

            AgentManifest? manifest;
            try
            {
                manifest = JsonSerializer.Deserialize<AgentManifest>(_fileSystem.ReadAllText(manifestPath), CoreJson.Options);
            }
            catch (JsonException)
            {
                warnings.Add($"Plugin '{folder}': agent.json cannot be parsed — skipped.");
                continue;
            }

            if (manifest is null)
            {
                warnings.Add($"Plugin '{folder}': agent.json is empty — skipped.");
                continue;
            }

            if (manifest.SchemaVersion != SupportedSchemaVersion)
            {
                warnings.Add(
                    $"Plugin '{folder}': unsupported schemaVersion {manifest.SchemaVersion} — skipped.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(manifest.Id) || string.IsNullOrWhiteSpace(manifest.Name))
            {
                warnings.Add($"Plugin '{folder}': required id/name are not set — skipped.");
                continue;
            }

            agents.Add(new AgentPlugin(manifest, directory));
        }

        return new PluginLoadResult(agents, warnings);
    }
}
