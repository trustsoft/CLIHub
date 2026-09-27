using CLIHub.Core.Models;

namespace CLIHub.Windows;

/// <summary>
/// Display item for an agent in the main window.
/// </summary>
public sealed class AgentItem
{
    public required Plugin Plugin { get; init; }
    public required string Name { get; init; }
    public required string Status { get; init; }
    public string? LogoPath { get; init; }
}
