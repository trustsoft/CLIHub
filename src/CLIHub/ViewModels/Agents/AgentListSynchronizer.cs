namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;

using CLIHub.Core.Agents;
using CLIHub.Core.Models;

/// <summary>
///   Applies composed agent entries to the launch-window rows while preserving stable row identity.
/// </summary>
public static class AgentListSynchronizer
{
    /// <summary>
    ///   Synchronizes membership, row properties, and order by plugin ID.
    /// </summary>
    /// <param name="rows"> The rows displayed by the launch window. </param>
    /// <param name="entries"> The target entries in composer order. </param>
    /// <param name="defaultLogoPath"> The logo path used when a plugin has no logo. </param>
    /// <param name="selectedPluginId"> The plugin ID selected before synchronization. </param>
    /// <returns> The row matching the previous selection, or null when it disappeared. </returns>
    public static AgentItem? Synchronize(
        ObservableCollection<AgentItem> rows,
        IReadOnlyList<AgentListEntry> entries,
        string defaultLogoPath,
        string? selectedPluginId)
    {
        var targetIds = entries.Select(entry => entry.Plugin.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var i = rows.Count - 1; i >= 0; i--)
        {
            if (!targetIds.Contains(rows[i].Plugin.Id))
            {
                rows.RemoveAt(i);
            }
        }

        var existingById = rows.ToDictionary(row => row.Plugin.Id, StringComparer.OrdinalIgnoreCase);
        var orderedRows = new List<AgentItem>(entries.Count);

        foreach (var entry in entries)
        {
            if (!existingById.TryGetValue(entry.Plugin.Id, out var row))
            {
                row = new AgentItem
                {
                    Plugin = entry.Plugin,
                    Name = entry.Plugin.Name,
                    LogoPath = entry.Plugin.LogoPath ?? defaultLogoPath,
                    IsAvailable = entry.IsAvailable
                };
            }
            else
            {
                row.Update(
                    entry.Plugin,
                    entry.Plugin.Name,
                    entry.Plugin.LogoPath ?? defaultLogoPath,
                    entry.IsAvailable);
            }

            orderedRows.Add(row);
        }

        for (var targetIndex = 0; targetIndex < orderedRows.Count; targetIndex++)
        {
            var currentIndex = rows.IndexOf(orderedRows[targetIndex]);
            if (currentIndex < 0)
            {
                rows.Insert(targetIndex, orderedRows[targetIndex]);
            }
            else if (currentIndex != targetIndex)
            {
                rows.Move(currentIndex, targetIndex);
            }
        }

        return selectedPluginId is null
            ? null
            : rows.FirstOrDefault(row => string.Equals(row.Plugin.Id, selectedPluginId, StringComparison.OrdinalIgnoreCase));
    }
}
