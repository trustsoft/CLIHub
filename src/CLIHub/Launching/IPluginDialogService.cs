namespace CLIHub.Launching;

using CLIHub.Core.Plugins;

/// <summary>
///   Modal dialogs for plugin package installation on the CLI Agents settings page.
/// </summary>
public interface IPluginDialogService
{
    /// <summary>
    ///   Opens a file picker for a <c> .clihub-plugin.zip </c> package.
    /// </summary>
    /// <returns> The selected package path, or null when the picker is cancelled. </returns>
    string? PickPluginPackage();

    /// <summary>
    ///   Shows the installation confirmation dialog: plugin metadata and every declared
    ///   command with its exact executable and arguments, so the user can inspect all
    ///   execution paths before approving the installation.
    /// </summary>
    /// <param name="preview"> The inspection preview to confirm. </param>
    /// <returns> True when the user confirms the installation. </returns>
    bool ShowInstallConfirmation(PluginInstallPreview preview);

    /// <summary>
    ///   Shows the validation errors that make a package or installation invalid.
    /// </summary>
    /// <param name="errors"> The validation errors to list. </param>
    void ShowValidationError(IReadOnlyList<string> errors);

    /// <summary>
    ///   Shows the outcome of a completed installation.
    /// </summary>
    /// <param name="result"> The installation result. </param>
    /// <param name="pluginName"> The installed plugin's display name, when known. </param>
    void ShowInstallOutcome(PluginInstallResult result, string? pluginName);

    /// <summary>
    ///   Opens the Browse Plugins dialog showing the remote feed.
    /// </summary>
    void ShowBrowsePluginsDialog();
}
