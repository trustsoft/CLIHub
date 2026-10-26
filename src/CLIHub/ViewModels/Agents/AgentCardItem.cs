namespace CLIHub.ViewModels;

using CLIHub.Core.Agents;
using CLIHub.Core.Models;
using CLIHub.Helpers;

/// <summary>
///   Installation state of an agent as known by a view-model surface. XAML data triggers match
///   enum members by name, so renaming a member is a XAML-breaking change.
/// </summary>
public enum AgentInstallState
{
    /// <summary>An installation probe is running; the settled state is not known yet.</summary>
    Detecting,

    /// <summary>The agent is installed on the host.</summary>
    Installed,

    /// <summary>The agent is not installed on the host.</summary>
    NotInstalled
}

/// <summary>
///   A selectable default-agent option; a null <see cref="Id" /> means "None".
/// </summary>
/// <param name="Id"> The agent plugin id, or null for no default. </param>
/// <param name="Name"> The display name. </param>
/// <param name="Version"> The resolved installed version, or null when unresolved. </param>
/// <param name="InstallState"> The agent's installation state; defaults to installed. </param>
public sealed record AgentOption(
    string? Id,
    string Name,
    string? Version = null,
    AgentInstallState InstallState = AgentInstallState.Installed)
{
    /// <summary>
    ///   Whether the agent is installed on the host; uninstalled options render disabled.
    ///   Prefer binding <see cref="InstallState" />: this derived flag is also false while the
    ///   state is being detected.
    /// </summary>
    public bool IsInstalled => this.InstallState == AgentInstallState.Installed;

    /// <summary>
    ///   The display text: the name with the resolved version in parentheses, a capitalized
    ///   "(Not installed)" suffix for a missing agent (deliberately different from the runtime
    ///   selector's lowercase suffix), or the bare name otherwise — including while the
    ///   installation state is being detected.
    /// </summary>
    public string DisplayName => this.InstallState switch
    {
        AgentInstallState.NotInstalled => $"{this.Name} (Not installed)",
        AgentInstallState.Installed when this.Version is { } version => $"{this.Name} ({version})",
        _ => this.Name
    };
}

/// <summary>
///   A management card for a single agent on the CLI Agents settings page.
/// </summary>
public sealed class AgentCardItem : ObservableObject
{
    private AgentInstallState installState;
    private string? version;
    private AgentUpdateAvailability? updateAvailability;
    private bool isCheckingUpdates;

