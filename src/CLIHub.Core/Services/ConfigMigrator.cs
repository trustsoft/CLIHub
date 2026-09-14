using System.Text.Json;
using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;

namespace CLIHub.Core.Services;

public sealed class ConfigMigrator
{
    public const string LegacyFileName = "config.json";
    public const string MigratedSuffix = ".migrated";

    private readonly IFileSystem _fileSystem;
    private readonly IPathProvider _paths;
    private readonly JsonDocumentStore<SettingsDocument> _settingsStore;
    private readonly JsonDocumentStore<ProjectsDocument> _projectsStore;
    private readonly JsonDocumentStore<AgentsDocument> _agentsStore;

    public ConfigMigrator(IFileSystem fileSystem, IPathProvider paths)
    {
        _fileSystem = fileSystem;
        _paths = paths;
        _settingsStore = new JsonDocumentStore<SettingsDocument>(fileSystem, paths, SettingsDocument.FileName);
        _projectsStore = new JsonDocumentStore<ProjectsDocument>(fileSystem, paths, ProjectsDocument.FileName);
        _agentsStore = new JsonDocumentStore<AgentsDocument>(fileSystem, paths, AgentsDocument.FileName);
    }

    public string LegacyPath => System.IO.Path.Combine(_paths.ConfigDirectory, LegacyFileName);

    public void MigrateIfNeeded()
    {
        if (_fileSystem.FileExists(_settingsStore.Path) || !_fileSystem.FileExists(LegacyPath))
        {
            return;
        }

        Config? legacy;
        try
        {
            legacy = JsonSerializer.Deserialize<Config>(_fileSystem.ReadAllText(LegacyPath), CoreJson.Options);
        }
        catch (JsonException)
        {
            return;
        }

        if (legacy is null)
        {
            return;
        }

        _settingsStore.Save(new SettingsDocument
        {
            Runtime = legacy.Runtime,
            Hotkey = legacy.Hotkey,
            Probe = legacy.Probe,
            Update = legacy.Update,
            AdditionalData = legacy.AdditionalData
        });
        _projectsStore.Save(new ProjectsDocument { Projects = legacy.Projects });
        _agentsStore.Save(new AgentsDocument { Agents = legacy.Agents });

        _fileSystem.Move(LegacyPath, LegacyPath + MigratedSuffix, overwrite: true);
    }
}
