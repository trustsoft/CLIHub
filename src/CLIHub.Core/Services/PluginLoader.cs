using System.Text.Json;
using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;

namespace CLIHub.Core.Services;

public sealed record PluginLoadResult(IReadOnlyList<AgentManifest> Agents, IReadOnlyList<string> Warnings);

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
        var agents = new List<AgentManifest>();
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
                warnings.Add($"Плагин '{folder}': отсутствует agent.json — пропущен.");
                continue;
            }

            AgentManifest? manifest;
            try
            {
                manifest = JsonSerializer.Deserialize<AgentManifest>(_fileSystem.ReadAllText(manifestPath), CoreJson.Options);
            }
            catch (JsonException)
            {
                warnings.Add($"Плагин '{folder}': agent.json не разбирается — пропущен.");
                continue;
            }

            if (manifest is null)
            {
                warnings.Add($"Плагин '{folder}': agent.json пуст — пропущен.");
                continue;
            }

            if (manifest.SchemaVersion != SupportedSchemaVersion)
            {
                warnings.Add(
                    $"Плагин '{folder}': неподдерживаемая schemaVersion {manifest.SchemaVersion} — пропущен.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(manifest.Id) || string.IsNullOrWhiteSpace(manifest.Name))
            {
                warnings.Add($"Плагин '{folder}': не заданы обязательные id/name — пропущен.");
                continue;
            }

            agents.Add(manifest);
        }

        return new PluginLoadResult(agents, warnings);
    }
}
