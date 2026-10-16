namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Agents;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Lifetime;
using CLIHub.Launching;

/// <summary>
///   Owns the CLI Agents management surface: one card per agent, the supported/installed metrics,
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
    public AgentManagementViewModel(
        IPluginCatalog catalog,
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
    }

    /// <inheritdoc />
    public ObservableCollection<AgentCardItem> Cards { get; } = new();

    /// <inheritdoc />
    public string Metrics
    {
        get;
        private set => this.SetProperty(ref field, value);
    } = "0 supported · 0 installed";

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
        set => this.SetProperty(ref field, value ?? NoneOption);
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
                CanUpdate = canUpdate,
                UpdateNote = canUpdate ? null : "Update not supported"
            };

            this.Cards.Add(card);
        }

        this.RefreshMetrics();
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
        options.AddRange(this.Cards.Select(card => new AgentOption(card.PluginId, card.Name, card.Version)));
        this.DefaultAgentOptions = options;

        this.SelectedDefaultAgent = options.FirstOrDefault(o => o.Id == defaultAgentId) ?? NoneOption;
    }

    private void RefreshMetrics()
    {
        var installed = this.Cards.Count(card => card.IsInstalled);
        this.Metrics = $"{this.Cards.Count} supported · {installed} installed";
    }

    private void PopulateVersions() =>
        _ = this.RunAsync("Agent version population", async cancellationToken =>
        {
            foreach (var card in this.Cards.ToList())
            {
                var version = await this.version.GetVersionAsync(card.Plugin, cancellationToken);
                if (!this.disposed)
                {
                    card.Version = version ?? "unknown";
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
        this.RefreshMetrics();
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
