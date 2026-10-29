namespace CLIHub.Core.Plugins;

using CLIHub.Core.Models;

/// <summary>
///   Describes a plugin descriptor that could not be loaded or validated.
/// </summary>
/// <param name="PluginDirectory"> The directory containing the descriptor. </param>
/// <param name="PartialPlugin"> The partially loaded plugin, when deserialization succeeded. </param>
/// <param name="ReadResult"> The descriptor read result when reading failed. </param>
/// <param name="ValidationResult"> The validation result when validation failed. </param>
public sealed record InvalidPluginEntry(string PluginDirectory,
                                        Plugin? PartialPlugin,
                                        PluginDescriptorReadResult? ReadResult,
                                        PluginValidationResult? ValidationResult);
