namespace CLIHub.ViewModels;

using System.ComponentModel;
using System.Runtime.CompilerServices;

using CLIHub.Core.Models;

/// <summary>
///   Display item for an agent row in the launch window.
/// </summary>
public sealed class AgentItem : INotifyPropertyChanged
{
    private Plugin _plugin = null!;
    private string _name = string.Empty;
    private string _status = string.Empty;
    private string? _logoPath;
    private bool _isAvailable = true;
    private string _version = "…";

    /// <summary>
    ///   The agent plugin this row represents.
    /// </summary>
    public required Plugin Plugin
    {
        get => _plugin;
        init => _plugin = value;
    }

    /// <summary>
    ///   The agent display name.
    /// </summary>
    public required string Name
    {
        get => _name;
        init => _name = value;
    }

    /// <summary>
    ///   Availability summary (host install and project usage).
    /// </summary>
    public string Status
    {
        get => _status;
        init => _status = value;
    }

    /// <summary>
    ///   Optional path to the agent's logo image.
    /// </summary>
    public string? LogoPath
    {
        get => _logoPath;
        init => _logoPath = value;
    }

    /// <summary>
    ///   Whether the agent is available in the current project (or no project is selected).
    /// </summary>
    public bool IsAvailable
    {
        get => _isAvailable;
        init => _isAvailable = value;
    }

    /// <summary>
    ///   Whether the agent defines the launch command.
    /// </summary>
    public bool CanLaunch => Plugin.Commands?.Get(AgentCommandKind.Launch) != null;

    /// <summary>
    ///   Whether the agent defines the resume command.
    /// </summary>
    public bool CanResume => Plugin.Commands?.Get(AgentCommandKind.Resume) != null;

    /// <summary>
    ///   Row opacity: normal when available, dimmed when not.
    /// </summary>
    public double RowOpacity => IsAvailable ? 1.0 : 0.4;

    /// <summary>
    ///   Version string; starts as a placeholder and updates when resolved.
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

    /// <summary>
    ///   Updates the row with the current values for its stable plugin identity.
    /// </summary>
    /// <param name="plugin"> The current plugin descriptor. </param>
    /// <param name="name"> The current display name. </param>
    /// <param name="logoPath"> The current logo path. </param>
    /// <param name="isAvailable"> Whether the agent is available in the current project. </param>
    public void Update(Plugin plugin, string name, string? logoPath, bool isAvailable)
    {
        SetValue(ref _plugin, plugin, nameof(Plugin));
        SetValue(ref _name, name, nameof(Name));
        SetValue(ref _logoPath, logoPath, nameof(LogoPath));

        if (_isAvailable != isAvailable)
        {
            _isAvailable = isAvailable;
            OnPropertyChanged(nameof(IsAvailable));
            OnPropertyChanged(nameof(RowOpacity));
        }
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void SetValue<T>(ref T field, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }
}
