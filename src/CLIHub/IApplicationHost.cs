namespace CLIHub;

/// <summary>
///   WPF lifecycle boundary that owns environment setup, DI composition, and shutdown.
/// </summary>
public interface IApplicationHost
{
    /// <summary>
    ///   Starts the application: prepares environment, builds DI, runs bootstrapper.
    /// </summary>
    /// <param name="dispatch"> Action to dispatch work to the UI thread. </param>
    /// <param name="setMainWindow"> Action to set the main window. </param>
    void Start(Action<Action> dispatch, Action<object> setMainWindow);

    /// <summary>
    ///   Shuts down the application: disposes session, stops operations, disposes DI, flushes logs.
    /// </summary>
    void Shutdown();
}
