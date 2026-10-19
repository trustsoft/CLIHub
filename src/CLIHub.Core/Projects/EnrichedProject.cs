namespace CLIHub.Core.Projects;

using CLIHub.Core.Models;

/// <summary>
///   A plain enriched snapshot of a project: the record itself plus the display-only facts
///   that can go stale — whether the project folder currently exists and the final logo path
///   (the project logo resolver has already applied the default-logo fallback).
/// </summary>
/// <param name="Project"> The project record this snapshot enriches. </param>
/// <param name="FolderExists"> Whether the project folder existed when the snapshot was taken. </param>
/// <param name="LogoPath"> The final logo path with the fallback applied, or null. </param>
public sealed record EnrichedProject(Project Project, bool FolderExists, string? LogoPath);
