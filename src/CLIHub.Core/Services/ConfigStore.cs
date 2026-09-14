using System.Text.Json;
using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;

namespace CLIHub.Core.Services;

public sealed class ConfigStore
{
    public const string FileName = "config.json";
    public const string BackupSuffix = ".bak";
    public const string TempSuffix = ".tmp";

    private readonly IFileSystem _fileSystem;
    private readonly IPathProvider _paths;

    public ConfigStore(IFileSystem fileSystem, IPathProvider paths)
    {
        _fileSystem = fileSystem;
        _paths = paths;
    }

    public string Path => System.IO.Path.Combine(_paths.ConfigDirectory, FileName);

    public string BackupPath => Path + BackupSuffix;

    public Config Load()
    {
        if (!_fileSystem.FileExists(Path))
        {
            return new Config();
        }

        return TryDeserialize(_fileSystem.ReadAllText(Path)) ?? new Config();
    }

    public void Save(Config config)
    {
        if (_fileSystem.FileExists(Path))
        {
            var contents = _fileSystem.ReadAllText(Path);
            if (TryDeserialize(contents) is null)
            {
                _fileSystem.WriteAllText(BackupPath, contents);
            }
        }

        var tempPath = Path + TempSuffix;
        _fileSystem.WriteAllText(tempPath, JsonSerializer.Serialize(config, CoreJson.Options));
        _fileSystem.Move(tempPath, Path, overwrite: true);
    }

    private static Config? TryDeserialize(string text)
    {
        try
        {
            return JsonSerializer.Deserialize<Config>(text, CoreJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
