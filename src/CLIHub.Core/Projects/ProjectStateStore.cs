namespace CLIHub.Core.Projects;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;

/// <summary>
///   Adapts the shared configuration document to the project state boundary.
/// </summary>
public sealed class ProjectStateStore : IProjectStateStore
{
    private readonly IConfigService _configService;

    /// <summary>
    ///   Creates the store over the shared configuration service.
    /// </summary>
    /// <param name="configService"> The shared configuration service. </param>
    public ProjectStateStore(IConfigService configService)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    }

    /// <inheritdoc />
    public ProjectState Load() => ProjectState.From(_configService.Load());

    /// <inheritdoc />
    public void Update(Action<ProjectState> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        var snapshot = _configService.Load();
        var state = ProjectState.From(snapshot);
        update(state);
        state.ApplyTo(snapshot);
        _configService.Save(snapshot);
    }
}
