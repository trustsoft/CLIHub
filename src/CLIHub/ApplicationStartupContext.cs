namespace CLIHub;

/// <summary>
///   WPF lifecycle callbacks supplied to the application bootstrapper.
/// </summary>
/// <param name="Dispatch"> Invokes an action on the UI dispatcher. </param>
/// <param name="SetMainWindow"> Assigns the application's main window. </param>
public sealed record ApplicationStartupContext(
    Action<Action> Dispatch,
    Action<IStartupWindow> SetMainWindow);
