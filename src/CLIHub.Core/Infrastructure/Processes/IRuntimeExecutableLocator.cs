namespace CLIHub.Core.Infrastructure.Processes;

using CLIHub.Core.Models;

/// <summary>
///   Resolves a configured runtime to the absolute path of the executable that should be launched.
/// </summary>
public interface IRuntimeExecutableLocator
{
    /// <summary>
    ///   Locates the executable for the specified runtime.
    /// </summary>
    /// <param name="kind"> The runtime kind to resolve. </param>
    /// <returns>
    ///   The absolute path of the executable, or <c> null </c> when this locator cannot resolve the kind.
    ///   The Windows implementation resolves <see cref="RuntimeKind.GitBash" /> and
    ///   <see cref="RuntimeKind.PowerShell7" />, so <c> null </c> for the Windows-shipped kinds
    ///   (<see cref="RuntimeKind.PowerShell" />, <see cref="RuntimeKind.CommandPrompt" />) means
    ///   "not covered by this locator", not "not installed".
    /// </returns>
    /// <remarks>
    ///   The result is not cached; every call re-reads the underlying installation sources so a changed
    ///   installation is reflected on the next launch or Settings page load.
    /// </remarks>
    string? Locate(RuntimeKind kind);
}
