namespace CLIHub.Core.Plugins;

/// <summary>
///   One command declared by a plugin package, as shown to the user before installation.
/// </summary>
/// <param name="CommandKind"> The command kind name: "launch", "resume", "version", or "update". </param>
/// <param name="Executable"> The exact executable the command runs. </param>
/// <param name="Arguments"> The exact arguments the command passes, or null. </param>
public sealed record CommandPreview(
    string CommandKind,
    string Executable,
    string? Arguments);
