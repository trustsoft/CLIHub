namespace CLIHub.Tests.Support;

using CLIHub.Core.Plugins;
using CLIHub.Launching;

/// <summary>
///   In-memory <see cref="IPluginDialogService" /> for view-model tests: the package picker
///   result and the confirmation answer are configured up front, and every dialog call is
///   recorded so a test can assert the install workflow's dialog sequence.
/// </summary>
public sealed class FakePluginDialogService : IPluginDialogService
{
    /// <summary> The path the package picker returns, or null while the picker is cancelled. </summary>
    public string? PackagePath { get; set; }

    /// <summary> The answer every installation confirmation dialog returns. </summary>
    public bool ConfirmInstallation { get; set; }

    /// <summary> How many times the package picker was opened. </summary>
    public int PickerCallCount { get; private set; }

    /// <summary> The previews the installation confirmation dialog was shown for. </summary>
    public List<PluginInstallPreview> ConfirmationPreviews { get; } = [];

    /// <summary> The error lists the validation-error dialog was shown for. </summary>
    public List<IReadOnlyList<string>> ValidationErrorsShown { get; } = [];

    /// <summary> The installation outcomes reported through the outcome dialog. </summary>
    public List<(PluginInstallResult Result, string? PluginName)> Outcomes { get; } = [];

    /// <inheritdoc />
    public string? PickPluginPackage()
    {
        this.PickerCallCount++;
        return this.PackagePath;
    }

    /// <inheritdoc />
    public bool ShowInstallConfirmation(PluginInstallPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        this.ConfirmationPreviews.Add(preview);
        return this.ConfirmInstallation;
    }

    /// <inheritdoc />
    public void ShowValidationError(IReadOnlyList<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        this.ValidationErrorsShown.Add(errors);
    }

    /// <inheritdoc />
    public void ShowInstallOutcome(PluginInstallResult result, string? pluginName)
    {
        ArgumentNullException.ThrowIfNull(result);
        this.Outcomes.Add((result, pluginName));
    }
}
