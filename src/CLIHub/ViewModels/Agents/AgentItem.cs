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
    private string? logoPath;
    private bool isAvailable = true;

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
    ///   Legacy-window-only: bound only by MainWindow.xaml (the documented build-only exception).
    ///   Intentionally not wired into <see cref="Apply" />, so the value freezes at construction on
    ///   launch-window rows; do not add new bindings to this property.
    /// </summary>
    public string Status
    {
        get;
        init => field = value;
    } = string.Empty;

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
    ///   The resolved version, or null while unresolved; renders no subtitle when null.
    /// </summary>
    public string? Version
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            this.OnPropertyChanged();
        }
    } = null;

    /// <summary>
    ///   Updates the row with the current values for its stable plugin identity.
    /// </summary>
    /// <param name="plugin"> The current plugin descriptor. </param>
    /// <param name="name"> The current display name. </param>
    /// <param name="logoPath"> The current logo path. </param>
    /// <param name="isAvailable"> Whether the agent is available in the current project. </param>
    public void Update(Plugin plugin, string name, string? logoPath, bool isAvailable) =>
        this.Apply(plugin, name, logoPath, isAvailable);

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    ///   The single post-construction mutation path for the row: every refreshed field is wired
    ///   here exactly once, so a field added to the row has one place to be announced.
    /// </summary>
    /// <param name="plugin"> The current plugin descriptor. </param>
    /// <param name="name"> The current display name. </param>
    /// <param name="logoPath"> The current logo path. </param>
    /// <param name="isAvailable"> Whether the agent is available in the current project. </param>
    private void Apply(Plugin plugin, string name, string? logoPath, bool isAvailable)
    {
        // Property names stay explicit at every SetValue call site. Do not "simplify" this to a
        // [CallerMemberName]-attributed helper while calls pass through Update/Apply: the compiler
        // would then raise "Update"/"Apply" as the property name and silently kill every row
        // notification. (AgentItem keeps required init accessors for the object-initializer
        // construction in AgentListSynchronizer, so Apply writes fields and must name them.)
        var pluginChanged = !ReferenceEquals(this.plugin, plugin);

        this.SetValue(ref this.plugin, plugin, nameof(this.Plugin));
        this.SetValue(ref this.name, name, nameof(this.Name));
        this.SetValue(ref this.logoPath, logoPath, nameof(this.LogoPath));

        if (this.isAvailable != isAvailable)
        {
            this.isAvailable = isAvailable;
            this.OnPropertyChanged(nameof(this.IsAvailable));
            this.OnPropertyChanged(nameof(this.RowOpacity));
        }

        if (pluginChanged)
        {
            // Derived command capabilities read the plugin; after every refresh supplies a fresh
            // detached copy, a reference change is the (cheap) trigger to re-announce them.
            this.OnPropertyChanged(nameof(this.CanLaunch));
            this.OnPropertyChanged(nameof(this.CanResume));
            this.OnPropertyChanged(nameof(this.CanUpdate));
            this.OnPropertyChanged(nameof(this.CanShowVersion));
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    // The propertyName argument must stay explicit here; see the guard comment in Apply.
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
