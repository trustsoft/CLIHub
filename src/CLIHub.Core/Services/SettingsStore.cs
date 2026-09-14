using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;

namespace CLIHub.Core.Services;

public sealed class SettingsStore
{
    private readonly JsonDocumentStore<SettingsDocument> _store;
    private SettingsDocument _document;

    public SettingsStore(IFileSystem fileSystem, IPathProvider paths)
    {
        _store = new JsonDocumentStore<SettingsDocument>(fileSystem, paths, SettingsDocument.FileName);
        _document = _store.Load();
    }

    public string Path => _store.Path;

    public string? Runtime => _document.Runtime;

    public string Hotkey => _document.Hotkey;

    public ProbeConfig Probe => _document.Probe;

    public UpdateConfig Update => _document.Update;

    public void Reload() => _document = _store.Load();
}
