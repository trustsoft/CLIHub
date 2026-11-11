namespace CLIHub.Launching;

using System.Windows;

/// <summary>
///   WPF implementation of user-facing information and warning dialogs.
/// </summary>
public sealed class UserNotificationService : IUserNotificationService
{
    /// <inheritdoc />
    public void ShowInformation(string message, string title) =>
        MessageBoxHelper.Show(message, title, MessageBoxImage.Information);

    /// <inheritdoc />
    public void ShowWarning(string message) =>
        MessageBoxHelper.Show(message, MessageBoxHelper.ApplicationTitle, MessageBoxImage.Warning);
}
