namespace CLIHub.Tray;

using CLIHub.Core.Models;

/// <summary>
///   The configured default and the initialized agents of one favorite project.
/// </summary>
/// <param name="Project"> The favorite project the tray submenu represents. </param>
/// <param name="DefaultAgent"> The catalog plugin resolved from the configured default agent id, or null when no default is configured or resolvable. </param>
/// <param name="InitializedAgents"> Plugins initialized for the project: they expose a launch command, are installed in the system, and are available in the project. </param>
public sealed record TrayProjectAgents(Project Project,
                                       Plugin? DefaultAgent,
                                       IReadOnlyList<Plugin> InitializedAgents);

/// <summary>
///   Snapshot of the application state projected into the tray menu.
/// </summary>
/// <param name="CurrentProject"> Currently selected project, if any. </param>
/// <param name="FavoriteProjects"> The default and initialized agents of every favorite project, in storage order. </param>
/// <param name="AvailableUpdateVersion"> Available update version, if any. </param>
/// <param name="IsDownloadingUpdate"> Whether an update download is active. </param>
/// <param name="IsCheckingForUpdates"> Whether an update check is active. </param>
public sealed record TrayMenuState(Project? CurrentProject,
                                   IReadOnlyList<TrayProjectAgents> FavoriteProjects,
                                   string? AvailableUpdateVersion,
                                   bool IsDownloadingUpdate,
                                   bool IsCheckingForUpdates);

/// <summary>
///   Application commands projected into the tray menu.
/// </summary>
/// <param name="SelectProjectAndLaunchAgentAsync"> Makes a project current and launches an agent in it. </param>
/// <param name="AddProject"> Opens the project-folder workflow. </param>
/// <param name="ShowSettings"> Shows Settings. </param>
/// <param name="ShowReleaseNotes"> Shows What's New. </param>
/// <param name="CheckForUpdates"> Checks for an available update. </param>
/// <param name="RequestUpdateDownload"> Requests an update download. </param>
/// <param name="Exit"> Exits the application. </param>
public sealed record TrayMenuCommands(Func<string, Plugin, Task> SelectProjectAndLaunchAgentAsync,
                                      Action AddProject,
                                      Action ShowSettings,
                                      Action ShowReleaseNotes,
                                      Action CheckForUpdates,
                                      Action RequestUpdateDownload,
                                      Action Exit);
