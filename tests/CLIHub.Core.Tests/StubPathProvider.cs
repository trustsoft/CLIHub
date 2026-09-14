using CLIHub.Core.Abstractions;

namespace CLIHub.Core.Tests;

internal sealed class StubPathProvider : IPathProvider
{
    public StubPathProvider(string configDirectory, string applicationDirectory)
    {
        ConfigDirectory = configDirectory;
        ApplicationDirectory = applicationDirectory;
    }

    public string ConfigDirectory { get; }

    public string ApplicationDirectory { get; }
}
