using System.Text.Json;
using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;

namespace CLIHub.Core.Services;

public sealed class ConfigStore
{
    public const string FileName = "config.json";

    private readonly IFileSystem _fileSystem;
    private readonly IPathProvider _paths;

    public ConfigStore(IFileSystem fileSystem, IPathProvider paths)
    {
        _fileSystem = fileSystem;
        _paths = paths;
    }

    public string Path => System.IO.Path.Combine(_paths.ConfigDirectory, FileName);

    public Config Load()
    {
        if (!_fileSystem.FileExists(Path))
        {
            return new Config();
        }

        try
        {
            return JsonSerializer.Deserialize<Config>(_fileSystem.ReadAllText(Path), CoreJson.Options) ?? new Config();
        }
        catch (JsonException)
        {
            return new Config();
        }
    }
}
