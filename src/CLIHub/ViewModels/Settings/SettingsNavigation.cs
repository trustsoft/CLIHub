namespace CLIHub.ViewModels;

using CLIHub.Themes;

/// <summary>
///   A Settings navigation page: a stable key, a display title, the group it belongs to, and the
///   resource keys of the page-specific header and summary templates it uses.
/// </summary>
/// <param name="Key"> Stable page key used to route the page view. </param>
/// <param name="Title"> Display title shown in the sidebar and page header. </param>
/// <param name="Description"> One-line description shown under the page title in the page header. </param>
/// <param name="Group"> The navigation group the page belongs to. </param>
/// <param name="Icon"> The Segoe MDL2 glyph shown before the page title in the sidebar. </param>
/// <param name="HeaderTitleExtraKey">
///   Resource key of the template rendered beside the page title, or null when the page has no
///   extra title content.
/// </param>
/// <param name="HeaderActionKey">
///   Resource key of the template rendered in the header action column, or null when the page has
///   no header actions.
/// </param>
/// <param name="SummaryRowKey">
///   Resource key of the template rendered in the header summary row, or null when the page has no
///   summary cards.
/// </param>
public sealed record SettingsPage(
    string Key,
    string Title,
    string Description,
    string Group,
    string Icon,
    string? HeaderTitleExtraKey = null,
    string? HeaderActionKey = null,
    string? SummaryRowKey = null);

/// <summary>
///   The static Settings navigation catalog: the page metadata in sidebar display order.
/// </summary>
internal static class SettingsPageCatalog
{
    private static readonly SettingsPage[] AllPagesData =
    {
        new(
            "general",
            "General & Startup",
            "How CLIHub starts and opens.",
            "General",
            IconGlyphs.Settings),
        new(
            "launch-window",
            "Launch Window",
            "Global hotkey, launcher position, and path display.",
            "General",
            IconGlyphs.OpenInNewWindow),
        new(
            "projects-registry",
            "Projects",
            "Where projects come from and how to manage them.",
            "Engines & Repos",
            IconGlyphs.FolderSelect,
            SummaryRowKey: "RegistrySummaryCards"),
        new(
            "projects",
            "Project Scanning",
            "Watched folders, scan rules, and watcher status.",
            "Engines & Repos",
            IconGlyphs.SyncFolder,
            HeaderActionKey: "ProjectsHeaderAction",
            SummaryRowKey: "ProjectsSummaryCards"),
        new(
            "agents",
            "CLI Agents",
            "Agent status, updates, and the default agent.",
            "Engines & Repos",
            IconGlyphs.Robot,
            SummaryRowKey: "AgentsSummaryCards"),
        new(
            "agent-detection",
            "Agent Detection",
            "Caching and timeout behavior for agent detection.",
            "Engines & Repos",
            IconGlyphs.Process,
            SummaryRowKey: "AgentDetectionSummaryCards"),
        new(
            "terminal",
            "Terminal Profiles",
            "The terminal that CLI agents run in.",
            "Engines & Repos",
            IconGlyphs.Terminal),
        new(
            "appearance",
            "Theme & Accent",
            "How CLIHub looks.",
            "System",
            IconGlyphs.Palette),
        new(
            "telemetry",
            "Logs",
            "Log level, retention, and the log file.",
            "System",
            IconGlyphs.History),
        new(
            "updates",
            "Updates",
            "Update checks, downloads, and notifications.",
            "System",
            IconGlyphs.Sync,
            HeaderTitleExtraKey: "UpdatesHeaderTitleExtra",
            HeaderActionKey: "UpdatesHeaderAction")
    };

    /// <summary>
    ///   All Settings pages, in sidebar display order.
    /// </summary>
    public static IReadOnlyList<SettingsPage> AllPages => AllPagesData;
}
