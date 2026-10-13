namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

using CLIHub.Core.Agents;
using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Lifetime;

/// <summary>
///   Owns agent composition, filtering, selection identity, cache invalidation, and versions.
/// </summary>
public sealed class AgentPaneController : IDisposable
{
    private static readonly string DefaultAgentLogoPath =
        Path.Combine(AppContext.BaseDirectory, "default-project.png");

    private readonly IPluginCatalog pluginCatalog;
    private readonly IAgentDetectionService agentDetectionService;
    private readonly IAgentVersionService agentVersionService;
    private readonly ILogoCacheService logoCacheService;
    private readonly IApplicationOperationLifetime operationLifetime;
    private CancellationTokenSource? versionPopulationCts;
    private int versionPopulationGeneration;
    private string? currentProjectPath;
    private bool showOnlyProjectAgents;
    private bool disposed;

    /// <summary>
    ///   Creates the agent-pane workflow boundary.
    /// </summary>
    /// <param name="pluginCatalog"> Plugin catalog supplying agent descriptors. </param>
    /// <param name="agentDetectionService"> Service supplying host and project availability. </param>
    /// <param name="agentVersionService"> Service resolving agent versions. </param>
    /// <param name="logoCacheService"> Logo cache used while composing rows. </param>
    /// <param name="operationLifetime"> Application lifetime for tracked version work. </param>
    public AgentPaneController(
        IPluginCatalog pluginCatalog,
        IAgentDetectionService agentDetectionService,
        IAgentVersionService agentVersionService,
        ILogoCacheService logoCacheService,
        IApplicationOperationLifetime operationLifetime)
    {
        this.pluginCatalog = pluginCatalog ?? throw new ArgumentNullException(nameof(pluginCatalog));
        this.agentDetectionService = agentDetectionService ?? throw new ArgumentNullException(nameof(agentDetectionService));
        this.agentVersionService = agentVersionService ?? throw new ArgumentNullException(nameof(agentVersionService));
        this.logoCacheService = logoCacheService ?? throw new ArgumentNullException(nameof(logoCacheService));
        this.operationLifetime = operationLifetime ?? throw new ArgumentNullException(nameof(operationLifetime));
        this.pluginCatalog.PluginsChanged += this.OnPluginsChanged;

        this.RefreshCommand = new RelayCommand(() => _ = this.Refresh(this.currentProjectPath, this.showOnlyProjectAgents));
    }

    /// <summary>
    ///   Agents currently shown in the agent pane.
    /// </summary>
    public ObservableCollection<AgentItem> Agents { get; } = new();

    /// <summary>
    ///   Command to refresh the agent list with current project path and filter settings.
    /// </summary>
    public RelayCommand RefreshCommand { get; }

    /// <summary>
    ///   The selected agent row, if one remains after the last refresh.
    /// </summary>
    public AgentItem? SelectedAgent { get; private set; }

    /// <summary>
    ///   Updates the selected agent row.
    /// </summary>
    /// <param name="agent"> Agent row to select, or null to clear selection. </param>
    public void Select(AgentItem? agent) => this.SelectedAgent = agent;

    /// <summary>
    ///   Rebuilds agent rows and starts cancellable version population.
    /// </summary>
    /// <param name="currentProjectPath"> Current project path, if selected. </param>
    /// <param name="showOnlyProjectAgents"> Whether unavailable project agents are filtered. </param>
    /// <returns> True when at least one agent row is available. </returns>
    public bool Refresh(string? currentProjectPath, bool showOnlyProjectAgents)
    {
        if (this.disposed)
        {
            return false;
        }

        this.currentProjectPath = currentProjectPath;
        this.showOnlyProjectAgents = showOnlyProjectAgents;
        this.versionPopulationCts?.Cancel();
        this.versionPopulationCts?.Dispose();
        this.versionPopulationCts = new CancellationTokenSource();
        var generation = ++this.versionPopulationGeneration;
        var refreshCancellationToken = this.versionPopulationCts.Token;

        var entries = AgentListComposer.Compose(
            this.pluginCatalog.GetAllPlugins(),
            this.agentDetectionService,
            currentProjectPath,
            showOnlyProjectAgents);
        var selectedPluginId = this.SelectedAgent?.Plugin.Id;

        this.SelectedAgent = AgentListSynchronizer.Synchronize(
            this.Agents,
            entries,
            DefaultAgentLogoPath,
            selectedPluginId);

        if (this.Agents.Count > 0)
        {
            _ = this.operationLifetime.RunAsync(
                "Agent version population",
                applicationToken => this.PopulateVersionsAsync(
                    this.Agents.ToList(),
                    generation,
                    refreshCancellationToken,
                    applicationToken));
        }

        return this.Agents.Count > 0;
    }

    /// <summary>
    ///   Invalidates detection, version, and logo caches before the next refresh.
    /// </summary>
    public void InvalidateCaches()
    {
        this.logoCacheService.InvalidateAll();
        this.agentVersionService.Invalidate();
        this.agentDetectionService.Invalidate();
    }

    private void OnPluginsChanged(object? sender, EventArgs e)
    {
        if (this.disposed)
        {
            return;
        }

        this.InvalidateCaches();
        this.Refresh(this.currentProjectPath, this.showOnlyProjectAgents);
    }

    /// <summary>
    ///   Unsubscribes from plugin reloads and cancels owned version population.
    /// </summary>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.versionPopulationGeneration++;
        this.versionPopulationCts?.Cancel();
        this.versionPopulationCts?.Dispose();
        this.versionPopulationCts = null;
        this.pluginCatalog.PluginsChanged -= this.OnPluginsChanged;
    }

    private async Task PopulateVersionsAsync(
        IReadOnlyList<AgentItem> items,
        int generation,
        CancellationToken refreshCancellationToken,
        CancellationToken applicationCancellationToken)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            refreshCancellationToken,
            applicationCancellationToken);
        var cancellationToken = cancellation.Token;

        try
        {
            await Task.WhenAll(items.Select(async item =>
            {
                var version = await this.agentVersionService.GetVersionAsync(item.Plugin, cancellationToken);
                if (!cancellationToken.IsCancellationRequested && generation == this.versionPopulationGeneration)
                {
                    item.Version = version ?? "unknown";
                }
            }));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Agent version population failed: {ex}");
        }
    }
}
