namespace CLIHub;

/// <summary>
///   Coordinates optional best-effort startup operations.
/// </summary>
public interface IOptionalStartupCoordinator
{
    /// <summary>
///   Runs optional startup operations with best-effort failure policy.
/// </summary>
/// <param name="startupState"> The loaded startup state. </param>
/// <param name="startupUi"> The application startup UI. </param>
/// <param name="context"> The application startup context. </param>
    void RunOptionalStartup(
        StartupState startupState,
        IApplicationStartupUi startupUi,
        ApplicationStartupContext context);
}
