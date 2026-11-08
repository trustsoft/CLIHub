namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;

using Microsoft.Extensions.Logging;

using CLIHub.Controls;
using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Lifetime;
using CLIHub.Launching;
using CLIHub.Theming;

/// <summary>
///   Owns the CLI Agents management surface: one card per agent, the supported/installed counts,
///   the default-agent selection, the per-card actions, and the plugin lifecycle actions
///   (install, disable, enable, open plugins folder, reload catalog).
/// </summary>
public sealed class AgentManagementViewModel : ObservableObject, IAgentManagementSection
{
    private static readonly AgentOption NoneOption = new(null, "None");

    /// <summary>
    ///   The summary line shown when a manual update check is abandoned by a thrown check. Kept
    ///   deliberately distinct from every <see cref="BuildUpdateCheckSummary" /> wording so an
    ///   aborted pass can never read as "checked everything, no updates".
    /// </summary>
    private const string UpdateCheckFailureSummary = "Update check failed";

    private readonly IPluginCatalog catalog;
    private readonly IAgentDetectionService detection;
    private readonly IAgentVersionService version;
    private readonly IAgentUpdateAvailabilityService availability;
    private readonly IAgentProbeService probe;
    private readonly IAgentUpdateService update;
    private readonly IExternalLauncher launcher;
    private readonly AgentProcessMonitor processMonitor;
    private readonly IApplicationOperationLifetime operationLifetime;
    private readonly IPluginInstaller pluginInstaller;
    private readonly IPluginDialogService pluginDialogs;
    private readonly IPluginStateRepository pluginState;
    private readonly IPreferencesStore preferencesStore;
    private readonly ILogger<AgentManagementViewModel> logger;

    private bool disposed;
    private readonly RestartablePass versionPopulationPass = new();
    private readonly RestartablePass blockedStatePass = new();
    private bool isDetecting;
    private bool isCheckingUpdates;
    private int runningCardActionCount;

