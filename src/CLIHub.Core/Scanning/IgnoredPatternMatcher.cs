namespace CLIHub.Core.Scanning;

/// <summary>
///   Matches directory names against ignored-name patterns. Patterns are case-insensitive and
///   support a single wildcard character (<c> * </c>); all other characters match literally.
/// </summary>
internal static class IgnoredPatternMatcher
{
    /// <summary>
    ///   Returns whether a directory name matches any of the supplied patterns.
    /// </summary>
    /// <param name="directoryName"> The directory name to test. </param>
    /// <param name="patterns"> The ignored-name patterns. </param>
    /// <returns> True when the name matches at least one non-empty pattern. </returns>
    public static bool IsIgnored(string directoryName, IReadOnlyList<string> patterns)
    {
        if (string.IsNullOrEmpty(directoryName) || patterns.Count == 0)
        {
            return false;
        }

        foreach (var pattern in patterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                continue;
            }

            if (Matches(directoryName, pattern.Trim()))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Matches(string value, string pattern)
    {
        int valueIndex = 0;
        int patternIndex = 0;
        int starIndex = -1;
        int resumeIndex = 0;

        while (valueIndex < value.Length)
        {
            if (patternIndex < pattern.Length
                && pattern[patternIndex] != '*'
                && char.ToLowerInvariant(pattern[patternIndex]) == char.ToLowerInvariant(value[valueIndex]))
            {
                valueIndex++;
                patternIndex++;
            }
            else if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
            {
                starIndex = patternIndex;
                resumeIndex = valueIndex;
                patternIndex++;
            }
            else if (starIndex != -1)
            {
                patternIndex = starIndex + 1;
                resumeIndex++;
                valueIndex = resumeIndex;
            }
            else
            {
                return false;
            }
        }

        while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
        {
            patternIndex++;
        }

        return patternIndex == pattern.Length;
    }
}
