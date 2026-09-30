namespace CLIHub.Core.Formatting;

/// <summary>
/// Shortens text to fit a maximum length by dropping its beginning while keeping the end
/// visible. Preferred break characters (path separators) are used to resume at a segment
/// boundary, which produces the mockup's "...Tail\Folder" presentation for long paths.
/// </summary>
public static class PathLeftTrimFormatter
{
    /// <summary>
    /// Character that marks the removed beginning of the text.
    /// </summary>
    public const char Ellipsis = MiddleEllipsisFormatter.Ellipsis;

    private static readonly char[] BreakCharacters = ['\\', '/'];

    /// <summary>
    /// Returns <paramref name="text"/> shortened to at most <paramref name="maxLength"/>
    /// characters, or the text unchanged when it already fits.
    /// </summary>
    public static string Format(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || maxLength <= 0)
        {
            return string.Empty;
        }

        if (text.Length <= maxLength)
        {
            return text;
        }

        if (maxLength == 1)
        {
            return Ellipsis.ToString();
        }

        var budget = maxLength - 1;
        var tail = ShortenTail(text.AsSpan(text.Length - budget));

        return $"{Ellipsis}{tail}";
    }

    /// <summary>
    /// Drops everything up to and including the first break character of the tail, so the
    /// shortened text resumes at a segment boundary instead of inside a cut segment name.
    /// </summary>
    private static string ShortenTail(ReadOnlySpan<char> tail)
    {
        var firstBreak = tail.IndexOfAny(BreakCharacters);

        return firstBreak >= 0 && firstBreak < tail.Length - 1
            ? tail[(firstBreak + 1)..].ToString()
            : tail.ToString();
    }
}
