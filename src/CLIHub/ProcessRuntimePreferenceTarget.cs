namespace CLIHub;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;

/// <summary>
///   Adapts the process runtime boundary to the narrow preference port.
/// </summary>
public sealed class ProcessRuntimePreferenceTarget : IRuntimePreferenceTarget
{
    private readonly IProcessLauncher _processLauncher;

    /// <summary>
    ///   Creates the runtime preference target.
    /// </summary>
    /// <param name="processLauncher"> Process boundary receiving the runtime. </param>
    public ProcessRuntimePreferenceTarget(IProcessLauncher processLauncher)
    {
        _processLauncher = processLauncher ?? throw new ArgumentNullException(nameof(processLauncher));
    }

    /// <inheritdoc />
    public void SetRuntime(RuntimeKind runtime) => _processLauncher.SetRuntime(runtime);
}
