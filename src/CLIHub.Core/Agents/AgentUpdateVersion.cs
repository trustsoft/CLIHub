namespace CLIHub.Core.Agents;

using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
///   Extracts and compares numeric major.minor.patch version triples for update detection.
///   Pre-release and build suffixes are ignored; comparison is numeric, never lexical.
/// </summary>
internal static class AgentUpdateVersion
{
    private static readonly Regex TriplePattern = new(@"\d+\.\d+\.\d+", RegexOptions.Compiled);

    /// <summary>
    ///   Extracts the first numeric major.minor.patch triple from the given text, ignoring
    ///   surrounding content such as a leading "v" or a product name.
    /// </summary>
    /// <param name="value"> The raw version text, or null. </param>
    /// <param name="triple"> The extracted triple when the return value is true. </param>
    /// <returns> True when a parseable triple was found; otherwise false. </returns>
    public static bool TryExtract(string? value, out AgentVersionTriple triple)
    {
        triple = default;
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var match = TriplePattern.Match(value);
        if (!match.Success)
        {
            return false;
        }

        var parts = match.Value.Split('.');
        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var major)
            || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minor)
            || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var patch))
        {
            return false;
        }

        triple = new AgentVersionTriple(major, minor, patch);
        return true;
    }

    /// <summary>
    ///   Compares the installed triple with the latest published triple numerically over
    ///   major, then minor, then patch.
    /// </summary>
    /// <param name="installed"> The installed version triple. </param>
    /// <param name="latest"> The latest published version triple. </param>
    /// <returns>
    ///   Negative when the latest is greater than the installed version, zero when equal, and
    ///   positive when the installed version is newer.
    /// </returns>
    public static int Compare(AgentVersionTriple installed, AgentVersionTriple latest)
    {
        var major = installed.Major.CompareTo(latest.Major);
        if (major != 0)
        {
            return major;
        }

        var minor = installed.Minor.CompareTo(latest.Minor);
        return minor != 0 ? minor : installed.Patch.CompareTo(latest.Patch);
    }
}

/// <summary>
///   A numeric major.minor.patch version triple.
/// </summary>
/// <param name="Major"> The major component. </param>
/// <param name="Minor"> The minor component. </param>
/// <param name="Patch"> The patch component. </param>
internal readonly record struct AgentVersionTriple(int Major, int Minor, int Patch)
{
    public override string ToString() => $"{this.Major}.{this.Minor}.{this.Patch}";
}
