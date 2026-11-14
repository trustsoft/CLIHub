namespace CLIHub.Core.Plugins;

/// <summary>
///   Evaluates a plugin compatibility range against the current application version. A range is a
///   list of constraints such as <c> "&gt;=1.1.0 &lt;2.0.0" </c>, separated by commas and/or
///   whitespace, and all constraints must hold (ANDed). Supported operators are
///   <c> &gt;=, &lt;=, &gt;, &lt;, = </c>; versions use one to three dot-separated non-negative
///   integer segments with missing minor/patch defaulting to 0. An unparseable range fails closed.
/// </summary>
public static class AppCompatibilityRange
{
    /// <summary>
    ///   Checks whether the current application version satisfies every constraint in the
    ///   compatibility range. Returns false when the range does not parse (fail closed).
    /// </summary>
    /// <param name="currentVersion"> Current CLIHub version (e.g., "1.2.3"). </param>
    /// <param name="compatibility"> Compatibility range (e.g., "&gt;=1.1.0 &lt;2.0.0"). </param>
    /// <returns> True if all constraints hold; false otherwise. </returns>
    public static bool IsSatisfied(string currentVersion, string compatibility)
    {
        if (!TryParse(compatibility, out var constraints))
        {
            return false;
        }

        var current = AppVersionComparator.ParseVersion(currentVersion);
        foreach (var (op, version) in constraints)
        {
            if (!ConstraintSatisfied(current, op, version))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///   Parses a compatibility range into its constraint list. Constraints are separated by
    ///   commas and/or whitespace; each is one of the supported operators followed by one to
    ///   three dot-separated non-negative integer segments. Missing minor/patch segments
    ///   default to 0, mirroring <see cref="AppVersionComparator.ParseVersion" /> tolerance.
    /// </summary>
    /// <param name="compatibility"> Compatibility range text. </param>
    /// <param name="constraints">
    ///   Parsed constraints in order: operator and component-wise version, or an empty list when
    ///   the parse fails.
    /// </param>
    /// <returns> True when the range parses; false for empty, malformed, or unknown-operator input. </returns>
    internal static bool TryParse(string compatibility, out IReadOnlyList<(string Op, (int Major, int Minor, int Patch) Version)> constraints)
    {
        var parsed = new List<(string Op, (int Major, int Minor, int Patch) Version)>();
        constraints = parsed;

        if (string.IsNullOrWhiteSpace(compatibility))
        {
            return false;
        }

        // Constraints are separated by commas and/or whitespace; empty entries are dropped so
        // leading, trailing, and repeated separators are tolerated.
        var tokens = compatibility.Split([',', ' ', '\t', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var token in tokens)
        {
            if (!TryParseConstraint(token, out var op, out var version))
            {
                return false;
            }

            parsed.Add((op, version));
        }

        return parsed.Count > 0;
    }

    private static bool TryParseConstraint(string token, out string op, out (int Major, int Minor, int Patch) version)
    {
        op = string.Empty;
        version = (0, 0, 0);

        // Longest operator match first so ">=" and "<=" are not read as ">" and "<".
        foreach (var candidate in (string[])[">=", "<=", ">", "<", "="])
        {
            if (token.StartsWith(candidate, StringComparison.Ordinal))
            {
                op = candidate;
                break;
            }
        }

        if (op.Length == 0 || token.Length <= op.Length)
        {
            return false;
        }

        var versionText = token[op.Length..];
        var segments = versionText.Split('.');
        if (segments.Length is < 1 or > 3)
        {
            return false;
        }

        var values = new int[3];
        for (var i = 0; i < segments.Length; i++)
        {
            // NumberStyles.None rejects signs, so only non-negative integers parse.
            if (!int.TryParse(segments[i], System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out var value))
            {
                return false;
            }

            values[i] = value;
        }

        version = (values[0], values[1], values[2]);
        return true;
    }

    private static bool ConstraintSatisfied((int Major, int Minor, int Patch) current, string op, (int Major, int Minor, int Patch) required)
    {
        var comparison = Compare(current, required);
        return op switch
        {
            ">=" => comparison >= 0,
            "<=" => comparison <= 0,
            ">" => comparison > 0,
            "<" => comparison < 0,
            "=" => comparison == 0,
            // TryParse only emits the operators above; unreachable, but fail closed.
            _ => false
        };
    }

    private static int Compare((int Major, int Minor, int Patch) left, (int Major, int Minor, int Patch) right)
    {
        if (left.Major != right.Major)
        {
            return left.Major < right.Major ? -1 : 1;
        }

        if (left.Minor != right.Minor)
        {
            return left.Minor < right.Minor ? -1 : 1;
        }

        if (left.Patch != right.Patch)
        {
            return left.Patch < right.Patch ? -1 : 1;
        }

        return 0;
    }
}