    /// <summary>
    ///   Creates the section.
    /// </summary>
    /// <param name="catalog"> Plugin catalog supplying the agents. </param>
    /// <param name="detection"> Host availability detection. </param>
    /// <param name="version"> Version probe service. </param>
    /// <param name="availability"> Advisory update availability service. </param>
    /// <param name="probe"> On-demand probe operations. </param>
    /// <param name="update"> On-demand update operations. </param>
    /// <param name="launcher"> Opens the agent's configuration file. </param>
    /// <param name="processMonitor"> Running-agent monitor used to gate card update availability. </param>
    /// <param name="operationLifetime"> Application lifetime for tracked actions. </param>
    /// <param name="pluginInstaller"> Installs agent plugins from local packages. </param>
    /// <param name="pluginDialogs"> Plugin installation dialogs. </param>
    /// <param name="pluginState"> Plugin lifecycle state shown on the cards and updated by the lifecycle actions. </param>
    /// <param name="preferencesStore"> Preferences store for feed URL checking. </param>
    /// <param name="logger"> Logger. </param>
    public AgentManagementViewModel(IPluginCatalog catalog,
                                    IAgentDetectionService detection,
                                    IAgentVersionService version,
                                    IAgentUpdateAvailabilityService availability,
                                    IAgentProbeService probe,
                                    IAgentUpdateService update,
                                    IExternalLauncher launcher,
                                    AgentProcessMonitor processMonitor,
                                    IApplicationOperationLifetime operationLifetime,
                                    IPluginInstaller pluginInstaller,
                                    IPluginDialogService pluginDialogs,
                                    IPluginStateRepository pluginState,
                                    IPreferencesStore preferencesStore,
                                    ILogger<AgentManagementViewModel> logger)
    {
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        this.detection = detection ?? throw new ArgumentNullException(nameof(detection));
        this.version = version ?? throw new ArgumentNullException(nameof(version));
        this.availability = availability ?? throw new ArgumentNullException(nameof(availability));
        this.probe = probe ?? throw new ArgumentNullException(nameof(probe));
        this.update = update ?? throw new ArgumentNullException(nameof(update));
        this.launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        this.processMonitor = processMonitor ?? throw new ArgumentNullException(nameof(processMonitor));
        this.operationLifetime = operationLifetime ?? throw new ArgumentNullException(nameof(operationLifetime));
        this.pluginInstaller = pluginInstaller ?? throw new ArgumentNullException(nameof(pluginInstaller));
        this.pluginDialogs = pluginDialogs ?? throw new ArgumentNullException(nameof(pluginDialogs));
        this.pluginState = pluginState ?? throw new ArgumentNullException(nameof(pluginState));
        this.preferencesStore = preferencesStore ?? throw new ArgumentNullException(nameof(preferencesStore));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.catalog.PluginsChanged += this.OnPluginsChanged;

        this.DetectCardCommand = new RelayCommand<AgentCardItem>(card => _ = this.RunAsync("Agent detect", ct => this.DetectCardAsync(card, ct)), card => this.CanDetectCard(card));
        this.CheckCardUpdatesCommand = new RelayCommand<AgentCardItem>(card => _ = this.RunAsync("Agent card update check", ct => this.CheckCardUpdatesAsync(card, ct)), card => this.CanCheckCardUpdates(card));
        this.UpdateCommand = new RelayCommand<AgentCardItem>(card => _ = this.RunAsync("Agent update", ct => this.UpdateAsync(card, ct)));
        this.EditConfigCommand = new RelayCommand<AgentCardItem>(this.EditConfig);
        this.DisablePluginCommand = new RelayCommand<AgentCardItem>(this.DisablePlugin);
        this.EnablePluginCommand = new RelayCommand<DisabledAgentCardItem>(this.EnablePlugin);
        this.DetectCommand = new RelayCommand(() => _ = this.RunAsync("Agent detection", this.DetectAsync), () => this.CanDetect);
        this.CheckUpdatesCommand = new RelayCommand(() => _ = this.RunAsync("Agent update check", this.CheckUpdatesAsync), () => this.CanCheckUpdates);
        this.InstallPluginCommand = new RelayCommand(this.InstallPlugin);
        this.BrowsePluginsCommand = new RelayCommand(this.BrowsePlugins, this.CanBrowsePlugins);
        this.OpenPluginsFolderCommand = new RelayCommand(this.OpenPluginsFolder);
        this.ReloadCatalogCommand = new RelayCommand(this.ReloadCatalog);
        this.PluginActions =
        [
            new MenuAction { Label = "Install Plugin…", Glyph = IconGlyphs.Add, Command = this.InstallPluginCommand },
            new MenuAction { Label = "Browse Plugins…", Glyph = IconGlyphs.Search, Command = this.BrowsePluginsCommand },
            new MenuAction { Label = "Open Plugins Folder", Glyph = IconGlyphs.FolderOpen, Command = this.OpenPluginsFolderCommand },
            new MenuAction { Label = "Reload Catalog", Glyph = IconGlyphs.SyncFolder, Command = this.ReloadCatalogCommand }
        ];
    }

    /// <inheritdoc />
    public ObservableCollection<AgentCardItem> Cards { get; } = new();

    /// <summary>
    ///   Invalid plugin entries, rendered without actions.
    /// </summary>
    public ObservableCollection<InvalidAgentCardItem> InvalidCards { get; } = new();

    /// <summary>
    ///   Plugins disabled in the plugin state file, rendered in the separate Disabled section
    ///   with an Enable action. Populated during <see cref="Load" /> and every catalog reload.
    /// </summary>
    public ObservableCollection<DisabledAgentCardItem> DisabledCards { get; } = new();

    /// <summary>
    ///   Whether any disabled plugin is rendered: gates the Disabled section's visibility.
    /// </summary>
    public bool HasDisabledPlugins => this.DisabledCards.Count > 0;

