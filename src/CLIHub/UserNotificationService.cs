namespace CLIHub;

using System.Windows;

/// <summary>
///   WPF implementation of user-facing information and warning dialogs.
/// </summary>
public sealed class UserNotificationService : IUserNotificationService
{
    /// <inheritdoc />
    public void ShowInformation(string message, string title) =>
        Show(message, title, MessageBoxImage.Information);

    /// <inheritdoc />
    public void ShowWarning(string message) =>
        Show(message, "CLIHub", MessageBoxImage.Warning);

    private static void Show(string message, string title, MessageBoxImage image)
    {
        var owner = Application.Current?.MainWindow;

        if (owner is null)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, image);
        }
        else
        {
            MessageBox.Show(owner, message, title, MessageBoxButton.OK, image);
        }
    }
}
