namespace CLIHub.ViewModels;

using System.IO;

using CLIHub.Core.Plugins;

/// <summary>
///   A disabled management card for a plugin descriptor that could not be loaded.
/// </summary>
public sealed class InvalidAgentCardItem
{
    /// <summary>
    ///   Creates a card from an invalid plugin entry.
    /// </summary>
    /// <param name="entry"> The invalid catalog entry. </param>
    public InvalidAgentCardItem(InvalidPluginEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        this.PluginDirectory = entry.PluginDirectory;
        var directoryName = Path.GetFileName(Path.TrimEndingDirectorySeparator(entry.PluginDirectory));
        this.Name = string.IsNullOrWhiteSpace(entry.PartialPlugin?.Name)
            ? (string.IsNullOrEmpty(directoryName) ? entry.PluginDirectory : directoryName)
            : entry.PartialPlugin.Name;
        this.LogoPath = null;
        this.Reason = ComposeReason(entry);
    }

    /// <summary> The directory containing the invalid descriptor. </summary>
    public string PluginDirectory { get; }

    /// <summary> The plugin name or directory basename. </summary>
    public string Name { get; }

    /// <summary> The optional plugin logo path. </summary>
    public string? LogoPath { get; }

    /// <summary> The deterministic diagnostic reason. </summary>
    public string Reason { get; }

    /// <summary> Invalid plugin cards are never enabled. </summary>
    public bool IsEnabled => false;

    /// <summary>
    ///   Composes the diagnostic reason for an invalid catalog entry.
    /// </summary>
    /// <param name="entry"> The invalid catalog entry. </param>
    /// <returns> Validation errors, the read error, or the plugin directory fallback. </returns>
    public static string ComposeReason(InvalidPluginEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.ValidationResult is { Errors.Count: > 0 } validationResult)
        {
            return string.Join("; ", validationResult.Errors);
        }

        if (entry.ReadResult?.Error is { } error && !string.IsNullOrEmpty(error.Message))
        {
            return error.Message;
        }

        return $"Plugin directory: {entry.PluginDirectory}";
    }
}
