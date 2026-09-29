namespace CLIHub.ViewModels;

using CLIHub.Core.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>
/// Display item for an agent row in the launch window.
/// </summary>
public sealed class AgentItem : INotifyPropertyChanged
{
    private string _version = "…";

    public required Plugin Plugin { get; init; }
    public required string Name { get; init; }

    /// <summary>
    /// Availability summary (host install and project usage).
    /// </summary>
    public string Status { get; init; } = string.Empty;

    public string? LogoPath { get; init; }

    /// <summary>
    /// Whether the agent is available in the current project (or no project is selected).
    /// </summary>
    public bool IsAvailable { get; init; } = true;

    /// <summary>
    /// Whether the agent defines the launch command.
    /// </summary>
    public bool CanLaunch => Plugin.Commands?.Get(AgentCommandKind.Launch) != null;

    /// <summary>
    /// Whether the agent defines the resume command.
    /// </summary>
    public bool CanResume => Plugin.Commands?.Get(AgentCommandKind.Resume) != null;

    /// <summary>
    /// Row opacity: normal when available, dimmed when not.
    /// </summary>
    public double RowOpacity => IsAvailable ? 1.0 : 0.4;

    /// <summary>
    /// Version string; starts as a placeholder and updates when resolved.
    /// </summary>
    public string Version
    {
        get => _version;
        set
        {
            if (_version == value)
            {
                return;
            }

            _version = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
