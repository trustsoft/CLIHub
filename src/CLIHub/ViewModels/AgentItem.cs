namespace CLIHub.ViewModels;

using System.ComponentModel;
using System.Runtime.CompilerServices;

using CLIHub.Core.Models;

/// <summary>
///   Display item for an agent row in the launch window.
/// </summary>
public sealed class AgentItem : INotifyPropertyChanged
{
    private Plugin plugin = null!;
    private string name = string.Empty;
    private string status = string.Empty;
    private string? logoPath;
    private bool isAvailable = true;
    private string version = "…";

    /// <summary>
    ///   The agent plugin this row represents.
    /// </summary>
    public required Plugin Plugin
    {
        get => this.plugin;
        init => this.plugin = value;
    }

    /// <summary>
    ///   The agent display name.
    /// </summary>
    public required string Name
    {
        get => this.name;
        init => this.name = value;
    }

    /// <summary>
    ///   Availability summary (host install and project usage).
    /// </summary>
    public string Status
    {
        get => this.status;
        init => this.status = value;
    }

    /// <summary>
    ///   Optional path to the agent's logo image.
    /// </summary>
    public string? LogoPath
    {
        get => this.logoPath;
        init => this.logoPath = value;
    }

    /// <summary>
    ///   Whether the agent is available in the current project (or no project is selected).
    /// </summary>
    public bool IsAvailable
    {
        get => this.isAvailable;
        init => this.isAvailable = value;
    }

    /// <summary>
    ///   Whether the agent defines the launch command.
    /// </summary>
    public bool CanLaunch => this.Plugin.Commands?.Get(AgentCommandKind.Launch) != null;

    /// <summary>
    ///   Whether the agent defines the resume command.
    /// </summary>
    public bool CanResume => this.Plugin.Commands?.Get(AgentCommandKind.Resume) != null;

    /// <summary>
    ///   Whether the agent defines the update command.
    /// </summary>
    public bool CanUpdate => this.Plugin.Commands?.Get(AgentCommandKind.Update) != null;

    /// <summary>
    ///   Whether the agent defines the version command.
    /// </summary>
    public bool CanShowVersion => this.Plugin.Commands?.Get(AgentCommandKind.Version) != null;

    /// <summary>
    ///   Row opacity: normal when available, dimmed when not.
    /// </summary>
    public double RowOpacity => this.IsAvailable ? 1.0 : 0.4;

    /// <summary>
    ///   Version string; starts as a placeholder and updates when resolved.
    /// </summary>
    public string Version
    {
        get => this.version;
        set
        {
            if (this.version == value)
            {
                return;
            }

            this.version = value;
            this.OnPropertyChanged();
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
        this.SetValue(ref this.plugin, plugin, nameof(this.Plugin));
        this.SetValue(ref this.name, name, nameof(this.Name));
        this.SetValue(ref this.logoPath, logoPath, nameof(this.LogoPath));

        if (this.isAvailable != isAvailable)
        {
            this.isAvailable = isAvailable;
            this.OnPropertyChanged(nameof(this.IsAvailable));
            this.OnPropertyChanged(nameof(this.RowOpacity));
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
        this.OnPropertyChanged(propertyName);
    }
}
