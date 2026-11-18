namespace CLIHub.Tray;

using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Projects;
using CLIHub.Updates;

/// <summary>
///   Projects project, plugin, preference, and update facts into the immutable tray menu state.
/// </summary>
public sealed class TrayStateProjection : IDisposable
{
    private readonly IProjectService projects;
    private readonly IPluginCatalog pluginCatalog;
    private readonly IUpdateWorkflow updates;
    private readonly IAgentDetectionService detection;
    private readonly IPreferencesStore preferences;
    private bool disposed;

    /// <summary>
    ///   Raised when one of the projected source values changes.
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    ///   Creates a tray state projection over project, plugin, preference, and update sources.
    /// </summary>
    /// <param name="projects"> Project state source. </param>
    /// <param name="pluginCatalog"> Current plugin catalog. </param>
    /// <param name="updates"> Shared application update workflow. </param>
    /// <param name="detection"> Agent installation and project availability source. </param>
    /// <param name="preferences"> Application preferences source. </param>
    public TrayStateProjection(IProjectService projects,
                               IPluginCatalog pluginCatalog,
                               IUpdateWorkflow updates,
                               IAgentDetectionService detection,
                               IPreferencesStore preferences)
    {
        this.projects = projects ?? throw new ArgumentNullException(nameof(projects));
        this.pluginCatalog = pluginCatalog ?? throw new ArgumentNullException(nameof(pluginCatalog));
        this.updates = updates ?? throw new ArgumentNullException(nameof(updates));
        this.detection = detection ?? throw new ArgumentNullException(nameof(detection));
        this.preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));

        this.projects.ProjectsChanged += this.OnSourceStateChanged;
        this.pluginCatalog.PluginsChanged += this.OnSourceStateChanged;
        this.updates.UpdateStateChanged += this.OnSourceStateChanged;
    }

    /// <summary>
    ///   Gets whether a manual update check is currently running.
    /// </summary>
    public bool IsCheckingForUpdates => this.updates.IsCheckingForUpdates;

    /// <summary>
    ///   Builds a snapshot from the current version, project, plugin, preference, and update state.
    /// </summary>
    public TrayMenuState GetState()
    {
        var defaultAgent = this.ResolveDefaultAgent();
        var favoriteProjects = this.projects.GetAllProjects()
            .Where(project => project.IsFavorite)
            .Select(project => new TrayProjectAgents(project, defaultAgent, this.ComposeInitializedAgents(project)))
            .ToArray();

        return new TrayMenuState(
            this.updates.GetCurrentVersion(),
            favoriteProjects,
            this.updates.LastKnownAvailableVersion,
            this.updates.IsDownloading,
            this.IsCheckingForUpdates);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.projects.ProjectsChanged -= this.OnSourceStateChanged;
        this.pluginCatalog.PluginsChanged -= this.OnSourceStateChanged;
        this.updates.UpdateStateChanged -= this.OnSourceStateChanged;
    }

    private void OnSourceStateChanged(object? sender, EventArgs e) => this.StateChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>
    ///   Resolves the configured default agent id against the unfiltered plugin catalog; a blank
    ///   or unknown id yields no default.
    /// </summary>
    /// <returns> The default agent plugin, or null when none is configured or resolvable. </returns>
    private Plugin? ResolveDefaultAgent()
    {
        var defaultAgentId = this.preferences.Load().DefaultAgentId;
        if (string.IsNullOrWhiteSpace(defaultAgentId))
        {
            return null;
        }

        return this.pluginCatalog.GetAllPlugins().FirstOrDefault(plugin => plugin.Id == defaultAgentId);
    }

    /// <summary>
    ///   Composes the agents initialized for a project: plugins that expose a launch command,
    ///   are installed in the system, and are available in the project, in catalog order.
    /// </summary>
    /// <param name="project"> The project the agents must be available in. </param>
    /// <returns> The initialized plugins in catalog order. </returns>
    private IReadOnlyList<Plugin> ComposeInitializedAgents(Project project) =>
        this.pluginCatalog.GetAllPlugins()
            .Where(plugin => plugin.Commands?.Launch is not null
                             && this.detection.IsInstalledInSystem(plugin)
                             && this.detection.IsAvailableInProject(plugin, project.Path))
            .ToArray();
}