    /// <summary>
    ///   The plugin origin mix for the AGENTS summary card: how many loaded plugins are
    ///   built-in, installed from packages, and of unknown origin.
    /// </summary>
    public string PluginCatalogSummary
    {
        get
        {
            if (this.Cards.Count == 0)
            {
                return "No agent plugins found";
            }

            var builtin = this.Cards.Count(card => card.Origin == PluginOrigin.Builtin);
            var installed = this.Cards.Count(card => card.Origin == PluginOrigin.Installed);
            var unknown = this.Cards.Count - builtin - installed;

            var parts = new List<string>();
            if (builtin > 0)
            {
                parts.Add($"{builtin} built-in");
            }

            if (installed > 0)
            {
                parts.Add($"{installed} installed");
            }

            if (unknown > 0)
            {
                parts.Add($"{unknown} unknown");
            }

            return string.Join(" · ", parts);
        }
    }

    /// <inheritdoc />
    public int InstalledAgentCount => this.Cards.Count(card => card.IsInstalled);

    /// <summary>
    ///   The number of supported agent plugins.
    /// </summary>
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

    /// <summary>
    ///   Whether a default agent is selected.
    /// </summary>
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
                return "Choose an agent below";
            }

            var version = this.Cards
                .FirstOrDefault(card => card.PluginId == this.CurrentDefaultAgentId)
                ?.Version;

            return version is null
                ? "In the launch window"
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

    /// <summary>
    ///   Installs an agent plugin from a local package: opens the package picker, inspects the
    ///   package, shows the confirmation listing every declared command, and installs after the
    ///   user confirms.
    /// </summary>
    public RelayCommand InstallPluginCommand { get; }

    /// <summary>
    ///   Opens the Browse Plugins dialog showing the remote feed.
    /// </summary>
    public RelayCommand BrowsePluginsCommand { get; }

    /// <summary>
    ///   Disables a plugin: marks it disabled in the plugin state file (no file deletion) and
    ///   reloads the catalog so the card moves to the Disabled section.
    /// </summary>
    public RelayCommand<AgentCardItem> DisablePluginCommand { get; }

    /// <summary>
    ///   Re-enables a disabled plugin: clears the disabled flag in the plugin state file and
    ///   reloads the catalog so the card returns to the active list.
    /// </summary>
    public RelayCommand<DisabledAgentCardItem> EnablePluginCommand { get; }

    /// <summary>
    ///   Opens the plugins folder (<c> %APPDATA%\CLIHub\plugins </c>) in Windows Explorer.
    /// </summary>
    public RelayCommand OpenPluginsFolderCommand { get; }

    /// <summary>
    ///   Reloads the plugin catalog without restarting the application.
    /// </summary>
    public RelayCommand ReloadCatalogCommand { get; }

    /// <summary>
    ///   The plugin-catalog entries of the AGENTS header's Actions menu: install a plugin from a
    ///   package, browse the remote feed, open the plugins folder, and reload the catalog, in that
    ///   order. Mirrors the launch window's data-driven pane Actions menus; each entry's command is
    ///   the corresponding standalone command, so an entry is enabled exactly when its command can
    ///   execute.
    /// </summary>
    public IReadOnlyList<MenuAction> PluginActions { get; }

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
    public bool CanDetect => !this.IsAnyPassRunning;

    /// <summary>
    ///   Whether an update-availability check pass is currently running.
    /// </summary>
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
    public bool CanCheckUpdates => !this.IsAnyPassRunning;

    /// <summary>
    ///   Whether any per-card Detect or Check updates action is currently running: true while at
    ///   least one card's action runs, gating the header actions. Not exposed on
    ///   <see cref="IAgentManagementSection" />, which carries only what its consumers bind and
    ///   read; tests assert it through this concrete view model. Distinct from
    ///   <see cref="IsDetecting" /> and <see cref="IsCheckingUpdates" />, the header-wide pass
    ///   flags.
    /// </summary>
    public bool IsCardActionRunning => this.runningCardActionCount > 0;

    /// <summary>
    ///   Whether any pass or per-card action is currently running: true while Detect, Check updates, or a per-card
    ///   action runs.
    /// </summary>
    private bool IsAnyPassRunning => this.IsDetecting || this.IsCheckingUpdates || this.IsCardActionRunning;

    /// <summary>
    ///   Enters a per-card action: increments the running-action counter and reports the change
    ///   so the header actions re-query while any card action runs. Raises on every mutation,
    ///   not only on 0↔1 transitions — a raise when the value did not change is a harmless
    ///   re-query. A plain <see cref="int" /> counter is safe here: increments happen
    ///   synchronously in the command execute delegates and decrements in the per-card
    ///   action scope's disposal, both on the UI thread.
    /// </summary>
    private void EnterCardAction()
    {
        this.runningCardActionCount++;
        this.RaiseCardActionCountChanges();
    }

    /// <summary>
    ///   Exits a per-card action: decrements the running-action counter and reports the change.
    ///   Called from the per-card action scope's disposal, so the counter resets on every path,
    ///   including failures.
    /// </summary>
    private void ExitCardAction()
    {
        this.runningCardActionCount--;
        this.RaiseCardActionCountChanges();
    }

    private void RaiseCardActionCountChanges()
    {
        this.OnPropertyChanged(nameof(this.IsCardActionRunning));
        this.RaiseHeaderGateChanges();
    }

    /// <summary>
    ///   Whether the per-card Detect action may run for the card: the card is idle (neither of
    ///   its own "Detect" and "Check updates" actions is running) and no header-wide pass is
    ///   running. Card-local and header flags only — other cards' actions and the running-action
    ///   counter never gate a card's own commands, so per-card actions stay independent across
    ///   cards.
    /// </summary>
    /// <param name="card"> The card to check. </param>
    /// <returns> Whether the command may execute for the card. </returns>
    private bool CanDetectCard(AgentCardItem card) => card.CanDetect && !this.IsDetecting && !this.IsCheckingUpdates;

    /// <summary>
    ///   Whether the per-card Check updates action may run for the card: the card is checkable
    ///   (installed, neither of its own "Detect" and "Check updates" actions is running) and no
    ///   header-wide pass is running. Card-local and header flags only — other cards' actions and
    ///   the running-action counter never gate a card's own commands, so per-card actions stay
    ///   independent across cards.
    /// </summary>
    /// <param name="card"> The card to check. </param>
    /// <returns> Whether the command may execute for the card. </returns>
    private bool CanCheckCardUpdates(AgentCardItem card) => card.CanCheckUpdates && !this.IsDetecting && !this.IsCheckingUpdates;

    /// <inheritdoc />
    public string? UpdateCheckSummary
    {
        get;
        private set => this.SetProperty(ref field, value);
    }

    /// <inheritdoc />
    public int UpdatesAvailableCount => this.Cards.Count(card => card.HasUpdateAvailable);

    /// <inheritdoc />
    public string UpdatesValue => this.UpdatesAvailableCount > 0
        ? this.UpdatesAvailableCount.ToString()
        : this.AllInstalledAgentsUpToDate ? "None" : "Unknown";

    /// <inheritdoc />
    public string UpdatesDetail
    {
        get
        {
            if (this.UpdatesAvailableCount > 0)
            {
                var installed = this.InstalledAgentCount;
                var noun = installed == 1 ? "agent" : "agents";
                return this.UpdatesAvailableCount == 1
                    ? $"1 of {installed} {noun} has an update"
                    : $"{this.UpdatesAvailableCount} of {installed} {noun} have updates";
            }

            if (this.InstalledAgentCount == 0)
            {
                return "No installed agents";
            }

            return this.AllInstalledAgentsUpToDate ? "All installed agents up to date" : "Check updates";
        }
    }

    /// <inheritdoc />
    public SummaryCardTone UpdatesValueTone => this.UpdatesAvailableCount > 0
        ? SummaryCardTone.Accent
        : this.AllInstalledAgentsUpToDate ? SummaryCardTone.Success : SummaryCardTone.Muted;

    /// <summary>
    ///   Whether every installed agent's last update-availability check reported
    ///   <see cref="AgentUpdateStatus.UpToDate" />. Vacuously true when no agent is installed, so
    ///   the UPDATES card reads "None" rather than "Unknown" in that case.
    /// </summary>
    private bool AllInstalledAgentsUpToDate =>
        this.Cards
            .Where(card => card.IsInstalled)
            .All(card => card.UpdateAvailability?.Status == AgentUpdateStatus.UpToDate);

    /// <inheritdoc />
    public void Load(string? defaultAgentId)
    {
        if (this.disposed)
        {
            return;
        }

        this.UpdateCheckSummary = null;
        this.Cards.Clear();
        this.InvalidCards.Clear();
        this.DisabledCards.Clear();

        var plugins = this.catalog.GetAllPlugins();
        foreach (var entry in this.catalog.GetInvalidPlugins())
        {
            this.InvalidCards.Add(new InvalidAgentCardItem(entry));
        }

        foreach (var disabled in this.catalog.GetDisabledPlugins())
        {
            this.DisabledCards.Add(new DisabledAgentCardItem(disabled));
        }

        // Origin metadata comes from the plugin state file, not the descriptor: a plugin with
        // no state entry renders with the neutral Unknown badge.
        var pluginState = this.pluginState.Load();

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

            var stateEntry = pluginState.Plugins.GetValueOrDefault(plugin.Id);

            var card = new AgentCardItem(
                plugin,
                plugin.LogoPath,
                isInstalled,
                null,
                AgentConfigurationPath.For(plugin))
            {
                CanUpdate = declaresUpdate,
                Origin = stateEntry?.Origin,
                InstalledVersion = stateEntry?.InstalledVersion
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
        this.versionPopulationPass.Dispose();
        this.blockedStatePass.Dispose();
        this.catalog.PluginsChanged -= this.OnPluginsChanged;
        this.disposed = true;
    }

    private void OnPluginsChanged(object? sender, EventArgs e)
    {
        this.Load(this.CurrentDefaultAgentId);
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
        this.OnPropertyChanged(nameof(this.UpdatesAvailableCount));
        this.OnPropertyChanged(nameof(this.UpdatesValue));
        this.OnPropertyChanged(nameof(this.UpdatesDetail));
        this.OnPropertyChanged(nameof(this.UpdatesValueTone));
        this.OnPropertyChanged(nameof(this.HasDisabledPlugins));
        this.OnPropertyChanged(nameof(this.PluginCatalogSummary));
    }

    /// <summary>
    ///   Reports a busy-state flip to the UI: the derived availability properties change, and the
    ///   gated commands re-query explicitly. WPF buttons cache the
    ///   <see cref="System.Windows.Input.ICommand.CanExecute" />
    ///   result and only re-query on <c> CanExecuteChanged </c>, so without the explicit raise a
    ///   button that re-queried mid-pass would stay disabled after the pass ends.
    /// </summary>
    private void RaiseBusyStateChanges()
    {
        this.RaiseHeaderGateChanges();
        this.DetectCardCommand.RaiseCanExecuteChanged();
        this.CheckCardUpdatesCommand.RaiseCanExecuteChanged();
    }

    /// <summary>
    ///   Raises property-changed and command-changed notifications for the header-wide gates.
    /// </summary>
    private void RaiseHeaderGateChanges()
    {
        this.OnPropertyChanged(nameof(this.CanDetect));
        this.OnPropertyChanged(nameof(this.CanCheckUpdates));
        this.DetectCommand.RaiseCanExecuteChanged();
        this.CheckUpdatesCommand.RaiseCanExecuteChanged();
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
        var lease = this.versionPopulationPass.Begin();

        var cards = this.Cards.ToList();

        _ = this.RunAsync(
            "Agent version population",
            applicationToken => this.PopulateVersionsAsync(cards, lease.Generation, lease.RefreshToken, applicationToken));
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
                    if (this.versionPopulationPass.IsCurrent(generation, cancellationToken))
                    {
                        card.UpdateAvailability = null;
                    }

                    return;
                }

                var ver = await this.version.GetVersionAsync(card.Plugin, cancellationToken);
                if (this.versionPopulationPass.IsCurrent(generation, cancellationToken))
                {
                    card.Version = ver;
                }

                // The availability check reuses the just-probed version through the version
                // service's cache, so this adds no extra process launch per card.
                var availability = await this.availability.CheckAsync(card.Plugin, cancellationToken);
                if (this.versionPopulationPass.IsCurrent(generation, cancellationToken))
                {
                    card.UpdateAvailability = availability;
                }
            }));

            if (this.versionPopulationPass.IsCurrent(generation, cancellationToken))
            {
                this.RefreshAvailabilitySummary();
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
        var lease = this.blockedStatePass.Begin();

        var cards = this.Cards.ToList();

        _ = this.RunAsync(
            "Agent blocked-state population",
            applicationToken => this.PopulateBlockedStateAsync(cards, lease.Generation, lease.RefreshToken, applicationToken));
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

                if (this.blockedStatePass.IsCurrent(generation, cancellationToken))
                {
                    card.CanUpdate = declaresUpdate && running.Count == 0;

                    // Write the block explanation only when blocked; never clear a message a
                    // user action may have set between render and pass completion.
                    if (running.Count > 0)
                    {
                        card.Message = "Agent is running — stop it to update";
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
        using var scope = new CardActionScope(this, busy => card.IsDetecting = busy);
        var previousState = card.InstallState;
        card.InstallState = AgentInstallState.Detecting;
        try
        {
            var result = await this.probe.ProbeAsync(card.Plugin, cancellationToken);
            this.ApplyProbeResult(card, result);
            card.Message = (result.IsInstalled, result.Version) switch
            {
                (true, { } version) => $"Installed · v{version}",
                (true, null) => "Installed",
                _ => "Not installed on this PC"
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
            var previousStates = cards.ToDictionary(
                card => card,
                card => (InstallState: card.InstallState, card.Version, card.UpdateAvailability));

            // Cards this pass has already committed a fresh probe result to. A committed result
            // describes the host right now, so it is never rolled back to the pre-pass snapshot.
            var resolved = new HashSet<AgentCardItem>();

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
                    var result = await this.probe.ProbeAsync(card.Plugin, cancellationToken);
                    if (this.disposed)
                    {
                        // The pass is abandoned mid-flight: settle it exactly like a thrown probe,
                        // so no card is left announcing a probe that will never complete.
                        this.SettleAbortedDetectPass(cards, resolved, previousStates);
                        return;
                    }

                    this.ApplyProbeResult(card, result);
                    resolved.Add(card);
                }

                this.RefreshAvailabilitySummary();
                this.RebuildDefaultAgentOptions(this.SelectedDefaultAgent?.Id);
            }
            catch (Exception)
            {
                // The pass is abandoning: settle every card it had flipped to Detecting back to
                // its pre-pass state so no chip stays stuck, then let the caller log the failure.
                this.SettleAbortedDetectPass(cards, resolved, previousStates);

                // The cards changed before the pass abandoned, so the derived surfaces must be
                // rebuilt too: the header counts and the default-agent options otherwise keep
                // describing the pre-pass state — a selector option reading "(Not installed)"
                // next to a summary card that already reports a version. A disposed surface is
                // gone, so it is left alone.
                if (!this.disposed)
                {
                    this.RefreshAvailabilitySummary();
                    this.RebuildDefaultAgentOptions(this.SelectedDefaultAgent?.Id);
                }

                throw;
            }
        }
        finally
        {
            this.IsDetecting = false;
        }
    }

    /// <summary>
    ///   Settles an aborted header-wide detection pass: every card the pass had not committed a
    ///   fresh probe result to is restored to its complete pre-pass state — installation state,
    ///   version, and update-availability indicator together — so nothing is left announcing a
    ///   probe that will never complete and no chip disagrees with its shown version. A card the
    ///   pass already resolved keeps its fresh result: a committed probe result is never rolled
    ///   back to the state the host had before the pass started.
    /// </summary>
    /// <param name="cards"> The card snapshot the aborted pass worked through. </param>
    /// <param name="resolved"> The cards the pass already applied a fresh probe result to. </param>
    /// <param name="previousStates"> The pre-pass state of every card in the snapshot. </param>
    private void SettleAbortedDetectPass(
        IReadOnlyList<AgentCardItem> cards,
        IReadOnlySet<AgentCardItem> resolved,
        IReadOnlyDictionary<AgentCardItem, (AgentInstallState InstallState, string? Version, AgentUpdateAvailability? UpdateAvailability)> previousStates)
    {
        foreach (var card in cards)
        {
            if (resolved.Contains(card))
            {
                continue;
            }

            var (installState, version, updateAvailability) = previousStates[card];
            card.InstallState = installState;
            card.Version = version;
            card.UpdateAvailability = updateAvailability;
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

            try
            {
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
                    this.RefreshAvailabilitySummary();
                }
            }
            // Cancellation is not a failure: the exception still propagates (so the operation
            // boundary logs it as cancelled), but it never writes the failure summary.
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // The pass is abandoning. Leaving the summary cleared would read as "nothing to
                // report", so replace it with an explicit failure line. Cards the pass already
                // settled keep their fresh result; cards it never reached keep their pre-pass
                // indicator, which the next check re-fetches because the cache was invalidated.
                // Rethrow so the shared operation boundary still logs the failure.
                if (!this.disposed)
                {
                    this.UpdateCheckSummary = UpdateCheckFailureSummary;
                    this.RefreshAvailabilitySummary();
                }

                throw;
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
            card.Message = "Not installed on this PC";
            this.RefreshAvailabilitySummary();
            return;
        }

        using var scope = new CardActionScope(this, busy => card.IsCheckingUpdates = busy);
        // Invalidate first so the check below bypasses the cached result and its TTL.
        this.availability.InvalidateFor(card.Plugin);
        var availability = await this.availability.CheckAsync(card.Plugin, cancellationToken);
        if (!this.disposed)
        {
            card.UpdateAvailability = availability;
            card.Message = availability.Status switch
            {
                AgentUpdateStatus.UpdateAvailable => "Update available",
                AgentUpdateStatus.UpToDate => "Up to date",
                _ => "Update status unknown"
            };
            this.RefreshAvailabilitySummary();
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
        var result = await this.update.UpdateAsync(card.Plugin, cancellationToken);
        card.Message = result.Outcome switch
        {
            AgentUpdateOutcome.Started => "Update started",
            _ => result.Message ?? "Update failed"
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

    /// <summary>
    ///   Runs the plugin installation workflow: pick a package, inspect it without writes,
    ///   confirm with the user (every declared command is listed), then install. Validation
    ///   failures surface as an error dialog and stop before any file is written.
    /// </summary>
    private void InstallPlugin()
    {
        var packagePath = this.pluginDialogs.PickPluginPackage();
        if (string.IsNullOrWhiteSpace(packagePath))
        {
            return;
        }

        var preview = this.pluginInstaller.Inspect(packagePath);
        if (!preview.IsValid)
        {
            this.pluginDialogs.ShowValidationError(preview.Errors);
            return;
        }

        if (!this.pluginDialogs.ShowInstallConfirmation(preview))
        {
            return;
        }

        var result = this.pluginInstaller.Install(packagePath, replaceExisting: preview.ReplacesExistingPlugin);
        if (result.Success)
        {
            this.pluginDialogs.ShowInstallOutcome(result, preview.PluginName);
        }
        else
        {
            this.pluginDialogs.ShowValidationError(result.Errors);
        }
    }

    /// <summary>
    ///   Marks a plugin disabled in the state file (files stay on disk) and reloads the catalog
    ///   so the plugin leaves the active list and appears in the Disabled section.
    /// </summary>
    /// <param name="card"> The card of the plugin to disable. </param>
    private void DisablePlugin(AgentCardItem card)
    {
        try
        {
            this.UpdatePluginStateEntry(card.PluginId, entry => entry.Disabled = true);
            this.catalog.ReloadPlugins();
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Could not disable plugin {PluginId}", card.PluginId);
            card.Message = "Could not disable the plugin";
        }
    }

    /// <summary>
    ///   Clears a plugin's disabled flag in the state file and reloads the catalog so the
    ///   plugin returns to the active list without re-installation.
    /// </summary>
    /// <param name="card"> The disabled card of the plugin to enable. </param>
    private void EnablePlugin(DisabledAgentCardItem card)
    {
        try
        {
            this.UpdatePluginStateEntry(card.PluginId, entry => entry.Disabled = false);
            this.catalog.ReloadPlugins();
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Could not enable plugin {PluginId}", card.PluginId);
        }
    }

    /// <summary>
    ///   Opens the Browse Plugins dialog.
    /// </summary>
    private void BrowsePlugins()
    {
        try
        {
            this.pluginDialogs.ShowBrowsePluginsDialog();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Could not open Browse Plugins dialog");
        }
    }

    /// <summary>
    ///   Whether the Browse Plugins command is available (feed is not disabled).
    /// </summary>
    private bool CanBrowsePlugins() => FeedUrlProvider.GetFeedUrl(this.preferencesStore) is not null;

    /// <summary>
    ///   Opens the plugins folder in Windows Explorer through the external launcher.
    /// </summary>
    private void OpenPluginsFolder()
    {
        try
        {
            this.launcher.Open(AppPaths.PluginsDirectory);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Could not open the plugins folder {Path}", AppPaths.PluginsDirectory);
        }
    }

    /// <summary>
    ///   Reloads the plugin catalog on demand; the PluginsChanged notification rebuilds the
    ///   cards, including any newly installed, modified, or removed plugins.
    /// </summary>
    private void ReloadCatalog() => this.catalog.ReloadPlugins();

    /// <summary>
    ///   Applies a mutation to one plugin's lifecycle state entry, creating the entry when the
    ///   plugin has none yet, and persists the state document.
    /// </summary>
    /// <param name="pluginId"> The plugin id whose entry is updated. </param>
    /// <param name="update"> The mutation applied to the entry. </param>
    private void UpdatePluginStateEntry(string pluginId, Action<PluginStateEntry> update)
    {
        var document = this.pluginState.Load();
        var entry = document.Plugins.GetValueOrDefault(pluginId) ?? new PluginStateEntry(PluginOrigin.Unknown);
        update(entry);
        document.Plugins[pluginId] = entry;
        this.pluginState.Save(document);
    }

    private Task RunAsync(string operationName, Func<CancellationToken, Task> operation) =>
        this.operationLifetime.RunAsync(operationName, operation);

    private sealed class CardActionScope : IDisposable
    {
        private readonly AgentManagementViewModel owner;
        private readonly Action<bool> setBusy;
        private bool disposed;

        public CardActionScope(AgentManagementViewModel owner, Action<bool> setBusy)
        {
            this.owner = owner;
            this.setBusy = setBusy;
            setBusy(true);
            owner.EnterCardAction();
            owner.DetectCardCommand.RaiseCanExecuteChanged();
            owner.CheckCardUpdatesCommand.RaiseCanExecuteChanged();
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            // Clear the card's busy flag BEFORE the completion raises: WPF caches the
            // CanExecute result it queries during CanExecuteChanged, so a raise that still
            // sees the card busy would cache false and leave the button disabled until an
            // unrelated input event re-queries it.
            this.setBusy(false);
            this.owner.ExitCardAction();
            this.owner.DetectCardCommand.RaiseCanExecuteChanged();
            this.owner.CheckCardUpdatesCommand.RaiseCanExecuteChanged();
        }
    }
}
