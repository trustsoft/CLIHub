namespace CLIHub;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Interfaces;
using CLIHub.Views;

/// <summary>
///   Owns the application's single <see cref="WhatsNewWindow"/> instance so that every entry point
///   (the tray menu, the one-time display after an upgrade) activates the same window instead of
///   creating another, and keeps the recorded "notes seen" version up to date.
/// </summary>
public sealed class ReleaseNotesLauncher : IReleaseNotesLauncher
{
    private readonly Func<WhatsNewWindow> _factory;
    private readonly IUpdateService _updateService;
    private readonly IConfigService _configService;
    private readonly ILogger<ReleaseNotesLauncher> _logger;
    private WhatsNewWindow? _window;

    /// <summary>
    ///   Creates the launcher with its dependencies.
    /// </summary>
    /// <param name="factory"> Factory that creates the <see cref="WhatsNewWindow"/> on demand. </param>
    /// <param name="updateService"> Update service that reports the running version. </param>
    /// <param name="configService"> Configuration used to store the seen-versions record. </param>
    /// <param name="logger"> Logger. </param>
    public ReleaseNotesLauncher(
        Func<WhatsNewWindow> factory,
        IUpdateService updateService,
        IConfigService configService,
        ILogger<ReleaseNotesLauncher> logger)
    {
        _factory = factory;
        _updateService = updateService;
        _configService = configService;
        _logger = logger;
    }

    /// <summary>
    ///   Shows the What's New window (creating it on first use) and records the running
    ///   version as seen.
    /// </summary>
    public void ShowReleaseNotes()
    {
        if (_window is null)
        {
            _window = _factory();
            _window.Closed += (_, _) => _window = null;
        }

        _window.ShowNotes();
        _logger.LogInformation("Opened the What's New window");
        MarkReleaseNotesSeen();
    }

    /// <summary>
    ///   Records the running application version as the last one whose notes were shown.
    /// </summary>
    public void MarkReleaseNotesSeen()
    {
        try
        {
            var version = _updateService.GetCurrentVersion();
            var config = _configService.Load();

            if (string.Equals(
                    config.Preferences.LastSeenReleaseNotesVersion,
                    version,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            config.Preferences.LastSeenReleaseNotesVersion = version;
            _configService.Save(config);
            _logger.LogInformation("Release notes recorded as seen for version {Version}", version);
        }
        catch (Exception ex)
        {
            // Recording is bookkeeping: failing it must not disturb the application.
            _logger.LogWarning(ex, "Could not record the release notes version");
        }
    }
}
