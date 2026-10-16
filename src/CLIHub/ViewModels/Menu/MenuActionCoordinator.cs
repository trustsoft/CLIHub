namespace CLIHub.ViewModels;

using System.ComponentModel;
using System.Windows.Input;

/// <summary>
///   Coordinates menu action construction and filter synchronization for the launch window.
/// </summary>
public sealed class MenuActionCoordinator : ObservableObject, IDisposable
{
    private readonly MenuAction filterAction;
    private bool disposed;

    /// <summary>
    ///   Actions displayed in the Projects pane.
    /// </summary>
    public IReadOnlyList<MenuAction> ProjectsActions { get; }

    /// <summary>
    ///   Actions displayed in the Agents pane.
    /// </summary>
    public IReadOnlyList<MenuAction> AgentsActions { get; }

    /// <summary>
    ///   Current state of the "Show only project agents" filter.
    ///   Reflects changes from the menu action and notifies subscribers.
    /// </summary>
    public bool ShowOnlyProjectAgents
    {
        get => this.filterAction.IsChecked;
        set
        {
            if (this.filterAction.IsChecked != value)
            {
                this.filterAction.IsChecked = value;
            }
        }
    }

    /// <summary>
    ///   Creates the coordinator and builds both action menus from the provided commands.
    /// </summary>
    /// <param name="addProject"> Command for adding a new project. </param>
    /// <param name="removeProject"> Command for removing the selected project. </param>
    /// <param name="toggleFavorite"> Command for toggling favorite status. </param>
    /// <param name="refresh"> Command for refreshing the project list. </param>
    /// <param name="launch"> Command for launching the selected agent. </param>
    /// <param name="resume"> Command for resuming an agent in the selected project. </param>
    /// <param name="update"> Command for updating the selected agent. </param>
    /// <param name="showVersion"> Command for showing agent version. </param>
    /// <param name="actionBuilder"> Builder for constructing the action menu structures. </param>
    /// <param name="initialFilterState"> Initial value for the "Show only project agents" filter. </param>
    public MenuActionCoordinator(ICommand addProject,
                                 ICommand removeProject,
                                 ICommand toggleFavorite,
                                 ICommand refresh,
                                 ICommand launch,
                                 ICommand resume,
                                 ICommand update,
                                 ICommand showVersion,
                                 LaunchWindowActionBuilder actionBuilder,
                                 bool initialFilterState)
    {
        ArgumentNullException.ThrowIfNull(addProject);
        ArgumentNullException.ThrowIfNull(removeProject);
        ArgumentNullException.ThrowIfNull(toggleFavorite);
        ArgumentNullException.ThrowIfNull(refresh);
        ArgumentNullException.ThrowIfNull(launch);
        ArgumentNullException.ThrowIfNull(resume);
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(showVersion);
        ArgumentNullException.ThrowIfNull(actionBuilder);

        var actions = actionBuilder.Build(addProject,
                                          removeProject,
                                          toggleFavorite,
                                          refresh,
                                          launch,
                                          resume,
                                          update,
                                          showVersion,
                                          initialFilterState);

        this.ProjectsActions = actions.Projects;
        this.AgentsActions = actions.Agents;
        this.filterAction = actions.FilterAction;

        this.filterAction.PropertyChanged += this.OnFilterActionChanged;
    }

    /// <summary>
    ///   Propagates filter action changes to the ShowOnlyProjectAgents property.
    /// </summary>
    private void OnFilterActionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MenuAction.IsChecked))
        {
            this.OnPropertyChanged(nameof(this.ShowOnlyProjectAgents));
        }
    }

    /// <summary>
    ///   Unsubscribes from filter action change events.
    /// </summary>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.filterAction.PropertyChanged -= this.OnFilterActionChanged;
    }
}
