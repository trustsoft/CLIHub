namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Agents;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Lifetime;
using CLIHub.Launching;

/// <summary>
///   Owns the CLI Agents management surface: one card per agent, the supported/installed counts,
///   the default-agent selection, and the per-card actions.
/// </summary>
public sealed class AgentManagementViewModel : ObservableObject, IAgentManagementSection
{
    private static readonly AgentOption NoneOption = new(null, "None");

    private readonly IPluginCatalog catalog;
    private readonly IAgentDetectionService detection;
    private readonly IAgentVersionService version;
    private readonly IAgentManagementService management;
    private readonly IExternalLauncher launcher;
    private readonly IApplicationOperationLifetime operationLifetime;
    private readonly ILogger<AgentManagementViewModel> logger;

    private bool disposed;
    private bool isDetecting;

    /// <summary>
    ///   Creates the section.
    /// </summary>
    /// <param name="catalog"> Plugin catalog supplying the agents. </param>
    /// <param name="detection"> Host availability detection. </param>
    /// <param name="version"> Version probe service. </param>
    /// <param name="management"> On-demand probe/update operations. </param>
    /// <param name="launcher"> Opens the agent's configuration file. </param>
    /// <param name="operationLifetime"> Application lifetime for tracked actions. </param>
    /// <param name="logger"> Logger. </param>
    public AgentManagementViewModel(IPluginCatalog catalog,
                                    IAgentDetectionService detection,
                                    IAgentVersionService version,
                                    IAgentManagementService management,
                                    IExternalLauncher launcher,
                                    IApplicationOperationLifetime operationLifetime,
                                    ILogger<AgentManagementViewModel> logger)
    {
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        this.detection = detection ?? throw new ArgumentNullException(nameof(detection));
        this.version = version ?? throw new ArgumentNullException(nameof(version));
        this.management = management ?? throw new ArgumentNullException(nameof(management));
        this.launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        this.operationLifetime = operationLifetime ?? throw new ArgumentNullException(nameof(operationLifetime));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        this.TestCommand = new RelayCommand<AgentCardItem>(card => _ = this.RunAsync("Agent test", ct => this.TestAsync(card, ct)));
        this.UpdateCommand = new RelayCommand<AgentCardItem>(card => _ = this.RunAsync("Agent update", ct => this.UpdateAsync(card, ct)));
        this.EditConfigCommand = new RelayCommand<AgentCardItem>(this.EditConfig);
        this.DetectCommand = new RelayCommand(() => _ = this.RunAsync("Agent detection", this.DetectAsync), () => this.CanDetect);
    }

    /// <inheritdoc />
    public ObservableCollection<AgentCardItem> Cards { get; } = new();

    /// <inheritdoc />
    public int InstalledAgentCount => this.Cards.Count(card => card.IsInstalled);

    /// <inheritdoc />
    public int SupportedAgentCount => this.Cards.Count;

    /// <summary>
    ///   The number of supported agents that are not installed on this machine.
    /// </summary>
    public int NotInstalledAgentCount => this.SupportedAgentCount - this.InstalledAgentCount;

    /// <inheritdoc />
    public string AvailabilitySummary => this.SupportedAgentCount switch
    {
        0 => "No agent plugins found",
        var supported when this.NotInstalledAgentCount == 0 => $"{supported} supported · all installed",
        var supported => $"{supported} supported · {this.NotInstalledAgentCount} not installed"
    };

    /// <inheritdoc />
    public string DefaultAgentName => this.SelectedDefaultAgent.Name;

    /// <inheritdoc />
    public bool HasDefaultAgent => this.CurrentDefaultAgentId is not null;

