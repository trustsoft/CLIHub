namespace CLIHub.Core.Services;

using CLIHub.Core.Interfaces;
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
    public void Save(ProjectState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var config = _configService.Load();
        state.ApplyTo(config);
        _configService.Save(config);
    }
}
