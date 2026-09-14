namespace CLIHub.Core.Abstractions;

public interface IProcessRunner
{
    bool CommandExists(string command);

    void StartDetached(string fileName, string arguments, string workingDirectory);
}
