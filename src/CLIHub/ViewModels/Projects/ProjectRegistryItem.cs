namespace CLIHub.ViewModels;

using CLIHub.Core.Models;

/// <summary>
///   One registered project on the Settings Projects page: identity, full path,
///   provenance, and whether its folder still exists.
/// </summary>
public sealed class ProjectRegistryItem
{
    /// <summary>
    ///   Creates the row from a registered project and the folder-existence check.
    /// </summary>
    /// <param name="project"> The registered project. </param>
    /// <param name="isFolderMissing"> Whether the project folder no longer exists. </param>
    public ProjectRegistryItem(Project project, bool isFolderMissing)
    {
        ArgumentNullException.ThrowIfNull(project);

        this.Id = project.Id;
        this.Name = project.Name;
        this.Path = project.Path;
        this.IsManual = project.Source == ProjectSource.Manual;
        this.IsFolderMissing = isFolderMissing;
    }

    /// <summary>
    ///   The stable project identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    ///   The project display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///   The full project folder path.
    /// </summary>
    public string Path { get; }

    /// <summary>
    ///   Whether the project was registered manually rather than discovered by scanning.
    /// </summary>
    public bool IsManual { get; }

    /// <summary>
    ///   The single-letter provenance mark shown on the row: "M" for a manual project
    ///   and "D" for a discovered project.
    /// </summary>
    public string SourceMark => this.IsManual ? "M" : "D";

    /// <summary>
    ///   The full provenance label, used as the mark's tooltip and accessible name.
    /// </summary>
    public string SourceLabel => this.IsManual ? "Manual" : "Discovered";

    /// <summary>
    ///   Whether the project folder no longer exists on disk.
    /// </summary>
    public bool IsFolderMissing { get; }
}

