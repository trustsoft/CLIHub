namespace CLIHub.ViewModels;

/// <summary>
///   Action entries and the availability-filter entry used by the launch-window panes.
/// </summary>
/// <param name="Projects"> Actions displayed in the Projects pane. </param>
/// <param name="Agents"> Actions displayed in the Agents pane. </param>
/// <param name="FilterAction"> The checkable project-availability filter action. </param>
public sealed record LaunchWindowActions(
    IReadOnlyList<MenuAction> Projects,
    IReadOnlyList<MenuAction> Agents,
    MenuAction FilterAction);
