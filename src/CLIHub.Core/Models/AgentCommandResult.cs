namespace CLIHub.Core.Models;

/// <summary>
///   Outcome of executing an agent command.
/// </summary>
/// <param name="Success"> Whether the command succeeded. </param>
/// <param name="Output"> Captured output when applicable (for example, the version). </param>
/// <param name="Error"> Error message when the command failed. </param>
public sealed record AgentCommandResult(bool Success, string? Output, string? Error);