    /// <summary>
    ///   Creates a card from the agent's plugin descriptor and current state.
    /// </summary>
    /// <param name="plugin"> The agent plugin. </param>
    /// <param name="logoPath"> The agent logo path, or null. </param>
    /// <param name="isInstalled"> Whether the agent is installed on the host. </param>
    /// <param name="version"> The resolved installed version, or null when unresolved. </param>
    /// <param name="configPath"> The path to the agent's plugin.json, or null. </param>
    public AgentCardItem(Plugin plugin, string? logoPath, bool isInstalled, string? version = null, string? configPath = null)
    {
        this.Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        this.PluginId = plugin.Id;
        this.Name = plugin.Name;
        this.LogoPath = logoPath;
        this.ConfigPath = configPath;
        this.CommandLabels = BuildCommandLabels(plugin.Commands);
        this.installState = isInstalled ? AgentInstallState.Installed : AgentInstallState.NotInstalled;
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
    ///   Whether the agent declares any commands.
    /// </summary>
    public bool HasCommands => this.CommandLabels.Count > 0;

    /// <summary>
    ///   The declared command kinds as a single line, or an em dash when none are declared.
    /// </summary>
    public string CommandsText => this.CommandLabels.Count > 0 ? string.Join(", ", this.CommandLabels) : "—";

    /// <summary>
    ///   The agent's installation state as currently known; the observable source of truth for
    ///   the chip and selector styling. Bindings should prefer this property over
    ///   <see cref="IsInstalled" />, which is also false while detecting.
    /// </summary>
    public AgentInstallState InstallState
    {
        get => this.installState;
        set
        {
            if (this.SetProperty(ref this.installState, value))
            {
                this.OnPropertyChanged(nameof(this.IsInstalled));
                this.OnPropertyChanged(nameof(this.VersionChipText));
                this.OnPropertyChanged(nameof(this.CanCheckUpdates));
            }
        }
    }

    /// <summary>
    ///   Whether the agent is installed on the host. Derived from <see cref="InstallState" />;
    ///   the value is also false while the state is being detected.
    /// </summary>
    public bool IsInstalled => this.InstallState == AgentInstallState.Installed;

    /// <summary>
    ///   The state chip text: "Detecting…" while an installation probe runs, "Not installed"
    ///   for a missing agent, the resolved version as "v{version}" when installed with one, and
    ///   "Installed" when installed without one.
    /// </summary>
    public string VersionChipText => this.InstallState switch
    {
        AgentInstallState.Detecting => AgentVersionText.Detecting,
        AgentInstallState.NotInstalled => AgentVersionText.NotInstalled,
        AgentInstallState.Installed when this.version is { } version => $"v{version}",
        _ => AgentVersionText.Installed
    };

    /// <summary>
    ///   The resolved installed version, or null when no version has been resolved. Display
    ///   text never derives from this value's shape; it is read together with
    ///   <see cref="InstallState" />.
    /// </summary>
    public string? Version
    {
        get => this.version;
        set
        {
            if (this.SetProperty(ref this.version, value))
            {
                this.OnPropertyChanged(nameof(this.VersionChipText));
            }
        }
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
    ///   The latest update availability check result, or null before the first check. The result
    ///   is advisory only: it never gates or alters the Update action.
    /// </summary>
    public AgentUpdateAvailability? UpdateAvailability
    {
        get => this.updateAvailability;
        set
        {
            if (this.SetProperty(ref this.updateAvailability, value))
            {
                this.OnPropertyChanged(nameof(this.UpdateAvailabilityText));
                this.OnPropertyChanged(nameof(this.HasUpdateAvailable));
            }
        }
    }

    /// <summary>
    ///   The advisory update indicator text ("installed → latest"), or null when availability is
    ///   unknown or the agent is up to date, so no placeholder is ever rendered.
    /// </summary>
    public string? UpdateAvailabilityText =>
        this.UpdateAvailability is { Status: AgentUpdateStatus.UpdateAvailable, InstalledVersion: { } installed, LatestVersion: { } latest }
            ? $"{installed} → {latest}"
            : null;

    /// <summary>
    ///   Whether an update is known to be available. Advisory only; it does not enable, disable,
    ///   or block the Update action.
    /// </summary>
    public bool HasUpdateAvailable => this.UpdateAvailability?.Status == AgentUpdateStatus.UpdateAvailable;

    /// <summary>
    ///   Whether the per-card "Check updates" action is currently running for this card: the
    ///   per-card busy flag for that action, written by the management view model. Distinct from
    ///   the section-level <c>IsCheckingUpdates</c> pass flag on <c>IAgentManagementSection</c>,
    ///   which tracks the header-wide check.
    /// </summary>
    public bool IsCheckingUpdates
    {
        get => this.isCheckingUpdates;
        set
        {
            if (this.SetProperty(ref this.isCheckingUpdates, value))
            {
                this.OnPropertyChanged(nameof(this.CanCheckUpdates));
            }
        }
    }

    /// <summary>
    ///   Whether the per-card "Check updates" action is available for this card: the agent is
    ///   installed and this card's own check is not already running. Card-local enablement only;
    ///   exclusion from header-wide passes and other cards' checks is added by the view model's
    ///   command predicate.
    /// </summary>
    public bool CanCheckUpdates => this.IsInstalled && !this.IsCheckingUpdates;

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
            if (this.SetProperty(ref field, value))
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
