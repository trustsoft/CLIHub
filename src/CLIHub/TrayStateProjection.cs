namespace CLIHub;

using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Projects;

/// <summary>
///   Projects project, plugin, and update facts into the immutable tray menu state.
/// </summary>
public sealed class TrayStateProjection : IDisposable
{
    private readonly IProjectService _projects;
    private readonly IPluginCatalog _pluginCatalog;
    private readonly IUpdateWorkflow _updates;
    private bool _disposed;

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
        _projects = projects ?? throw new ArgumentNullException(nameof(projects));
        _pluginCatalog = pluginCatalog ?? throw new ArgumentNullException(nameof(pluginCatalog));
        _updates = updates ?? throw new ArgumentNullException(nameof(updates));

        _projects.ProjectsChanged += OnSourceStateChanged;
        _pluginCatalog.PluginsChanged += OnSourceStateChanged;
        _updates.UpdateStateChanged += OnSourceStateChanged;
    }

    /// <summary>
    ///   Gets whether a manual update check is currently running.
    /// </summary>
    public bool IsCheckingForUpdates => _updates.IsCheckingForUpdates;

    /// <summary>
    ///   Builds a snapshot from the current project, plugin, and update state.
    /// </summary>
    public TrayMenuState GetState() => new(
        _projects.GetCurrentProject(),
        _projects.GetRecentProjects(10),
        _pluginCatalog.GetAllPlugins().Where(plugin => plugin.Commands?.Launch is not null).ToArray(),
        _updates.LastKnownAvailableVersion,
        _updates.IsDownloading,
        IsCheckingForUpdates);

    /// <summary>
    ///   Persists the selected project from a recent-project menu action.
    /// </summary>
    /// <param name="projectId"> ID of the selected project. </param>
    public void SetCurrentProject(string projectId) => _projects.SetCurrentProject(projectId);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _projects.ProjectsChanged -= OnSourceStateChanged;
        _pluginCatalog.PluginsChanged -= OnSourceStateChanged;
        _updates.UpdateStateChanged -= OnSourceStateChanged;
    }

    private void OnSourceStateChanged(object? sender, EventArgs e) => StateChanged?.Invoke(this, EventArgs.Empty);
}
