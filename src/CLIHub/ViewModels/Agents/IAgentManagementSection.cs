namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;

/// <summary>
///   The agent-management section of the Settings window: cards, metrics, the default-agent
///   selection, and the per-card actions.
/// </summary>
public interface IAgentManagementSection : IDisposable
{
    /// <summary>
    ///   One card per loaded agent.
    /// </summary>
    ObservableCollection<AgentCardItem> Cards { get; }

    /// <summary>
    ///   The supported/installed metrics line.
    /// </summary>
    string Metrics { get; }

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
