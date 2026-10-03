namespace CLIHub.Core.Models;

/// <summary>
///   Represents a project directory tracked by CLIHub.
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
    ///   Whether this project is marked as a favorite.
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    ///   Timestamp of when this project was last used.
    /// </summary>
    public DateTime LastUsed { get; set; }

    /// <summary>
    ///   Optional path to project-specific logo image.
    /// </summary>
    public string? LogoPath { get; set; }
}
