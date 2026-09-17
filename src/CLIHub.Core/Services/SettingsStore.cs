using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;

namespace CLIHub.Core.Services;

public sealed record SettingsSaveResult(bool Saved, IReadOnlyList<string> Errors)
{
    public static SettingsSaveResult Ok() => new(true, Array.Empty<string>());

    public static SettingsSaveResult Fail(IReadOnlyList<string> errors) => new(false, errors);
}

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

    public SettingsSaveResult Save(SettingsDocument document)
    {
        var errors = Validate(document);
        if (errors.Count > 0)
        {
            return SettingsSaveResult.Fail(errors);
        }

        _document = document;
        _store.Save(document);
        return SettingsSaveResult.Ok();
    }

    public void Reload() => _document = _store.Load();

    private static List<string> Validate(SettingsDocument document)
    {
        var errors = new List<string>();

        if (document.Probe.TtlMinutes is <= 0)
        {
            errors.Add("Probe TTL must be a positive number.");
        }

        if (document.Probe.TimeoutSeconds is <= 0)
        {
            errors.Add("Probe timeout must be a positive number.");
        }

        if (!HotkeyParser.TryParse(document.Hotkey, out _, out _, out var hotkeyError))
        {
            errors.Add(hotkeyError ?? "Invalid hotkey combination.");
        }

        return errors;
    }
}
