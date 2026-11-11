namespace CLIHub.Core.Models;

using System.Diagnostics.CodeAnalysis;

/// <summary>
///   Descriptor-declared update source for an agent: where the latest published version
///   can be read from.
/// </summary>
/// <param name="Source">
///   The raw source kind ("npm" or "githubReleases"). The value stays a raw string so an
///   unknown kind written by a newer CLIHub survives loading and is ignored at use time
///   instead of rejecting the descriptor.
/// </param>
/// <param name="Package"> The npm package name when <paramref name="Source" /> is "npm". </param>
/// <param name="Repo"> The "owner/repository" when <paramref name="Source" /> is "githubReleases". </param>
public sealed record PluginUpdateCheck(string Source, string? Package = null, string? Repo = null)
{
    /// <summary>
    ///   Whether <see cref="Package" /> is present; a guard on this property also proves the
    ///   package name non-null to the compiler.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Package))]
    public bool HasPackage => !string.IsNullOrWhiteSpace(this.Package);

    /// <summary>
    ///   Whether <see cref="Repo" /> is present; a guard on this property also proves the
    ///   repository non-null to the compiler.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Repo))]
    public bool HasRepo => !string.IsNullOrWhiteSpace(this.Repo);
}
