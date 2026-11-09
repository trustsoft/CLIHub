namespace CLIHub.ViewModels;

/// <summary>
///   Presentation-order policy for the AGENTS card list: installed agents first, not-installed
///   agents last, preserving the input (catalog) order within each group. Pure function with no
///   dependencies; the caller owns the input order and decides when to apply the policy.
/// </summary>
public static class AgentCardOrdering
{
    /// <summary>
    ///   Partitions cards into installed first and not installed last, preserving the relative
    ///   input (catalog) order within each group. No alphabetical or other secondary sort is
    ///   applied. A card whose installation state is being detected keeps its last settled group.
    /// </summary>
    /// <param name="catalogOrder"> The cards in plugin catalog order. </param>
    /// <returns> A new list with installed cards before not-installed cards. </returns>
    public static IReadOnlyList<AgentCardItem> InstalledFirst(IReadOnlyList<AgentCardItem> catalogOrder)
    {
        ArgumentNullException.ThrowIfNull(catalogOrder);

        var installed = new List<AgentCardItem>(catalogOrder.Count);
        var notInstalled = new List<AgentCardItem>(catalogOrder.Count);
        foreach (var card in catalogOrder)
        {
            if (card.OrderingState == AgentInstallState.NotInstalled)
            {
                notInstalled.Add(card);
            }
            else
            {
                installed.Add(card);
            }
        }

        var ordered = new List<AgentCardItem>(catalogOrder.Count);
        ordered.AddRange(installed);
        ordered.AddRange(notInstalled);
        return ordered;
    }
}
