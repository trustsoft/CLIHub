namespace CLIHub;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Models;
using CLIHub.Core.Updates;

/// <summary>
///   Coordinates release-notes startup evaluation without owning presentation or persistence.
/// </summary>
public sealed class ReleaseNotesStartupCoordinator : IReleaseNotesStartupCoordinator
{
    private readonly IUpdateService _updateService;
    private readonly IReleaseNotesService _releaseNotes;
    private readonly IReleaseNotesLauncher _launcher;
    private readonly ILogger<ReleaseNotesStartupCoordinator> _logger;

    /// <summary>
    ///   Creates the coordinator with version, notes, and presentation boundaries.
    /// </summary>
    /// <param name="updateService"> Provides the running application version. </param>
    /// <param name="releaseNotes"> Provides release notes for the running version. </param>
    /// <param name="launcher"> Shows the notes or records them as seen. </param>
    /// <param name="logger"> Logger for non-fatal startup failures. </param>
    public ReleaseNotesStartupCoordinator(
        IUpdateService updateService,
        IReleaseNotesService releaseNotes,
        IReleaseNotesLauncher launcher,
        ILogger<ReleaseNotesStartupCoordinator> logger)
    {
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
        _releaseNotes = releaseNotes ?? throw new ArgumentNullException(nameof(releaseNotes));
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void Evaluate(AppPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        try
        {
            var currentVersion = _updateService.GetCurrentVersion();
            var action = ReleaseNotesPrompt.Decide(
                preferences.LastSeenReleaseNotesVersion,
                currentVersion,
                _releaseNotes.GetNote(currentVersion) != null);

            if (action == ReleaseNotesPromptAction.Show)
            {
                _launcher.ShowReleaseNotes();
            }
            else if (action == ReleaseNotesPromptAction.RecordOnly)
            {
                _launcher.MarkReleaseNotesSeen();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The release notes check failed");
        }
    }
}
