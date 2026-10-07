namespace CLIHub;

using CLIHub.Core.Models;

/// <summary>
///   Applies the runtime used by interactive agent launches.
/// </summary>
public interface IRuntimePreferenceTarget
{
    /// <summary>
    ///   Sets the runtime for subsequent interactive launches.
    /// </summary>
    /// <param name="runtime"> Runtime to use. </param>
    void SetRuntime(RuntimeKind runtime);
}
