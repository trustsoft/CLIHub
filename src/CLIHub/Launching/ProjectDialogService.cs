namespace CLIHub.Launching;

using System.Windows;

using Microsoft.Win32;

/// <summary>
///   WPF implementation of project-related modal dialogs.
/// </summary>
public sealed class ProjectDialogService : IProjectDialogService
{
    /// <inheritdoc />
    public string? SelectProjectFolder() => this.SelectFolder("Select Project Folder");

    /// <inheritdoc />
    public string? SelectFolder(string title)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title
        };

        return dialog.ShowDialog(ResolveOwner()) == true
            ? dialog.FolderName
            : null;
    }

    /// <inheritdoc />
    public bool ConfirmProjectRemoval(string projectName)
    {
        return Show($"Remove \"{projectName}\" from CLIHub?\n\nThe folder and its files are not deleted.",
                   "CLIHub",
                   MessageBoxButton.YesNo,
                   MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    private static MessageBoxResult Show(string message,
                                         string title,
                                         MessageBoxButton buttons,
                                         MessageBoxImage image)
    {
        var owner = ResolveOwner();

        return owner is null
            ? MessageBox.Show(message, title, buttons, image)
            : MessageBox.Show(owner, message, title, buttons, image);
    }

    /// <summary>
    ///   Resolves the window that owns the next dialog: the currently active application window
    ///   (so a prompt triggered from the Settings window is modal to Settings), then the
    ///   application main window, then none.
    /// </summary>
    /// <returns> The owner window, or null when no application window is available. </returns>
    private static Window? ResolveOwner()
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
