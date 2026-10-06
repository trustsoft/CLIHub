namespace CLIHub.Core.Projects;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;

/// <summary>
///   Adapts the configuration repository to the project state boundary.
/// </summary>
public sealed class ProjectStateStore : IProjectStateStore
{
    private readonly IConfigurationRepository _repository;

    /// <summary>
    ///   Creates the store over the configuration repository.
    /// </summary>
    /// <param name="repository"> The configuration repository. </param>
    public ProjectStateStore(IConfigurationRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public ProjectState Load() => ProjectState.From(_repository.Read());

    /// <inheritdoc />
    public void Update(Action<ProjectState> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        _repository.Update(snapshot =>
        {
            var state = ProjectState.From(snapshot);
            update(state);
            state.ApplyTo(snapshot);
        });
    }
}
