namespace CLIHub.ViewModels;

/// <summary>
///   Remembers the last custom accent entered in Settings. The value is kept independently of the
///   selected preset, and the singleton lifetime lets it survive the Settings window being closed
///   and reopened.
/// </summary>
public sealed class AccentSelectionMemory
{
    /// <summary>
    ///   The last custom accent as <c> #RRGGBB </c>, or null when no custom value has been entered.
    /// </summary>
    public string? LastCustomAccent { get; set; }
}
