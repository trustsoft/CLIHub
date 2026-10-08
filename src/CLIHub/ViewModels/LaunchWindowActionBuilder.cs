namespace CLIHub.ViewModels;

using System.Windows.Input;

using CLIHub.Themes;

/// <summary>
///   Builds the data-driven Actions menus shown in the launch-window panes.
/// </summary>
public sealed class LaunchWindowActionBuilder
{
    /// <summary>
    ///   Creates the existing Projects and Agents menu entries for the launch screen.
    /// </summary>
    /// <param name="addProject"> Adds a project. </param>
    /// <param name="removeProject"> Removes the selected project. </param>
    /// <param name="toggleFavorite"> Toggles the selected project's favorite state. </param>
    /// <param name="refresh"> Refreshes project and agent state. </param>
    /// <param name="launch"> Launches the selected agent. </param>
    /// <param name="resume"> Resumes the selected agent. </param>
    /// <param name="initialize"> Initializes the selected agent. </param>
    /// <param name="update"> Updates the selected agent. </param>
    /// <param name="showVersion"> Shows the selected agent's version. </param>
    /// <param name="showOnlyProjectAgents"> Initial state of the project-availability filter. </param>
    /// <returns> The pane menu entries and filter action. </returns>
    public LaunchWindowActions Build(
        ICommand addProject,
        ICommand removeProject,
        ICommand toggleFavorite,
        ICommand refresh,
        ICommand launch,
        ICommand resume,
        ICommand initialize,
        ICommand update,
        ICommand showVersion,
        bool showOnlyProjectAgents)
    {
        ArgumentNullException.ThrowIfNull(addProject);
        ArgumentNullException.ThrowIfNull(removeProject);
        ArgumentNullException.ThrowIfNull(toggleFavorite);
        ArgumentNullException.ThrowIfNull(refresh);
        ArgumentNullException.ThrowIfNull(launch);
        ArgumentNullException.ThrowIfNull(resume);
        ArgumentNullException.ThrowIfNull(initialize);
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(showVersion);

        var filterAction = new MenuAction
        {
            Label = "Only agents available in project",
            Glyph = IconGlyphs.Filter,
            IsCheckable = true,
            IsChecked = showOnlyProjectAgents
        };

        return new LaunchWindowActions(
            new MenuAction[]
            {
                new() { Label = "Add Project...", Glyph = IconGlyphs.Add, Command = addProject },
                new() { Label = "Remove Project", Glyph = IconGlyphs.Delete, Command = removeProject },
                new() { Label = "Toggle Favorite", Glyph = IconGlyphs.FavoriteStar, Command = toggleFavorite },
                new() { Label = "Refresh", Glyph = IconGlyphs.Refresh, Command = refresh }
            },
            new MenuAction[]
            {
                new() { Label = "Launch", Glyph = IconGlyphs.Play, Command = launch },
                new() { Label = "Resume Session", Glyph = IconGlyphs.Refresh, Command = resume },
                new() { Label = "Initialize", Glyph = IconGlyphs.Initialize, Command = initialize },
                new() { Label = "Update", Glyph = IconGlyphs.Update, Command = update },
                new() { Label = "Show Version", Glyph = IconGlyphs.Version, Command = showVersion },
                MenuAction.Separator(),
                filterAction,
                MenuAction.Separator(),
                new() { Label = "Refresh", Glyph = IconGlyphs.Refresh, Command = refresh }
            },
            filterAction);
    }
}
