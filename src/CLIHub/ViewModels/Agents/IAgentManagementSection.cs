namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;

using CLIHub.Controls;

/// <summary>
///   The agent-management section of the Settings window: cards,
///   supported/installed counts, the default-agent selection, and the per-card actions.
/// </summary>
public interface IAgentManagementSection : IDisposable
{
    /// <summary>
    ///   One card per loaded agent.
    /// </summary>
    ObservableCollection<AgentCardItem> Cards { get; }

    /// <summary>
    ///   The number of agents installed on this machine.
    /// </summary>
    int InstalledAgentCount { get; }

    /// <summary>
    ///   The supported/not-installed split shown under the installed count.
    /// </summary>
    string AvailabilitySummary { get; }

    /// <summary>
    ///   The selected default agent name, or "None".
    /// </summary>
    string DefaultAgentName { get; }

    /// <summary>
    ///   The version or selection hint shown under the default agent name.
    /// </summary>
    string DefaultAgentDetail { get; }

    /// <summary>
    ///   The tone of the default-agent summary value: <see cref="SummaryCardTone.Muted" /> while no
    ///   default agent is selected (the value is the "None" placeholder) and
    ///   <see cref="SummaryCardTone.Neutral" /> otherwise.
    /// </summary>
    SummaryCardTone DefaultAgentValueTone { get; }

    /// <summary>
    ///   The default-agent options, including a "None" option.
    /// </summary>
    IReadOnlyList<AgentOption> DefaultAgentOptions { get; }

    /// <summary>
    ///   The selected default agent.
    /// </summary>
    AgentOption SelectedDefaultAgent { get; set; }

    /// <summary>
    ///   The id of the selected default agent, or null for "None".
    /// </summary>
    string? CurrentDefaultAgentId { get; }

    /// <summary>
    ///   Re-detects the selected agent's host installation and version locally, without an
    ///   update-availability check.
    /// </summary>
    RelayCommand<AgentCardItem> DetectCardCommand { get; }

    /// <summary>
    ///   Forces a fresh update-availability check for the selected agent only, by invalidating
    ///   that agent's cached result first; does not re-run installation detection, does not issue
    ///   a version command, and does not touch the header-wide update-check summary.
    /// </summary>
    RelayCommand<AgentCardItem> CheckCardUpdatesCommand { get; }

    /// <summary>
    ///   Updates the selected agent.
    /// </summary>
    RelayCommand<AgentCardItem> UpdateCommand { get; }

    /// <summary>
    ///   Opens the selected agent's configuration.
    /// </summary>
    RelayCommand<AgentCardItem> EditConfigCommand { get; }

    /// <summary>
    ///   Re-detects every supported agent's host installation and version locally, without an
    ///   update-availability check, and refreshes the cards and the default-agent options.
    /// </summary>
    RelayCommand DetectCommand { get; }

    /// <summary>
    ///   Forces a fresh update-availability check for every installed agent by invalidating each
    ///   agent's cached result first; does not re-run installation detection or issue a version
    ///   command.
    /// </summary>
    RelayCommand CheckUpdatesCommand { get; }

    /// <summary>
    ///   Whether a detection pass is currently running.
    /// </summary>
    bool IsDetecting { get; }

    /// <summary>
    ///   Whether the detect action is available: true while neither the detection pass nor the
    ///   update-availability check pass is running, nor while any per-card action (Detect or
    ///   Check updates) is running.
    /// </summary>
    bool CanDetect { get; }

    /// <summary>
    ///   Whether the check-updates action is available: true while neither the detection pass nor
    ///   the update-availability check pass is running, nor while any per-card action (Detect or
    ///   Check updates) is running.
    /// </summary>
    bool CanCheckUpdates { get; }

    /// <summary>
    ///   The muted summary of the last manual update check, or null when no summary is shown.
    ///   Cleared on page reload and when a new check starts.
    /// </summary>
    string? UpdateCheckSummary { get; }

    /// <summary>
    ///   Rebuilds the cards and the default-agent options for the stored default agent.
    /// </summary>
    /// <param name="defaultAgentId"> The stored default agent id, or null. </param>
    void Load(string? defaultAgentId);
}
