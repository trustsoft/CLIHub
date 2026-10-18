namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;

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
    ///   The number of supported agent plugins.
    /// </summary>
    int SupportedAgentCount { get; }

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
    ///   Whether a default agent is selected.
    /// </summary>
    bool HasDefaultAgent { get; }

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
    ///   Re-evaluates the selected agent.
    /// </summary>
    RelayCommand<AgentCardItem> TestCommand { get; }

    /// <summary>
    ///   Updates the selected agent.
    /// </summary>
    RelayCommand<AgentCardItem> UpdateCommand { get; }

    /// <summary>
    ///   Opens the selected agent's configuration.
    /// </summary>
    RelayCommand<AgentCardItem> EditConfigCommand { get; }

    /// <summary>
    ///   Rebuilds the cards and the default-agent options for the stored default agent.
    /// </summary>
    /// <param name="defaultAgentId"> The stored default agent id, or null. </param>
    void Load(string? defaultAgentId);
}
