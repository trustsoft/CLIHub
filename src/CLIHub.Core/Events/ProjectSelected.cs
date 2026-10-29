namespace CLIHub.Core.Events;

/// <summary>
///   Published when the current project selection changes.
///   This event is published after the project state is updated and persisted.
/// </summary>
/// <param name="Id"> The unique identifier of the selected project. </param>
/// <param name="Path"> The file system path to the project. </param>
/// <param name="SelectedAt"> The UTC timestamp when the project was selected. </param>
public record ProjectSelected(string Id, string Path, DateTimeOffset SelectedAt) : IDomainEvent;
