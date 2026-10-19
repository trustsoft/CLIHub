namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;

/// <summary>
///   Applies catalog rows to one surface collection while preserving stable row identity:
///   removes rows whose project vanished, inserts new rows, and reorders with Move so no row
///   is ever recreated on refresh (pattern reference: <see cref="AgentListSynchronizer" />).
/// </summary>
public static class ProjectModelListSynchronizer
{
    /// <summary>
    ///   Synchronizes a surface collection's membership and order to the target rows.
    /// </summary>
    /// <param name="rows"> The surface collection displayed by one surface. </param>
    /// <param name="orderedModels"> The target rows in the surface's display order. </param>
    public static void Synchronize(ObservableCollection<ProjectModel> rows,
                                   IReadOnlyList<ProjectModel> orderedModels)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(orderedModels);

        var targetIds = orderedModels.Select(model => model.Id).ToHashSet(StringComparer.Ordinal);

        for (var index = rows.Count - 1; index >= 0; index--)
        {
            if (!targetIds.Contains(rows[index].Id))
            {
                rows.RemoveAt(index);
            }
        }

        for (var targetIndex = 0; targetIndex < orderedModels.Count; targetIndex++)
        {
            var currentIndex = rows.IndexOf(orderedModels[targetIndex]);
            if (currentIndex < 0)
            {
                rows.Insert(targetIndex, orderedModels[targetIndex]);
            }
            else if (currentIndex != targetIndex)
            {
                rows.Move(currentIndex, targetIndex);
            }
        }
    }
}
