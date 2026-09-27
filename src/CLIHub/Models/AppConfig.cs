namespace CLIHub.Models;

/// <summary>
/// Application configuration persisted to config.json
/// </summary>
public class AppConfig
{
    /// <summary>
    /// List of registered projects
    /// </summary>
    public List<Project> Projects { get; set; } = new();

    /// <summary>
    /// User preferences
    /// </summary>
    public AppPreferences Preferences { get; set; } = new();

    /// <summary>
    /// ID of the currently selected project (if any)
    /// </summary>
    public string? CurrentProjectId { get; set; }
}

/// <summary>
/// User preferences stored in configuration
/// </summary>
public class AppPreferences
{
    /// <summary>
    /// Whether to start CLIHub with Windows
    /// </summary>
    public bool StartWithWindows { get; set; }

    /// <summary>
    /// Global hotkey combination (e.g., "Ctrl+Shift+A")
    /// </summary>
    public string Hotkey { get; set; } = "Ctrl+Shift+A";

    /// <summary>
    /// Path to terminal executable (default: wt.exe for Windows Terminal)
    /// </summary>
    public string TerminalExecutable { get; set; } = "wt.exe";

    /// <summary>
    /// Log level (Debug, Info, Warning, Error)
    /// </summary>
    public string LogLevel { get; set; } = "Information";
}
