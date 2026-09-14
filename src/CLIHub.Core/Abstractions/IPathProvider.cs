namespace CLIHub.Core.Abstractions;

public interface IPathProvider
{
    string ConfigDirectory { get; }

    string ApplicationDirectory { get; }
}
