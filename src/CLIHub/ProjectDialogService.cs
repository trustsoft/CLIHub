namespace CLIHub;

using System.Windows;

using Microsoft.Win32;

/// <summary>
///   WPF implementation of project-related modal dialogs.
/// </summary>
public sealed class ProjectDialogService : IProjectDialogService
{
    /// <inheritdoc />
    public string? SelectProjectFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Project Folder"
        };

        return dialog.ShowDialog(Application.Current?.MainWindow) == true
            ? dialog.FolderName
            : null;
    }

    /// <inheritdoc />
    public bool ConfirmProjectRemoval(string projectName) =>
        Show(
            $"Remove \"{projectName}\" from CLIHub?\n\nThe folder and its files are not deleted.",
            "CLIHub",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question) == MessageBoxResult.Yes;

    private static MessageBoxResult Show(
        string message,
        string title,
        MessageBoxButton buttons,
        MessageBoxImage image)
    {
        var owner = Application.Current?.MainWindow;

        return owner is null
            ? MessageBox.Show(message, title, buttons, image)
            : MessageBox.Show(owner, message, title, buttons, image);
    }
}
