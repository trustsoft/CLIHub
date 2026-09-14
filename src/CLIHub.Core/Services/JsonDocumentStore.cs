using System.Text.Json;
using CLIHub.Core.Abstractions;

namespace CLIHub.Core.Services;

public sealed class JsonDocumentStore<T> where T : class, new()
{
    public const string BackupSuffix = ".bak";
    public const string TempSuffix = ".tmp";

    private readonly IFileSystem _fileSystem;
    private readonly IPathProvider _paths;
    private readonly string _fileName;

    public JsonDocumentStore(IFileSystem fileSystem, IPathProvider paths, string fileName)
    {
        _fileSystem = fileSystem;
        _paths = paths;
        _fileName = fileName;
    }

    public string Path => System.IO.Path.Combine(_paths.ConfigDirectory, _fileName);

    public string BackupPath => Path + BackupSuffix;

    public T Load()
    {
        if (!_fileSystem.FileExists(Path))
        {
            return new T();
        }

        return TryDeserialize(_fileSystem.ReadAllText(Path)) ?? new T();
    }

    public void Save(T document)
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
        _fileSystem.WriteAllText(tempPath, JsonSerializer.Serialize(document, CoreJson.Options));
        _fileSystem.Move(tempPath, Path, overwrite: true);
    }

    private static T? TryDeserialize(string text)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(text, CoreJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
