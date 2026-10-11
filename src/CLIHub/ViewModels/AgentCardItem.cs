namespace CLIHub.ViewModels;

using CLIHub.Core.Models;

/// <summary>
///   A selectable default-agent option; a null <see cref="Id"/> means "None".
/// </summary>
/// <param name="Id"> The agent plugin id, or null for no default. </param>
/// <param name="Name"> The display name. </param>
public sealed record AgentOption(string? Id, string Name);

/// <summary>
///   A management card for a single agent on the CLI Agents settings page.
/// </summary>
public sealed class AgentCardItem : ObservableObject
{
    private bool _isInstalled;
    private string _version;
    private bool _canUpdate;
    private string? _updateNote;
    private string? _message;

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
        Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        PluginId = plugin.Id;
        Name = plugin.Name;
        LogoPath = logoPath;
        ConfigPath = configPath;
        CommandLabels = BuildCommandLabels(plugin.Commands);
        HostMarkers = plugin.Detection?.SystemPaths?.ToArray() ?? [];
        ProjectMarkers = plugin.Detection?.ProjectIndicators?.ToArray() ?? [];
        _isInstalled = isInstalled;
        _version = version;
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
    public bool HasHostMarkers => HostMarkers.Count > 0;

    /// <summary>
    ///   Whether the agent declares any project indicators.
    /// </summary>
    public bool HasProjectMarkers => ProjectMarkers.Count > 0;

    /// <summary>
    ///   Whether the agent declares any commands.
    /// </summary>
    public bool HasCommands => CommandLabels.Count > 0;

    /// <summary>
    ///   The declared command kinds as a single line, or an em dash when none are declared.
    /// </summary>
    public string CommandsText => CommandLabels.Count > 0 ? string.Join(", ", CommandLabels) : "—";

    /// <summary>
    ///   The declared host markers as a single line, or an em dash when none are declared.
    /// </summary>
    public string HostMarkersText => HostMarkers.Count > 0 ? string.Join(", ", HostMarkers) : "—";

    /// <summary>
    ///   The declared project indicators as a single line, or an em dash when none are declared.
    /// </summary>
    public string ProjectMarkersText => ProjectMarkers.Count > 0 ? string.Join(", ", ProjectMarkers) : "—";

    /// <summary>
    ///   Whether the agent is installed on the host.
    /// </summary>
    public bool IsInstalled
    {
        get => _isInstalled;
        set
        {
            if (SetProperty(ref _isInstalled, value))
            {
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    /// <summary>
    ///   The installation status text.
    /// </summary>
    public string StatusText => IsInstalled ? "Installed" : "Not installed";

    /// <summary>
    ///   The installed version, or an "unknown" placeholder.
    /// </summary>
    public string Version
    {
        get => _version;
        set => SetProperty(ref _version, value);
    }

    /// <summary>
    ///   Whether the Update action is available for the agent.
    /// </summary>
    public bool CanUpdate
    {
        get => _canUpdate;
        set => SetProperty(ref _canUpdate, value);
    }

    /// <summary>
    ///   Why the Update action is unavailable, when it is.
    /// </summary>
    public string? UpdateNote
    {
        get => _updateNote;
        set => SetProperty(ref _updateNote, value);
    }

    /// <summary>
    ///   Whether the Edit Config action is available.
    /// </summary>
    public bool CanEditConfig => ConfigPath is not null;

    /// <summary>
    ///   The last action outcome message, or null.
    /// </summary>
    public string? Message
    {
        get => _message;
        set
        {
            if (SetProperty(ref _message, value))
            {
                OnPropertyChanged(nameof(HasMessage));
            }
        }
    }

    /// <summary>
    ///   Whether the card has an action outcome message to show.
    /// </summary>
    public bool HasMessage => !string.IsNullOrEmpty(Message);

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
