namespace CLIHub.Helpers;

/// <summary>
///   Owns the user-facing agent state chip texts shared by the agent surfaces, so the display
///   literals live in exactly one place. The class owns display text only — no parseable state:
///   an unresolved version is a null value, never sentinel text.
/// </summary>
public static class AgentVersionText
{
    /// <summary>
    ///   The chip text shown while an installation probe is running.
    /// </summary>
    public const string Detecting = "Detecting…";

    /// <summary>
    ///   The chip text shown when the agent is not installed on the host.
    /// </summary>
    public const string NotInstalled = "Not installed";

    /// <summary>
    ///   The chip text shown when the agent is installed without a resolved version.
    /// </summary>
    public const string Installed = "Installed";
}
