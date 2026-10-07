namespace CLIHub;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;

/// <summary>
///   Adapts the process runtime boundary to the narrow preference port.
/// </summary>
public sealed class ProcessRuntimePreferenceTarget : IRuntimePreferenceTarget
{
    private readonly IInteractiveProcessRunner _processRunner;

    /// <summary>
    ///   Creates the runtime preference target.
    /// </summary>
    /// <param name="processRunner"> Interactive process boundary receiving the runtime. </param>
    public ProcessRuntimePreferenceTarget(IInteractiveProcessRunner processRunner)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
    }

    /// <inheritdoc />
    public void SetRuntime(RuntimeKind runtime) => _processRunner.SetRuntime(runtime);
}
