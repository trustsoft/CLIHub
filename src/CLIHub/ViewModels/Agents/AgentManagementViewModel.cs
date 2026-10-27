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
    private bool isCheckingUpdates;
    private bool isCheckingCardUpdates;

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

        this.DetectCardCommand = new RelayCommand<AgentCardItem>(card => _ = this.RunAsync("Agent detect", ct => this.DetectCardAsync(card, ct)), _ => this.CanDetect);
        this.CheckCardUpdatesCommand = new RelayCommand<AgentCardItem>(card => _ = this.RunAsync("Agent card update check", ct => this.CheckCardUpdatesAsync(card, ct)), card => this.CanCheckCardUpdates(card));
        this.UpdateCommand = new RelayCommand<AgentCardItem>(card => _ = this.RunAsync("Agent update", ct => this.UpdateAsync(card, ct)));
        this.EditConfigCommand = new RelayCommand<AgentCardItem>(this.EditConfig);
        this.DetectCommand = new RelayCommand(() => _ = this.RunAsync("Agent detection", this.DetectAsync), () => this.CanDetect);
        this.CheckUpdatesCommand = new RelayCommand(() => _ = this.RunAsync("Agent update check", this.CheckUpdatesAsync), () => this.CanCheckUpdates);
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

            return version is null
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
    public RelayCommand<AgentCardItem> DetectCardCommand { get; }

    /// <inheritdoc />
    public RelayCommand<AgentCardItem> CheckCardUpdatesCommand { get; }

    /// <inheritdoc />
    public RelayCommand<AgentCardItem> UpdateCommand { get; }

    /// <inheritdoc />
    public RelayCommand<AgentCardItem> EditConfigCommand { get; }

    /// <inheritdoc />
    public RelayCommand DetectCommand { get; }

    /// <inheritdoc />
    public RelayCommand CheckUpdatesCommand { get; }

    /// <inheritdoc />
    public bool IsDetecting
    {
        get => this.isDetecting;
        private set
        {
            if (this.SetProperty(ref this.isDetecting, value))
            {
                this.RaiseBusyStateChanges();
            }
        }
    }

    /// <inheritdoc />
    public bool CanDetect => !this.IsDetecting && !this.IsCheckingUpdates && !this.IsCheckingCardUpdates;

    /// <inheritdoc />
    public bool IsCheckingUpdates
    {
        get => this.isCheckingUpdates;
        private set
        {
            if (this.SetProperty(ref this.isCheckingUpdates, value))
            {
                this.RaiseBusyStateChanges();
            }
        }
    }

    /// <inheritdoc />
    public bool CanCheckUpdates => !this.IsDetecting && !this.IsCheckingUpdates && !this.IsCheckingCardUpdates;

    /// <inheritdoc />
    public bool IsCheckingCardUpdates
    {
        get => this.isCheckingCardUpdates;
        private set
        {
            if (this.SetProperty(ref this.isCheckingCardUpdates, value))
            {
                this.RaiseBusyStateChanges();
            }
        }
    }

    /// <summary>
    ///   Whether the per-card Check updates action may run for the card: the card is checkable
    ///   (installed, not already being checked by its own action) and no header-wide pass or
    ///   other per-card check is running.
    /// </summary>
    /// <param name="card"> The card to check. </param>
    /// <returns> Whether the command may execute for the card. </returns>
    private bool CanCheckCardUpdates(AgentCardItem card) => card.CanCheckUpdates && this.CanDetect;

    /// <inheritdoc />
    public string? UpdateCheckSummary
    {
        get;
        private set => this.SetProperty(ref field, value);
    }

    /// <inheritdoc />
    public void Load(string? defaultAgentId)
    {
        if (this.disposed)
        {
            return;
        }

        this.UpdateCheckSummary = null;
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
                null,
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
        options.AddRange(this.Cards.Select(card => new AgentOption(card.PluginId, card.Name, card.Version, card.InstallState)));
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

    /// <summary>
    ///   Reports a busy-state flip to the UI: the derived availability properties change, and the
    ///   gated commands re-query explicitly. WPF buttons cache the <see cref="System.Windows.Input.ICommand.CanExecute" />
    ///   result and only re-query on <c>CanExecuteChanged</c>, so without the explicit raise a
    ///   button that re-queried mid-pass would stay disabled after the pass ends.
    /// </summary>
    private void RaiseBusyStateChanges()
    {
        this.OnPropertyChanged(nameof(this.CanDetect));
        this.OnPropertyChanged(nameof(this.CanCheckUpdates));
        this.DetectCommand.RaiseCanExecuteChanged();
        this.CheckUpdatesCommand.RaiseCanExecuteChanged();
        this.DetectCardCommand.RaiseCanExecuteChanged();
        this.CheckCardUpdatesCommand.RaiseCanExecuteChanged();
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
                if (card.InstallState == AgentInstallState.NotInstalled)
                {
                    // A missing CLI has no version to probe and no update source to check; clear
                    // any stale advisory indicator so it cannot survive an uninstall.
                    if (!cancellationToken.IsCancellationRequested && generation == this.versionPopulationGeneration)
                    {
                        card.UpdateAvailability = null;
                    }

                    return;
                }

                var ver = await this.version.GetVersionAsync(card.Plugin, cancellationToken);
                if (!cancellationToken.IsCancellationRequested && generation == this.versionPopulationGeneration)
                {
                    card.Version = ver;
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

                if (!cancellationToken.IsCancellationRequested && generation == this.blockedStateGeneration)
                {
                    card.CanUpdate = declaresUpdate && running.Count == 0;

                    // Write the block explanation only when blocked; never clear a message a
                    // user action may have set between render and pass completion.
                    if (running.Count > 0)
                    {
                        card.Message = "Update blocked while running";
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

    private async Task DetectCardAsync(AgentCardItem card, CancellationToken cancellationToken)
    {
        var previousState = card.InstallState;
        card.InstallState = AgentInstallState.Detecting;
        try
        {
            var result = await this.management.ProbeAsync(card.Plugin, cancellationToken);
            this.ApplyProbeResult(card, result);
            card.Message = (result.IsInstalled, result.Version) switch
            {
                (true, { } version) => $"Installed · version {version}",
                (true, null) => "Installed",
                _ => "Not installed on the host"
            };
            this.RefreshAvailabilitySummary();
            this.RebuildDefaultAgentOptions(this.SelectedDefaultAgent?.Id);
        }
        catch (Exception)
        {
            // Never leave the card stuck in Detecting: settle it back and let the caller's
            // operation logging observe the failure. Version and availability stay untouched.
            card.InstallState = previousState;
            throw;
        }
    }

    private async Task DetectAsync(CancellationToken cancellationToken)
    {
        this.IsDetecting = true;
        try
        {
            var cards = this.Cards.ToList();
            var previousStates = cards.ToDictionary(card => card, card => card.InstallState);

            // Announce the pass on every card before the first probe, so no chip shows a stale
            // settled state while its agent is being re-probed.
            foreach (var card in cards)
            {
                card.InstallState = AgentInstallState.Detecting;
            }

            try
            {
                foreach (var card in cards)
                {
                    var result = await this.management.ProbeAsync(card.Plugin, cancellationToken);
                    if (this.disposed)
                    {
                        return;
                    }

                    this.ApplyProbeResult(card, result);
                }

                this.RefreshAvailabilitySummary();
                this.RebuildDefaultAgentOptions(this.SelectedDefaultAgent?.Id);
            }
            catch (Exception)
            {
                // The pass is abandoning: settle every card it had flipped to Detecting back to
                // its pre-pass state so no chip stays stuck, then let the caller log the failure.
                foreach (var card in cards)
                {
                    card.InstallState = previousStates[card];
                }

                throw;
            }
        }
        finally
        {
            this.IsDetecting = false;
        }
    }

    private async Task CheckUpdatesAsync(CancellationToken cancellationToken)
    {
        this.IsCheckingUpdates = true;
        try
        {
            // A new pass starts from a clean slate: the previous summary line goes away and is
            // only rebuilt once every card has settled.
            this.UpdateCheckSummary = null;

            var cards = this.Cards.ToList();

            // The counted checks are the cards that were installed when the pass started; the
            // pass itself never changes an install state.
            var checkedCount = cards.Count(card => card.InstallState == AgentInstallState.Installed);

            await Task.WhenAll(cards.Select(async card =>
            {
                if (card.InstallState != AgentInstallState.Installed)
                {
                    // A missing CLI has no update source to check; clear any stale advisory
                    // indicator so it cannot survive an uninstall.
                    card.UpdateAvailability = null;
                    return;
                }

                // Invalidate first so the check below bypasses the cached result and its TTL.
                this.availability.InvalidateFor(card.Plugin);
                var availability = await this.availability.CheckAsync(card.Plugin, cancellationToken);
                if (!this.disposed)
                {
                    card.UpdateAvailability = availability;
                }
            }));

            if (!this.disposed)
            {
                this.UpdateCheckSummary = BuildUpdateCheckSummary(
                    checkedCount,
                    cards.Count(card => card.HasUpdateAvailable));
            }
        }
        finally
        {
            this.IsCheckingUpdates = false;
        }
    }

    /// <summary>
    ///   Runs the per-card forced update-availability check for one agent: invalidates only that
    ///   agent's cached result, re-checks its declared source, and reports the outcome on the
    ///   card's message line. Never re-runs installation detection, never issues a version
    ///   command, and never touches the header-wide update-check summary.
    /// </summary>
    /// <param name="card"> The card whose agent is checked. </param>
    /// <param name="cancellationToken"> Token that cancels the check. </param>
    private async Task CheckCardUpdatesAsync(AgentCardItem card, CancellationToken cancellationToken)
    {
        if (card.InstallState != AgentInstallState.Installed)
        {
            // Defensive gate (command + IsEnabled already refuse not-installed cards): a missing
            // CLI has no update source to check; clear any stale advisory indicator.
            card.UpdateAvailability = null;
            card.Message = "Not installed on the host";
            return;
        }

        card.IsCheckingUpdates = true;
        this.IsCheckingCardUpdates = true;
        try
        {
            // Invalidate first so the check below bypasses the cached result and its TTL.
            this.availability.InvalidateFor(card.Plugin);
            var availability = await this.availability.CheckAsync(card.Plugin, cancellationToken);
            if (!this.disposed)
            {
                card.UpdateAvailability = availability;
                card.Message = availability.Status switch
                {
                    AgentUpdateStatus.UpdateAvailable when availability.InstalledVersion is { } installed
                                                     && availability.LatestVersion is { } latest
                        => $"Update available · {installed} → {latest}",
                    AgentUpdateStatus.UpdateAvailable => "Update available",
                    AgentUpdateStatus.UpToDate => "Up to date",
                    _ => "Update availability unknown"
                };
            }
        }
        finally
        {
            this.IsCheckingCardUpdates = false;
            card.IsCheckingUpdates = false;
        }
    }

    /// <summary>
    ///   Builds the muted summary line for a finished manual update check: the singular/plural
    ///   agent and update wordings, or the no-installed-agents notice when nothing was checked.
    /// </summary>
    /// <param name="checkedCount"> The number of installed agents the pass checked. </param>
    /// <param name="updateCount"> The number of checked agents with an update available. </param>
    /// <returns> The summary text. </returns>
    private static string BuildUpdateCheckSummary(int checkedCount, int updateCount)
    {
        if (checkedCount == 0)
        {
            return "No installed agents to check";
        }

        var agents = checkedCount == 1 ? "1 agent" : $"{checkedCount} agents";
        return updateCount switch
        {
            0 => $"Checked {agents} · no updates",
            1 => $"Checked {agents} · 1 update available",
            _ => $"Checked {agents} · {updateCount} updates available"
        };
    }

    /// <summary>
    ///   Settles a card from a finished probe: the install state and the raw version (null stays
    ///   unresolved). A detect never re-checks update availability — an installed agent keeps its
    ///   last shown indicator untouched, and a not-installed agent gets its stale indicator
    ///   cleared so it cannot survive an uninstall.
    /// </summary>
    /// <param name="card"> The card to settle. </param>
    /// <param name="result"> The finished probe result. </param>
    private void ApplyProbeResult(AgentCardItem card, AgentProbeResult result)
    {
        if (result.IsInstalled)
        {
            card.InstallState = AgentInstallState.Installed;
            card.Version = result.Version;
        }
        else
        {
            card.InstallState = AgentInstallState.NotInstalled;
            card.Version = null;
            card.UpdateAvailability = null;
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
