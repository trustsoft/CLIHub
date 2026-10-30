namespace CLIHub.Core.Models.Plugins;

/// <summary>
///   Resolves a plugin's declared update-check source into the supported source kind and the
///   reason a declaration is unusable, so every caller applies the same rule and only the
///   caller's message differs.
/// </summary>
internal static class UpdateCheckSource
{
    /// <summary>
    ///   The npm registry source kind.
    /// </summary>
    internal const string Npm = "npm";

    /// <summary>
    ///   The GitHub releases source kind.
    /// </summary>
    internal const string GitHubReleases = "githubReleases";

    /// <summary>
    ///   The resolved source kind of an update check.
    /// </summary>
    internal enum SourceKind
    {
        /// <summary> No usable source. </summary>
        None,

        /// <summary> The npm registry, addressed by package name. </summary>
        Npm,

        /// <summary> GitHub releases, addressed by owner/repository. </summary>
        GitHubReleases
    }

    /// <summary>
    ///   The reason an update check declares no usable source.
    /// </summary>
    internal enum Problem
    {
        /// <summary> The declaration is usable. </summary>
        None,

        /// <summary> An npm declaration without a package name. </summary>
        MissingPackage,

        /// <summary> A githubReleases declaration without a repository. </summary>
        MissingRepo,

        /// <summary> A blank or unsupported source kind. </summary>
        UnknownSource
    }

    /// <summary>
    ///   Resolves the update-check source of a declaration.
    /// </summary>
    /// <param name="updateCheck"> The declared update check, or null when the plugin declares none. </param>
    /// <param name="kind"> The resolved source kind, or <see cref="SourceKind.None" /> on failure. </param>
    /// <param name="problem">
    ///   The reason resolution failed, or <see cref="Problem.None" /> when it succeeded or no
    ///   update check was declared.
    /// </param>
    /// <returns> True only when a supported source was resolved. </returns>
    internal static bool TryResolve(PluginUpdateCheck? updateCheck, out SourceKind kind, out Problem problem)
    {
        if (updateCheck is null)
        {
            kind = SourceKind.None;
            problem = Problem.None;
            return false;
        }

        if (string.Equals(updateCheck.Source, Npm, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(updateCheck.Package))
            {
                kind = SourceKind.None;
                problem = Problem.MissingPackage;
                return false;
            }

            kind = SourceKind.Npm;
            problem = Problem.None;
            return true;
        }

        if (string.Equals(updateCheck.Source, GitHubReleases, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(updateCheck.Repo))
            {
                kind = SourceKind.None;
                problem = Problem.MissingRepo;
                return false;
            }

            kind = SourceKind.GitHubReleases;
            problem = Problem.None;
            return true;
        }

        kind = SourceKind.None;
        problem = Problem.UnknownSource;
        return false;
    }
}
