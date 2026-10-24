namespace CLIHub.Helpers;

/// <summary>
///   Owns the agent version placeholder texts and the pending/known checks shared by the agent
///   surfaces, so the sentinel literals live in exactly one place.
/// </summary>
public static class AgentVersionText
{
    /// <summary>
    ///   The placeholder shown while a version probe has not finished.
    /// </summary>
    public const string Pending = "…";

    /// <summary>
    ///   The placeholder shown when a probe finished without producing a version.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    ///   Whether the version is still pending: absent, empty, or the pending placeholder.
    /// </summary>
    /// <param name="version"> The version text to test. </param>
    /// <returns> True when no resolved version value exists yet. </returns>
    public static bool IsPending(string? version) =>
        string.IsNullOrEmpty(version) || version == Pending;

    /// <summary>
    ///   Whether the version is a resolved value: neither pending nor the unknown placeholder.
    /// </summary>
    /// <param name="version"> The version text to test. </param>
    /// <returns> True when the version is a known value. </returns>
    public static bool IsKnown(string? version) =>
        !IsPending(version) && version != Unknown;
}
