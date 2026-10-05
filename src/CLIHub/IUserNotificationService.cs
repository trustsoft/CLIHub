namespace CLIHub;

/// <summary>
///   Shows user-facing information and warning dialogs.
/// </summary>
public interface IUserNotificationService
{
    /// <summary>
    ///   Shows an informational message with the specified title.
    /// </summary>
    /// <param name="message"> The message to display. </param>
    /// <param name="title"> The dialog title. </param>
    void ShowInformation(string message, string title);

    /// <summary>
    ///   Shows a warning message using the CLIHub title.
    /// </summary>
    /// <param name="message"> The warning message to display. </param>
    void ShowWarning(string message);
}
