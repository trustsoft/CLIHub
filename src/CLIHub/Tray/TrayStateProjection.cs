namespace CLIHub.Tray;

using CLIHub.Core.Plugins;
using CLIHub.Core.Projects;
using CLIHub.Updates;

/// <summary>
///   Projects project, plugin, and update facts into the immutable tray menu state.
/// </summary>
public sealed class TrayStateProjection : IDisposable
{
    private readonly IProjectService projects;
    private readonly IPluginCatalog pluginCatalog;
    private readonly IUpdateWorkflow updates;
    private bool disposed;

    /// <summary>
    ///   Raised when one of the projected source values changes.
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    ///   Creates a tray state projection over project, plugin, and update sources.
    /// </summary>
    /// <param name="projects"> Project state source. </param>
    /// <param name="pluginCatalog"> Current plugin catalog. </param>
    /// <param name="updates"> Shared application update workflow. </param>
    public TrayStateProjection(
        IProjectService projects,
        IPluginCatalog pluginCatalog,
        IUpdateWorkflow updates)
    {
        this.projects = projects ?? throw new ArgumentNullException(nameof(projects));
        this.pluginCatalog = pluginCatalog ?? throw new ArgumentNullException(nameof(pluginCatalog));
        this.updates = updates ?? throw new ArgumentNullException(nameof(updates));

        this.projects.ProjectsChanged += this.OnSourceStateChanged;
        this.pluginCatalog.PluginsChanged += this.OnSourceStateChanged;
        this.updates.UpdateStateChanged += this.OnSourceStateChanged;
    }

    /// <summary>
    ///   Gets whether a manual update check is currently running.
    /// </summary>
    public bool IsCheckingForUpdates => this.updates.IsCheckingForUpdates;

    /// <summary>
    ///   Builds a snapshot from the current project, plugin, and update state.
    /// </summary>
    public TrayMenuState GetState() => new(
        this.projects.GetCurrentProject(),
        this.projects.GetRecentProjects(10),
        this.pluginCatalog.GetAllPlugins().Where(plugin => plugin.Commands?.Launch is not null).ToArray(),
        this.updates.LastKnownAvailableVersion,
        this.updates.IsDownloading,
        this.IsCheckingForUpdates);

    /// <summary>
    ///   Persists the selected project from a recent-project menu action.
    /// </summary>
    /// <param name="projectId"> ID of the selected project. </param>
    public void SetCurrentProject(string projectId) => this.projects.SetCurrentProject(projectId);

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

    private void OnSourceStateChanged(object? sender, EventArgs e) => StateChanged?.Invoke(this, EventArgs.Empty);
}