    /// <inheritdoc />
    public string DefaultAgentDetail
    {
        get
        {
            if (!this.HasDefaultAgent)
            {
                return "No default agent — choose one";
            }

            var version = this.Cards
                .FirstOrDefault(card => card.PluginId == this.CurrentDefaultAgentId)
                ?.Version;

            return string.IsNullOrEmpty(version) || version == "…"
                ? "Preselected in the launch window"
                : $"Version {version}";
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<AgentOption> DefaultAgentOptions
    {
        get;
        private set => this.SetProperty(ref field, value);
    } = [NoneOption];

    /// <inheritdoc />
    public AgentOption SelectedDefaultAgent
    {
        get;
        set
        {
            if (this.SetProperty(ref field, value ?? NoneOption))
            {
                this.RaiseDefaultAgentChanges();
            }
        }
    } = NoneOption;

    /// <inheritdoc />
    public string? CurrentDefaultAgentId => this.SelectedDefaultAgent.Id;

    /// <inheritdoc />
    public RelayCommand<AgentCardItem> TestCommand { get; }

    /// <inheritdoc />
    public RelayCommand<AgentCardItem> UpdateCommand { get; }

    /// <inheritdoc />
    public RelayCommand<AgentCardItem> EditConfigCommand { get; }

    /// <inheritdoc />
    public RelayCommand DetectCommand { get; }

    /// <inheritdoc />
    public bool IsDetecting
    {
        get => this.isDetecting;
        private set
        {
            if (this.SetProperty(ref this.isDetecting, value))
            {
                this.OnPropertyChanged(nameof(this.CanDetect));
            }
        }
    }

    /// <inheritdoc />
    public bool CanDetect => !this.IsDetecting;

    /// <inheritdoc />
    public void Load(string? defaultAgentId)
    {
        if (this.disposed)
        {
            return;
        }

        this.Cards.Clear();

        foreach (var plugin in this.catalog.GetAllPlugins())
        {
            var isInstalled = this.detection.IsInstalledInSystem(plugin);
            var canUpdate = plugin.Commands?.Get(AgentCommandKind.Update) is not null;
            var card = new AgentCardItem(
                plugin,
                plugin.LogoPath,
                isInstalled,
                "…",
                AgentConfigurationPath.For(plugin))
            {
                CanUpdate = canUpdate
            };

            this.Cards.Add(card);
        }

        this.RefreshAvailabilitySummary();
        this.RebuildDefaultAgentOptions(defaultAgentId);
        this.PopulateVersions();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.disposed = true;
    }

    private void RebuildDefaultAgentOptions(string? defaultAgentId)
    {
        var options = new List<AgentOption> { NoneOption };
        options.AddRange(this.Cards.Select(card => new AgentOption(card.PluginId, card.Name, card.Version, card.IsInstalled)));
        this.DefaultAgentOptions = options;

        this.SelectedDefaultAgent = options.FirstOrDefault(o => o.Id == defaultAgentId) ?? NoneOption;
    }

    private void RefreshAvailabilitySummary()
    {
        this.OnPropertyChanged(nameof(this.InstalledAgentCount));
        this.OnPropertyChanged(nameof(this.SupportedAgentCount));
        this.OnPropertyChanged(nameof(this.NotInstalledAgentCount));
        this.OnPropertyChanged(nameof(this.AvailabilitySummary));
        this.OnPropertyChanged(nameof(this.DefaultAgentDetail));
    }

    private void RaiseDefaultAgentChanges()
    {
        this.OnPropertyChanged(nameof(this.DefaultAgentName));
        this.OnPropertyChanged(nameof(this.DefaultAgentDetail));
        this.OnPropertyChanged(nameof(this.HasDefaultAgent));
    }

    private void PopulateVersions() =>
        _ = this.RunAsync("Agent version population", async cancellationToken =>
        {
            foreach (var card in this.Cards.ToList())
            {
                var ver = await this.version.GetVersionAsync(card.Plugin, cancellationToken);
                if (!this.disposed)
                {
                    card.Version = ver ?? "unknown";
                }
            }

            if (!this.disposed)
            {
                this.RebuildDefaultAgentOptions(this.SelectedDefaultAgent?.Id);
            }
        });

    private async Task TestAsync(AgentCardItem card, CancellationToken cancellationToken)
    {
        var result = await this.management.ProbeAsync(card.Plugin, cancellationToken);
        card.IsInstalled = result.IsInstalled;
        card.Version = result.Version ?? "unknown";
        card.Message = result.IsInstalled
            ? $"Installed · version {card.Version}"
            : "Not installed on the host";
        this.RefreshAvailabilitySummary();
        this.RebuildDefaultAgentOptions(this.SelectedDefaultAgent?.Id);
    }

    private async Task DetectAsync(CancellationToken cancellationToken)
    {
        this.IsDetecting = true;
        try
        {
            foreach (var card in this.Cards.ToList())
            {
                var result = await this.management.ProbeAsync(card.Plugin, cancellationToken);
                if (this.disposed)
                {
                    return;
                }

                card.IsInstalled = result.IsInstalled;
                card.Version = result.Version ?? "unknown";
            }

            this.RefreshAvailabilitySummary();
            this.RebuildDefaultAgentOptions(this.SelectedDefaultAgent?.Id);
        }
        finally
        {
            this.IsDetecting = false;
        }
    }

    private async Task UpdateAsync(AgentCardItem card, CancellationToken cancellationToken)
    {
        card.Message = $"Updating {card.Name}…";
        var result = await this.management.UpdateAsync(card.Plugin, cancellationToken);
        card.Message = result.Outcome switch
        {
            AgentUpdateOutcome.Started => "Update started.",
            _ => result.Message ?? "Update failed."
        };
    }

    private void EditConfig(AgentCardItem card)
    {
        if (card.ConfigPath is not { } path)
        {
            return;
        }

        try
        {
            this.launcher.Open(path);
            card.Message = $"Opened {path}";
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Could not open agent config {Path}", path);
            card.Message = $"Could not open {path}";
        }
    }

    private Task RunAsync(string operationName, Func<CancellationToken, Task> operation) =>
        this.operationLifetime.RunAsync(operationName, operation);
}
