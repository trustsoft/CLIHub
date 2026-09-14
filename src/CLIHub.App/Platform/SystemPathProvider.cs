using CLIHub.Core.Abstractions;

namespace CLIHub.App.Platform;

public sealed class SystemPathProvider : IPathProvider
{
    public string ConfigDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CLIHub");

    public string ApplicationDirectory { get; } = AppContext.BaseDirectory;
}
