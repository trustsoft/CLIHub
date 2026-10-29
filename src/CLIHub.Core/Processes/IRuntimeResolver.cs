namespace CLIHub.Core.Processes;

using CLIHub.Core.Models;

/// <summary>
/// Resolves runtime executables based on runtime kind.
/// </summary>
public interface IRuntimeResolver
{
    /// <summary>
    /// Resolves the executable path for the specified runtime kind.
    /// </summary>
    /// <param name="kind">The runtime kind to resolve.</param>
    /// <param name="cancellationToken">A cancellation token for the operation.</param>
    /// <returns>
    /// The resolved runtime executable, or null if the runtime is not available.
    /// </returns>
    Task<RuntimeExecutable?> ResolveAsync(
        RuntimeKind kind,
        CancellationToken cancellationToken = default);
}
