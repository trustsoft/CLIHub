namespace CLIHub.Core.Formatting;

/// <summary>
/// Shortens text to fit a maximum length while keeping both the beginning and the end
/// of the original text visible. Preferred break characters (path separators) are used
/// to cut at a segment boundary when one is available inside the trimmed region.
/// </summary>
public static class MiddleEllipsisFormatter
{
    /// <summary>
    /// Character that marks the removed middle part.
    /// </summary>
    public const char Ellipsis = '…';

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
        var headLength = (budget + 1) / 2;
        var tailLength = budget - headLength;

        var head = ShortenHead(text.AsSpan(0, headLength));

        // Characters freed by trimming the head are given to the tail, so the end of the text
        // (usually the most informative part of a path) stays as long as the budget allows.
        var effectiveTailLength = Math.Min(tailLength + headLength - head.Length, text.Length - headLength);
        var tail = effectiveTailLength <= 0
            ? string.Empty
            : ShortenTail(text.AsSpan(text.Length - effectiveTailLength));

        return $"{head}{Ellipsis}{tail}";
    }

    /// <summary>
    /// Keeps the head up to and including its last break character, so the shortened
    /// text does not show a partially cut segment name.
    /// </summary>
    private static string ShortenHead(ReadOnlySpan<char> head)
    {
        var lastBreak = head.LastIndexOfAny(BreakCharacters);

        return lastBreak > 0
            ? head[..(lastBreak + 1)].ToString()
            : head.ToString();
    }

    /// <summary>
    /// Drops everything up to and including the first break character of the tail, so the
    /// shortened text resumes at a segment boundary.
    /// </summary>
    private static string ShortenTail(ReadOnlySpan<char> tail)
    {
        var firstBreak = tail.IndexOfAny(BreakCharacters);

        return firstBreak >= 0 && firstBreak < tail.Length - 1
            ? tail[(firstBreak + 1)..].ToString()
            : tail.ToString();
    }
}
