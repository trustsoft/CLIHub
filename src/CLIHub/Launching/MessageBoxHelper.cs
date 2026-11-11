namespace CLIHub.Launching;

using System.Windows;

/// <summary>
///   Owns the application title, modal owner resolution, and MessageBox display for the launcher
///   services.
/// </summary>
internal static class MessageBoxHelper
{
    /// <summary>
    ///   The application title displayed in modal dialogs.
    /// </summary>
    internal const string ApplicationTitle = "CLIHub";

    /// <summary>
    ///   Resolves the window that owns the next dialog: the currently active application window
    ///   (so a prompt triggered from the Settings window is modal to Settings), then the
    ///   application main window, then none. Re-read live on every access.
    /// </summary>
    internal static Window? Owner
    {
        get
        {
            if (Application.Current is not { } application)
            {
                return null;
            }

            foreach (Window window in application.Windows)
            {
                if (window.IsActive)
                {
                    return window;
                }
            }

            return application.MainWindow;
        }
    }

    /// <summary>
    ///   Shows an information or warning message with an OK button.
    /// </summary>
    /// <param name="message"> The message text. </param>
    /// <param name="title"> The dialog title. </param>
    /// <param name="image"> The icon type. </param>
    internal static void Show(string message, string title, MessageBoxImage image)
    {
        if (Owner is { } owner)
        {
            MessageBox.Show(owner, message, title, MessageBoxButton.OK, image);
        }
        else
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, image);
        }
    }

    /// <summary>
    ///   Shows a confirmation or question dialog and returns the user's choice.
    /// </summary>
    /// <param name="message"> The message text. </param>
    /// <param name="title"> The dialog title. </param>
    /// <param name="buttons"> The button set. </param>
    /// <param name="image"> The icon type. </param>
    /// <returns> The button the user clicked. </returns>
    internal static MessageBoxResult Show(string message, string title, MessageBoxButton buttons, MessageBoxImage image)
    {
        if (Owner is { } owner)
        {
            return MessageBox.Show(owner, message, title, buttons, image);
        }

        return MessageBox.Show(message, title, buttons, image);
    }
}
