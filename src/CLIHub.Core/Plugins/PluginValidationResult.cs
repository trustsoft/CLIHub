namespace CLIHub.Core.Plugins;

/// <summary>
///   Structured result of plugin descriptor validation.
/// </summary>
public sealed class PluginValidationResult
{
    /// <summary>
    ///   Creates a validation result.
    /// </summary>
    /// <param name="errors"> Validation errors in rule evaluation order. </param>
    public PluginValidationResult(IEnumerable<string> errors)
    {
        Errors = errors?.ToArray() ?? throw new ArgumentNullException(nameof(errors));
    }

    /// <summary>
    ///   Validation errors in rule evaluation order.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    ///   Indicates whether the descriptor passed validation.
    /// </summary>
    public bool IsValid => Errors.Count == 0;
}
