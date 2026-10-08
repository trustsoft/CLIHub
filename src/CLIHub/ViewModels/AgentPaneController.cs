namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

using CLIHub.Core.Agents;
using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;

/// <summary>
///   Owns agent composition, filtering, selection identity, cache invalidation, and versions.
/// </summary>
public sealed class AgentPaneController : IDisposable
{
    private static readonly string DefaultAgentLogoPath =
        Path.Combine(AppContext.BaseDirectory, "default-project.png");

    private readonly IPluginCatalog _pluginCatalog;
    private readonly IAgentDetectionService _agentDetectionService;
    private readonly IAgentVersionService _agentVersionService;
    private readonly ILogoCacheService _logoCacheService;
    private readonly IApplicationOperationLifetime _operationLifetime;
    private CancellationTokenSource? _versionPopulationCts;
    private int _versionPopulationGeneration;
    private string? _currentProjectPath;
    private bool _showOnlyProjectAgents;
    private bool _disposed;

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
        _pluginCatalog = pluginCatalog ?? throw new ArgumentNullException(nameof(pluginCatalog));
        _agentDetectionService = agentDetectionService ?? throw new ArgumentNullException(nameof(agentDetectionService));
        _agentVersionService = agentVersionService ?? throw new ArgumentNullException(nameof(agentVersionService));
        _logoCacheService = logoCacheService ?? throw new ArgumentNullException(nameof(logoCacheService));
        _operationLifetime = operationLifetime ?? throw new ArgumentNullException(nameof(operationLifetime));
        _pluginCatalog.PluginsChanged += OnPluginsChanged;
    }

    /// <summary>
    ///   Agents currently shown in the agent pane.
    /// </summary>
    public ObservableCollection<AgentItem> Agents { get; } = new();

    /// <summary>
    ///   The selected agent row, if one remains after the last refresh.
    /// </summary>
    public AgentItem? SelectedAgent { get; private set; }

    /// <summary>
    ///   Updates the selected agent row.
    /// </summary>
    /// <param name="agent"> Agent row to select, or null to clear selection. </param>
    public void Select(AgentItem? agent) => SelectedAgent = agent;

    /// <summary>
    ///   Rebuilds agent rows and starts cancellable version population.
    /// </summary>
    /// <param name="currentProjectPath"> Current project path, if selected. </param>
    /// <param name="showOnlyProjectAgents"> Whether unavailable project agents are filtered. </param>
    /// <returns> True when at least one agent row is available. </returns>
    public bool Refresh(string? currentProjectPath, bool showOnlyProjectAgents)
    {
        if (_disposed)
        {
            return false;
        }

        _currentProjectPath = currentProjectPath;
        _showOnlyProjectAgents = showOnlyProjectAgents;
        _versionPopulationCts?.Cancel();
        _versionPopulationCts?.Dispose();
        _versionPopulationCts = new CancellationTokenSource();
        var generation = ++_versionPopulationGeneration;
        var refreshCancellationToken = _versionPopulationCts.Token;

        var entries = AgentListComposer.Compose(
            _pluginCatalog.GetAllPlugins(),
            _agentDetectionService,
            currentProjectPath,
            showOnlyProjectAgents);
        var selectedPluginId = SelectedAgent?.Plugin.Id;

        SelectedAgent = AgentListSynchronizer.Synchronize(
            Agents,
            entries,
            DefaultAgentLogoPath,
            selectedPluginId);

        if (Agents.Count > 0)
        {
            _ = _operationLifetime.RunAsync(
                "Agent version population",
                applicationToken => PopulateVersionsAsync(
                    Agents.ToList(),
                    generation,
                    refreshCancellationToken,
                    applicationToken));
        }

        return Agents.Count > 0;
    }

    /// <summary>
    ///   Invalidates detection, version, and logo caches before the next refresh.
    /// </summary>
    public void InvalidateCaches()
    {
        _logoCacheService.InvalidateAll();
        _agentVersionService.Invalidate();
        _agentDetectionService.Invalidate();
    }

    private void OnPluginsChanged(object? sender, EventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        InvalidateCaches();
        Refresh(_currentProjectPath, _showOnlyProjectAgents);
    }

    /// <summary>
    ///   Unsubscribes from plugin reloads and cancels owned version population.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _versionPopulationGeneration++;
        _versionPopulationCts?.Cancel();
        _versionPopulationCts?.Dispose();
        _versionPopulationCts = null;
        _pluginCatalog.PluginsChanged -= OnPluginsChanged;
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
                var version = await _agentVersionService.GetVersionAsync(item.Plugin, cancellationToken);
                if (!cancellationToken.IsCancellationRequested && generation == _versionPopulationGeneration)
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
