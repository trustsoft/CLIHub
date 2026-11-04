namespace CLIHub.Core.Plugins;

/// <summary>
///   The result of inspecting a <c> .clihub-plugin.zip </c> package: metadata, every declared
///   command with its exact executable and arguments, replacement info, or the validation
///   errors that make the package invalid. Produced without writing any files.
/// </summary>
/// <param name="IsValid"> Whether the package passed all validation. </param>
/// <param name="PluginId"> The declared plugin id, or null when it could not be determined. </param>
/// <param name="PluginName"> The declared plugin name, or null. </param>
/// <param name="Version"> The declared descriptor version, or null. </param>
/// <param name="Publisher"> The declared publisher, or null. </param>
/// <param name="Homepage"> The declared homepage URL, or null. </param>
/// <param name="DeclaredCommands"> Every declared command with its exact executable and arguments. </param>
/// <param name="ReplacesExistingPlugin"> Whether an installed plugin with the same id already exists. </param>
/// <param name="ExistingPluginVersion"> The currently installed version from the state file, or null. </param>
/// <param name="Errors"> The validation errors; empty when <paramref name="IsValid" /> is true. </param>
public sealed record PluginInstallPreview(
    bool IsValid,
    string? PluginId,
    string? PluginName,
    string? Version,
    string? Publisher,
    string? Homepage,
    IReadOnlyList<CommandPreview> DeclaredCommands,
    bool ReplacesExistingPlugin,
    string? ExistingPluginVersion,
    IReadOnlyList<string> Errors)
{
    /// <summary>
    ///   Creates an invalid preview that carries only the validation errors.
    /// </summary>
    /// <param name="errors"> The validation errors. </param>
    /// <returns> The invalid preview. </returns>
    public static PluginInstallPreview Invalid(IReadOnlyList<string> errors) =>
        new(false, null, null, null, null, null, [], false, null, errors);
}
