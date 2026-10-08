namespace CLIHub;

/// <summary>
///   Owns application-level event subscriptions for the running first-instance session.
/// </summary>
public interface IApplicationSession : IDisposable
{
    /// <summary>
    ///   Creates the startup UI and connects application-level event sources.
    /// </summary>
    /// <param name="context"> WPF lifecycle callbacks used by the session. </param>
    /// <returns> The startup UI owned by this session. </returns>
    IApplicationStartupUi Start(ApplicationStartupContext context);
}
