namespace CLIHub.ViewModels;

using CLIHub.Core.Models;

/// <summary>
///   A selectable default-agent option; a null <see cref="Id" /> means "None".
/// </summary>
/// <param name="Id"> The agent plugin id, or null for no default. </param>
/// <param name="Name"> The display name. </param>
/// <param name="Version"> The installed version, or null when unknown. </param>
public sealed record AgentOption(string? Id, string Name, string? Version = null)
{
    /// <summary>
    ///   The display text: the name with the installed version in parentheses when it is known.
    /// </summary>
    public string DisplayName =>
        string.IsNullOrEmpty(this.Version) || this.Version == "…" ? this.Name : $"{this.Name} ({this.Version})";
}

/// <summary>
///   A management card for a single agent on the CLI Agents settings page.
/// </summary>
public sealed class AgentCardItem : ObservableObject
{
    private bool isInstalled;
    private string version;

    /// <summary>
    ///   Creates a card from the agent's plugin descriptor and current state.
    /// </summary>
    /// <param name="plugin"> The agent plugin. </param>
    /// <param name="logoPath"> The agent logo path, or null. </param>
    /// <param name="isInstalled"> Whether the agent is installed on the host. </param>
    /// <param name="version"> The installed version, or a placeholder. </param>
    /// <param name="configPath"> The path to the agent's plugin.json, or null. </param>
    public AgentCardItem(Plugin plugin, string? logoPath, bool isInstalled, string version, string? configPath)
    {
        this.Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        this.PluginId = plugin.Id;
        this.Name = plugin.Name;
        this.LogoPath = logoPath;
        this.ConfigPath = configPath;
        this.CommandLabels = BuildCommandLabels(plugin.Commands);
        this.HostMarkers = plugin.Detection?.SystemPaths?.ToArray() ?? [];
        this.ProjectMarkers = plugin.Detection?.ProjectIndicators?.ToArray() ?? [];
        this.isInstalled = isInstalled;
        this.version = version;
    }

    /// <summary>
    ///   The agent plugin.
    /// </summary>
    public Plugin Plugin { get; }

    /// <summary>
    ///   The stable plugin id.
    /// </summary>
    public string PluginId { get; }

    /// <summary>
    ///   The agent display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///   The agent logo path, or null.
    /// </summary>
    public string? LogoPath { get; }

    /// <summary>
    ///   The path to the agent's plugin.json, or null.
    /// </summary>
    public string? ConfigPath { get; }

    /// <summary>
    ///   Labels for the commands the agent declares.
    /// </summary>
    public IReadOnlyList<string> CommandLabels { get; }

    /// <summary>
    ///   The declared host installation markers.
    /// </summary>
    public IReadOnlyList<string> HostMarkers { get; }

    /// <summary>
    ///   The declared project indicators.
    /// </summary>
    public IReadOnlyList<string> ProjectMarkers { get; }

    /// <summary>
    ///   Whether the agent declares any host markers.
    /// </summary>
    public bool HasHostMarkers => this.HostMarkers.Count > 0;

    /// <summary>
    ///   Whether the agent declares any project indicators.
    /// </summary>
    public bool HasProjectMarkers => this.ProjectMarkers.Count > 0;

    /// <summary>
    ///   Whether the agent declares any commands.
    /// </summary>
    public bool HasCommands => this.CommandLabels.Count > 0;

    /// <summary>
    ///   The declared command kinds as a single line, or an em dash when none are declared.
    /// </summary>
    public string CommandsText => this.CommandLabels.Count > 0 ? string.Join(", ", this.CommandLabels) : "—";

    /// <summary>
    ///   The declared host markers as a single line, or an em dash when none are declared.
    /// </summary>
    public string HostMarkersText => this.HostMarkers.Count > 0 ? string.Join(", ", this.HostMarkers) : "—";

    /// <summary>
    ///   The declared project indicators as a single line, or an em dash when none are declared.
    /// </summary>
    public string ProjectMarkersText => this.ProjectMarkers.Count > 0 ? string.Join(", ", this.ProjectMarkers) : "—";

    /// <summary>
    ///   Whether the agent is installed on the host.
    /// </summary>
    public bool IsInstalled
    {
        get => this.isInstalled;
        set
        {
            if (this.SetProperty(ref this.isInstalled, value))
            {
                this.OnPropertyChanged(nameof(this.StatusText));
            }
        }
    }

    /// <summary>
    ///   The installation status text.
    /// </summary>
    public string StatusText => this.IsInstalled ? "Installed" : "Not installed";

    /// <summary>
    ///   The installed version, or an "unknown" placeholder.
    /// </summary>
    public string Version
    {
        get => this.version;
        set => this.SetProperty(ref this.version, value);
    }

    /// <summary>
    ///   Whether the Update action is available for the agent.
    /// </summary>
    public bool CanUpdate
    {
        get;
        set => this.SetProperty(ref field, value);
    }

    /// <summary>
    ///   Why the Update action is unavailable, when it is.
    /// </summary>
    public string? UpdateNote
    {
        get;
        set => this.SetProperty(ref field, value);
    }

    /// <summary>
    ///   Whether the Edit Config action is available.
    /// </summary>
    public bool CanEditConfig => this.ConfigPath is not null;

    /// <summary>
    ///   The last action outcome message, or null.
    /// </summary>
    public string? Message
    {
        get;
        set
        {
            if (this.SetProperty(ref this.message, value))
            {
                this.OnPropertyChanged(nameof(this.HasMessage));
            }
        }
    }

    /// <summary>
    ///   Whether the card has an action outcome message to show.
    /// </summary>
    public bool HasMessage => !string.IsNullOrEmpty(this.Message);

    private static IReadOnlyList<string> BuildCommandLabels(AgentCommands? commands)
    {
        if (commands is null)
        {
            return [];
        }

        var labels = new List<string>();

        if (commands.Get(AgentCommandKind.Launch) is not null)
        {
            labels.Add("launch");
        }

        if (commands.Get(AgentCommandKind.Resume) is not null)
        {
            labels.Add("resume");
        }

        if (commands.Get(AgentCommandKind.Version) is not null)
        {
            labels.Add("version");
        }

        if (commands.Get(AgentCommandKind.Update) is not null)
        {
            labels.Add("update");
        }

        return labels;
    }
}
