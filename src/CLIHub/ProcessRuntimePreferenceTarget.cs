namespace CLIHub;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;

/// <summary>
///   Adapts the process runtime boundary to the narrow preference port.
/// </summary>
public sealed class ProcessRuntimePreferenceTarget : IRuntimePreferenceTarget
{
    private readonly ProcessLauncher _processLauncher;

    /// <summary>
    ///   Creates the runtime preference target.
    /// </summary>
    /// <param name="processLauncher"> Process launcher receiving the runtime. </param>
    public ProcessRuntimePreferenceTarget(ProcessLauncher processLauncher)
    {
        _processLauncher = processLauncher ?? throw new ArgumentNullException(nameof(processLauncher));
    }

    /// <inheritdoc />
    public void SetRuntime(RuntimeKind runtime) => _processLauncher.SetRuntime(runtime);
}
