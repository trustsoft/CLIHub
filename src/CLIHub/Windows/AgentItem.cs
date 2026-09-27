using CLIHub.Core.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CLIHub.Windows;

/// <summary>
/// Display item for an agent in the main window.
/// </summary>
public sealed class AgentItem : INotifyPropertyChanged
{
    private string _version = "…";

    public required Plugin Plugin { get; init; }
    public required string Name { get; init; }
    public required string Status { get; init; }
    public string? LogoPath { get; init; }

    /// <summary>
    /// Whether the agent is available in the current project (or no project is selected).
    /// </summary>
    public bool IsAvailable { get; init; } = true;

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
                return;

            _version = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
