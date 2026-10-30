namespace CLIHub.Core.Agents;

/// <summary>
///   Shared defaults and resolution for the agent probe time-to-live and probe timeout
///   preferences, so version, detection, and update checks all age and time out the same way.
/// </summary>
internal static class AgentProbePolicy
{
    /// <summary>
    ///   The probe time-to-live in minutes used when the TTL preference is not set.
    /// </summary>
    internal const int DefaultTtlMinutes = 15;

    /// <summary>
    ///   The probe timeout in seconds used when the timeout preference is not set.
    /// </summary>
    internal const int DefaultProbeTimeoutSeconds = 10;

    /// <summary>
    ///   The probe time-to-live used when the TTL preference is not set.
    /// </summary>
    internal static TimeSpan DefaultTtl { get; } = TimeSpan.FromMinutes(DefaultTtlMinutes);

    /// <summary>
    ///   The probe timeout used when the timeout preference is not set.
    /// </summary>
    internal static TimeSpan DefaultProbeTimeout { get; } = TimeSpan.FromSeconds(DefaultProbeTimeoutSeconds);

    /// <summary>
    ///   Resolves the probe time-to-live from the configured minutes, falling back to the default
    ///   when the preference is unset or not positive.
    /// </summary>
    /// <param name="agentProbeTtlMinutes"> The configured TTL in minutes, or null when unset. </param>
    /// <returns> The effective probe time-to-live. </returns>
    internal static TimeSpan ResolveTtl(int? agentProbeTtlMinutes) =>
        agentProbeTtlMinutes is > 0 ? TimeSpan.FromMinutes(agentProbeTtlMinutes.Value) : DefaultTtl;

    /// <summary>
    ///   Resolves the probe timeout from the configured seconds, falling back to the default when
    ///   the preference is unset or not positive.
    /// </summary>
    /// <param name="agentProbeTimeoutSeconds"> The configured timeout in seconds, or null when unset. </param>
    /// <returns> The effective probe timeout. </returns>
    internal static TimeSpan ResolveTimeout(int? agentProbeTimeoutSeconds) =>
        agentProbeTimeoutSeconds is > 0 ? TimeSpan.FromSeconds(agentProbeTimeoutSeconds.Value) : DefaultProbeTimeout;
}
