namespace CLIHub.Launching;

using System.Text;

using System.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

using CLIHub.Core.Plugins;
using CLIHub.Views;

/// <summary>
///   WPF implementation of plugin installation dialogs: a package file picker, the
///   pre-installation confirmation listing every declared command, and the validation-error
///   and outcome messages.
/// </summary>
public sealed class PluginDialogService : IPluginDialogService
{
    private readonly IServiceProvider serviceProvider;

    /// <summary>
    ///   Creates the dialog service.
    /// </summary>
    /// <param name="serviceProvider"> Service provider for resolving dialog view models and windows. </param>
    public PluginDialogService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public string? PickPluginPackage()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Install Agent Plugin",
            Filter = "CLIHub plugin packages (*.clihub-plugin.zip)|*.clihub-plugin.zip|All files (*.*)|*.*",
            DefaultExt = ".clihub-plugin.zip"
        };

        return dialog.ShowDialog(ResolveOwner()) == true
            ? dialog.FileName
            : null;
    }

    /// <inheritdoc />
    public bool ShowInstallConfirmation(PluginInstallPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);

        var message = new StringBuilder()
            .AppendLine($"Name:      {preview.PluginName ?? preview.PluginId ?? "(unknown)"}")
            .AppendLine($"Version:   {preview.Version ?? "(unspecified)"}")
            .AppendLine($"Publisher: {preview.Publisher ?? "(unspecified)"}")
            .AppendLine($"Homepage:  {preview.Homepage ?? "(unspecified)"}")
            .AppendLine()
            .AppendLine("This plugin declares the following commands:")
            .AppendLine();

        if (preview.DeclaredCommands.Count == 0)
        {
            message.AppendLine("  (none declared)");
        }
        else
        {
            foreach (var command in preview.DeclaredCommands)
            {
                var invocation = string.IsNullOrEmpty(command.Arguments)
                    ? command.Executable
                    : $"{command.Executable} {command.Arguments}";
                message.AppendLine($"  {command.CommandKind}:  {invocation}");
            }
        }

        if (preview.ReplacesExistingPlugin)
        {
            message.AppendLine()
                .AppendLine(preview.ExistingPluginVersion is { } version
                    ? $"Replaces installed version {version}"
                    : "Replaces the installed plugin");
        }

        message.AppendLine()
            .Append("Only plugin.json and logo.png are installed; CLIHub never runs plugin code.");

        return Show(message.ToString(),
                    "Install Agent Plugin",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    /// <inheritdoc />
    public void ShowValidationError(IReadOnlyList<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var message = errors.Count == 0
            ? "The plugin package is invalid."
            : string.Join("\n", errors.Select(error => $"• {error}"));

        Show(message, "Plugin package invalid", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    /// <inheritdoc />
    public void ShowInstallOutcome(PluginInstallResult result, string? pluginName)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Success)
        {
            Show($"Installed {pluginName ?? result.InstalledPluginId}.", "Plugin installed", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Show(result.Errors.Count == 0
            ? "The installation failed."
            : string.Join("\n", result.Errors.Select(error => $"• {error}")),
            "Plugin installation failed",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    /// <inheritdoc />
    public void ShowBrowsePluginsDialog()
    {
        var dialog = this.serviceProvider.GetRequiredService<BrowsePluginsDialog>();
        dialog.Owner = ResolveOwner();
        dialog.ShowDialog();
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
