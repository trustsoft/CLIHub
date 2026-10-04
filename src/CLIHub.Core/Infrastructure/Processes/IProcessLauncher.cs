namespace CLIHub.Core.Interfaces;

using CLIHub.Core.Models;

/// <summary>
///   Launches processes for AI agent CLI tools.
/// </summary>
public interface IProcessLauncher : IInteractiveProcessRunner, IProcessOutputRunner
{
}
