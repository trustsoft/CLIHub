namespace CLIHub.Core.Models;

/// <summary>
///   The persisted brief record of a project directory tracked by CLIHub: identity, path,
///   provenance, favorite flag, last-used timestamp, and the vestigial persisted logo path.
///   A plain mutable POCO with no change notifications; UI display rows are separate enriched
///   models (see the app project's <c>ProjectModel</c>).
/// </summary>
public class Project
{
    /// <summary>
    ///   Unique identifier for the project.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    ///   Display name of the project.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///   Full path to the project directory.
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    ///   How the project entered the list: registered manually by the user,
    ///   or discovered by repository scanning. Defaults to <see cref="ProjectSource.Manual" />.
    /// </summary>
    public ProjectSource Source { get; set; } = ProjectSource.Manual;

    /// <summary>
    ///   Whether this project is marked as a favorite.
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    ///   Timestamp of when this project was last used.
    /// </summary>
    public DateTime LastUsed { get; set; }

    /// <summary>
    ///   Optional path to project-specific logo image. Vestigial: logo resolution for display
    ///   happens through the project enricher; the field stays because the config format is
    ///   stable and no longer written by the service.
    /// </summary>
    public string? LogoPath { get; set; }

    /// <summary>
    ///   Creates a detached copy of this record. Service read methods hand out copies, so
    ///   callers can mutate a returned record without corrupting registered project state.
    /// </summary>
    /// <returns> A new project record with the same field values. </returns>
    public Project Copy() => new()
    {
        Id = this.Id,
        Name = this.Name,
        Path = this.Path,
        Source = this.Source,
        IsFavorite = this.IsFavorite,
        LastUsed = this.LastUsed,
        LogoPath = this.LogoPath
    };
}
