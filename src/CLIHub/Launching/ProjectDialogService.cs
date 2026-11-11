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

        return dialog.ShowDialog(MessageBoxHelper.Owner) == true
            ? dialog.FolderName
            : null;
    }

    /// <inheritdoc />
    public bool ConfirmProjectRemoval(string projectName)
    {
        return MessageBoxHelper.Show(
                   $"Remove \"{projectName}\" from CLIHub?\n\nThe folder and its files are not deleted.",
                   MessageBoxHelper.ApplicationTitle,
                   MessageBoxButton.YesNo,
                   MessageBoxImage.Question) == MessageBoxResult.Yes;
    }
}
