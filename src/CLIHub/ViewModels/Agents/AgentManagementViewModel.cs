namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;

using Microsoft.Extensions.Logging;

using CLIHub.Controls;
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
    private readonly IAgentUpdateAvailabilityService availability;
    private readonly IAgentManagementService management;
    private readonly IExternalLauncher launcher;
    private readonly AgentProcessMonitor processMonitor;
    private readonly IApplicationOperationLifetime operationLifetime;
    private readonly ILogger<AgentManagementViewModel> logger;

    private bool disposed;
    private CancellationTokenSource? versionPopulationCts;
    private int versionPopulationGeneration;
    private CancellationTokenSource? blockedStateCts;
    private int blockedStateGeneration;
    private bool isDetecting;

    /// <summary>
    ///   Creates the section.
    /// </summary>
    /// <param name="catalog"> Plugin catalog supplying the agents. </param>
    /// <param name="detection"> Host availability detection. </param>
    /// <param name="version"> Version probe service. </param>
    /// <param name="availability"> Advisory update availability service. </param>
    /// <param name="management"> On-demand probe/update operations. </param>
    /// <param name="launcher"> Opens the agent's configuration file. </param>
    /// <param name="processMonitor"> Running-agent monitor used to gate card update availability. </param>
    /// <param name="operationLifetime"> Application lifetime for tracked actions. </param>
    /// <param name="logger"> Logger. </param>
    public AgentManagementViewModel(IPluginCatalog catalog,
                                    IAgentDetectionService detection,
                                    IAgentVersionService version,
                                    IAgentUpdateAvailabilityService availability,
                                    IAgentManagementService management,
                                    IExternalLauncher launcher,
                                    AgentProcessMonitor processMonitor,
                                    IApplicationOperationLifetime operationLifetime,
                                    ILogger<AgentManagementViewModel> logger)
    {
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        this.detection = detection ?? throw new ArgumentNullException(nameof(detection));
        this.version = version ?? throw new ArgumentNullException(nameof(version));
        this.availability = availability ?? throw new ArgumentNullException(nameof(availability));
        this.management = management ?? throw new ArgumentNullException(nameof(management));
        this.launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        this.processMonitor = processMonitor ?? throw new ArgumentNullException(nameof(processMonitor));
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
    public SummaryCardTone DefaultAgentValueTone =>
        this.HasDefaultAgent ? SummaryCardTone.Neutral : SummaryCardTone.Muted;

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

        var plugins = this.catalog.GetAllPlugins();

        // Cards render with declarative availability (Update enabled exactly when the plugin
        // declares an update command); the running-agent blocked state populates afterwards
        // through a fire-and-forget pass, so the page load stays the refresh trigger. The
        // execution-time block itself stays in AgentManagementService. The pass runs one
        // enumeration that feeds every card: grouping the single result by plugin id yields
        // exactly the per-plugin partitions (first-match attribution, one match per process), so
        // each card sees the same running-agent state a per-plugin call would report.
        var anyDeclaresUpdate = plugins.Any(plugin => plugin.Commands?.Get(AgentCommandKind.Update) is not null);

        foreach (var plugin in plugins)
        {
            var isInstalled = this.detection.IsInstalledInSystem(plugin);

            var declaresUpdate = plugin.Commands?.Get(AgentCommandKind.Update) is not null;

            var card = new AgentCardItem(
                plugin,
                plugin.LogoPath,
                isInstalled,
                "…",
                AgentConfigurationPath.For(plugin))
            {
                CanUpdate = declaresUpdate
            };

            this.Cards.Add(card);
        }

        this.RefreshAvailabilitySummary();
        this.RebuildDefaultAgentOptions(defaultAgentId);

        if (anyDeclaresUpdate)
        {
            this.PopulateBlockedState();
        }

        this.PopulateVersions();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.versionPopulationGeneration++;
        this.versionPopulationCts?.Cancel();
        this.versionPopulationCts?.Dispose();
        this.versionPopulationCts = null;
        this.blockedStateGeneration++;
        this.blockedStateCts?.Cancel();
        this.blockedStateCts?.Dispose();
        this.blockedStateCts = null;
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
        this.OnPropertyChanged(nameof(this.DefaultAgentValueTone));
    }

    private void PopulateVersions()
    {
        this.versionPopulationCts?.Cancel();
        this.versionPopulationCts?.Dispose();
        this.versionPopulationCts = new CancellationTokenSource();
        var generation = ++this.versionPopulationGeneration;
        var refreshCancellationToken = this.versionPopulationCts.Token;

        var cards = this.Cards.ToList();

        _ = this.RunAsync(
            "Agent version population",
            applicationToken => this.PopulateVersionsAsync(cards, generation, refreshCancellationToken, applicationToken));
    }

    private async Task PopulateVersionsAsync(
        IReadOnlyList<AgentCardItem> cards,
        int generation,
        CancellationToken refreshCancellationToken,
        CancellationToken applicationToken)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            refreshCancellationToken,
            applicationToken);
        var cancellationToken = cancellation.Token;

        try
        {
            await Task.WhenAll(cards.Select(async card =>
            {
                var ver = await this.version.GetVersionAsync(card.Plugin, cancellationToken);
                if (!cancellationToken.IsCancellationRequested && generation == this.versionPopulationGeneration)
                {
                    card.Version = ver ?? "unknown";
                }

                // The availability check reuses the just-probed version through the version
                // service's cache, so this adds no extra process launch per card.
                var availability = await this.availability.CheckAsync(card.Plugin, cancellationToken);
                if (!cancellationToken.IsCancellationRequested && generation == this.versionPopulationGeneration)
                {
                    card.UpdateAvailability = availability;
                }
            }));

            if (!cancellationToken.IsCancellationRequested && generation == this.versionPopulationGeneration)
            {
                this.RebuildDefaultAgentOptions(this.SelectedDefaultAgent?.Id);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Agent version population failed");
        }
    }

    private void PopulateBlockedState()
    {
        this.blockedStateCts?.Cancel();
        this.blockedStateCts?.Dispose();
        this.blockedStateCts = new CancellationTokenSource();
        var generation = ++this.blockedStateGeneration;
        var refreshCancellationToken = this.blockedStateCts.Token;

        var cards = this.Cards.ToList();

        _ = this.RunAsync(
            "Agent blocked-state population",
            applicationToken => this.PopulateBlockedStateAsync(cards, generation, refreshCancellationToken, applicationToken));
    }

    private async Task PopulateBlockedStateAsync(
        IReadOnlyList<AgentCardItem> cards,
        int generation,
        CancellationToken refreshCancellationToken,
        CancellationToken applicationToken)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            refreshCancellationToken,
            applicationToken);
        var cancellationToken = cancellation.Token;

        try
        {
            // GetRunningAgents is synchronous, so Task.Run moves the process snapshot (and its
            // candidate reads) off the UI thread; without it the pass would execute inline on the
            // caller's thread. The token only prevents the delegate from starting — an in-flight
            // enumeration may complete, but the guard below discards its result.
            var runningAgents = await Task.Run(() => this.processMonitor.GetRunningAgents(), cancellationToken);
            var runningByPlugin = runningAgents.ToLookup(agent => agent.Plugin.Id);

            foreach (var card in cards)
            {
                var declaresUpdate = card.Plugin.Commands?.Get(AgentCommandKind.Update) is not null;
                var running = runningByPlugin[card.Plugin.Id].ToList();
                var blockedNames = string.Join(", ", running.Select(agent => agent.Plugin.Name));

                if (!cancellationToken.IsCancellationRequested && generation == this.blockedStateGeneration)
                {
                    card.CanUpdate = declaresUpdate && running.Count == 0;

                    // Write the block explanation only when blocked; never clear a message a
                    // user action may have set between render and pass completion.
                    if (blockedNames.Length > 0)
                    {
                        card.Message = $"Update blocked while the agent is running: {blockedNames}";
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Agent blocked-state population failed");
        }
    }

    private async Task TestAsync(AgentCardItem card, CancellationToken cancellationToken)
    {
        var result = await this.management.ProbeAsync(card.Plugin, cancellationToken);
        card.IsInstalled = result.IsInstalled;
        card.Version = result.Version ?? "unknown";
        card.UpdateAvailability = await this.availability.CheckAsync(card.Plugin, cancellationToken);
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
                card.UpdateAvailability = await this.availability.CheckAsync(card.Plugin, cancellationToken);
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

        if (result.Started)
        {
            // The updater may keep running interactively; dropping the cached result only
            // forces the next lookup to re-probe instead of serving the stale cue.
            this.availability.InvalidateFor(card.Plugin);
        }
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
