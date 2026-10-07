namespace CLIHub.Core.Models;

/// <summary>
///   Application configuration persisted to config.json.
/// </summary>
public class AppConfig
{
    /// <summary>
    ///   List of registered projects.
    /// </summary>
    public List<Project> Projects { get; set; } = new();

    /// <summary>
    ///   User preferences.
    /// </summary>
    public AppPreferences Preferences { get; set; } = new();

    /// <summary>
    ///   ID of the currently selected project (if any).
    /// </summary>
    public string? CurrentProjectId { get; set; }
}

/// <summary>
///   User preferences stored in configuration.
/// </summary>
public class AppPreferences
{
    /// <summary>
    ///   Whether to start CLIHub with Windows.
    /// </summary>
    public bool StartWithWindows { get; set; }

    /// <summary>
    ///   Global hotkey combination (e.g., "Ctrl+Shift+A").
    /// </summary>
    public string Hotkey { get; set; } = "Ctrl+Shift+A";

    /// <summary>
    ///   Legacy path to the terminal executable. Superseded by <see cref="DefaultRuntime"/>;
    ///   kept for backward compatibility and read once to migrate existing configurations.
    /// </summary>
    public string TerminalExecutable { get; set; } = "wt.exe";

    /// <summary>
    ///   Runtime used to launch interactive agent commands.
    /// </summary>
    public RuntimeKind DefaultRuntime { get; set; } = RuntimeKind.WindowsTerminal;

    /// <summary>
    ///   Log level (Debug, Info, Warning, Error).
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    ///   When true, the agent list shows only agents available in the current project.
    ///   When false (default), unavailable agents are shown dimmed.
    /// </summary>
    public bool ShowOnlyProjectAgents { get; set; }

    /// <summary>
    ///   Time-to-live, in minutes, for cached agent detection results.
    ///   Null uses the built-in default.
    /// </summary>
    public int? AgentProbeTtlMinutes { get; set; }

    /// <summary>
    ///   Timeout, in seconds, for a single agent version probe.
    ///   Null uses the built-in default.
    /// </summary>
    public int? AgentProbeTimeoutSeconds { get; set; }

    /// <summary>
    ///   When true (default), the application checks for updates on startup.
    /// </summary>
    public bool CheckForUpdatesOnStartup { get; set; } = true;

    /// <summary>
    ///   When true (default), the main window is shown at startup; when false, the
    ///   application starts in the system tray. Applies to manual and Windows starts.
    /// </summary>
    public bool ShowWindowOnStartup { get; set; } = true;

    /// <summary>
    ///   When true, the launch window stays visible when it loses focus instead of hiding.
    ///   The footer's pin control toggles this.
    /// </summary>
    public bool PinLaunchWindow { get; set; }

    /// <summary>
    ///   How long project paths are shortened in the launch window: "leftTrim" (default) keeps
    ///   the end of the path, "middleEllipsis" keeps both ends.
    /// </summary>
    public string PathDisplayStyle { get; set; } = PathDisplayStyles.LeftTrimToken;

    /// <summary>
    ///   The application version whose release notes the user has already been shown. Null (or
    ///   missing) means the notes have never been shown — a first run, which does not open the
    ///   What's New window automatically. Written by the application; not exposed in Settings.
    /// </summary>
    public string? LastSeenReleaseNotesVersion { get; set; }
}
