namespace CLIHub.Updates;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
using CLIHub.Core.Updates;
using CLIHub.Views;

/// <summary>
///   Owns the application's single <see cref="WhatsNewWindow" /> instance so that every entry point
///   (the tray menu, the one-time display after an upgrade) activates the same window instead of
///   creating another, and keeps the recorded "notes seen" version up to date.
/// </summary>
public sealed class ReleaseNotesLauncher : IReleaseNotesLauncher
{
    private readonly Func<WhatsNewWindow> factory;
    private readonly IUpdateVersionProvider versionProvider;
    private readonly IPreferencesStore preferencesStore;
    private readonly ILogger<ReleaseNotesLauncher> logger;
    private WhatsNewWindow? window;

    /// <summary>
    ///   Creates the launcher with its dependencies.
    /// </summary>
    /// <param name="factory"> Factory that creates the <see cref="WhatsNewWindow" /> on demand. </param>
    /// <param name="versionProvider"> Provides the running version. </param>
    /// <param name="preferencesStore"> Preferences used to store the seen-versions record. </param>
    /// <param name="logger"> Logger. </param>
    public ReleaseNotesLauncher(Func<WhatsNewWindow> factory,
                                IUpdateVersionProvider versionProvider,
                                IPreferencesStore preferencesStore,
                                ILogger<ReleaseNotesLauncher> logger)
    {
        this.factory = factory;
        this.versionProvider = versionProvider;
        this.preferencesStore = preferencesStore;
        this.logger = logger;
    }

    /// <summary>
    ///   Shows the What's New window (creating it on first use) and records the running version as seen.
    /// </summary>
    public void ShowReleaseNotes()
    {
        if (this.window is null)
        {
            this.window = this.factory();
            this.window.Closed += (_, _) => this.window = null;
        }

        this.window.ShowNotes();
        this.logger.LogInformation("Opened the What's New window");
        this.MarkReleaseNotesSeen();
    }

    /// <summary>
    ///   Records the running application version as the last one whose notes were shown.
    /// </summary>
    public void MarkReleaseNotesSeen()
    {
        try
        {
            var version = this.versionProvider.GetCurrentVersion();
            var preferences = this.preferencesStore.Load();

            if (string.Equals(
                    preferences.LastSeenReleaseNotesVersion,
                    version,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            this.preferencesStore.Update(prefs => prefs.LastSeenReleaseNotesVersion = version);
            this.logger.LogInformation("Release notes recorded as seen for version {Version}", version);
        }
        catch (Exception ex)
        {
            // Recording is bookkeeping: failing it must not disturb the application.
            this.logger.LogWarning(ex, "Could not record the release notes version");
        }
    }
}
