namespace CLIHub;

using CLIHub.Core.Models;

/// <summary>
///   Snapshot of the application state projected into the tray menu.
/// </summary>
/// <param name="CurrentProject"> Currently selected project, if any. </param>
/// <param name="RecentProjects"> Recently used projects. </param>
/// <param name="LaunchableAgents"> Plugins that expose a launch command. </param>
/// <param name="AvailableUpdateVersion"> Available update version, if any. </param>
/// <param name="IsDownloadingUpdate"> Whether an update download is active. </param>
/// <param name="IsCheckingForUpdates"> Whether an update check is active. </param>
public sealed record TrayMenuState(
    Project? CurrentProject,
    IReadOnlyList<Project> RecentProjects,
    IReadOnlyList<Plugin> LaunchableAgents,
    string? AvailableUpdateVersion,
    bool IsDownloadingUpdate,
    bool IsCheckingForUpdates);

/// <summary>
///   Application commands projected into the tray menu.
/// </summary>
/// <param name="SelectProject"> Selects a recent project. </param>
/// <param name="LaunchAgentAsync"> Launches an agent for a project. </param>
/// <param name="AddProject"> Opens the project-folder workflow. </param>
/// <param name="ShowSettings"> Shows Settings. </param>
/// <param name="ShowReleaseNotes"> Shows What's New. </param>
/// <param name="CheckForUpdates"> Checks for an available update. </param>
/// <param name="RequestUpdateDownload"> Requests an update download. </param>
/// <param name="ShowLaunchWindow"> Shows the launch window. </param>
/// <param name="Exit"> Exits the application. </param>
public sealed record TrayMenuCommands(
    Action<string> SelectProject,
    Func<Plugin, Project, Task> LaunchAgentAsync,
    Action AddProject,
    Action ShowSettings,
    Action ShowReleaseNotes,
    Action CheckForUpdates,
    Action RequestUpdateDownload,
    Action ShowLaunchWindow,
    Action Exit);
